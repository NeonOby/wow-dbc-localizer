using System.Collections.Generic;

namespace DbcMerger
{
	internal enum LogLevel
	{
		Info = 1,
		Verbose = 2
	}

	internal sealed class MergeStats
	{
		public int RowsMerged { get; set; }
		public int FieldUpdates { get; set; }
	}

	internal sealed class MergeReport
	{
		public string TimestampUtc { get; set; } = string.Empty;
		public string Build { get; set; } = string.Empty;
		public string PatchMpq { get; set; } = string.Empty;
		public string OutputMpq { get; set; } = string.Empty;
		public List<string> LocaleMpqs { get; set; } = new();
		public List<string> Languages { get; set; } = new();
		public List<string> SelectedDbcs { get; set; } = new();
		public List<string> Warnings { get; set; } = new();
		public int TotalTablesMerged { get; set; }
		public int TotalRowsMerged { get; set; }
		public int TotalFieldsUpdated { get; set; }
		public List<LocaleMergeResult> PerLocale { get; set; } = new();
	}

	internal sealed class LocaleMergeResult
	{
		public string LocaleMpq { get; set; } = string.Empty;
		public string Language { get; set; } = string.Empty;
		public int TablesMerged { get; set; }
		public int RowsMerged { get; set; }
		public int FieldsUpdated { get; set; }
	}
}
