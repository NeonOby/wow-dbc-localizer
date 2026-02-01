using System;
using System.Collections.Generic;
using System.IO;

namespace DbcLocalizer
{
	/// <summary>
	/// Handles the "scan-mpq" command for listing localizable DBCs
	/// </summary>
	internal static class ScanMpqCommandHandler
	{
		public static int Execute(string[] args)
		{
			var scanArgs = ScanMpqArgs.Parse(args);
			if (string.IsNullOrWhiteSpace(scanArgs.DefsPath))
				scanArgs.DefsPath = DefsManager.GetDefaultDefsPath();

			if (!scanArgs.IsValid)
				return Fail("Missing required arguments. Use --patch, --defs.");

			if (!File.Exists(scanArgs.PatchMpq))
				return Fail($"Patch MPQ not found: {scanArgs.PatchMpq}");

			if (scanArgs.LocaleMpqs.Count == 0)
				return Fail("Missing locale MPQ(s). Use --locale-mpq or --locale-mpqs.");

			foreach (var mpq in scanArgs.LocaleMpqs)
			{
				if (!File.Exists(mpq))
					return Fail($"Locale MPQ not found: {mpq}");
			}

			if (!DefsManager.EnsureDefinitions(ref scanArgs.DefsPath, scanArgs.Build))
				return Fail($"Definitions path not found: {scanArgs.DefsPath}");

			if (!File.Exists(scanArgs.MpqCliPath))
				return Fail($"mpqcli not found: {scanArgs.MpqCliPath}");

			var warnings = new List<string>();
			var candidates = DbcScanner.GetLocalizableDbcCandidates(scanArgs.MpqCliPath, scanArgs.PatchMpq, scanArgs.LocaleMpqs, scanArgs.DefsPath, scanArgs.Build, warnings);

			Logger.Info($"[*] Found {candidates.Count} localizable DBC table(s):");
			foreach (var c in candidates)
			{
				Logger.Info($"  - {c.Path} ({c.LocCount} locstring field(s))");
			}

			if (warnings.Count > 0)
			{
				Logger.Info("");
				Logger.Info($"[!] {warnings.Count} warning(s):");
				foreach (var w in warnings)
				{
					Logger.Info($"  - {w}");
				}
			}

			return 0;
		}

		private static int Fail(string message, int exitCode = 1)
		{
			Logger.Error(message);
			return exitCode;
		}
	}
}
