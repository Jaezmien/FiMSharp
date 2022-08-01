using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using FiMSharp.Kirin;

namespace FiMSharp
{
	public class FiMReport
	{
		public FiMReport(string report)
		{
			this.Variables = new FiMVariableList();
			this.Paragraphs = new List<FiMParagraph>();
			this.Report = report.Replace("\r\n", "\n");

			var tree = FiMLexer.ParseReport(this, this.Report);

			foreach( var node in tree.Body )
			{
				if( node.NodeType == "KirinFunction" )
				{
					var n = node as KirinFunction;
					if( n.IsMain )
					{
						if (this._MainParagraph != string.Empty) throw new Exception("Multiple main methods found");
						this._MainParagraph = n.Name;
					}
					this.Paragraphs.Add(new FiMParagraph(this, n));
				}
				if( node.NodeType == "KirinVariableDeclaration" )
				{
					((KirinVariableDeclaration)node).Execute(this);
				}
			}

			this.KirinTree = tree;

			//

			this.AddMethod("convert a number to char", new Func<double, char>((value) =>
			{
				return (char)value;
			}));
			this.AddMethod("convert a char to num", new Func<char, double>((value) =>
			{
				return (double)char.Parse(value.ToString());
			}));
			this.AddMethod("convert a number to literal string", new Func<double, string>((value) =>
			{
				return value.ToString();
			}));
			this.AddMethod("convert a char to literal num", new Func<char, double>((value) =>
			{
				return int.Parse(value.ToString());
			}));
			this.AddMethod("square root of a num", new Func<double, double>((value) =>
			{
				return Math.Sqrt(value);
			}));
		}
		public static FiMReport FromFile(string directory)
			=> new FiMReport(File.ReadAllText(Path.GetFullPath(directory)));

		public FiMReportInfo Info;
		public FiMReportAuthor Author;

		public string Report;
		public KirinProgram KirinTree;

		public FiMVariableList Variables;
		public List<FiMParagraph> Paragraphs;
		private readonly string _MainParagraph = string.Empty;
		public FiMParagraph MainParagraph
		{
			get
			{
				if (string.IsNullOrEmpty(this._MainParagraph)) return null;
				return this.Paragraphs[ this.Paragraphs.FindIndex(p => p.Name == this._MainParagraph) ];
			}
		}

		public TextWriter ConsoleOutput = Console.Out;
		public TextReader ConsoleInput = Console.In;

		public void AddVariable(string name, object value)
		{
			if (this.Variables.Exists(name))
				throw new Exception("Variable " + Variables + " already exists");
			this.Variables.PushGlobalVarible(new FiMVariable(name, value));
		}
		public void AddMethod(string name, Delegate func)
		{
			var node = new KirinInternalFunction(name, func);
			if (this.Paragraphs.FindIndex(p => p.Name == node.Name) != -1)
				throw new Exception("Paragraph " + node.Name + " already exists");
			this.Paragraphs.Add(new FiMParagraph(this, node));
		}

		public string GetLine(int start, int length)
		{
			return this.Report.Substring(start, length);
		}
	}

	public class FiMReportInfo : KirinNode
	{
		public FiMReportInfo(int start, int length) : base(start, length) { }
		public string Name;
		public string Recipient;
	}
	public class FiMReportAuthor : KirinNode
	{
		public FiMReportAuthor(int start, int length) : base(start, length) { }
		public string Name;
		public string Role;
	}
}
