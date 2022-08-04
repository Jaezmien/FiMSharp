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
