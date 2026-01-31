#!/usr/bin/env python3
"""
Finalize MPQ with merged Spell.dbc using mpqcli
"""

import os
import subprocess
import shutil

# Constants
MPQCLI_EXE = r"D:\Spiele\WOW\Editor\DBC-Localizer\mpqcli.exe"
PATCH_MPQ = r"D:\Spiele\WOW\Editor\DBC-Localizer\patch-B.mpq"
MERGED_DBC = r"D:\Spiele\WOW\Editor\DBC-Localizer\extract\merged\DBFilesClient\Spell.dbc"
OUTPUT_MPQ = r"D:\Spiele\WOW\Editor\DBC-Localizer\Patch-B-merged.mpq"
DBC_PATH_IN_MPQ = "DBFilesClient\\Spell.dbc"

print("=" * 70)
print("Finalizing MPQ with merged Spell.dbc (using mpqcli)")
print("=" * 70)
print()

# Step 1: Check if merged DBC exists
if not os.path.exists(MERGED_DBC):
    print(f"ERROR: Merged DBC not found: {MERGED_DBC}")
    print("Please run merge_mpq_complete.py first")
    exit(1)

merged_size = os.path.getsize(MERGED_DBC)
print(f"✓ Merged DBC found: {MERGED_DBC}")
print(f"  Size: {merged_size:,} bytes")
print()

# Check if mpqcli is available
if not os.path.exists(MPQCLI_EXE):
    print(f"ERROR: mpqcli not found: {MPQCLI_EXE}")
    print("Please download it from: https://github.com/TheGrayDot/mpqcli/releases")
    exit(1)

print(f"✓ mpqcli found: {MPQCLI_EXE}")
print()

# Step 2: Create a working copy of Patch-B.mpq
print(f"Creating working copy of Patch-B.mpq...")
if os.path.exists(OUTPUT_MPQ):
    backup_mpq = OUTPUT_MPQ + ".backup"
    if os.path.exists(backup_mpq):
        os.remove(backup_mpq)
    os.rename(OUTPUT_MPQ, backup_mpq)
    print(f"  Backed up existing: {backup_mpq}")

shutil.copy(PATCH_MPQ, OUTPUT_MPQ)
original_size = os.path.getsize(OUTPUT_MPQ)
print(f"✓ Working copy created: {OUTPUT_MPQ}")
print(f"  Size: {original_size:,} bytes")
print()

# Step 3: Remove old Spell.dbc from the MPQ
print(f"Step 1/2: Removing old Spell.dbc from MPQ...")
result = subprocess.run(
    [MPQCLI_EXE, "remove", DBC_PATH_IN_MPQ, OUTPUT_MPQ],
    capture_output=True,
    text=True
)

if result.returncode == 0:
    print(f"  ✓ Old file removed")
    if result.stdout:
        print(f"    {result.stdout.strip()}")
else:
    print(f"  ⚠ Remove warning: {result.stderr.strip() if result.stderr else 'File may not have existed'}")

print()

# Step 4: Add the merged Spell.dbc to the MPQ
print(f"Step 2/2: Adding merged Spell.dbc to MPQ...")
result = subprocess.run(
    [MPQCLI_EXE, "add", MERGED_DBC, OUTPUT_MPQ, "--path", "DBFilesClient"],
    capture_output=True,
    text=True
)

if result.returncode != 0:
    print(f"✗ Failed to add file: {result.stderr}")
    exit(1)

print(f"  ✓ File added successfully")
if result.stdout:
    print(f"    {result.stdout.strip()}")

print()

# Step 5: Verify the output
new_size = os.path.getsize(OUTPUT_MPQ)
size_diff = new_size - original_size

print("=" * 70)
print("Verification")
print("=" * 70)
print(f"Original size: {original_size:,} bytes ({original_size / 1024 / 1024:.2f} MB)")
print(f"New size:      {new_size:,} bytes ({new_size / 1024 / 1024:.2f} MB)")
print(f"Difference:    {size_diff:+,} bytes ({size_diff / original_size * 100:+.2f}%)")
print()

if size_diff != 0:
    print("✓ SUCCESS! MPQ was modified")
    print()
    print(f"Final MPQ: {OUTPUT_MPQ}")
    print(f"Size: {new_size:,} bytes")
    print()
    print("Next steps:")
    print("1. Copy Patch-B-merged.mpq to your WoW installation")
    print("2. Rename it to Patch-B.mpq (backup original first!)")
    print("3. Test in WoW 3.3.5")
    print()
else:
    print("⚠ Size unchanged - verification needed")
    
# Verify file in archive
print("Verifying file in archive...")
result = subprocess.run(
    [MPQCLI_EXE, "list", OUTPUT_MPQ],
    capture_output=True,
    text=True
)

if DBC_PATH_IN_MPQ.replace("\\", "/").lower() in result.stdout.lower() or \
   DBC_PATH_IN_MPQ.lower() in result.stdout.lower() or \
   "spell.dbc" in result.stdout.lower():
    print("✓ Spell.dbc found in archive")
else:
    print("⚠ Could not verify Spell.dbc in listing")
    
print()
