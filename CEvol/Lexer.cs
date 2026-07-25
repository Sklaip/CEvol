using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CEvol
{
	internal class LexerOld
	{
		public List<Token> Load(string sourceCode)
		{
			var tokens = new List<Token>();
			int i = 0;
			while (i < sourceCode.Length)
			{
				SkipWhitespace(ref i, sourceCode);

				var tokenData = ReadToken(ref i, sourceCode);

				if (!tokenData.IsEmpty)
				{
					tokens.Add(ParseToken(tokenData));
				}
			}

			return tokens;

		}

		private void SkipWhitespace(ref int point, string sourceCode)
		{
			while (point < sourceCode.Length && char.IsWhiteSpace(sourceCode[point]))
			{
				point++;
			}
		}

		private ReadOnlyMemory<char> ReadToken(ref int point, string sourceCode)
		{
			if (!char.IsLetterOrDigit(sourceCode[point]))
			{
				int currentPoint = point;
				point++;
				return sourceCode.AsMemory(currentPoint, 1);
			}

			int startIndex = point;
			while (point < sourceCode.Length && !char.IsWhiteSpace(sourceCode[point]) && char.IsLetterOrDigit(sourceCode[point]))
			{
				point++;
			}

			return sourceCode.AsMemory(startIndex, point - startIndex);
		}

		private Token ParseToken(ReadOnlyMemory<char> tokenData)
		{
			if (char.IsDigit(tokenData.Span[0]))
			{
				return new Token(TokenType.Number, tokenData, 0, 0);
			}
			else if (Identifiers.TryGetValue(tokenData.ToString(), out TokenType type))
			{
				return new Token(type, null, 0, 0);
			}
			else
			{
				return new Token(TokenType.Identifier, tokenData, 0, 0);
			}
		}

		private Dictionary<string, TokenType> Identifiers = new()
		{
			["var"] = TokenType.Var,
			["func"] = TokenType.Func,
			["if"] = TokenType.If,
			["else"] = TokenType.Else,
			["return"] = TokenType.Return,
			["while"] = TokenType.While,

			["int"] = TokenType.IntType,
			["long"] = TokenType.LongType,

			["+"] = TokenType.Plus,
			["*"] = TokenType.Multiply,
			["="] = TokenType.Assign,
			[";"] = TokenType.Semicolon,
			["("] = TokenType.LeftBracket,
			[")"] = TokenType.RightBracket,
			["{"] = TokenType.LeftCurlyBrace,
			["}"] = TokenType.RightCurlyBrace,
			[","] = TokenType.Comma,
		};
	}
}
