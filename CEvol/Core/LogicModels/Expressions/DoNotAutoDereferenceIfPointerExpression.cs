using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class DoNotAutoDereferenceIfPointerExpression : Expression
	{
		public Expression Expression { get; }

		public DoNotAutoDereferenceIfPointerExpression(Expression expr) : base(expr.ResultTypeSpec)
		{
			Expression = expr;
		}
	}
}
