using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinSwitch : KirinExecutableNode
	{
		public KirinSwitch(int start, int length) : base(start, length) {
			Cases = new Dictionary<string, KirinStatement>();
			DefaultCase = null;
		}

		private Dictionary<string, KirinStatement> Cases;
		private KirinStatement DefaultCase;
		public string RawVariable;
		private bool Complete;

		public void AddCase(KirinStatement statement, string rawIndex)
		{
			if (this.Complete) throw new FiMException("Expected end of switch statement");
			if (this.Cases.ContainsKey(rawIndex)) throw new FiMException("Case " + rawIndex +" already exists");
			this.Cases[rawIndex] = statement;
		}
		public void AddCase(KirinStatement statement)
		{
			if (this.Complete) throw new FiMException("Expected end of switch statement");
			this.Complete = true;
			this.DefaultCase = statement;
		}

		public override object Execute(FiMClass reportClass)
		{
			if (!this.Complete) throw new FiMException("Executing an incomplete switch statement");

			var variable = reportClass.GetVariable(this.RawVariable);
			if (variable == null)
				throw new FiMException("Varible " + this.RawVariable + " does not exist");

			if (FiMHelper.IsTypeArray(variable))
				throw new FiMException("Cannot use array on a switch");

			Dictionary<object, string> CasesLookup = new Dictionary<object, string>();
			foreach(var key in Cases.Keys)
			{
				if (!KirinSwitchCase.IsValidPlace(key, reportClass, out object value))
					throw new FiMException("Invalid case " + key);

				if(CasesLookup.Keys.Any(k => KirinValue.IsEqual(k, value)))
					throw new FiMException("Duplicate case value " + key);

				CasesLookup.Add(value, key);
			}

			KirinStatement s = DefaultCase;
			if (CasesLookup.Keys.Any(k => KirinValue.IsEqual(k, variable.Value)))
				s = Cases[CasesLookup.First(l => KirinValue.IsEqual(l.Key, variable.Value)).Value];

			if( s != null ) return s.Execute(reportClass);
			return null;
		}

		public static KirinSwitch ParseNodes(
			KirinSwitchStart startNode,
			KirinNode[] nodes,
			KirinLoopEnd endNode,
			string content)
		{
			var statement = new KirinSwitch(-1, -1);
			statement.RawVariable = startNode.RawVariable;
			statement.Start = startNode.Start;
			statement.Length = (endNode.Start + endNode.Length) - startNode.Start;

			string currentCase = string.Empty;
			bool isDefault = false;
			List<KirinNode> subStatement = new List<KirinNode>();
			foreach (var subnode in nodes)
			{
				if (subnode.NodeType == "KirinSwitchCase")
				{
					if( subStatement.Count > 0 )
					{
						var s = FiMLexer.ParseStatement(subStatement.ToArray(), content);
						if (currentCase != string.Empty) statement.AddCase(s, currentCase);
						else if (isDefault) statement.AddCase(s);
						subStatement.Clear();
					}

					var cNode = subnode as KirinSwitchCase;
					currentCase = cNode.RawCase;
					isDefault = false;
				}
				else if (subnode.NodeType == "KirinSwitchCaseDefault")
				{
					if (subStatement.Count > 0)
					{
						var s = FiMLexer.ParseStatement(subStatement.ToArray(), content);
						if (currentCase != string.Empty) statement.AddCase(s, currentCase);
						else if (isDefault) statement.AddCase(s);
						subStatement.Clear();
					}

					currentCase = string.Empty;
					isDefault = true;
				}
				else
				{
					if (currentCase == string.Empty && !isDefault)
						throw new FiMException("Switch case not found");

					subStatement.Add(subnode);
				}
			}

			if (subStatement.Count > 0)
			{
				var s = FiMLexer.ParseStatement(subStatement.ToArray(), content);
				if (currentCase != string.Empty) statement.AddCase(s, currentCase);
				else if (isDefault) statement.AddCase(s);
				subStatement.Clear();
			}

			return statement;
		}
	}

	public class KirinSwitchStart : KirinNode
	{
		public KirinSwitchStart(int start, int length) : base(start, length) { }

		public string RawVariable;

		private readonly static string Keyword = "In regards to ";
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith(Keyword)) return false;

			result = new KirinSwitchStart(start, length)
			{
				RawVariable = content.Substring(Keyword.Length)
			};
			return true;
		}
	}

	public class KirinSwitchCase : KirinNode
	{
		public KirinSwitchCase(int start, int length) : base(start, length) { }

		private static bool IsValidNumberPlace(string place, out int index)
		{
			index = -1;

			var match = Regex.Match(place, @"^(\d)+(nd|rd|st|th)$");
			if (!match.Success) return false;

			index = Convert.ToInt32(match.Groups[1].Value);
			string placementSuffix = match.Groups[2].Value;
			if( index % 100 >= 10 && index % 100 <= 19 )
			{
				if (placementSuffix != "th") throw new FiMException("Invalid placement suffix");
			}
			else
			{
				string[] suffix = new string[] { "th", "st", "nd", "rd", "th", "th", "th", "th", "th", "th" };
				int lastNumber = Convert.ToInt32(index.ToString().Substring(index.ToString().Length - 1, 1));

				if (suffix[lastNumber] != placementSuffix) throw new FiMException("Invalid placement suffix");
			}

			return true;
		}
		public static bool IsValidPlace(string place, FiMClass reportClass, out object index)
		{
			if(IsValidNumberPlace(place, out var nIndex))
			{
				index = nIndex;
				return true;
			}

			if( reportClass.GetVariable(place) != null )
			{
				var variable = reportClass.GetVariable(place);
				if (!variable.Constant) throw new FiMException("Cannot use a non-constant variable as a case");
				if (FiMHelper.IsTypeArray(variable.Type)) throw new FiMException("Can only use non-array variables as a case");

				index = variable.Value;
				return true;
			}

			index = null;
			return false;
		}

		public string RawCase;

		private readonly static string PreKeyword = "On the ";
		private readonly static string PostKeyword = " hoof";
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith(PreKeyword) || !content.EndsWith(PostKeyword)) return false;

			result = new KirinSwitchCase(start, length)
			{
				RawCase = content.Substring(PreKeyword.Length, content.Length - PreKeyword.Length - PostKeyword.Length)
			};
			return true;
		}
	}
	public class KirinSwitchCaseDefault : KirinNode
	{
		public KirinSwitchCaseDefault(int start, int length) : base(start, length) { }

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (content != "If all else fails") return false;

			result = new KirinSwitchCaseDefault(start, length);
			return true;
		}
	}
}
