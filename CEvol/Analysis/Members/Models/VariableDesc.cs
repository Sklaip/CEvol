using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Analysis.Members.Models
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
