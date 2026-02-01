# DBC Localizer - Automatisches Lokalisierungstool

Ein C#-basiertes Tool zum Zusammenführen von lokalisierten WoW-DBC-Dateien aus mehreren MPQ-Archiven.

## Features

- ✅ Automatische Erkennung von lokalisierungsfähigen DBCs
- ✅ Mehrfach-Lokalisierungen in einem Durchlauf (z.B. alle Patch-Updates)
- ✅ Strukturierte Logging-Ausgabe (Info/Verbose Level)
- ✅ JSON-Reportgenerierung mit Statistiken
- ✅ Interaktive oder automatische DBC-Auswahl
- ✅ Config-Datei-basierte automatische Ausführung
- ✅ **Automatische Output-Bennenung** (Output hat gleichen Namen wie Input-Patch)
- ✅ Detaillierte Fehlermeldungen und Warnungen

## Installation

Das Tool benötigt:
- .NET 10.0 SDK
- `mpqcli.exe` (Pfad wird automatisch erkannt)
- DBCD-Bibliotheken und DBD-Definitionen

## Schnellstart - Automatischer Modus

### Schritt 1: Config-Datei anpassen

Bearbeite `config.json` und passe die Pfade an:

```json
{
  "patch": "../input/patch/patch-B.mpq",
  "locale-mpqs": [
    "../input/locale/locale-deDE.MPQ",
    "../input/locale/patch-deDE.MPQ",
    "../input/locale/patch-deDE-2.MPQ",
    "../input/locale/patch-deDE-3.MPQ"
  ],
  "langs": ["deDE", "deDE", "deDE", "deDE"],
  "defs": "../dbcd-lib/definitions/definitions",
  "output": "../output/",
  "auto": true,
  "verbose": false,
  "report": "../output/localize-report.json"
}
```

**Hinweis:** Der `output`-Pfad kann ein Verzeichnis sein (mit `/` oder `\` am Ende).
Die Output-Datei wird automatisch den gleichen Namen wie die Input-Patch-Datei bekommen:
- Input: `patch-B.mpq` → Output: `../output/patch-B.mpq`

### Schritt 2: Ausführen

```bash
# Aus dem dbc-localizer-Verzeichnis:
dotnet bin/Release/net9.0/dbc-localizer.dll

# Oder mit Verbose-Logging:
dotnet bin/Release/net9.0/dbc-localizer.dll
# (Und "verbose": true in config.json setzen)
```

Das Tool wird automatisch alle Lokalisierungsdateien zusammenführen!

## Manueller Modus - Einzelne Operationen

### Scan durchführen
```bash
dotnet bin/Release/net9.0/dbc-localizer.dll scan-mpq \
  --patch "../input/patch/patch-B.mpq" \
  --locale-mpq "../input/locale/locale-deDE.MPQ" \
  --defs "../dbcd-lib/definitions/definitions"
```

### Lokalisieren mit Auto-Detektion
```bash
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq \
  --patch "../input/patch/patch-B.mpq" \
  --locale-mpq "../input/locale/locale-deDE.MPQ" \
  --defs "../dbcd-lib/definitions/definitions" \
  --output "../output/localized.mpq" \
  --auto \
  --report "../output/report.json"
```

### Interaktive DBC-Auswahl
```bash
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq \
  --patch "../input/patch/patch-B.mpq" \
  --locale-mpq "../input/locale/locale-deDE.MPQ" \
  --defs "../dbcd-lib/definitions/definitions" \
  --output "../output/" \
  --select
```

## Multi-Locale Lokalisierung (alle Patch-Updates)

```bash
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq \
  --patch "../input/patch/patch-B.mpq" \
  --locale-mpqs "locale-deDE.MPQ;patch-deDE.MPQ;patch-deDE-2.MPQ;patch-deDE-3.MPQ" \
  --langs "deDE;deDE;deDE;deDE" \
  --defs "../dbcd-lib/definitions/definitions" \
  --output "../output/localized-all.mpq" \
  --auto \
  --report "../output/report.json"
```

## Logging-Stufen

### Info-Modus (Standard)
```bash
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq ...
```
Zeigt Zusammenfassungen:
```
[*] Rows localized: 20050
[*] Fields updated: 20050
```

### Verbose-Modus
```bash
dotnet bin/Release/net9.0/dbc-localizer.dll localize-mpq ... --verbose
```
Zeigt jede einzelne Feldänderung:
```
copied deDE from locale-deDE.MPQ to patch-B.mpq Spell.dbc ID 1 field Name_lang
copied deDE from locale-deDE.MPQ to patch-B.mpq Spell.dbc ID 1 field Description_lang
...
```

## Output-Struktur

Nach der Ausführung:
```
output/
├── patch-B.mpq             # Lokalisierte MPQ-Datei (automatisch mit Input-Namen benannt)
└── localize-report.json    # Statistiken und Metadaten
```

### Report-Beispiel
```json
{
  "TimestampUtc": "2026-01-31T19:34:05Z",
  "Build": "3.3.5.12340",
  "TotalTablesMerged": 20,
  "TotalRowsMerged": 118358,
  "TotalFieldsUpdated": 152843,
  "PerLocale": [
    {
      "Language": "deDE",
      "TablesMerged": 5,
      "RowsMerged": 21620,
      "FieldsUpdated": 21640
    }
  ]
}
```

## Wichtige Hinweise

- **Mehrfach-Updates**: Wenn mehrere Locale-MPQs die gleiche DBC enthalten, werden Updates nacheinander angewendet (letzte gewinnt)
- **Validierung**: Fehlende DBCs werden als Warnung ausgegeben, aber nicht als Fehler behandelt
- **Temp-Dateien**: Werden automatisch nach der Lokalisierung gelöscht (mit `--keep-temp` beibehalten)
- **Build-Spezifisch**: Der Build-String (z.B. 3.3.5.12340) bestimmt die DBC-Struktur

## Architektur

Das Tool ist modular aufgebaut:

- **Program.cs** - CLI und Kommando-Routing
- **Models.cs** - Datenstrukturen
- **Logger.cs** - Strukturiertes Logging
- **Helpers.cs** - Hilfsfunktionen
- **MpqHelper.cs** - MPQ-Operationen
- **DbcScanner.cs** - DBC-Analyse
- **MergeEngine.cs** - Lokalisierungs-Logik

## Lizenz

Basiert auf DBCD und WoWDBDefs - siehe entsprechende Lizenzdateien.
