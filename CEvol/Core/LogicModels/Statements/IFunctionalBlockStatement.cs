using CEvol.Core.MemebersModels;
using CEvol.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Statements
{
	public interface IFunctionalBlockStatement
	{
		TypeSpec ReturnType { get; }
		Argument[] Arguments { get; }
		FuncRefData RefData { get; }
		string Name { get; }
	}
}
