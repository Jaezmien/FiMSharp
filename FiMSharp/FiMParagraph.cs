using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using FiMSharp.Kirin;

namespace FiMSharp
{
	public class FiMParagraph
	{
		public FiMParagraph(FiMClass rClass, KirinBaseFunction func)
		{
			this.reportClass = rClass;
			this.Function = func;
			this.Name = func.Name;
		}

		public readonly string Name;
		private readonly FiMClass reportClass;
		public readonly KirinBaseFunction Function;
		public string FunctionType { get { return Function.NodeType; } }
		public KirinVariableType ReturnType
		{
			get { return this.Function.Returns ?? KirinVariableType.UNKNOWN; }
		}

		public object Execute(params object[] args)
		{
			var result = this.Function.Execute(this.reportClass, args);
			return result;
		}
		public object Execute(KirinValue[] args)
		{
			return this.Execute(args?.Select(p => p.Value).ToArray());
		}
	}
}
