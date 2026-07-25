using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Statements
{
	public abstract class Statement : ILogicModel
	{
		public readonly IReadOnlyCollection<ILogicModel> Childs;

		protected Statement(IReadOnlyCollection<ILogicModel> childs)
		{
			Childs = childs;
		}
	}
}
