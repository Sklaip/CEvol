using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public abstract class ConstOperationExpression<TValue> : Expression
	{
		public readonly BaseTypes IntType;
		public TValue Value;

		public ConstOperationExpression(TypeSpec intTypeSpec, BaseTypes intType, TValue value) : base(intTypeSpec)
		{
			IntType = intType;
			Value = value;
		}
	}
}
