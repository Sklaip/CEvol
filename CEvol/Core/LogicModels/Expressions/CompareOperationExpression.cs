using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class CompareOperationExpression : Expression
	{
		public readonly Expression LeftExpression;
		public readonly Expression RightExpression;
		public readonly CompareOperator CompareOperator;
		public readonly bool IsSigned;

		public CompareOperationExpression(CompareOperator compareOperator, bool isSigned, Expression leftExpression, Expression rightExpression, TypeSpec resultTypeSpec, PositionInSources pos)
			: base(resultTypeSpec, pos)
		{
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
			CompareOperator = compareOperator;
			IsSigned = isSigned;
		}
	}
}
