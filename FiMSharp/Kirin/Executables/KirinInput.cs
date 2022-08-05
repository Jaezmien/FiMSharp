using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinInput : KirinExecutableNode
	{
		public KirinInput(int start, int length) : base(start, length) { }

		private readonly static string[] PreKeywords = new[] { "I heard ", "I read ", "I asked " };
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = PreKeywords.FirstOrDefault(k => content.StartsWith(k));
			if (match == null) return false;
			content = content.Substring(match.Length);

			var prompt = Regex.Match(content, @"(.+) ""(.+)""");
			if (prompt.Success)
			{
				result = new KirinInput(start, length)
				{
					RawVariable = prompt.Groups[1].Value,
					PromptString = prompt.Groups[2].Value
				};

				return true;
			}

			result = new KirinInput(start, length)
			{
				RawVariable = content,
				PromptString = string.Empty
			};

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
