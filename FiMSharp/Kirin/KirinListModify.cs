using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	class KirinListModify : KirinExecutableNode
	{
		public KirinListModify(int start, int length) : base(start, length) { }

		private readonly static Regex ListModif = new Regex(@"(.+) (\d+) (?:is|was|ha[sd]|like[sd]?) (.+)");
		public static bool TryParse(string content, int start, int length, out KirinListModify result)
		{
			result = null;
			var matches = ListModif.Matches(content);
			if (matches.Count != 1) return false;

			GroupCollection groups = matches[0].Groups;

			result = new KirinListModify(start, length)
			{
				LeftOp = groups[1].Value,
				Index = Convert.ToInt32(groups[2].Value),
				RightOp = groups[3].Value
			};
			return true;
		}

		public string LeftOp;
		public int Index;
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
