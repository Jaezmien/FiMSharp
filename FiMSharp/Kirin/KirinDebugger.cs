namespace FiMSharp.Kirin
{
	public class KirinDebugger : KirinExecutableNode
	{
		public KirinDebugger(int start, int length) : base(start, length) { }

		public static bool TryParse(string content, int start, int length, out KirinDebugger result)
		{
			result = null;
			if (content != "I took a break") return false;

			result = new KirinDebugger(start, length);
			return true;
		}

		public override object Execute(FiMReport report)
		{
#if DEBUG
			if (System.Diagnostics.Debugger.IsAttached)
			{
				System.Diagnostics.Debugger.Break();
			}
#endif

			return null;
		}
	}
}
