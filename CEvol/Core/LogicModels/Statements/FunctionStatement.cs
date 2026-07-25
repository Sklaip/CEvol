using CEvol.Core.LogicModels.Expressions;
using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Statements
{
	public class FunctionStatement : Statement
	{
		public readonly FuncDesc FunctionSignature;

		public FunctionStatement(FuncDesc functionSignature, IReadOnlyCollection<ILogicModel> childs) : base(childs)
		{
			FunctionSignature = functionSignature;
		}
	}
}
