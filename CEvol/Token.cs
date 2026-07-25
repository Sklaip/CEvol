using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol
{
	internal readonly struct Token
	{
		public TokenType Type { get; }

		public int Priority { 
			get
			{
				if (Type == TokenType.Var) return 3;
				else if (Type == TokenType.Multiply) return 2;
				else if (Type == TokenType.Plus) return 1;
				else return -1;
			} 
		}

		public ReadOnlyMemory<char> Value { get; }

		public int Line { get; }
		public int Column { get; }

		public Token(TokenType type, ReadOnlyMemory<char> value, int line, int column)
		{
			Type = type;
			Value = value;
			Line = line;
			Column = column;
		}
	}
}
