using CEvol.Analysis.Members.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Statements
{
	internal class ClassStatement : Statement
	{
		public readonly TypeDesc TypeDesc;
		public ClassStatement(TypeDesc typeDesc, IReadOnlyCollection<ILogicModel> childs) : base(childs)
		{
			TypeDesc = typeDesc;
		}
	}
}
