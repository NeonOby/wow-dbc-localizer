using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DbcMerger
{
	/// <summary>
	/// Handles the "merge-mpq" command for merging DBCs from MPQ archives
	/// </summary>
	internal static class MergeMpqCommandHandler
	{
		public static int Execute(string[] args)
		{
			Logger.SetLogLevel(args);

			var mpqArgs = MergeMpqArgs.Parse(args);

			// Check for multi-patch-dir mode
			if (mpqArgs.IsMultiPatchDir)
			{
				var multiPatchDir = Helpers.GetArg(args, "--multi-patch-dir");
				if (string.IsNullOrWhiteSpace(multiPatchDir))
					return Fail("Missing --multi-patch-dir argument");
				return ExecuteMultiPatch(multiPatchDir, args);
			}

			return ExecuteSinglePatch(mpqArgs);
		}

		private static int ExecuteMultiPatch(string multiPatchDir, string[] args)
		{
			if (string.IsNullOrEmpty(multiPatchDir))
				return Fail("Missing --multi-patch-dir argument");

			if (!Directory.Exists(multiPatchDir))
				return Fail($"Patch directory not found: {multiPatchDir}");

			// Get all .mpq files in the directory
			var patchFiles = Directory.GetFiles(multiPatchDir, "*.mpq", SearchOption.TopDirectoryOnly)
				.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
				.ToList();

			if (patchFiles.Count == 0)
			{
				Logger.Info($"No .mpq files found in: {multiPatchDir}");
				return 0;
			}

			Logger.Info($"Found {patchFiles.Count} patch file(s) to process");

			var allExitCode = 0;

			foreach (var patchFile in patchFiles)
			{
				Logger.Info($"\n[*] Processing: {Path.GetFileName(patchFile)}");
				
				// Build arguments for this specific patch
				var patchArgs = args.ToList();
				
				// Remove the multi-patch-dir argument
				var multiIdx = patchArgs.IndexOf("--multi-patch-dir");
				if (multiIdx >= 0)
				{
					patchArgs.RemoveAt(multiIdx);
					if (multiIdx < patchArgs.Count)
						patchArgs.RemoveAt(multiIdx);
				}
				
				// Add patch argument
				patchArgs.Add("--patch");
				patchArgs.Add(patchFile);
				
				// Adjust output path to use same filename
				var outputIdx = patchArgs.IndexOf("--output");
				if (outputIdx >= 0 && outputIdx + 1 < patchArgs.Count)
				{
					var outputDir = patchArgs[outputIdx + 1];
					if ((outputDir.EndsWith("/") || outputDir.EndsWith("\\")) && Directory.Exists(outputDir))
					{
						patchArgs[outputIdx + 1] = Path.Combine(outputDir, Path.GetFileName(patchFile));
					}
				}

				// Adjust report path to use patch filename
				var reportIdx = patchArgs.IndexOf("--report");
				if (reportIdx >= 0 && reportIdx + 1 < patchArgs.Count)
				{
					var reportDir = patchArgs[reportIdx + 1];
					if ((reportDir.EndsWith("/") || reportDir.EndsWith("\\")) && Directory.Exists(reportDir))
					{
						var patchBaseName = Path.GetFileNameWithoutExtension(patchFile);
						patchArgs[reportIdx + 1] = Path.Combine(reportDir, $"{patchBaseName}-report.json");
					}
				}

				// Run merge for this patch
				var exitCode = ExecuteSinglePatch(MergeMpqArgs.Parse(patchArgs.ToArray()));
				if (exitCode != 0)
					allExitCode = exitCode;
			}

			Logger.Info($"\n[*] Batch processing complete");
			return allExitCode;
		}

		private static int ExecuteSinglePatch(MergeMpqArgs mpqArgs)
		{
			if (!mpqArgs.IsValid)
				return Fail("Missing required arguments. Use --patch, --defs, --output.");

			if (!File.Exists(mpqArgs.PatchMpq))
				return Fail($"Patch MPQ not found: {mpqArgs.PatchMpq}");

			if (mpqArgs.LocaleMpqs.Count == 0)
				return Fail("Missing locale MPQ(s). Use --locale-mpq or --locale-mpqs.");

			foreach (var mpq in mpqArgs.LocaleMpqs)
			{
				if (!File.Exists(mpq))
					return Fail($"Locale MPQ not found: {mpq}");
			}

			if (!Directory.Exists(mpqArgs.DefsPath))
				return Fail($"Definitions path not found: {mpqArgs.DefsPath}");

			if (!File.Exists(mpqArgs.MpqCliPath))
				return Fail($"mpqcli not found: {mpqArgs.MpqCliPath}");

			if (mpqArgs.Languages.Count != mpqArgs.LocaleMpqs.Count)
			{
				return Fail($"Number of languages ({mpqArgs.Languages.Count}) must match number of locale MPQs ({mpqArgs.LocaleMpqs.Count}).");
			}

			// Determine which DBCs to merge
			List<string> selectedDbcs;
			if (mpqArgs.AutoAll)
			{
				Logger.Info("[*] Auto-detecting all localizable DBCs...");
				var warnings = new List<string>();
				var candidates = DbcScanner.GetLocalizableDbcCandidates(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, mpqArgs.LocaleMpqs, mpqArgs.DefsPath, mpqArgs.Build, warnings);
				selectedDbcs = candidates.Select(c => c.Path).ToList();
				Logger.Info($"[*] Found {selectedDbcs.Count} localizable table(s)");
			}
			else if (mpqArgs.DbcList.Count > 0)
			{
				selectedDbcs = mpqArgs.DbcList;
				Logger.Info($"[*] Using explicit list of {selectedDbcs.Count} DBC(s)");
			}
			else if (!string.IsNullOrWhiteSpace(mpqArgs.DbcRelPath))
			{
				selectedDbcs = new List<string> { mpqArgs.DbcRelPath };
				Logger.Info($"[*] Using single DBC: {mpqArgs.DbcRelPath}");
			}
			else if (mpqArgs.Interactive)
			{
				selectedDbcs = SelectDbcsForMerge(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, mpqArgs.LocaleMpqs, mpqArgs.DefsPath, mpqArgs.Build);
				if (selectedDbcs.Count == 0)
				{
					Logger.Info("[*] No DBCs selected, exiting.");
					WriteMergeReport(new MergeReport
					{
						TimestampUtc = DateTime.UtcNow.ToString("O"),
						Build = mpqArgs.Build,
						PatchMpq = mpqArgs.PatchMpq,
						OutputMpq = mpqArgs.OutputMpq,
						Languages = mpqArgs.Languages,
						LocaleMpqs = mpqArgs.LocaleMpqs,
						SelectedDbcs = new List<string>(),
						PerLocale = new List<LocaleMergeResult>(),
						TotalTablesMerged = 0,
						TotalRowsMerged = 0,
						TotalFieldsUpdated = 0,
						Warnings = new List<string>()
					}, mpqArgs.ReportPath);
					return 0;
				}
			}
			else
			{
				return Fail("No DBC specified. Use --dbc, --dbc-list, --auto, or --select.");
			}

			// Prepare temp directories
			var tempRoot = Path.Combine(Path.GetTempPath(), "dbc-merger", Guid.NewGuid().ToString("N"));
			var patchExtractDir = Path.Combine(tempRoot, "patch");
			var localeExtractDir = Path.Combine(tempRoot, "locale");
			var mergedDir = Path.Combine(tempRoot, "merged");
			Directory.CreateDirectory(patchExtractDir);
			Directory.CreateDirectory(localeExtractDir);
			Directory.CreateDirectory(mergedDir);

			try
			{
				// Copy patch to output
				Logger.Info($"[*] Copying patch MPQ to output: {mpqArgs.OutputMpq}");
				File.Copy(mpqArgs.PatchMpq, mpqArgs.OutputMpq, overwrite: true);

				// Validate selected DBCs
				var warnings2 = new List<string>();
				selectedDbcs = DbcScanner.ValidateSelectedDbcs(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, mpqArgs.LocaleMpqs, selectedDbcs, warnings2);

				foreach (var warn in warnings2)
				{
					Logger.Info($"[!] WARNING: {warn}");
				}

				if (selectedDbcs.Count == 0)
				{
					Logger.Info("[*] No valid DBCs to merge.");
					WriteMergeReport(new MergeReport
					{
						TimestampUtc = DateTime.UtcNow.ToString("O"),
						Build = mpqArgs.Build,
						PatchMpq = mpqArgs.PatchMpq,
						OutputMpq = mpqArgs.OutputMpq,
						Languages = mpqArgs.Languages,
						LocaleMpqs = mpqArgs.LocaleMpqs,
						SelectedDbcs = selectedDbcs,
						PerLocale = new List<LocaleMergeResult>(),
						TotalTablesMerged = 0,
						TotalRowsMerged = 0,
						TotalFieldsUpdated = 0,
						Warnings = warnings2
					}, mpqArgs.ReportPath);
					return 0;
				}

				// Process merging
				return PerformMerge(mpqArgs, selectedDbcs, patchExtractDir, localeExtractDir, mergedDir, warnings2);
			}
			finally
			{
				if (!mpqArgs.KeepTemp && Directory.Exists(tempRoot))
				{
					try { Directory.Delete(tempRoot, recursive: true); }
					catch { /* ignore cleanup errors */ }
				}
			}
		}

		private static int PerformMerge(MergeMpqArgs mpqArgs, List<string> selectedDbcs, 
			string patchExtractDir, string localeExtractDir, string mergedDir, List<string> warnings)
		{
			int totalRowsMerged = 0;
			int totalFieldsUpdated = 0;
			int totalTablesMerged = 0;
			var perLocaleResults = new List<LocaleMergeResult>();

			for (int locIdx = 0; locIdx < mpqArgs.LocaleMpqs.Count; locIdx++)
			{
				var localeMpqPath = mpqArgs.LocaleMpqs[locIdx];
				var lang = mpqArgs.Languages[locIdx];

				Logger.Info($"");
				Logger.Info($"[*] Processing locale: {lang} ({localeMpqPath})");

				int localeRowsMerged = 0;
				int localeFieldsUpdated = 0;
				int localeTablesMerged = 0;

				foreach (var dbcRelPath in selectedDbcs)
				{
					try
					{
						// Extract from patch
						var patchExtracted = MpqHelper.ExtractFile(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, dbcRelPath, patchExtractDir);

						// Extract from locale
						var localeExtracted = MpqHelper.ExtractFile(mpqArgs.MpqCliPath, localeMpqPath, dbcRelPath, localeExtractDir);

						// Merge
						var mergedPath = Path.Combine(mergedDir, Path.GetFileName(dbcRelPath));
						var exitCode = MergeEngine.MergeDbc(
							patchExtracted,
							localeExtracted,
							mpqArgs.DefsPath,
							mergedPath,
							mpqArgs.Build,
							lang,
							localeMpqPath,
							mpqArgs.OutputMpq,
							Logger.Verbose,
							out var stats);

						if (exitCode == 0)
						{
							localeRowsMerged += stats.RowsMerged;
							localeFieldsUpdated += stats.FieldUpdates;
							localeTablesMerged++;

							// Remove old DBC from output MPQ
							try
							{
								MpqHelper.RemoveFile(mpqArgs.MpqCliPath, mpqArgs.OutputMpq, dbcRelPath);
							}
							catch
							{
								// Ignore if not present
							}

							// Add merged DBC to output MPQ
							MpqHelper.AddFile(mpqArgs.MpqCliPath, mpqArgs.OutputMpq, mergedPath, dbcRelPath);
						}

						// Cleanup temp files
						if (File.Exists(patchExtracted)) File.Delete(patchExtracted);
						if (File.Exists(localeExtracted)) File.Delete(localeExtracted);
						if (File.Exists(mergedPath)) File.Delete(mergedPath);
					}
					catch (Exception ex)
					{
						Logger.Error($"Failed to merge {dbcRelPath}: {ex.Message}");
						warnings.Add($"Failed to merge {dbcRelPath} for locale {lang}: {ex.Message}");
					}
				}

				totalRowsMerged += localeRowsMerged;
				totalFieldsUpdated += localeFieldsUpdated;
				totalTablesMerged += localeTablesMerged;

				perLocaleResults.Add(new LocaleMergeResult
				{
					LocaleMpq = localeMpqPath,
					Language = lang,
					TablesMerged = localeTablesMerged,
					RowsMerged = localeRowsMerged,
					FieldsUpdated = localeFieldsUpdated
				});

				Logger.Info($"[*] Locale {lang} summary: {localeTablesMerged} table(s), {localeRowsMerged} row(s), {localeFieldsUpdated} field(s)");
			}

			Logger.Info($"[*] Output MPQ: {mpqArgs.OutputMpq}");

			// Write report
			var report = new MergeReport
			{
				TimestampUtc = DateTime.UtcNow.ToString("O"),
				Build = mpqArgs.Build,
				PatchMpq = mpqArgs.PatchMpq,
				OutputMpq = mpqArgs.OutputMpq,
				Languages = mpqArgs.Languages,
				LocaleMpqs = mpqArgs.LocaleMpqs,
				SelectedDbcs = selectedDbcs,
				PerLocale = perLocaleResults,
				TotalTablesMerged = totalTablesMerged,
				TotalRowsMerged = totalRowsMerged,
				TotalFieldsUpdated = totalFieldsUpdated,
				Warnings = warnings
			};
			WriteMergeReport(report, mpqArgs.ReportPath);

			return 0;
		}

		private static void WriteMergeReport(MergeReport report, string reportPath)
		{
			if (!string.IsNullOrWhiteSpace(reportPath))
			{
				var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(reportPath, json);
				Logger.Info($"[*] Report written: {reportPath}");
			}
		}

		private static List<string> SelectDbcsForMerge(
			string mpqcliPath,
			string patchMpq,
			List<string> localeMpqs,
			string defsPath,
			string build)
		{
			var warnings = new List<string>();
			var candidates = DbcScanner.GetLocalizableDbcCandidates(mpqcliPath, patchMpq, localeMpqs, defsPath, build, warnings);

			if (candidates.Count == 0)
			{
				Logger.Info("[*] No localizable DBCs found.");
				return new List<string>();
			}

			Logger.Info($"[*] Found {candidates.Count} localizable DBC table(s):");
			for (int i = 0; i < candidates.Count; i++)
			{
				var c = candidates[i];
				Logger.Info($"  {i + 1}. {c.Path} ({c.LocCount} locstring field(s))");
			}

			Logger.Info("");
			Logger.Info("Select DBCs to merge (comma-separated numbers, or 'A' for all):");
			var input = Console.ReadLine()?.Trim();

			if (string.IsNullOrWhiteSpace(input))
				return new List<string>();

			if (input.Equals("A", StringComparison.OrdinalIgnoreCase))
				return candidates.Select(c => c.Path).ToList();

			var indices = input.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => int.TryParse(s, out _))
				.Select(s => int.Parse(s) - 1)
				.Where(idx => idx >= 0 && idx < candidates.Count)
				.Distinct()
				.ToList();

			return indices.Select(idx => candidates[idx].Path).ToList();
		}

		private static int Fail(string message, int exitCode = 1)
		{
			Logger.Error(message);
			return exitCode;
		}
	}
}
