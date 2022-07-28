using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinPostScript : KirinNode
	{
		public KirinPostScript(int start, int length) : base(start, length) { }

		private readonly static Regex PSComment = new Regex(@"^P\.(?:P\.)*S\.\s(?:.+)$");

		public static bool TryParse(string content, int start, int length, out KirinPostScript result)
		{
			result = null;
			var matches = PSComment.Matches(content);
			if (matches.Count != 1) return false;
			result = new KirinPostScript(start, length);
			return true;
		}
	}
}
