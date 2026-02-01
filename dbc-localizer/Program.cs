using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DbcLocalizer
{
	internal static class Program
	{
		private const string DefaultConfigFile = "config.json";

		private static int Main(string[] args)
		{
			RegisterLibResolver();

			var exitCode = 0;

			// If --help is requested, show usage
			if (args.Contains("--help") || args.Contains("-h"))
			{
				UsageWriter.PrintUsage();
				return MaybePauseOnError(0);
			}

			// If no arguments, default to localize-mpq with auto-detection
			if (args.Length == 0)
			{
				Logger.Info("[*] No arguments provided, using default: localize-mpq with auto-detection");
				return MaybePauseOnError(LocalizeMpqCommandHandler.Execute(new string[0]));
			}

			// If only flags, try to load from config file for automatic mode
			if (args.Length > 0 && args[0].StartsWith("--"))
			{
				if (File.Exists(DefaultConfigFile))
				{
					Logger.Info("[*] Loading from config.json...");
					var configArgs = ConfigLoader.LoadConfigAsArgs(DefaultConfigFile);
					if (configArgs.Length == 0)
					{
						UsageWriter.PrintUsage();
						exitCode = 1;
						return MaybePauseOnError(exitCode);
					}
					// Append remaining arguments (like --verbose) to config arguments
					args = configArgs.Concat(args).ToArray();
				}
				else
				{
					UsageWriter.PrintUsage();
					exitCode = 1;
					return MaybePauseOnError(exitCode);
				}
			}

			try
			{
				var command = args[0].ToLowerInvariant();
				var cmdArgs = args.Skip(1).ToArray();
				
				exitCode = command switch
				{

					"localize-mpq" => LocalizeMpqCommandHandler.Execute(cmdArgs),
					"scan-mpq" => ScanMpqCommandHandler.Execute(cmdArgs),
					"verify-dbc" => VerifyDbcCommandHandler.Execute(cmdArgs),

					_ => UnknownCommand(command)
				};
				return MaybePauseOnError(exitCode);
			}
			catch (Exception ex)
			{
				Logger.Error(ex.ToString());
				exitCode = 1;
				return MaybePauseOnError(exitCode);
			}
		}

		private static void RegisterLibResolver()
		{
			var baseDir = AppContext.BaseDirectory;
			var libDir = Path.Combine(baseDir, "lib");
			if (!Directory.Exists(libDir))
				return;

			AppDomain.CurrentDomain.AssemblyResolve += (_, evt) =>
			{
				try
				{
					var name = new AssemblyName(evt.Name).Name + ".dll";
					var candidate = Path.Combine(libDir, name);
					if (File.Exists(candidate))
						return Assembly.LoadFrom(candidate);
				}
				catch
				{
					// ignore and let default resolver handle it
				}

				return null;
			};
		}

		private static int UnknownCommand(string command)
		{
			Logger.Error($"Unknown command: {command}");
			return 1;
		}

		private static int MaybePauseOnError(int exitCode)
		{
			if (exitCode != 0 && Environment.UserInteractive)
			{
				Console.WriteLine("\nPress any key to exit...");
				Console.ReadKey(true);
			}
			return exitCode;
		}
	}
}














