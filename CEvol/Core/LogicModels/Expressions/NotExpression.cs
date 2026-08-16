using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class NotExpression : Expression
	{
		public readonly Expression Expression;

		public NotExpression(Expression expression, PositionInSources pos) : base(expression.ResultTypeSpec, pos)
		{
			Expression = expression;
		}
	}
}
