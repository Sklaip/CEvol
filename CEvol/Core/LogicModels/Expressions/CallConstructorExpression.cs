using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class CallConstructorExpression : Expression
	{
		public readonly Expression MemoryGetting;

		public readonly ConstructorDesc Constructor;
		public readonly Expression[] Arguments;


		public CallConstructorExpression(Expression memoryGetting, ConstructorDesc constructor, Expression[] arguments) : base(memoryGetting.ResultTypeSpec)
		{
			MemoryGetting = memoryGetting;
			Constructor = constructor;
			Arguments = arguments;
		}
	}
}
