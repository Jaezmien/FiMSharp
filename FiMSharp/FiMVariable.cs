using System;
using System.Collections.Generic;
using System.Linq;
using FiMSharp.Kirin;

namespace FiMSharp
{
	public class FiMVariable
	{
		public FiMVariable(string name, KirinValue value)
		{
			if (!KirinValue.ValidateName(name)) throw new Exception("Invalid variable name " + name);
			this.Name = name;
			this.KValue = value;
		}

		public FiMVariable(string name, object value, bool constant = false) :
			this(name, new KirinValue(value) { Constant = constant } ) { }

		public readonly string Name;
		private readonly KirinValue KValue;

		public KirinVariableType Type
		{
			get { return this.KValue.Type; }
		}
		public object Value
		{
			get { return this.KValue.Value; }
			set { this.KValue.Value = value; }
		}
		public bool Constant
		{
			get { return KValue.Constant; }
		}
	}

	public class FiMVariableList
	{
		public FiMVariableList()
		{
			this.GlobalVariables = new Stack<FiMVariable>();
			this.LocalVariables = new Stack<Stack<FiMVariable>>();
		}

		public readonly Stack<FiMVariable> GlobalVariables;
		public readonly Stack<Stack<FiMVariable>> LocalVariables;
		public int StackDepth { get { return LocalVariables.Count; } }

		public void Push(FiMVariable value, bool global = false)
		{
			if (global) this.GlobalVariables.Push(value);
			else this.LocalVariables.Peek().Push(value);
		}
		public FiMVariable Pop(bool global = false)
		{
			if (global) return this.GlobalVariables.Pop();
			else return this.LocalVariables.Peek().Pop();
		}
		public FiMVariable[] Pop(bool global = false, uint count = 1)
		{
			Stack<FiMVariable> variables = new Stack<FiMVariable>();

			while(count > 0 && this.LocalVariables.Peek().Count > 0)
			{
				variables.Push(this.LocalVariables.Peek().Pop());
				count--;
			}

			return variables.ToArray();
		}

		public void PushFunctionStack() => this.LocalVariables.Push(new Stack<FiMVariable>());
		public void PopFunctionStack() => this.LocalVariables.Pop();

		public bool Has(string name, bool local = true) => this.Get(name, local) != null;
		public FiMVariable Get(string name, bool local = true)
		{
			var variable = this.GlobalVariables.FirstOrDefault(v => v.Name == name);
			if (variable != null) return variable;

			if (local)
			{
				variable = this.LocalVariables.Peek().FirstOrDefault(v => v.Name == name);
				if (variable != null) return variable;
			}

			return null;
		}
	}
}
