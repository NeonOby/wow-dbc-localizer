using System;

namespace DbcLocalizer
{
	/// <summary>
	/// Writes usage and help information to console
	/// </summary>
	internal static class UsageWriter
	{
		public static void PrintUsage()
		{
			Console.WriteLine(@"
DBC Localizer - Localize WoW DBC files with locale data

USAGE:
  dbc-localizer                 (run with config.json - automatic mode)
  dbc-localizer localize --base <path> --locale <path> --defs <path> --output <path> [options]
  dbc-localizer localize-mpq --patch <mpq> --locale-mpq <mpq> --defs <path> --output <mpq> [options]
  dbc-localizer scan-mpq --patch <mpq> --locale-mpq <mpq> --defs <path> [options]

COMMANDS:
  localize      Localize a single DBC file
  localize-mpq  Localize DBC(s) from MPQ archives
  scan-mpq      List all localizable DBCs in MPQ archives

AUTOMATIC MODE (config.json):
  If you run dbc-localizer without arguments, it will look for config.json in the current directory.
  Create a config.json file with the following structure:
  {
    ""patch"": ""input/patch/patch-B.mpq"",
    ""locale-dir"": ""input/locale/"",
    ""defs"": ""defs/WoWDBDefs/definitions"",
    ""build"": ""3.3.5.12340"",
    ""output"": ""output/localized.mpq"",
    ""auto"": true,
    ""verbose"": false,
    ""report"": ""output/localize-report.json""
  }

  If ""locale-mpqs"" is not provided, locale MPQs are auto-detected from ""locale-dir""
  (matching locale-<lang>.mpq and patch-<lang>-X.mpq), and languages are derived
  from the filenames.

LOCALIZE OPTIONS:
  --base <path>       Base DBC file (patch)
  --locale <path>     Locale DBC file
  --defs <path>       DBD definitions directory
  --output <path>     Output DBC path
  --build <build>     WoW build (default: 3.3.5.12340)
  --lang <code>       Locale code (default: deDE)

LOCALIZE-MPQ OPTIONS:
  --patch <mpq>         Patch MPQ file
  --locale-mpq <mpq>    Single locale MPQ file
  --locale-mpqs <list>  Multiple locale MPQs (semicolon-separated)
  --langs <list>        Language codes (semicolon-separated, matches --locale-mpqs order)
  --defs <path>         DBD definitions directory
  --output <mpq>        Output MPQ path
  --dbc <path>          Single DBC relative path (e.g., DBFilesClient\Spell.dbc)
  --dbc-list <list>     Semicolon-separated DBC paths (e.g., ""A.dbc;B.dbc"")
  --select              Interactive DBC selection mode
  --auto                Automatically localize all localizable DBCs
  --build <build>       WoW build (default: 3.3.5.12340)
  --lang <code>         Locale code for single locale (default: deDE)
  --mpqcli <path>       Path to mpqcli.exe
  --keep-temp           Keep temporary files
  --report <path>       Write JSON report
  --log-level <level>   Log level: info or verbose (default: info)
  --verbose             Enable verbose logging (alias for --log-level verbose)

SCAN-MPQ OPTIONS:
  --patch <mpq>         Patch MPQ file
  --locale-mpq <mpq>    Single locale MPQ file
  --locale-mpqs <list>  Multiple locale MPQs (semicolon-separated)
  --defs <path>         DBD definitions directory
  --build <build>       WoW build (default: 3.3.5.12340)
  --mpqcli <path>       Path to mpqcli.exe

EXAMPLES:
  # Run with config.json (automatic)
  dbc-localizer

  # Scan for localizable DBCs
  dbc-localizer scan-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions

  # Interactive selection
  dbc-localizer localize-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --select

  # Localize all localizable DBCs automatically
  dbc-localizer localize-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --auto

  # Localize specific DBC
  dbc-localizer localize-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --dbc DBFilesClient\Spell.dbc

  # Localize multiple locales
  dbc-localizer localize-mpq --patch patch.mpq --locale-mpqs ""locale-deDE.MPQ;locale-frFR.MPQ"" --langs ""deDE;frFR"" --defs definitions --output output.mpq --auto

  # Verbose logging
  dbc-localizer localize-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --auto --verbose
");
		}
	}
}
