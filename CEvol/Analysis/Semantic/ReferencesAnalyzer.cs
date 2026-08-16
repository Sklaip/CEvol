using CEvol.Core;
using CEvol.Core.LogicModels;
using CEvol.Core.LogicModels.Expressions;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Analysis.Semantic
{
	internal class ReferencesData
	{
		public string? VariableName;
		public bool IsOwnerRef = false;
		public bool IsBorrowRef = false;

		public ReferencesData(bool isBorrowRef, bool isOwnerRef, string? variableName)
		{
			IsBorrowRef = isBorrowRef;
			IsOwnerRef = isOwnerRef;
			VariableName = variableName;
		}

		public ReferencesData(Expression expr):
			this(expr.ResultTypeSpec.IsBorrowRef, expr.ResultTypeSpec.IsOwnerRef, null)
		{
		}
	}

	internal class ReferencesAnalyzer : SemanticTreeVisitor<ReferencesData?>
	{
		private readonly ErrorsBag _errorsBag;
		private HashSet<string> _givenRefs = new HashSet<string>();

		public ReferencesAnalyzer(ErrorsBag errorsBag)
		{
			_errorsBag = errorsBag;
		}

		protected override void HandleFunctionalBlock<TBlock>(TBlock statement)
		{
			base.HandleFunctionalBlock(statement);
			_givenRefs = new HashSet<string>();
		}

		protected override ReferencesData? CallFunction(CallFunctionExpression expr)
		{
			for (int i = 0; i < expr.Function.Arguments.Length; i++)
			{
				TypeSpec acceptedtype = expr.Function.Arguments[i].Declaring;
				Expression argument = expr.Arguments[i];

				var res = HandleExpression(argument);
				if (acceptedtype.IsOwnerRef)
				{
					if (argument.ResultTypeSpec.IsBorrowRef)
					{
						throw new NotImplementedException();
					}

					if (res?.VariableName != null)
					{
						_givenRefs.Add(res.VariableName);
					}
				}
			}

			return new ReferencesData(expr);
		}

		protected override ReferencesData VarAccess(VariableAccessExpression expr)
		{
			if (_givenRefs.Contains(expr.Name))
			{
				throw new NotImplementedException();
			}

			return new ReferencesData(expr.ResultTypeSpec.IsBorrowRef, expr.ResultTypeSpec.IsOwnerRef, expr.Name);
		}
	}
}
