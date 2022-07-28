using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinValue : KirinBaseNode
	{
		public KirinValue(string raw, FiMReport report)
		{
			this.Raw = raw;
			this.Report = report;
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
				if( this._Value == null )
				{
					var eType = FiMHelper.DeclarationType.Determine(" " + this.Raw, out string eKeyword, false);
					string raw = this.Raw;
					if (eType != KirinVariableType.UNKNOWN) raw = raw.Substring(eKeyword.Length);
					object value;
					if (KirinLiteral.TryParse(raw, out object lResult)) value = lResult;
					else value = KirinValue.Evaluate(Report, raw, ForcedVarType);
					if (eType != KirinVariableType.UNKNOWN)
					{
						if (FiMHelper.AsVariableType(value) != eType)
							throw new Exception("Expected " + eType.AsNamedString() + ", got " + FiMHelper.AsVariableType(value));
					}
					this._Value = value;
				}
				return this._Value;
			}
			set
			{
				if (Constant) throw new Exception("Cannot modify a const variable");
				if (FiMHelper.AsVariableType(value) != this.VarType)
					throw new Exception("Expected " + this.VarType.AsNamedString() + ", got " + FiMHelper.AsVariableType(value));
				this._Value = value;
			}
		}

		private KirinVariableType? ForcedVarType = null;
		public KirinVariableType VarType
		{
			get
			{
				return this.ForcedVarType ?? FiMHelper.AsVariableType(this.Value);
			}
		}

		public void ForceType(KirinVariableType type)
		{
			if (this.ForcedVarType != null) return;
			this.ForcedVarType = type;
		}

		/// <summary>
		/// Evaluates raw FiM++ string into a value
		/// </summary>
		public static object Evaluate(
			FiMReport report,
			string evaluatable,
			KirinVariableType? expectedType = null
		) {
			// Calling an existing variable
			if( report.Variables.Exists(evaluatable) )
				return report.Variables.Get(evaluatable).Value;

			// Calling an existing method
			if( report.Paragraphs.FindIndex(v => evaluatable.StartsWith(v.Name)) > -1 )
			{
				KirinValue[] args = null;
				string pName = evaluatable;

				if( pName.Contains(KirinFunctionCall.FunctionParam) )
				{
					int pIndex = pName.IndexOf(KirinFunctionCall.FunctionParam);
					pName = pName.Substring(0, pIndex);
					args = KirinFunctionCall.ParseCallArguments(
						pName.Substring(pName.Length + KirinFunctionCall.FunctionParam.Length), report
					).ToArray();
				}

				if (report.Paragraphs.FindIndex(v => v.Name == pName) == -1)
					throw new Exception("Paragraph " + pName + " not found");
				return report.Paragraphs.Find(v => v.Name == pName).Execute(args);
			}

			// Array
			if( expectedType != null && FiMHelper.IsTypeArray((KirinVariableType)expectedType) )
			{
				var list = FiMHelper.CreateArrayFromType((KirinArrayType)expectedType);
				var args = KirinFunctionCall.ParseCallArguments(evaluatable, report);

				if (!FiMHelper.IsTypeOfArray(args[0].VarType, (KirinArrayType)expectedType))
					throw new Exception("Invalid list value type");
				if (!args.All(a => a.VarType == args[0].VarType))
					throw new Exception("Unidentical list value type");

				// TODO: Is there a better way to do this?
				int i = 1;
				if (expectedType == KirinVariableType.STRING_ARRAY)
				{
					args.ForEach(kv =>
					{
						(list as Dictionary<int, string>).Add(i, (string)kv.Value);
						i++;
					});
				}
				else if (expectedType == KirinVariableType.NUMBER_ARRAY)
				{
					args.ForEach(kv =>
					{
						(list as Dictionary<int, double>).Add(i, (double)kv.Value);
						i++;
					});
				}
				else if (expectedType == KirinVariableType.BOOL_ARRAY)
				{
					args.ForEach(kv =>
					{
						(list as Dictionary<int, bool>).Add(i, (bool)kv.Value);
						i++;
					});
				}

				return args;
			}

			// String concatenation
			// return null;
			throw new NotImplementedException();
		}
		public static bool ValidateName(string name)
		{
			if (name.StartsWith("\"") && name.EndsWith("\"")) return false;
			if (Regex.IsMatch(name, @"\d")) return false;
			if (FiMConstants.Keywords.Any(kw => name.Contains($" {kw}") || name.Contains($"{kw} "))) return false;
			if (name.Contains("'s ")) return false;
			return true;
		}
	}

	public class KirinLiteral
	{
		private readonly static Regex StringCheck = new Regex("^\"[^\"]+\"$");
		private static class Boolean
		{
			public static readonly string[] True = { "correct", "right", "true", "yes" };
			public static readonly string[] False = { "false", "incorrect", "no", "wrong" };
		}
		private static char CharAsLiteral(char input)
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
				result = content.Substring(1, content.Length - 2);
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
