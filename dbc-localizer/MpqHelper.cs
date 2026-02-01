using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DbcLocalizer
{
	internal static class MpqHelper
	{
		public static string GetDefaultMpqCliPath()
		{
			var baseDir = AppContext.BaseDirectory;
			var localTools = Path.Combine(baseDir, "tools", "mpqcli.exe");
			if (File.Exists(localTools))
				return localTools;

			var repoTools = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "tools", "mpqcli.exe"));
			return repoTools;
		}

		public static string ExtractFile(string mpqcliPath, string mpqPath, string filePath, string outputDir)
		{
			Directory.CreateDirectory(outputDir);
			RunProcess(mpqcliPath, $"extract \"{mpqPath}\" --file \"{filePath}\" --output \"{outputDir}\" --keep");
			var extractedPath = Path.Combine(outputDir, filePath);
			if (!File.Exists(extractedPath))
				throw new FileNotFoundException($"Extracted file not found: {extractedPath}");
			return extractedPath;
		}

		public static void RemoveFile(string mpqcliPath, string mpqPath, string filePath)
		{
			RunProcess(mpqcliPath, $"remove \"{filePath}\" \"{mpqPath}\"");
		}

		public static void AddFile(string mpqcliPath, string mpqPath, string localPath, string archivePath)
		{
			if (string.IsNullOrWhiteSpace(archivePath))
			{
				RunProcess(mpqcliPath, $"add \"{localPath}\" \"{mpqPath}\"");
			}
			else
			{
				// FIX: mpqcli appends the local filename to the --path argument
				// We need to rename the file to match the archive basename and use parent directory
				var archiveFileName = Path.GetFileName(archivePath);
				var archiveDir = Path.GetDirectoryName(archivePath);
				
				// Create temp directory and copy with correct name
				var tempDir = Path.Combine(Path.GetTempPath(), "dbc-localizer", "mpq-add", Guid.NewGuid().ToString("N"));
				Directory.CreateDirectory(tempDir);
				var renamedPath = Path.Combine(tempDir, archiveFileName);
				
				try
				{
					File.Copy(localPath, renamedPath, true);
					
					// Use parent directory as --path so mpqcli appends the filename correctly
					if (string.IsNullOrWhiteSpace(archiveDir))
					{
						RunProcess(mpqcliPath, $"add \"{renamedPath}\" \"{mpqPath}\"");
					}
					else
					{
						RunProcess(mpqcliPath, $"add \"{renamedPath}\" \"{mpqPath}\" --path \"{archiveDir}\"");
					}
				}
				finally
				{
					// Cleanup
					try
					{
						if (File.Exists(renamedPath))
							File.Delete(renamedPath);
						if (Directory.Exists(tempDir))
							Directory.Delete(tempDir, true);
					}
					catch { /* ignore cleanup errors */ }
				}
			}
		}

		public static List<string> ListFiles(string mpqcliPath, string mpqPath)
		{
			var output = RunProcessCapture(mpqcliPath, $"list \"{mpqPath}\" --all");
			return output
				.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
				.Select(l => l.Trim())
				.Where(l => !string.IsNullOrWhiteSpace(l))
				.ToList();
		}

		public static IEnumerable<string> FilterDbcPaths(IEnumerable<string> mpqListLines)
		{
			foreach (var line in mpqListLines)
			{
				var normalized = line.Replace('/', '\\');
				if (normalized.IndexOf("DBFilesClient\\", StringComparison.OrdinalIgnoreCase) >= 0 &&
					normalized.EndsWith(".dbc", StringComparison.OrdinalIgnoreCase))
				{
					yield return normalized;
				}
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
			if (process == null)
				throw new Exception($"Failed to start process: {exePath} {arguments}");

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

		private static string RunProcessCapture(string exePath, string arguments)
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
			if (process == null)
				throw new Exception($"Failed to start process: {exePath} {arguments}");

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (process.ExitCode != 0)
				throw new Exception($"Command failed: {exePath} {arguments}\n{error}");

			return output;
		}
	}
}
