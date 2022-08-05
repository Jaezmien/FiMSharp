using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace FiMSharp.Kirin
{
	public class KirinFunctionCall : KirinExecutableNode
	{
		public KirinFunctionCall(int start, int length) : base(start, length)
		{
			this.Start = start;
			this.Length = length;
		}

		public readonly static string FunctionParam = " using ";

		/// <param name="content">Starting after <c>KirinFunctionCall.FunctionParam</c></param>
		public static List<KirinValue> ParseCallArguments(string content, FiMClass reportClass)
		{
			var a = new List<KirinValue>();

			if( content != string.Empty )
			{
				foreach (string param in content.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries))
				{
					a.Add(new KirinValue(param, reportClass));
				}
			}

			return a;
		}

		private readonly static string[] Keywords = new[] { "I remembered ", "I would " };
		public static bool TryParse(string content, FiMReport report, int start, int length, out KirinNode result)
		{
			result = null;
			var match = Keywords.FirstOrDefault(k => content.StartsWith(k));
			if (match == null) return false;

			string value = content.Substring(match.Length);
			var node = new KirinFunctionCall(start, length)
			{
				FunctionName = value
			};

			int keywordIndex = value.IndexOf(FunctionParam);
			if ( keywordIndex > -1 )
			{
				node.FunctionName = value.Substring(0, keywordIndex);
				node.RawParameters = value.Substring(keywordIndex + FunctionParam.Length);
			}
			else
			{
				node.RawParameters = string.Empty;
			}

			result = node;
			return true;
		}

		public string FunctionName;
		public string RawParameters;

		public override object Execute(FiMClass reportClass)
		{
			var p = reportClass.GetParagraph(FunctionName);

			if (p == null) throw new FiMException("Cannot find paragraph " + FunctionName);

			var parameters = KirinFunctionCall.ParseCallArguments(this.RawParameters, reportClass);
			p.Execute(parameters.ToArray());
			return null;
		}
	}
}
