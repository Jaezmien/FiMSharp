using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
    public class KirinArithmetic : KirinBaseNode
    {
        public KirinArithmetic(ArithmeticCheckResult result)
        {
            this.NodeTree = CreateNode(result);
        }

        private BaseNode NodeTree;
        private BaseNode CreateNode(string value)
        {
            if (!KirinArithmetic.IsArithmetic(value, out var result)) return new ValueNode() { RawValue = value };
            return CreateNode(result);
        }
        private BaseNode CreateNode(ArithmeticCheckResult result)
        {
            var expression = new ExpressionNode
            {
                Left = CreateNode(result.Left),
                Right = CreateNode(result.Right),
                Expression = result.Expression
            };
            return expression;
        }

        private struct ArithmeticStruct
        {
            public Regex Prefix;
            public Regex Infix;
        }
        private static Dictionary<string, ArithmeticStruct> Arithmetic = new Dictionary<string, ArithmeticStruct>()
        {
            {
                "*",
                new ArithmeticStruct() {
                    Prefix = new Regex(@"(?:multiply|the product of) (.+)? (?:by|and) (.+)?"),
                    Infix = new Regex(@"(.+)? (?:multiplied with|times) (.+)?"),
                }
            },
            {
                "/",
                new ArithmeticStruct() {
                    Prefix = new Regex(@"divide (.+)? (?:by|and) (.+)?"),
                    Infix = new Regex(@"(.+)? (?:divided by|over) (.+)?"),
                }
            },
            {
                "%",
                new ArithmeticStruct() {
                    Prefix = new Regex(@"the remainder of (.+)? and (.+)?"),
                    Infix = new Regex(@"(.+)? (?:mod(?:ulo)?) (.+)?"),
                }
            },
            {
                "+",
                new ArithmeticStruct() {
                    Prefix = new Regex(@"add (.+)? and (.+)?"),
                    Infix = new Regex(@"(.+)? (?:added to|plus) (.+)?"),
                }
            },
            {
                "-",
                new ArithmeticStruct() {
                    Prefix = new Regex(@"(?:subtract|the difference between) (.+)? and (.+)?"),
                    Infix = new Regex(@"(.+)? (?:minus|without) (.+)?"),
                }
            },
        };
        public struct ArithmeticCheckResult
		{
            public string Left;
            public string Expression;
            public string Right;
		}
        public static bool IsArithmetic(string content, out ArithmeticCheckResult result)
		{
            result = new ArithmeticCheckResult();

            foreach(string type in Arithmetic.Keys)
			{
                var keywords = Arithmetic[type];
                Match match = null;

                if(keywords.Prefix.IsMatch(content)) match = keywords.Prefix.Match(content);
                else if(keywords.Infix.IsMatch(content)) match = keywords.Infix.Match(content);

                if( match != null )
				{
                    result.Left = match.Groups[1].Value;
                    result.Right = match.Groups[2].Value;
                    result.Expression = type;
                    return true;
                }
            }

            return false;
		}

        public object GetValue(FiMClass reportClass) => this.NodeTree.Eval(reportClass);

        private abstract class BaseNode
        {
            public abstract dynamic Eval(FiMClass reportClass);
        }
        private class ExpressionNode : BaseNode
        {
            public BaseNode Left;
            public BaseNode Right;
            public string Expression;
            public override dynamic Eval(FiMClass reportClass)
            {
                switch (this.Expression)
                {
                    case "+": return Left.Eval(reportClass) + Right.Eval(reportClass);
                    case "-": return Left.Eval(reportClass) - Right.Eval(reportClass);
                    case "*": return Left.Eval(reportClass) * Right.Eval(reportClass);
                    case "/": return Left.Eval(reportClass) / Right.Eval(reportClass);
                    case "%": return Left.Eval(reportClass) % Right.Eval(reportClass);
                    default: throw new FiMException("Invalid expression " + this.Expression);
                }
            }
        }
        private class ValueNode : BaseNode
        {
            public string RawValue;
            public override dynamic Eval(FiMClass reportClass)
            {
				var value = new KirinValue(this.RawValue, reportClass);

				if (value.Type == KirinVariableType.STRING)
					return Convert.ToString(value.Value);
                
				if (value.Type == KirinVariableType.NUMBER)
					return Convert.ToDouble(value.Value);

				throw new FiMException("Cannot do arithmetic on a non-number value");
            }
        }
    }
}
