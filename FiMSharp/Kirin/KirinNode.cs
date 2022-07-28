namespace FiMSharp.Kirin
{
	public class KirinBaseNode
	{
		public string NodeType
		{
			get { return this.GetType().Name; }
		}
	}
	public class KirinNode : KirinBaseNode
	{
		public KirinNode(int start, int length)
		{
			this.Start = start;
			this.Length = length;
		}

		public int Start;
		public int Length;
	}

	public class KirinExecutableNode : KirinNode
	{
		public KirinExecutableNode(int start, int length): base(start, length) { }

		public virtual object Execute(FiMReport report) => null;
	}
}
