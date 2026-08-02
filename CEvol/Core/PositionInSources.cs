using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core
{
	internal struct PositionInSources
	{
		public readonly string SourceFile;
		public readonly int Line;
		public readonly int Symbol;

		public PositionInSources(string sourceFile, int line, int symbol) : this()
		{
			SourceFile = sourceFile;
			Line = line;
			Symbol = symbol;
		}
	}
}
