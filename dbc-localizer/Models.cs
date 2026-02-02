using System.Collections.Generic;

namespace DbcLocalizer
{
	internal enum LogLevel
	{
		Info = 1,
		Verbose = 2
	}

	internal sealed class LocalizeStats
	{
		public int RowsMerged { get; set; }
		public int FieldUpdates { get; set; }
		public List<VerificationTestCase> TestCases { get; set; } = new();
	}

	internal sealed class SampleChange
	{
		public int ID { get; set; }
		public string Field { get; set; } = string.Empty;
		public string Value_enUS { get; set; } = string.Empty;
		public string Value_Locale_Before { get; set; } = string.Empty;
		public string Value_Locale_After { get; set; } = string.Empty;
	}

	internal sealed class VerificationTestCase
	{
		public int RecordId { get; set; }
		public string TestType { get; set; } = string.Empty;
		public Dictionary<string, string> ExpectedValues { get; set; } = new();
	}

	internal sealed class VerificationResult
	{
		public string TableName { get; set; } = string.Empty;
		public int TestCasesTotal { get; set; }
		public int TestCasesPassed { get; set; }
		public int TestCasesFailed { get; set; }
		public List<string> FailureDetails { get; set; } = new();
	}
}
