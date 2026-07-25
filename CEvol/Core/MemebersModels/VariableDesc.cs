using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.MemebersModels
{
	public class VariableDesc
	{
		public readonly TypeSpec Declaring;
		public readonly string Name;
		public readonly uint Order;

		public VariableDesc(TypeSpec type, string name, uint order)
		{
			Declaring = type;
			Name = name;
			Order = order;
		}
	}
}
