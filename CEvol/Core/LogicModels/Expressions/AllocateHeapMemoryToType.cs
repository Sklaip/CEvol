using CEvol.Analysis.Members.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class AllocateHeapMemoryToType : Expression
	{
		public readonly Expression? Multiper;

		public AllocateHeapMemoryToType(TypeSpec resultTypeSpec, Expression? multiper = null) : base(resultTypeSpec)
		{
			Multiper = multiper;
		}
	}
}
