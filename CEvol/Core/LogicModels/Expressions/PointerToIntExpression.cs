using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class PointerToIntExpression : Expression
	{
		public readonly Expression PointerGetting;

		public PointerToIntExpression(Expression pointerGetting, TypeSpec resultTypeSpec) : base(resultTypeSpec)
		{
			PointerGetting = pointerGetting;
		}
	}
}
