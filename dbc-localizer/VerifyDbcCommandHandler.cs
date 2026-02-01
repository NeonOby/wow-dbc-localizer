using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBCD;
using DBCD.Providers;

namespace DbcLocalizer
{
	/// <summary>
	/// Handles the "verify-dbc" command for testing and validating DBC content in MPQ archives
	/// </summary>
	internal static class VerifyDbcCommandHandler
	{
		public static int Execute(string[] args)
		{
			Logger.SetLogLevel(args);

			var mpqPath = Helpers.GetArg(args, "--mpq");
			var dbcPath = Helpers.GetArg(args, "--dbc");
			var recordId = Helpers.GetArg(args, "--id");
			var defsPath = Helpers.GetArg(args, "--defs");
			var build = Helpers.GetArg(args, "--build") ?? "3.3.5.12340";
			var mpqcliPath = Helpers.GetArg(args, "--mpqcli") ?? "tools/mpqcli.exe";
			var showAllIds = args.Contains("--show-all-ids");
			var showAllLocales = args.Contains("--show-all-locales");
			var fieldsFilter = Helpers.GetArg(args, "--fields");

			if (!File.Exists(mpqPath))
				return Fail($"MPQ file not found: {mpqPath}");

			if (string.IsNullOrWhiteSpace(defsPath))
				defsPath = DefsManager.GetDefaultDefsPath();

			if (!DefsManager.EnsureDefinitions(ref defsPath, build))
				return Fail($"Definitions not found: {defsPath}");

			if (!File.Exists(mpqcliPath))
				return Fail($"mpqcli not found: {mpqcliPath}");

			// Extract DBC from MPQ
			var tempDir = Path.Combine(Path.GetTempPath(), "dbc-localizer", "verify", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(tempDir);

			try
			{
				Logger.Info($"[*] Extracting {dbcPath} from {Path.GetFileName(mpqPath)}...");
				var extractedPath = MpqHelper.ExtractFile(mpqcliPath, mpqPath, dbcPath, tempDir);

				if (!File.Exists(extractedPath))
					return Fail($"Failed to extract {dbcPath} from MPQ");

				// Load DBC
				Logger.Info($"[*] Loading DBC...");
				var dbcd = new DBCD.DBCD(
					new FilesystemDBCProvider(Path.GetDirectoryName(extractedPath)),
					new FilesystemDBDProvider(defsPath)
				);

				var tableName = Path.GetFileNameWithoutExtension(dbcPath);
				var storage = dbcd.Load(tableName, build, Locale.None);

				Logger.Info($"[+] Loaded {storage.Count} records from {tableName}");

				// Get locstring columns
				var locstringColumns = GetLocstringColumns(storage);
				if (locstringColumns.Any())
				{
					Logger.Info($"[*] Locstring columns: {string.Join(", ", locstringColumns)}");
				}

				// Get min/max IDs from actual ID column
				var dict = storage.ToDictionary();
				var allIds = new List<int>();
				foreach (var record in storage.Values)
				{
					try
					{
						var id = Convert.ToInt32(record["ID"]);
						allIds.Add(id);
					}
					catch { /* skip records without ID */ }
				}
				allIds = allIds.OrderBy(id => id).ToList();
				if (allIds.Any())
				{
					Logger.Info($"[*] ID range: {allIds.First()} - {allIds.Last()}");
				}

				// If specific record ID requested
				if (!string.IsNullOrWhiteSpace(recordId))
				{
					if (!int.TryParse(recordId, out var id))
						return Fail($"Invalid record ID: {recordId}");

					Logger.Info($"");
					Logger.Info($"[*] Searching for Spell ID {id}...");

					DBCDRow? foundRecord = null;
					foreach (var record in storage.Values)
					{
						try
						{
							var recordSpellId = Convert.ToInt32(record["ID"]);
							if (recordSpellId == id)
							{
								foundRecord = record;
								break;
							}
						}
						catch { /* skip */ }
					}

					if (foundRecord == null)
					{
						Logger.Error($"[-] Spell ID {id} not found in {tableName}");
						return 1;
					}

					Logger.Info($"[+] Found Spell ID {id}:");
					DisplayRecord(foundRecord, locstringColumns, showAllLocales, fieldsFilter);
					return 0;
				}

				// If show-all-ids flag set
				if (showAllIds)
				{
					Logger.Info($"");
					Logger.Info($"[*] All Spell IDs grouped by thousands:");
					Logger.Info($"");

					var groups = allIds.GroupBy(id => id / 1000);
					foreach (var group in groups)
					{
						var rangeStart = group.Key * 1000;
						var rangeEnd = rangeStart + 999;
						var count = group.Count();
						var firstId = group.First();
						var lastId = group.Last();
						
						Logger.Info($"  [{rangeStart:D6} - {rangeEnd:D6}]: {count,4} IDs (actual: {firstId} - {lastId})");
					}
					return 0;
				}

				// Otherwise show summary
				Logger.Info($"");

				var recordsWithLoc = dict.Count(kvp => HasNonEmptyLocstring(kvp.Value, locstringColumns));
				Logger.Info($"[*] Records with non-empty locstrings: {recordsWithLoc} / {dict.Count}");

				Logger.Info($"[*] Sample records (first 3):");
				var samplesShown = 0;
				foreach (var kvp in dict.Take(10))
				{
					if (samplesShown >= 3)
						break;

					if (HasNonEmptyLocstring(kvp.Value, locstringColumns))
					{
						Logger.Info($"");
						Logger.Info($"  Record ID {kvp.Key}:");
						DisplayRecord(kvp.Value, locstringColumns);
						samplesShown++;
					}
				}

				return 0;
			}
			catch (Exception ex)
			{
				Logger.Error($"[-] Error: {ex.Message}");
				Logger.Verbose($"{ex}");
				return 1;
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					try { Directory.Delete(tempDir, true); }
					catch { /* ignore */ }
				}
			}
		}

		private static List<string> GetLocstringColumns(IDBCDStorage storage)
		{
			var locstringColumns = new List<string>();

			if (storage.AvailableColumns.Length == 0)
				return locstringColumns;

			// Get first record to check column types
			var firstRecord = storage.Values.FirstOrDefault();
			if (firstRecord == null)
				return locstringColumns;

			foreach (var col in storage.AvailableColumns)
			{
				try
				{
					var value = firstRecord[col];
					if (value is string[] stringArray && stringArray.Length > 0)
					{
						locstringColumns.Add(col);
					}
				}
				catch
				{
					// Skip columns that can't be accessed
				}
			}

			return locstringColumns;
		}

		private static bool HasNonEmptyLocstring(DBCDRow record, List<string> locstringColumns)
		{
			if (!locstringColumns.Any())
				return false;

			foreach (var col in locstringColumns)
			{
				try
				{
					var value = record[col];
					if (value is string[] stringArray)
					{
						if (stringArray.Any(s => !string.IsNullOrWhiteSpace(s)))
							return true;
					}
				}
				catch
				{
					// Skip
				}
			}

			return false;
		}

		private static void DisplayRecord(DBCDRow record, List<string> locstringColumns, bool showAllLocales = false, string? fieldsFilter = null)
		{
			var localeNames = new[] { "enUS", "koKR", "frFR", "deDE", "zhCN", "zhTW", "esES", "esMX", "ruRU", "unk9", "ptBR", "itIT", "unk12", "unk13", "unk14", "unk15" };
			var filterFields = string.IsNullOrWhiteSpace(fieldsFilter) 
				? null 
				: fieldsFilter.Split(',').Select(f => f.Trim()).ToList();

			foreach (var col in record.GetDynamicMemberNames())
			{
				// Apply field filter if specified
				if (filterFields != null && !filterFields.Contains(col))
					continue;

				try
				{
					var value = record[col];

					if (locstringColumns.Contains(col))
					{
						if (value is string[] stringArray)
						{
							if (showAllLocales)
							{
								// Show all 16 locale slots
								Logger.Info($"    {col}:");
								for (int i = 0; i < Math.Min(stringArray.Length, 16); i++)
								{
									var text = stringArray[i] ?? "";
									var isEmpty = string.IsNullOrWhiteSpace(text);
									Logger.Info($"      [{i:D2}] {localeNames[i],-6}: {(isEmpty ? "(empty)" : $"'{text}'")}");
								}
							}
							else
							{
								// Show only non-empty values (original behavior)
								var nonEmptyTexts = stringArray.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
								if (nonEmptyTexts.Any())
								{
									Logger.Info($"    {col}: {string.Join(" | ", nonEmptyTexts)}");
								}
							}
						}
					}
					else
					{
						var displayValue = value?.ToString() ?? "(null)";
						if (displayValue.Length > 100)
							displayValue = displayValue.Substring(0, 100) + "...";

						Logger.Info($"    {col}: {displayValue}");
					}
				}
				catch (Exception ex)
				{
					Logger.Verbose($"    {col}: <error: {ex.Message}>");
				}
			}
		}

		private static int Fail(string message)
		{
			Logger.Error($"[-] {message}");
			return 1;
		}
	}
}
