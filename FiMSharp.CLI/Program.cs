using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Options;

namespace FiMSharp.CLI
{
	class Program
	{
		static bool FileExists(string path)
		{
			return File.Exists(Path.GetFullPath(path));
		}
		static string GetVersion()
		{
			var version = Assembly.GetAssembly(typeof(FiMReport)).GetName().Version;
			return $"{version.Major}.{version.Minor}.{version.Build}";
		}
		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				string report_path = "";
				bool pretty = false;
				bool show_help = false;

				OptionSet p = new OptionSet()
					.Add("p|prettify", "Prettify console output.", v => pretty = true)
					.Add("h|help", "Show this message and exit.", v => show_help = true);
				List<string> extra = p.Parse(args);

				if (show_help)
				{
					if( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) )
						Console.WriteLine("Usage: fim [options] report_path");
					else
						Console.WriteLine("Usage: ./fim [options] report_path");

					Console.WriteLine("Interprets the specified FiM++ report.");
					Console.WriteLine();
					Console.WriteLine("Options:");
					p.WriteOptionDescriptions(Console.Out);
					return;
				}

				if (extra.Count > 0) report_path = extra[0];
				if (string.IsNullOrWhiteSpace(report_path) || !FileExists(report_path))
					throw new FileNotFoundException($"Cannot find report '{ report_path }'");

#if DEBUG
				var debugTime = new System.Diagnostics.Stopwatch();
				debugTime.Start();
#endif

				FiMReport report = FiMReport.FromFile(report_path);

				if (pretty)
				{
					Console.WriteLine($"[ FiMSharp v{GetVersion()} ]");
					Console.WriteLine($"Report Name: {report.Info.Name}");
					Console.WriteLine($"Student Name: {report.Author.Name}");
					Console.WriteLine("[@]===[@]");
				}

				report.MainParagraph?.Execute();

				if (pretty)
				{
					Console.WriteLine("[@]===[@]");
				}

#if DEBUG
				Console.WriteLine($"[Debug] Code execution took {debugTime.Elapsed:d\\.hh\\:mm\\:ss\\:fff}.");
#endif
			}
		}
	}
}
