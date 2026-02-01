using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBCD;
using DBCD.Providers;

namespace DbcLocalizer
{
	/// <summary>
	/// Simplified localization engine:
	/// 1. Iterate through all patch.mpq files
	/// 2. For each patch, check which tables need localization
	/// 3. Iterate through all locale.mpq files
	/// 4. For each locale, check which tables exist
	/// 5. Extract DBCs if not already done
	/// 6. Go row by row through patch DBC and update from all available locale DBCs
	/// </summary>
	internal static class SimplifiedLocalizeEngine
	{
		/// <summary>
		/// Extract base locale code from strings like "patch-deDE-3" -> "deDE"
		/// </summary>
		private static string ExtractLocaleCode(string input)
		{
			var lower = input.ToLowerInvariant();
			
			// Known locale codes in priority order (longer first to avoid false matches)
			string[] knownLocales = { "enus", "engb", "kokr", "frfr", "dede", "zhcn", "encn", "zhtw", "entw", "eses", "esmx", "ruru", "ptbr", "ptpt", "itit" };
			
			foreach (var locale in knownLocales)
			{
				if (lower.Contains(locale))
					return locale;
			}
			
			return "enus"; // fallback
		}

		private static DBCD.Locale GetDbcdLocale(string localeCode)
		{
		// Extract base locale code (e.g., "deDE" from "patch-deDE-3")
		var code = localeCode.ToLowerInvariant();
		
		// Try to find a known locale substring
		if (code.Contains("enus") || code.Contains("engb")) return DBCD.Locale.EnUS;
		if (code.Contains("kokr")) return DBCD.Locale.KoKR;
		if (code.Contains("frfr")) return DBCD.Locale.FrFR;
		if (code.Contains("dede")) return DBCD.Locale.DeDE;
		if (code.Contains("zhcn") || code.Contains("encn")) return DBCD.Locale.EnCN;
		if (code.Contains("zhtw") || code.Contains("entw")) return DBCD.Locale.EnTW;
		if (code.Contains("eses")) return DBCD.Locale.EsES;
		if (code.Contains("esmx")) return DBCD.Locale.EsMX;
		if (code.Contains("ruru")) return DBCD.Locale.RuRU;
		if (code.Contains("ptpt") || code.Contains("ptbr")) return DBCD.Locale.PtPT;
		if (code.Contains("itit")) return DBCD.Locale.ItIT;
		
		return DBCD.Locale.None;
	}

	/// <summary>
	/// Localize a DBC by applying all available locale data
	/// </summary>
	public static Dictionary<string, LocalizeStats> LocalizeDbcSimplified(
		string patchDbcPath,
		Dictionary<string, string> localeDbcPaths, // localeCode -> path
			string defsPath,
			string outputPath,
			string build,
			string fallbackLocale = "enUS",
			bool trackSampleChanges = true,
			Dictionary<string, List<SampleChange>>? sampleChanges = null)
		{
			var tableName = Path.GetFileNameWithoutExtension(patchDbcPath);
			Logger.Info($"[*] Localizing DBC: {tableName}");

			// Load DBD definition to get locstring fields
			var dbdProvider = new FilesystemDBDProvider(defsPath);
			var dbdStream = dbdProvider.StreamForTableName(tableName, build);
			var dbdDefinition = new DBDefsLib.DBDReader().Read(dbdStream);
			
			if (!DBDefsLib.Utils.GetVersionDefinitionByBuild(dbdDefinition, new DBDefsLib.Build(build), out var versionDef) || versionDef == null)
			{
				Logger.Error($"No DBD version definition found for build {build}");
				return new Dictionary<string, LocalizeStats>();
			}

			var locFields = Helpers.GetLocStringFields(versionDef.Value, dbdDefinition);
			if (locFields.Count == 0)
			{
				Logger.Info($"[*] No locstring fields found for {tableName}");
				return new Dictionary<string, LocalizeStats>();
			}

			Logger.Info($"[*] Locstring fields: {locFields.Count}");

			// Initialize per-locale statistics
			var localeStats = new Dictionary<string, LocalizeStats>();
			foreach (var localeCode in localeDbcPaths.Keys)
			{
				localeStats[localeCode] = new LocalizeStats { RowsMerged = 0, FieldUpdates = 0 };
			}

			// Load patch DBC
			var patchProvider = new FilesystemDBCProvider(Path.GetDirectoryName(patchDbcPath) ?? ".");
			var patchDbcd = new DBCD.DBCD(patchProvider, dbdProvider);
			var patchStorage = patchDbcd.Load(tableName, build, DBCD.Locale.None);

			// Load all locale DBCs
			var localeStorages = new Dictionary<string, IDBCDStorage>();
			foreach (var kvp in localeDbcPaths)
			{
				var localeCode = kvp.Key;
				var localePath = kvp.Value;
				
				var localeDbcdLocale = GetDbcdLocale(localeCode);
				if (localeDbcdLocale == DBCD.Locale.None)
					continue;

				try
				{
					var localeProvider = new FilesystemDBCProvider(Path.GetDirectoryName(localePath) ?? ".");
					var localeDbcd = new DBCD.DBCD(localeProvider, dbdProvider);
					var storage = localeDbcd.Load(tableName, build, localeDbcdLocale);
					localeStorages[localeCode] = storage;
					Logger.Info($"[*] Loaded locale: {localeCode}");
				}
				catch (Exception ex)
				{
					Logger.Verbose($"[!] Failed to load locale {localeCode}: {ex.Message}");
				}
			}

			if (localeStorages.Count == 0)
			{
				Logger.Info($"[*] No locale data available for {tableName}, saving patch DBC unchanged");
				
				// Save patch DBC unchanged
				var outputDir = Path.GetDirectoryName(outputPath);
				if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
					Directory.CreateDirectory(outputDir);
				
				patchStorage.Save(outputPath);
				Logger.Info($"[*] Wrote: {outputPath}");
				return localeStats;
			}

			// Get fallback index
			int fallbackIndex = Helpers.GetLocaleIndex(fallbackLocale, build);		// Build ID index for fast O(1) lookup (instead of O(n) iteration)
		var localeIdIndices = new Dictionary<string, Dictionary<int, DBCDRow>>();
		foreach (var kvp in localeStorages)
		{
			var localeCode = kvp.Key;
			var storage = kvp.Value;
			var idIndex = new Dictionary<int, DBCDRow>();
			foreach (var row in storage.Values)
			{
				try
				{
					var rowId = Convert.ToInt32(row["ID"]);
					idIndex[rowId] = row;
				}
				catch { }
			}
			localeIdIndices[localeCode] = idIndex;
		}
			// Track which rows were updated per locale
			var updatedRows = new Dictionary<string, HashSet<int>>();
			foreach (var localeCode in localeDbcPaths.Keys)
			{
				updatedRows[localeCode] = new HashSet<int>();
			}

			// Sample changes tracking
			if (trackSampleChanges && sampleChanges != null && !sampleChanges.ContainsKey(tableName))
			{
				sampleChanges[tableName] = new List<SampleChange>();
			}

			// Process each row in patch DBC
			int rowsProcessed = 0;
			int fieldsUpdated = 0;

			foreach (var patchRow in patchStorage.Values)
			{
				int id;
				try
				{
					id = Convert.ToInt32(patchRow["ID"]);
				}
				catch
				{
					continue;
				}

				// For each locstring field, update from all available locales
				foreach (var field in locFields)
				{
					if (!patchStorage.AvailableColumns.Contains(field))
						continue;

					try
					{
						var value = patchRow[field];
						if (value is not string[] arr)
							continue;

						// Apply data from each locale
						foreach (var kvp in localeStorages)
						{
							var localeCode = kvp.Key;
							var localeStorage = kvp.Value;
							
							// Extract base locale code (e.g., "deDE" from "patch-deDE-3")
							string baseLocaleCode = ExtractLocaleCode(localeCode);
							int localeIndex = Helpers.GetLocaleIndex(baseLocaleCode, build);

							if (localeIndex < 0 || localeIndex >= arr.Length)
								continue;

						// Get locale row using ID index (O(1) lookup instead of O(n) iteration)
						DBCDRow? localeRow = null;
						if (localeIdIndices.ContainsKey(localeCode) && localeIdIndices[localeCode].TryGetValue(id, out var row))
						{
							localeRow = row;
						}

						if (localeRow == null)
							continue;
							// NOTE: In locale-specific DBC, the localized text is always at index 0
							// because the DBC was loaded with a specific locale (e.g., DBCD.Locale.DeDE)
							string locStr = string.Empty;
							try
							{
								var locValue = localeRow[field];
								if (locValue is string[] locArray && locArray.Length > 0)
								{
									locStr = locArray[0] ?? string.Empty; // Read from index 0 of locale DBC
								
								// Debug: Show what's actually in the locale DBC for first few IDs
								if (id <= 15 && field == "Name_lang")
								{
									Logger.Verbose($"[LOCALE DBC] ID={id}, Field={field}, LocaleCode={localeCode}, Value='{locStr}'");
								}
							}
							else if (locValue is string str)
							{
								locStr = str ?? string.Empty;
							}
						}
						catch
						{
							continue;
						}

						// Apply to patch row if not empty
						if (!string.IsNullOrWhiteSpace(locStr))
						{
							// Capture values for sample changes
							string valueEnUS = arr[fallbackIndex] ?? string.Empty;
							string valueLocaleBefore = arr[localeIndex] ?? string.Empty;

// Apply localized string
arr[localeIndex] = locStr;
fieldsUpdated++;

// Update stats
if (localeStats.ContainsKey(localeCode))
{
localeStats[localeCode].FieldUpdates++;
updatedRows[localeCode].Add(id);
}

// Track sample change (max 10 samples per table, only actual changes)
if (trackSampleChanges && sampleChanges != null && sampleChanges.ContainsKey(tableName))
{
var list = sampleChanges[tableName];
// Only track if value actually changed from what was in locale column
									if (list.Count < 10 && !string.Equals(valueLocaleBefore, locStr, StringComparison.Ordinal))
									{
										list.Add(new SampleChange
										{
											ID = id,
											Field = field,
											Value_enUS = valueEnUS,
											Value_Locale_Before = valueLocaleBefore,
											Value_Locale_After = locStr
										});
									}
								}
							}
							else if (!string.IsNullOrWhiteSpace(arr[fallbackIndex]))
							{
								// Apply fallback (don't count as update)
								arr[localeIndex] = arr[fallbackIndex];
							}
						}

						patchRow[field] = arr;
					}
					catch (Exception ex)
					{
						Logger.Verbose($"[!] Error processing field {field} for ID {id}: {ex.Message}");
					}
				}

				rowsProcessed++;
			}

			// Save output
			var outDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrWhiteSpace(outDir) && !Directory.Exists(outDir))
				Directory.CreateDirectory(outDir);

			patchStorage.Save(outputPath);

			// Calculate RowsMerged for each locale
			foreach (var kvp in updatedRows)
			{
				if (localeStats.ContainsKey(kvp.Key))
				{
					localeStats[kvp.Key].RowsMerged = kvp.Value.Count;
				}
			}
			
			Logger.Info($"[*] Rows processed: {rowsProcessed}");
			Logger.Info($"[*] Fields updated: {fieldsUpdated}");
			Logger.Info($"[*] Wrote: {outputPath}");

			return localeStats;
		}
	}
}

