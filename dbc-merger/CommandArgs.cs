using System;
using System.Collections.Generic;

namespace DbcMerger
{
	/// <summary>
	/// Arguments for the "merge" command (single DBC files)
	/// </summary>
	internal class MergeArgs
	{
		public string BasePath { get; set; } = string.Empty;
		public string LocalePath { get; set; } = string.Empty;
		public string DefsPath { get; set; } = string.Empty;
		public string OutputPath { get; set; } = string.Empty;
		public string Build { get; set; } = string.Empty;
		public string LocaleCode { get; set; } = string.Empty;

		public bool IsValid => !string.IsNullOrWhiteSpace(BasePath) && 
		                       !string.IsNullOrWhiteSpace(LocalePath) && 
		                       !string.IsNullOrWhiteSpace(DefsPath) && 
		                       !string.IsNullOrWhiteSpace(OutputPath);

		public static MergeArgs Parse(string[] args, string defaultBuild = "3.3.5.12340")
		{
			return new MergeArgs
			{
				BasePath = Helpers.GetArg(args, "--base") ?? string.Empty,
				LocalePath = Helpers.GetArg(args, "--locale") ?? string.Empty,
				DefsPath = Helpers.GetArg(args, "--defs") ?? string.Empty,
				OutputPath = Helpers.GetArg(args, "--output") ?? string.Empty,
				Build = Helpers.GetArg(args, "--build") ?? defaultBuild,
				LocaleCode = Helpers.GetArg(args, "--lang") ?? "deDE"
			};
		}
	}

	/// <summary>
	/// Arguments for the "merge-mpq" command
	/// </summary>
	internal class MergeMpqArgs
	{
		public string PatchMpq { get; set; } = string.Empty;
		public string LocaleMpq { get; set; } = string.Empty;
		public List<string> LocaleMpqs { get; set; } = new();
		public string DefsPath { get; set; } = string.Empty;
		public string OutputMpq { get; set; } = string.Empty;
		public string DbcRelPath { get; set; } = string.Empty;
		public List<string> DbcList { get; set; } = new();
		public string Build { get; set; } = string.Empty;
		public string LocaleCode { get; set; } = string.Empty;
		public List<string> Languages { get; set; } = new();
		public string MpqCliPath { get; set; } = string.Empty;
		public bool KeepTemp { get; set; }
		public bool Interactive { get; set; }
		public bool AutoAll { get; set; }
		public string ReportPath { get; set; } = string.Empty;
		public bool IsMultiPatchDir { get; set; }

		public bool IsValid => !string.IsNullOrWhiteSpace(PatchMpq) && 
		                       !string.IsNullOrWhiteSpace(DefsPath) && 
		                       !string.IsNullOrWhiteSpace(OutputMpq);

		public static MergeMpqArgs Parse(string[] args, string defaultBuild = "3.3.5.12340")
		{
			var patchArg = Helpers.GetArg(args, "--patch");
			var localeMpqArg = Helpers.GetArg(args, "--locale-mpq");
			var localeMpqsArg = Helpers.GetArg(args, "--locale-mpqs");
			var dbcArg = Helpers.GetArg(args, "--dbc");
			var dbcListArg = Helpers.GetArg(args, "--dbc-list");
			var langsArg = Helpers.GetArg(args, "--langs");
			var multiPatchDir = Helpers.GetArg(args, "--multi-patch-dir");

			var localeMpqs = new List<string>();
			if (!string.IsNullOrWhiteSpace(localeMpqsArg))
			{
				localeMpqs = Helpers.ParseDbcList(localeMpqsArg);
			}
			else if (!string.IsNullOrWhiteSpace(localeMpqArg))
			{
				localeMpqs.Add(localeMpqArg);
			}

			var dbcList = new List<string>();
			if (!string.IsNullOrWhiteSpace(dbcListArg))
			{
				dbcList = Helpers.ParseDbcList(dbcListArg);
			}

			var languages = new List<string>();
			if (!string.IsNullOrWhiteSpace(langsArg))
			{
				languages = Helpers.ParseDbcList(langsArg);
			}

			var localeCode = Helpers.GetArg(args, "--lang") ?? "deDE";
			if (languages.Count == 0)
				languages.Add(localeCode);

			return new MergeMpqArgs
			{
				PatchMpq = patchArg ?? string.Empty,
				LocaleMpq = localeMpqArg ?? string.Empty,
				LocaleMpqs = localeMpqs,
				DefsPath = Helpers.GetArg(args, "--defs") ?? string.Empty,
				OutputMpq = Helpers.GetArg(args, "--output") ?? string.Empty,
				DbcRelPath = dbcArg ?? string.Empty,
				DbcList = dbcList,
				Build = Helpers.GetArg(args, "--build") ?? defaultBuild,
				LocaleCode = localeCode,
				Languages = languages,
				MpqCliPath = Helpers.GetArg(args, "--mpqcli") ?? MpqHelper.GetDefaultMpqCliPath(),
				KeepTemp = args.Contains("--keep-temp", StringComparer.OrdinalIgnoreCase),
				ReportPath = Helpers.GetArg(args, "--report") ?? string.Empty,
				AutoAll = args.Contains("--auto", StringComparer.OrdinalIgnoreCase) || args.Contains("--all", StringComparer.OrdinalIgnoreCase),
				Interactive = args.Contains("--select", StringComparer.OrdinalIgnoreCase),
				IsMultiPatchDir = !string.IsNullOrWhiteSpace(multiPatchDir)
			};
		}
	}

	/// <summary>
	/// Arguments for the "scan-mpq" command
	/// </summary>
	internal class ScanMpqArgs
	{
		public string PatchMpq { get; set; } = string.Empty;
		public string LocaleMpq { get; set; } = string.Empty;
		public List<string> LocaleMpqs { get; set; } = new();
		public string DefsPath { get; set; } = string.Empty;
		public string Build { get; set; } = string.Empty;
		public string MpqCliPath { get; set; } = string.Empty;

		public bool IsValid => !string.IsNullOrWhiteSpace(PatchMpq) && 
		                       !string.IsNullOrWhiteSpace(DefsPath);

		public static ScanMpqArgs Parse(string[] args, string defaultBuild = "3.3.5.12340")
		{
			var localeMpqsArg = Helpers.GetArg(args, "--locale-mpqs");
			var localeMpqArg = Helpers.GetArg(args, "--locale-mpq");

			var localeMpqs = new List<string>();
			if (!string.IsNullOrWhiteSpace(localeMpqsArg))
			{
				localeMpqs = Helpers.ParseDbcList(localeMpqsArg);
			}
			else if (!string.IsNullOrWhiteSpace(localeMpqArg))
			{
				localeMpqs.Add(localeMpqArg);
			}

			return new ScanMpqArgs
			{
				PatchMpq = Helpers.GetArg(args, "--patch") ?? string.Empty,
				LocaleMpq = localeMpqArg ?? string.Empty,
				LocaleMpqs = localeMpqs,
				DefsPath = Helpers.GetArg(args, "--defs") ?? string.Empty,
				Build = Helpers.GetArg(args, "--build") ?? defaultBuild,
				MpqCliPath = Helpers.GetArg(args, "--mpqcli") ?? MpqHelper.GetDefaultMpqCliPath()
			};
		}
	}
}
