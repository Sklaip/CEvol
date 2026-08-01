using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.CLI
{
	internal interface ICommandExecutor
	{
		bool IsDefault { get; }
		string Name { get; }
		string Execute(IEnumerable<string> arguments);
	}
}
