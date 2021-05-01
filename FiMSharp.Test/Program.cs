#define TOJS

using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using FiMSharp;
using FiMSharp.Javascript;

using Mono.Options;

namespace FiMSharpTest
{
	class Program
	{
		static void Main(params string[] args) { TestingMain(args); }

		public static void ExecuteReport(string[] lines, bool prettify)
		{
			try
			{
				FiMReport report = new FiMReport(lines);
				if (prettify)
				{
					Console.WriteLine("[ FiMSharp Test v0.3.2 ]");
					Console.WriteLine($"Report Name: {report.ReportName}");
					Console.WriteLine($"Student Name: {report.StudentName}");
					Console.WriteLine("[@]=======================================[@]");
				}
				report.MainParagraph.Execute();
				if (prettify)
				{
					Console.WriteLine("[@]=======================================[@]");
				}
			}
			catch (FiMException exception)
			{
				Console.WriteLine(exception.Message);
			}
		}
		public static string[] CompileReport(string[] lines)
		{
			FiMReport report = new FiMReport(lines);
			return FiMJavascript.Parse(report);
		}

		public static bool FindReport(string report_name, out string[] lines)
		{
			if (File.Exists(report_name))
			{
				lines = File.ReadAllLines(report_name);
				return true;
			}
			lines = new string[] { };
			return false;
		}
		static object TestingMain(string[] args)
		{
			if (args.Length > 0)
			{
				string report_name = "";
				string toJS = "";
				bool prettify = false;
				bool show_help = false;

				OptionSet p = new OptionSet()
					.Add("js=", "Converts the report into a Javascript file and outputs into {DIRECTORY}.", v => toJS = v)
					.Add("tojs", "Converts the report into a Javascript file.", v => toJS = ".")
					.Add("p|prettify", "Prettify console output.", v => prettify = true)
					.Add("h|help", "Show this message and exit.", v => show_help = true);
				List<string> extra = p.Parse(args);

				if (show_help)
				{
					Console.WriteLine("Usage: FiMSharp.Test.exe [OPTIONS]+ reportDirectory");
					Console.WriteLine("Interprets the specified FiM++ report.");
					Console.WriteLine();
					Console.WriteLine("Options:");
					p.WriteOptionDescriptions(Console.Out);
					return 0;
				}

				if (extra.Count > 0) report_name = extra[0];
				if (string.IsNullOrEmpty(report_name) || !FindReport(report_name, out string[] report_lines))
				{
					Console.WriteLine("[Console] Invalid report " + report_name);
					return 1;
				}

				try
				{
					report_name = Path.GetFullPath(report_name);
				}
				catch (UriFormatException)
				{
					Console.WriteLine("[Console] Invalid report path " + report_name);
					return 1;
				}
				catch (Exception)
				{
					Console.WriteLine("[Console] Error while parsing report path " + report_name);
					return 1;
				}

				string report_filename = Path.GetFileNameWithoutExtension(report_name);

				Stopwatch s = new Stopwatch();
				s.Start();

				if (toJS.Length > 0)
				{
					string[] new_lines = CompileReport(report_lines);

					string new_directory = "";
					if (toJS.EndsWith(".js")) new_directory = toJS;
					else
					{
						if (!Directory.Exists(toJS))
						{
							Console.WriteLine($"[Console] Invalid directory '{toJS}'");
							return 1;
						}
						new_directory = (toJS.EndsWith("/") ? toJS.Substring(0, toJS.Length - 1) : toJS) + "/" + report_filename + ".js";
					}

					try
					{
						new_directory = Path.GetFullPath(new_directory);
					}
					catch (UriFormatException)
					{
						Console.WriteLine("[Console] Invalid js output path " + new_directory);
						return 1;
					}
					catch (Exception)
					{
						Console.WriteLine("[Console] Error while parsing js output path " + new_directory);
						return 1;
					}

					if (!File.Exists(new_directory)) File.Create(new_directory).Close();
					File.WriteAllLines(new_directory, new_lines);
					if (prettify) Console.WriteLine("[Debug] Code compilation took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
				}
				else
				{
					ExecuteReport(report_lines, prettify);
					s.Stop();
					if (prettify) Console.WriteLine("[Debug] Code execution took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
				}

				return 0;
			}
			return 0;
		}
	}
}
