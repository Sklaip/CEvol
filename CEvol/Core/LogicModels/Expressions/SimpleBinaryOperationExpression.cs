using CEvol.Analysis.Members.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class SimpleBinaryOperationExpression : Expression
	{
		public readonly Expression LeftExpression;
		public readonly Expression RightExpression;
		public readonly BinaryOperation OperationType;

		public SimpleBinaryOperationExpression(BinaryOperation operationType, Expression leftExpression, Expression rightExpression, TypeSpec resultTypeSpec)
			: base(new TypeSpec(resultTypeSpec.Type))
		{
			LeftExpression = leftExpression;
			RightExpression = rightExpression;
			OperationType = operationType;
		}
	}
}
