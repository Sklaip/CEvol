using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class VariableCreatingExpression : Expression
	{
		public readonly string Name;
		public VariableCreatingExpression(string name, TypeSpec resultTypeSpec, PositionInSources pos) : base(resultTypeSpec, pos)
		{
			Name = name;
		}
	}
}

