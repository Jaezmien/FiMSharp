#define TOJS

using System;
using System.IO;
using System.Linq;
using System.Diagnostics;

using FiMSharp;
using FiMSharp.Javascript;

using Mono.Options;

namespace FiMSharpTest
{
    class Program
    {
        static void Main(params string[] args) { TestingMain(args); }

        public static void ExecuteReport( string[] lines )
        {
            try
            {
                FiMReport report = new FiMReport(lines);
                Console.WriteLine("[ FiMSharp Test v0.3 ]");
                Console.WriteLine($"Report Name: {report.ReportName}");
                Console.WriteLine($"Student Name: {report.StudentName}");
                Console.WriteLine("[@]=======================================[@]");
                report.MainParagraph.Execute();
                Console.WriteLine("[@]=======================================[@]");
            }
            catch ( FiMException exception )
            {
                Console.WriteLine(exception.Message);
            }
        }
        public static string[] CompileReport( string[] lines ) {
            FiMReport report = new FiMReport(lines);
            return FiMJavascript.Parse(report);
        }

        public static bool FindReport( string report_name, out string[] lines ) {
            if( File.Exists(report_name) ) {
                lines = File.ReadAllLines( report_name );
                return true;
            }
            lines = new string[] {};
            return false;
        }
        static object TestingMain(string[] args)
        {
            if (args.Length > 0)
            {
                string report_name = "";
                string toJS = "";

                OptionSet p = new OptionSet()
                    .Add("r|report=", "The directory to the report.", v => report_name = v)
                    .Add("js=", "Converts the report into a Javascript file and outputs into a directory.", v => toJS = v)
                    .Add("tojs", "Converts the report into a Javascript file.", v => toJS = ".");
                p.Parse(args);

                if( string.IsNullOrEmpty(report_name) || !FindReport(report_name, out string[] report_lines) )
                {
                    Console.WriteLine("[Console] Invalid report " + report_name);
                    return 1;
                }
                string report_filename = Path.GetFileNameWithoutExtension(report_name);

                Stopwatch s = new Stopwatch();
                s.Start();

                if( toJS.Length > 0 ) {
                    string[] new_lines = CompileReport(report_lines);

                    string new_directory = "";
                    if( toJS.EndsWith(".js") ) new_directory = toJS;
                    else {
                        if( !Directory.Exists(toJS) ) {
                            Console.WriteLine( $"[Console] Invalid directory '{toJS}'" );
                            return 1;
                        }
                        new_directory = (toJS.EndsWith("/") ? toJS.Substring(0, toJS.Length-1) : toJS) + "/" + report_filename + ".js";
                    }

                    if (!File.Exists(new_directory)) File.Create(new_directory).Close();
                    File.WriteAllLines(new_directory, new_lines);
                    Console.WriteLine("[Debug] Code compilation took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
                }
                else {
                    ExecuteReport(report_lines);
                    s.Stop();
                    Console.WriteLine("[Debug] Code execution took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
                }

                return 0;
            }
            return 0;
        }
    }
}
