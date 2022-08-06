using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinListModify : KirinExecutableNode
	{
		public KirinListModify(int start, int length) : base(start, length) { }

		private readonly static string[] Keywords = new[] { " is ", " was ", " has ", " had ", " like ", " likes ", " liked " };
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!Keywords.Any(k => content.Contains(k))) return false;

			var listModifIndex = Regex.Match(content, @"^(.+) (\d+) (?:(?:i|wa)s|ha[sd]|like[sd]?) (.+)");
			if ( listModifIndex.Success )
			{
				GroupCollection groups = listModifIndex.Groups;
				result = new KirinListModify(start, length)
				{
					LeftOp = groups[1].Value,
					RawIndex = groups[2].Value,
					RightOp = groups[3].Value
				};

				return true;
			}

			var listModifVar = Regex.Match(content, @"^(.+) of (.+) (?:(?:i|wa)s|ha[sd]|like[sd]?) (.+)");
			if( listModifVar.Success )
			{
				GroupCollection groups = listModifVar.Groups;
				result = new KirinListModify(start, length)
				{
					RawIndex = groups[1].Value,
					LeftOp = groups[2].Value,
					RightOp = groups[3].Value
				};

				return true;
			}

			return false;
		}

		public string LeftOp;
		public string RawIndex;
		public string RightOp;

		public override object Execute(FiMClass reportClass)
		{

			var variable = reportClass.GetVariable(this.LeftOp);
			if( variable == null )
				throw new FiMException("Variable " + this.LeftOp + " does not exist");
			if (!FiMHelper.IsTypeArray(variable.Type) && variable.Type != KirinVariableType.STRING)
				throw new FiMException("Variable " + this.LeftOp + " is not an array");

			var kIndex = new KirinValue(this.RawIndex, reportClass);
			if (kIndex.Type != KirinVariableType.NUMBER)
				throw new FiMException("Invalid index " + kIndex.Value);
			var iValue = Convert.ToInt32(kIndex.Value);

			var value = new KirinValue(this.RightOp, reportClass);
			
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
