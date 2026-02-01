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

			// If no arguments, try to load from config file for automatic mode
			if (args.Length == 0)
			{
				if (File.Exists(DefaultConfigFile))
				{
					Logger.Info("[*] No arguments provided. Loading from config.json...");
					args = ConfigLoader.LoadConfigAsArgs(DefaultConfigFile);
					if (args.Length == 0)
					{
						UsageWriter.PrintUsage();
						return 1;
					}
				}
				else
				{
					UsageWriter.PrintUsage();
					return 1;
				}
			}

			if (args.Contains("--help") || args.Contains("-h"))
			{
				UsageWriter.PrintUsage();
				return 1;
			}

			try
			{
				var command = args[0].ToLowerInvariant();
				var cmdArgs = args.Skip(1).ToArray();
				
				return command switch
				{
					"localize" => LocalizeCommandHandler.Execute(cmdArgs),
					"localize-mpq" => LocalizeMpqCommandHandler.Execute(cmdArgs),
					"scan-mpq" => ScanMpqCommandHandler.Execute(cmdArgs),

					_ => UnknownCommand(command)
				};
			}
			catch (Exception ex)
			{
				Logger.Error(ex.ToString());
				return 1;
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
	}
}














