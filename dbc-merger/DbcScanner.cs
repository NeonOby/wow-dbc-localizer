using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DBCD.Providers;

namespace DbcMerger
{
	internal static class DbcScanner
	{
		public static List<(string Path, int LocCount)> GetLocalizableDbcCandidates(
			string mpqcliPath,
			string patchMpq,
			List<string> localeMpqs,
			string defsPath,
			string build,
			List<string> warnings)
		{
			var patchFiles = MpqHelper.ListFiles(mpqcliPath, patchMpq);
			var patchDbcs = MpqHelper.FilterDbcPaths(patchFiles).ToHashSet(StringComparer.OrdinalIgnoreCase);

			var localeDbcsList = new List<HashSet<string>>();
			foreach (var mpq in localeMpqs)
			{
				var localeFiles = MpqHelper.ListFiles(mpqcliPath, mpq);
				localeDbcsList.Add(MpqHelper.FilterDbcPaths(localeFiles).ToHashSet(StringComparer.OrdinalIgnoreCase));
			}

			var dbdProvider = new FilesystemDBDProvider(defsPath);
			var candidates = new List<(string Path, int LocCount)>();

			foreach (var dbcPath in patchDbcs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
			{
				bool presentInAllLocales = true;
				for (int i = 0; i < localeDbcsList.Count; i++)
				{
					if (!localeDbcsList[i].Contains(dbcPath))
					{
						warnings.Add($"Locale MPQ missing DBC: {dbcPath} (locale index {i})");
						presentInAllLocales = false;
						break;
					}
				}

				if (!presentInAllLocales)
					continue;

				var tableName = Path.GetFileNameWithoutExtension(dbcPath);
				try
				{
					var dbdStream = dbdProvider.StreamForTableName(tableName, build);
					var dbdDefinition = new DBDefsLib.DBDReader().Read(dbdStream);
					if (!DBDefsLib.Utils.GetVersionDefinitionByBuild(dbdDefinition, new DBDefsLib.Build(build), out var versionDef) || versionDef == null)
						continue;
					var locFields = Helpers.GetLocStringFields(versionDef.Value, dbdDefinition);
					if (locFields.Count > 0)
						candidates.Add((dbcPath, locFields.Count));
				}
				catch
				{
					// ignore tables without definitions
				}
			}

			return candidates;
		}

		public static List<string> ValidateSelectedDbcs(
			string mpqcliPath,
			string patchMpq,
			List<string> localeMpqs,
			List<string> selectedDbcs,
			List<string> warnings)
		{
			var patchFiles = MpqHelper.ListFiles(mpqcliPath, patchMpq);
			var patchDbcs = MpqHelper.FilterDbcPaths(patchFiles).ToHashSet(StringComparer.OrdinalIgnoreCase);

			var localeDbcsList = new List<HashSet<string>>();
			foreach (var mpq in localeMpqs)
			{
				var localeFiles = MpqHelper.ListFiles(mpqcliPath, mpq);
				localeDbcsList.Add(MpqHelper.FilterDbcPaths(localeFiles).ToHashSet(StringComparer.OrdinalIgnoreCase));
			}

			var valid = new List<string>();
			foreach (var dbc in selectedDbcs)
			{
				if (!patchDbcs.Contains(dbc))
				{
					warnings.Add($"Patch MPQ missing DBC: {dbc}");
					continue;
				}

				bool missingLocale = false;
				for (int i = 0; i < localeDbcsList.Count; i++)
				{
					if (!localeDbcsList[i].Contains(dbc))
					{
						warnings.Add($"Locale MPQ missing DBC: {dbc} (locale index {i})");
						missingLocale = true;
						break;
					}
				}

				if (!missingLocale)
					valid.Add(dbc);
			}

			return valid.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
	}
}
