using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class NumConstExpression(TypeSpec intTypeSpec, BaseTypes intType, BigInteger value, PositionInSources pos) : ConstOperationExpression<BigInteger>(intTypeSpec, intType, value, pos);
}
