using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class ReinterpretCastExpression : Expression
	{
		public readonly Expression CastedExpression;

		public ReinterpretCastExpression(Expression castedExpression, TypeSpec resultTypeSpec, PositionInSources pos) : base(resultTypeSpec, pos)
		{
			CastedExpression = castedExpression;
		}
	}
}
