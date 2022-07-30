using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace FiMSharp.Kirin
{
	class KirinListModify : KirinExecutableNode
	{
		public KirinListModify(int start, int length) : base(start, length) { }

		private readonly static Regex ListModif = new Regex(@"^(.+) (\d+) (?:is|was|ha[sd]|like[sd]?) (.+)");
		private readonly static Regex ListModifVar = new Regex(@"^(.+) of (.+) (?:is|was|ha[sd]|like[sd]?) (.+)");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;

			if ( ListModif.IsMatch(content) )
			{
				var matches = ListModif.Matches(content);
				GroupCollection groups = matches[0].Groups;

				result = new KirinListModify(start, length)
				{
					LeftOp = groups[1].Value,
					RawIndex = groups[2].Value,
					RightOp = groups[3].Value
				};
			}
			else if( ListModifVar.IsMatch(content) )
			{
				var matches = ListModifVar.Matches(content);
				GroupCollection groups = matches[0].Groups;

				result = new KirinListModify(start, length)
				{
					RawIndex = groups[1].Value,
					LeftOp = groups[2].Value,
					RightOp = groups[3].Value
				};
			}
			else return false;

			return true;
		}

		public string LeftOp;
		public string RawIndex;
		public string RightOp;

		public override object Execute(FiMReport report)
		{
			if (!report.Variables.Exists(this.LeftOp))
				throw new FiMException("Variable " + this.LeftOp + " does not exist");

			var variable = report.Variables.Get(this.LeftOp);
			if (!FiMHelper.IsTypeArray(variable.Type) && variable.Type != KirinVariableType.STRING)
				throw new FiMException("Variable " + this.LeftOp + " is not an array");

			var kIndex = new KirinValue(this.RawIndex, report);
			if (kIndex.Type != KirinVariableType.NUMBER)
				throw new FiMException("Invalid index " + kIndex.Value);
			var iValue = Convert.ToInt32(kIndex.Value);

			var value = new KirinValue(this.RightOp, report);
			
			if ( variable.Type == KirinVariableType.STRING )
			{
				if (value.Type != KirinVariableType.CHAR)
					throw new FiMException("Invalid array modify value");

				var sb = new StringBuilder(variable.Value as string);
				sb[iValue] = (char)variable.Value;
				variable.Value = sb.ToString();
			}
			else
			{
				if (!FiMHelper.IsTypeOfArray(value.Type, (KirinArrayType)variable.Type))
					throw new FiMException("Invalid array modify value");

				dynamic dict;
				int index = Convert.ToInt32(kIndex.Value);
				if ( variable.Type == KirinVariableType.STRING_ARRAY )
				{
					dict = variable.Value as Dictionary<int, string>;
					dict[index] = Convert.ToString(value.Value);
				}
				else if( variable.Type == KirinVariableType.NUMBER_ARRAY )
				{
					dict = variable.Value as Dictionary<int, double>;
					dict[index] = Convert.ToDouble(value.Value);
				}
				else if( variable.Type == KirinVariableType.BOOL_ARRAY )
				{
					dict = variable.Value as Dictionary<int, bool>;
					dict[index] = Convert.ToBoolean(value.Value);
				}
			}

			;
			return null;
		}
	}
}
