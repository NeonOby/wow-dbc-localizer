# Setup Instructions

This document describes how to set up DBC-Localizer for development or use.

## Quick Start

### Windows (PowerShell)
```powershell
.\setup.ps1
cd dbc-merger
dotnet build -c Release
```

### Linux / macOS (Bash)
```bash
chmod +x setup.sh
./setup.sh
cd dbc-merger
dotnet build -c Release
```

## Dependencies

DBC-Localizer requires two main dependencies:

### 1. dbcd-lib (DBCD Library)

This contains the core DBC reading/writing libraries and WoW 3.3.5 definitions.

**Location:** `./dbcd-lib/`

**Contents:**
- `DBCD/` - DBC file format reader/writer
- `DBCD.IO/` - Binary I/O operations
- `definitions/` - WoW 3.3.5 DBD field definitions

**Current Status:** Currently included in the repository. In the future, this should be:
- Hosted as a separate GitHub repository
- Added as a git submodule, or
- Downloaded during setup

### 2. tools/ (External Tools)

External utilities needed for MPQ manipulation.

**Location:** `./tools/`

**Contents:**
- `mpqcli.exe` - Command-line MPQ manipulation tool (Windows)
- `mpqcli` - Command-line MPQ manipulation tool (Linux/macOS)

**Current Status:** Currently included in the repository. In the future:
- Could be downloaded from a releases page
- Could be compiled as part of the build process
- Could be downloaded from a separate tools repository

## Manual Setup (If Automated Setup Fails)

### Prerequisites
- **.NET 9.0 SDK** - https://dotnet.microsoft.com/download
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
├── tools/                   (External tools - must exist)
│   └── mpqcli.exe          (Windows) or mpqcli (Linux/macOS)
├── input/                   (Your input MPQ files)
├── output/                  (Generated merged MPQ files)
└── setup.ps1               (Windows setup script)
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
The DBCD library is required for building. Ensure:
1. The `dbcd-lib` directory exists
2. It contains `DBCD.csproj` and `DBCD.IO.csproj`

**Solution:**
- Clone https://github.com/NeonOby/dbcd-lib into the `dbcd-lib` directory, or
- Ensure you have the correct workspace structure

### ".NET SDK not found"
Install .NET 9.0 or higher from https://dotnet.microsoft.com/download

Verify installation:
```bash
dotnet --version
```

### "mpqcli.exe not found"
The tools directory must contain the MPQ manipulation tools.

**Solution:**
- Ensure `tools/mpqcli.exe` exists (Windows) or `tools/mpqcli` (Linux/macOS)
- Download from the releases page or clone the tools repository

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

### Option 2: Automated Download
The setup script could automatically download dependencies:
```powershell
# Download dbcd-lib
Invoke-WebRequest -Uri "https://github.com/NeonOby/dbcd-lib/releases/download/latest/dbcd-lib.zip" -OutFile "dbcd-lib.zip"
Expand-Archive -Path "dbcd-lib.zip" -DestinationPath "."
```

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
