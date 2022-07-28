using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinFunctionCall : KirinExecutableNode
	{
		public KirinFunctionCall(int start, int length) : base(start, length)
		{
			this.Start = start;
			this.Length = length;
		}

		private readonly static Regex FunctionCall = new Regex(@"^I (?:remembered|would) (.+)");
		public readonly static string FunctionParam = " using ";

		/// <param name="content">Starting after <c>KirinFunctionCall.FunctionParam</c></param>
		public static List<KirinValue> ParseCallArguments(string content, FiMReport report)
		{
			var a = new List<KirinValue>();

			foreach (string param in content.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries))
				a.Add(new KirinValue(param, report));

			return a;
		}

		public static bool TryParse(string content, FiMReport report, int start, int length, out KirinFunctionCall result)
		{
			result = null;
			var matches = FunctionCall.Matches(content);
			if (matches.Count != 1) return false;

			string value = matches[0].Groups[1].Value;
			result = new KirinFunctionCall(start, length)
			{
				FunctionName = value
			};
			if ( value.Contains(FunctionParam) )
			{
				int keywordIndex = value.IndexOf(FunctionParam);
				var args = KirinFunctionCall.ParseCallArguments(value.Substring(keywordIndex + FunctionParam.Length), report);
				result.FunctionName = value.Substring(0, keywordIndex);
				result.RawParameters = value.Substring(keywordIndex + FunctionParam.Length);
				result.Parameters = args;
			}
			else
			{
				result.RawParameters = string.Empty;
				result.Parameters = new List<KirinValue>();
			}

			return true;
		}

		public string FunctionName;
		public List<KirinValue> Parameters;
		public string RawParameters;

		public override object Execute(FiMReport report)
		{
			if (report.Paragraphs.FindIndex(v => v.Name == FunctionName) == -1)
				throw new Exception("Cannot find paragraph " + FunctionName);
			var p = report.Paragraphs.Find(v => v.Name == FunctionName);
			p.Execute(Parameters.ToArray());
			return null;
		}
	}
}
