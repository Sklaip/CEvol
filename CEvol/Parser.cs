using CEvol.Parsing;
using CEvol.Parsing.Operations;
using CEvol.Parsing.Operations.Results;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace CEvol
{
	internal class ParserOld
	{
		private HashSet<Member> _members = new HashSet<Member>();
		public List<IOperation> Load(List<Token> tokens)
		{
			var result = new List<IOperation>();

			int point = 0;
			IOperation? lastOperation = null;
			while (point < tokens.Count)
			{
				var operation = Load(tokens, ref point, lastOperation);
				lastOperation = operation;
				if (operation == null) continue;
				result.Add(operation);
			}

			return result;

		}

		public IOperation? Load(List<Token> tokens, ref int point, IOperation? lastOperation)
		{
			if (point >= tokens.Count) return null;

			Token currentToken = tokens[point];

			switch (currentToken.Type)
			{
				case TokenType.Semicolon:
					point++;
					return null;
				case TokenType.Var:
					{
						point++;
						string typeName = tokens[point].Value.ToString();
						point++;
						string varName = tokens[point].Value.ToString();
						point++;

						Member member = new Member(varName, MemberType.Var);
						if (_members.Contains(member))
						{
							throw new NotImplementedException();
						}

						_members.Add(member);

						var varCreate = new VarCreate(varName, new DataType(typeName));
						return SingleOperationLink(varCreate, tokens, ref point);
					}

				case TokenType.Assign:
					{
						if (lastOperation == null || lastOperation.OperationResult is not VarAccessingResult)
							throw new NotImplementedException();

						var assing = new Assign();

						point++;
						return BinaryOperationLink<ValueExtraction>(assing, lastOperation, tokens, ref point);
					}

				case TokenType.Plus:
					{
						if (lastOperation == null || lastOperation.OperationResult is not ValueExtraction)
							throw new NotImplementedException();

						var sum = new Sum();
						point++;
						return PriorityBinaryOperationLink<ValueExtraction>(sum, lastOperation, tokens, ref point);
					}

				case TokenType.Multiply:
					{
						if (lastOperation == null || lastOperation.OperationResult is not ValueExtraction)
							throw new NotImplementedException();

						var sum = new Multiple();
						point++;
						return PriorityBinaryOperationLink<ValueExtraction>(sum, lastOperation, tokens, ref point);
					}

				case TokenType.Identifier:
					{
						if (!_members.TryGetValue(new Member(currentToken.Value.ToString()), out Member member))
						{
							throw new NotImplementedException();
						}

						if (member.Type == MemberType.Var)
						{
							var accessing = new VarAccessing(member);
							point++;
							return SingleOperationLink(accessing, tokens, ref point);
						}
						else
						{
							throw new NotImplementedException();
						}
					}

				case TokenType.Number:
					{
						var valueCreate = new ValueCreate(currentToken.Value.ToString(), new DataType("int"));
						point++;
						return SingleOperationLink(valueCreate, tokens, ref point);
					}

				case TokenType.LeftBracket:
					{
						point++;
						var nestedSegment = ParseSegment(tokens, ref point, TokenType.LeftBracket, TokenType.RightBracket);
						int segmentPoint = 0;

						var opearion = Load(nestedSegment, ref segmentPoint, null);
						if (opearion == null)
						{
							throw new NotImplementedException();
						}

						return SingleOperationLink(opearion, tokens, ref point);
					}

				default:
					throw new NotImplementedException();

			}
		}

		private List<Token> ParseSegment(List<Token> tokens, ref int point, TokenType openingToken, TokenType closingToken)
		{
			var segmentTokens = new List<Token>();
			int nestingLevel = 0;
			while (point < tokens.Count)
			{
				var currentToken = tokens[point];
				if (currentToken.Type == closingToken && nestingLevel == 0)
				{
					point++;
					return segmentTokens;
				}

				segmentTokens.Add(currentToken);

				if (currentToken.Type == openingToken) nestingLevel++;

				point++;
			}

			throw new NotImplementedException();

		}

		private IOperation SingleOperationLink(IOperation currentOperation, List<Token> tokens, ref int point)
		{
			//var nextOperation = Load(tokens, ref point, currentOperation);
			//if (nextOperation == null) return currentOperation;

			//nextOperation.LeftOpearion = currentOperation;
			//return nextOperation;
			return currentOperation;
		}

		private IOperation BinaryOperationLink<TNextRes>(IOperation currentOperation, IOperation lastOperation, List<Token> tokens, ref int point) where TNextRes : IOperationResult
		{
			var nextOperation = Load(tokens, ref point, currentOperation);
			if (nextOperation == null || nextOperation.OperationResult is not TNextRes)
				throw new NotImplementedException();

			currentOperation.LeftOpearion = lastOperation;
			currentOperation.RightOpearion = nextOperation;

			return currentOperation;
		}

		private IOperation PriorityBinaryOperationLink<TNextRes>(IOperation currentOperation, IOperation lastOperation, List<Token> tokens, ref int point) where TNextRes : IOperationResult
		{
			var nextOperation = Load(tokens, ref point, currentOperation);
			if (nextOperation == null || nextOperation.OperationResult is not TNextRes)
				throw new NotImplementedException();

			currentOperation.LeftOpearion = lastOperation;
			currentOperation.RightOpearion = nextOperation;


			return currentOperation;
		}
	}
}
