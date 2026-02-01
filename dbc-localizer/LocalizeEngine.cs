using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBCD;
using DBCD.Providers;

namespace DbcLocalizer
{
	internal static class LocalizeEngine
	{
		public static int LocalizeDbc(
			string basePath,
			string localePath,
			string defsPath,
			string outputPath,
			string build,
			string localeCode,
			string localeSource,
			string targetPath,
			Action<string>? verboseLog,
			out LocalizeStats stats)
		{
			stats = new LocalizeStats();
			var baseName = Path.GetFileNameWithoutExtension(basePath);

			Logger.Info($"[*] Localize DBC: {baseName}");
			Logger.Info($"[*] Build: {build}");

			var dbdProvider = new FilesystemDBDProvider(defsPath);

			// Load base DBC
			var baseProvider = new FilesystemDBCProvider(Path.GetDirectoryName(basePath) ?? ".");
			var baseDbcd = new DBCD.DBCD(baseProvider, dbdProvider);
			var baseStorage = baseDbcd.Load(baseName, build, DBCD.Locale.None);

			// Read DBD definition to locate locstring fields
			var dbdStream = dbdProvider.StreamForTableName(baseName, build);
			var dbdDefinition = new DBDefsLib.DBDReader().Read(dbdStream);
			if (!DBDefsLib.Utils.GetVersionDefinitionByBuild(dbdDefinition, new DBDefsLib.Build(build), out var versionDef) || versionDef == null)
			{
				Logger.Error($"No DBD version definition found for build {build}");
				return 1;
			}

			var locFields = Helpers.GetLocStringFields(versionDef.Value, dbdDefinition);
			Logger.Info($"[*] Locstring fields: {locFields.Count}");

			var localeIndex = Helpers.GetLocaleIndex(localeCode, build);
			var localeStrings = ReadLocaleStrings(localePath, versionDef.Value, dbdDefinition, localeIndex);

			int mergedRows = 0;
			int fieldsUpdated = 0;
			foreach (var kvp in baseStorage.ToDictionary())
			{
				var id = kvp.Key;
				var baseRow = kvp.Value;

				if (!localeStrings.TryGetValue(id, out var strings))
					continue;

				bool changed = LocalizeRow(
					baseRow,
					strings,
					locFields,
					baseStorage.AvailableColumns,
					localeIndex,
					baseName,
					id,
					localeCode,
					localeSource,
					targetPath,
					verboseLog,
					out var rowFieldUpdates);
				if (changed)
					mergedRows++;
				fieldsUpdated += rowFieldUpdates;
			}

			Logger.Info($"[*] Rows localized: {mergedRows}");
			Logger.Info($"[*] Fields updated: {fieldsUpdated}");

			// Ensure output directory exists
			var outputDir = Path.GetDirectoryName(outputPath);
			if (!string.IsNullOrWhiteSpace(outputDir) && !Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);

			baseStorage.Save(outputPath);
			Logger.Info($"[*] Wrote localized DBC: {outputPath}");

			stats.RowsMerged = mergedRows;
			stats.FieldUpdates = fieldsUpdated;

			return 0;
		}

		private static bool LocalizeRow(
			DBCDRow baseRow,
			List<string> localeValues,
			List<string> locFields,
			string[] availableColumns,
			int localeIndex,
			string tableName,
			int id,
			string localeCode,
			string localeSource,
			string targetPath,
			Action<string>? verboseLog,
			out int fieldUpdates)
		{
			bool changed = false;
			fieldUpdates = 0;

			for (int i = 0; i < locFields.Count; i++)
			{
				if (i >= localeValues.Count)
					break;

				var text = localeValues[i];
				if (string.IsNullOrWhiteSpace(text))
					continue;

				var field = locFields[i];
				try
				{
					var value = baseRow[field];
					if (value is string[] arr)
					{
						if (localeIndex >= 0 && localeIndex < arr.Length)
						{
							arr[localeIndex] = text;
							baseRow[field] = arr;
							changed = true;
							fieldUpdates++;
							verboseLog?.Invoke($"copied {localeCode} from {localeSource} to {targetPath} {tableName}.dbc ID {id} field {field}");

							var maskField = field + "_mask";
							if (availableColumns.Contains(maskField))
							{
								var currentMask = Convert.ToUInt32(baseRow[maskField]);
								if (localeIndex < 32)
								{
									baseRow[maskField] = currentMask | (1u << localeIndex);
								}
							}
						}
					}
					else if (value is string)
					{
						baseRow[field] = text;
						changed = true;
						fieldUpdates++;
						verboseLog?.Invoke($"copied {localeCode} from {localeSource} to {targetPath} {tableName}.dbc ID {id} field {field}");
					}
				}
				catch
				{
					// Ignore field assignment errors
				}
			}

			return changed;
		}

		private static Dictionary<int, List<string>> ReadLocaleStrings(
			string localeDbcPath,
			DBDefsLib.Structs.VersionDefinitions versionDef,
			DBDefsLib.Structs.DBDefinition dbd,
			int localeIndex)
		{
			var data = File.ReadAllBytes(localeDbcPath);
			if (data.Length < 20)
				throw new Exception("Invalid DBC file (too small)");

			// Header
			uint records = BitConverter.ToUInt32(data, 4);
			uint fields = BitConverter.ToUInt32(data, 8);
			uint recordSize = BitConverter.ToUInt32(data, 12);

			int headerSize = 20;
			int stringBlockStart = headerSize + (int)records * (int)recordSize;

			// Calculate locstring field counts
			int locCount = 0;
			int nonLocCount = 0;

			foreach (var def in versionDef.definitions)
			{
				if (!dbd.columnDefinitions.TryGetValue(def.name, out var col))
					continue;

				int arrLength = def.arrLength > 0 ? def.arrLength : 1;

				if (string.Equals(col.type, "locstring", StringComparison.OrdinalIgnoreCase))
				{
					locCount += 1;
				}
				else
				{
					nonLocCount += arrLength;
				}
			}

			if (locCount == 0)
				return new Dictionary<int, List<string>>();

			int locWidth = (int)((fields - nonLocCount) / locCount);
			if (locWidth <= 0)
				throw new Exception("Could not compute localized string width.");

			var result = new Dictionary<int, List<string>>();

			for (int rec = 0; rec < records; rec++)
			{
				int offset = headerSize + rec * (int)recordSize;
				int pos = offset;

				int id = -1;
				var strings = new List<string>();

				foreach (var def in versionDef.definitions)
				{
					if (!dbd.columnDefinitions.TryGetValue(def.name, out var col))
						continue;

					int arrLength = def.arrLength > 0 ? def.arrLength : 1;

					if (def.isID)
					{
						id = BitConverter.ToInt32(data, pos);
					}

					if (string.Equals(col.type, "locstring", StringComparison.OrdinalIgnoreCase))
					{
						int localeOffset = 0;
						if (localeIndex >= 0 && localeIndex < locWidth)
						{
							localeOffset = BitConverter.ToInt32(data, pos + (localeIndex * 4));
						}

						string value = ReadString(data, stringBlockStart, localeOffset);
						strings.Add(value);

						pos += locWidth * 4;
					}
					else
					{
						pos += GetFieldSize(col.type, def.size) * arrLength;
					}
				}

				if (id != -1)
					result[id] = strings;
			}

			return result;
		}

		private static int GetFieldSize(string type, int sizeBits)
		{
			switch (type)
			{
				case "int":
					return Math.Max(1, sizeBits / 8);
				case "float":
					return 4;
				case "string":
					return 4;
				default:
					return 4;
			}
		}

		private static string ReadString(byte[] data, int stringBlockStart, int offset)
		{
			if (offset <= 0)
				return string.Empty;

			int pos = stringBlockStart + offset;
			if (pos < 0 || pos >= data.Length)
				return string.Empty;

			int end = pos;
			while (end < data.Length && data[end] != 0)
				end++;

			return System.Text.Encoding.UTF8.GetString(data, pos, end - pos);
		}
	}
}
