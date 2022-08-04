using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
#if DEBUG
	class KirinClass : KirinNode
	{
		public KirinClass(int start, int length) : base(start, length) { }
	}

	public class KirinClassStart : KirinNode
	{
		public KirinClassStart(int start, int length) : base(start, length) { }

		public string Name;

		private readonly static Regex ClassStart = new Regex(@"^Have you heard about (.+)");

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = ClassStart.Match(content);
			if (!match.Success) return false;

			result = new KirinClassStart(start, length)
			{
				Name = match.Groups[1].Value
			};
			return false;
		}
	}
	public class KirinClassEnd : KirinNode
	{
		public KirinClassEnd(int start, int length) : base(start, length) { }

		public string ClassName;
		public string Name;

		private readonly static Regex ClassEnd = new Regex(@"^That's what (.+?) know about (.+)");

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = ClassEnd.Match(content);
			if (!match.Success) return false;

			result = new KirinClassEnd(start, length)
			{
				ClassName = match.Groups[1].Value,
				Name = match.Groups[2].Value
			};
			return true;
		}
	}

	public class KirinClassConstructorStart : KirinNode
	{
		public KirinClassConstructorStart(int start, int length) : base(start, length) { }

		public string Name;

		private readonly static Regex ClassEnd = new Regex(@"^When (.+) gets called");

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = ClassEnd.Match(content);
			if (!match.Success) return false;

			result = new KirinClassConstructorStart(start, length)
			{
				Name = match.Groups[1].Value
			};
			return true;
		}
	}

	public class KirinClassConstructorEnd : KirinNode
	{
		public KirinClassConstructorEnd(int start, int length) : base(start, length) { }

		public string Name;

		private readonly static Regex ClassEnd = new Regex(@"^That's what (.+) would do.");

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = ClassEnd.Match(content);
			if (!match.Success) return false;

			result = new KirinClassConstructorStart(start, length)
			{
				Name = match.Groups[1].Value
			};
			return true;
		}
	}
#endif
}
