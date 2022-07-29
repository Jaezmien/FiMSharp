#define HIDE_ERROR

using System;
using System.Collections.Generic;
using System.Text;

namespace FiMSharp.Kirin
{
	public class KirinStatement : KirinNode
	{
		public KirinStatement(int start, int length): base( start, length )
		{
			_Body = new List<KirinBaseNode>();
		}

		protected List<KirinBaseNode> _Body;
		public IReadOnlyCollection<KirinBaseNode> Body
		{
			get
			{
				return _Body.AsReadOnly();
			}
		}
		public void PushNode(KirinBaseNode node)
		{
			_Body.Add(node);
		}
		public KirinBaseNode PopNode()
		{
			var node = _Body[_Body.Count - 1];
			_Body.RemoveAt(_Body.Count - 1);
			return node;
		}

		public virtual object Execute(FiMReport report)
		{
			int localVariables = 0;
			object result = null;

			foreach (var node in this.Body)
			{
				if (!node.GetType().IsSubclassOf(typeof(KirinExecutableNode)))
				{
					if(node.GetType().IsSubclassOf(typeof(KirinNode)) || node.GetType() == typeof(KirinNode))
					{
						var no = (KirinNode)node;
						throw new Exception($"Paragraph contains a non-KirinExecutable node (Line: '{report.GetLine(no.Start, no.Length)}')");
					}
					else
					{
						throw new Exception($"Paragraph contains a non-KirinExecutable node ('{node.NodeType}')");
					}
				}
				var n = (KirinExecutableNode)node;

				if (n.NodeType == "KirinVariableDeclaration") localVariables++;

				object r;
#if HIDE_ERROR
				r = n.Execute(report);
#else
				try
				{
					r = n.Execute(report);
				}
				catch(Exception err)
				{
					throw new Exception(err.Message + " at line " + FiMHelper.GetIndexPair(report.Report, n.Start).Line);
				}
#endif
				if (r != null)
				{
					report.Variables.PopVariableRange(localVariables);
					return r;
				}

					if ( n.NodeType == "KirinReturn" )
				{
					result = ((KirinValue)r).Value;
					break;
				}
			}

			report.Variables.PopVariableRange(localVariables);
			return result;
		}
	}
}
