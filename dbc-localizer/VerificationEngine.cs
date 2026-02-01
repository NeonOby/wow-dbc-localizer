using System;
using System.Collections.Generic;
using System.IO;
using DBCD;
using DBCD.Providers;

namespace DbcLocalizer
{
	internal static class VerificationEngine
	{
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
