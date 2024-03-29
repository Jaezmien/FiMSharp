﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinVariableModify : KirinExecutableNode
	{
		public KirinVariableModify(int start, int length) : base(start, length) { }

		private readonly static string[] ReplaceKW = {
			" becomes ", " become ", " became ", " is now ", " now likes ", " now like ", " are now "
		};
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			string mKeyword = ReplaceKW.FirstOrDefault(kw => content.Contains(kw));
			if (mKeyword == null) return false;

			var node = new KirinVariableModify(start, length);
			int mIndex = content.IndexOf(mKeyword);

			node.LeftOp = content.Substring(0, mIndex);
			node.RightOp = content.Substring(mIndex + mKeyword.Length);
			result = node;
			return true;
		}

		public string LeftOp;
		public string RightOp;

		public override object Execute(FiMClass reportClass)
		{
			var lVariable = reportClass.GetVariable(this.LeftOp);
			if (lVariable == null)
				throw new FiMException("Variable " + this.LeftOp + " does not exist");

			var kValue = new KirinValue(this.RightOp, reportClass);
			lVariable.Value = kValue.Value;
			return null;
		}
	}
}
