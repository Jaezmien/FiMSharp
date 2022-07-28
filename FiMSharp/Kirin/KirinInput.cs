using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	class KirinInput : KirinExecutableNode
	{
		public KirinInput(int start, int length) : base(start, length) { }

		private readonly static Regex Read = new Regex(@"^I (?:heard|read|asked) (.+)");
		private readonly static Regex Prompt = new Regex(@"^I (?:heard|read|asked) (.+) ""(.+)""$");
		public static bool TryParse(string content, int start, int length, out KirinInput result)
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

		public override object Execute(FiMReport report)
		{
			if (!report.Variables.Exists(this.RawVariable))
				throw new Exception("Variable " + this.RawVariable + " does not exist");
			var variable = report.Variables.Get(this.RawVariable);
			if (FiMHelper.IsTypeArray(variable.Type))
				throw new Exception("Cannot input into an array");

			if (!string.IsNullOrWhiteSpace(this.PromptString))
				report.ConsoleOutput.WriteLine(this.PromptString);

			string input = report.ConsoleInput.ReadLine();
			object value;
			
			if(variable.Type == KirinVariableType.STRING)
				input = $"\"{input}\"";
			else if(variable.Type == KirinVariableType.CHAR)
				input = $"'{input}'";

			if (!KirinLiteral.TryParse(input, out value))
				throw new Exception("Invalid input " + input);
			if (FiMHelper.AsVariableType(value) != variable.Type)
				throw new Exception("Input type doesnt match variable type");

			variable.Value = value;

			return null;
		}
	}
}
