#define TOJS

using System;
using System.IO;
using System.Diagnostics;

using FiMSharp;
using FiMSharp.Javascript;

using Mono.Options;

namespace FiMSharpTest
{
    class Program
    {
        static void Main(params string[] args)
        {
            TestingMain(args);
        }

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
            string[] file_extensions = {".fim", ".fpp", ".fimpp"};
            foreach(string ext in file_extensions) {
                if( File.Exists("Reports/"+ report_name + ext ) ) {
                    lines = File.ReadAllLines( "Reports/"+ report_name + ext );
                    return true;
                }
            }
            lines = new string[] {};
            return false;
        }
        static object TestingMain(string[] args)
        {
            if (args.Length > 0)
            {
                string report_name = "";
                bool toJS = false;

                OptionSet p = new OptionSet()
                    .Add("report=", v => report_name = v)
                    .Add("js", v => toJS = true);
                p.Parse(args);

                if( string.IsNullOrEmpty(report_name) || !FindReport(report_name, out string[] report_lines) )
                {
                    Console.WriteLine("[Console] Invalid report " + report_name);
                    return 1;
                }

                Stopwatch s = new Stopwatch();
                s.Start();

                if( toJS ) {
                    if (!Directory.Exists("Build/")) Directory.CreateDirectory("Build/");
                    string[] new_lines = CompileReport(report_lines);
                    if (!File.Exists("Build/" + report_name + ".js")) File.Create("Build/" + report_name + ".js").Close();
                    File.WriteAllLines("Build/" + report_name + ".js", new_lines);
                    Console.WriteLine("[Debug] Code compilation took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
                }
                else {
                    ExecuteReport(report_lines);
                    s.Stop();
                    Console.WriteLine("[Debug] Code execution took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
                }

                return 0;
            }
            else
            {
                Console.WriteLine("[?] Input the filename of the report you wish to run");
                while(true) {
                    Console.Write("> ");
                    string report_name = Console.ReadLine();
                    bool show = false;
                    bool js = false;

                    if( report_name == ".list" ) {
                        foreach(string path in Directory.GetFiles("Reports/")) {
                            Console.WriteLine(path);
                        }
                        goto End;
                    }
                    if( report_name == ".clear" ) {
                        Console.Clear();
                        continue;
                    }
                    if( report_name == ".exit" ) {
                        break;
                    }
                    if( report_name == ".help" )
                    {
                        Console.WriteLine("[FiMTest] All available commands");
                        Console.WriteLine(".list - Lists all available reports from the \"Reports/\" folder");
                        Console.WriteLine(".clear - Clears the console screen");
                        Console.WriteLine(".exit - Exits the program");
                        Console.WriteLine(".show [report name] - Prints out the report into the console");
                        Console.WriteLine(".help - Shows all available commands");
                        Console.WriteLine(".js [report name] - Compiles report to Javascript instead");
                        continue;
                    }

                    if( report_name.StartsWith(".show " ) ) {
                        show = true;
                        report_name = report_name.Substring(".show ".Length);
                    }
                    else if( report_name.StartsWith(".js " ) ) {
                        js = true;
                        report_name = report_name.Substring(".js ".Length);
                    }

                    if( string.IsNullOrEmpty(report_name) || !FindReport(report_name, out string[] report_lines) )
                    {
                        Console.WriteLine("[Console] Invalid report " + report_name);
                        continue;
                    }

                    if( show ) {
                        foreach(string line in report_lines) Console.WriteLine(line);
                        goto End;
                    }

                    Stopwatch s = new Stopwatch();
                    s.Start();
                    
                    if( js ) {
                        if (!Directory.Exists("Build/")) Directory.CreateDirectory("Build/");
                        string[] new_lines = CompileReport(report_lines);
                        if (!File.Exists("Build/" + report_name + ".js")) File.Create("Build/" + report_name + ".js").Close();
                        File.WriteAllLines("Build/" + report_name + ".js", new_lines);

                        s.Stop();
                        Console.WriteLine("[Debug] Code compilation took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
                    } else {
                        ExecuteReport(report_lines);

                        s.Stop();
                        Console.WriteLine("[Debug] Code execution took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));
                    }

                    End:
                    Console.WriteLine();
                    Console.WriteLine();
                }

                return 0;
            }
        }
    }
}
