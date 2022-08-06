using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinIfStatement : KirinExecutableNode
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
				throw new FiMException("Expected end of if statement");

			if( condition == string.Empty )
			{
				condition = "correct is correct";
				this.Complete = true;
			}

			if (!KirinConditional.IsConditional(condition, out var result))
				throw new FiMException("Expression is not a conditional");

			Conditions.Add(new KirinConditionStatement()
			{
				Condition = result,
				Statement = statement
			});
		}
		public override object Execute(FiMClass reportClass)
		{
			if (!this.Complete) throw new FiMException("Executing an incomplete if statement");

			foreach (var cS in Conditions)
			{
				var conditional = new KirinConditional(cS.Condition);
				if (conditional.GetValue(reportClass) == false) continue;
				return cS.Statement.Execute(reportClass);
			}
			return null;
		}

		public static KirinIfStatement ParseNodes(
			KirinIfStatementStart startNode,
			KirinNode[] nodes,
			KirinIfStatementEnd endNode,
			string content)
		{
			var statement = new KirinIfStatement(-1, -1);

			string currentCondition = startNode.RawCondition;
			KirinNode conditionNode = startNode;
			List<KirinNode> subStatement = new List<KirinNode>();
			foreach(var subnode in nodes)
			{
				if (subnode.NodeType != "KirinElseIfStatement")
				{
					subStatement.Add(subnode);
				}
				else
				{
					var conditionStatement = FiMLexer.ParseStatement(subStatement.ToArray(), content);
					try
					{
						statement.AddCondition(currentCondition, conditionStatement);
					}
					catch (FiMException ex)
					{
						throw new Exception(ex.Message + " at line " +
							FiMHelper.GetIndexPair(content, conditionNode.Start).Line);
					}


					var elseIfNode = subnode as KirinElseIfStatement;
					currentCondition = elseIfNode.RawCondition;
					conditionNode = subnode;

					subStatement.Clear();
				}
			}

			if( subStatement.Count > 0 )
			{
				var conditionStatement = FiMLexer.ParseStatement(subStatement.ToArray(), content);
				try
				{
					statement.AddCondition(currentCondition, conditionStatement);
				}
				catch (FiMException ex)
				{
					throw new Exception(ex.Message + " at line " +
						FiMHelper.GetIndexPair(content, conditionNode.Start).Line);
				}
			}

			statement.SetComplete(startNode.Start, endNode.Start + endNode.Length);

			if (statement.Count == 0)
				throw new FiMException("If Statement has empty conditions");

			return statement;
		}
	}

	public class KirinIfStatementStart : KirinNode
	{
		public KirinIfStatementStart(int start, int length) : base(start, length) { }

		public string RawCondition;

		private readonly static string[] PreKeywords = new[] { "If ", "When " };
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = PreKeywords.FirstOrDefault(k => content.StartsWith(k));
			if (match == null) return false;

			var node = new KirinIfStatementStart(start, length)
			{
				RawCondition = content.Substring(match.Length)
			};

			if(node.RawCondition.EndsWith(" then"))
				node.RawCondition = node.RawCondition.Substring(0, node.RawCondition.Length - " then".Length);

			result = node;
			return true;
		}
	}
	public class KirinElseIfStatement : KirinNode
	{
		public KirinElseIfStatement(int start, int length) : base(start, length) { }

		public string RawCondition;

		private readonly static string[] PreKeywords = new[] { "Or else", "Otherwise" };

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = PreKeywords.FirstOrDefault(k => content.StartsWith(k));
			if (match == null) return false;

			var node = new KirinElseIfStatement(start, length)
			{
				RawCondition = ""
			};
			if (content.Substring(match.Length).Length > 0 && content.Substring(match.Length,1) == " " )
			{
				var matchStr = content.Substring(match.Length + 1);
				var startStr = matchStr[0].ToString();
				if (startStr != startStr.ToLower()) throw new FiMException("Invalid else if statement");
				if (!KirinIfStatementStart.TryParse(
					matchStr.Substring(0,1).ToUpper() + matchStr.Substring(1), start, length, out var ifResult)
				)
					throw new FiMException("Invalid else if statement");

				node.RawCondition = (ifResult as KirinIfStatementStart).RawCondition;
			}

			if (node.RawCondition.EndsWith(" then"))
				node.RawCondition = node.RawCondition.Substring(0, node.RawCondition.Length - " then".Length);

			result = node;
			return true;
		}
	}
	public class KirinIfStatementEnd : KirinNode
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
