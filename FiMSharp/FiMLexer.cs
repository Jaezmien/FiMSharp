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

		private readonly static Regex PSComment = new Regex(@"^P\.(?:P\.)*S\.\s(?:.+)$");
		private readonly static Regex ReportStart = new Regex(@"Dear (?:.+)?: (?:.+)?");
		private readonly static Regex ReportEnding = new Regex(@"Your (?:.+)?, (?:.+)?");
		private readonly static Regex SwitchCase = new Regex(@"On the (?:.+)? hoof\.\.\.");
		private readonly static Regex SwitchDefault = new Regex(@"If all else fails\.\.\.");
		private readonly static Regex Foreach = new Regex(@"For every (?:.+)? (?:in (?:.+)|from (?:.+) to (?:.+))?\.\.\.");

		public static KirinProgram ParseReport(FiMReport report, string content)
		{
			var program = new KirinProgram(-1, -1);

			int currentIndex = 0;
			var nodes = new List<KirinBaseNode>();
			while (currentIndex < content.Length)
			{
				int endIndex = FindNextPunctuation(content, currentIndex);
				if (endIndex == -1) break;

				var node = ParseLine(report, content, currentIndex, (endIndex + 1) - currentIndex);
				nodes.Add(node);

				currentIndex = endIndex + 1;
			}

			if (nodes.FindIndex(n => n.NodeType == "KirinProgramStart") != 0) throw new Exception("Start of report must be the first line");
			if (nodes.FindIndex(n => n.NodeType == "KirinProgramEnd") == -1) throw new Exception("Cannot find end of report");
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

		private static int FindNextPunctuation(string content, int currentIndex)
		{
			int firstValidIndex = -1;

			for( int i = currentIndex; i < content.Length; i++ )
			{
				char c = content[i];
				if( !string.IsNullOrWhiteSpace(c.ToString()) && firstValidIndex == -1)
				{
					firstValidIndex = i;
				}

				if( c == '\\' )
				{
					i++;
					continue;
				}

				if( c == '\'' )
				{
					// Find possible start of word
					int wordStartingIndex = i;
					while (!string.IsNullOrWhiteSpace(content[wordStartingIndex - 1].ToString())) wordStartingIndex--;

					// Is this on the start (and part of) a keyword?
					if (FiMConstants.Keywords.Any(kw => content.Substring(wordStartingIndex, kw.Length) == kw)) continue;

					// Find end
					int endingIndex = i + 1;
					while (content[endingIndex] != '\'') endingIndex++;
					
					if( endingIndex - wordStartingIndex <= 3 )
					{
						i = endingIndex;
						continue;
					}
				}
				if( StringLiterals.Any(l => l == c) )
				{
					int endingIndex = i + 1;
					while (!StringLiterals.Any(l => l == content[endingIndex])) endingIndex++;
					i = endingIndex;
					continue;
				}

				if( c == '(' )
				{
					int endingIndex = i + 1;
					while (content[endingIndex] != ')') endingIndex++;
					i = endingIndex;
					continue;
				}

				if (Punctuations.Any(p => p == c))
				{
					int wordStartingIndex = i;
					while (!string.IsNullOrWhiteSpace(content[wordStartingIndex - 1].ToString())) wordStartingIndex--;

					int EOLIndex = i + 1;
					while (EOLIndex < content.Length && content[EOLIndex] != '\n') EOLIndex++;

					string subContent = content.Substring(wordStartingIndex, EOLIndex - wordStartingIndex);
					string subContent2 = content.Substring(firstValidIndex, EOLIndex - firstValidIndex);

					if ( PSComment.IsMatch(subContent) )
					{
						i = EOLIndex - 1;
					}

					if (Foreach.IsMatch(subContent2))
					{
						i = EOLIndex - 1;
					}
					if (SwitchCase.IsMatch(subContent2))
					{
						i = EOLIndex - 1;
					}
					if (SwitchDefault.IsMatch(subContent2))
					{
						i = EOLIndex - 1;
					}
					if ( ReportStart.IsMatch(subContent2) )
					{
						i = EOLIndex - 1;
					}
					if ( ReportEnding.IsMatch(subContent2) )
					{
						i = EOLIndex - 1;
					}

					return i;
				}
			}
			return -1;
		}

		private static KirinNode ParseLine(FiMReport report, string content, int start, int length)
		{

			string subContent = content.Substring(start, length);
			int trueStart = FindLineTrueStart(subContent); // Move to first actual valid character
			start += trueStart;
			length -= trueStart;
			subContent = subContent.Substring(trueStart);
			while(string.IsNullOrWhiteSpace(subContent[0].ToString()))
			{
				subContent = subContent.Substring(1);
				start += 1;
				length -= 1;
			}
			while (string.IsNullOrWhiteSpace(subContent.Last().ToString()))
			{
				subContent = subContent.Substring(0, subContent.Length - 1);
				length -= 1;
			}
			subContent = subContent.Substring(0, subContent.Length - 1); // Remove punctuation

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
			while(true)
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
