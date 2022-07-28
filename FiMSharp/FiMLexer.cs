using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.Kirin;
using System.Text.RegularExpressions;

namespace FiMSharp
{
	class FiMLexer
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
			var nodes = new List<KirinBaseNode>();

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
						throw new Exception("Expected EOR at line " + FiMHelper.GetIndexPair(content, (nodes[i] as KirinNode).Start).Line);
				}
			}

			for( int i = 0; i < nodes.Count; i++ )
			{
				var node = nodes[i];
				switch (node.NodeType)
				{
					case "KirinProgramStart":
						{
							var n = node as KirinProgramStart;
							program.Start = n.Start;

							report.Info = new FiMReportInfo(n.Start, n.Length)
							{
								Recipient = n.ProgramRecipient,
								Name = n.ProgramName
							};

							program.PushNode(node);
						}
						break;
					case "KirinProgramEnd":
						{
							var n = node as KirinProgramEnd;
							program.Length = (n.Start + n.Length) - program.Start;

							report.Author = new FiMReportAuthor(n.Start, n.Length)
							{
								Role = n.AuthorRole,
								Name = n.AuthorName
							};

							program.PushNode(node);
						}
						break;
					case "KirinFunctionStart":
						{
							i++;
							var n = node as KirinFunctionStart;
							var fn = new KirinFunction(n);

							var s = new KirinStatement(-1, -1);
							while (nodes[i].NodeType != "KirinFunctionEnd")
							{
								var sn = nodes[i++];
								s.PushNode(sn);
							}

							var en = nodes[i] as KirinFunctionEnd;
							if (en.NodeType == "KirinFunctionEnd" && en.Name != n.Name)
								throw new Exception($"Method '{n.Name}' does not end with the same name");


							s.Start = n.Start;
							s.Length = (en.Start + en.Length) - n.Start;
							fn.Statement = s;
							fn.Start = s.Start;
							fn.Length = s.Length;

							program.PushNode(fn);
						}
						break;

					case "KirinVariableDeclaration":
						{
							program.PushNode(node);
						}
						break;

					case "KirinPostScript": { } break;
					default:
						{
							program.PushNode(node);
						}
						break;
				}
			}

			return program;
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
						var match = Regex.Match(line, @"(?:^(.+?)\.\.\.)");
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
			if (ElipsesMatches.Any(m => m.IsMatch(subContent)))
				subContent = subContent.Substring(0, subContent.Length - 3);
			else
				subContent = subContent.Substring(0, subContent.Length - 1);

			{
				if (KirinProgramStart.TryParse(subContent, start, length, out KirinProgramStart psResult)) return psResult;
			}
			{
				if (KirinProgramEnd.TryParse(subContent, start, length, out KirinProgramEnd peResult)) return peResult;
			}

			{
				if (KirinFunctionStart.TryParse(subContent, start, length, out KirinFunctionStart fsResult)) return fsResult;
			}
			{
				if (KirinFunctionEnd.TryParse(subContent, start, length, out KirinFunctionEnd feResult)) return feResult;
			}

			{
				if (KirinPostScript.TryParse(subContent, start, length, out KirinPostScript psResult)) return psResult;
			}

			{
				if (KirinPrint.TryParse(subContent, start, length, out KirinPrint pResult)) return pResult;
			}
			{
				if (KirinInput.TryParse(subContent, start, length, out KirinInput kResult)) return kResult;
			}
			{
				if (KirinFunctionCall.TryParse(subContent, report, start, length, out KirinFunctionCall fcResult)) return fcResult;
			}
			{
				if (KirinVariableDeclaration.TryParse(subContent, start, length, out KirinVariableDeclaration vdResult)) return vdResult;
			}
			{
				if (KirinListModify.TryParse(subContent, start, length, out KirinListModify lmResult)) return lmResult;
			}
			{
				if (KirinVariableModify.TryParse(subContent, start, length, out KirinVariableModify vmResult)) return vmResult;
			}
			{
				if (KirinUnary.TryParse(subContent, start, length, out KirinUnary uResult)) return uResult;
			}
			{
				if (KirinReturn.TryParse(subContent, start, length, out KirinReturn rResult)) return rResult;
			}

			{
				if (KirinDebugger.TryParse(subContent, start, length, out KirinDebugger dResult)) return dResult;
			}

#if DEBUG
			if( System.Diagnostics.Debugger.IsAttached )
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
