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

		public static KirinNode[] GetStatementNodes(KirinNode[] nodes, int nodeIndex, string type, out int newIndex, out KirinNode endNode)
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
					subnode.NodeType == "KirinWhileLoopStart" ||
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
		}
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
