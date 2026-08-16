using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class IntToFloatExtensionExpression : Expression
	{
		public readonly Expression NumGetting;
		public readonly bool IsSigned;

		public IntToFloatExtensionExpression(Expression numGetting, bool isSigned, TypeSpec resultTypeSpec, PositionInSources pos) : base(resultTypeSpec, pos)
		{
			NumGetting = numGetting;
			IsSigned = isSigned;
		}
	}
}
