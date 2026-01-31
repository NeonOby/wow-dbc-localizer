using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using DBCD;
using DBCD.Providers;

namespace dbcd_cli
{
    /// <summary>
    /// DBCD CLI - Command line interface for DBC manipulation
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            try
            {
                string command = args[0];
                return command switch
                {
                    "scan" => CommandScan(args),
                    "read" => CommandRead(args),
                    "write" => CommandWrite(args),
                    "--help" or "-h" => PrintUsage(),
                    _ => throw new Exception($"Unknown command: {command}")
                };
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Scan DBC schema for Lang_* fields
        /// Usage: dbcd-cli scan <dbc_name> --defs <path> [--build 3.3.5.12340]
        /// Output: JSON with Lang field information
        /// </summary>
        static int CommandScan(string[] args)
        {
            if (args.Length < 3)
                throw new Exception("scan requires: <dbc_name> --defs <path>");

            string dbcName = args[1];
            string defsPath = ExtractArgValue(args, "--defs");
            string build = ExtractArgValue(args, "--build") ?? "3.3.5.12340";

            Console.WriteLine($"[*] Scanning {dbcName} in build {build}...");
            Console.WriteLine($"[*] Definitions path: {defsPath}");

            var dbdProvider = new FilesystemDBDProvider(defsPath);
            
            // Try to load definition
            DBDefsLib.Structs.DBDefinition definition = null;
            try
            {
                var stream = dbdProvider.StreamForTableName(dbcName, build);
                definition = DBDefsLib.DBDReader.Read(stream);
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not load definition for {dbcName}: {ex.Message}");
            }

            if (definition == null)
                throw new Exception($"No definition found for {dbcName}");

            // Find Lang fields
            var langFields = new List<string>();
            var allFields = new List<string>();

            if (definition.Versions?.Count > 0)
            {
                var latestVersion = definition.Versions[definition.Versions.Count - 1];
                foreach (var field in latestVersion.Fields)
                {
                    allFields.Add(field.Name);
                    if (field.Name.Contains("Lang", StringComparison.OrdinalIgnoreCase))
                    {
                        langFields.Add(field.Name);
                    }
                }
            }

            var result = new
            {
                dbc = dbcName,
                build = build,
                has_lang_fields = langFields.Count > 0,
                lang_fields = langFields,
                total_fields = allFields.Count,
                all_fields = allFields
            };

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
            return 0;
        }

        /// <summary>
        /// Read DBC to JSON
        /// Usage: dbcd-cli read <dbc_path> --defs <path> [--build 3.3.5.12340]
        /// Output: JSON to stdout
        /// </summary>
        static int CommandRead(string[] args)
        {
            if (args.Length < 3)
                throw new Exception("read requires: <dbc_path> --defs <path>");

            string dbcPath = args[1];
            string defsPath = ExtractArgValue(args, "--defs");
            string build = ExtractArgValue(args, "--build") ?? "3.3.5.12340";

            if (!File.Exists(dbcPath))
                throw new Exception($"DBC not found: {dbcPath}");

            string dbcName = Path.GetFileNameWithoutExtension(dbcPath);
            string dbcDir = Path.GetDirectoryName(dbcPath);

            var dbdProvider = new FilesystemDBDProvider(defsPath);
            var dbcProvider = new FilesystemDBCProvider(dbcDir);
            var dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);

            var storage = dbcd.Load(dbcName, build);

            // Convert to JSON
            var records = new List<Dictionary<string, object>>();
            foreach (var row in storage.Values)
            {
                var record = new Dictionary<string, object>();
                foreach (var key in row.Keys)
                {
                    record[key] = ConvertToSerializable(row[key]);
                }
                records.Add(record);
            }

            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(json);
            return 0;
        }

        /// <summary>
        /// Write JSON to DBC
        /// Usage: dbcd-cli write <input_json> --defs <path> --output <dbc_path> [--build 3.3.5.12340]
        /// </summary>
        static int CommandWrite(string[] args)
        {
            if (args.Length < 5)
                throw new Exception("write requires: <input_json> --defs <path> --output <dbc>");

            string jsonPath = args[1];
            string defsPath = ExtractArgValue(args, "--defs");
            string outputPath = ExtractArgValue(args, "--output");
            string build = ExtractArgValue(args, "--build") ?? "3.3.5.12340";

            if (!File.Exists(jsonPath))
                throw new Exception($"JSON not found: {jsonPath}");

            string dbcName = Path.GetFileNameWithoutExtension(outputPath);
            string dbcDir = Path.GetDirectoryName(outputPath) ?? ".";

            // Read JSON
            string jsonText = File.ReadAllText(jsonPath);
            var inputRecords = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(jsonText);

            var dbdProvider = new FilesystemDBDProvider(defsPath);
            var dbcProvider = new FilesystemDBCProvider(dbcDir);
            var dbcd = new DBCD.DBCD(dbcProvider, dbdProvider);

            // Load existing structure
            var storage = dbcd.Load(dbcName, build);

            // Clear and repopulate
            storage.Clear();
            foreach (var inputRecord in inputRecords)
            {
                var row = storage.AddRow();
                foreach (var kvp in inputRecord)
                {
                    try
                    {
                        row[kvp.Key] = ConvertFromJson(kvp.Value);
                    }
                    catch
                    {
                        // Skip fields that don't exist or can't be set
                    }
                }
            }

            storage.Save(outputPath);
            Console.Error.WriteLine($"Wrote {inputRecords.Count} records to {outputPath}");
            return 0;
        }

        static object ConvertToSerializable(object value)
        {
            if (value == null) return null;
            if (value is string) return value;
            if (value is int or long or float or double or bool) return value;
            if (value is Array arr)
            {
                var list = new List<object>();
                foreach (var item in arr)
                    list.Add(ConvertToSerializable(item));
                return list;
            }
            return value.ToString();
        }

        static object ConvertFromJson(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => element.TryGetInt32(out int i) ? i : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertFromJson).ToArray(),
                _ => null
            };
        }

        static string ExtractArgValue(string[] args, string argName)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == argName)
                    return args[i + 1];
            }
            return null;
        }

        static int PrintUsage()
        {
            Console.WriteLine(@"
dbcd-cli - DBCD Command Line Interface for WoW DBC manipulation

USAGE:
  dbcd-cli <command> [options]

COMMANDS:
  scan <dbc_name>
    Scan DBC schema for localizable fields
    Options:
      --defs <path>     Path to WoWDBDefs/definitions directory (required)
      --build <build>   Build number (default: 3.3.5.12340)
    Output: JSON with Lang field information

  read <dbc_path>
    Read DBC file and output as JSON
    Options:
      --defs <path>     Path to WoWDBDefs/definitions directory (required)
      --build <build>   Build number (default: 3.3.5.12340)
    Output: JSON to stdout

  write <input_json>
    Write JSON to DBC file
    Options:
      --defs <path>     Path to WoWDBDefs/definitions directory (required)
      --output <path>   Output DBC file path (required)
      --build <build>   Build number (default: 3.3.5.12340)

EXAMPLES:
  dbcd-cli scan Spell --defs ./definitions
  dbcd-cli read Spell.dbc --defs ./definitions > spell.json
  dbcd-cli write spell_merged.json --defs ./definitions --output Spell_new.dbc
");
            return 0;
        }
    }
}
