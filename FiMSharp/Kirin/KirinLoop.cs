using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinLoop : KirinExecutableNode
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

				if (si == nodes.Length - 1) throw new FiMException($"Failed to find end of statement");

				statementNodes.Add(subnode);
			}

			return statementNodes.ToArray();
		}
	}

	public class KirinForInLoop : KirinLoop
	{
		public KirinForInLoop(int start, int length) : base(start, length) { }
		public string RawValue;
		
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = Regex.Match(content, @"^For every (.+) in (.+)");
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

		public override object Execute(FiMClass reportClass)
		{
			if (reportClass.Variables.Has(this.VariableName))
				throw new FiMException("Variable " + this.VariableName + " already exists");

			var varArray = new KirinValue(this.RawValue, reportClass);
			if (!FiMHelper.IsTypeArray(varArray.Type) && varArray.Type != KirinVariableType.STRING)
				throw new FiMException("Expected type array on for-in loops");

			if(varArray.Type == KirinVariableType.STRING)
			{
				string str = Convert.ToString(varArray.Value);
				reportClass.Variables.Push(new FiMVariable(this.VariableName, str[0]));
				foreach (char c in str)
				{
					reportClass.Variables.Get(this.VariableName).Value = c;
					var value = this.Statement.Execute(reportClass);
					if (value != null)
					{
						reportClass.Variables.Pop(false);
						return value;
					}
				}
			}
			else
			{
				var list = varArray.Value as System.Collections.IDictionary;
				var sortedKeys = list.Keys.Cast<int>().ToArray().OrderBy(k => k).ToArray();

				reportClass.Variables.Push(new FiMVariable(this.VariableName, list[sortedKeys[0]]));
				foreach ( int k in sortedKeys ) {
					reportClass.Variables.Get(this.VariableName).Value = list[k];
					var value = this.Statement.Execute(reportClass);
					if (value != null)
					{
						reportClass.Variables.Pop(false);
						return value;
					}
				}
			}

			reportClass.Variables.Pop(false);
			return null;
		}

		public static KirinForInLoop ParseNodes(
			KirinForInLoop startNode,
			KirinNode[] nodes,
			KirinLoopEnd endNode,
			string content)
		{
			var statement = startNode;
			if( endNode != null )
			{
				statement.Length = (endNode.Start + endNode.Length) - startNode.Start;
				statement.Statement = FiMLexer.ParseStatement(nodes, content);
			}

			return statement;
		}
	}
	public class KirinForToLoop : KirinLoop
	{
		public KirinForToLoop(int start, int length) : base(start, length) { }
		public string RawFrom;
		public string RawTo;
		public string RawInterval;

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = Regex.Match(content, @"^For every (.+) from (.+) to (.+)");
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
				throw new FiMException("Expected type number in a for-to loop");

			var byMatch = Regex.Match(node.RawTo, @"^(.+?) by (.+)");
			if(byMatch.Success)
			{
				node.RawTo = byMatch.Groups[1].Value;
				node.RawInterval = byMatch.Groups[2].Value;
			}

			node.VariableName = node.VariableName.Substring(eKeyword.Length);
			node.ExpectedType = eType;

			result = node;
			return true;
		}

		public override object Execute(FiMClass reportClass)
		{
			if( reportClass.GetVariable(this.VariableName) != null )
				throw new FiMException("Variable " + this.VariableName + " already exists");

			var varFrom = new KirinValue(this.RawFrom, reportClass);
			var varTo = new KirinValue(this.RawTo, reportClass);

			if(varFrom.Type != KirinVariableType.NUMBER || varTo.Type != KirinVariableType.NUMBER)
				throw new FiMException("Expected type number on for-to loops");

			var valFrom = Convert.ToDouble(varFrom.Value);
			var valTo = Convert.ToDouble(varTo.Value);
			
			double interval;
			if( this.RawInterval != string.Empty )
			{
				var varInterval = new KirinValue(this.RawInterval, reportClass);
				if (varInterval.Type != KirinVariableType.NUMBER)
					throw new FiMException("Expected tpye number of for-to interval");
				interval = Convert.ToDouble(varInterval.Value);
			}
			else
			{
				interval = valTo - valFrom > 0 ? 1.0d : -1.0d;
			}

			reportClass.Variables.Push(new FiMVariable(this.VariableName, valFrom));
			while ( interval > 0 ? valFrom <= valTo : valFrom >= valTo )
			{
				reportClass.Variables.Get(this.VariableName).Value = valFrom;

				var value = this.Statement.Execute(reportClass);
				if (value != null)
				{
					reportClass.Variables.Pop(false);
					return value;
				}

				valFrom += interval;
			}

			reportClass.Variables.Pop(false);
			return null;
		}

		public static KirinForToLoop ParseNodes(
			KirinForToLoop startNode,
			KirinNode[] nodes,
			KirinLoopEnd endNode,
			string content)
		{
			var statement = startNode;
			if( endNode != null )
			{
				statement.Length = (endNode.Start + endNode.Length) - startNode.Start;
				statement.Statement = FiMLexer.ParseStatement(nodes, content);
			}

			return statement;
		}
	}

	public class KirinWhileLoop : KirinExecutableNode
	{
		public KirinWhileLoop(int start, int length) : base(start, length) { }

		public KirinConditional.ConditionalCheckResult Condition;
		public KirinStatement Statement;

		private readonly static string[] PreKeywords = new[] { "As long as ", "While " };

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = PreKeywords.FirstOrDefault(k => content.StartsWith(k));
			if (match == null) return false;

			string condition = content.Substring(match.Length);

			if (!KirinConditional.IsConditional(condition, out var cResult))
				throw new FiMException("Expression is not a conditional");

			var node = new KirinWhileLoop(start, length) { Condition = cResult };

			result = node;
			return true;
		}

		public override object Execute(FiMClass reportClass)
		{
			if (this.Statement == null)
				throw new FiMException("While loop has no statement");

			var conditional = new KirinConditional(Condition);
			while (conditional.GetValue(reportClass) == true)
			{
				var value = Statement.Execute(reportClass);
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
			if( endNode != null ) {
				statement.Length = (endNode.Start + endNode.Length) - startNode.Start;
				statement.Statement = FiMLexer.ParseStatement(nodes, content);
			}

			return statement;
		}
	}

	public class KirinLoopEnd : KirinNode
	{
		public KirinLoopEnd(int start, int length) : base(start, length) { }

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (content != "That's what I did") return false;

			result = new KirinLoopEnd(start, length);
			return true;
		}
	}
}
