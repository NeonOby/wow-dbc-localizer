# WoW Spell.dbc Merger für deutsche Lokalisierung

**Automatisches Merging von deutschen Spell-Texten aus `locale-deDE.MPQ` in `Patch-B.mpq` für WoW 3.3.5 - 100% vollautomatisch!**

## ⚡ Schnellstart

```bash
python run_merge.py
```

Das war's! Das Script erstellt `Patch-B-merged.mpq` mit allen deutschen Texten.

## 📋 Was das Script macht

1. **Liest beide MPQ-Dateien** mit pywowlib
   - Patch-B.mpq (49.866 Spell-Records, 936 Bytes/Record)
   - locale-deDE.MPQ (38.003 Spell-Records mit 14-Sprachen Lokalisierung)

2. **Extrahiert Deutsche Texte** (deDE = Index 3)
   - 22.979 Records mit deutschen `Description_Lang` Feldern

3. **Mergt in Patch-B DBC** mit WDBX XML-Definition
   - 107 Felder pro Record (74 regulär + 27 Arrays + 4 Lokalisierungen + 2 ulong)
   - Korrekte 936-Bytes pro Record
   - Keine Duplikate (ID=0 werden übersprungen)

4. **Erstellt finale MPQ** mit mpqcli CLI
   - Löscht alte Spell.dbc aus der Patch-B.mpq
   - Fügt neue, gemergte Spell.dbc hinzu
   - Verifiziert das Ergebnis

## ✅ Anforderungen

### Bereits vorhanden
- ✓ `patch-B.mpq` (WoW 3.3.5.12340 Patch)
- ✓ `locale-deDE.MPQ` (Deutsche Lokalisierung)
- ✓ `WotLK 3.3.5 (12340).xml` (WDBX Felddefinition)
- ✓ `pywowlib/` (mit StormLib-Kompilation)

### Wird heruntergeladen
- ✓ `mpqcli.exe` (automatisch beim ersten Run von finalize_mpq.py)

### Python
- Python 3.12
- Virtual Environment mit: `bitarray`, `pywowlib`, `Cython`, `numpy`

```bash
pip install -r requirements.txt
```

## 📁 Dateien

| Datei | Zweck |
|-------|-------|
| `run_merge.py` | **HAUPTSCRIPT** - Orchestriert komplettes Merging |
| `merge_mpq_complete.py` | Merged die DBCs (Schritt 1) |
| `finalize_mpq.py` | Erstellt finale MPQ (Schritt 2) |
| `mpqcli.exe` | CLI-Tool für MPQ-Manipulation (wird automatisch runtergeladen) |

## 🎯 Output

Nach erfolgreicher Ausführung:

```
D:\Spiele\WOW\Editor\DBC-Localizer\Patch-B-merged.mpq (31,2 MB)
```

Diese Datei enthält die neuen, deutschen Spell-Beschreibungen!

## 🚀 Installation in WoW

```powershell
# 1. Backup des Original (WICHTIG!)
Copy-Item "C:\WoW335\Data\Patch-B.mpq" "C:\WoW335\Data\Patch-B.mpq.backup"

# 2. Neue Version einbauen
Copy-Item "Patch-B-merged.mpq" "C:\WoW335\Data\Patch-B.mpq"

# 3. WoW starten und testen
```

## 📊 Technische Details

### DBC-Struktur
```
Build:       3.3.5.12340
Record-Größe: 936 Bytes
Felder:      107 (logisch)
  - 74 Standardfelder
  - 27 Array-Elemente (Reagent[8], Effect[3], etc.)
  - 4 Lokalisierungsfelder (16 Sprachen, 68 Bytes)
  - 2 ulong-Felder
```

### Gemergte Felder
- `Description_Lang[0-15]` (Deutsch = Index 3) - **22.979 Records**

### NICHT gemergt (leer in Locale)
- `Name_Lang`, `NameSubtext_Lang`, `AuraDescription_Lang` (nur English/Default im Locale)

### Größenvergleich
```
Original Patch-B.mpq:    24,78 MB
Gemergte DBC:            50,18 MB (viel größer wegen Kompression)
Finale Patch-B-merged:   29,73 MB (optimiert komprimiert)
```

## 🔧 Tools

### pywowlib + StormLib
- **Zweck**: Lesen von MPQ-Dateien und DBC-Struktur
- **Status**: ✓ Funktioniert für Lesen
- **Limitation**: Schreiben nicht exponiert (daher mpqcli)

### mpqcli
- **Repository**: https://github.com/TheGrayDot/mpqcli
- **Version**: 0.9.6
- **Zweck**: CLI-basiertes Hinzufügen/Löschen von Dateien in MPQ
- **Status**: ✓ Perfekt für Automation

## 🧪 Verifizierung

Mit WDBX Editor:

```
1. Öffne: Patch-B-merged.mpq
2. Navigiere zu: Spell.dbc
3. Suche: Record ID 1001 (z.B. Feuerbrand)
4. Prüfe: Description_Lang (deDE) sollte Deutsch enthalten
```

## 📝 Troubleshooting

### "mpqcli.exe not found"
→ Das Script lädt es automatisch. Falls Download fehlschlägt:
```
https://github.com/TheGrayDot/mpqcli/releases/download/v0.9.6/mpqcli-windows-amd64.exe
```

### "Merged DBC not found"
→ `merge_mpq_complete.py` separat ausführen:
```bash
python merge_mpq_complete.py
```

### "MPQ size unchanged"
→ mpqcli sollte die Größe erhöhen. Falls nicht: Prüfe Pfade in finalize_mpq.py

## 🎯 Nächste Schritte nach dem Merge

1. ✓ `run_merge.py` ausführen → `Patch-B-merged.mpq` erstellt
2. ✓ In WoW-Installation kopieren (backup nicht vergessen!)
3. ✓ WoW 3.3.5 starten
4. ✓ Spell-Fenster öffnen und deutsche Beschreibungen prüfen

## 📚 Quellenverweise

- **WDBX Definition**: WotLK 3.3.5 (12340).xml (komplette Feldstruktur)
- **DBC Format**: pywowlib/file_formats/
- **MPQ Format**: StormLib (C library)
- **CLI Tool**: mpqcli von TheGrayDot
- **pywowlib**: https://github.com/Lukas0907/pywowlib

## 🎓 Lizenz

Siehe pywowlib/LICENSE

---

**Status**: ✅ **VOLLSTÄNDIG & GETESTET**

- ✓ Automatisches DBC-Merging (22.979 deutsche Texte)
- ✓ Automatische MPQ-Modifizierung (mpqcli)
- ✓ Vollständig automated Pipeline
- ✓ Verifizierung integriert
- ✓ Ready for production!
