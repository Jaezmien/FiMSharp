using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinUnary : KirinExecutableNode
	{
		public KirinUnary(int start, int length) : base(start, length) { }

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.Contains(" one ")) return false;

			var preUnaries = new[] { "There was one more ", "There was one less " };
			var postUnaries = new[] { " got one more", " got one less" };

			if (preUnaries.Any(u => content.StartsWith(u)))
			{
				string keyword = preUnaries.First(u => content.StartsWith(u));
				result = new KirinUnary(start, length)
				{
					RawVariable = content.Substring( keyword.Length ),
					Increment = Array.FindIndex(preUnaries, u => content.StartsWith(u)) == 0
				};

				return true;
			}

			if (postUnaries.Any(u => content.EndsWith(u)))
			{
				string keyword = postUnaries.First(u => content.EndsWith(u));
				result = new KirinUnary(start, length)
				{
					RawVariable = content.Substring(0, content.Length - keyword.Length),
					Increment = Array.FindIndex(postUnaries, u => content.EndsWith(u)) == 0
				};

				return true;
			}

			return false;
		}

		public string RawVariable;
		public bool Increment;

		public override object Execute(FiMClass reportClass)
		{
			if (reportClass.GetVariable(this.RawVariable) != null)
			{
				var variable = reportClass.GetVariable(this.RawVariable);

				if (variable.Type != KirinVariableType.NUMBER)
				{
					if (this.Increment)
						throw new FiMException("Cannot apply unary increment on a non-number variable");
					else
						throw new FiMException("Cannot apply unary decrement on a non-number variable");
				}

				if (this.Increment)
					variable.Value = Convert.ToDouble(variable.Value) + 1.0d;
				else
					variable.Value = Convert.ToDouble(variable.Value) - 1.0d;
			}
			else if(FiMHelper.ArrayIndex.IsArrayIndex(this.RawVariable, reportClass))
			{
				var match = FiMHelper.ArrayIndex.GetArrayIndex(this.RawVariable, reportClass);
				var variable = reportClass.GetVariable(match.RawVariable);
				if( variable.Type != KirinVariableType.NUMBER_ARRAY )
				{
					if (this.Increment)
						throw new FiMException("Cannot apply unary increment on a non-number array");
					else
						throw new FiMException("Cannot apply unary decrement on a non-number array");
				}

				var varIndex = new KirinValue(match.RawIndex, reportClass);
				if (varIndex.Type != KirinVariableType.NUMBER) throw new FiMException("Invalid index value");
				int index = Convert.ToInt32(varIndex.Value);

				var dict = variable.Value as Dictionary<int, double>;
				if (!dict.ContainsKey(index)) dict[index] = 0.0d;

				if (this.Increment)
					dict[index] += 1.0d;
				else
					dict[index] -= 1.0d;
			}
			else
			{
				throw new FiMException("Variable " + this.RawVariable + " does not exist");
			}

			return null;
		}
	}
}
