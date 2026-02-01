using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;

namespace DbcLocalizer
{
	internal static class DefsManager
	{
		private const string WoWDbDefsZipUrl = "https://github.com/wowdev/WoWDBDefs/archive/refs/heads/master.zip";

		public static string GetDefaultDefsPath()
		{
			var baseDir = AppContext.BaseDirectory;
			var defsRoot = Path.Combine(baseDir, "defs", "WoWDBDefs");
			return Path.Combine(defsRoot, "definitions");
		}

		public static bool EnsureDefinitions(ref string defsPath, string build)
		{
			if (string.IsNullOrWhiteSpace(defsPath))
				defsPath = GetDefaultDefsPath();

			if (Directory.Exists(defsPath))
				return true;

			Logger.Info($"[*] Definitions not found: {defsPath}");
			Logger.Info($"[*] Downloading WoWDBDefs for build {build}...");

			var defaultDefsPath = GetDefaultDefsPath();
			var targetDefsPath = defsPath;

			if (!PathsEqual(defsPath, defaultDefsPath))
			{
				Logger.Info($"[*] Using default definitions location: {defaultDefsPath}");
				targetDefsPath = defaultDefsPath;
			}

			var success = DownloadAndExtractDefinitions(targetDefsPath);
			if (success)
			{
				defsPath = targetDefsPath;
				return true;
			}

			return false;
		}

		private static bool DownloadAndExtractDefinitions(string targetDefinitionsPath)
		{
			string? tempRoot = null;
			try
			{
				tempRoot = Path.Combine(Path.GetTempPath(), "dbc-localizer", "wowdbdefs", Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(tempRoot);

				var tempZip = Path.Combine(tempRoot, "WoWDBDefs.zip");
				var extractDir = Path.Combine(tempRoot, "extract");

				using (var http = new HttpClient())
				{
					var data = http.GetByteArrayAsync(WoWDbDefsZipUrl).GetAwaiter().GetResult();
					File.WriteAllBytes(tempZip, data);
				}

				ZipFile.ExtractToDirectory(tempZip, extractDir);

				var extractedRoot = Directory.GetDirectories(extractDir).FirstOrDefault();
				if (string.IsNullOrWhiteSpace(extractedRoot))
					return false;

				var sourceDefinitions = Path.Combine(extractedRoot, "definitions");
				if (!Directory.Exists(sourceDefinitions))
					return false;

				Directory.CreateDirectory(targetDefinitionsPath);
				CopyDirectory(sourceDefinitions, targetDefinitionsPath);

				Logger.Info($"[*] Definitions downloaded to: {targetDefinitionsPath}");
				return true;
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to download WoWDBDefs: {ex.Message}");
				return false;
			}
			finally
			{
				try
				{
					if (!string.IsNullOrWhiteSpace(tempRoot) && Directory.Exists(tempRoot))
						Directory.Delete(tempRoot, true);

					var wowdefsRoot = Path.Combine(Path.GetTempPath(), "dbc-localizer", "wowdbdefs");
					if (Directory.Exists(wowdefsRoot) && !Directory.EnumerateFileSystemEntries(wowdefsRoot).Any())
						Directory.Delete(wowdefsRoot, true);

					var dbcRoot = Path.Combine(Path.GetTempPath(), "dbc-localizer");
					if (Directory.Exists(dbcRoot) && !Directory.EnumerateFileSystemEntries(dbcRoot).Any())
						Directory.Delete(dbcRoot, true);
				}
				catch
				{
					// ignore cleanup errors
				}
			}
		}

		private static void CopyDirectory(string sourceDir, string destDir)
		{
			Directory.CreateDirectory(destDir);

			foreach (var file in Directory.GetFiles(sourceDir))
			{
				var destFile = Path.Combine(destDir, Path.GetFileName(file));
				File.Copy(file, destFile, true);
			}

			foreach (var dir in Directory.GetDirectories(sourceDir))
			{
				var destSub = Path.Combine(destDir, Path.GetFileName(dir));
				CopyDirectory(dir, destSub);
			}
		}

		private static bool PathsEqual(string a, string b)
		{
			return string.Equals(
				Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
				StringComparison.OrdinalIgnoreCase);
		}
	}
}
