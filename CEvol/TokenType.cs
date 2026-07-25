using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol
{
	public enum TokenType
	{
		// Ключевые слова
		Var, Func, If, Else, Return, While,

		//Типы
		IntType,
		LongType,

		// Литералы и идентификаторы (требуют сохранения значения)
		Identifier,
		Number,
		StringLiteral,
		CharLiteral,

		// Операторы и разделители
		Plus, Multiply, Assign,

		// Символы
		LeftCurlyBrace,
		RightCurlyBrace,
		LeftBracket,
		RightBracket,
		Semicolon,
		Comma
	}
}
