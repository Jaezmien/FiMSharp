using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinInput : KirinExecutableNode
	{
		public KirinInput(int start, int length) : base(start, length) { }

		private readonly static Regex Read = new Regex(@"^I (?:heard|read|asked) (.+)");
		private readonly static Regex Prompt = new Regex(@"^I (?:heard|read|asked) (.+) ""(.+)""$");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (Prompt.IsMatch(content))
			{
				var match = Prompt.Match(content);
				result = new KirinInput(start, length)
				{
					RawVariable = match.Groups[1].Value,
					PromptString = match.Groups[2].Value
				};
			}
			else if (Read.IsMatch(content))
			{
				var match = Read.Match(content);
				result = new KirinInput(start, length)
				{
					RawVariable = match.Groups[1].Value,
					PromptString = string.Empty
				};
			}
			else return false;
			
			return true;
		}
		public string RawVariable;
		public string PromptString;

		public override object Execute(FiMClass reportClass)
		{
			var variable = reportClass.GetVariable(this.RawVariable);
			if( variable == null )
				throw new Exception("Variable " + this.RawVariable + " does not exist");
			if (FiMHelper.IsTypeArray(variable.Type))
				throw new Exception("Cannot input into an array");

			
			string prompt = "";
			if (!string.IsNullOrWhiteSpace(this.PromptString)) prompt = this.PromptString;
			string input = reportClass.Report.Input(prompt, this.RawVariable);

			if (variable.Type == KirinVariableType.STRING)
				input = $"\"{input}\"";
			else if (variable.Type == KirinVariableType.CHAR)
				input = $"'{input}'";

			if (!KirinLiteral.TryParse(input, out object value))
				throw new Exception("Invalid input " + input);
			if (FiMHelper.AsVariableType(value) != variable.Type)
				throw new Exception("Input type doesnt match variable type");

			variable.Value = value;

			return null;
		}
	}
}
