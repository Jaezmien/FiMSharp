﻿using System.Text.RegularExpressions;

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

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith("Dear ")) return false;

			var match = Regex.Match(content, @"Dear (.+): (.+)");
			if (!match.Success) return false;

			result = new KirinProgramStart(start, length)
			{
				ProgramRecipient = match.Groups[1].Value,
				ProgramName = match.Groups[2].Value
			};

			return true;
		}
	}

	public class KirinProgramEnd : KirinNode
	{
		public KirinProgramEnd(int start, int length) : base(start, length) { }

		public string AuthorName;
		public string AuthorRole;

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith("Your ")) return false;

			var match = Regex.Match(content, @"^Your (.+), (.+)");
			if (!match.Success) return false;

			result = new KirinProgramEnd(start, length)
			{
				AuthorRole = match.Groups[1].Value,
				AuthorName = match.Groups[2].Value
			};
			return true;
		}
	}
}
