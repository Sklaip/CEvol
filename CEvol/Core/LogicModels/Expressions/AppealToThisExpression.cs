using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class AppealToThisExpression : Expression
	{
		public AppealToThisExpression(TypeDesc cls) : base(new TypeSpec(cls, [new Qualifier(Qualifier.QKind.Reference)]))
		{
		}
	}
}
