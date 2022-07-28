using System;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinPrint : KirinExecutableNode
	{
		public KirinPrint(int start, int length) : base(start, length)
		{
			this.Start = start;
			this.Length = length;
		}

		private readonly static Regex Print = new Regex(@"^I (?:said|sang|wrote) (.+)");

		public static bool TryParse(string content, int start, int length, out KirinPrint result)
		{
			result = null;
			var matches = Print.Matches(content);
			if (matches.Count != 1) return false;

			Group group = matches[0].Groups[1];

			result = new KirinPrint(start, length)
			{
				RawParameters = group.Value
			};

			return true;
		}

		public string RawParameters;

		public override object Execute(FiMReport report)
		{
			var result = (new KirinValue(RawParameters, report)).Value;
			if (FiMHelper.IsTypeArray(result)) throw new Exception("Cannot print an array");
			report.ConsoleOutput.WriteLine(result);
			return null;
		}
	}
}
