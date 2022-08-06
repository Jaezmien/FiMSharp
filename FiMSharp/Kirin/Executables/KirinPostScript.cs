using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinPostScript : KirinNode
	{
		public KirinPostScript(int start, int length) : base(start, length) { }

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith("P.")) return false;
			if (!Regex.IsMatch(content, @"^(?:P\.)+S\.\s")) return false;
			result = new KirinPostScript(start, length);
			return true;
		}
	}
}
