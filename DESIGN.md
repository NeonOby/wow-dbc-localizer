# DBC-Localizer v2 - Neue Architektur

## 📁 Struktur

```
input/
  ├── patch/              ← User packt hier die zu patchenden MPQs rein
  │   ├── patch-B.mpq
  │   ├── patch-C.mpq
  │   └── ...
  │
  └── locale/             ← User packt hier ALLE Locale MPQs rein
      ├── locale-deDE.MPQ ← Deutsch
      ├── locale-frFR.MPQ ← Französisch
      └── locale-ruRU.MPQ ← Russisch
      
output/
  ├── patch-B-merged.mpq  ← Patch mit allen Übersetzungen
  ├── patch-C-merged.mpq
  └── ...
```

## 🔄 Automatischer Workflow

```
1. SCAN INPUT
   ├─ Erkenne alle .mpq in input/patch/
   └─ Erkenne alle .mpq in input/locale/ + Locale-Code (z.B. deDE aus locale-deDE.MPQ)

2. SCAN DBCD DEFINITIONS
   ├─ Lade alle .dbd Files aus WoWDBDefs/definitions/
   ├─ Filtere nur DBCs mit Lang_* Felder
   └─ Generiere Liste der zu patchenden DBCs (z.B. Spell, Item, Achievement, ...)

3. FÜR JEDES PATCH-MPQ:
   ├─ Extrahiere NUR die zu patchenden DBCs aus patch MPQ (Speicheroptimierung)
   │
   ├─ FÜR JEDES DBC-MIT-LANG-FELDERN:
   │  ├─ TEMP: Extrahiere DBC aus patch-B.mpq → temp/extract/{dbc_name}.dbc
   │  ├─ Iteriere alle Locales:
   │  │  ├─ Versuche Übersetzung aus locale-XYZ.MPQ laden
   │  │  │  └─ TEMP: Extrahiere → temp/locale/{locale}/{dbc_name}.dbc
   │  │  └─ WENN nicht gefunden: nutze enUS fallback
   │  ├─ Merge Daten mit DBCD (lokal übernimmt englisch nicht, fallback only)
   │  ├─ TEMP: Schreibe merged DBC → temp/merged/{dbc_name}.dbc
   │  └─ 🧹 CLEANUP: Lösche temp/extract/{dbc_name}.dbc + temp/locale/{locale}/{dbc_name}.dbc
   │
   ├─ Extrahiere UNVERÄNDERTE DBCs aus original patch MPQ
   ├─ Packe alle DBCs (gemergt + unverändert) ins neue MPQ
   └─ 🧹 CLEANUP: Lösche temp/merged/* + temp/extract/*

4. OUTPUT
   └─ output/{ORIGINAL_NAME}-merged.mpq

SPEICHEROPTIMIERUNG:
- Nicht alle DBCs aus MPQ extrahieren, nur die mit Lang-Feldern
- Nach jedem DBC-Merge die Temp-Dateien löschen (nicht am Ende)
- Damit: Speicher nur für AKTUELLES DBC + AKTUELLES Locale DBC
```

## 🧩 Komponenten

### 1. **MPQExtractor** (mpqcli-basiert)
```python
extract_mpq(mpq_path) -> List[DBC]
pack_mpq(dbcs, output_path) -> None
```

### 2. **DBCScanner** (DBCD-basiert)
```python
scan_dbc(dbc_path) -> {
    'name': 'Spell',
    'has_localization': True,
    'lang_fields': ['Name_lang', 'Description_lang', ...],
    'record_count': 49866
}
```

### 3. **DBCMerger** (DBCD-basiert)
```python
merge_dbc(
    base_dbc,           # DBC aus patch-B.mpq
    locale_dbc,         # DBC aus locale-deDE.MPQ
    locale_code='deDE'
) -> merged_dbc
# Logik: Für jedes Record mit Lang_*:
#   - Wenn Übersetzung vorhanden: nutzen
#   - Sonst: enUS fallback
```

### 4. **LocaleDetector**
```python
detect_locale(filename) -> 'deDE'  # aus "locale-deDE.MPQ"
```

### 5. **Main Orchestrator**
```python
def process_all_patches():
    patch_mpqs = find_mpqs('input/patch/')
    locale_mpqs = find_mpqs('input/locale/')
    locales = {detect_locale(mpq): mpq for mpq in locale_mpqs}
    
    for patch_mpq in patch_mpqs:
        process_single_patch(patch_mpq, locales)
```

## 🔑 Wichtige Logik

### Fallback-Strategie
```
Für jedes DBC mit Lang_deDE Feld:
1. Versuche deDE aus locale-deDE.MPQ
2. Wenn nicht gefunden → enUS fallback
3. Wenn auch enUS nicht → leer lassen (selten)
```

### Multi-Locale
```
User hat 3 locale MPQs:
- locale-deDE.MPQ → DBCs mit deDE Übersetzungen
- locale-frFR.MPQ → DBCs mit frFR Übersetzungen
- locale-ruRU.MPQ → DBCs mit ruRU Übersetzungen

Für jedes DBC:
- Merge deDE Felder von deDE-MPQ
- Merge frFR Felder von frFR-MPQ
- Merge ruRU Felder von ruRU-MPQ
- Alles andere: fallback zu enUS
```

## 📊 Beispiel: Spell.dbc Merge

```
patch-B/Spell.dbc Record #1:
  ID: 1
  Name_lang: [enUS:"Fireball", deDE:"", frFR:"", ...]
  
locale-deDE/Spell.dbc Record #1:
  ID: 1
  Name_lang: [enUS:"Fireball", deDE:"Feuerball", frFR:"", ...]

MERGE RESULT:
  ID: 1
  Name_lang: [enUS:"Fireball", deDE:"Feuerball", frFR:"", ...]
```

## 🛠️ Technologie

- **DBCD**: DBC Read/Write ✅ (getestet)
  - Definitions Auto-Scan für Lang_* Felder
- **mpqcli**: MPQ extract/pack ✅ (vorhanden)
  - Smart Extract: nur nötige DBCs
- **Python**: Orchestration + Logik
- **Subprocess**: dbcd-cli Calls + mpqcli Calls

## ✅ Vorteile dieser Architektur

- ✅ **Zero-Config für User**: Nur MPQ-Dateien in Ordner packen
- ✅ **Auto-Scan DBCs**: Definitions sagen uns welche Lang-Felder haben
- ✅ **Multi-Locale**: Beliebig viele Lokalisierungen gleichzeitig
- ✅ **Smart Fallback**: Fehlende Übersetzungen nutzen enUS
- ✅ **Speicheroptimierung**: Cleanup während Process, nicht am Ende
- ✅ **Bestandsschutz**: Alte pywowlib-Version bleibt (--method flag)
- ✅ **Erweiterbar**: Leicht weitere DBCs hinzufügen

## 🚀 Nächste Schritte

1. **C# dbcd-cli Tool** bauen:
   - `dbcd-cli scan <dbc> --defs <path>` → JSON mit Lang-Feldern
   - `dbcd-cli read <dbc> --defs <path> --locale deDE` → JSON Data
   - `dbcd-cli write <json> --defs <path> --output <dbc>` → Binary DBC

2. **Python Komponenten** implementieren:
   - Locale Detector, MPQ Scanner, DBC Merger
   - Main Orchestrator mit Cleanup während Process

3. Tests mit patch-B.mpq + locale-deDE.MPQ

4. Multi-Locale testen (falls mehrere locales vorhanden)

5. Integration in run_merge.py (mit --method dbcd flag)
