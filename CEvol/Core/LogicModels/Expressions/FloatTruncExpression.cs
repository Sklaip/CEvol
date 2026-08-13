using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class FloatTruncExpression : Expression
	{
		public readonly Expression NumGetting;

		public FloatTruncExpression(Expression numGetting, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			NumGetting = numGetting;
		}
	}
}
