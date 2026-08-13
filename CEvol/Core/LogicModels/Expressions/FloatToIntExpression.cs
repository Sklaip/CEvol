using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class FloatToIntExpression : Expression
	{
		public readonly Expression NumGetting;
		public readonly bool IsSigned;

		public FloatToIntExpression(Expression numGetting, bool isSigned, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			NumGetting = numGetting;
			IsSigned = isSigned;
		}
	}
}
