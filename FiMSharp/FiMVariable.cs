using System;
using System.Collections.Generic;
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

		public FiMVariable(string name, object value, bool constant = false) : this(name, new KirinValue(value) { Constant = constant } ) { }

		public readonly string Name;
		private readonly KirinValue KValue;
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
			this.Variables = new List<List<FiMVariable>>();
			this.PushStack();
		}

		private readonly List<List<FiMVariable>> Variables;
		public List<FiMVariable> GlobalStack
		{
			get
			{
				return Variables[0];
			}
		}
		public List<FiMVariable> CurrentStack
		{
			get
			{
				return Variables[Variables.Count - 1];
			}
		}

		public void PushStack()
		{
			Variables.Add(new List<FiMVariable>());
		}
		public List<FiMVariable> PopStack()
		{
			if (Variables.Count == 1) return null;

			var stack = Variables[Variables.Count - 1];
			Variables.RemoveAt(Variables.Count - 1);
			return stack;
		}
		public int StackCount() => Variables.Count;
		public int CurrentStackCount() => CurrentStack.Count;

		public void PushGlobalVarible(FiMVariable variable)
		{
			GlobalStack.Add(variable);
		}
		public void PushVariable(FiMVariable variable)
		{
			CurrentStack.Add(variable);
		}
		public FiMVariable PopVariable()
		{
			if (CurrentStack.Count == 0) return null;
			var v = CurrentStack[CurrentStack.Count - 1];
			CurrentStack.RemoveAt(CurrentStack.Count - 1);
			return v;
		}
		public List<FiMVariable> PopVariableRange(int count)
		{
			var l = new List<FiMVariable>();
			for( int i = 0; i < count; i++ )
			{
				var v = this.PopVariable();
				if (v == null) break;
				l.Add(v);
			}
			return l;
		}

		public bool Exists(string name)
		{
			return this.Get(name) != null;
		}
		public FiMVariable Get(string name)
		{
			for( int i = 0; i < GlobalStack.Count; i++)
			{
				if (GlobalStack.FindIndex(v => v.Name == name) > -1) return GlobalStack.Find(v => v.Name == name);
			}
			for (int i = 0; i < CurrentStack.Count; i++)
			{
				if (CurrentStack.FindIndex(v => v.Name == name) > -1) return CurrentStack.Find(v => v.Name == name);
			}
			return null;
		}
	}
}
