#!/usr/bin/env python3
"""
Complete MPQ to MPQ merger with XML-based DBC writer
Merges German texts from locale-deDE.MPQ into Patch-B.mpq
"""

import os
import struct
import shutil
import subprocess
import xml.etree.ElementTree as ET
from io import BytesIO

# Get project root
PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Configuration (relative paths)
MPQCLI_EXE = os.path.join(PROJECT_ROOT, "tools", "mpqcli.exe")
patch_mpq_path = os.path.join(PROJECT_ROOT, "input", "patch", "patch-B.mpq")
locale_mpq_path = os.path.join(PROJECT_ROOT, "input", "locale", "locale-deDE.MPQ")
dbc_path = "DBFilesClient\\Spell.dbc"
xml_definition = os.path.join(PROJECT_ROOT, "WotLK 3.3.5 (12340).xml")
output_dbc_dir = os.path.join(PROJECT_ROOT, "output", "extract", "merged", "DBFilesClient")
output_dbc_path = os.path.join(output_dbc_dir, "Spell.dbc")
output_mpq_path = os.path.join(PROJECT_ROOT, "output", "Patch-B-merged.mpq")

# Extraction directories
patch_extract_dir = os.path.join(PROJECT_ROOT, "output", "extract", "patch")
locale_extract_dir = os.path.join(PROJECT_ROOT, "output", "extract", "locale")

def parse_wdbx_definition(xml_path, table_name, build):
    """Parse WDBX XML and return field definitions"""
    tree = ET.parse(xml_path)
    root = tree.getroot()
    
    for table in root.findall(f".//Table[@Name='{table_name}'][@Build='{build}']"):
        fields = []
        for field_elem in table.findall("Field"):
            fields.append({
                'name': field_elem.get("Name"),
                'type': field_elem.get("Type"),
                'array_size': int(field_elem.get("ArraySize", "1")),
                'is_index': field_elem.get("IsIndex") == "true"
            })
        return fields
    
            os.makedirs(output_dbc_dir, exist_ok=True)
            output_file = os.path.join(output_dbc_dir, "Spell.dbc")
            print(f"      Output directory: {output_dbc_dir}")
    raise ValueError(f"Table {table_name} with build {build} not found in {xml_path}")

def get_field_size(field_type):
    """Get the size in bytes for a field type"""
    sizes = {
        'int': 4, 'uint': 4, 'float': 4,
        'byte': 1, 'ulong': 8,
        'string': 4,  # Offset
        'loc': 68,    # 16 languages * 4 bytes + 4 flags = 68 bytes
    }
    return sizes.get(field_type, 4)

def read_dbc_header(data):
    """Read DBC header"""
    magic, records, fields, record_size, string_block_size = struct.unpack('<4sIIII', data[:20])
    return {
        'magic': magic,
        'records': records,
        'fields': fields,
        'record_size': record_size,
        'string_block_size': string_block_size
    }

def read_string(data, str_block_start, offset):
    """Read a null-terminated string from the string block"""
    if offset == 0:
        return ""
    pos = str_block_start + offset
    end = pos
    while end < len(data) and data[end] != 0:
        end += 1
    return data[pos:end].decode('utf-8', errors='ignore')

def read_locstring(record_data, pos, full_data, str_block_start):
    """Read a localized string (16 language offsets + flags)"""
    offsets = struct.unpack('<16I', record_data[pos:pos+64])
    flags = struct.unpack('<I', record_data[pos+64:pos+68])[0]
    
    strings = {}
    lang_names = ['enUS', 'koKR', 'frFR', 'deDE', 'enCN', 'enTW', 'esES', 'esMX',
                  'ruRU', 'jaJP', 'ptPT', 'itIT', 'unknown_12', 'unknown_13', 'unknown_14', 'unknown_15']
    
    for i, lang in enumerate(lang_names):
        strings[lang] = read_string(full_data, str_block_start, offsets[i])
    
    strings['flags'] = flags
    return strings

def write_string(string, string_block, string_map):
    """Add string to string block and return offset"""
    if string not in string_map:
        string_map[string] = len(string_block)
        string_block.extend((string + '\0').encode('utf-8'))
    return string_map[string]

def write_locstring(locstrings, string_block, string_map):
    """Write localized string offsets"""
    lang_names = ['enUS', 'koKR', 'frFR', 'deDE', 'enCN', 'enTW', 'esES', 'esMX',
                  'ruRU', 'jaJP', 'ptPT', 'itIT', 'unknown_12', 'unknown_13', 'unknown_14', 'unknown_15']
    
    offsets = []
    for lang in lang_names:
        offset = write_string(locstrings.get(lang, ''), string_block, string_map)
        offsets.append(offset)
    
    return offsets, locstrings.get('flags', 0)

def extract_dbc(mpq_path, output_dir, dbc_rel_path):
    """Extract a single DBC file from MPQ using mpqcli."""
    if not os.path.exists(MPQCLI_EXE):
        raise FileNotFoundError(f"mpqcli not found: {MPQCLI_EXE}")
    if not os.path.exists(mpq_path):
        raise FileNotFoundError(f"MPQ not found: {mpq_path}")

    os.makedirs(output_dir, exist_ok=True)

    result = subprocess.run(
        [MPQCLI_EXE, "extract", mpq_path, "--file", dbc_rel_path, "--output", output_dir, "--keep"],
        capture_output=True,
        text=True
    )

    if result.returncode != 0:
        raise RuntimeError(f"mpqcli extract failed: {result.stderr.strip()}")

    extracted_path = os.path.join(output_dir, dbc_rel_path)
    if not os.path.exists(extracted_path):
        raise FileNotFoundError(f"Extracted DBC not found: {extracted_path}")

    return extracted_path

print("=" * 70)
print("COMPLETE MPQ TO MPQ MERGER")
print("=" * 70)

# Validate input MPQs (auto-copy from root if present)
root_patch = os.path.join(PROJECT_ROOT, "patch-B.mpq")
root_locale = os.path.join(PROJECT_ROOT, "locale-deDE.MPQ")

os.makedirs(os.path.dirname(patch_mpq_path), exist_ok=True)
os.makedirs(os.path.dirname(locale_mpq_path), exist_ok=True)

if not os.path.exists(patch_mpq_path) and os.path.exists(root_patch):
    shutil.copy(root_patch, patch_mpq_path)
if not os.path.exists(locale_mpq_path) and os.path.exists(root_locale):
    shutil.copy(root_locale, locale_mpq_path)

if not os.path.exists(patch_mpq_path):
    raise FileNotFoundError(f"Patch MPQ not found: {patch_mpq_path}")
if not os.path.exists(locale_mpq_path):
    raise FileNotFoundError(f"Locale MPQ not found: {locale_mpq_path}")

# Step 1: Parse WDBX definition
print("\n[1/7] Parsing WDBX XML definition...")
fields = parse_wdbx_definition(xml_definition, "Spell", "12340")
print(f"      Found {len(fields)} field definitions")

total_size = sum(get_field_size(f['type']) * f['array_size'] for f in fields)
print(f"      Expected record size: {total_size} bytes")

# Step 2: Load both DBCs
print("\n[2/7] Loading DBCs from MPQs (via mpqcli)...")

patch_dbc_file = extract_dbc(patch_mpq_path, patch_extract_dir, dbc_path)
locale_dbc_file = extract_dbc(locale_mpq_path, locale_extract_dir, dbc_path)

with open(patch_dbc_file, "rb") as f:
    patch_data = f.read()

with open(locale_dbc_file, "rb") as f:
    locale_data = f.read()

patch_header = read_dbc_header(patch_data)
locale_header = read_dbc_header(locale_data)

print(f"      Patch: {patch_header['records']} records, {patch_header['record_size']} bytes/record")
print(f"      Locale: {locale_header['records']} records, {locale_header['record_size']} bytes/record")

# Step 3: Parse locale DBC to extract German strings
print("\n[3/7] Extracting German strings from locale...")

locstring_count = sum(1 for f in fields if f['type'] == 'loc')
non_locstring_field_count = sum(f['array_size'] for f in fields if f['type'] != 'loc')
locstring_width = (locale_header['fields'] - non_locstring_field_count) // locstring_count
deDE_index = 3

print(f"      Locstring width: {locstring_width} languages")

locale_str_block_start = 20 + locale_header['records'] * locale_header['record_size']
locale_str_block = locale_data[locale_str_block_start:]

locale_records = {}

for rec_idx in range(locale_header['records']):
    rec_offset = 20 + rec_idx * locale_header['record_size']
    rec_data = locale_data[rec_offset:rec_offset+locale_header['record_size']]
    
    pos = 0
    de_strings = []
    spell_id = None
    
    for field in fields:
        field_type = field['type']
        array_size = field['array_size']
        
        if field['is_index'] and spell_id is None:
            spell_id = struct.unpack('<i', rec_data[pos:pos+4])[0]
        
        if field_type == 'loc':
            if pos + (locstring_width * 4) <= len(rec_data):
                offsets = struct.unpack(f'<{locstring_width}I', rec_data[pos:pos + locstring_width * 4])
                
                if deDE_index < len(offsets):
                    de_offset = offsets[deDE_index]
                    if de_offset > 0 and de_offset < len(locale_str_block):
                        de_text = read_string(locale_data, locale_str_block_start, de_offset)
                        de_strings.append(de_text)
                    else:
                        de_strings.append('')
                else:
                    de_strings.append('')
            else:
                de_strings.append('')
            
            pos += locstring_width * 4
        elif field_type == 'ulong':
            pos += 8 * array_size
        elif field_type == 'byte':
            pos += 1 * array_size
        else:
            pos += 4 * array_size
        
        if pos >= len(rec_data):
            break
    
    if spell_id is not None:
        locale_records[spell_id] = de_strings

print(f"      Extracted {len(locale_records)} spell records")

# Step 4: Parse patch DBC
print("\n[4/7] Parsing patch DBC...")
patch_str_block_start = 20 + patch_header['records'] * patch_header['record_size']

patch_records = []
for rec_idx in range(patch_header['records']):
    rec_offset = 20 + rec_idx * patch_header['record_size']
    record_data = patch_data[rec_offset:rec_offset+patch_header['record_size']]
    
    record = {}
    pos = 0
    
    for field in fields:
        field_name = field['name']
        field_type = field['type']
        array_size = field['array_size']
        
        if field_type == 'loc':
            locstrings = read_locstring(record_data, pos, patch_data, patch_str_block_start)
            record[field_name] = locstrings
            pos += 68
        elif field_type == 'ulong':
            values = []
            for _ in range(array_size):
                val = struct.unpack('<Q', record_data[pos:pos+8])[0]
                values.append(val)
                pos += 8
            record[field_name] = values if array_size > 1 else values[0]
        elif field_type == 'int':
            values = []
            for _ in range(array_size):
                val = struct.unpack('<i', record_data[pos:pos+4])[0]
                values.append(val)
                pos += 4
            record[field_name] = values if array_size > 1 else values[0]
        elif field_type == 'uint':
            values = []
            for _ in range(array_size):
                val = struct.unpack('<I', record_data[pos:pos+4])[0]
                values.append(val)
                pos += 4
            record[field_name] = values if array_size > 1 else values[0]
        elif field_type == 'float':
            values = []
            for _ in range(array_size):
                val = struct.unpack('<f', record_data[pos:pos+4])[0]
                values.append(val)
                pos += 4
            record[field_name] = values if array_size > 1 else values[0]
        elif field_type == 'byte':
            values = []
            for _ in range(array_size):
                val = record_data[pos]
                values.append(val)
                pos += 1
            record[field_name] = values if array_size > 1 else values[0]
        elif field_type == 'string':
            values = []
            for _ in range(array_size):
                offset = struct.unpack('<I', record_data[pos:pos+4])[0]
                string_val = read_string(patch_data, patch_str_block_start, offset)
                values.append(string_val)
                pos += 4
            record[field_name] = values if array_size > 1 else values[0]
    
    patch_records.append(record)

print(f"      Parsed {len(patch_records)} records")

# Step 5: Merge German strings
print("\n[5/7] Merging German strings...")
merged_count = 0

locstring_fields = [f['name'] for f in fields if f['type'] == 'loc']

for record in patch_records:
    spell_id = record['ID']
    
    if spell_id in locale_records:
        de_strings = locale_records[spell_id]
        
        for idx, field_name in enumerate(locstring_fields):
            if idx < len(de_strings) and de_strings[idx]:
                if field_name in record and isinstance(record[field_name], dict):
                    record[field_name]['deDE'] = de_strings[idx]
                    merged_count += 1

print(f"      Merged {merged_count} German text fields")

# Step 6: Write merged DBC
print("\n[6/7] Writing merged DBC...")
os.makedirs(output_dir, exist_ok=True)
output_file = os.path.join(output_dir, "Spell.dbc")

string_block = bytearray()
string_map = {}

output = bytearray()
output.extend(b'WDBC')
output.extend(struct.pack('<I', len(patch_records)))
output.extend(struct.pack('<I', patch_header['fields']))
output.extend(struct.pack('<I', patch_header['record_size']))
output.extend(struct.pack('<I', 0))  # string block size (update later)

for rec_idx, record in enumerate(patch_records):
    record_bytes = bytearray()
    
    for field in fields:
        field_name = field['name']
        field_type = field['type']
        array_size = field['array_size']
        
        if field_name not in record:
            # Use default value
            if field_type == 'loc':
                value = {'enUS': '', 'koKR': '', 'frFR': '', 'deDE': '', 'enCN': '', 'enTW': '', 
                        'esES': '', 'esMX': '', 'ruRU': '', 'jaJP': '', 'ptPT': '', 'itIT': '',
                        'unknown_12': '', 'unknown_13': '', 'unknown_14': '', 'unknown_15': '', 'flags': 0}
            elif field_type == 'string':
                value = [''] * array_size if array_size > 1 else ''
            else:
                value = [0] * array_size if array_size > 1 else 0
        else:
            value = record[field_name]
        
        if field_type == 'loc':
            offsets, flags = write_locstring(value, string_block, string_map)
            for offset in offsets:
                record_bytes.extend(struct.pack('<I', offset))
            record_bytes.extend(struct.pack('<I', flags))
        elif field_type == 'ulong':
            values = value if isinstance(value, list) else [value]
            for val in values:
                record_bytes.extend(struct.pack('<Q', val))
        elif field_type == 'int':
            values = value if isinstance(value, list) else [value]
            for val in values:
                record_bytes.extend(struct.pack('<i', val))
        elif field_type == 'uint':
            values = value if isinstance(value, list) else [value]
            for val in values:
                record_bytes.extend(struct.pack('<I', val))
        elif field_type == 'float':
            values = value if isinstance(value, list) else [value]
            for val in values:
                record_bytes.extend(struct.pack('<f', val))
        elif field_type == 'byte':
            values = value if isinstance(value, list) else [value]
            for val in values:
                record_bytes.append(val)
        elif field_type == 'string':
            values = value if isinstance(value, list) else [value]
            for string_val in values:
                offset = write_string(string_val, string_block, string_map)
                record_bytes.extend(struct.pack('<I', offset))
    
    output.extend(record_bytes)

output.extend(string_block)

string_block_size = len(string_block)
struct.pack_into('<I', output, 16, string_block_size)

with open(output_file, 'wb') as f:
    f.write(output)

print(f"      Saved: {output_file}")
print(f"      File size: {len(output)} bytes")
print(f"      String block: {string_block_size} bytes")

# Step 7: Create merged MPQ
print(f"\n[7/7] Creating merged MPQ...")

# pywowlib does NOT have write support - the Python bindings don't expose
# SFileCreateArchive, SFileAddFileEx, etc. even though StormLib C library has them.
# 
# We have three options:
# 1. Use MPQ Editor manually (recommended, easiest)
# 2. Add Python bindings for write functions to pywowlib (requires C++ compilation)
# 3. Use a different MPQ library that supports writing

print(f"      INFO: pywowlib does not expose StormLib write functions.")
print(f"      The C library (StormLib) HAS write support, but the Python")
print(f"      bindings in pywowlib only expose read functions.")
print()
print(f"      Creating base MPQ by copying original...")
shutil.copy(patch_mpq_path, output_mpq_path)
print(f"      Copied: {output_mpq_path}")
print()
print(f"      To complete the merge, use MPQ Editor:")
print(f"      ==========================================")
print(f"      1. Download MPQ Editor if you don't have it:")
print(f"         http://www.zezula.net/en/mpq/download.html")
print()
print(f"      2. Open: {output_mpq_path}")
print()
print(f"      3. Navigate to: {dbc_path}")
print()
print(f"      4. Right-click -> 'Remove' or 'Delete' the old Spell.dbc")
print()
print(f"      5. Click 'Operations' -> 'Add File(s)'")
print()
print(f"      6. Select: {os.path.abspath(output_file)}")
print()
print(f"      7. Set archived name to: {dbc_path}")
print()
print(f"      8. Click 'OK' and save the MPQ")
print(f"      ==========================================")
print()
print(f"      Alternative: Use command-line MPQ tools like MPQEditor.exe")
print(f"      or StormLib's standalone utilities if available.")

print("\n" + "=" * 70)
print("MERGE COMPLETE!")
print("=" * 70)
print(f"  German texts merged: {merged_count}")
print(f"  Output DBC: {output_file}")
print(f"  Output MPQ: {output_mpq_path}")
print("\nNext steps:")
print("  1. Test the DBC in WDBX Editor")
print("  2. Manually add the DBC to the MPQ using MPQ Editor")
print("  3. Test the MPQ in WoW 3.3.5a")
