# DBC-Localizer

<table>
  <tr>
    <td width="420" valign="top">
      <img src="dbc-localizer/icons/wow-style-icon--welt-als-hintegrund--zahnrad-umran.png" width="420" alt="DBC-Localizer Icon" />
    </td>
    <td valign="top">
      <strong>Automated German text localization for World of Warcraft MPQ files (build configurable)</strong>
      <br />
      A C# tool that extracts German locale data from <code>locale-*.MPQ</code> and localizes patch MPQs using build-specific WDBX definitions. Build/version is configurable via CLI or <code>config.json</code>.
      <br /><br />
      <strong>Features</strong>
      <ul>
        <li>✓ <strong>Fast C# implementation</strong> - High-performance DBC processing</li>
        <li>✓ <strong>Batch processing</strong> - Localize multiple patches/locales at once</li>
        <li>✓ <strong>Multiple modes</strong> - Auto-detect, interactive selection, or explicit DBC lists</li>
        <li>✓ <strong>JSON reports</strong> - Detailed localization statistics and warnings</li>
        <li>✓ <strong>Config file support</strong> - Fully automated via <code>config.json</code></li>
        <li>✓ <strong>WDBX definitions</strong> - Build-specific DBC field mappings (configurable)</li>
      </ul>
    </td>
  </tr>
</table>

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
├── dbcd-lib/                        # DBCD Library (auto-downloaded at build)
│   ├── DBCD/                       # DBC file reader/writer
│   └── DBCD.IO/                    # Low-level I/O operations
│
├── bin/Release/                     # Release output
│   ├── input/                      # Input MPQ files
│   │   ├── locale/                 # Locale MPQ files
│   │   └── patch/                  # Patch MPQ files
│   └── output/                     # Generated output
│
├── tools/                           # External tools
│   └── mpqcli.exe                  # MPQ manipulation CLI
│
└── tools/                           # External tools
  └── mpqcli.exe                  # MPQ manipulation CLI
```

## Installation

### Prerequisites
- **.NET 9.0 SDK**
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
  --defs defs/WoWDBDefs/definitions \
  --output output/ \
  --auto \
  --report output/{patch}-report.json
```

### Command Line - Multiple Locales
```bash
dbc-localizer localize-mpq \
  --patch input/patch/patch-B.mpq \
  --locale-mpqs "input/locale/locale-deDE.MPQ;input/locale/patch-deDE.MPQ" \
  --langs "deDE;deDE" \
  --defs defs/WoWDBDefs/definitions \
  --output output/ \
  --auto
```

### Interactive Selection
```bash
dbc-localizer localize-mpq \
  --patch input/patch/patch-B.mpq \
  --locale-mpq input/locale/locale-deDE.MPQ \
  --defs defs/WoWDBDefs/definitions \
  --output output/ \
  --select
```

## Configuration File (config.json)

```json
{
  "patch": "input/patch/patch-B.mpq",
  "locale-dir": "input/locale/",
  "defs": "defs/WoWDBDefs/definitions",
  "build": "3.3.5.12340",
  "output": "output/",
  "auto": true,
  "verbose": false,
  "report": "output/{patch}-report.json"
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

- **Language**: C# (.NET 9.0)
- **Architecture**: Single-responsibility command handlers
- **Build**: dotnet CLI / Visual Studio 2024+
- **MPQ Tooling**: mpqcli
- **Definitions**: WoWDBDefs (auto-downloaded at runtime)
