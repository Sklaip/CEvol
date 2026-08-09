using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class CastExpression : Expression
	{
		public readonly Expression Expression;

		public CastExpression(Expression expression, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			Expression = expression;
		}
	}
}
