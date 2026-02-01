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
DBC Localizer v2.0.0 - Localize WoW DBC files with locale data

USAGE:
  dbc-localizer                 (run with config.json - automatic mode)
  dbc-localizer localize --base <path> --locale <path> --defs <path> --output <path> [options]
  dbc-localizer localize-mpq --patch <mpq> --locale-mpq <mpq> --defs <path> --output <mpq> [options]
  dbc-localizer scan-mpq --patch <mpq> --locale-mpq <mpq> --defs <path> [options]
  dbc-localizer verify-dbc --mpq <mpq> --dbc <path> [--id <record-id>] [options]

COMMANDS:
  localize      Localize a single DBC file
  localize-mpq  Localize DBC(s) from MPQ archives
  scan-mpq      List all localizable DBCs in MPQ archives
  verify-dbc    Verify and inspect DBC content in MPQ archives

AUTOMATIC MODE (config.json):
  If you run dbc-localizer without arguments, it will look for config.json in the current directory.
  Create a config.json file with the following structure:

  {
    ""patch"": ""input/patch/"",
    ""locale-dir"": ""input/locale/"",
    ""defs"": ""defs/WoWDBDefs/definitions"",
    ""build"": ""3.3.5.12340"",
    ""output"": ""output/"",
    ""auto"": true,
    ""verbose"": false,
    ""report"": ""output/{patch}-report.json"",
    ""clear-output"": true,
    ""fallback-locale"": ""enUS""
  }

  AUTOMATIC LOCALE DETECTION:
  - Locale codes are automatically extracted from MPQ filenames
  - Example: locale-deDE.MPQ → deDE, locale-frFR.MPQ → frFR, patch-ruRU-2.mpq → ruRU
  
  CROSS-PRODUCT MODE (automatic with multiple locales + patch directory):
  - When ""patch"" is a directory and multiple locale MPQs exist, ALL combinations are created:
    - patch-A.mpq + locale-deDE.MPQ → output/patch-A-deDE.mpq
    - patch-A.mpq + locale-frFR.MPQ → output/patch-A-frFR.mpq
    - patch-B.mpq + locale-deDE.MPQ → output/patch-B-deDE.mpq
    - patch-B.mpq + locale-frFR.MPQ → output/patch-B-frFR.mpq

  The {patch} placeholder in ""report"" is replaced with the patch filename + locale (no extension).

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
                        If omitted, locales are auto-detected from MPQ filenames
  --defs <path>         DBD definitions directory
  --output <mpq>        Output MPQ path
  --dbc <path>          Single DBC relative path (e.g., DBFilesClient\Spell.dbc)
  --dbc-list <list>     Semicolon-separated DBC paths (e.g., ""A.dbc;B.dbc"")
  --select              Interactive DBC selection mode
  --auto                Automatically localize all localizable DBCs
  --build <build>       WoW build (default: 3.3.5.12340)
  --lang <code>         Locale code for single locale (if not auto-detected)
  --mpqcli <path>       Path to mpqcli.exe
  --keep-temp           Keep temporary files
  --report <path>       Write JSON report
  --cross-product       Create all patch×locale combinations (automatic in config mode)
  --clear-output        Clear output directory before processing (default: true in config mode)
  --log-level <level>   Log level: info or verbose (default: info)
  --verbose             Enable verbose logging (alias for --log-level verbose)
  --fallback-locale <code>  Fallback locale for missing translations (default: enUS, empty = no fallback)

SCAN-MPQ OPTIONS:
  --patch <mpq>         Patch MPQ file
  --locale-mpq <mpq>    Single locale MPQ file
  --locale-mpqs <list>  Multiple locale MPQs (semicolon-separated)
  --defs <path>         DBD definitions directory
  --build <build>       WoW build (default: 3.3.5.12340)
  --mpqcli <path>       Path to mpqcli.exe

VERIFY-DBC OPTIONS:
  --mpq <mpq>           Output MPQ file to inspect
  --dbc <path>          DBC relative path (e.g., DBFilesClient\Spell.dbc)
  --id <record-id>      Search for specific record ID
  --show-all-ids        Show all record IDs grouped by thousands

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

  # Verify DBC content - check specific record
  dbc-localizer verify-dbc --mpq output/patch-Y.mpq --dbc ""DBFilesClient\\Spell.dbc"" --id 81423

  # Verify DBC content - show summary
  dbc-localizer verify-dbc --mpq output/patch-Y.mpq --dbc ""DBFilesClient\\Spell.dbc""

  # Verbose logging
  dbc-localizer localize-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --auto --verbose
");
		}
	}
}
