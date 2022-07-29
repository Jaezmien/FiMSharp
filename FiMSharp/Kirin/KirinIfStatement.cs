using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	class KirinIfStatement : KirinExecutableNode
	{
		public KirinIfStatement(int start, int length) : base(start, length)
		{
			this.Complete = false;
			this.Conditions = new List<KirinConditionStatement>();
		}
		private struct KirinConditionStatement
		{
			public KirinConditional.ConditionalCheckResult Condition;
			public KirinStatement Statement;
		}
		private List<KirinConditionStatement> Conditions;
		private bool Complete;
		public int Count
		{
			get { return Conditions.Count; }
		}

		public void SetComplete(int start, int end) {
			this.Start = start;
			this.Length = end - start;
			this.Complete = true;
		}
		public void AddCondition(string condition, KirinStatement statement)
		{
			if (this.Complete)
				throw new Exception("Expected end of if statement");

			if( condition == string.Empty )
			{
				condition = "correct is correct";
				this.Complete = true;
			}

			if (!KirinConditional.IsConditional(condition, out var result))
				throw new Exception("Expression is not a conditional");

			Conditions.Add(new KirinConditionStatement()
			{
				Condition = result,
				Statement = statement
			});
		}
		public override object Execute(FiMReport report)
		{
			if (!this.Complete) throw new Exception("Executing an incomplete if statement");

			foreach (var cS in Conditions)
			{
				var conditional = new KirinConditional(cS.Condition);
				if (conditional.GetValue(report) == false) continue;
				return cS.Statement.Execute(report);
			}
			return null;
		}
	}

	class KirinIfStatementStart : KirinNode
	{
		public KirinIfStatementStart(int start, int length) : base(start, length) { }

		public string RawCondition;

		private readonly static Regex IfStart = new Regex(@"^(?:If|When) (.+)");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = IfStart.Match(content);
			if (!match.Success) return false;

			var node = new KirinIfStatementStart(start, length)
			{
				RawCondition = match.Groups[1].Value
			};

			if(node.RawCondition.EndsWith(" then"))
				node.RawCondition = node.RawCondition.Substring(0, node.RawCondition.Length - " then".Length);

			result = node;
			return true;
		}
	}
	class KirinElseIfStatement : KirinNode
	{
		public KirinElseIfStatement(int start, int length) : base(start, length) { }

		public string RawCondition;

		private readonly static Regex IfStart = new Regex(@"^(?:Or else|Otherwise)( .+)?");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = IfStart.Match(content);
			if (!match.Success) return false;

			var node = new KirinElseIfStatement(start, length)
			{
				RawCondition = ""
			};
			if( match.Groups[1].Success )
			{
				var matchStr = match.Groups[1].Value.Substring(1);
				var startStr = matchStr[0].ToString();
				if (startStr != startStr.ToLower()) throw new Exception("Invalid else if statement");
				if (!KirinIfStatementStart.TryParse(
					matchStr.Substring(0,1).ToUpper() + matchStr.Substring(1), start, length, out var ifResult)
				)
					throw new Exception("Invalid else if statement");

				node.RawCondition = (ifResult as KirinIfStatementStart).RawCondition;
			}

			if (node.RawCondition.EndsWith(" then"))
				node.RawCondition = node.RawCondition.Substring(0, node.RawCondition.Length - " then".Length);

			result = node;
			return true;
		}
	}
	class KirinIfStatementEnd : KirinNode
	{
		public KirinIfStatementEnd(int start, int length) : base(start, length) { }

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (content != "That's what I would do") return false;

			result = new KirinIfStatementEnd(start, length);
			return true;
		}
	}
}
