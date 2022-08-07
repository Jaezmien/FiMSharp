using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Reflection;
using Mono.Options;

namespace FiMSharp.CLI
{
	public class Program
	{
		public static void AddExperimentalFunctions(FiMReport report)
		{
			report.AddParagraph("count of an array", new Func<IDictionary, double>((value) =>
			{
				return value.Keys.Count;
			}));
			report.AddParagraph("length of a string", new Func<string, double>((value) =>
			{
				return value.Length;
			}));
			report.AddParagraph("convert a number to char", new Func<double, char>((value) =>
			{
				return (char)value;
			}));
			report.AddParagraph("convert a char to num", new Func<char, double>((value) =>
			{
				return (double)char.Parse(value.ToString());
			}));
			report.AddParagraph("convert a number to literal string", new Func<double, string>((value) =>
			{
				return value.ToString();
			}));
			report.AddParagraph("convert a char to literal num", new Func<char, double>((value) =>
			{
				return int.Parse(value.ToString());
			}));
			report.AddParagraph("square root of a num", new Func<double, double>((value) =>
			{
				return Math.Sqrt(value);
			}));
		}
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
				bool experimental = false;
				bool js = false;

				OptionSet p = new OptionSet()
					.Add("p|pretty", "Prettify console output.", v => pretty = true)
					.Add("h|help", "Show this message and exit.", v => show_help = true)
					.Add("e|experimental", "Add experimental functions", v => experimental = true)
					.Add("j|javascript", "Convert file to Javascript", v => js = true);
				List<string> extra = p.Parse(args);

				if (show_help)
				{
					if( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) )
						Console.WriteLine("Usage: fim [options] <report_path>");
					else
						Console.WriteLine("Usage: ./fim [options] <report_path>");

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

				if (experimental) AddExperimentalFunctions(report);

				if ( js )
				{
					string filename = Path.GetFileNameWithoutExtension(report_path);
					string old_path = Path.GetPathRoot(report_path);
					string new_path = Path.GetFullPath(Path.Combine(old_path, filename + ".js"));

					File.WriteAllText(
						new_path,
						Changeling.Javascript.Transpile(
							report,
							onInternalFunction: (name) =>
							{
								var func = new Changeling.Javascript.JavascriptInternalFunction()
								{
									Name = "",
									Function = ""
								};

								switch (name)
								{
									case "count of an array":
										{
											func.Name = "fim_count";
											func.Function = "function fim_count(a){return a.filter(x=>x).length}";
										}
										break;
									case "length of a string":
										{
											func.Name = "fim_length";
											func.Function = "function fim_length(s){return s.length}";
										}
										break;
									case "convert a number to char":
										{
											func.Name = "fim_ntc";
											func.Function = "function fim_ntc(n){return String.fromCharCode(n)}";
										}
										break;
									case "convert a char to num":
										{
											func.Name = "fim_ctn";
											func.Function = "function fim_ctn(c){return c.charCodeAt(0)}";
										}
										break;
									case "convert a number to literal string":
										{
											func.Name = "fim_ntls";
											func.Function = "function fim_ntls(n){return n+''}";
										}
										break;
									case "convert a char to literal num":
										{
											func.Name = "fim_ctln";
											func.Function = "function fim_ctln(c){return +c}";
										}
										break;
									case "square root of a num":
										{
											func.Name = "fim_sqrt";
											func.Function = "function fim_sqrt(n){return Math.sqrt(n)}";
										}
										break;

								}

								return func;
							}
						)
					);
				}

				else
				{
					report.Output = (l) => Console.Write(l);
					report.Input = (p, n) =>
					{
						if (string.IsNullOrEmpty(p))
							Console.Write($"{n} is asking for an input: ");
						else
							Console.Write(p);

						return Console.ReadLine();
					};

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
				}

#if DEBUG
				Console.WriteLine($"[Debug] Code execution took {debugTime.Elapsed:d\\.hh\\:mm\\:ss\\:fff}.");
#endif
			}
		}
	}
}
