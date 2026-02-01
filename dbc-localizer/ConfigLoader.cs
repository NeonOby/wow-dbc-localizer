using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DbcLocalizer
{
	/// <summary>
	/// Loads and parses configuration from config.json file
	/// </summary>
	internal static class ConfigLoader
	{
		public static string[] LoadConfigAsArgs(string configPath)
		{
			try
			{
				var json = File.ReadAllText(configPath);
				using var doc = JsonDocument.Parse(json);
				var root = doc.RootElement;

				var args = new List<string> { "localize-mpq" };

				// Required fields - check if patch is a directory (multi-file mode)
				string? patchPath = null;

				if (root.TryGetProperty("patch", out var patch) && patch.ValueKind == JsonValueKind.String)
				{
					patchPath = patch.GetString()!;
					
					// Check if it's a directory
					if ((patchPath.EndsWith("/") || patchPath.EndsWith("\\")) && Directory.Exists(patchPath))
					{
						args.Add("--multi-patch-dir");
						args.Add(patchPath);
					}
					else
					{
						args.AddRange(new[] { "--patch", patchPath });
					}
				}

				if (root.TryGetProperty("defs", out var defs) && defs.ValueKind == JsonValueKind.String)
					args.AddRange(new[] { "--defs", defs.GetString()! });

				// Handle output path
				if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.String)
				{
					var outputPath = output.GetString()!;
					args.AddRange(new[] { "--output", outputPath });
				}

				// Locale MPQs (array) - if missing, auto-detect from locale directory
				var mpqs = new List<string>();
				var langList = new List<string>();

				if (root.TryGetProperty("locale-mpqs", out var locales) && locales.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in locales.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.String)
							mpqs.Add(item.GetString()!);
					}
				}
				else
				{
					var localeDir = GetLocaleDirectory(root, patchPath);
					if (!string.IsNullOrWhiteSpace(localeDir) && Directory.Exists(localeDir))
					{
						var detected = Directory
							.GetFiles(localeDir, "*.mpq", SearchOption.TopDirectoryOnly)
							.Where(IsLocaleMpq)
							.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
							.ToList();

						foreach (var file in detected)
							mpqs.Add(file);
					}
				}

				if (mpqs.Count > 0)
					args.AddRange(new[] { "--locale-mpqs", string.Join(";", mpqs) });

				// Languages (array) - if missing, derive from locale mpq names
				if (root.TryGetProperty("langs", out var langs) && langs.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in langs.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.String)
							langList.Add(item.GetString()!);
					}
				}
				else if (mpqs.Count > 0)
				{
					foreach (var mpq in mpqs)
					{
						var lang = ExtractLangFromMpqName(mpq);
						if (!string.IsNullOrWhiteSpace(lang))
							langList.Add(lang);
					}
				}

				if (langList.Count > 0)
					args.AddRange(new[] { "--langs", string.Join(";", langList) });

				// Optional fields
				if (root.TryGetProperty("build", out var build) && build.ValueKind == JsonValueKind.String)
					args.AddRange(new[] { "--build", build.GetString()! });

				if (root.TryGetProperty("auto", out var auto) && auto.ValueKind == JsonValueKind.True)
					args.Add("--auto");

				if (root.TryGetProperty("verbose", out var verbose) && verbose.ValueKind == JsonValueKind.True)
					args.Add("--verbose");

				if (root.TryGetProperty("report", out var report) && report.ValueKind == JsonValueKind.String)
					args.AddRange(new[] { "--report", report.GetString()! });

				return args.ToArray();
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to load config: {ex.Message}");
				return Array.Empty<string>();
			}
		}

		private static string? GetLocaleDirectory(JsonElement root, string? patchPath)
		{
			if (root.TryGetProperty("locale-dir", out var localeDir) && localeDir.ValueKind == JsonValueKind.String)
				return localeDir.GetString();

			if (root.TryGetProperty("locale", out var locale) && locale.ValueKind == JsonValueKind.String)
				return locale.GetString();

			if (!string.IsNullOrWhiteSpace(patchPath))
			{
				var normalized = patchPath!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
				var baseDir = Directory.Exists(normalized)
					? Path.GetDirectoryName(normalized)
					: Path.GetDirectoryName(patchPath);

				if (!string.IsNullOrWhiteSpace(baseDir))
					return Path.Combine(baseDir!, "locale");
			}

			return null;
		}

		private static bool IsLocaleMpq(string path)
		{
			return Regex.IsMatch(Path.GetFileName(path),
				@"^(locale|patch)-[a-zA-Z]{2}[a-zA-Z]{2}(?:-\d+)?\.mpq$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		}

		private static string? ExtractLangFromMpqName(string path)
		{
			var match = Regex.Match(Path.GetFileName(path),
				@"^(?:locale|patch)-(?<lang>[a-zA-Z]{2}[a-zA-Z]{2})(?:-\d+)?\.mpq$",
				RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

			return match.Success ? match.Groups["lang"].Value : null;
		}
	}
}
