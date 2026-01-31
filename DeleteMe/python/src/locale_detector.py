#!/usr/bin/env python3
"""
Locale Detector - Extract locale code from MPQ filename

Examples:
    'locale-deDE.MPQ' -> 'deDE'
    'locale-frFR.mpq' -> 'frFR'
    'patch-enUS.mpq' -> 'enUS'
"""

import re
from pathlib import Path
from typing import Optional, Dict

# Mapping of common locale codes
LOCALE_CODES = {
    'enUS': 'English (USA)',
    'dede': 'German',
    'frfr': 'French',
    'ruru': 'Russian',
    'eses': 'Spanish',
    'zhtw': 'Chinese (Traditional)',
    'koko': 'Korean',
    'esMX': 'Spanish (Mexico)',
    'ptBR': 'Portuguese (Brazil)',
    'itIT': 'Italian',
    'plPL': 'Polish',
}


def detect_locale(filename: str) -> Optional[str]:
    """
    Extract locale code from MPQ filename.
    
    Supports patterns:
    - locale-deDE.MPQ -> deDE
    - patch-deDE.mpq -> deDE
    - deDE.mpq -> deDE
    - Any-deDE-suffix.mpq -> deDE
    
    Returns:
        Locale code (e.g. 'deDE') or None if not recognized
    """
    filename = filename.upper()
    
    # Pattern 1: locale-XXXX or patch-XXXX or similar-XXXX
    match = re.search(r'[A-Za-z]+-([A-Za-z]{2}[A-Za-z]{2})', filename)
    if match:
        return match.group(1).lower()
    
    # Pattern 2: Direct locale code without prefix
    match = re.search(r'([a-z]{2}[a-z]{2})', filename, re.IGNORECASE)
    if match:
        locale = match.group(1).lower()
        # Verify it's a known locale
        if locale in [k.lower() for k in LOCALE_CODES.keys()]:
            return locale
    
    return None


def get_locale_name(locale_code: str) -> str:
    """Get human-readable name for locale code."""
    return LOCALE_CODES.get(locale_code, 'Unknown')


def find_mpqs_with_locales(directory: Path) -> Dict[str, Path]:
    """
    Find all locale MPQs in directory and return mapping: locale_code -> file_path
    
    Args:
        directory: Path to search for *.mpq files
        
    Returns:
        Dict like {'deDE': Path('locale-deDE.MPQ'), 'frFR': Path('locale-frFR.MPQ')}
    """
    directory = Path(directory)
    if not directory.exists():
        return {}
    
    result = {}
    for mpq_file in directory.glob('*.mpq') | directory.glob('*.MPQ'):
        locale = detect_locale(mpq_file.name)
        if locale:
            # Only include locale files (not patch files without locale)
            if 'locale' in mpq_file.name.lower() or locale != 'enus':
                result[locale] = mpq_file
                print(f"[*] Found {locale.upper()}: {mpq_file.name}")
    
    return result


if __name__ == '__main__':
    # Test examples
    test_files = [
        'locale-deDE.MPQ',
        'locale-frFR.mpq',
        'locale-ruRU.mpq',
        'patch-B.mpq',
        'Patch-enUS.mpq',
        'deDE_data.mpq',
        'unknown.mpq'
    ]
    
    print("Testing locale detection:\n")
    for filename in test_files:
        locale = detect_locale(filename)
        print(f"  {filename:25} -> {locale or 'Not detected'}")
