using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	class KirinUnary : KirinExecutableNode
	{
		public KirinUnary(int start, int length) : base(start, length) { }

		private readonly static Regex PreIncrement = new Regex(@"^There was one more (.+)");
		private readonly static Regex PostIncrement = new Regex(@"^(.+) got one more");
		private readonly static Regex PreDecrement = new Regex(@"^There was one less (.+)");
		private readonly static Regex PostDecrement = new Regex(@"^(.+) got one less");

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;

			var unaries = new [] { PreIncrement, PostIncrement, PreDecrement, PostDecrement };
			if (!unaries.Any(r => r.IsMatch(content))) return false;

			var unaryRegex = unaries.First(r => r.IsMatch(content));
			result = new KirinUnary(start, length)
			{
				RawVariable = unaryRegex.Match(content).Groups[1].Value,
				Increment = Array.IndexOf(unaries, unaryRegex) < 2
			};

			return true;
		}

		public string RawVariable;
		public bool Increment;

		public override object Execute(FiMReport report)
		{
			if (!report.Variables.Exists(this.RawVariable))
				throw new Exception("Variable " + this.RawVariable + " does not exist");

			var variable = report.Variables.Get(this.RawVariable);
			if (variable.Type != KirinVariableType.NUMBER)
			{
				if (this.Increment)
					throw new Exception("Cannot apply unary increment on a non-number variable");
				else
					throw new Exception("Cannot apply unary decrement on a non-number variable");
			}

			if (this.Increment)
				variable.Value = Convert.ToDouble(variable.Value) + 1.0d;
			else
				variable.Value = Convert.ToDouble(variable.Value) - 1.0d;

			return null;
		}
	}
}
