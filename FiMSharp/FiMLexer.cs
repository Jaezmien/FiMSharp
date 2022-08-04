using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.Kirin;
using System.Text.RegularExpressions;

namespace FiMSharp
{
	internal class FiMLexer
	{
		public readonly static char[] Punctuations = new char[] { '.', '!', '?', ':', ',' };
		public readonly static char[] StringLiterals = new char[] { '"', '“', '”' };

		private readonly static Regex _PSComment = new Regex(@"(^P\.(?:P\.)*S\.\s(?:.+)$)");
		private readonly static Regex _ReportStart = new Regex(@"(^Dear .+: .+?[.!,:])( .+)?");
		private readonly static Regex _ReportEnding = new Regex(@"(^Your .+?, .+?[.!?:,])( .+)?");
		private readonly static Regex _SwitchCase = new Regex(@"^On the .+? hoof");
		private readonly static Regex _SwitchDefault = new Regex(@"^If all else fails");
		private readonly static Regex _Foreach = new Regex(@"^For every .+? (?:in .+|from .+ to .+)");
		private readonly static Regex _WhileLoop = new Regex(@"^(?:As long as|While) .+");
		private readonly static Regex[] ElipsesMatches = new[] { _Foreach, _SwitchCase, _SwitchDefault, _WhileLoop };

		public static KirinProgram ParseReport(FiMReport report, string content)
		{
			var program = new KirinProgram(-1, -1);

			var lines = FindAllLines(content);
			var nodes = new List<KirinNode>();

			foreach(var line in lines)
			{
				var node = ParseLine(report, content, line.Start, line.End - line.Start);
				nodes.Add(node);
			}

			if (nodes.FindIndex(n => n.NodeType == "KirinProgramStart") != 0)
				throw new Exception("Start of report must be the first line");
			if (nodes.FindIndex(n => n.NodeType == "KirinProgramEnd") == -1)
				throw new Exception("Cannot find end of report");
			if (nodes.FindIndex(n => n.NodeType == "KirinProgramEnd") < nodes.Count - 1)
			{
				for( int i = nodes.FindIndex(n => n.NodeType == "KirinProgramEnd") + 1; i < nodes.Count; i++ )
				{
					if (nodes[i].NodeType != "KirinPostScript")
						throw new Exception($"Expected EOR at line {FiMHelper.GetIndexPair(content, (nodes[i] as KirinNode).Start).Line}");
				}
			}

			var startIndex = nodes.FindIndex(n => n.NodeType == "KirinProgramStart");
			var endIndex = nodes.FindIndex(n => n.NodeType == "KirinProgramEnd");

			ParseClass(
				report,
				nodes.GetRange(startIndex + 1, endIndex - (startIndex + 1)).ToArray(),
				content
			);

			var startNode = nodes[startIndex] as KirinProgramStart;
			var endNode = nodes[endIndex] as KirinProgramEnd;

			report.Info = new FiMReportInfo(startNode.Start, startNode.Length)
			{
				Recipient = startNode.ProgramRecipient,
				Name = startNode.ProgramName
			};
			report.Author = new FiMReportAuthor(endNode.Start, endNode.Length)
			{
				Role = endNode.AuthorRole,
				Name = endNode.AuthorName
			};

			return program;
		}

		internal static void ParseClass(FiMClass c, KirinNode[] nodes, string content)
		{
			for(int i = 0; i < nodes.Length; i++)
			{
				var node = nodes[i];
				switch(node.NodeType)
				{
					case "KirinFunctionStart":
						{
							i++;
							var startNode = node as KirinFunctionStart;

							List<KirinNode> funcNodes = new List<KirinNode>();
							while (nodes[i].NodeType != "KirinFunctionEnd")
							{
								funcNodes.Add(nodes[i]);
								i++;
							}
							// for(int j = i + 1; )
							var statement = ParseStatement(funcNodes.ToArray(), content);

							var endNode = nodes[i] as KirinFunctionEnd;

							var func = ParseFunction(startNode, endNode, statement);

							if (func.Today && c.GetType() == typeof(FiMReport))
							{
								var n = (FiMReport)c;
								if (n._MainParagraph != string.Empty) throw new Exception("Multiple main methods found");
								n._MainParagraph = n.Name;
							}

							c.AddParagraph(new FiMParagraph(c, func));
						}
						break;

					case "KirinVariableDeclaration":
						{
							((KirinVariableDeclaration)node).Execute(c, true);
						}
						break;

					// "KirinClassDeclarationStart":

					case "KirinPostScript":
						{
							// Ignore
						}
						break;
					default:
						{
							var line = FiMHelper.GetIndexPair(content, node.Start).Line;
							throw new Exception($"Illegal class body node at line {line}");
						}
				}
			}
		}

		internal static KirinFunction ParseFunction(
			KirinFunctionStart startNode,
			KirinFunctionEnd endNode,
			KirinStatement statement
		)
		{
			if (endNode.NodeType == "KirinFunctionEnd" && endNode.Name != startNode.Name)
				throw new Exception($"Method '{startNode.Name}' does not end with the same name");

			var firstNode = statement.Body.First() as KirinNode;
			var lastNode = statement.Body.Last() as KirinNode;
			statement.Start = firstNode.Start;
			statement.Length = (lastNode.Start + lastNode.Length) - firstNode.Start;

			return new KirinFunction(startNode, endNode)
			{
				Statement = statement
			};
		}
		internal static KirinStatement ParseStatement(KirinNode[] nodes, string content)
		{
			var statement = new KirinStatement(-1, -1);

			for(int i = 0; i < nodes.Length; i++)
			{
				var node = nodes[i];

				switch (node.NodeType)
				{
					case "KirinProgramStart":
					case "KirinProgramEnd":
					case "KirinFunctionStart":
					case "KirinFunctionEnd":
					case "KirinElseIfStatement":
					case "KirinIfStatementEnd":
					case "KirinSwitchCase":
					case "KirinSwitchCaseDefault":
					case "KirinLoopEnd":
						{
							var line = FiMHelper.GetIndexPair(content, node.Start).Line;
							throw new Exception($"Illegal statement node at line {line}");
						}

					case "KirinIfStatementStart":
						{
							var startingNode = node as KirinIfStatementStart;
							var statementNodes = KirinLoop.GetStatementNodes(
								startingNode,
								nodes,
								i,
								content,
								out i
							);
							var endingNode = nodes[i] as KirinIfStatementEnd;

							statement.PushNode(
								KirinIfStatement.ParseNodes(startingNode, statementNodes, endingNode, content)
							);
						}
						break;

					case "KirinForInLoop":
						{
							var startingNode = node as KirinForInLoop;
							var statementNodes = KirinLoop.GetStatementNodes(
								startingNode,
								nodes,
								i,
								content,
								out i
							);
							var endingNode = nodes[i] as KirinLoopEnd;

							statement.PushNode(
								KirinForInLoop.ParseNodes(startingNode, statementNodes, endingNode, content)
							);
						}
						break;
					case "KirinForToLoop":
						{
							var startingNode = node as KirinForToLoop;
							var statementNodes = KirinLoop.GetStatementNodes(
								startingNode,
								nodes,
								i,
								content,
								out i
							);
							var endingNode = nodes[i] as KirinLoopEnd;

							statement.PushNode(
								KirinForToLoop.ParseNodes(startingNode, statementNodes, endingNode, content)
							);
						}
						break;

					case "KirinWhileLoop":
						{
							var startingNode = node as KirinWhileLoop;
							var statementNodes = KirinLoop.GetStatementNodes(
								startingNode,
								nodes,
								i,
								content,
								out i
							);
							var endingNode = nodes[i] as KirinLoopEnd;

							statement.PushNode(
								KirinWhileLoop.ParseNodes(startingNode, statementNodes, endingNode, content)
							);
						}
						break;

					case "KirinSwitchStart":
						{
							var startingNode = node as KirinSwitchStart;
							var statementNodes = KirinLoop.GetStatementNodes(
								startingNode,
								nodes,
								i,
								content,
								out i
							);
							var endingNode = nodes[i] as KirinLoopEnd;

							statement.PushNode(
								KirinSwitch.ParseNodes(startingNode, statementNodes, endingNode, content)
							);
						}
						break;

					case "KirinPostScript":
						{
							// Ignore
						}
						break;
					default:
						{
							statement.PushNode(node);
						}
						break;
				}
			}

			var firstNode = statement.Body.First() as KirinNode;
			var lastNode = statement.Body.Last() as KirinNode;
			statement.Start = firstNode.Start;
			statement.Length = (lastNode.Start + lastNode.Length) - firstNode.Start;
			return statement;
		}

		private struct ReportLine
		{
			public int Start;
			public int End;
		}
		private static ReportLine[] FindAllLines(string content)
		{
			List<ReportLine> lines = new List<ReportLine>();
			var length = content.Length;

			var matches = new[] { _PSComment, _ReportStart, _ReportEnding };

			int startIndex = 0;
			for(int i = startIndex; i < content.Length; i++)
			{
				char c = content[i];
				if( c == '(' )
				{
					while (content[++i] != ')');
					continue;
				}
				if( c == '"')
				{
					while (content[++i] != '"');
					continue;
				}
				if( c == '\'')
				{
					if (content[i + 1] == '\\' && content[i + 3] == '\'')
					{
						i += 3;
						continue;
					}
					else if (content[i + 2] == '\'')
					{
						i += 2;
						continue;
					}
				}

				if( Punctuations.Any(p => p == c) )
				{
					int endIndex = i + 1;

					// Checks in case of special lines, see above RegExes.
					int eolIndex = i;
					while (eolIndex + 1 < content.Length && content[eolIndex + 1] != '\n') eolIndex++;
					eolIndex++;
					string line = content.Substring(startIndex, eolIndex - startIndex);
					int startOffset = FindLineTrueStart(line);
					line = line.Substring(startOffset);
					startIndex += startOffset;
					if (matches.Any(m => m.IsMatch(line)))
					{
						var match = matches.First(m => m.IsMatch(line)).Match(line);
						if (match.Groups.Count == 3 && match.Groups[2].Success)
						{
							endIndex = startIndex + match.Groups[2].Index;
						}
						else {
							endIndex = eolIndex;
						}
					}
					else if( Regex.IsMatch(line, @"(^.+?\.\.\.)" ) )
					{
						var match = Regex.Match(line, @"(?:^(.+)?\.\.\.)");
						if( match.Success )
						{
							if(ElipsesMatches.Any(m => m.IsMatch(match.Groups[1].Value)))
							{
								endIndex = startIndex + match.Length;
							}
						}
					}

					lines.Add(new ReportLine()
					{
						Start = startIndex,
						End = endIndex
					});

					startIndex = endIndex;
					i = endIndex;
				}
			}
			
			if(startIndex < content.Length)
				lines.Add(new ReportLine() { Start = lines.Last().End + 1, End = content.Length });

			for(int i = lines.Count - 1; i >= 0; i--)
			{
				var line = lines[i];
				string l = content.Substring(line.Start, line.End - line.Start);
				if (FindLineTrueStart(l) >= l.Length) lines.RemoveAt(i);
			}

			return lines.ToArray();
		}

		private static KirinNode ParseLine(FiMReport report, string content, int start, int length)
		{
			string subContent = content.Substring(start, length);

			// Remove punctuation
			if (ElipsesMatches.Any(m => m.IsMatch(subContent)) && subContent.Substring(subContent.Length - 3) == "...")
				subContent = subContent.Substring(0, subContent.Length - 3);
			else
				subContent = subContent.Substring(0, subContent.Length - 1);

			if (KirinPostScript.TryParse(subContent, start, length, out KirinNode node)) return node;

			if (KirinProgramStart.TryParse(subContent, start, length, out node)) return node;
			if (KirinProgramEnd.TryParse(subContent, start, length, out node)) return node;

#if DEBUG
			if (KirinClassStart.TryParse(subContent, start, length, out node)) return node;
			if (KirinClassEnd.TryParse(subContent, start, length, out node)) return node;
			if (KirinClassConstructorStart.TryParse(subContent, start, length, out node)) return node;
			if (KirinClassConstructorEnd.TryParse(subContent, start, length, out node)) return node;
#endif

			if (KirinFunctionStart.TryParse(subContent, start, length, out node)) return node;
			if (KirinFunctionEnd.TryParse(subContent, start, length, out node)) return node; ;

			if (KirinForInLoop.TryParse(subContent, start, length, out node)) return node;
			if (KirinForToLoop.TryParse(subContent, start, length, out node)) return node;
			if (KirinWhileLoop.TryParse(subContent, start, length, out node)) return node;
			if (KirinSwitchStart.TryParse(subContent, start, length, out node)) return node;
			if (KirinSwitchCase.TryParse(subContent, start, length, out node)) return node;
			if (KirinSwitchCaseDefault.TryParse(subContent, start, length, out node)) return node;
			if (KirinLoopEnd.TryParse(subContent, start, length, out node)) return node;

			if (KirinIfStatementStart.TryParse(subContent, start, length, out node)) return node;
			if (KirinElseIfStatement.TryParse(subContent, start, length, out node)) return node;
			if (KirinIfStatementEnd.TryParse(subContent, start, length, out node)) return node;

			if (KirinPrint.TryParse(subContent, start, length, out node)) return node;
			if (KirinInput.TryParse(subContent, start, length, out node)) return node;
			if (KirinFunctionCall.TryParse(subContent, report, start, length, out node)) return node;
			if (KirinVariableDeclaration.TryParse(subContent, start, length, out node)) return node;
			if (KirinListModify.TryParse(subContent, start, length, out node)) return node;
			if (KirinVariableModify.TryParse(subContent, start, length, out node)) return node;
			if (KirinUnary.TryParse(subContent, start, length, out node)) return node;
			if (KirinReturn.TryParse(subContent, start, length, out node)) return node;
			if (KirinDebugger.TryParse(subContent, start, length, out node)) return node;

#if DEBUG
			if ( System.Diagnostics.Debugger.IsAttached )
			{
				System.Diagnostics.Debugger.Break();
			}
			else
			{
				Console.WriteLine("Unhandled node: " + subContent);
			}
#endif

			return new KirinNode(start, length);
		}

		private static int FindLineTrueStart(string subContent)
		{
			int i = 0;
			while(i < subContent.Length)
			{
				char c = subContent[i];
				if (c == '(')
				{
					while (subContent[i] != ')') i++;
					i++;
				}
				else if (string.IsNullOrWhiteSpace(c.ToString()))
				{
					i++;
				}
				else
				{
					break;
				}
			}
			return i;
		}
	}
}
