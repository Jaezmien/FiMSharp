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

		private readonly static Regex VarDeclaration = new Regex(@"^Did you know that (.+)");
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
					index = subContent.IndexOf(word);
					return word;
				}

				throw new FiMException("Cannot determine Initialization Keyword");
			}
		}

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = VarDeclaration.Match(content);
			if (!match.Success) return false;

			var node = new KirinVariableDeclaration(start, length);

			Group group = match.Groups[1];
			string varName = group.Value;
			string iKeyword = InitKeyword.GetKeyword(varName, out int iIndex);
			varName = varName.Substring(0, iIndex - 1);
			node.Name = varName;

			string subContent = group.Value.Substring(iIndex + iKeyword.Length);

			node.Constant = subContent.StartsWith(ConstantKW);
			if( node.Constant ) subContent = subContent.Substring(ConstantKW.Length);

			var varType = FiMHelper.DeclarationType.Determine(subContent, out string tKeyword);
			string varValueRaw = null;
			if(tKeyword.Length + 1 <= subContent.Length)
			{
				varValueRaw = subContent.Substring(tKeyword.Length + 1);
			}
			node.ExpectedType = varType;
			node.RawValue = varValueRaw;
			
			result = node;
			return true;
		}

		public override object Execute(FiMReport report)
		{
			if (report.Variables.Exists(this.Name))
				throw new FiMException("Variable " + this.Name + " already exists");

			KirinValue value;
			// if( FiMHelper.IsTypeArray(this.ExpectedType) || this.RawValue == null )
			value = new KirinValue(this.RawValue, report, this.ExpectedType);

			if (value.Value == null)
			{
				value.Value = FiMHelper.GetDefaultValue(this.ExpectedType);
			}
			else
			{
				if (value.Type != this.ExpectedType)
					throw new FiMException("Expected " + this.ExpectedType.AsNamedString() +
						", got " + value.Type.AsNamedString());
			}
			value.Constant = Constant;

			FiMVariable var = new FiMVariable(this.Name, value);
			report.Variables.PushVariable(var);
			return null;
		}
	}
}
