using System;

namespace DbcMerger
{
	/// <summary>
	/// Writes usage and help information to console
	/// </summary>
	internal static class UsageWriter
	{
		public static void PrintUsage()
		{
			Console.WriteLine(@"
DBC Merger - Merge localized strings into WoW DBC files

USAGE:
  dbc-merger                    (run with config.json - automatic mode)
  dbc-merger merge --base <path> --locale <path> --defs <path> --output <path> [options]
  dbc-merger merge-mpq --patch <mpq> --locale-mpq <mpq> --defs <path> --output <mpq> [options]
  dbc-merger scan-mpq --patch <mpq> --locale-mpq <mpq> --defs <path> [options]

COMMANDS:
  merge         Merge a single DBC file
  merge-mpq     Merge DBC(s) from MPQ archives
  scan-mpq      List all localizable DBCs in MPQ archives

AUTOMATIC MODE (config.json):
  If you run dbc-merger without arguments, it will look for config.json in the current directory.
  Create a config.json file with the following structure:
  {
    ""patch"": ""input/patch/patch-B.mpq"",
    ""locale-mpqs"": [
      ""input/locale/locale-deDE.MPQ"",
      ""input/locale/patch-deDE.MPQ"",
      ""input/locale/patch-deDE-2.MPQ"",
      ""input/locale/patch-deDE-3.MPQ""
    ],
    ""langs"": [""deDE"", ""deDE"", ""deDE"", ""deDE""],
    ""defs"": ""test-dbcd/definitions/definitions"",
    ""output"": ""output/merged.mpq"",
    ""auto"": true,
    ""verbose"": false,
    ""report"": ""output/merge-report.json""
  }

MERGE OPTIONS:
  --base <path>       Base DBC file (patch)
  --locale <path>     Locale DBC file
  --defs <path>       DBD definitions directory
  --output <path>     Output DBC path
  --build <build>     WoW build (default: 3.3.5.12340)
  --lang <code>       Locale code (default: deDE)

MERGE-MPQ OPTIONS:
  --patch <mpq>         Patch MPQ file
  --locale-mpq <mpq>    Single locale MPQ file
  --locale-mpqs <list>  Multiple locale MPQs (semicolon-separated)
  --langs <list>        Language codes (semicolon-separated, matches --locale-mpqs order)
  --defs <path>         DBD definitions directory
  --output <mpq>        Output MPQ path
  --dbc <path>          Single DBC relative path (e.g., DBFilesClient\Spell.dbc)
  --dbc-list <list>     Semicolon-separated DBC paths (e.g., ""A.dbc;B.dbc"")
  --select              Interactive DBC selection mode
  --auto                Automatically merge all localizable DBCs
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
  dbc-merger

  # Scan for localizable DBCs
  dbc-merger scan-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions

  # Interactive selection
  dbc-merger merge-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --select

  # Merge all localizable DBCs automatically
  dbc-merger merge-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --auto

  # Merge specific DBC
  dbc-merger merge-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --dbc DBFilesClient\Spell.dbc

  # Merge multiple locales
  dbc-merger merge-mpq --patch patch.mpq --locale-mpqs ""locale-deDE.MPQ;locale-frFR.MPQ"" --langs ""deDE;frFR"" --defs definitions --output output.mpq --auto

  # Verbose logging
  dbc-merger merge-mpq --patch patch.mpq --locale-mpq locale-deDE.MPQ --defs definitions --output output.mpq --auto --verbose
");
		}
	}
}
