# DBC-Localizer

![DBC-Localizer Icon](icons/wow-style-icon--welt-als-hintegrund--zahnrad-umran.png)

**Automated German text localization for World of Warcraft 3.3.5 MPQ files**

A C# tool that extracts German locale data from `locale-deDE.MPQ` and localizes `patch-B.mpq` using WDBX XML definitions. Creates fully-localized MPQ files ready for use in WoW 3.3.5.

## Features
- ✓ **Fast C# implementation** - High-performance DBC processing
- ✓ **Batch processing** - Localize multiple patches/locales at once
- ✓ **Multiple modes** - Auto-detect, interactive selection, or explicit DBC lists
- ✓ **JSON reports** - Detailed localization statistics and warnings
- ✓ **Config file support** - Fully automated via `config.json`
- ✓ **WDBX definitions** - WoW 3.3.5 DBC field mappings

## Project Structure

```
DBC-Localizer/
├── dbc-localizer/                   # C# Console Application (Main Tool)
│   ├── Program.cs                  # CLI Entry point
│   ├── CommandArgs.cs              # Argument parsing classes
│   ├── ConfigLoader.cs             # Config.json loading
│   ├── UsageWriter.cs              # Help text
│   ├── LocalizeCommandHandler.cs    # Single DBC localization
│   ├── LocalizeMpqCommandHandler.cs # MPQ localization with batch support
│   ├── ScanMpqCommandHandler.cs     # DBC scanning
│   ├── LocalizeEngine.cs            # Core localization logic
│   ├── MpqHelper.cs                # MPQ I/O operations
│   ├── DbcScanner.cs               # Localizable DBC detection
│   ├── Logger.cs                   # Logging
│   ├── Models.cs                   # Data structures
│   ├── Helpers.cs                  # Utility functions
│   ├── dbc-localizer.csproj        # Project file
│   └── config.json                 # Configuration example
│
├── dbc-localizer.sln                # Visual Studio Solution
│
├── dbcd-lib/                        # DBCD Library & Definitions
│   ├── DBCD/                       # DBC file reader/writer
│   ├── DBCD.IO/                    # Low-level I/O operations
│   └── definitions/                # WoW 3.3.5 DBD definitions
│
├── input/                           # Input MPQ files
│   ├── locale/                     # Locale MPQ files
│   └── patch/                      # Patch MPQ files
│
├── output/                          # Generated output
│   └── Patch-*.mpq                 # Localized MPQ files
│
├── tools/                           # External tools
│   └── mpqcli.exe                  # MPQ manipulation CLI
│
└── DeleteMe/                        # Legacy & cleanup folder
    ├── python/                     # Legacy Python implementation
    ├── pywowlib/                   # Original Python library
    ├── temp/                       # Old test scripts
    └── venv/                       # Legacy virtual environment
```

## Installation

### Prerequisites
- **.NET 10.0 SDK**
- **Input MPQ files** in appropriate folders

### Quick Setup
```bash
cd dbc-localizer
dotnet build -c Release
```

The build will automatically download:
- **dbcd-lib** from https://github.com/wowdev/DBCD
- **mpqcli** from https://github.com/TheGrayDot/mpqcli/releases

**For detailed setup instructions, see [SETUP.md](SETUP.md)**

## Usage

### Quick Start - Automatic Mode
```bash
# 1. Configure input files in config.json
# 2. Run without arguments
dbc-localizer

# Output: Localized MPQ file + JSON report
```

### Command Line - Single Locale
```bash
dbc-localizer localize-mpq \
  --patch input/patch/patch-B.mpq \
  --locale-mpq input/locale/locale-deDE.MPQ \
  --defs dbcd-lib/definitions/definitions \
  --output output/Patch-B-localized.mpq \
  --auto \
  --report output/localize-report.json
```

### Command Line - Multiple Locales
```bash
dbc-localizer localize-mpq \
  --patch input/patch/patch-B.mpq \
  --locale-mpqs "input/locale/locale-deDE.MPQ;input/locale/patch-deDE.MPQ" \
  --langs "deDE;deDE" \
  --defs dbcd-lib/definitions/definitions \
  --output output/Patch-B-localized.mpq \
  --auto
```

### Interactive Selection
```bash
dbc-localizer localize-mpq \
  --patch input/patch/patch-B.mpq \
  --locale-mpq input/locale/locale-deDE.MPQ \
  --defs dbcd-lib/definitions/definitions \
  --output output/Patch-B-localized.mpq \
  --select
```

## Configuration File (config.json)

```json
{
  "patch": "input/patch/patch-B.mpq",
  "locale-mpqs": [
    "input/locale/locale-deDE.MPQ",
    "input/locale/patch-deDE.MPQ"
  ],
  "langs": ["deDE", "deDE"],
  "defs": "dbcd-lib/definitions/definitions",
  "output": "output/Patch-B-localized.mpq",
  "auto": true,
  "verbose": false,
  "report": "output/localize-report.json"
}
```

## Commands

### localize
Localize a single DBC file between base and locale versions.

```bash
dbc-localizer localize \
  --base <path> --locale <path> \
  --defs <path> --output <path>
```

### localize-mpq
Localize DBC files from MPQ archives (main command).

```bash
dbc-localizer localize-mpq \
  --patch <mpq> --locale-mpq <mpq> \
  --defs <path> --output <mpq>
```

### scan-mpq
List all localizable DBCs in MPQ archives.

```bash
dbc-localizer scan-mpq \
  --patch <mpq> --locale-mpq <mpq> \
  --defs <path>
```

## How It Works

1. **Scan Phase**: Identify localizable DBCs (containing locstring fields)
2. **Extract Phase**: Extract DBC files from both patch and locale MPQs
3. **Localization Phase**: Combine German texts with base patch data using WDBX definitions
4. **Update Phase**: Remove old DBCs and add localized versions to output MPQ
5. **Report Phase**: Generate JSON report with statistics

## Architecture

### Key Classes

- **Program.cs** - CLI entry point and command routing
- **CommandArgs.cs** - Type-safe argument parsing (LocalizeArgs, LocalizeMpqArgs, ScanMpqArgs)
- **LocalizeMpqCommandHandler.cs** - Main localization orchestration with multi-patch support
- **LocalizeEngine.cs** - Core DBC localization logic
- **DbcScanner.cs** - Localization candidate detection
- **MpqHelper.cs** - MPQ file operations via mpqcli
- **ConfigLoader.cs** - config.json parsing

### Dependencies

- **DBCD** - DBC file format reader/writer
- **DBCD.IO** - Binary I/O operations
- **System.Text.Json** - JSON serialization for reports

## Testing

See [QUICKSTART.md](QUICKSTART.md) for test data and examples.

## Troubleshooting

### "mpqcli not found"
Ensure `tools/mpqcli.exe` exists and is in the correct path.

### "No localizable DBCs found"
The locale MPQ might not contain locstring fields for the target DBC.

### "Number of languages must match number of locale MPQs"
Each locale MPQ must have a corresponding language code.

## License

See LICENSE file for details.

## Author

NeonOby - 2026

## Technical Stack

- **Language**: C# 13 (.NET 9.0)
- **Architecture**: Single-responsibility command handlers
- **Code Style**: Clean, maintainable, well-documented
- **Build**: dotnet CLI / Visual Studio 2024+
- **DBC Format**: WotLK (3.3.5) with 107 fields
- **Record Size**: 936 bytes (Spell.dbc)
- **German Texts**: 22,979 entries
- **Output Size**: ~29.7 MB
- **Localization Logic**: Field-by-field comparison with preservation of base data

## Requirements
- Python 3.12+
- pywowlib (included, requires Cython build)
- mpqcli v0.9.6 (included in `tools/`)
- Input: patch-B.mpq, locale-deDE.MPQ

## Tools
- **pywowlib**: MPQ archive reading, DBC parsing, StormLib bindings
- **mpqcli**: CLI tool for adding/removing files in MPQ archives
- **WDBX**: XML-based DBC field definitions

## License
See [pywowlib/LICENSE](pywowlib/LICENSE)

## Notes
- All paths are relative (PROJECT_ROOT-based)
- Old development files in `temp/` directory
- Test scripts preserved but unused
- Configuration in scripts (no config files needed)

---
For development details, see previous test files in `temp/` directory.
