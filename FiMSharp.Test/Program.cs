using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
#if DEBUG
using System.Linq;
#endif

namespace FiMSharp.Test
{
	class Program
	{
		public static FiMReport GetReport(string path)
		{
			FiMReport report = FiMReport.FromFile(path);
			FiMSharp.CLI.Program.AddExperimentalFunctions(report);
			return report;
		}
		static void RunReport(string file)
		{
			string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string path = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\Reports", file));
			var report = GetReport(path);
			report.MainParagraph?.Execute();
		}
		static void RunDebugReport()
		{
			string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string path = Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\", "debug.fim"));
			var report = GetReport(path);
			report.MainParagraph?.Execute();
		}

		static void Main(string[] args)
		{
#if RELEASE
			ReportTests.RunAll();
#else
			// RunReport("rot13.fim");
			ReportTests.RunAll();
			/*if (args.Any(a => a == "--test-basic")) ReportTests.RunBasic();
			else if (args.Any(a => a == "--test-all")) ReportTests.RunAll();
			else RunDebugReport();*/
#endif
		}
	}
	class ReportTests
	{
		public static void RunBasic()
		{
			Test("array.fim", new string[] { "Banana Cake", "Gala" });
			Test("conditional.fim", new string[] {
				"true == true",
				"true == true && false == true",
				"true == false || true == true",
				"(true == false || false != true) && true == true",
				"(false == false || false == true) && true != true",
				"(false == false || true == true) && (true == true || true == false)"
			});
			Test("for loops.fim");
			Test("hello.fim", new string[] { "Hello World!" });
			Test("input.fim", new string[] { "Hello World!" }, new string[] { "Hello World!" });
			Test("multiple parameters.fim", new string[] { "x", "1", "y", "0" });
			Test("string index.fim", new string[] { "T", "w" });
			Test("switch.fim", new string[] {
				"That's impossible!",
				"There must be a scientific explanation",
				"There must be an explanation",
				"Why does this happen?!",
				"She's just being Pinkie Pie."
			});
		}
		public static void RunAll()
		{
			Test("array.fim", new string[] { "Banana Cake", "Gala" });
			Test("brainfuck.fim", new string[] { "Hello World!" });
			Test("bubblesort.fim", new string[] { "1", "2", "3", "4", "5", "7", "7" });
			Test("cider.fim");
			Test("conditional.fim", new string[] {
				"true == true",
				"true == true && false == true",
				"true == false || true == true",
				"(true == false || false != true) && true == true",
				"(false == false || false == true) && true != true",
				"(false == false || true == true) && (true == true || true == false)"
			});
			Test("deadfish.fim", new string[] { "Hello world" });
			Test("digital root.fim", new string[] { "9" });
			Test("disan.fim", new string[] {
				"Insert a number",
				"0 is divisible by 2!",
				"2 is divisible by 2!",
				"4 is divisible by 2!"
			}, new string[] { "5" });
			// e.fim
			Test("eratosthenes.fim", new string[] { "2", "3", "5", "7", "11", "13", "17", "19" });
			Test("factorial.fim", new string[] { "120" });
			Test("fibonacci.fim", new string[] { "34" });
			Test("fizzbuzz.fim");
			Test("for loops.fim");
			Test("hello.fim", new string[] { "Hello World!" });
			Test("input.fim", new string[] { "Hello World!" }, new string[] { "Hello World!" });
			Test("insertionsort.fim", new string[] { "1", "2", "3", "4", "5", "7", "7" });
			Test("mississippis.fim");
			Test("multiple parameters.fim", new string[] { "x", "1", "y", "0" });
			Test("quicksort.fim", new string[] { "1", "2", "3", "4", "5", "7", "7" });
			Test("recursion.fim", new string[] { "5", "4", "3", "2", "1" });
			Test("rot13.fim", new string[] { "Hello World!", "Uryyb Jbeyq!", "Hello World!" });
			Test("string index.fim", new string[] { "T", "w" });
			Test("sum.fim", new string[] { "5051" });
			Test("switch.fim", new string[] {
				"That's impossible!",
				"There must be a scientific explanation",
				"There must be an explanation",
				"Why does this happen?!",
				"She's just being Pinkie Pie."
			});
			// truth machine.fim
		}

		static string GetPathFromDir(string path)
		{
			string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
			return Path.GetFullPath(Path.Combine(currentDirectory, @"..\..\..\", path));
		}

		class EmptyWriter : TextWriter
		{
			public override void Write(string value) { }
			public override void WriteLine(string value) { }

			public override Encoding Encoding
			{
				get { return Encoding.Default; }
			}
		}
		class TestWriter : TextWriter
		{
			public delegate void OnWrite(string msg);
			public OnWrite onWriteEvent;
			public override void Write(string value)
			{
				onWriteEvent?.Invoke(value);
			}
			public override void WriteLine(string value)
			{
				onWriteEvent?.Invoke(value);
			}

			public override Encoding Encoding
			{
				get { return Encoding.Default; }
			}
		}
		class TestReader : TextReader
		{
			public delegate string OnInput();
			public OnInput onInputEvent;
			public override string ReadLine()
			{
				return onInputEvent.Invoke();
			}
		}
		/// <summary>
		/// Test if a report runs without throwing an error, also checks its outputs.
		/// </summary>
		public static void Test(string file, string[] expected, string[] input = null)
		{
			var w = new TestWriter();
			int wI = 0;
			List<string> errors = new List<string>();
			w.onWriteEvent += (m) =>
			{
				if (wI >= expected.Length)
				{
					errors.Add($"Report outputted more messages than expected ({m})");
					return;
				}
				if (m != expected[wI]) errors.Add($"Got '{m}', expected '{expected[wI]}'");
				wI++;
			};
			var r = new TestReader();
			int rI = 0;
			r.onInputEvent += () =>
			{
				if (input == null) throw new Exception("Report asked for input while input is null");
				string inp = input[rI];
				rI++;
				return inp;
			};

			try
			{
				var report = Program.GetReport(GetPathFromDir($"Reports/{ file }"));
				report.ConsoleOutput = w;
				report.ConsoleInput = r;
				Console.Write($"Running report '{file}'... ");
				report.MainParagraph?.Execute();
			}
			catch (Exception err)
			{
				Console.WriteLine("failed");
				throw new Exception("Report has thrown an error:\n\n" + err.ToString());
			}

			if (errors.Count > 0)
			{
				Console.WriteLine("failed");
				throw new Exception($"Report '{file}' output contains errors:\n\n{string.Join("\n", errors)}");
			}

			Console.WriteLine("passes");
		}
		/// <summary>
		/// Test if a report runs without throwing an error
		/// </summary>
		public static void Test(string file)
		{
			try
			{
				var report = Program.GetReport(GetPathFromDir($"Reports/{ file }"));
				report.ConsoleOutput = new EmptyWriter();
				Console.Write($"Running report '{file}'... ");
				report.MainParagraph?.Execute();
			}
			catch (Exception err)
			{
				Console.WriteLine("failed");
				throw new Exception($"Report '{file}' has thrown an error:\n\n{err.ToString()}");
			}

			Console.WriteLine("passes");
		}
	}
}
