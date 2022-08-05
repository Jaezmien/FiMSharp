using System;

namespace FiMSharp.Kirin
{
	public class KirinReturn : KirinExecutableNode
	{
		public KirinReturn(int start, int length) : base(start, length) { }

		public string RawParameters;
		public KirinVariableType ExpectedType;

		private readonly static string ReturnStart = "Then you get ";
		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			if (!content.StartsWith(ReturnStart)) return false;

			string subContent = content.Substring(ReturnStart.Length);
			var node = new KirinReturn(start, length)
			{
				RawParameters = subContent
			};

			var expectedType = FiMHelper.DeclarationType.Determine(" " + subContent, out string eKeyword, false);
			if( expectedType != KirinVariableType.UNKNOWN )
			{
				subContent = subContent.Substring(eKeyword.Length);
				node.RawParameters = subContent;
				node.ExpectedType = expectedType;
			}

			result = node;
			return true;
		}
		public override object Execute(FiMClass reportClass)
		{
			var value = new KirinValue(this.RawParameters, reportClass);
			if( this.ExpectedType != KirinVariableType.UNKNOWN )
			{
				if (value.Type != this.ExpectedType)
					throw new FiMException("Expected " + this.ExpectedType.AsNamedString() +
						", got " + value.Type.AsNamedString());
			}
			return value.Value;
		}
	}
}
