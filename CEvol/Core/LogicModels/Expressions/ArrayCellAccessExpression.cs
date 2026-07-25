using CEvol.Analysis.Members.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class ArrayCellAccessExpression : Expression
	{
		public readonly Expression ArrayGetting;
		public readonly Expression IndexGetting;

		public ArrayCellAccessExpression(Expression arrayGetting, Expression indexGetting) : base(new TypeSpec(arrayGetting.ResultTypeSpec.Type))
		{
			ArrayGetting = arrayGetting;
			IndexGetting = indexGetting;
		}

		private static TypeSpec RemoveArrayQualifier(TypeSpec typeSpec)
		{
			if (!typeSpec.QualifiersExists || typeSpec.Qualifiers[0].Kind != Qualifier.QKind.Array)
				throw new NotImplementedException();

			if (typeSpec.Qualifiers.Length == 1) return new TypeSpec(typeSpec.Type);

			return new TypeSpec(typeSpec.Type, typeSpec.Qualifiers[1..]);
		}
	}
}
