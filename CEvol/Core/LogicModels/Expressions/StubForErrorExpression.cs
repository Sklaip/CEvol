using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class StubForErrorExpression : Expression
	{
		public StubForErrorExpression(PositionInSources pos) : base(new TypeSpec(new TypeDesc("ERROR", null!)), pos)
		{
		}
	}
}
