using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	internal class CallFunctionExpression : Expression
	{
		public readonly FuncDesc Function;
		public readonly Expression[] Arguments;

		public CallFunctionExpression(Expression[] arguments, FuncDesc function) : base(function.ReturnType.Value)
		{
			Arguments = arguments;
			Function = function;
		}
	}
}
