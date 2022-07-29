using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	class KirinLoop : KirinExecutableNode
	{
		public KirinLoop(int start, int length): base(start, length) { }

		public string VariableName;
		public KirinVariableType ExpectedType;
		public KirinStatement Statement;

		public static KirinNode[] GetStatementNodes(
			KirinNode startingNode,
			KirinNode[] nodes,
			int startIndex,
			string content,
			out int endIndex
		) {
			endIndex = startIndex;
			string endNodeType = startingNode.NodeType == "KirinIfStatementStart" ? "KirinIfStatementEnd" : "KirinLoopEnd";

			List<KirinNode> statementNodes = new List<KirinNode>();
			for (int si = startIndex + 1; si < nodes.Length; si++)
			{
				var subnode = nodes[si];

				if (subnode.NodeType == "KirinIfStatementStart" ||
					subnode.NodeType == "KirinForInLoop" ||
					subnode.NodeType == "KirinForToLoop" ||
					subnode.NodeType == "KirinWhileLoop" ||
					subnode.NodeType == "KirinSwitchStart")
				{
					var subnodes = GetStatementNodes( subnode, nodes, si, content, out si );
					var eNode = new KirinExecutableNode(-1, -1);

					if( subnode.NodeType == "KirinIfStatementStart" )
					{
						var sn = subnode as KirinIfStatementStart;
						var en = nodes[si] as KirinIfStatementEnd;
						
						eNode = KirinIfStatement.ParseNodes(sn, subnodes, en, content);
					}
					else if (subnode.NodeType == "KirinForInLoop")
					{
						var sn = subnode as KirinForInLoop;
						var en = nodes[si] as KirinLoopEnd;

						eNode = KirinForInLoop.ParseNodes(sn, subnodes, en, content);
					}
					else if (subnode.NodeType == "KirinForToLoop")
					{
						var sn = subnode as KirinForToLoop;
						var en = nodes[si] as KirinLoopEnd;

						eNode = KirinForToLoop.ParseNodes(sn, subnodes, en, content);
					}
					else if (subnode.NodeType == "KirinWhileLoop")
					{
						var sn = subnode as KirinWhileLoop;
						var en = nodes[si] as KirinLoopEnd;

						eNode = KirinWhileLoop.ParseNodes(sn, subnodes, en, content);
					}
					else if (subnode.NodeType == "KirinSwitchStart")
					{
						var sn = subnode as KirinSwitchStart;
						var en = nodes[si] as KirinLoopEnd;

						eNode = KirinSwitch.ParseNodes(sn, subnodes, en, content);
					}

					statementNodes.Add(eNode);
					continue;
				}

				if( subnode.NodeType == endNodeType )
				{
					endIndex = si;
					break;
				}

				if (si == nodes.Length - 1) throw new Exception($"Failed to find end of statement");

				statementNodes.Add(subnode);
			}

			return statementNodes.ToArray();
		}

			/*public static KirinNode[] GetStatementNodes(KirinNode[] nodes, int nodeIndex, string type, out int newIndex, out KirinNode endNode)
			{
				newIndex = nodeIndex;
				endNode = new KirinNode(-1, -1);

				List<KirinNode> subStatement = new List<KirinNode>();
				int depth = 0;
				for (int si = nodeIndex + 1; si < nodes.Length; si++)
				{
					var subnode = nodes[si];

					if (subnode.NodeType == "KirinForInLoop" ||
						subnode.NodeType == "KirinForToLoop" ||
						subnode.NodeType == "KirinWhileLoop" ||
						subnode.NodeType == "KirinSwitchStart") depth++;

					if (depth != 0)
					{
						if (subnode.NodeType == "KirinLoopEnd") depth--;
					}
					else
					{
						if (subnode.NodeType != "KirinLoopEnd")
						{
							subStatement.Add(subnode);
						}
						else
						{
							newIndex = si;
							endNode = subnode;
							break;
						}
					}

					if (si == nodes.Length - 1)
						throw new Exception($"Failed to find end of {type} statement");
				}

				return subStatement.ToArray();
			}*/
		}
	class KirinForInLoop : KirinLoop
	{
		public KirinForInLoop(int start, int length) : base(start, length) { }
		public string RawValue;

		private readonly static Regex ForIn = new Regex(@"^For every (.+?) in (.+)");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = ForIn.Match(content);
			if (!match.Success) return false;

			var node = new KirinForInLoop(start, length)
			{
				VariableName = match.Groups[1].Value,
				RawValue = match.Groups[2].Value
			};

			var eType = FiMHelper.DeclarationType.Determine(" " + node.VariableName, out string eKeyword);
			node.VariableName = node.VariableName.Substring(eKeyword.Length);
			node.ExpectedType = eType;

			result = node;
			return true;
		}

		public override object Execute(FiMReport report)
		{
			if (report.Variables.Exists(this.VariableName))
				throw new Exception("Variable " + this.VariableName + " already exists");

			var varArray = new KirinValue(this.RawValue, report);

			if (!FiMHelper.IsTypeArray(varArray.Type) && varArray.Type != KirinVariableType.STRING)
				throw new Exception("Expected type array on for-in loops");

			if(varArray.Type == KirinVariableType.STRING)
			{
				string str = Convert.ToString(varArray.Value);
				report.Variables.PushVariable(new FiMVariable(this.VariableName, str[0]));
				foreach (char c in str)
				{
					report.Variables.Get(this.VariableName).Value = c;
					var value = this.Statement.Execute(report);
					if (value != null)
					{
						report.Variables.PopVariable();
						return value;
					}
				}
				report.Variables.PopVariable();
			}
			else
			{
				var list = varArray.Value as Dictionary<int, object>;
				var sortedKeys = list.Keys.ToArray().OrderBy(k => k).ToArray();

				report.Variables.PushVariable(new FiMVariable(this.VariableName, list[sortedKeys[0]]));
				foreach ( int k in sortedKeys ) {
					report.Variables.Get(this.VariableName).Value = list[k];
					var value = this.Statement.Execute(report);
					if (value != null)
					{
						report.Variables.PopVariable();
						return value;
					}
				}
				report.Variables.PopVariable();
			}

			return null;
		}

		public static KirinForInLoop ParseNodes(
			KirinForInLoop startNode,
			KirinNode[] nodes,
			KirinLoopEnd endNode,
			string content)
		{
			var statement = startNode;
			statement.Length = (endNode.Start + endNode.Length) - startNode.Start;
			statement.Statement = FiMLexer.ParseStatement(nodes, content);

			return statement;
		}
	}
	class KirinForToLoop : KirinLoop
	{
		public KirinForToLoop(int start, int length) : base(start, length) { }
		public string RawFrom;
		public string RawTo;
		public string RawInterval;

		private readonly static Regex ForIn = new Regex(@"^For every (.+?) from (.+?) to (.+)");
		private readonly static Regex ToBy = new Regex(@"^(.+?) by (.+)");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = ForIn.Match(content);
			if (!match.Success) return false;

			var node = new KirinForToLoop(start, length)
			{
				VariableName = match.Groups[1].Value,
				RawFrom = match.Groups[2].Value,
				RawTo = match.Groups[3].Value,
				RawInterval = string.Empty
			};

			var eType = FiMHelper.DeclarationType.Determine(" " + node.VariableName, out string eKeyword);
			if (eType != KirinVariableType.NUMBER)
				throw new Exception("Expected type number in a for-to loop");

			if(ToBy.IsMatch(node.RawTo))
			{
				var byMatch = ToBy.Match(node.RawInterval);
				node.RawTo = byMatch.Groups[1].Value;
				node.RawInterval = byMatch.Groups[2].Value;
			}

			node.VariableName = node.VariableName.Substring(eKeyword.Length);
			node.ExpectedType = eType;

			result = node;
			return true;
		}

		public override object Execute(FiMReport report)
		{
			if (report.Variables.Exists(this.VariableName))
				throw new Exception("Variable " + this.VariableName + " already exists");

			var varFrom = new KirinValue(this.RawFrom, report);
			var varTo = new KirinValue(this.RawTo, report);

			if(varFrom.Type != KirinVariableType.NUMBER || varTo.Type != KirinVariableType.NUMBER)
				throw new Exception("Expected type number on for-to loops");

			var valFrom = Convert.ToDouble(varFrom.Value);
			var valTo = Convert.ToDouble(varTo.Value);
			if (valFrom == valTo) return null;

			double interval;
			if( this.RawInterval != string.Empty )
			{
				var varInterval = new KirinValue(this.RawInterval, report);
				if (varInterval.Type != KirinVariableType.NUMBER)
					throw new Exception("Expected tpye number of for-to interval");
				interval = Convert.ToDouble(varInterval.Value);
			}
			else
			{
				interval = valTo - valFrom > 0 ? 1.0d : -1.0d;
			}

			report.Variables.PushVariable(new FiMVariable(this.VariableName, valFrom));
			while ( interval > 0 ? valFrom <= valTo : valFrom >= valTo )
			{
				report.Variables.Get(this.VariableName).Value = valFrom;

				var value = this.Statement.Execute(report);
				if (value != null)
				{
					report.Variables.PopVariable();
					return value;
				}

				valFrom += interval;
			}
			report.Variables.PopVariable();

			return null;
		}

		public static KirinForToLoop ParseNodes(
			KirinForToLoop startNode,
			KirinNode[] nodes,
			KirinLoopEnd endNode,
			string content)
		{
			var statement = startNode;
			statement.Length = (endNode.Start + endNode.Length) - startNode.Start;
			statement.Statement = FiMLexer.ParseStatement(nodes, content);

			return statement;
		}
	}

	class KirinWhileLoop : KirinExecutableNode
	{
		public KirinWhileLoop(int start, int length) : base(start, length) { }

		public KirinConditional.ConditionalCheckResult Condition;
		public KirinStatement Statement;

		private readonly static Regex WhileLoop = new Regex(@"^(?:As long as|While) (.+)");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = WhileLoop.Match(content);
			if (!match.Success) return false;

			string condition = match.Groups[1].Value;

			if (!KirinConditional.IsConditional(condition, out var cResult))
				throw new Exception("Expression is not a conditional");

			var node = new KirinWhileLoop(start, length) { Condition = cResult };

			result = node;
			return true;
		}

		public override object Execute(FiMReport report)
		{
			if (this.Statement == null)
				throw new Exception("While loop has no statement");

			var conditional = new KirinConditional(Condition);
			while (conditional.GetValue(report) == true)
			{
				var value = Statement.Execute(report);
				if (value != null) break;
			}

			return null;
		}

		public static KirinWhileLoop ParseNodes(
			KirinWhileLoop startNode,
			KirinNode[] nodes,
			KirinLoopEnd endNode,
			string content)
		{
			var statement = startNode;
			statement.Length = (endNode.Start + endNode.Length) - startNode.Start;
			statement.Statement = FiMLexer.ParseStatement(nodes, content);

			return statement;
		}
	}

	class KirinLoopEnd : KirinNode
	{
		public KirinLoopEnd(int start, int length) : base(start, length) { }

		public readonly static string LoopEnd = "That's what I did";

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (content != LoopEnd) return false;

			result = new KirinLoopEnd(start, length);
			return true;
		}
	}
}
