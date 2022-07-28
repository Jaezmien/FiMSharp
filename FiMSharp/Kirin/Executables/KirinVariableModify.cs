using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	class KirinVariableModify : KirinExecutableNode
	{
		public KirinVariableModify(int start, int length) : base(start, length) { }

		private readonly static string[] ReplaceKW = {
			" becomes ", " become ", " became ", " is now ", " now likes ", " now like ", " are now "
		};
		public static bool TryParse(string content, int start, int length, out KirinVariableModify result)
		{
			result = null;
			if (!ReplaceKW.Any(kw => content.Contains(kw))) return false;

			result = new KirinVariableModify(start, length);
			string mKeyword = ReplaceKW.First(kw => content.Contains(kw));
			int mIndex = content.IndexOf(mKeyword);

			result.LeftOp = content.Substring(0, mIndex);
			result.RightOp = content.Substring(mIndex + mKeyword.Length);
			return true;
		}

		public string LeftOp;
		public string RightOp;

		public override object Execute(FiMReport report)
		{
			if (!report.Variables.Exists(this.LeftOp))
				throw new Exception("Variable " + this.LeftOp + " does not exist");
			var kValue = new KirinValue(this.RightOp, report);
			report.Variables.Get(this.LeftOp).Value = kValue.Value;
			return null;
		}
	}
}
