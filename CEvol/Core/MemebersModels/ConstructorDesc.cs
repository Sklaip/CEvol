using CEvol.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.MemebersModels
{
	public class ConstructorDesc
	{
		public readonly Argument[] Arguments;
		public readonly FuncRefData RefData;

		public ConstructorDesc(Argument[] arguments, FuncRefData refData)
		{
			Arguments = arguments;
			RefData = refData;
		}

	}
}
