using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBCD;
using DBCD.Providers;

namespace DbcMerger
{
	internal static class Program
	{
		private const string DefaultBuild = "3.3.5.12340";

		private static int Main(string[] args)
		{
			if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
			{
				PrintUsage();
				return 1;
			}

			try
			{
				var command = args[0].ToLowerInvariant();
				return command switch
				{
					"merge" => CommandMerge(args.Skip(1).ToArray()),
					"merge-mpq" => CommandMergeMpq(args.Skip(1).ToArray()),
					_ => Fail($"Unknown command: {command}")
				};
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine("ERROR: " + ex);
				return 1;
			}
		}

		private static int CommandMerge(string[] args)
		{
			// Required args
			var basePath = GetArg(args, "--base");
			var localePath = GetArg(args, "--locale");
			var defsPath = GetArg(args, "--defs");
			var outputPath = GetArg(args, "--output");
			var build = GetArg(args, "--build") ?? DefaultBuild;
			var localeCode = GetArg(args, "--lang") ?? "deDE";

			if (string.IsNullOrWhiteSpace(basePath) || string.IsNullOrWhiteSpace(localePath) ||
				string.IsNullOrWhiteSpace(defsPath) || string.IsNullOrWhiteSpace(outputPath))
			{
				return Fail("Missing required arguments. Use --base, --locale, --defs, --output.");
			}

			if (!File.Exists(basePath))
				return Fail($"Base DBC not found: {basePath}");
			if (!File.Exists(localePath))
				return Fail($"Locale DBC not found: {localePath}");
			if (!Directory.Exists(defsPath))
				return Fail($"Definitions path not found: {defsPath}");

			var baseName = Path.GetFileNameWithoutExtension(basePath);
			var localeName = Path.GetFileNameWithoutExtension(localePath);
			if (!string.Equals(baseName, localeName, StringComparison.OrdinalIgnoreCase))
			{
				return Fail("Base and locale DBC names must match (e.g., Spell.dbc + Spell.dbc).", 2);
			}

			return MergeDbcInternal(basePath, localePath, defsPath, outputPath, build, localeCode);
		}

		private static int CommandMergeMpq(string[] args)
		{
			var patchMpq = GetArg(args, "--patch");
			var localeMpq = GetArg(args, "--locale-mpq");
			var defsPath = GetArg(args, "--defs");
			var outputMpq = GetArg(args, "--output");
			var dbcRelPath = GetArg(args, "--dbc") ?? "DBFilesClient\\Spell.dbc";
			var build = GetArg(args, "--build") ?? DefaultBuild;
			var localeCode = GetArg(args, "--lang") ?? "deDE";
			var mpqcliPath = GetArg(args, "--mpqcli") ?? GetDefaultMpqCliPath();
			var keepTemp = args.Any(a => a.Equals("--keep-temp", StringComparison.OrdinalIgnoreCase));

			if (string.IsNullOrWhiteSpace(patchMpq) || string.IsNullOrWhiteSpace(localeMpq) ||
				string.IsNullOrWhiteSpace(defsPath) || string.IsNullOrWhiteSpace(outputMpq))
			{
				return Fail("Missing required arguments. Use --patch, --locale-mpq, --defs, --output.");
			}

			if (!File.Exists(patchMpq))
				return Fail($"Patch MPQ not found: {patchMpq}");
			if (!File.Exists(localeMpq))
				return Fail($"Locale MPQ not found: {localeMpq}");
			if (!Directory.Exists(defsPath))
				return Fail($"Definitions path not found: {defsPath}");
			if (!File.Exists(mpqcliPath))
				return Fail($"mpqcli not found: {mpqcliPath}");

			var tempRoot = Path.Combine(Path.GetTempPath(), "dbc-merger", Guid.NewGuid().ToString("N"));
			var patchExtractDir = Path.Combine(tempRoot, "patch");
			var localeExtractDir = Path.Combine(tempRoot, "locale");
			var mergedDir = Path.Combine(tempRoot, "merged");
			Directory.CreateDirectory(patchExtractDir);
			Directory.CreateDirectory(localeExtractDir);
			Directory.CreateDirectory(mergedDir);

			Console.WriteLine($"[*] Using temp dir: {tempRoot}");

			var patchDbcPath = ExtractMpqFile(mpqcliPath, patchMpq, dbcRelPath, patchExtractDir);
			var localeDbcPath = ExtractMpqFile(mpqcliPath, localeMpq, dbcRelPath, localeExtractDir);
			var mergedDbcPath = Path.Combine(mergedDir, Path.GetFileName(dbcRelPath));

			var mergeResult = MergeDbcInternal(patchDbcPath, localeDbcPath, defsPath, mergedDbcPath, build, localeCode);
			if (mergeResult != 0)
				return mergeResult;

			// Copy patch MPQ to output and replace DBC
			var outputDir = Path.GetDirectoryName(outputMpq);
			if (!string.IsNullOrWhiteSpace(outputDir))
				Directory.CreateDirectory(outputDir);

			File.Copy(patchMpq, outputMpq, true);

			RemoveMpqFile(mpqcliPath, outputMpq, dbcRelPath);
			AddMpqFile(mpqcliPath, outputMpq, mergedDbcPath, Path.GetDirectoryName(dbcRelPath) ?? string.Empty);

			if (!keepTemp)
			{
				try { Directory.Delete(tempRoot, true); } catch { }
			}

			Console.WriteLine($"[*] Output MPQ: {outputMpq}");
			return 0;
		}

		private static int MergeDbcInternal(string basePath, string localePath, string defsPath, string outputPath, string build, string localeCode)
		{
			var baseName = Path.GetFileNameWithoutExtension(basePath);

			Console.WriteLine($"[*] Merge DBC: {baseName}");
			Console.WriteLine($"[*] Build: {build}");

			var dbdProvider = new FilesystemDBDProvider(defsPath);

			// Load base DBC
			var baseProvider = new FilesystemDBCProvider(Path.GetDirectoryName(basePath) ?? ".");
			var baseDbcd = new DBCD.DBCD(baseProvider, dbdProvider);
			var baseStorage = baseDbcd.Load(baseName, build, DBCD.Locale.None);

			// Read DBD definition to locate locstring fields
			var dbdStream = dbdProvider.StreamForTableName(baseName, build);
			var dbdDefinition = new DBDefsLib.DBDReader().Read(dbdStream);
			if (!DBDefsLib.Utils.GetVersionDefinitionByBuild(dbdDefinition, new DBDefsLib.Build(build), out var versionDef) || versionDef == null)
				return Fail($"No DBD version definition found for build {build}");

			var locFields = GetLocStringFields(versionDef.Value, dbdDefinition);
			Console.WriteLine($"[*] Locstring fields: {locFields.Count}");

			var localeIndex = GetLocaleIndex(localeCode, build);
			var localeStrings = ReadLocaleStrings(localePath, versionDef.Value, dbdDefinition, localeIndex);

			int mergedRows = 0;
			foreach (var kvp in baseStorage.ToDictionary())
			{
				var id = kvp.Key;
				var baseRow = kvp.Value;

				if (!localeStrings.TryGetValue(id, out var strings))
					continue;

				bool changed = MergeRow(baseRow, strings, locFields, baseStorage.AvailableColumns, localeIndex);
				if (changed)
					mergedRows++;
			}

			Console.WriteLine($"[*] Rows merged: {mergedRows}");

			// Ensure output directory exists
			var outputDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			baseStorage.Save(outputPath);
			Console.WriteLine($"[*] Wrote merged DBC: {outputPath}");

			return 0;
		}

		private static string ExtractMpqFile(string mpqcliPath, string mpqPath, string filePath, string outputDir)
		{
			Directory.CreateDirectory(outputDir);
			RunProcess(mpqcliPath, $"extract \"{mpqPath}\" --file \"{filePath}\" --output \"{outputDir}\" --keep");
			var extractedPath = Path.Combine(outputDir, filePath);
			if (!File.Exists(extractedPath))
				throw new FileNotFoundException($"Extracted file not found: {extractedPath}");
			return extractedPath;
		}

		private static void RemoveMpqFile(string mpqcliPath, string mpqPath, string filePath)
		{
			RunProcess(mpqcliPath, $"remove \"{filePath}\" \"{mpqPath}\"");
		}

		private static void AddMpqFile(string mpqcliPath, string mpqPath, string localPath, string archivePath)
		{
			if (string.IsNullOrWhiteSpace(archivePath))
			{
				RunProcess(mpqcliPath, $"add \"{localPath}\" \"{mpqPath}\"");
			}
			else
			{
				RunProcess(mpqcliPath, $"add \"{localPath}\" \"{mpqPath}\" --path \"{archivePath}\"");
			}
		}

		private static void RunProcess(string exePath, string arguments)
		{
			var info = new System.Diagnostics.ProcessStartInfo
			{
				FileName = exePath,
				Arguments = arguments,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = System.Diagnostics.Process.Start(info);
			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				throw new Exception($"Command failed: {exePath} {arguments}\n{error}");
			}

			if (!string.IsNullOrWhiteSpace(output))
				Console.WriteLine(output.Trim());
		}

		private static string GetDefaultMpqCliPath()
		{
			var baseDir = AppContext.BaseDirectory;
			var candidate = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "tools", "mpqcli.exe"));
			return candidate;
		}

		private static List<string> GetLocStringFields(DBDefsLib.Structs.VersionDefinitions versionDef, DBDefsLib.Structs.DBDefinition dbd)
		{
			var list = new List<string>();
			foreach (var def in versionDef.definitions)
			{
				if (!dbd.columnDefinitions.TryGetValue(def.name, out var col))
					continue;

				if (string.Equals(col.type, "locstring", StringComparison.OrdinalIgnoreCase))
					list.Add(def.name);
			}
			return list;
		}

		private static bool MergeRow(DBCDRow baseRow, List<string> localeValues, List<string> locFields, string[] availableColumns, int localeIndex)
		{
			bool changed = false;

			for (int i = 0; i < locFields.Count; i++)
			{
				if (i >= localeValues.Count)
					break;

				var text = localeValues[i];
				if (string.IsNullOrWhiteSpace(text))
					continue;

				var field = locFields[i];
				try
				{
					var value = baseRow[field];
					if (value is string[] arr)
					{
						if (localeIndex >= 0 && localeIndex < arr.Length)
						{
							arr[localeIndex] = text;
							baseRow[field] = arr;
							changed = true;

							var maskField = field + "_mask";
							if (availableColumns.Contains(maskField))
							{
								var currentMask = Convert.ToUInt32(baseRow[maskField]);
								if (localeIndex < 32)
								{
									baseRow[maskField] = currentMask | (1u << localeIndex);
								}
							}
						}
					}
					else if (value is string)
					{
						baseRow[field] = text;
						changed = true;
					}
				}
				catch
				{
					// Ignore field assignment errors
				}
			}

			return changed;
		}

		private static Dictionary<int, List<string>> ReadLocaleStrings(
			string localeDbcPath,
			DBDefsLib.Structs.VersionDefinitions versionDef,
			DBDefsLib.Structs.DBDefinition dbd,
			int localeIndex)
		{
			var data = File.ReadAllBytes(localeDbcPath);
			if (data.Length < 20)
				throw new Exception("Invalid DBC file (too small)");

			// Header
			uint records = BitConverter.ToUInt32(data, 4);
			uint fields = BitConverter.ToUInt32(data, 8);
			uint recordSize = BitConverter.ToUInt32(data, 12);

			int headerSize = 20;
			int stringBlockStart = headerSize + (int)records * (int)recordSize;

			// Calculate locstring field counts
			int locCount = 0;
			int nonLocCount = 0;

			foreach (var def in versionDef.definitions)
			{
				if (!dbd.columnDefinitions.TryGetValue(def.name, out var col))
					continue;

				int arrLength = def.arrLength > 0 ? def.arrLength : 1;

				if (string.Equals(col.type, "locstring", StringComparison.OrdinalIgnoreCase))
				{
					locCount += 1;
				}
				else
				{
					nonLocCount += arrLength;
				}
			}

			if (locCount == 0)
				return new Dictionary<int, List<string>>();

			int locWidth = (int)((fields - nonLocCount) / locCount);
			if (locWidth <= 0)
				throw new Exception("Could not compute localized string width.");

			var result = new Dictionary<int, List<string>>();

			for (int rec = 0; rec < records; rec++)
			{
				int offset = headerSize + rec * (int)recordSize;
				int pos = offset;

				int id = -1;
				var strings = new List<string>();

				foreach (var def in versionDef.definitions)
				{
					if (!dbd.columnDefinitions.TryGetValue(def.name, out var col))
						continue;

					int arrLength = def.arrLength > 0 ? def.arrLength : 1;

					if (def.isID)
					{
						id = BitConverter.ToInt32(data, pos);
					}

					if (string.Equals(col.type, "locstring", StringComparison.OrdinalIgnoreCase))
					{
						int localeOffset = 0;
						if (localeIndex >= 0 && localeIndex < locWidth)
						{
							localeOffset = BitConverter.ToInt32(data, pos + (localeIndex * 4));
						}

						string value = ReadString(data, stringBlockStart, localeOffset);
						strings.Add(value);

						pos += locWidth * 4;
					}
					else
					{
						pos += GetFieldSize(col.type, def.size) * arrLength;
					}
				}

				if (id != -1)
					result[id] = strings;
			}

			return result;
		}

		private static int GetFieldSize(string type, int sizeBits)
		{
			switch (type)
			{
				case "int":
					return Math.Max(1, sizeBits / 8);
				case "float":
					return 4;
				case "string":
					return 4;
				default:
					return 4;
			}
		}

		private static string ReadString(byte[] data, int stringBlockStart, int offset)
		{
			if (offset <= 0)
				return string.Empty;

			int pos = stringBlockStart + offset;
			if (pos < 0 || pos >= data.Length)
				return string.Empty;

			int end = pos;
			while (end < data.Length && data[end] != 0)
				end++;

			return System.Text.Encoding.UTF8.GetString(data, pos, end - pos);
		}

		private static int GetLocaleIndex(string code, string build)
		{
			if (!DBDefsLib.Build.TryParse(build, out var parsed))
				return 0;

			int locSize = GetLocStringSize(parsed);
			if (locSize == 1)
				return 0;

			return code.Trim().ToUpperInvariant() switch
			{
				"ENUS" => 0,
				"KOKR" => 1,
				"FRFR" => 2,
				"DEDE" => 3,
				"ENCN" => 4,
				"ZHCN" => 4,
				"ENTW" => 5,
				"ZHTW" => 5,
				"ESES" => 6,
				"ESMX" => 7,
				"RURU" => 8,
				"JAJP" => 9,
				"PTPT" => 10,
				"PTBR" => 10,
				"ITIT" => 11,
				_ => 0
			};
		}

		private static int GetLocStringSize(DBDefsLib.Build build)
		{
			if (build.expansion >= 4 || build.build > 12340)
				return 1;
			if (build.build >= 6692)
				return 16;
			return 8;
		}

		private static string GetArg(string[] args, string key)
		{
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
					return args[i + 1];
			}
			return null;
		}

		private static int Fail(string message, int exitCode = 1)
		{
			Console.Error.WriteLine($"ERROR: {message}");
			return exitCode;
		}

				private static void PrintUsage()
		{
						Console.WriteLine(@"
dbc-merger - DBCD-based DBC merger

USAGE:
	dbc-merger merge --base <base.dbc> --locale <locale.dbc> --defs <defs_dir> --output <out.dbc> [--build 3.3.5.12340] [--lang deDE]

	dbc-merger merge-mpq --patch <patch.mpq> --locale-mpq <locale.mpq> --defs <defs_dir> --output <out.mpq>
											[--dbc DBFilesClient\\Spell.dbc] [--build 3.3.5.12340] [--lang deDE] [--mpqcli <path>] [--keep-temp]

EXAMPLES:
	dbc-merger merge --base Spell.dbc --locale Spell.dbc --defs .\definitions --output Spell_merged.dbc --lang deDE

	dbc-merger merge-mpq --patch patch-B.mpq --locale-mpq locale-deDE.MPQ --defs .\definitions --output Patch-B-merged.mpq \
		--dbc DBFilesClient\\Spell.dbc --lang deDE
");
		}
	}
}
