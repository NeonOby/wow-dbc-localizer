using System;
using System.Collections.Generic;
using System.Linq;

namespace DbcMerger
{
	internal static class Helpers
	{
		public static string? GetArg(string[] args, string key)
		{
			for (int i = 0; i < args.Length - 1; i++)
			{
				if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
					return args[i + 1];
			}
			return null;
		}

		public static List<string> ParseDbcList(string input)
		{
			return input
				.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s))
				.Select(s => s.Replace('/', '\\'))
				.ToList();
		}

		public static int GetLocaleIndex(string code, string build)
		{
			if (!DBDefsLib.Build.TryParse(build, out var parsed))
				return 0;

			int locSize = GetLocStringSize(parsed);
			if (locSize == 1)
				return 0;

			return code.Trim().ToUpperInvariant() switch
			{
				"ENUS" => 0,
				"KOKR" => 1,
				"FRFR" => 2,
				"DEDE" => 3,
				"ENCN" => 4,
				"ZHCN" => 4,
				"ENTW" => 5,
				"ZHTW" => 5,
				"ESES" => 6,
				"ESMX" => 7,
				"RURU" => 8,
				"JAJP" => 9,
				"PTPT" => 10,
				"PTBR" => 10,
				"ITIT" => 11,
				_ => 0
			};
		}

		public static int GetLocStringSize(DBDefsLib.Build build)
		{
			if (build.expansion >= 4 || build.build > 12340)
				return 1;
			if (build.build >= 6692)
				return 16;
			return 8;
		}

		public static List<string> GetLocStringFields(DBDefsLib.Structs.VersionDefinitions versionDef, DBDefsLib.Structs.DBDefinition dbd)
		{
			var list = new List<string>();
			foreach (var def in versionDef.definitions)
			{
				if (!dbd.columnDefinitions.TryGetValue(def.name, out var col))
					continue;

				if (string.Equals(col.type, "locstring", StringComparison.OrdinalIgnoreCase))
					list.Add(def.name);
			}
			return list;
		}
	}
}
