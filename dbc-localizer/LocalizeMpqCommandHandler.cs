using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DbcLocalizer
{
	/// <summary>
	/// Handles the "localize-mpq" command for localizing DBCs from MPQ archives
	/// </summary>
	internal static class LocalizeMpqCommandHandler
	{
		/// <summary>
		/// Normalize locale code from "patch-deDE-3" to "deDE"
		/// </summary>
		private static string NormalizeLocaleCode(string input)
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

		public static int Execute(string[] args)
		{
			Logger.SetLogLevel(args);

			var mpqArgs = LocalizeMpqArgs.Parse(args);
			
			// Set defaults if no arguments provided
			if (args.Length == 0)
			{
				// Use application directory, not current working directory
				var baseDir = AppContext.BaseDirectory;
				mpqArgs.PatchMpq = Path.Combine(baseDir, "input", "patch");
				mpqArgs.OutputMpq = Path.Combine(baseDir, "output");
				mpqArgs.ClearOutput = true; // Default to clearing output when running without args
			}
			
			if (string.IsNullOrWhiteSpace(mpqArgs.DefsPath))
				mpqArgs.DefsPath = DefsManager.GetDefaultDefsPath();

			// Auto-detect locale MPQs if not provided
			if (mpqArgs.LocaleMpqs.Count == 0)
			{
				var localeDir = Path.Combine(AppContext.BaseDirectory, "input", "locale");
				if (Directory.Exists(localeDir))
				{
					// Look for both patch-*locale*.mpq and locale-*.mpq files
					var localePatterns = new[] { "patch-*de*-*.MPQ", "patch-*de*-*.mpq", "locale-*.MPQ", "locale-*.mpq" };
					var localeMpqFiles = new List<string>();
					
					foreach (var pattern in localePatterns)
					{
						localeMpqFiles.AddRange(Directory.GetFiles(localeDir, pattern, SearchOption.TopDirectoryOnly));
					}

					// Remove duplicates and sort
					localeMpqFiles = localeMpqFiles.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(f => f).ToList();

					foreach (var mpqFile in localeMpqFiles)
					{
						mpqArgs.LocaleMpqs.Add(mpqFile);
						var fileName = Path.GetFileNameWithoutExtension(mpqFile).ToLowerInvariant();
						mpqArgs.Languages.Add(fileName);
					}
				}
			}

			return ExecuteSinglePatch(mpqArgs);
		}

		private static int ExecuteSinglePatch(LocalizeMpqArgs mpqArgs)
		{
			if (!mpqArgs.IsValid)
				return Fail("Missing required arguments. Use --patch, --defs, --output.");

			// Clear output directory if requested
			if (mpqArgs.ClearOutput)
			{
				var outputDir = mpqArgs.OutputMpq;
				if(!outputDir.IsDirectory())
					outputDir = Path.GetDirectoryName(outputDir) ?? outputDir;

				if (Directory.Exists(outputDir))
				{
					Logger.Info($"[*] Clearing output directory: {outputDir}");
					try
					{
						foreach (var file in Directory.GetFiles(outputDir, "*.*", SearchOption.TopDirectoryOnly))
						{
							File.Delete(file);
						}
					}
					catch (Exception ex)
					{
						Logger.Verbose($"[!] Warning: Could not clear all files in output directory: {ex.Message}");
					}
				}
			}

			// If multi-patch directory mode, resolve multiple patches
			if (mpqArgs.PatchMpq.IsDirectory())
			{
				var patchFiles = Directory.GetFiles(mpqArgs.PatchMpq, "patch-*.mpq", SearchOption.TopDirectoryOnly);
				if (patchFiles.Length == 0)
					return Fail($"No patch MPQ files found in: {mpqArgs.PatchMpq}");

				Logger.Info($"[*] Found {patchFiles.Length} patch file(s)");

				int totalReturnCode = 0;
				foreach (var patchFile in patchFiles)
				{
					Logger.Info($"");
					Logger.Info($"[*] Processing patch: {Path.GetFileName(patchFile)}");
					
					var singlePatchArgs = new LocalizeMpqArgs
					{
						PatchMpq = patchFile,
						LocaleMpqs = new List<string>(mpqArgs.LocaleMpqs),
						Languages = new List<string>(mpqArgs.Languages),
						DefsPath = mpqArgs.DefsPath,
						OutputMpq = mpqArgs.OutputMpq,
						WoWBuild = mpqArgs.WoWBuild,
						MpqCliPath = mpqArgs.MpqCliPath,
						KeepTemp = mpqArgs.KeepTemp
					};

					int returnCode = ExecuteSinglePatchFile(singlePatchArgs);
					if (returnCode != 0)
						totalReturnCode = returnCode;
				}

				return totalReturnCode;
			}

			if (!File.Exists(mpqArgs.PatchMpq))
				return Fail($"Patch MPQ not found: {mpqArgs.PatchMpq}");

			return ExecuteSinglePatchFile(mpqArgs);
		}

		private static int ExecuteSinglePatchFile(LocalizeMpqArgs mpqArgs)
		{
			if (!File.Exists(mpqArgs.PatchMpq))
				return Fail($"Patch MPQ not found: {mpqArgs.PatchMpq}");

			if (mpqArgs.LocaleMpqs.Count == 0)
				return Fail("Missing locale MPQ(s). Use --locale-mpq or --locale-mpqs.");

			foreach (var mpq in mpqArgs.LocaleMpqs)
			{
				if (!File.Exists(mpq))
					return Fail($"Locale MPQ not found: {mpq}");
			}

			// Ensure Languages aligns with LocaleMpqs
			if (mpqArgs.Languages.Count != mpqArgs.LocaleMpqs.Count)
			{
				Logger.Verbose($"[!] Warning: Languages count ({mpqArgs.Languages.Count}) does not match LocaleMpqs count ({mpqArgs.LocaleMpqs.Count}). Rebuilding Languages from LocaleMpqs.");
				mpqArgs.Languages = mpqArgs.LocaleMpqs
					.Select(mpq => Path.GetFileNameWithoutExtension(mpq).ToLowerInvariant())
					.ToList();
			}

			var defsPath = mpqArgs.DefsPath;
			if (!DefsManager.EnsureDefinitions(ref defsPath, mpqArgs.WoWBuild))
				return Fail($"Definitions path not found: {mpqArgs.DefsPath}");
			mpqArgs.DefsPath = defsPath;

			if (!File.Exists(mpqArgs.MpqCliPath))
				return Fail($"mpqcli not found: {mpqArgs.MpqCliPath}");

			// Create report at the beginning
			var report = ReportManager.CreateReport(mpqArgs);

			// Auto-detect all localizable DBCs
			Logger.Info("[*] Auto-detecting all localizable DBCs...");
			var candidates = DbcScanner.GetLocalizableDbcCandidates(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, mpqArgs.LocaleMpqs, mpqArgs.DefsPath, mpqArgs.WoWBuild, report.Warnings);
			foreach (var candidate in candidates)
			{
				ReportManager.AddSelectedDbc(report, candidate.Path);
			}
			Logger.Info($"[*] Found {report.SelectedDbcs.Count} localizable table(s)");

			// Resolve output path (directory vs file)
			var outputPath = mpqArgs.OutputMpq;
			if (outputPath.EndsWith("/") || outputPath.EndsWith("\\"))
			{
				Directory.CreateDirectory(outputPath);
				outputPath = Path.Combine(outputPath, Path.GetFileName(mpqArgs.PatchMpq));
			}
			else if (Directory.Exists(outputPath))
			{
				outputPath = Path.Combine(outputPath, Path.GetFileName(mpqArgs.PatchMpq));
			}
			mpqArgs.OutputMpq = outputPath;
			report.OutputMpq = outputPath;

			// Copy patch to output even if no localizable DBCs found
			Logger.Info($"[*] Copying patch MPQ to output: {mpqArgs.OutputMpq}");
			File.Copy(mpqArgs.PatchMpq, mpqArgs.OutputMpq, overwrite: true);

			if (report.SelectedDbcs.Count == 0)
			{
				Logger.Info("[*] No localizable DBCs found, patch copied unchanged.");
								
				// Write report even when nothing was localized
				ReportManager.WriteReport(report, ReportManager.GetDefaultReportPath(mpqArgs.PatchMpq, mpqArgs.OutputMpq));
				
				return 0;
			}

			// Prepare temp directories
			var tempRoot = Path.Combine(Path.GetTempPath(), "dbc-localizer", Guid.NewGuid().ToString("N"));
			var patchExtractDir = Path.Combine(tempRoot, "patch");
			var localeExtractDir = Path.Combine(tempRoot, "locale");
			var mergedDir = Path.Combine(tempRoot, "merged");
			Directory.CreateDirectory(patchExtractDir);
			Directory.CreateDirectory(localeExtractDir);
			Directory.CreateDirectory(mergedDir);

			try
			{
				// Validate selected DBCs
				var validatedDbcs = DbcScanner.ValidateSelectedDbcs(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, mpqArgs.LocaleMpqs, report.SelectedDbcs, report.Warnings);

				foreach (var warn in report.Warnings)
				{
					Logger.Info($"[!] WARNING: {warn}");
				}

				// Update selected DBCs with validated list
				report.SelectedDbcs.Clear();
				foreach (var dbc in validatedDbcs)
				{
					report.SelectedDbcs.Add(dbc);
				}

				if (report.SelectedDbcs.Count == 0)
				{
					Logger.Info("[*] No valid DBCs to localize.");
					ReportManager.WriteReport(report, ReportManager.GetDefaultReportPath(mpqArgs.PatchMpq, mpqArgs.OutputMpq));
					return 0;
				}

				// Process localization with report
				return PerformLocalization(mpqArgs, report, patchExtractDir, localeExtractDir, mergedDir);
			}
			finally
			{
				if (!mpqArgs.KeepTemp)
				{
					try
					{
						if (Directory.Exists(tempRoot))
							Directory.Delete(tempRoot, true);

						var tempParent = Path.Combine(Path.GetTempPath(), "dbc-localizer");
						if (Directory.Exists(tempParent) && !Directory.EnumerateFileSystemEntries(tempParent).Any())
							Directory.Delete(tempParent, true);
					}
					catch
					{
						// ignore cleanup errors
					}
				}
			}
		}

		private static int PerformLocalization(LocalizeMpqArgs mpqArgs, LocalizeReport report,
			string patchExtractDir, string localeExtractDir, string mergedDir)
		{
			var globalSampleChanges = new Dictionary<string, List<SampleChange>>();
			
			// Initialize per-locale results
			ReportManager.InitializeLocales(report, mpqArgs.Languages, mpqArgs.LocaleMpqs, NormalizeLocaleCode);

			// Process each DBC table (per-DBC with all locales)
			foreach (var dbcRelPath in report.SelectedDbcs.ToList())
			{
				try
				{
					Logger.Info($"");
					Logger.Info($"[*] Processing table: {Path.GetFileName(dbcRelPath)}");

					// Extract patch DBC
					var patchExtracted = MpqHelper.ExtractFile(mpqArgs.MpqCliPath, mpqArgs.PatchMpq, dbcRelPath, patchExtractDir);

					// Extract all locale DBCs
					var localeDbcPaths = new Dictionary<string, string>();
					for (int locIdx = 0; locIdx < mpqArgs.LocaleMpqs.Count; locIdx++)
					{
						var localeMpqPath = mpqArgs.LocaleMpqs[locIdx];
						var lang = mpqArgs.Languages[locIdx];

						try
						{
							var localeExtracted = MpqHelper.ExtractFile(mpqArgs.MpqCliPath, localeMpqPath, dbcRelPath, localeExtractDir);
							localeDbcPaths[lang] = localeExtracted;
							Logger.Info($"[*] Extracted locale: {lang}");
						}
						catch (Exception ex)
						{
							Logger.Verbose($"[!] Locale {lang} does not contain {dbcRelPath}: {ex.Message}");
						}
					}

					if (localeDbcPaths.Count == 0)
					{
						Logger.Info($"[!] No locale data found for {dbcRelPath}, skipping");
						continue;
					}

					// Localize using simplified engine (all locales at once)
					var mergedPath = Path.Combine(mergedDir, Path.GetFileName(dbcRelPath));
					var localeStats = SimplifiedLocalizeEngine.LocalizeDbcSimplified(
						patchExtracted,
						localeDbcPaths,
						mpqArgs.DefsPath,
						mergedPath,
						mpqArgs.WoWBuild,
						mpqArgs.FallbackLocale,
						true,
						globalSampleChanges);

					// Update report statistics per locale
					foreach (var kvp in localeStats)
					{
						var localeCode = kvp.Key;
						var stats = kvp.Value;
						var normalizedCode = NormalizeLocaleCode(localeCode);
						
						ReportManager.UpdateLocaleStats(report, normalizedCode, stats.RowsMerged, stats.FieldUpdates, true);
					}

					ReportManager.IncrementTablesMerged(report);

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

					// Cleanup temp files
					if (File.Exists(patchExtracted)) File.Delete(patchExtracted);
					foreach (var localePath in localeDbcPaths.Values)
					{
						if (File.Exists(localePath)) File.Delete(localePath);
					}
					if (File.Exists(mergedPath)) File.Delete(mergedPath);
				}
				catch (Exception ex)
				{
					Logger.Error($"Failed to localize {dbcRelPath}: {ex.Message}");
					ReportManager.AddWarning(report, $"Failed to localize {dbcRelPath}: {ex.Message}");
				}
			}

			// Calculate totals and log summaries
			ReportManager.CalculateTotals(report);
			
			foreach (var result in report.PerLocale)
			{
				Logger.Info($"[*] Locale {result.Language} summary: {result.TablesMerged} table(s), {result.RowsMerged} row(s), {result.FieldsUpdated} field(s)");
			}

			// Assign sample changes to the locale with most updates
			ReportManager.AssignSampleChanges(report, globalSampleChanges);

			Logger.Info($"[*] Output MPQ: {mpqArgs.OutputMpq}");

			// Write report
			ReportManager.WriteReport(report, ReportManager.GetDefaultReportPath(mpqArgs.PatchMpq, mpqArgs.OutputMpq));

			return 0;
		}



		private static int Fail(string message, int exitCode = 1)
		{
			Logger.Error(message);
			return exitCode;
		}
	}
}
