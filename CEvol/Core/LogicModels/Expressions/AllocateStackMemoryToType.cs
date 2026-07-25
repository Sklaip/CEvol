using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class AllocateStackMemoryToType : Expression
	{
		public readonly Expression? Multiper;

		public AllocateStackMemoryToType(TypeSpec resultTypeSpec, Expression? multiper = null) : base(resultTypeSpec)
		{
			Multiper = multiper;
		}
	}
}
