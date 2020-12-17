using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

using FiMSharp;

namespace FiMSharp.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TestingMain(args);
        }
        static object TestingMain(string[] args)
        {
            if (args.Length > 0)
            {
                // Use args
                var Parser = new ArgsParser();
                Parser.Parse(args);

                if( Parser["report"] == null || !File.Exists("Reports/"+ (string)Parser["report"] + ".fim") )
                {
                    Console.WriteLine("[Console] Invalid report");
                    return 1;
                }

                string[] report_lines = File.ReadAllLines( "Reports/"+ (string)Parser["report"] + ".fim" );

                Stopwatch s = new Stopwatch();
                s.Start();
                FiMReport report = new FiMReport( report_lines );
                s.Stop();
                Console.WriteLine("[Debug] Code execution took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));

                /*Console.WriteLine(
                    FiMArithmetic.Evaluate(1,"+",1)
                );*/

                return 0;
            }
            else
            {
                Console.WriteLine("[?] Input the filename of the report you wish to run");
                while(true) {
                    Console.Write("> ");
                    string report_name = Console.ReadLine();
                    bool show = false;

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
                    if( report_name.StartsWith(".show " ) ) {
                        show = true;
                        report_name = report_name.Substring(".show ".Length);
                    }

                    if( !File.Exists("Reports/"+ report_name + ".fim") )
                    {
                        Console.WriteLine("Invalid report");
                        continue;
                    }

                    string[] report_lines = File.ReadAllLines( "Reports/"+ report_name + ".fim" );

                    if( show ) {
                        foreach(string line in report_lines) Console.WriteLine(line);
                        goto End;
                    }

                    Stopwatch s = new Stopwatch();
                    s.Start();
                    FiMReport report = new FiMReport( report_lines );
                    s.Stop();
                    Console.WriteLine("[Debug] Code execution took " + s.Elapsed.ToString(@"d\.hh\:mm\:ss\:fff"));

                    End:
                    Console.WriteLine();
                    Console.WriteLine();
                }

                return 0;
            }
        }
    }
}
