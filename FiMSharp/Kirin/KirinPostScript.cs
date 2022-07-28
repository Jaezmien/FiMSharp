using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinPostScript : KirinNode
	{
		public KirinPostScript(int start, int length) : base(start, length) { }

		private readonly static string PSComment = @"^P\.(?:P\.)*S\.\s(?:.+)$";

		public static bool TryParse(string content, int start, int length, out KirinPostScript result)
		{
			result = null;
			if (!Regex.IsMatch(content, PSComment)) return false;
			result = new KirinPostScript(start, length);
			return true;
		}
	}
}
