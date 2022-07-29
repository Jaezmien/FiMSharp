using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace FiMSharp.Kirin
{
	static class KirinValueHelper
	{
		public static char AsChar(this KirinValue value)
		{
			if (value.Type != KirinVariableType.CHAR) throw new Exception("Value is not a character");
			return Convert.ToChar(value.Value);
		}
		public static string AsString(this KirinValue value)
		{
			if (value.Type != KirinVariableType.STRING) throw new Exception("Value is not a string");
			return Convert.ToString(value.Value);
		}
		public static double AsNumber(this KirinValue value)
		{
			if (value.Type != KirinVariableType.NUMBER) throw new Exception("Value is not a number");
			return Convert.ToDouble(value.Value);
		}
		public static bool AsBoolean(this KirinValue value)
		{
			if (value.Type != KirinVariableType.BOOL) throw new Exception("Value is not a boolean");
			return Convert.ToBoolean(value.Value);
		}
		public static Dictionary<int, string> AsStringDictionary(this KirinValue value)
		{
			if (value.Type != KirinVariableType.STRING_ARRAY) throw new Exception("Value is not a string array");
			return (value.Value as Dictionary<int, object>).ToDictionary(e => e.Key, v => (string)v.Value);
		}
		public static Dictionary<int, double> AsNumberDictionary(this KirinValue value)
		{
			if (value.Type != KirinVariableType.NUMBER_ARRAY) throw new Exception("Value is not a number array");
			return (value.Value as Dictionary<int, object>).ToDictionary(e => e.Key, v => (double)v.Value);
		}
		public static Dictionary<int, bool> AsBooleanDictionary(this KirinValue value)
		{
			if (value.Type != KirinVariableType.BOOL_ARRAY) throw new Exception("Value is not a boolean array");
			return (value.Value as Dictionary<int, object>).ToDictionary(e => e.Key, v => (bool)v.Value);
		}
	}
	public class KirinValue : KirinBaseNode
	{
		public KirinValue(string raw, FiMReport report)
		{
			this.Raw = raw;
			this.Report = report;
			if (raw != null) this.Load();
		}
		public KirinValue(string raw, FiMReport report, KirinVariableType forcedType)
		{
			this.Raw = raw;
			this.Report = report;
			this.ForcedType = forcedType;
			this.Load();
		}
		public KirinValue(object value)
		{
			this.Raw = Convert.ToString(value);
			this._Value = value;

			if (this._Value.GetType() == typeof(int)) this._Value = Convert.ToDouble(value);
		}

		public FiMReport Report;
		public string Raw;
		public bool Constant;
		private object _Value;
		public object Value
		{
			get
			{
				return this._Value;
			}
			set
			{
				if (Constant) throw new Exception("Cannot modify a const variable");
				if (FiMHelper.AsVariableType(value) == this.Type)
				{
					this._Value = value;
				}
				else
				{
					if (this.Type == KirinVariableType.STRING)
					{
						this._Value = Convert.ToString(value);
					}
					else
					{
						throw new Exception("Expected " + this.Type.AsNamedString() + ", got " + FiMHelper.AsVariableType(value));
					}
				}
			}
		}

		/*public dynamic TrueValue
		{
			get
			{
				if (_Value == null) throw new Exception("Cannot get true value of a null variable");
				switch(this.Type)
				{
					case KirinVariableType.CHAR: return Convert.ToChar(this.Value);
					case KirinVariableType.STRING: return Convert.ToString(this.Value);
					case KirinVariableType.NUMBER: return Convert.ToDouble(this.Value);
					case KirinVariableType.BOOL: return Convert.ToBoolean(this.Value);
					case KirinVariableType.STRING_ARRAY:
						return (this.Value as Dictionary<int, object>).ToDictionary(s => s.Key, s => Convert.ToString(s.Value));
					case KirinVariableType.NUMBER_ARRAY:
						return (this.Value as Dictionary<int, object>).ToDictionary(s => s.Key, s => Convert.ToDouble(s.Value));
					case KirinVariableType.BOOL_ARRAY:
						return (this.Value as Dictionary<int, object>).ToDictionary(s => s.Key, s => Convert.ToBoolean(s.Value));
				}
				throw new Exception("Cannot get true value");
			}
		}*/

		public KirinValue Load()
		{
			if (this.Raw == null)
			{
				if (FiMHelper.IsTypeArray(this.Type))
				{
					this._Value = FiMHelper.GetDefaultValue(this.Type);
				}
				else
				{
					if (this.ForcedType == null) throw new Exception("Value is null");
					this._Value = FiMHelper.GetDefaultValue((KirinVariableType)this.ForcedType);
				}
			}
			else
			{
				string raw = this.Raw;

				var eType = FiMHelper.DeclarationType.Determine(" " + raw, out string eKeyword, false);
				if (eType != KirinVariableType.UNKNOWN)
				{
					raw = raw.Substring(eKeyword.Length);
					this.ForceType(eType);
				}

				object value;
				if (KirinLiteral.TryParse(raw, out object lResult)) value = lResult;
				else
				{
					value = KirinValue.Evaluate(Report, raw, out var returnedType, ForcedType);
					this.ForceType(returnedType);
				}

				if (eType != KirinVariableType.UNKNOWN && FiMHelper.AsVariableType(value) != eType)
					throw new Exception("Expected " + eType.AsNamedString() + ", got " + FiMHelper.AsVariableType(value));
				this._Value = value;
			}
			return this;
		}

		private KirinVariableType? ForcedType = null;
		public KirinVariableType Type
		{
			get
			{
				return this.ForcedType ?? FiMHelper.AsVariableType(this.Value);
			}
		}

		public void ForceType(KirinVariableType type)
		{
			if (this.ForcedType != null) return;
			this.ForcedType = type;
		}

		/// <summary>
		/// Evaluates raw FiM++ string into a value
		/// </summary>
		public static object Evaluate(
			FiMReport report,
			string evaluatable,
			KirinVariableType? expectedType = null
		)
		{
			return Evaluate(report, evaluatable, out _, expectedType);
		}

		/// <summary>
		/// Evaluates raw FiM++ string into a value
		/// </summary>
		public static object Evaluate(
			FiMReport report,
			string evaluatable,
			out KirinVariableType returnedType,
			KirinVariableType? expectedType = null
		)
		{
			returnedType = KirinVariableType.UNKNOWN;

			// Nothing
			if (evaluatable == "nothing" && expectedType != null)
			{
				returnedType = (KirinVariableType)expectedType;
				return FiMHelper.GetDefaultValue(returnedType);
			}

			// Calling an existing variable
			if (report.Variables.Exists(evaluatable))
			{
				var variable = report.Variables.Get(evaluatable);
				returnedType = variable.Type;
				return variable.Value;
			}

			// Calling an existing method
			if (report.Paragraphs.FindIndex(v => evaluatable.StartsWith(v.Name)) > -1)
			{
				KirinValue[] args = null;
				string pName = evaluatable;

				if (pName.Contains(KirinFunctionCall.FunctionParam))
				{
					int pIndex = pName.IndexOf(KirinFunctionCall.FunctionParam);
					pName = pName.Substring(0, pIndex);
					args = KirinFunctionCall.ParseCallArguments(
						evaluatable.Substring(pName.Length + KirinFunctionCall.FunctionParam.Length), report
					).ToArray();
				}

				if (report.Paragraphs.FindIndex(v => v.Name == pName) == -1)
					throw new Exception("Paragraph " + pName + " not found");

				var paragraph = report.Paragraphs.Find(v => v.Name == pName);
				if (paragraph.ReturnType == KirinVariableType.UNKNOWN)
					throw new Exception("Paragraph returns nothing");
				returnedType = paragraph.ReturnType;
				return paragraph.Execute(args);
			}

			// Array
			if (expectedType != null && FiMHelper.IsTypeArray((KirinVariableType)expectedType))
			{
				var dict = new Dictionary<int, object>();
				var args = KirinFunctionCall.ParseCallArguments(evaluatable, report);

				if (!FiMHelper.IsTypeOfArray(args[0].Type, (KirinArrayType)expectedType))
					throw new Exception("Invalid list value type");
				if (!args.All(a => a.Type == args[0].Type))
					throw new Exception("Unidentical list value type");

				int i = 1;
				args.ForEach(kv => dict.Add(i++, kv.Value));

				returnedType = (KirinVariableType)expectedType;
				return dict;
			}

			// Array count
			if (Regex.IsMatch(evaluatable, @"^count of (.+)"))
			{
				var match = Regex.Match(evaluatable, @"^count of (.+)");
				string varName = match.Groups[1].Value;
				if (!report.Variables.Exists(varName)) throw new Exception("Variable " + varName + " does not exist");
				var variable = report.Variables.Get(varName);

				if (!FiMHelper.IsTypeArray(variable.Type) && variable.Type != KirinVariableType.STRING)
					throw new Exception("Cannot get count of a non-array variable");

				returnedType = KirinVariableType.NUMBER;
				if (variable.Type == KirinVariableType.STRING)
				{
					return Convert.ToString(variable.Value).Length;
				}
				else
				{
					var array = variable.Value as Dictionary<int, object>;
					return array.Count;
				}
			}

			// Array index (explicit)
			if (Regex.IsMatch(evaluatable, @"^(.+) of (.+)$"))
			{
				var match = Regex.Match(evaluatable, @"^(.+) (of) (.+)$");

				if (!FiMHelper.IsIndexInsideString(evaluatable, match.Groups[2].Index))
				{
					string strIndex = match.Groups[1].Value;
					var varIndex = new KirinValue(strIndex, report);
					if (varIndex.Type != KirinVariableType.NUMBER) throw new Exception("Invalid index value");
					int index = Convert.ToInt32(varIndex.Value);

					string strVar = match.Groups[3].Value;
					if (!report.Variables.Exists(strVar)) throw new Exception("Variable " + strVar + " does not exist");
					var variable = report.Variables.Get(strVar);
					if (!FiMHelper.IsTypeArray(variable.Type) && variable.Type != KirinVariableType.STRING)
						throw new Exception("Cannot index a non-array variable");

					if (variable.Type == KirinVariableType.STRING)
					{
						returnedType = KirinVariableType.CHAR;
						return Convert.ToString(variable.Value)[index - 1];
					}

					var dict = variable.Value as Dictionary<int, object>;
					if (variable.Type == KirinVariableType.STRING_ARRAY)
					{
						returnedType = KirinVariableType.STRING;
						return Convert.ToString(dict[index]);
					}
					if (variable.Type == KirinVariableType.BOOL_ARRAY)
					{
						returnedType = KirinVariableType.BOOL;
						return Convert.ToBoolean(dict[index]);
					}
					if (variable.Type == KirinVariableType.NUMBER_ARRAY)
					{
						returnedType = KirinVariableType.NUMBER;
						return Convert.ToDouble(dict[index]);
					}
				}
			}
			// Array index (implicit)
			if (Regex.IsMatch(evaluatable, @"^(.+) (\d+)$"))
			{
				var match = Regex.Match(evaluatable, @"^(.+) (\d+)$");
				string varName = match.Groups[1].Value;
				if (report.Variables.Exists(varName))
					return Evaluate(report, $"{match.Groups[2].Value} of {varName}", out returnedType, expectedType);
			}

			// Arithmetic
			if (KirinArithmetic.IsArithmetic(evaluatable, out var arithmeticResult))
			{
				var arithmetic = new KirinArithmetic(arithmeticResult);
				returnedType = KirinVariableType.NUMBER;
				return arithmetic.GetValue(report);
			}
			// Conditional
			if (KirinConditional.IsConditional(evaluatable, out var conditionalResult))
			{
				var conditional = new KirinConditional(conditionalResult);
				returnedType = KirinVariableType.BOOL;
				return conditional.GetValue(report);
			}

			// String concatenation
			if (evaluatable.Contains("\""))
			{
				StringBuilder finalValue = new StringBuilder();

				StringBuilder buffer = new StringBuilder();
				bool isInString = false;
				bool escapeNextChar = false;
				foreach (char c in evaluatable)
				{
					if (c == '\\')
					{
						escapeNextChar = true;
						continue;
					}

					if (escapeNextChar)
					{
						escapeNextChar = false;
						buffer.Append(c);
						continue;
					}

					if (c == '"')
					{
						if (buffer.Length > 0)
						{
							string value = buffer.ToString();

							if (isInString) finalValue.Append(value);
							else finalValue.Append(Evaluate(report, value));

							buffer.Clear();
						}

						isInString = !isInString;
						continue;
					}
					buffer.Append(c);
				}

				if (buffer.Length > 0)
				{
					string value = buffer.ToString();
					if (isInString) finalValue.Append(value);
					else finalValue.Append(Evaluate(report, value));
				}

				returnedType = KirinVariableType.STRING;
				return finalValue.ToString();
			}

			throw new Exception("Cannot evaluate " + evaluatable);
		}
		public static bool ValidateName(string name)
		{
			if (name.StartsWith("\"") && name.EndsWith("\"")) return false;
			if (Regex.IsMatch(name, @"\d")) return false;
			if (FiMConstants.Keywords.Any(kw => name.Contains($" {kw}") || name.Contains($"{kw} "))) return false;
			if (name.Contains("'s ")) return false;
			return true;
		}

		public static bool IsEqual(object _x, object _y)
		{
			dynamic x;
			dynamic y;

			var lvt = FiMHelper.AsVariableType(_x);
			var rvt = FiMHelper.AsVariableType(_y);

			if (lvt != rvt) return false;

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
		public static bool IsEqual(KirinValue x, KirinValue y) => KirinValue.IsEqual(x.Value, y.Value);
	}

	public class KirinLiteral
	{
		private readonly static Regex StringCheck = new Regex("^\"[^\"]+\"$");
		private static class Boolean
		{
			public static readonly string[] True = { "correct", "right", "true", "yes" };
			public static readonly string[] False = { "false", "incorrect", "no", "wrong" };
		}
		public static char CharAsLiteral(char input)
		{
			switch (input)
			{
				case '0': return '\0';
				case 'r': return '\r';
				case 'n': return '\n';
				case 't': return '\t';
			}
			return input;
		}
		public static bool TryParse(string content, out object result)
		{
			if (StringCheck.IsMatch(content))
			{
				string str = content.Substring(1, content.Length - 2);
				var sb = new StringBuilder();
				for (int i = 0; i < str.Length; i++)
				{
					char c = str[i];
					if (c != '\\' || i + 1 >= str.Length - 1)
					{
						sb.Append(c);
						continue;
					}

					char nc = str[++i];
					char lc = CharAsLiteral(nc);
					if (lc == nc)
					{
						sb.Append(c);
						sb.Append(nc);
						continue;
					}
					else
					{
						sb.Append(lc);
					}
				}

				result = sb.ToString();
				return true;
			}
			if (content.StartsWith("'") && content.EndsWith("'") && ((content.Length == 3) || (content.Length == 4 && content[1] == '\\')))
			{
				result = content[1] == '\\' ? CharAsLiteral(content[2]) : content[1];
				return true;
			}
			if (Boolean.True.Any(v => v == content) || Boolean.False.Any(v => v == content))
			{
				result = Boolean.True.Any(v => v == content);
				return true;
			}
			if (double.TryParse(content, out double fValue))
			{
				result = fValue;
				return true;
			}

			result = null;
			return false;
		}
	}
}
