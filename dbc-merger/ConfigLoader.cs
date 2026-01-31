using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DbcMerger
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

				var args = new List<string> { "merge-mpq" };

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

				// Locale MPQs (array)
				if (root.TryGetProperty("locale-mpqs", out var locales) && locales.ValueKind == JsonValueKind.Array)
				{
					var mpqs = new List<string>();
					foreach (var item in locales.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.String)
							mpqs.Add(item.GetString()!);
					}
					if (mpqs.Count > 0)
						args.AddRange(new[] { "--locale-mpqs", string.Join(";", mpqs) });
				}

				// Languages (array)
				if (root.TryGetProperty("langs", out var langs) && langs.ValueKind == JsonValueKind.Array)
				{
					var langList = new List<string>();
					foreach (var item in langs.EnumerateArray())
					{
						if (item.ValueKind == JsonValueKind.String)
							langList.Add(item.GetString()!);
					}
					if (langList.Count > 0)
						args.AddRange(new[] { "--langs", string.Join(";", langList) });
				}

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
	}
}
