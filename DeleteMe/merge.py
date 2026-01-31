#!/usr/bin/env python3
"""
DBC-Localizer: Merge German texts from locale-deDE.MPQ into patch-B.mpq

Usage:
    python merge.py

Requires:
    - patch-B.mpq in project root
    - locale-deDE.MPQ in project root
    - WotLK.xml in project root
    - venv activated
"""

import sys
import os
import subprocess

# Add src directory to path
PROJECT_ROOT = os.path.dirname(os.path.abspath(__file__))
SRC_DIR = os.path.join(PROJECT_ROOT, "src")
sys.path.insert(0, SRC_DIR)

if __name__ == "__main__":
    print("DBC-Localizer - Merging German texts...")
    print(f"Project root: {PROJECT_ROOT}")
    
    # Run the merge script
    merge_script = os.path.join(SRC_DIR, "run_merge.py")
    
    if not os.path.exists(merge_script):
        print(f"❌ Error: {merge_script} not found!")
        sys.exit(1)
    
    # Execute run_merge.py
    result = subprocess.run(
        [sys.executable, merge_script],
        cwd=PROJECT_ROOT,
        capture_output=False
    )
    
    sys.exit(result.returncode)
