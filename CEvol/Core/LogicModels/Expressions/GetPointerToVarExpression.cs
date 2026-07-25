using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class GetPointerToVarExpression : Expression
	{
		public readonly Expression Variable;
		public GetPointerToVarExpression(Expression variable) : base(PointerTypeSpec(variable.ResultTypeSpec))
		{
			Variable = variable;
		}

		private static TypeSpec PointerTypeSpec(TypeSpec typeScec)
		{
			return new TypeSpec(typeScec.Type, [new Qualifier(Qualifier.QKind.Reference), .. typeScec.Qualifiers]);
		}
	}
}
