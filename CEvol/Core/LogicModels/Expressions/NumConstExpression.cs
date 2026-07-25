using CEvol.Analysis.Members.Models;
using CEvol.Parsing;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class NumConstExpression(TypeSpec intTypeSpec, BaseTypes intType, BigInteger value) : ConstOperationExpression<BigInteger>(intTypeSpec, intType, value);
}
