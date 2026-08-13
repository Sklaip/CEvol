using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class FloatToFloatExpression : Expression
	{
		public readonly Expression NumGetting;

		public FloatToFloatExpression(Expression numGetting, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			NumGetting = numGetting;
		}
	}
}
