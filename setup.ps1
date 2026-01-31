#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Downloads and sets up DBC-Localizer dependencies

.DESCRIPTION
    This script sets up the required dependencies:
    - dbcd-lib: DBCD library and WoW definitions
    - tools: External tools like mpqcli

.EXAMPLE
    .\setup.ps1
    .\setup.ps1 -Verbose
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Stop"
$rootPath = Split-Path -Parent $MyInvocation.MyCommandPath

Write-Host "=== DBC-Localizer Setup ===" -ForegroundColor Cyan
Write-Host "Root path: $rootPath`n" -ForegroundColor Gray

# Function to clone or update a git repository
function Ensure-GitRepo {
    param(
        [string]$RepoUrl,
        [string]$LocalPath,
        [string]$Branch = "main",
        [string]$Description
    )
    
    Write-Host "Setting up $Description..." -ForegroundColor Yellow
    
    if (Test-Path $LocalPath) {
        Write-Host "  ✓ Already exists at: $LocalPath" -ForegroundColor Green
        
        # Update if it's a git repo
        if (Test-Path "$LocalPath\.git") {
            Write-Host "  Updating..." -ForegroundColor Gray
            Push-Location $LocalPath
            git pull origin $Branch --quiet 2>$null
            Pop-Location
            Write-Host "  ✓ Updated" -ForegroundColor Green
        }
    }
    else {
        Write-Host "  Cloning from: $RepoUrl" -ForegroundColor Gray
        git clone --branch $Branch --quiet $RepoUrl $LocalPath
        Write-Host "  ✓ Downloaded" -ForegroundColor Green
    }
}

# Function to download a file
function Ensure-File {
    param(
        [string]$Url,
        [string]$LocalPath,
        [string]$Description
    )
    
    Write-Host "Setting up $Description..." -ForegroundColor Yellow
    
    if (Test-Path $LocalPath) {
        Write-Host "  ✓ Already exists at: $LocalPath" -ForegroundColor Green
    }
    else {
        $parentDir = Split-Path -Parent $LocalPath
        if (-not (Test-Path $parentDir)) {
            New-Item -ItemType Directory -Path $parentDir -Force | Out-Null
        }
        
        Write-Host "  Downloading from: $Url" -ForegroundColor Gray
        Invoke-WebRequest -Uri $Url -OutFile $LocalPath -ErrorAction Stop | Out-Null
        Write-Host "  ✓ Downloaded" -ForegroundColor Green
    }
}

# ============================================================================
# Setup Dependencies
# ============================================================================

try {
    # DBCD Library - Clone from GitHub
    # Note: Update this URL to point to your actual DBCD repository
    # For now, we assume it's already in the workspace or needs to be cloned
    $dbcdPath = Join-Path $rootPath "dbcd-lib"
    
    if (-not (Test-Path $dbcdPath)) {
        Write-Host "Note: dbcd-lib not found." -ForegroundColor Yellow
        Write-Host "Please ensure it exists at: $dbcdPath" -ForegroundColor Yellow
        Write-Host "  This should contain: DBCD/, DBCD.IO/, definitions/" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host "✓ dbcd-lib found at: $dbcdPath" -ForegroundColor Green
    }
    
    # Tools directory
    $toolsPath = Join-Path $rootPath "tools"
    
    if (-not (Test-Path $toolsPath)) {
        Write-Host "`nNote: tools/ directory not found." -ForegroundColor Yellow
        Write-Host "Please ensure it exists at: $toolsPath" -ForegroundColor Yellow
        Write-Host "  This should contain: mpqcli.exe and other tools" -ForegroundColor Gray
        Write-Host ""
    }
    else {
        Write-Host "`n✓ tools/ found at: $toolsPath" -ForegroundColor Green
        
        $mpqcli = Join-Path $toolsPath "mpqcli.exe"
        if (Test-Path $mpqcli) {
            Write-Host "  ✓ mpqcli.exe ready" -ForegroundColor Green
        }
    }
    
    # ========================================================================
    # Verify Structure
    # ========================================================================
    
    Write-Host "`n=== Verifying Project Structure ===" -ForegroundColor Cyan
    
    $requiredDirs = @(
        "dbc-merger",
        "input",
        "output",
        "dbcd-lib",
        "tools"
    )
    
    foreach ($dir in $requiredDirs) {
        $path = Join-Path $rootPath $dir
        if (Test-Path $path) {
            Write-Host "  ✓ $dir" -ForegroundColor Green
        }
        else {
            Write-Host "  ✗ $dir (MISSING)" -ForegroundColor Red
        }
    }
    
    # ========================================================================
    # Build Verification
    # ========================================================================
    
    Write-Host "`n=== Checking Build Requirements ===" -ForegroundColor Cyan
    
    # Check .NET SDK
    try {
        $dotnetVersion = dotnet --version
        Write-Host "  ✓ .NET SDK $dotnetVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ .NET SDK not found" -ForegroundColor Red
        Write-Host "    Install from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    }
    
    # Check git
    try {
        $gitVersion = git --version
        Write-Host "  ✓ $gitVersion" -ForegroundColor Green
    }
    catch {
        Write-Host "  ✗ Git not found" -ForegroundColor Red
        Write-Host "    Install from: https://git-scm.com/download" -ForegroundColor Yellow
    }
    
    # ========================================================================
    # Ready to Build
    # ========================================================================
    
    Write-Host "`n=== Setup Complete ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Build:  cd dbc-merger && dotnet build -c Release" -ForegroundColor White
    Write-Host "  2. Run:    dotnet run -- --help" -ForegroundColor White
    Write-Host "  3. Config: Edit config.json with your paths" -ForegroundColor White
    Write-Host ""
    
}
catch {
    Write-Host "`n✗ Setup failed: $_" -ForegroundColor Red
    exit 1
}
