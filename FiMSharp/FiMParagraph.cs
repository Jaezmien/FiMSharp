using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using FiMSharp.Kirin;

namespace FiMSharp
{
	public class FiMParagraph
	{
		public FiMParagraph(FiMReport report, KirinBaseFunction func)
		{
			this.report = report;
			this.func = func;
			this.Name = func.Name;
		}

		public readonly string Name;
		private readonly FiMReport report;
		private readonly KirinBaseFunction func;
		public string FunctionType { get { return func.NodeType; } }
		public KirinVariableType ReturnType
		{
			get { return this.func.Returns ?? KirinVariableType.UNKNOWN; }
		}
		public object Execute(params object[] args)
		{
			var result = this.func.Execute(this.report, args);
			return result;
		}
		public object Execute(KirinValue[] args)
		{
			return this.Execute(args?.Select(p => p.Value).ToArray());
		}
	}
}
