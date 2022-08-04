#if DEBUG
#define HIDE_ERROR
#endif

using System.Collections.Generic;

namespace FiMSharp.Kirin
{
	public class KirinStatement : KirinNode
	{
		public KirinStatement(int start, int length): base( start, length )
		{
			_Body = new List<KirinNode>();
		}

		protected List<KirinNode> _Body;
		public List<KirinNode> Body
		{
			get
			{
				return _Body;
			}
		}
		public void PushNode(KirinNode node)
		{
			_Body.Add(node);
		}
		public KirinBaseNode PopNode()
		{
			var node = _Body[_Body.Count - 1];
			_Body.RemoveAt(_Body.Count - 1);
			return node;
		}

		public virtual object Execute(FiMClass reportClass)
		{
			uint localVariables = 0;
			object result = null;

			foreach (var node in this.Body)
			{
				if (!node.GetType().IsSubclassOf(typeof(KirinExecutableNode)))
				{
					if(node.GetType().IsSubclassOf(typeof(KirinNode)) || node.GetType() == typeof(KirinNode))
					{
						var no = (KirinNode)node;
						throw new FiMException($"Paragraph contains a non-KirinExecutable node at line ${FiMHelper.GetIndexPair(reportClass.Report.ReportString, node.Start).Line}");
					}
					else
					{
						throw new FiMException($"Paragraph contains a non-KirinExecutable node ('{node.NodeType}')");
					}
				}
				var n = (KirinExecutableNode)node;

				if (n.NodeType == "KirinVariableDeclaration") localVariables++;

				object r;
#if HIDE_ERROR
				r = n.Execute(reportClass);
#else
				try
				{
					r = n.Execute(report);
				}
				catch(FiMException err)
				{
					throw new Exception(err.Message + " at line " + FiMHelper.GetIndexPair(report.Report, n.Start).Line);
				}
#endif
				if (r != null)
				{
					reportClass.Variables.Pop(count: localVariables);
					return r;
				}

					if ( n.NodeType == "KirinReturn" )
				{
					result = ((KirinValue)r).Value;
					break;
				}
			}

			reportClass.Variables.Pop(count: localVariables);
			return result;
		}
	}
}
