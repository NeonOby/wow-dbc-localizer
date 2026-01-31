# Setup Instructions

This document describes how to set up DBC-Localizer for development or use.

## Quick Start

### All Platforms
```bash
cd dbc-merger
dotnet build -c Release
```

The build will automatically download:
- **dbcd-lib** from https://github.com/wowdev/DBCD
- **mpqcli** from https://github.com/TheGrayDot/mpqcli/releases

## Dependencies

DBC-Localizer requires two main dependencies:

### 1. dbcd-lib (DBCD Library)

This contains the core DBC reading/writing libraries and WoW 3.3.5 definitions.

**Location:** `./dbcd-lib/`

**Contents:**
- `DBCD/` - DBC file format reader/writer
- `DBCD.IO/` - Binary I/O operations
- `definitions/` - WoW 3.3.5 DBD field definitions

**Current Status:** Automatically downloaded during build from GitHub.

### 2. tools/ (External Tools)

External utilities needed for MPQ manipulation.

**Location:** `./tools/`

**Contents:**
- `mpqcli.exe` - Command-line MPQ manipulation tool (Windows)
- `mpqcli` - Command-line MPQ manipulation tool (Linux/macOS)

**Current Status:** Automatically downloaded during build from GitHub releases.

## Manual Setup (If Automated Setup Fails)

### Prerequisites
- **.NET 10.0 SDK** - https://dotnet.microsoft.com/download
- **Git** - https://git-scm.com/download
- **Visual Studio Code** or **Visual Studio 2024** (optional)

### Step 1: Clone the Repository
```bash
git clone https://github.com/NeonOby/dbc-localizer.git
cd dbc-localizer
```

### Step 2: Verify Directory Structure
Ensure these directories exist:

```
dbc-localizer/
├── dbc-merger/              (C# project source)
├── dbcd-lib/                (DBCD library - must exist)
│   ├── DBCD/
│   ├── DBCD.IO/
│   └── definitions/
├── tools/                   (External tools - auto-downloaded)
│   └── mpqcli.exe          (Windows) or mpqcli (Linux)
├── input/                   (Your input MPQ files)
├── output/                  (Generated merged MPQ files)
└── README.md                (Documentation)
```

### Step 3: Build the Project
```bash
cd dbc-merger
dotnet build -c Release
```

**Output:** `bin/Release/net9.0/dbc-merger.exe`

### Step 4: Run Tests
```bash
dotnet run -- --help
dotnet run -- scan-mpq --patch <path> --locale-mpq <path> --defs ../dbcd-lib/definitions/definitions
```

## Troubleshooting

### "dbcd-lib not found"
The build will download DBCD automatically. If it fails:
1. Ensure Git is installed
2. Run `dotnet build dbc-merger -c Release` again
3. Or manually clone https://github.com/wowdev/DBCD into `dbcd-lib`

### ".NET SDK not found"
Install .NET 10.0 or higher from https://dotnet.microsoft.com/download

Verify installation:
```bash
dotnet --version
```

### "mpqcli.exe not found"
The build will download mpqcli automatically. If it fails:
1. Ensure you have network access
2. Download from https://github.com/TheGrayDot/mpqcli/releases
3. Place it in `tools/mpqcli.exe` (Windows) or `tools/mpqcli` (Linux)

## Future: Dependency Management Strategy

### Option 1: Git Submodules (Recommended)
```bash
git submodule add https://github.com/NeonOby/dbcd-lib.git dbcd-lib
git submodule add https://github.com/NeonOby/dbc-tools.git tools
```

Users would then clone with:
```bash
git clone --recursive https://github.com/NeonOby/dbc-localizer.git
```

### Option 2: Automated Download (Current)
Dependencies are downloaded automatically during `dotnet build` via MSBuild targets.

### Option 3: NuGet Package
Package DBCD libraries as NuGet packages and reference them in the `.csproj` file.

## Environment Variables

Optional environment variables that can be set:

- `DBCD_DEFINITIONS` - Path to DBCD definitions directory
- `TOOLS_PATH` - Path to external tools directory

## Next Steps

After setup, see [README.md](README.md) for usage instructions.

## Questions?

See [CONTRIBUTING.md](CONTRIBUTING.md) for development guidelines.
