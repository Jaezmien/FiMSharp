using System;

namespace FiMSharp.Kirin
{
	public class KirinConditional : KirinBaseNode
	{
		public KirinConditional(ConditionalCheckResult result)
		{
			this.NodeTree = CreateNode(result);
		}

		private BaseNode NodeTree;
		private BaseNode CreateNode(string value)
		{
			if (!KirinConditional.IsConditional(value, out var result)) return new ValueNode() { RawValue = value };
			return CreateNode(result);
		}
		private BaseNode CreateNode(ConditionalCheckResult result)
		{
			var expression = new ExpressionNode
			{
				Left = CreateNode(result.Left),
				Right = CreateNode(result.Right),
				Condition = result.Expression
			};
			return expression;
		}

		private class Conditionals
		{
			private readonly static string[] And = { "and" };
			private readonly static string[] Or = { "or" };
			private readonly static string[] LessThanEqual = { "had no more than", "has no more than", "is no greater than", "is no more than", "is not greater than", "is not more than", "isn't greater than", "isn't more than", "was no greater than", "was no more than", "was not greater than", "was not more than", "wasn't greater than", "wasn't more than", "were no greater than", "were no more than", "were not greater than", "were not more than", "weren't greater than", "weren't more than" };
			private readonly static string[] GreaterThanEqual = { "had no less than", "has no less than", "is no less than", "is not less than", "isn't less than", "was no less than", "was not less than", "wasn't less than", "were no less than", "were not less than", "weren't less than" };
			private readonly static string[] GreaterThan = { "had more than", "has more than", "is greater than", "was greater than", "were greater than", "were more than", "was more than" };
			private readonly static string[] LessThan = { "had less than", "has less than", "is less than", "was less than", "were less than" };
			private readonly static string[] Not = { "wasn't equal to", "isn't equal to", "weren't equal to", "hadn't", "had not", "hasn't", "has not", "isn't", "is not", "wasn't", "was not", "weren't", "were not" };
			private readonly static string[] Equal = { "is equal to", "was equal to", "were equal to", "had", "has", "is", "was", "were" };

			public struct ConditionalResult
			{
				public string Keyword;
				public string Sign;
				public int Index;
			}
			private static bool HasConditional(string content, string[] conditionals, string sign, out ConditionalResult result)
			{
				result = new ConditionalResult();

				foreach(var _keyword in conditionals)
				{
					string keyword = $" {_keyword} ";

					if (content.IndexOf(keyword) == -1) continue;

					var index = content.IndexOf(keyword);
					while( index != -1 )
					{
						if (!FiMHelper.IsIndexInsideString(content, index))
						{
							result.Keyword = keyword;
							result.Sign = sign;
							result.Index = index;
							return true;
						}

						var newIndex = content.IndexOf(keyword, index);
						if (newIndex == index) break;
						index = newIndex;
					}
				}
				return false;
			}
			public static bool GetConditional(string content, out ConditionalResult result)
			{
				if (HasConditional(content, And, "&&", out result)) return true;
				if (HasConditional(content, Or, "||", out result)) return true;
				if (HasConditional(content, LessThanEqual, "<=", out result)) return true;
				if (HasConditional(content, GreaterThanEqual, ">=", out result)) return true;
				if (HasConditional(content, GreaterThan, ">", out result)) return true;
				if (HasConditional(content, LessThan, "<", out result)) return true;
				if (HasConditional(content, Not, "!=", out result)) return true;
				if (HasConditional(content, Equal, "==", out result)) return true;

				return false;
			}
		}

		public struct ConditionalCheckResult
		{
			public string Left;
			public string Expression;
			public string Right;
		}
		public static bool IsConditional(string content, out ConditionalCheckResult result)
		{
			result = new ConditionalCheckResult();

			if (!Conditionals.GetConditional(content, out var conditional)) return false;

			result.Expression = conditional.Sign;
			result.Left = content.Substring(0, conditional.Index);
			result.Right = content.Substring(conditional.Index + conditional.Keyword.Length);

			return true;
		}

		public bool GetValue(FiMClass reportClass) => Convert.ToBoolean(this.NodeTree.Eval(reportClass));

		private abstract class BaseNode
		{
			public abstract object Eval(FiMClass reportClass);
		}
		private class ExpressionNode : BaseNode
		{
			public BaseNode Left;
			public BaseNode Right;
			public string Condition;

			private static bool IsEqual(object _x, object _y)
			{
				dynamic x;
				dynamic y;

				var lvt = FiMHelper.AsVariableType(_x);
				var rvt = FiMHelper.AsVariableType(_y);

				switch (lvt)
				{
					case KirinVariableType.BOOL: x = Convert.ToBoolean(_x); break;
					case KirinVariableType.NUMBER: x = Convert.ToDouble(_x); break;
					case KirinVariableType.STRING: x = Convert.ToString(_x); break;
					case KirinVariableType.CHAR: x = Convert.ToChar(_x); break;
					default: x = null; break;
				}

				switch (rvt)
				{
					case KirinVariableType.BOOL: y = Convert.ToBoolean(_y); break;
					case KirinVariableType.NUMBER: y = Convert.ToDouble(_y); break;
					case KirinVariableType.STRING: y = Convert.ToString(_y); break;
					case KirinVariableType.CHAR: y = Convert.ToChar(_y); break;
					default: y = null; break;
				}

				return x == y;
			}
			public override object Eval(FiMClass reportClass)
			{
				var lv = Left.Eval(reportClass);
				var rv = Right.Eval(reportClass);
				var lvt = FiMHelper.AsVariableType(lv);
				var rvt = FiMHelper.AsVariableType(rv);

				if (FiMHelper.IsTypeArray(lvt) || FiMHelper.IsTypeArray(rvt))
					throw new FiMException("Cannot execute conditional with an array");

				if (this.Condition == "==") return IsEqual(lv, rv);
				if (this.Condition == "!=") return !IsEqual(lv, rv);
				if (this.Condition == "&&") return Convert.ToBoolean(lv) && Convert.ToBoolean(rv);
				if (this.Condition == "||") return Convert.ToBoolean(lv) || Convert.ToBoolean(rv);

				if (lvt != KirinVariableType.NUMBER ||
					rvt != KirinVariableType.NUMBER)
					throw new FiMException("Expected number value in conditional");
				

				double lvd = Convert.ToDouble(lv);
				double rvd = Convert.ToDouble(rv);
				switch (this.Condition)
				{
					case ">=": return lvd >= rvd;
					case "<=": return lvd <= rvd;
					case ">":  return lvd >  rvd;
					case "<":  return lvd <  rvd;
					default: throw new FiMException("Invalid expression " + this.Condition);
				}
			}
		}
		private class ValueNode : BaseNode
		{
			public string RawValue;
			public override object Eval(FiMClass reportClass)
			{
				var value = new KirinValue(this.RawValue, reportClass);
				return value.Value;
			}
		}
	}
}
