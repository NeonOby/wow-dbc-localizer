# DBC-Localizer - Quickstart

Localize texts from WoW MPQ files using the C# CLI tool (supports all locales: deDE, enUS, frFR, ruRU, etc.).

## Requirements
- .NET 10.0 SDK

## Setup
1. Open Visual Studio or VS Code
2. Open `dbc-localizer.sln` in the project root
3. Build: `dotnet build dbc-localizer.sln -c Release`

## Run Localization
```bash
cd dbc-localizer
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq ^
  --patch "path/to/patch-B.mpq" ^
  --locale-mpq "path/to/locale-deDE.MPQ" ^
  --defs "path/to/definitions" ^
  --output "output/Patch-B-localized.mpq" ^
  --dbc "DBFilesClient\Spell.dbc" ^
  --lang deDE ^
  --mpqcli "path/to/mpqcli.exe"
```

Or use defaults from project root:
```bash
cd dbc-localizer
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq
```

This will:
1. ✓ Extract DBCs from both MPQs (patch-B.mpq, locale-deDE.MPQ)
2. ✓ Parse DBD definitions for field mapping
3. ✓ Localize 20,050+ locale texts into Spell.dbc
4. ✓ Create new MPQ: `output/Patch-B-localized.mpq`

## Input Files
Place these in project root:
- `patch-B.mpq` - Base patch file
- `locale-*.MPQ` - Locale data (e.g., locale-deDE.MPQ for German, locale-frFR.MPQ for French)
- `dbcd-lib/definitions/` - WoW 3.3.5 DBD definitions (auto-downloaded)

## Commands
- `localize-mpq` - Full pipeline: extract → localize → repack MPQ
- `localize` - Direct DBC localization (two DBC files)

## Technology Stack
- **C#** / **.NET 10.0**: Core implementation
- **DBCD**: DBC file parsing & writing
- **mpqcli**: MPQ file manipulation

## Legacy Python Version
Original Python implementation in `python/` folder for reference.
