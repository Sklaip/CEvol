using CEvol.Analysis.Members.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class VariableCreatingExpression : Expression
	{
		public readonly string Name;
		public VariableCreatingExpression(string name, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			Name = name;
		}
	}
}

