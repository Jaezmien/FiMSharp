using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinProgram : KirinStatement
	{
		public KirinProgram(int start, int length) : base(start, length) { }
	}

	public class KirinProgramStart : KirinNode
	{
		public KirinProgramStart(int start, int length) : base(start, length) { }

		public string ProgramName;
		public string ProgramRecipient;

		private readonly static Regex ReportStart = new Regex(@"^Dear (.+)?: (.+)?");
		public static bool TryParse(string content, int start, int length, out KirinProgramStart result)
		{
			result = null;
			var match = ReportStart.Match(content);
			if (!match.Success) return false;

			var groups = match.Groups;
			result = new KirinProgramStart(start, length)
			{
				ProgramRecipient = groups[1].Value,
				ProgramName = groups[2].Value
			};

			return true;
		}
	}

	public class KirinProgramEnd : KirinNode
	{
		public KirinProgramEnd(int start, int length) : base(start, length) { }

		public string AuthorName;
		public string AuthorRole;

		private readonly static Regex ReportEnd = new Regex(@"^Your (.+)?, (.+)?");
		public static bool TryParse(string content, int start, int length, out KirinProgramEnd result)
		{
			result = null;
			var match = ReportEnd.Match(content);
			if (!match.Success) return false;

			var groups = match.Groups;
			result = new KirinProgramEnd(start, length)
			{
				AuthorRole = groups[1].Value,
				AuthorName = groups[2].Value
			};
			return true;
		}
	}
}
