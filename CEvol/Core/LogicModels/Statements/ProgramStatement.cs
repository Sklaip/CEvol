using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Statements
{
	public class ProgramStatement : Statement
	{
		private readonly List<ILogicModel> _childs;

		public ProgramStatement() : base(new List<ILogicModel>())
		{
			_childs = (Childs as List<ILogicModel>)!; // ну и хуета, но да поебать
		}

		public void AddStatement(Statement statement)
		{
			_childs.Add(statement);
		}
	}
}
