using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinVariableDeclaration : KirinExecutableNode
	{
		public KirinVariableDeclaration(int start, int length) : base(start, length) {  }

		public string Name;
		public string RawValue;
		public KirinVariableType ExpectedType;
		public bool Constant;

		private readonly static Regex Print = new Regex(@"^Did you know that (.+)");
		private readonly static string ConstantKW = " always";
		private static class InitKeyword
		{
			static readonly string[] Keywords = { "has", "is", "likes", "like", "was" };

			public static string GetKeyword(string subContent, out int index)
			{
				index = -1;

				if( Keywords.Any(kw => subContent.Contains($" {kw} ")) )
				{
					string word = Keywords.First(kw => subContent.Contains($" {kw} "));
					index = subContent.LastIndexOf(word);
					return word;
				}

				throw new Exception("Cannot determine Initialization Keyword");
			}
		}

		public static bool TryParse(string content, int start, int length, out KirinVariableDeclaration result)
		{
			result = null;
			var matches = Print.Matches(content);
			if (matches.Count != 1) return false;

			result = new KirinVariableDeclaration(start, length);

			Group group = matches[0].Groups[1];
			string varName = group.Value;
			string iKeyword = InitKeyword.GetKeyword(varName, out int iIndex);
			varName = varName.Substring(0, iIndex - 1);
			result.Name = varName;

			string subContent = group.Value.Substring(iIndex + iKeyword.Length);

			result.Constant = subContent.StartsWith(ConstantKW);
			if( result.Constant ) subContent = subContent.Substring(ConstantKW.Length);

			var varType = FiMHelper.DeclarationType.Determine(subContent, out string tKeyword);
			var varValueRaw = subContent.Substring(tKeyword.Length + 1);
			result.ExpectedType = varType;
			result.RawValue = varValueRaw;

			return true;
		}

		public override object Execute(FiMReport report)
		{
			if (report.Variables.Exists(this.Name))
				throw new Exception("Variable " + this.Name + " already exists");

			var value = new KirinValue(this.RawValue, report);
			if( FiMHelper.IsTypeArray(this.ExpectedType) )
			{
				value.ForceType(this.ExpectedType);
			}

			if (value.Value == null)
			{
				value.ForceType(this.ExpectedType);
			}
			else
			{
				if (value.VarType != this.ExpectedType)
					throw new Exception("Expected " + this.ExpectedType.AsNamedString() +
						", got " + value.VarType.AsNamedString());
			}
			value.Constant = Constant;

			FiMVariable var = new FiMVariable(this.Name, value);
			report.Variables.PushVariable(var);
			return null;
		}
	}
}
