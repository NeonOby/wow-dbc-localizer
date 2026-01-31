# DBC-Localizer - Quickstart

Merge German texts from WoW 3.3.5 MPQ files using C# CLI tool.

## Requirements
- .NET 9.0 Runtime or SDK
- mpqcli.exe (already in `tools/`)

## Setup
1. Open Visual Studio or VS Code
2. Open `dbc-merger.sln` in the project root
3. Build: `dotnet build dbc-merger.sln -c Release`

## Run Merge
```bash
cd dbc-merger
dotnet bin/Release/net9.0/dbc-merger.dll merge-mpq ^
  --patch "path/to/patch-B.mpq" ^
  --locale-mpq "path/to/locale-deDE.MPQ" ^
  --defs "path/to/definitions" ^
  --output "output/Patch-B-merged.mpq" ^
  --dbc "DBFilesClient\Spell.dbc" ^
  --lang deDE ^
  --mpqcli "path/to/mpqcli.exe"
```

Or use defaults from project root:
```bash
cd dbc-merger
dotnet bin/Release/net9.0/dbc-merger.dll merge-mpq
```

This will:
1. ✓ Extract DBCs from both MPQs (patch-B.mpq, locale-deDE.MPQ)
2. ✓ Parse DBD definitions for field mapping
3. ✓ Merge 20,050+ German texts into Spell.dbc
4. ✓ Create new MPQ: `output/Patch-B-merged.mpq`

## Input Files
Place these in project root:
- `patch-B.mpq` - Base patch file
- `locale-deDE.MPQ` - German locale data
- `test-dbcd/definitions/` - WoW 3.3.5 DBD definitions (already included)

## Commands
- `merge-mpq` - Full pipeline: extract → merge → repack MPQ
- `merge` - Direct DBC merge (two DBC files)

## Technology Stack
- **C#** / **.NET 9.0**: Core implementation
- **DBCD**: DBC file parsing & writing
- **mpqcli**: MPQ file manipulation

## Legacy Python Version
Original Python implementation in `python/` folder for reference.
