using CEvol.Core.LogicModels.Expressions;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Statements
{
	internal class WhileStatement : Statement
	{
		public readonly Expression Condition;

		public WhileStatement(IReadOnlyCollection<ILogicModel> childs, Expression condition) : base(childs)
		{
			Condition = condition;
		}
	}
}
