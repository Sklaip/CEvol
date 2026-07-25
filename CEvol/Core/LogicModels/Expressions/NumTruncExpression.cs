using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class NumTruncExpression : Expression
	{
		public readonly Expression NumGetting;

		public NumTruncExpression(Expression numGetting, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			NumGetting = numGetting;
		}
	}
}
