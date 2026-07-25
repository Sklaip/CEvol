using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class NotExpression : Expression
	{
		public readonly Expression Expression;

		public NotExpression(Expression expression) : base(expression.ResultTypeSpec)
		{
			Expression = expression;
		}
	}
}
