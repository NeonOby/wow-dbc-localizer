#!/bin/bash
#
# DBC-Localizer Setup Script
# Downloads and sets up required dependencies
#
# Usage: ./setup.sh

set -e

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== DBC-Localizer Setup ==="
echo "Root path: $ROOT_DIR"
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Function to ensure git repo
ensure_git_repo() {
    local url=$1
    local path=$2
    local branch=${3:-main}
    local desc=$4
    
    echo -e "${YELLOW}Setting up $desc...${NC}"
    
    if [ -d "$path" ]; then
        echo -e "  ${GREEN}✓${NC} Already exists at: $path"
        
        if [ -d "$path/.git" ]; then
            echo -e "  ${YELLOW}Updating...${NC}"
            cd "$path"
            git pull origin "$branch" --quiet 2>/dev/null || true
            cd - > /dev/null
            echo -e "  ${GREEN}✓${NC} Updated"
        fi
    else
        echo -e "  ${YELLOW}Cloning from: $url${NC}"
        git clone --branch "$branch" --quiet "$url" "$path"
        echo -e "  ${GREEN}✓${NC} Downloaded"
    fi
}

# Function to ensure directory exists
ensure_directory() {
    local path=$1
    local desc=$2
    
    echo -e "${YELLOW}Checking $desc...${NC}"
    
    if [ -d "$path" ]; then
        echo -e "  ${GREEN}✓${NC} Found at: $path"
    else
        echo -e "  ${YELLOW}✗${NC} Missing: $path"
    fi
}

# ==========================================================================
# Setup Dependencies
# ==========================================================================

# DBCD Library - Clone from GitHub
DBCD_PATH="$ROOT_DIR/dbcd-lib"
ensure_git_repo \
    "https://github.com/wowdev/DBCD.git" \
    "$DBCD_PATH" \
    "master" \
    "DBCD Library (wowdev/DBCD)"

echo ""

# ==========================================================================
# Verify Project Structure
# ==========================================================================

echo -e "${CYAN}=== Verifying Project Structure ===${NC}"
echo ""

require_dirs=(
    "dbc-merger"
    "input"
    "output"
    "dbcd-lib"
    "tools"
)

missing=0
for dir in "${require_dirs[@]}"; do
    path="$ROOT_DIR/$dir"
    if [ -d "$path" ]; then
        echo -e "  ${GREEN}✓${NC} $dir"
    else
        echo -e "  ${RED}✗${NC} $dir (MISSING)"
        missing=$((missing + 1))
    fi
done

if [ $missing -gt 0 ]; then
    echo ""
    echo -e "${YELLOW}Note: Some directories are missing:${NC}"
    echo -e "  dbcd-lib should contain: DBCD/, DBCD.IO/, definitions/"
    echo -e "  tools should contain: mpqcli, other tools"
fi

# ==========================================================================
# Check Build Requirements
# ==========================================================================

echo ""
echo -e "${CYAN}=== Checking Build Requirements ===${NC}"

# Check .NET SDK
if command -v dotnet &> /dev/null; then
    dotnet_version=$(dotnet --version)
    echo -e "  ${GREEN}✓${NC} .NET SDK $dotnet_version"
else
    echo -e "  ${RED}✗${NC} .NET SDK not found"
    echo -e "    ${YELLOW}Install from: https://dotnet.microsoft.com/download${NC}"
fi

# Check git
if command -v git &> /dev/null; then
    git_version=$(git --version)
    echo -e "  ${GREEN}✓${NC} $git_version"
else
    echo -e "  ${RED}✗${NC} Git not found"
    echo -e "    ${YELLOW}Install from: https://git-scm.com/download${NC}"
fi

# ==========================================================================
# Ready to Build
# ==========================================================================

echo ""
echo -e "${GREEN}=== Setup Complete ===${NC}"
echo ""
echo -e "${CYAN}Next steps:${NC}"
echo -e "  ${GREEN}1.${NC} Build:  cd dbc-merger && dotnet build -c Release"
echo -e "  ${GREEN}2.${NC} Run:    dotnet run -- --help"
echo -e "  ${GREEN}3.${NC} Config: Edit config.json with your paths"
echo ""
