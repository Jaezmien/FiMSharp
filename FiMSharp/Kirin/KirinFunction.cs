using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FiMSharp.Kirin
{
	public class KirinBaseFunction : KirinNode
	{
		public KirinBaseFunction(int start, int length) : base(start, length) { }

		public string Name;
		
		public KirinVariableType? Returns;

		public virtual object Execute(FiMReport report, params object[] args) { return null; }
	}
	public class KirinFunction : KirinBaseFunction
	{
		public KirinFunction(KirinFunctionStart startNode, KirinFunctionEnd endNode)
			: base(startNode.Start, (endNode.Start + endNode.Length) - startNode.Start)
		{
			this.Name = startNode.Name;
			this.IsMain = startNode.IsMain;
			this.Arguments = startNode.args;
			this.Returns = startNode.Return;
		}

		public List<KirinFunctionArgument> Arguments;
		public bool IsMain;
		public KirinStatement Statement;

		public override object Execute(FiMReport report, params object[] args)
		{
			int localVariables = 0;
			report.Variables.PushStack();

			if (this.Arguments?.Count() > 0)
			{
				for (int i = 0; i < this.Arguments.Count(); i++)
				{
					if (report.Variables.Exists(this.Arguments[i].Name))
						throw new FiMException("Variable name " + this.Arguments[i].Name + " already exists");

					if (i < args.Length)
					{
						if (FiMHelper.AsVariableType(args[i]) != this.Arguments[i].Type)
						{
							throw new FiMException("Expected " + this.Arguments[i].Type.AsNamedString()
								+ ", got " + FiMHelper.AsVariableType(args[i]).AsNamedString());
						}

						report.Variables.PushVariable(new FiMVariable(this.Arguments[i].Name, new KirinValue(args[i])));
					}
					else
					{
						report.Variables.PushVariable(
							new FiMVariable(
								this.Arguments[i].Name,
								new KirinValue(FiMHelper.GetDefaultValue(this.Arguments[i].Type))
							)
						);
					}
					localVariables++;
				}
			}

			var result = Statement.Execute(report);
			report.Variables.PopVariableRange(localVariables);
			report.Variables.PopStack();

			if (result != null && this.Returns == null)
				throw new FiMException("Non-value returning function returned value");
			if (result != null && this.Returns != null && this.Returns != KirinVariableType.UNKNOWN)
			{
				if (FiMHelper.AsVariableType(result) != this.Returns)
					throw new FiMException("Expected " + ((KirinVariableType)this.Returns).AsNamedString()
						+ ", got " + FiMHelper.AsVariableType(result).AsNamedString());

				return result;
			}

			return null;
		}
	}
	
	public class KirinInternalFunction : KirinBaseFunction
	{
		public KirinInternalFunction(string name, Delegate func) : base(-1, -1)
		{
			this.Name = name;
			this.Arguments = null;
			this.Returns = null;

			var funcArgs = func.Method.GetParameters();
			if (funcArgs.Length > 0)
			{
				this.Arguments = new List<KirinVariableType>();
				foreach(var arg in funcArgs)
					this.Arguments.Add(FiMHelper.AsVariableType(arg.ParameterType, true));
			}

			if( func.Method.ReturnType.Name != "Void" )
				this.Returns = FiMHelper.AsVariableType(func.Method.ReturnType, true);

			this.Function = func;
		}

		public List<KirinVariableType> Arguments;
		public Delegate Function;

		public override object Execute(FiMReport report, params object[] args)
		{
			object[] sanitizedArgs = null;
			if( this.Arguments?.Count > 0 )
			{
				sanitizedArgs = new object[this.Arguments.Count];
				for(int i = 0; i < this.Arguments.Count; i++)
				{
					if (i < args.Length)
					{
						if (FiMHelper.AsVariableType(args[i]) != this.Arguments[i])
							throw new FiMException("Expected " + this.Arguments[i].AsNamedString() + ", got " + FiMHelper.AsVariableType(args[i]).AsNamedString());
						sanitizedArgs[i] = args[i];
					}
					else
					{
						sanitizedArgs[i] = FiMHelper.GetDefaultValue(this.Arguments[i]);
					}
				}
			}

			object result;
			try
			{
				result = this.Function.DynamicInvoke(sanitizedArgs);
			}
			catch (Exception ex)
			{
				throw new Exception("An error has occured while running a custom method\n\n" + ex.ToString());
			}

			if (result != null && this.Returns == null)
				throw new FiMException("Non-value returning function returned value");
			if (result != null && this.Returns != null && this.Returns != KirinVariableType.UNKNOWN)
			{
				if (FiMHelper.AsVariableType(result) != this.Returns)
					throw new FiMException("Expected " + ((KirinVariableType)this.Returns).AsNamedString()
						+ ", got " + FiMHelper.AsVariableType(result).AsNamedString());

				return result;
			}

			return null;
		}
	}

	public struct KirinFunctionArgument
	{
		public KirinVariableType Type;
		public string Name;
	}

	public class KirinFunctionStart : KirinNode
	{
		public KirinFunctionStart(int start, int length) : base(start, length) { }

		public bool IsMain;
		public string Name;
		public List<KirinFunctionArgument> args;
		public KirinVariableType Return;

		private readonly static Regex FunctionStart = new Regex(@"^(?:Today )?I learned (.+)?");
		private readonly static string[] FunctionReturn = { " to get ", " with " };
		private readonly static string FunctionParam = " using ";

		public static bool TryParse(string content, int start, int length, out KirinNode result)
		{
			result = null;
			var match = FunctionStart.Match(content);
			if (!match.Success) return false;

			string functionName = match.Groups[1].Value;
			var node = new KirinFunctionStart(start, length)
			{
				IsMain = content.StartsWith("Today"),
			};

			if ( FunctionReturn.Any(kw => functionName.Contains(kw)) )
			{
				string keyword = FunctionReturn.First(kw => functionName.Contains(kw));
				int keywordIndex = functionName.IndexOf(keyword);
				string subContent = functionName.Substring(keywordIndex + keyword.Length - 1);
				var returnType = FiMHelper.DeclarationType.Determine(subContent, out string sKeyword);

				node.Return = returnType;

				string returnString = keyword.Substring(0, keyword.Length - 1) + sKeyword;
				functionName = functionName.Substring(0, keywordIndex) + functionName.Substring(keywordIndex + returnString.Length);
			}
			if( functionName.Contains(FunctionParam) )
			{
				int keywordIndex = functionName.IndexOf(FunctionParam);
				string subcontent = functionName.Substring(keywordIndex + FunctionParam.Length);
				node.args = new List<KirinFunctionArgument>();
				foreach(string param in subcontent.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries))
				{
					var returnType = FiMHelper.DeclarationType.Determine(" " + param, out string sKeyword);
					string paramName = param.Substring(sKeyword.Length);
					node.args.Add(
						new KirinFunctionArgument()
						{
							Type = returnType,
							Name = paramName
						}
					);
				}

				functionName = functionName.Substring(0, keywordIndex) +
					functionName.Substring(keywordIndex + FunctionParam.Length + subcontent.Length);
			}

			node.Name = functionName;

			result = node;
			return true;
		}
	}

	public class KirinFunctionEnd : KirinNode
	{
		public KirinFunctionEnd(int start, int length) : base(start, length) { }

		public string Name;

		private readonly static Regex FunctionEnd = new Regex(@"^That's all about (.+)?");
		public static bool TryParse(string content, int start, int length, out KirinNode result)
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
