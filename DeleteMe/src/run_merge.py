#!/usr/bin/env python3
"""
One-step merger: Merges German Spell.dbc and creates final MPQ
Uses mpqcli for automated MPQ modification
"""

import os
import subprocess
import sys

# Get paths
SRC_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SRC_DIR)

def run_command(script_name):
    """Run a Python script and return success status"""
    script_path = os.path.join(SRC_DIR, script_name)
    print(f"\n{'=' * 70}")
    print(f"Running: {script_name}")
    print('=' * 70)
    
    # Run from project root so relative paths work
    result = subprocess.run(
        [sys.executable, script_path],
        cwd=PROJECT_ROOT
    )
    
    return result.returncode == 0

def main():
    print("\n" + "=" * 70)
    print("WoW Spell.dbc Merger - Complete Pipeline (with mpqcli)")
    print("=" * 70)
    
    scripts = [
        ("merge_mpq_complete.py", "DBC Merging"),
        ("finalize_mpq.py", "MPQ Finalization"),
    ]
    
    for script, description in scripts:
        print(f"\n▶ {description}...")
        if not run_command(script):
            print(f"\n✗ FAILED at: {description}")
            return False
    
    # Verify output
    output_mpq = os.path.join(PROJECT_ROOT, "output", "Patch-B-merged.mpq")
    if os.path.exists(output_mpq):
        size = os.path.getsize(output_mpq)
        print("\n" + "=" * 70)
        print("✓ SUCCESS - Complete Merger Finished!")
        print("=" * 70)
        print(f"\nOutput: {output_mpq}")
        print(f"Size: {size:,} bytes")
        print("\nNext Steps:")
        print("1. Backup your original Patch-B.mpq")
        print("2. Copy output/Patch-B-merged.mpq to your WoW Data folder as Patch-B.mpq")
        print("3. Start WoW 3.3.5 and test")
        print("\nThe German Spell descriptions should now be visible in-game!")
        print()
        return True
    else:
        print("\n✗ ERROR: Output MPQ not created!")
        return False

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
