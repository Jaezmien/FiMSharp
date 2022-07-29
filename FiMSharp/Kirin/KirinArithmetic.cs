using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
    class KirinArithmetic : KirinBaseNode
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

        public double GetValue(FiMReport report) => this.NodeTree.Eval(report);

        private abstract class BaseNode
        {
            public abstract double Eval(FiMReport report);
        }
        private class ExpressionNode : BaseNode
        {
            public BaseNode Left;
            public BaseNode Right;
            public string Expression;
            public override double Eval(FiMReport report)
            {
                switch (this.Expression)
                {
                    case "+": return Left.Eval(report) + Right.Eval(report);
                    case "-": return Left.Eval(report) - Right.Eval(report);
                    case "*": return Left.Eval(report) * Right.Eval(report);
                    case "/": return Left.Eval(report) / Right.Eval(report);
                    case "%": return Left.Eval(report) % Right.Eval(report);
                    default: throw new Exception("Invalid expression " + this.Expression);
                }
            }
        }
        private class ValueNode : BaseNode
        {
            public string RawValue;
            public override double Eval(FiMReport report)
            {
                var value = new KirinValue(this.RawValue, report);
                if (value.Type != KirinVariableType.NUMBER) throw new Exception("Cannot do arithmetic on a non-number value");
                return Convert.ToDouble(value.Value);
            }
        }
    }
}
