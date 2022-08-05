using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace FiMSharp.Kirin
{
	public class KirinValue : KirinBaseNode
	{
		public KirinValue(string raw, FiMClass reportClass)
		{
			this.Raw = raw;
			this.Class = reportClass;
			if (raw != null) this.Load();
		}
		public KirinValue(string raw, FiMClass reportClass, KirinVariableType forcedType)
		{
			this.Raw = raw;
			this.Class = reportClass;
			this.ForcedType = forcedType;
			this.Load();
		}
		public KirinValue(object value)
		{
			this.Raw = Convert.ToString(value);
			this._Value = value;

			if (this._Value.GetType() == typeof(int)) this._Value = Convert.ToDouble(value);
		}

		public FiMClass Class;
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
				if (Constant) throw new FiMException("Cannot modify a const variable");
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
						throw new FiMException("Expected " + this.Type.AsNamedString() + ", got " + FiMHelper.AsVariableType(value));
					}
				}
			}
		}

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
					if (this.ForcedType == null) throw new FiMException("Value is null");
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
					value = KirinValue.Evaluate(Class, raw, out var returnedType, ForcedType);
					this.ForceType(returnedType);
				}

				if (eType != KirinVariableType.UNKNOWN && FiMHelper.AsVariableType(value) != eType)
					throw new FiMException("Expected " + eType.AsNamedString() + ", got " + FiMHelper.AsVariableType(value));
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
			FiMClass reportClass,
			string evaluatable,
			KirinVariableType? expectedType = null
		)
		{
			return Evaluate(reportClass, evaluatable, out _, expectedType);
		}

		/// <summary>
		/// Evaluates raw FiM++ string into a value
		/// </summary>
		public static object Evaluate(
			FiMClass reportClass,
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
			if( reportClass.GetVariable(evaluatable) != null)
			{
				var variable = reportClass.GetVariable(evaluatable);
				returnedType = variable.Type;
				return variable.Value;
			}

			// Calling an existing method
			if( reportClass.GetParagraphLazy(evaluatable) != null)
			{
				KirinValue[] args = null;
				string pName = evaluatable;

				if (pName.Contains(KirinFunctionCall.FunctionParam))
				{
					int pIndex = pName.IndexOf(KirinFunctionCall.FunctionParam);
					pName = pName.Substring(0, pIndex);
					args = KirinFunctionCall.ParseCallArguments(
						evaluatable.Substring(pName.Length + KirinFunctionCall.FunctionParam.Length), reportClass
					).ToArray();
				}

				var paragraph = reportClass.GetParagraph(pName);
				if (paragraph == null) throw new FiMException("Paragraph " + pName + " not found");

				if (paragraph.ReturnType == KirinVariableType.UNKNOWN)
					throw new FiMException("Paragraph returns nothing");
				returnedType = paragraph.ReturnType;
				return paragraph.Execute(args);
			}

			// Array
			if (expectedType != null && FiMHelper.IsTypeArray((KirinVariableType)expectedType))
			{
				System.Collections.IDictionary dict = null;
				var args = KirinFunctionCall.ParseCallArguments(evaluatable, reportClass);

				if (!FiMHelper.IsTypeOfArray(args[0].Type, (KirinArrayType)expectedType))
					throw new FiMException("Invalid list value type");
				if (!args.All(a => a.Type == args[0].Type))
					throw new FiMException("Unidentical list value type");

				int i = 1;
				if( expectedType == KirinVariableType.STRING_ARRAY )
				{
					dict = new Dictionary<int, string>();
					args.ForEach(kv => dict.Add(i++, Convert.ToString(kv.Value)));
				}
				else if (expectedType == KirinVariableType.NUMBER_ARRAY)
				{
					dict = new Dictionary<int, double>();
					args.ForEach(kv => dict.Add(i++, Convert.ToDouble(kv.Value)));
				}
				else if (expectedType == KirinVariableType.BOOL_ARRAY)
				{
					dict = new Dictionary<int, bool>();
					args.ForEach(kv => dict.Add(i++, Convert.ToBoolean(kv.Value)));
				}

				returnedType = (KirinVariableType)expectedType;
				return dict;
			}

			// Array count
			if (Regex.IsMatch(evaluatable, @"^count of (.+)"))
			{
				var match = Regex.Match(evaluatable, @"^count of (.+)");
				string varName = match.Groups[1].Value;
				if (!reportClass.Variables.Has(varName)) throw new FiMException("Variable " + varName + " does not exist");
				var variable = reportClass.Variables.Get(varName);

				if (!FiMHelper.IsTypeArray(variable.Type) && variable.Type != KirinVariableType.STRING)
					throw new FiMException("Cannot get count of a non-array variable");

				returnedType = KirinVariableType.NUMBER;
				if (variable.Type == KirinVariableType.STRING)
				{
					return Convert.ToString(variable.Value).Length;
				}
				else
				{
					var array = (System.Collections.IDictionary)variable.Value;
					return array.Count;
				}
			}

			// Array index
			if( FiMHelper.ArrayIndex.IsArrayIndex(evaluatable, reportClass) )
			{
				var match = FiMHelper.ArrayIndex.GetArrayIndex(evaluatable, reportClass);

				var varIndex = new KirinValue(match.RawIndex, reportClass);
				if (varIndex.Type != KirinVariableType.NUMBER) throw new FiMException("Invalid index value");
				int index = Convert.ToInt32(varIndex.Value);

				string strVar = match.RawVariable;
				if (!reportClass.Variables.Has(strVar)) throw new FiMException("Variable " + strVar + " does not exist");
				var variable = reportClass.Variables.Get(strVar);
				if (!FiMHelper.IsTypeArray(variable.Type) && variable.Type != KirinVariableType.STRING)
					throw new FiMException("Cannot index a non-array variable");

				if (variable.Type == KirinVariableType.STRING)
				{
					returnedType = KirinVariableType.CHAR;
					return Convert.ToString(variable.Value)[index - 1];
				}

				var dict = variable.Value as System.Collections.IDictionary;
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

			// Arithmetic
			if (KirinArithmetic.IsArithmetic(evaluatable, out var arithmeticResult))
			{
				var arithmetic = new KirinArithmetic(arithmeticResult);
				returnedType = KirinVariableType.NUMBER;
				return arithmetic.GetValue(reportClass);
			}

			// Conditional
			if (KirinConditional.IsConditional(evaluatable, out var conditionalResult))
			{
				var conditional = new KirinConditional(conditionalResult);
				returnedType = KirinVariableType.BOOL;
				return conditional.GetValue(reportClass);
			}

			throw new FiMException("Cannot evaluate " + evaluatable);
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
		private readonly static Regex StringCheck = new Regex("^\"(.+)\"$");
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
			result = null;

			if (StringCheck.IsMatch(content))
			{
				string str = content.Substring(1, content.Length - 2);
				if (Regex.IsMatch(str, @"(?<!\\)""")) return false;
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

			return false;
		}
	}
}
