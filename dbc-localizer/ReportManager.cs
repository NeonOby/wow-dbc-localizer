using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DbcLocalizer
{
	/// <summary>
	/// Manages localization report generation and persistence
	/// </summary>
	internal static class ReportManager
	{
		/// <summary>
		/// Generate default report path from patch file and output directory
		/// </summary>
		public static string GetDefaultReportPath(string patchMpqPath, string outputMpqPath)
		{
			var patchName = Path.GetFileNameWithoutExtension(patchMpqPath);
			var outputDir = Path.GetDirectoryName(outputMpqPath) ?? ".";
			return Path.Combine(outputDir, $"{patchName}-report.json");
		}

		/// <summary>
		/// Write localization report to JSON file
		/// </summary>
		public static void WriteReport(LocalizeReport report, string reportPath)
		{
			if (!string.IsNullOrWhiteSpace(reportPath))
			{
				var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(reportPath, json);
				Logger.Info($"[*] Report written: {reportPath}");
			}
		}

		/// <summary>
		/// Create a new report for localization
		/// </summary>
		public static LocalizeReport CreateReport(LocalizeMpqArgs args)
		{
			return new LocalizeReport
			{
				TimestampUtc = DateTime.UtcNow.ToString("O"),
				Build = args.WoWBuild,
				PatchMpq = args.PatchMpq,
				OutputMpq = args.OutputMpq,
				Languages = new List<string>(args.Languages),
				LocaleMpqs = new List<string>(args.LocaleMpqs)
			};
		}

		/// <summary>
		/// Add a DBC to the selected DBCs list
		/// </summary>
		public static void AddSelectedDbc(LocalizeReport report, string dbcPath)
		{
			report.SelectedDbcs.Add(dbcPath);
		}

		/// <summary>
		/// Add a warning to the report
		/// </summary>
		public static void AddWarning(LocalizeReport report, string warning)
		{
			report.Warnings.Add(warning);
		}

		/// <summary>
		/// Initialize locale results for all locales
		/// </summary>
		public static void InitializeLocales(LocalizeReport report, List<string> languages, List<string> localeMpqs, Func<string, string> normalizeLocaleCode)
		{
			// Use report's languages/localeMpqs if they're already set, otherwise use provided lists
			var langs = report.Languages.Count > 0 ? report.Languages : languages;
			var mpqs = report.LocaleMpqs.Count > 0 ? report.LocaleMpqs : localeMpqs;
			
			var count = Math.Min(langs.Count, mpqs.Count);
			if (langs.Count != mpqs.Count)
			{
				Logger.Verbose($"[!] Warning: Languages count ({langs.Count}) does not match LocaleMpqs count ({mpqs.Count}). Using first {count} entries.");
			}
			
			for (int i = 0; i < count; i++)
			{
				var lang = langs[i];
				var normalizedLang = normalizeLocaleCode(lang);
				report.PerLocale.Add(new LocaleLocalizeResult
				{
					LocaleMpq = mpqs[i],
					Language = normalizedLang
				});
			}
		}

		/// <summary>
		/// Get locale result by language code
		/// </summary>
		public static LocaleLocalizeResult? GetLocaleResult(LocalizeReport report, string language)
		{
			return report.PerLocale.FirstOrDefault(l => l.Language == language);
		}

		/// <summary>
		/// Update statistics for a specific locale
		/// </summary>
		public static void UpdateLocaleStats(LocalizeReport report, string language, int rowsMerged, int fieldsUpdated, bool tableMerged)
		{
			var locale = GetLocaleResult(report, language);
			if (locale != null)
			{
				if (tableMerged && rowsMerged > 0)
					locale.TablesMerged++;
				locale.RowsMerged += rowsMerged;
				locale.FieldsUpdated += fieldsUpdated;
			}
		}

		/// <summary>
		/// Increment total tables merged counter
		/// </summary>
		public static void IncrementTablesMerged(LocalizeReport report)
		{
			report.TotalTablesMerged++;
		}

		/// <summary>
		/// Calculate total statistics from per-locale results
		/// </summary>
		public static void CalculateTotals(LocalizeReport report)
		{
			report.TotalRowsMerged = report.PerLocale.Sum(l => l.RowsMerged);
			report.TotalFieldsUpdated = report.PerLocale.Sum(l => l.FieldsUpdated);
		}

		/// <summary>
		/// Assign sample changes to the locale with most updates
		/// </summary>
		public static void AssignSampleChanges(LocalizeReport report, Dictionary<string, List<SampleChange>> globalSampleChanges)
		{
			var topLocale = report.PerLocale.OrderByDescending(r => r.FieldsUpdated).FirstOrDefault();
			if (topLocale != null)
			{
				topLocale.SampleChanges = globalSampleChanges;
			}
		}
	}

	/// <summary>
	/// Localization report structure
	/// </summary>
	internal sealed class LocalizeReport
	{
		public string TimestampUtc { get; set; } = string.Empty;
		public string Build { get; set; } = string.Empty;
		public string PatchMpq { get; set; } = string.Empty;
		public string OutputMpq { get; set; } = string.Empty;
		public List<string> LocaleMpqs { get; set; } = new();
		public List<string> Languages { get; set; } = new();
		public List<string> SelectedDbcs { get; set; } = new();
		public List<string> Warnings { get; set; } = new();
		public int TotalTablesMerged { get; set; }
		public int TotalRowsMerged { get; set; }
		public int TotalFieldsUpdated { get; set; }
		public List<LocaleLocalizeResult> PerLocale { get; set; } = new();
		public List<VerificationResult> VerificationResults { get; set; } = new();
	}

	/// <summary>
	/// Per-locale localization result
	/// </summary>
	internal sealed class LocaleLocalizeResult
	{
		public string LocaleMpq { get; set; } = string.Empty;
		public string Language { get; set; } = string.Empty;
		public int TablesMerged { get; set; }
		public int RowsMerged { get; set; }
		public int FieldsUpdated { get; set; }
		public Dictionary<string, List<SampleChange>> SampleChanges { get; set; } = new();
	}
}
