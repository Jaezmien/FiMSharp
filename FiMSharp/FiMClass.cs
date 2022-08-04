using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FiMSharp.Kirin;

namespace FiMSharp
{
	public class FiMClass
	{
		public FiMClass(string name, bool main = false, FiMClass parent = null, FiMReport report = null)
		{
			this.Variables = new FiMVariableList();
			this.Paragraphs = new Stack<FiMParagraph>();
			this.Classes = new Stack<FiMClass>();

			this.Name = name;

			this.IsMain = main;
			if (this.IsMain)
			{
				this.Report = report;
				this.Parent = parent;
			}
		}

		public FiMClass Parent;
		public FiMReport Report;

		internal FiMVariableList Variables;
		public FiMVariable GetVariable(string name, int depth = 0, bool propagate = true)
		{
			if (this.Variables.Has(name, depth == 0))
				return this.Variables.Get(name, depth == 0);

			if( propagate )
			{
				if (this.IsMain == false && this.Parent != null)
					return this.Parent.GetVariable(name, depth + 1);
			}

			return null;
		}
		public void AddVariable(string name, object value)
		{
			if (this.GetVariable(name, propagate: false) != null) throw new Exception("Variable " + Variables + " already exists");

			this.Variables.Push(new FiMVariable(name, value), true);
		}

		internal Stack<FiMParagraph> Paragraphs;
		public FiMParagraph GetParagraph(string name, int depth = 0, bool propagate = true)
		{
			var paragraph = this.Paragraphs.FirstOrDefault(p => p.Name == name);
			if ( paragraph != null ) return paragraph;

			if( propagate )
			{
				if (this.IsMain == false && this.Parent != null)
					return this.Parent.GetParagraph(name, depth + 1);
			}

			return null;
		}
		public FiMParagraph GetParagraphLazy(string name, int depth = 0, bool propagate = true)
		{
			var paragraph = this.Paragraphs.FirstOrDefault(p => name.StartsWith(p.Name));
			if (paragraph != null) return paragraph;

			if (propagate)
			{
				if (this.IsMain == false && this.Parent != null)
					return this.Parent.GetParagraphLazy(name, depth + 1);
			}

			return null;
		}
		public void AddParagraph(string name, Delegate func)
		{
			var node = new KirinInternalFunction(name, func);
			if (this.GetParagraph(name) != null) throw new Exception("Paragraph " + node.Name + " already exists");

			this.Paragraphs.Push(new FiMParagraph(this, node));
		}
		internal void AddParagraph(FiMParagraph paragraph) => this.Paragraphs.Push(paragraph);

		public Stack<FiMClass> Classes;
		public FiMClass GetClass(string name, int depth = 0, bool propagate = true)
		{
			var fClass = this.Classes.FirstOrDefault(p => p.Name == name);
			if (fClass != null) return fClass;

			if (propagate)
			{
				if (this.IsMain == false && this.Parent != null)
					return this.Parent.GetClass(name, depth + 1);
			}

			return null;
		}
		public FiMClass GetClassLazy(string name, int depth = 0, bool propagate = true)
		{
			var fClass = this.Classes.FirstOrDefault(p => name.StartsWith(p.Name));
			if (fClass != null) return fClass;

			if (propagate)
			{
				if (this.IsMain == false && this.Parent != null)
					return this.Parent.GetClassLazy(name, depth + 1);
			}

			return null;
		}
		public void AddClass<T>(string name, T value)
		{
			throw new NotImplementedException();
		}

		public string Name;
		public bool IsMain;
	}
}
