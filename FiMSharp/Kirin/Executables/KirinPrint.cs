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

		private readonly static Regex Print = new Regex(@"^I (quickly )?(?:said|sang|wrote) (.+)");

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = Print.Match(content);
			if (!match.Success) return false;

			result = new KirinPrint(start, length)
			{
				NewLine = !match.Groups[1].Success,
				RawParameters = match.Groups[2].Value,
			};
			return true;
		}

		public string RawParameters;
		public bool NewLine;

		public override object Execute(FiMClass reportClass)
		{
			var result = (new KirinValue(RawParameters, reportClass)).Value;
			if (result == null) return null;
			if (FiMHelper.IsTypeArray(result)) throw new FiMException("Cannot print an array");
			reportClass.Report.Output(Convert.ToString(result) + (NewLine ? "\n" : ""));
			return null;
		}
	}
}
