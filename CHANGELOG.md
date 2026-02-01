# Changelog

All notable changes to DBC-Localizer will be documented in this file.

## [2.0.0] - 2025-01-XX

### 🔴 CRITICAL BUG FIXES

#### Row Index vs ID Column Bug
**Impact**: All text assignments were incorrect/offset in previous versions.

Previously, the localization engine used row indices (1, 2, 3...) instead of actual record IDs from the DBC files. Since WoW DBCs have sparse ID spaces (e.g., Spell.dbc has 51,275 rows but IDs range from 1 to 4,201,098), this caused all localized text to be assigned to the wrong records.

**Example**: A custom spell with ID 81423 at row index 50000 would have text from spell ID 50000 instead.

**Fixed**: Changed from `storage.ToDictionary()` keys (row indices) to reading the actual ID column via `record["ID"]`.

**Files Changed**: LocalizeEngine.cs, VerifyDbcCommandHandler.cs

#### MPQ Path Duplication Bug
**Impact**: DBC files were added to incorrect paths in output MPQ archives.

The MPQ CLI tool (`mpqcli.exe`) appends the local filename to the `--path` argument, causing paths like `DBFilesClient\Spell.dbc\Spell.dbc` instead of the correct `DBFilesClient\Spell.dbc`.

**Fixed**: Implemented workaround by renaming the file to match the target archive name and using the parent directory as the path.

**Files Changed**: MpqHelper.cs (AddFile method rewritten)

### ✨ NEW FEATURES

#### Automatic Verification System
The tool now automatically validates merge correctness by collecting test cases during localization and verifying them after the merge.

**Test Case Types**:
- **NoLocalization**: Records without German translations (should use fallback)
- **MultiColumn**: Records with multiple localized columns

**Verification Output**:
```
[VERIFY] Running verification tests for Spell...
[VERIFY] Collected test cases: NoLocalization=5, MultiColumn=8
[VERIFY] Tests: 13, Passed: 13, Failed: 0
[VERIFY] OK - All tests passed
```

If tests fail, detailed information is provided about which records failed and the expected vs. actual values.

**Files Changed**: LocalizeEngine.cs (VerifyLocalizedDbc method), LocalizeMpqCommandHandler.cs, Models.cs

#### DBC Inspection Tool (verify-dbc command)
New command for debugging and verifying DBC content in MPQ archives.

**Usage**:
```bash
# Search for a specific record ID
dbc-localizer verify-dbc \
  --mpq output/patch-Y.mpq \
  --dbc "DBFilesClient\Spell.dbc" \
  --id 81423

# Show all record IDs grouped by thousands
dbc-localizer verify-dbc \
  --mpq output/patch-Y.mpq \
  --dbc "DBFilesClient\Spell.dbc" \
  --show-all-ids
```

**Use Cases**:
- Verify specific records exist in output
- Debug missing or corrupted records
- Inspect ID ranges of custom content
- Validate localization results

**Files Added**: VerifyDbcCommandHandler.cs

#### Configurable Fallback Locale
Control what happens when German translations are missing.

**Configuration**:
```json
{
  "fallback-locale": "enUS"  // Default: Use English as fallback
}
```

**CLI Argument**:
```bash
dbc-localizer localize-mpq \
  --fallback-locale enUS  # Use English as fallback
  --fallback-locale ""    # Disable fallback (leave empty)
  --fallback-locale frFR  # Use French as fallback
```

**Behavior**:
- `"enUS"` (default): Use English text for missing German translations
- `"frFR"` or other code: Use that locale as fallback
- `""` (empty): Disable fallback, leave fields empty

**Files Changed**: LocalizeEngine.cs, LocalizeMpqCommandHandler.cs, CommandArgs.cs, ConfigLoader.cs, config.json

### 🔧 IMPROVEMENTS

- Added `VerificationTestCase` and `VerificationResult` models for structured validation
- Improved logging with `[VERIFY]` section showing test results
- Better error messages when verification fails
- Max 10 test cases collected per category to avoid excessive memory usage

### 📝 DOCUMENTATION

- Updated README.md with new features, commands, and configuration options
- Added verification system explanation with example output
- Documented verify-dbc command usage with examples
- Added fallback-locale configuration documentation
- Updated architecture section with new classes

---

## [1.0.0] - Initial Release

### Features

- Multi-patch support: Process multiple patches in a single command
- Automatic DBC detection: Scans and identifies localizable files
- Multi-MPQ locale support: Merge multiple locale MPQs
- WDBX definition support: Uses WoWDBDefs for field mapping
- JSON reports: Detailed statistics and warnings
- Config file support: Automated execution via config.json
- Interactive selection: Choose specific DBCs when scanning
- Auto locale detection: Locale codes from MPQ filenames
