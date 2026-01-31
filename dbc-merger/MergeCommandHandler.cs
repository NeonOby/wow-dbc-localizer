using System;
using System.IO;

namespace DbcMerger
{
	/// <summary>
	/// Handles the "merge" command for single DBC file merging
	/// </summary>
	internal static class MergeCommandHandler
	{
		public static int Execute(string[] args)
		{
			Logger.SetLogLevel(args);

			var mergeArgs = MergeArgs.Parse(args);

			if (!mergeArgs.IsValid)
				return Fail("Missing required arguments. Use --base, --locale, --defs, --output.");

			if (!File.Exists(mergeArgs.BasePath))
				return Fail($"Base DBC not found: {mergeArgs.BasePath}");
			if (!File.Exists(mergeArgs.LocalePath))
				return Fail($"Locale DBC not found: {mergeArgs.LocalePath}");
			if (!Directory.Exists(mergeArgs.DefsPath))
				return Fail($"Definitions path not found: {mergeArgs.DefsPath}");

			var baseName = Path.GetFileNameWithoutExtension(mergeArgs.BasePath);
			var localeName = Path.GetFileNameWithoutExtension(mergeArgs.LocalePath);
			if (!string.Equals(baseName, localeName, StringComparison.OrdinalIgnoreCase))
			{
				return Fail("Base and locale DBC names must match (e.g., Spell.dbc + Spell.dbc).", 2);
			}

			var exitCode = MergeEngine.MergeDbc(
				mergeArgs.BasePath,
				mergeArgs.LocalePath,
				mergeArgs.DefsPath,
				mergeArgs.OutputPath,
				mergeArgs.Build,
				mergeArgs.LocaleCode,
				mergeArgs.LocalePath,
				mergeArgs.BasePath,
				Logger.Verbose,
				out var stats);

			if (exitCode == 0)
			{
				Logger.Info($"[*] Total rows merged: {stats.RowsMerged}");
				Logger.Info($"[*] Total fields updated: {stats.FieldUpdates}");
			}

			return exitCode;
		}

		private static int Fail(string message, int exitCode = 1)
		{
			Logger.Error(message);
			return exitCode;
		}
	}
}
