using System;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using FiMSharp;
using FiMSharp.Kirin;

namespace FiMSharp.Changeling
{
	public class Javascript
	{
		public struct JavascriptInternalFunction
		{
			public string Name;
			/// <summary>
			/// Format: <c>function fim_[NAME](){return ...;}</c>
			/// </summary>
			public string Function;
		}

		private struct JavascriptContainer
		{
			public FiMReport report;
			public List<string> globalVariables;
			public Dictionary<string, string> internalFunctions;

			public string output;
			public string outputNewline;
			public string input;
		}

		public static string Transpile(
			FiMReport report,
			string indent = "    ",
			Func<string, JavascriptInternalFunction> onInternalFunction = null,
			string output = "console.log",
			string outputNewline = "console.log",
			string input = "prompt"
		) {
			StringBuilder sb = new StringBuilder();

			var container = new JavascriptContainer()
			{
				report = report,
				globalVariables = new List<string>(),
				internalFunctions = new Dictionary<string, string>(),

				output = output,
				outputNewline = outputNewline,
				input = input,
			};

			sb.AppendLine("'use strict';");

			sb.AppendLine("");

			sb.AppendLine( "/**");
			sb.AppendLine( " * ");
			sb.AppendLine($" * This report, entitled \"{ report.Info.Name }\", was written by: { report.Author.Name } ");
			sb.AppendLine( " * ");
			sb.AppendLine( " */");

			sb.AppendLine("");

			// Helpers
			// fim_index(x,y) = Index arrays normally, minus one for strings.
			sb.AppendLine("function fim_index(x,y){return y-(Array.isArray(x)?0:1)}");
			// fim_forin(x) = Removes the first (null) element if passed an array
			sb.AppendLine("function fim_forin(x){return Array.isArray(x)?x.slice(1):x}");

			Dictionary<string, string> internalHelpers = new Dictionary<string, string>();
			foreach(var paragraph in report.Paragraphs.Reverse().Where(p => p.FunctionType == "KirinInternalFunction"))
			{
				if (onInternalFunction == null)
					throw new Exception("Report requests an internal function conversion: " + paragraph.Name);

				var iFunc = onInternalFunction(paragraph.Name);
				sb.AppendLine(iFunc.Function);
				container.internalFunctions.Add(paragraph.Name, iFunc.Name);
			}

			sb.AppendLine("");

			sb.AppendLine($"const { SanitizeName(report.Info.Recipient) } = function() {{}}");
			string name = SanitizeName(report.Info.Name);
			if (int.TryParse(name[0].ToString(), out int _)) name = $"_{name}";
			sb.AppendLine($"function { name }() {{");

			foreach(var variable in report.Variables.GlobalVariables.Reverse())
			{
				sb.AppendLine($"{ indent }this.{ SanitizeName(variable.Name) } = { SanitizeObject(variable.Value, variable.Type) }");
				container.globalVariables.Add(variable.Name);
			}
			foreach (var paragraph in report.Paragraphs.Reverse().Where(p => p.FunctionType != "KirinInternalFunction"))
			{
				var func = (KirinFunction)paragraph.Function;
				string args = "";
				if( func.Arguments != null )
				{
					args = string.Join(", ",
						func.Arguments.Select(a =>
							$"/** @type {{{ KirinTypeAsJavascriptType(a.Type) }}} */ { SanitizeName(a.Name) } = { DefaultValue(a.Type) }"
						)
					);
				}

				sb.AppendLine($"{ indent }this.{ SanitizeName(paragraph.Name) } = function({ args }) {{");
				TranspileStatements(sb, container, func.Statement, indent, 2);
				sb.AppendLine($"{ indent }}}");
			}

			if(report.MainParagraph != null)
			{
				sb.AppendLine("");
				sb.AppendLine($"{ indent }this.today = function() {{");
				sb.AppendLine($"{ indent }{ indent }this.{ SanitizeName(report.MainParagraph.Name) }();");
				sb.AppendLine($"{ indent }}}");
			}

			sb.AppendLine($"}}");
			sb.AppendLine($"{name}.prototype = new {SanitizeName(report.Info.Recipient)}();");

			sb.AppendLine("");

			sb.AppendLine($"new {name}().today();");
			return sb.ToString();
		}

		private static void TranspileStatements(
			StringBuilder sb,
			JavascriptContainer container,
			KirinStatement statement,
			string indent,
			int depth = 1
		) {
			string _i = string.Concat(Enumerable.Repeat(indent, depth));
			string __i = string.Concat(Enumerable.Repeat(indent, depth + 1));
			string ___i = string.Concat(Enumerable.Repeat(indent, depth + 2));

			foreach (var node in statement.Body)
			{
				switch (node.NodeType)
				{
					case "KirinPrint":
						{
							var n = (KirinPrint)node;

							sb.AppendLine($"{_i}{ (n.NewLine ? container.outputNewline : container.output) }({ SanitizeValue(n.RawParameters, container) });");
						}
						break;
					case "KirinFunctionCall":
						{
							var n = (KirinFunctionCall)node;

							string args = "";
							if (n.RawParameters != string.Empty)
							{
								args = string.Join(", ",
									n.RawParameters
										.Split(new string[] { " and " }, StringSplitOptions.None)
										.Select(v => SanitizeValue(v, container))
								);
							}

							sb.AppendLine($"{_i}this.{ SanitizeName(n.FunctionName, container) }({ args });");
						}
						break;
					case "KirinVariableDeclaration":
						{
							var n = (KirinVariableDeclaration)node;

							string val = SanitizeValue(n.RawValue, container, n.ExpectedType);
							string modif = n.Constant ? "const" : "let";

							sb.AppendLine($"{_i}{modif} { SanitizeName(n.Name, container) } = { val };");
						}
						break;
					case "KirinListModify":
						{
							var n = (KirinListModify)node;

							string index = SanitizeValue(n.RawIndex, container);
							string value = SanitizeValue(n.RightOp, container);
							sb.AppendLine($"{_i}{ SanitizeName(n.LeftOp, container) }[{ index }] = { value };");
						}
						break;
					case "KirinUnary":
						{
							var n = (KirinUnary)node;

							string value = SanitizeValue(n.RawVariable, container);

							if( n.Increment )
							{
								sb.AppendLine($"{_i}{ value } = ({ value } || 0) + 1.0");
							}
							else
							{
								sb.AppendLine($"{_i}{ value } = ({ value } || 0) - 1.0");
							}
						}
						break;
					case "KirinInput":
						{
							var n = (KirinInput)node;

							string name = SanitizeName(n.RawVariable, container);
							string prompt = $"{ n.RawVariable } is asking for an input: ";
							if( n.PromptString != string.Empty ) prompt = n.PromptString;

							sb.AppendLine($"{_i}{name} = { container.input }(\"{ prompt }\");");
						}
						break;
					case "KirinIfStatement":
						{
							var n = (KirinIfStatement)node;

							for(int i = 0; i < n.Conditions.Count; i++)
							{
								var c = n.Conditions[i];

								string ifType = i == 0 ? "if" : "else if";

								if( i + 1 == n.Conditions.Count &&
									c.Condition.Left == "correct" && c.Condition.Expression == "==" && c.Condition.Right == "correct" )
									ifType = "else";

								string check = ParseConditionalNodes(c.Condition, container);
								if (ifType != "else") ifType += $" ({ check })";

								sb.AppendLine($"{_i}{ifType} {{");
								TranspileStatements(sb, container, c.Statement, indent, depth + 1);
								sb.AppendLine($"{_i}}}");
							}
						}
						break;

					case "KirinForToLoop":
						{
							var n = (KirinForToLoop)node;

							string name = SanitizeName(n.VariableName, container);
							string min = SanitizeValue(n.RawFrom, container);
							string max = SanitizeValue(n.RawTo, container);
							string interval = SanitizeValue(n.RawInterval, container);

							// HACK: uh.
							if (interval == string.Empty)
								interval = $"({min} <= {max} ? 1 : -1)";

							string check = $"({interval} > 0 ? {name} <= {max} : {name} >= {max})";

							sb.AppendLine($"{_i}for (let { name } = {min}; {check}; { name } += { interval }) {{");
							TranspileStatements(sb, container, n.Statement, indent, depth + 1);
							sb.AppendLine($"{_i}}}");
						}
						break;
					case "KirinForInLoop":
						{
							var n = (KirinForInLoop)node;

							string name = SanitizeName(n.VariableName, container);
							string value = SanitizeValue(n.RawValue, container);

							sb.AppendLine($"{_i}for (let {name} of fim_forin({value})) {{");
							TranspileStatements(sb, container, n.Statement, indent, depth + 1);
							sb.AppendLine($"{_i}}}");
						}
						break;

					case "KirinWhileLoop":
						{
							var n = (KirinWhileLoop)node;

							sb.AppendLine($"{_i}while ({ ParseConditionalNodes(n.Condition, container) }) {{");
							TranspileStatements(sb, container, n.Statement, indent, depth + 1);
							sb.AppendLine($"{_i}}}");
						}
						break;

					case "KirinSwitch":
						{
							var n = (KirinSwitch)node;

							sb.AppendLine($"{_i}switch ({ SanitizeName(n.RawVariable, container) }) {{");

							if(n.Cases.Count > 0)
							{
								foreach(var c in n.Cases)
								{
									string condition = SanitizeName(c.Key, container);

									if (KirinSwitchCase.IsValidNumberPlace(condition, out var index))
										condition = index.ToString();

									sb.AppendLine($"{__i}case ({ condition }): {{");
									TranspileStatements(sb, container, c.Value, indent, depth + 2);
									sb.AppendLine($"{__i}}}");
									sb.AppendLine($"{__i}break;");
								}
							}
							if(n.DefaultCase != null)
							{
								sb.AppendLine($"{__i}default: {{");
								TranspileStatements(sb, container, n.DefaultCase, indent, depth + 2);
								sb.AppendLine($"{__i}}}");
								sb.AppendLine($"{__i}break;");
							}

							sb.AppendLine($"{_i}}}");
						}
						break;

					case "KirinVariableModify":
						{
							var n = (KirinVariableModify)node;

							sb.AppendLine($"{_i}{SanitizeName(n.LeftOp, container)} = {SanitizeValue(n.RightOp, container)};");
						}
						break;
					case "KirinReturn":
						{
							var n = (KirinReturn)node;

							sb.AppendLine($"{_i}return { SanitizeValue(n.RawParameters, container) };");
						}
						break;

					default:
						{
							sb.AppendLine($"{_i}{node.NodeType}");
						}
						break;
				}
			}
		}

		private static string SanitizeObject(object value, KirinVariableType valueType)
		{
			switch( valueType )
			{
				case KirinVariableType.STRING: return $"\"{ value }\"";
				case KirinVariableType.CHAR: return $"\'{ value }\'";
				case KirinVariableType.BOOL: return (bool)value == true ? "true" : "false";
				case KirinVariableType.NUMBER: return Convert.ToDouble(value).ToString();

				case KirinVariableType.NUMBER_ARRAY:
					{
						IDictionary dict = value as IDictionary;
						return $"[,{ string.Join(", ", dict.Values.Cast<double>()) }]";
					}
				case KirinVariableType.STRING_ARRAY:
					{
						IDictionary dict = value as IDictionary;
						return $"[,{ string.Join(", ", dict.Values.Cast<string>().Select(s => $"\"{s}\"")) }]";
					}
				case KirinVariableType.BOOL_ARRAY:
					{
						IDictionary dict = value as IDictionary;
						return $"[,{ string.Join(", ", dict.Values.Cast<bool>().Select(b => (bool)b == true ? "true" : "false")) }]";
					}
				default: return "";
			}
		}
		private static string SanitizeName(string value, JavascriptContainer? container = null)
        {
            string newValue = value.Replace("_", "__").Replace(" ", "_").Replace("'", "_").Replace("-", "_");
			if (container != null && ((JavascriptContainer)container).globalVariables.Contains(value))
				newValue = $"this.{ newValue }";
			return newValue;
        }
		private static string SanitizeValue(string value, JavascriptContainer container, KirinVariableType? expectedType = null)
		{
			var eType = FiMHelper.DeclarationType.Determine(" " + value, out string eKeyword, strict: false);
			if (eType != KirinVariableType.UNKNOWN)
			{
				value = value.Substring(eKeyword.Length);
				expectedType = eType;
			}

			// Nothing
			if (( value == null || value == "nothing") && expectedType != null)
			{
				return DefaultValue((KirinVariableType)expectedType);
			}

			if (KirinLiteral.TryParse(value, out object val))
			{
				if (val.GetType() == typeof(bool)) return (bool)val == true ? "true" : "false";
				return value;
			}

			// Calling an existing method
			if (container.report.GetParagraphLazy(value) != null)
			{
				string args = "";
				string pName = value;

				if (pName.Contains(KirinFunctionCall.FunctionParam))
				{
					int pIndex = pName.IndexOf(KirinFunctionCall.FunctionParam);
					pName = pName.Substring(0, pIndex);
					args = string.Join(", ",
								value
								.Substring(pName.Length + KirinFunctionCall.FunctionParam.Length)
								.Split(new string[] { " and " }, StringSplitOptions.None)
								.Select(v => SanitizeValue(v, container))
							);
				}

				if (container.internalFunctions.ContainsKey(pName))
					return $"{ container.internalFunctions[pName] }({ args })";
				return $"this.{ SanitizeName(pName, container) }({ args })";
			}

			// Array
			if (expectedType != null && FiMHelper.IsTypeArray((KirinVariableType)expectedType))
			{
				string args = string.Join(", ",
								value
									.Split(new string[] { " and " }, StringSplitOptions.None)
									.Select(v => SanitizeValue(v, container))
							);

				return $"[, { args }]";
			}

			// Array index
			if (IsArrayIndex(value))
			{
				var match = GetArrayIndex(value);

				var index = SanitizeValue(match.RawIndex, container);
				var variable = SanitizeValue(match.RawVariable, container);

				return $"{ variable }[ fim_index({ variable }, {index}) ]";
			}

			// Arithmetic
			if (KirinArithmetic.IsArithmetic(value, out var arithmeticResult))
			{
				return ParseArithmeticNodes(arithmeticResult, container);
			}

			// Conditional
			if (KirinConditional.IsConditional(value, out var conditionalResult))
			{
				return ParseConditionalNodes(conditionalResult, container);
			}

			// Calling an existing variable (hopefully)
			return SanitizeName(value, container); 
		}
		private static string DefaultValue(KirinVariableType type)
		{
			if (type == KirinVariableType.STRING) return "\"\"";
			if (type == KirinVariableType.CHAR) return "\"\0\"";
			if (type == KirinVariableType.NUMBER) return "0.0";
			if (type == KirinVariableType.BOOL) return "false";
			if (type == KirinVariableType.STRING_ARRAY) return "[,]";
			if (type == KirinVariableType.NUMBER_ARRAY) return "[,]";
			if (type == KirinVariableType.BOOL_ARRAY) return "[,]";

			return "";
		}
		private static string KirinTypeAsJavascriptType(KirinVariableType type)
		{
			switch( type )
			{
				case KirinVariableType.STRING: return "string";
				case KirinVariableType.NUMBER: return "number";
				case KirinVariableType.BOOL: return "boolean";
				case KirinVariableType.CHAR: return "string";
				case KirinVariableType.BOOL_ARRAY: return "boolean[]";
				case KirinVariableType.STRING_ARRAY: return "string[]";
				case KirinVariableType.NUMBER_ARRAY: return "number[]";
			}

			return string.Empty;
		}

		private static bool IsArrayIndex(string content)
		{
			return GetArrayIndex(content) != null;
		}
		private static FiMHelper.ArrayIndex GetArrayIndex(string content)
		{
			// Explicit index
			if (Regex.IsMatch(content, @"^(.+) of (.+)$"))
			{
				var match = Regex.Match(content, @"^(.+) (of) (.+)$");

				if (!FiMHelper.IsIndexInsideString(content, match.Groups[2].Index))
				{
					string strIndex = match.Groups[1].Value;
					string strVar = match.Groups[3].Value;

					// (somewhat) stricter check
					if (KirinValue.ValidateName(strVar))
					{
						return new FiMHelper.ArrayIndex()
						{
							RawIndex = strIndex,
							RawVariable = strVar
						};
					}
				}
			}

			// Implicit index
			if (Regex.IsMatch(content, @"^(.+) (\d+)$"))
			{
				var match = Regex.Match(content, @"^(.+) (\d+)$");
				string varName = match.Groups[1].Value;
				string varIndex = match.Groups[2].Value;

				// (somewhat) stricter check
				if ( KirinValue.ValidateName(varName) )
				{
					return new FiMHelper.ArrayIndex()
					{
						RawIndex = varIndex,
						RawVariable = varName,
					};
				}
			}

			return null;
		}
    
		private static string ParseConditionalNodes(KirinConditional.ConditionalCheckResult tree, JavascriptContainer container)
		{
			string left = ParseConditionalNodes(tree.Left, container);
			string right = ParseConditionalNodes(tree.Right, container);
			string exp = tree.Expression;

			return $"{ left } { exp } { right }";
		}
		private static string ParseConditionalNodes(string value, JavascriptContainer container)
		{
			if (KirinConditional.IsConditional(value, out var tree)) return ParseConditionalNodes(tree, container);
			return SanitizeValue(value, container);
		}

		private static string ParseArithmeticNodes(KirinArithmetic.ArithmeticCheckResult tree, JavascriptContainer container)
		{
			string left = ParseArithmeticNodes(tree.Left, container);
			string right = ParseArithmeticNodes(tree.Right, container);
			string exp = tree.Expression;

			return $"{ left } { exp } { right }";
		}
		private static string ParseArithmeticNodes(string value, JavascriptContainer container)
		{
			if (KirinArithmetic.IsArithmetic(value, out var tree)) return ParseArithmeticNodes(tree, container);
			return SanitizeValue(value, container);
		}
	}
}
