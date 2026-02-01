using System;
using System.Linq;

namespace DbcLocalizer
{
	internal static class Logger
	{
		private static LogLevel _logLevel = LogLevel.Info;

		public static void SetLogLevel(string[] args)
		{
			var levelArg = Helpers.GetArg(args, "--log-level") ?? Helpers.GetArg(args, "--log");
			if (args.Any(a => a.Equals("--verbose", StringComparison.OrdinalIgnoreCase)))
			{
				_logLevel = LogLevel.Verbose;
				return;
			}

			if (!string.IsNullOrWhiteSpace(levelArg) && levelArg.Equals("verbose", StringComparison.OrdinalIgnoreCase))
			{
				_logLevel = LogLevel.Verbose;
				return;
			}

			_logLevel = LogLevel.Info;
		}

		public static void Info(string message)
		{
			if (_logLevel >= LogLevel.Info)
				Console.WriteLine(message);
		}

		public static void Verbose(string message)
		{
			if (_logLevel >= LogLevel.Verbose)
				Console.WriteLine(message);
		}

		public static void Error(string message)
		{
			Console.Error.WriteLine($"ERROR: {message}");
		}
	}
}
