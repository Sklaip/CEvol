using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public abstract class Expression : ILogicModel
	{
		public readonly TypeSpec ResultTypeSpec;

		protected Expression(TypeSpec resultTypeSpec)
		{
			ResultTypeSpec = resultTypeSpec;
		}
	}
}
