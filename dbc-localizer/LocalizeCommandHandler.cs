using System;
using System.IO;

namespace DbcLocalizer
{
	/// <summary>
	/// Handles the "localize" command for single DBC file localization
	/// </summary>
	internal static class LocalizeCommandHandler
	{
		public static int Execute(string[] args)
		{
			Logger.SetLogLevel(args);

			var localizeArgs = LocalizeArgs.Parse(args);
			localizeArgs.DefsPath = DefsManager.GetDefaultDefsPath();
			if (!string.IsNullOrWhiteSpace(Helpers.GetArg(args, "--defs")))
				localizeArgs.DefsPath = Helpers.GetArg(args, "--defs")!;

			if (!localizeArgs.IsValid)
				return Fail("Missing required arguments. Use --base, --locale, --defs, --output.");

			if (!File.Exists(localizeArgs.BasePath))
				return Fail($"Base DBC not found: {localizeArgs.BasePath}");
			if (!File.Exists(localizeArgs.LocalePath))
				return Fail($"Locale DBC not found: {localizeArgs.LocalePath}");
			if (!DefsManager.EnsureDefinitions(ref localizeArgs.DefsPath, localizeArgs.Build))
				return Fail($"Definitions path not found: {localizeArgs.DefsPath}");

			var baseName = Path.GetFileNameWithoutExtension(localizeArgs.BasePath);
			var localeName = Path.GetFileNameWithoutExtension(localizeArgs.LocalePath);
			if (!string.Equals(baseName, localeName, StringComparison.OrdinalIgnoreCase))
			{
				return Fail("Base and locale DBC names must match (e.g., Spell.dbc + Spell.dbc).", 2);
			}

			var exitCode = LocalizeEngine.LocalizeDbc(
				localizeArgs.BasePath,
				localizeArgs.LocalePath,
				localizeArgs.DefsPath,
				localizeArgs.OutputPath,
				localizeArgs.Build,
				localizeArgs.LocaleCode,
				localizeArgs.LocalePath,
				localizeArgs.BasePath,
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
