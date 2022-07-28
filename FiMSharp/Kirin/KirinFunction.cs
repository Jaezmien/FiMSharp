using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinBaseFunction : KirinBaseNode
	{
		public string Name;
		public List<KirinFunctionArgument> Arguments;
		public KirinFunctionReturn Returns;

		public KirinStatement Statement;

		public virtual object Execute(FiMReport report, params object[] args) { return null; }
	}
	public class KirinFunction : KirinBaseFunction
	{
		public KirinFunction(KirinFunctionStart details)
		{
			this.Name = details.Name;
			this.IsMain = details.IsMain;
			this.Arguments = details.args;
			this.Returns = details.Return;
		}
		public KirinFunction(int start, int length, KirinFunctionStart details) : this(details)
		{
			this.Start = start;
			this.Length = length;
		}

		public bool IsMain;
		public int Start;
		public int Length;

		public override object Execute(FiMReport report, params object[] args)
		{
			int localVariables = 0;
			report.Variables.PushStack();

			if (this.Arguments?.Count() > 0)
			{
				for (int i = 0; i < this.Arguments.Count(); i++)
				{
					if (i < args.Length)
					{
						if (FiMHelper.AsVariableType(args[i]) != this.Arguments[i].VarType)
						{
							throw new Exception("Expected " + this.Arguments[i].VarType.AsNamedString()
								+ ", got " + FiMHelper.AsVariableType(args[i]).AsNamedString());
						}

						report.Variables.PushVariable(new FiMVariable(this.Arguments[i].Name, new KirinValue(args[i])));
					}
					else
					{
						report.Variables.PushVariable(
							new FiMVariable(
								this.Arguments[i].Name,
								new KirinValue(FiMHelper.GetDefaultValue(this.Arguments[i].VarType))
							)
						);
					}
					localVariables++;
				}
			}

			var result = Statement.Execute(report, args);
			report.Variables.PopVariableRange(localVariables);
			report.Variables.PopStack();

			if (result != null && this.Returns == null)
				throw new Exception("Non-value returning function returned value");
			if (result != null && this.Returns != null && this.Returns.VarType != KirinVariableType.UNKNOWN)
			{
				if (FiMHelper.AsVariableType(result) != this.Returns.VarType)
				{
					throw new Exception("Expected " + this.Returns.VarType.AsNamedString()
						+ ", got " + FiMHelper.AsVariableType(result).AsNamedString());
				}

				return result;
			}

			return null;
		}
	}
	
	public class KirinBaseInternalFunction : KirinBaseFunction
	{
		public KirinBaseInternalFunction()
		{
			this.Arguments = new List<KirinFunctionArgument>();
		}
		protected void ValidateArgs(params object[] args)
		{
			if( this.Arguments.Count > 0 )
			{
				for(int i = 0; i < this.Arguments.Count; i++)
				{
					if (i >= args.Length) break;
					if (FiMHelper.AsVariableType(args[i]) != this.Arguments[i].VarType)
						throw new Exception("Expected " + this.Arguments[i].VarType.AsNamedString() + ", got " + FiMHelper.AsVariableType(args[i]).AsNamedString());
				}
			}
		}
		protected object[] SanitizeArgs(params object[] args)
		{
			var p = new object[this.Arguments.Count];
			for( int i = 0; i < this.Arguments.Count; i++)
			{
				if( i < args.Length )
					p[i] = args[i];
				else
					p[i] = FiMHelper.GetDefaultValue(this.Arguments[i].VarType);
			}
			return p;
		}
	}
	public class KirinInternalStaleFunction : KirinBaseInternalFunction
	{
		public KirinInternalStaleFunction(string name, BaseFunctionDelegate func, List<KirinVariableType> args) : base()
		{
			this.Name = name;
			this.Function = func;

			this.Arguments = args.Select(a => new KirinFunctionArgument() { VarType = a }).ToList();
		}

		public delegate void BaseFunctionDelegate(params object[] p);
		public BaseFunctionDelegate Function;

		public override object Execute(FiMReport report, params object[] args)
		{
			this.ValidateArgs(args);
			var a = this.SanitizeArgs(args);
			this.Function.Invoke(a);
			return null;
		}
	}
	public class KirinInternalReturningFunction : KirinBaseInternalFunction
	{
		public KirinInternalReturningFunction(string name, ReturnFunctionDelegate func, List<KirinVariableType> args) : base()
		{
			this.Name = name;
			this.Function = func;

			this.Arguments = args.Select(a => new KirinFunctionArgument() { VarType = a }).ToList();
		}

		public delegate object ReturnFunctionDelegate(params object[] p);
		public ReturnFunctionDelegate Function;

		public override object Execute(FiMReport report, params object[] args)
		{
			this.ValidateArgs(args);
			var p = this.SanitizeArgs(args);
			var value = this.Function.Invoke(args);
			if (FiMHelper.AsVariableType(value) != this.Returns.VarType)
				throw new Exception("Expected " + this.Returns.VarType.AsNamedString() + ", got " + FiMHelper.AsVariableType(value).AsNamedString());
			return value;
		}
	}

	public class KirinFunctionArgument
	{
		public KirinVariableType VarType;
		public string Name;
	}
	public class KirinFunctionReturn
	{
		public KirinVariableType VarType = KirinVariableType.UNKNOWN;
	}

	public class KirinFunctionStart : KirinNode
	{
		public KirinFunctionStart(int start, int length) : base(start, length) { }

		public bool IsMain;
		public string Name;
		public List<KirinFunctionArgument> args;
		public KirinFunctionReturn Return;

		private readonly static Regex FunctionStart = new Regex(@"^(?:Today )?I learned (.+)?");
		private readonly static string[] FunctionReturn = { " to get ", " with " };
		private readonly static string FunctionParam = " using ";

		public struct KirinFunctionStartParseResult
		{
			public bool IsMain;
			public string Name;
			public List<KirinFunctionArgument> Arguments;
			public KirinFunctionReturn Return;
		}
		public static bool TryParse(string content, int start, int length, out KirinFunctionStart result)
		{
			result = null;
			var match = FunctionStart.Match(content);
			if (!match.Success) return false;

			string functionName = match.Groups[1].Value;
			result = new KirinFunctionStart(start, length)
			{
				IsMain = content.StartsWith("Today"),
			};

			if ( FunctionReturn.Any(kw => functionName.Contains(kw)) )
			{
				string keyword = FunctionReturn.First(kw => functionName.Contains(kw));
				int keywordIndex = functionName.IndexOf(keyword);
				string subContent = functionName.Substring(keywordIndex + keyword.Length - 1);
				var returnType = FiMHelper.DeclarationType.Determine(subContent, out string sKeyword);

				result.Return = new KirinFunctionReturn() { VarType = returnType };

				string returnString = keyword.Substring(0, keyword.Length - 1) + sKeyword;
				functionName = functionName.Substring(0, keywordIndex) + functionName.Substring(keywordIndex + returnString.Length);
			}
			if( functionName.Contains(FunctionParam) )
			{
				int keywordIndex = functionName.IndexOf(FunctionParam);
				string subcontent = functionName.Substring(keywordIndex + FunctionParam.Length);
				result.args = new List<KirinFunctionArgument>();
				foreach(string param in subcontent.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries))
				{
					var returnType = FiMHelper.DeclarationType.Determine(" " + param, out string sKeyword);
					string paramName = param.Substring(sKeyword.Length);
					result.args.Add(
						new KirinFunctionArgument()
						{
							VarType = returnType,
							Name = paramName
						}
					);
				}

				functionName = functionName.Substring(0, keywordIndex) +
					functionName.Substring(keywordIndex + FunctionParam.Length + subcontent.Length);
			}

			result.Name = functionName;

			return true;
		}
	}

	public class KirinFunctionEnd : KirinNode
	{
		public KirinFunctionEnd(int start, int length) : base(start, length) { }

		public string Name;

		private readonly static Regex FunctionEnd = new Regex(@"^That's all about (.+)?");
		public static bool TryParse(string content, int start, int length, out KirinFunctionEnd result)
		{
			result = null;
			var match = FunctionEnd.Match(content);
			if (!match.Success) return false;

			result = new KirinFunctionEnd(start, length)
			{
				Name = match.Groups[1].Value
			};
			return true;
		}
	}
}
