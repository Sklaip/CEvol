using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class PointerDereferenceExpression : Expression
	{
		public readonly Expression Target;

		public PointerDereferenceExpression(Expression target) : base(RemovePointerQualifier(target.ResultTypeSpec))
		{
			Target = target;
		}

		private static TypeSpec RemovePointerQualifier(TypeSpec typeSpec)
		{
			if (!typeSpec.QualifiersExists || typeSpec.Qualifiers[0].Kind != Qualifier.QKind.Reference)
				throw new NotImplementedException();

			if (typeSpec.Qualifiers.Length == 1) return new TypeSpec(typeSpec.Type);

			return new TypeSpec(typeSpec.Type, typeSpec.Qualifiers[1..]);
		}
	}
}
