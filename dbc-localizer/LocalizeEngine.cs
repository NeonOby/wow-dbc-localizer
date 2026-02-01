using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBCD;
using DBCD.Providers;

namespace DbcLocalizer
{
	internal static class LocalizeEngine
	{
		private static Dictionary<string, List<SampleChange>>? _sampleChanges;
		private static bool _trackChanges;

		public static int LocalizeDbc(
			string basePath,
			string localePath,
			string defsPath,
			string outputPath,
			string build,
			string localeCode,
			string localeSource,
			string targetPath,
			Action<string>? verboseLog,
			out LocalizeStats stats,
			string fallbackLocale = "enUS",
			bool trackChanges = false,
			Dictionary<string, List<SampleChange>>? sampleChanges = null)
		{
			_trackChanges = trackChanges;
			_sampleChanges = sampleChanges;
			stats = new LocalizeStats();
			var baseName = Path.GetFileNameWithoutExtension(basePath);

			Logger.Info($"[*] Localize DBC: {baseName}");
			Logger.Info($"[*] Build: {build}");

			var dbdProvider = new FilesystemDBDProvider(defsPath);

			// Load base DBC
			var baseProvider = new FilesystemDBCProvider(Path.GetDirectoryName(basePath) ?? ".");
			var baseDbcd = new DBCD.DBCD(baseProvider, dbdProvider);
			var baseStorage = baseDbcd.Load(baseName, build, DBCD.Locale.None);

			// Read DBD definition to locate locstring fields
			var dbdStream = dbdProvider.StreamForTableName(baseName, build);
			var dbdDefinition = new DBDefsLib.DBDReader().Read(dbdStream);
			if (!DBDefsLib.Utils.GetVersionDefinitionByBuild(dbdDefinition, new DBDefsLib.Build(build), out var versionDef) || versionDef == null)
			{
				Logger.Error($"No DBD version definition found for build {build}");
				return 1;
			}

			var locFields = Helpers.GetLocStringFields(versionDef.Value, dbdDefinition);
			Logger.Info($"[*] Locstring fields: {locFields.Count}");

			var localeIndex = Helpers.GetLocaleIndex(localeCode, build);
			var localeStrings = ReadLocaleStrings(localePath, versionDef.Value, dbdDefinition, localeIndex);

			int mergedRows = 0;
			int fieldsUpdated = 0;
			var testCases = new List<VerificationTestCase>();
			int testCaseCounter = 0;
			const int maxTestCases = 10; // Sample up to 10 test cases per category

			foreach (var baseRow in baseStorage.Values)
			{
				// Get the actual ID from the row instead of using row index
				int id;
				try
				{
					id = Convert.ToInt32(baseRow["ID"]);
				}
				catch
				{
					Logger.Verbose($"[!] Row without ID column - skipping");
					continue;
				}

				if (!localeStrings.TryGetValue(id, out var strings))
				{
					// Apply fallback only if fallbackLocale is not empty
					if (!string.IsNullOrWhiteSpace(fallbackLocale))
					{
						if (TryFillMissingLocaleFromBase(
							baseRow,
							locFields,
							baseStorage.AvailableColumns,
							localeIndex,
							baseName,
							id,
							localeCode,
							targetPath,
							verboseLog,
							out var fallbackUpdates))
						{
							mergedRows++;
							fieldsUpdated += fallbackUpdates;
						}
					}

					// Record test case AFTER filling: No localization available (fallback to base or empty)
					if (testCaseCounter < maxTestCases)
					{
						var testCase = new VerificationTestCase
						{
							RecordId = id,
							TestType = "NoLocalization",
							ExpectedValues = new Dictionary<string, string>()
						};
						
						// Capture values AFTER fallback fill (or empty if no fallback)
						foreach (var field in locFields)
						{
							if (!baseStorage.AvailableColumns.Contains(field))
								continue;
							
							try
							{
								var value = baseRow[field];
								if (value is string[] strArray && strArray.Length > localeIndex)
								{
									testCase.ExpectedValues[field] = strArray[localeIndex] ?? string.Empty;
								}
							}
							catch { /* skip */ }
						}
						
						if (testCase.ExpectedValues.Count > 0)
						{
							testCases.Add(testCase);
							testCaseCounter++;
						}
					}

					continue;
				}

				// Count how many fields will be updated
				int fieldsToUpdate = 0;
				var expectedValues = new Dictionary<string, string>();
				
				for (int i = 0; i < locFields.Count && i < strings.Count; i++)
				{
					if (!baseStorage.AvailableColumns.Contains(locFields[i]))
						continue;
					
					if (!string.IsNullOrWhiteSpace(strings[i]))
					{
						fieldsToUpdate++;
						expectedValues[locFields[i]] = strings[i];
					}
				}

				// Record test case: Multi-column localization
				if (fieldsToUpdate >= 2 && testCases.Count(tc => tc.TestType == "MultiColumn") < maxTestCases)
				{
					testCases.Add(new VerificationTestCase
					{
						RecordId = id,
						TestType = "MultiColumn",
						ExpectedValues = expectedValues
					});
				}

				bool changed = LocalizeRow(
					baseRow,
					strings,
					locFields,
					baseStorage.AvailableColumns,
					localeIndex,
					baseName,
					id,
					localeCode,
					localeSource,
					targetPath,
					verboseLog,
					out var rowFieldUpdates);
				if (changed)
					mergedRows++;
				fieldsUpdated += rowFieldUpdates;
			}

			Logger.Info($"[*] Rows localized: {mergedRows}");
			Logger.Info($"[*] Fields updated: {fieldsUpdated}");
			Logger.Info($"[*] Verification test cases: {testCases.Count}");

			// Ensure output directory exists
			var outputDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			baseStorage.Save(outputPath);
			Logger.Info($"[*] Wrote localized DBC: {outputPath}");

			stats.RowsMerged = mergedRows;
			stats.FieldUpdates = fieldsUpdated;
			stats.TestCases = testCases;

			return 0;
		}

		private static bool LocalizeRow(
			DBCDRow baseRow,
			List<string> localeValues,
			List<string> locFields,
			string[] availableColumns,
			int localeIndex,
			string tableName,
			int id,
			string localeCode,
			string localeSource,
			string targetPath,
			Action<string>? verboseLog,
			out int fieldUpdates)
		{
			bool changed = false;
			fieldUpdates = 0;

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
							string oldValue = arr[localeIndex];
							arr[localeIndex] = text;
							baseRow[field] = arr;
							changed = true;
							fieldUpdates++;

							// Track sample changes if enabled
							if (_trackChanges && _sampleChanges != null)
							{
								if (!_sampleChanges.ContainsKey(tableName))
									_sampleChanges[tableName] = new();

								if (_sampleChanges[tableName].Count < 10)
								{
									_sampleChanges[tableName].Add(new SampleChange
									{
										ID = id,
										Field = field,
										OldValue = oldValue,
										NewValue = text
									});
								}
							}

							verboseLog?.Invoke($"copied {localeCode} from {localeSource} to {targetPath} {tableName}.dbc ID {id} field {field}");

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
						string oldValue = (string)value;
						baseRow[field] = text;
						changed = true;
						fieldUpdates++;

						// Track sample changes if enabled
						if (_trackChanges && _sampleChanges != null)
						{
							if (!_sampleChanges.ContainsKey(tableName))
								_sampleChanges[tableName] = new();

							if (_sampleChanges[tableName].Count < 10)
							{
								_sampleChanges[tableName].Add(new SampleChange
								{
									ID = id,
									Field = field,
									OldValue = oldValue,
									NewValue = text
								});
							}
						}

						verboseLog?.Invoke($"copied {localeCode} from {localeSource} to {targetPath} {tableName}.dbc ID {id} field {field}");
					}
				}
				catch
				{
					// Ignore field assignment errors
				}
			}

			return changed;
		}

		private static bool TryFillMissingLocaleFromBase(
			DBCDRow baseRow,
			List<string> locFields,
			string[] availableColumns,
			int localeIndex,
			string tableName,
			int id,
			string localeCode,
			string targetPath,
			Action<string>? verboseLog,
			out int fieldUpdates)
		{
			fieldUpdates = 0;
			bool changed = false;

			if (localeIndex <= 0)
				return false;

			foreach (var field in locFields)
			{
				try
				{
					var value = baseRow[field];
					if (value is string[] arr)
					{
						if (localeIndex < 0 || localeIndex >= arr.Length)
							continue;

						if (!string.IsNullOrWhiteSpace(arr[localeIndex]))
							continue;

						var fallback = arr.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
						if (string.IsNullOrWhiteSpace(fallback))
							continue;

						arr[localeIndex] = fallback;
						baseRow[field] = arr;
						changed = true;
						fieldUpdates++;


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
				catch
				{
					// ignore
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

		public static VerificationResult VerifyLocalizedDbc(
			string outputPath,
			string defsPath,
			string build,
			string localeCode,
			List<VerificationTestCase> testCases)
		{
			var result = new VerificationResult
			{
				TableName = Path.GetFileNameWithoutExtension(outputPath),
				TestCasesTotal = testCases.Count
			};

			if (testCases.Count == 0)
			{
				return result;
			}

			try
			{
				var dbdProvider = new FilesystemDBDProvider(defsPath);
				var dbcProvider = new FilesystemDBCProvider(Path.GetDirectoryName(outputPath) ?? ".");
				var dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);
				
				var tableName = Path.GetFileNameWithoutExtension(outputPath);
				var storage = dbcd.Load(tableName, build, DBCD.Locale.None);

				var localeIndex = Helpers.GetLocaleIndex(localeCode, build);

				foreach (var testCase in testCases)
				{
					bool passed = true;
					var failureReasons = new List<string>();

					// Find the record by ID
					DBCDRow? record = null;
					foreach (var row in storage.Values)
					{
						try
						{
							var rowId = Convert.ToInt32(row["ID"]);
							if (rowId == testCase.RecordId)
							{
								record = row;
								break;
							}
						}
						catch { /* continue */ }
					}

					if (record == null)
					{
						passed = false;
						failureReasons.Add($"Record ID {testCase.RecordId} not found in output");
					}
					else
					{
						// Verify each expected field value
						foreach (var kvp in testCase.ExpectedValues)
						{
							var fieldName = kvp.Key;
							var expectedValue = kvp.Value;

							try
							{
								var actualValue = record[fieldName];
								if (actualValue is string[] strArray && strArray.Length > localeIndex)
								{
									var actualText = strArray[localeIndex] ?? string.Empty;
									
									if (!string.Equals(actualText, expectedValue, StringComparison.Ordinal))
									{
										passed = false;
										failureReasons.Add($"ID {testCase.RecordId}, Field '{fieldName}': Expected '{expectedValue}', got '{actualText}'");
									}
								}
								else
								{
									passed = false;
									failureReasons.Add($"ID {testCase.RecordId}, Field '{fieldName}': Not a locstring or wrong format");
								}
							}
							catch (Exception ex)
							{
								passed = false;
								failureReasons.Add($"ID {testCase.RecordId}, Field '{fieldName}': {ex.Message}");
							}
						}
					}

					if (passed)
					{
						result.TestCasesPassed++;
					}
					else
					{
						result.TestCasesFailed++;
						result.FailureDetails.AddRange(failureReasons);
					}
				}
			}
			catch (Exception ex)
			{
				result.TestCasesFailed = testCases.Count;
				result.FailureDetails.Add($"Verification error: {ex.Message}");
			}

			return result;
		}
	}
}
