using System;
using System.IO;
using FiMSharp.Kirin;

namespace FiMSharp
{
	public class FiMReport : FiMClass
	{
		public FiMReport(string report) : base("I", true, null, null)
		{
			this.Report = this;
			this.ReportString = report.Replace("\r\n", "\n");

			var tree = FiMLexer.ParseReport(this, this.ReportString);

			foreach( var node in tree.Body )
			{
				if( node.NodeType == "KirinFunction" )
				{
					var n = node as KirinFunction;

					if( n.Today )
					{
						if (this._MainParagraph != string.Empty) throw new Exception("Multiple main methods found");
						this._MainParagraph = n.Name;
					}

					this.AddParagraph(new FiMParagraph(this, n));
				}
				if( node.NodeType == "KirinVariableDeclaration" )
				{
					((KirinVariableDeclaration)node).Execute(this, true);
				}
			}

			this.KirinTree = tree;
		}
		public static FiMReport FromFile(string directory)
			=> new FiMReport(File.ReadAllText(Path.GetFullPath(directory)));

		public string ReportString;

		public FiMReportInfo Info;
		public FiMReportAuthor Author;

		public KirinProgram KirinTree;

		internal string _MainParagraph = string.Empty;
		public FiMParagraph MainParagraph
		{
			get
			{
				if (string.IsNullOrEmpty(this._MainParagraph)) return null;
				return this.GetParagraph(this._MainParagraph, propagate: false);
			}
		}

		public TextWriter Output = Console.Out;
		public TextReader Input = Console.In;

		public string GetLine(int start, int length)
		{
			return this.ReportString.Substring(start, length);
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
