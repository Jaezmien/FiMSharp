﻿using System;
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

		private readonly static string PreKeyword = "Did you know that ";
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
					index = subContent.IndexOf($" {word} ");
					return $" {word} ";
				}

				throw new FiMException("Cannot determine Initialization Keyword");
			}
		}

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith(PreKeyword)) return false;

			var node = new KirinVariableDeclaration(start, length);
			string varName = content.Substring(PreKeyword.Length);
			string iKeyword = InitKeyword.GetKeyword(varName, out int iIndex);
			string subContent = varName.Substring(iIndex + iKeyword.Length);
			varName = varName.Substring(0, iIndex);
			node.Name = varName;

			node.Constant = subContent.StartsWith(ConstantKW);
			if( node.Constant ) subContent = subContent.Substring(ConstantKW.Length);

			var varType = FiMHelper.DeclarationType.Determine(" " + subContent, out string tKeyword);
			string varValueRaw = null;
			if(tKeyword.Length <= subContent.Length)
			{
				varValueRaw = subContent.Substring(tKeyword.Length);
			}
			node.ExpectedType = varType;
			node.RawValue = varValueRaw;
			
			result = node;
			return true;
		}

		public object Execute(FiMClass reportClass, bool global = false)
		{
			if (reportClass.GetVariable(this.Name) != null)
				throw new FiMException("Variable " + this.Name + " already exists");

			KirinValue value = new KirinValue(this.RawValue, reportClass, this.ExpectedType);

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
			reportClass.Variables.Push(var, global);
			return null;
		}
		public override object Execute(FiMClass reportClass)
		{
			return Execute(reportClass, false);
		}
	}
}
