using CEvol.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.MemebersModels
{
	public class FuncDesc
	{
		public class Argument
		{
			public readonly TypeSpec Declaring;
			public readonly string Name;

			public Argument(TypeSpec declaring, string name)
			{
				Declaring = declaring;
				Name = name;
			}
		}

		public readonly TypeSpec ReturnType;
		public readonly string Name;
		public readonly Argument[] Arguments;
		public readonly FuncRefData RefData;
		public readonly bool IsInfArgs;

		public FuncDesc(TypeSpec returnType, string name, Argument[] arguments, FuncRefData refData, bool isInfArgs)
		{
			ReturnType = returnType;
			Name = name;
			Arguments = arguments;
			RefData = refData;
			IsInfArgs = isInfArgs;
		}

	}
}
