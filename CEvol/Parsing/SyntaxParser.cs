using Antlr4.Runtime.Misc;
using CEvol.Analysis;
using CEvol.Analysis.Members;
using CEvol.Analysis.Members.Models;
using CEvol.Analysis.Semantic;
using CEvol.Generation;
using System.Numerics;
using static CEvol.Analysis.Semantic.SemanticAnalyzer;

namespace CEvol.Parsing
{
	internal class SyntaxParser : CEvolParserBaseVisitor<Expr?>
	{
		private readonly MembersFinder _membersFinder;
		private CodeGenerator _codeGenerator = null!;
		private SemanticAnalyzer _semanticAnalyzer = null!;
		private TypeAnalyzer _typeAnalyzer = null!;
		private TypeDesc? _currentClass = null;

		public SyntaxParser(MembersFinder membersFinder, CodeGenerator codeGenerator)
		{
			_membersFinder = membersFinder;
			_codeGenerator = codeGenerator;
			_typeAnalyzer = new TypeAnalyzer(membersFinder);
		}

		public override Expr? VisitProgram(CEvolParser.ProgramContext context)
		{
			return VisitChildren(context);
		}

		public override Expr? VisitNamespaceDecl([NotNull] CEvolParser.NamespaceDeclContext context)
		{
			_semanticAnalyzer = new SemanticAnalyzer(_membersFinder, _codeGenerator, _typeAnalyzer);

			return VisitChildren(context);
		}

		public override Expr? VisitClassDecl([NotNull] CEvolParser.ClassDeclContext context)
		{
			_currentClass = _membersFinder.FindType(context.IDENTIFIER().GetText());
			_semanticAnalyzer.StartClassFill(_currentClass);
			var classParseResult = base.VisitClassDecl(context);
			_currentClass = null;
			_semanticAnalyzer.StopClassFill();
			return classParseResult;
		}

		public override Expr? VisitFunctionDecl([NotNull] CEvolParser.FunctionDeclContext context)
		{
			var prms = context.@params();

			List<(TypeSpec Type, string Name)> parameters = null!;

			if (prms != null)
			{
				parameters = ParseParams(prms);
			}

			if (parameters == null) parameters = [];

			TypeSpec typeSpec = ParseTypeSpec(context.typeSpec());
			string? funcName = context.IDENTIFIER().ToString();
			if (funcName == null) throw new NotImplementedException();

			FuncDesc funcDesc;
			if (_currentClass != null)
			{
				funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(_currentClass, funcName), parameters.Select(x => x.Type));
			}
			else
			{
				funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(funcName), parameters.Select(x => x.Type));
			}

			// TODO: учитывать что аргументов может не быть
			_semanticAnalyzer.StartFunctionBodyFill(funcDesc, funcName, typeSpec, parameters);

			Visit(context.block());

			_semanticAnalyzer.StopFuncCreating();

			return null;
		}

		public override Expr? VisitVarDeclStmt([NotNull] CEvolParser.VarDeclStmtContext context)
		{
			var ctx = context.fieldDecl();

			var typeSpec = ParseTypeSpec(ctx.typeSpec());

			var varName = ctx.IDENTIFIER().ToString();
			if (varName == null) throw new NotImplementedException();

			Expr varAccessing = _semanticAnalyzer.CreateLocalVariable(varName, typeSpec);

			if (ctx.ASSIGN() != null)
			{
				var value = Visit(ctx.expression());
				if (value == null) throw new NotImplementedException();

				Qualifier? qaliffer = typeSpec.QualifiersExists ? typeSpec.Qualifiers[0] : null;
				_semanticAnalyzer.VariableAssing(varAccessing, value.Value, qaliffer);
			}

			return null;
		}


		public override Expr? VisitAssignment([NotNull] CEvolParser.AssignmentContext context)
		{
			var expressions = context.expression();
			if (expressions.Length != 2)
				throw new NotImplementedException();

			var leftExpression = Visit(expressions[0]);
			var rightExpression = Visit(expressions[1]);

			var qualiffer = context.qualifier()?.GetText();

			if (leftExpression == null || rightExpression == null) throw new NotImplementedException();

			// TODO: где-то сделать проверку что это выражение - доступ к переменной, а не каккая-то хуета
			return _semanticAnalyzer.VariableAssing(leftExpression.Value, rightExpression.Value, qualiffer != null ? Qualifier.FromString(qualiffer) : null);
		}

		public override Expr? VisitCallExpr([NotNull] CEvolParser.CallExprContext context)
		{
			string? methodName = context.IDENTIFIER().ToString();
			if (methodName == null) throw new NotImplementedException();
			var args = context.args();

			var arguments = args != null ? ParseArgs(context.args()) : Array.Empty<Expr>();

			return _semanticAnalyzer.CallFunction(methodName, arguments);
		}

		private Expr[] ParseArgs(CEvolParser.ArgsContext context)
		{
			var ars = context?.expression();
			if (ars == null) return Array.Empty<Expr>();
			//return ars.Select(x => (IValueAccessor)Visit(x)).ToArray();
			Expr[] result = new Expr[ars.Length];

			for (int i = 0; i < ars.Length; i++)
			{
				var expr = Visit(ars[i]);
				if (expr == null) throw new NotImplementedException();

				result[i] = expr.Value;
			}

			return result;
		}

		private List<(TypeSpec Type, string Name)> ParseParams([NotNull] CEvolParser.ParamsContext context)
		{
			var parameters = new List<(TypeSpec Type, string Name)>();

			int count = context.typeSpec().Length;

			for (int i = 0; i < count; i++)
			{
				TypeSpec paramDecl = ParseTypeSpec(context.typeSpec(i));
				string paramName = context.IDENTIFIER(i).GetText();

				parameters.Add((paramDecl, paramName));
			}

			return parameters;
		}

		public override Expr? VisitLocExpr([NotNull] CEvolParser.LocExprContext context)
		{
			var value = Visit(context.expression());
			if (value == null) throw new NotImplementedException();

			return _semanticAnalyzer.GetPointerToVar(value.Value);
		}

		public override Expr? VisitNewExpr([NotNull] CEvolParser.NewExprContext context)
		{
			string? className = context.IDENTIFIER()?.GetText();
			if (className == null) throw new NotImplementedException();

			// TODO: пропарсить аргументы
			return _semanticAnalyzer.CallHeapConstructor(className, new Expr[0]);
		}

		public override Expr? VisitNewArrayExpr([NotNull] CEvolParser.NewArrayExprContext context)
		{
			if (context.arraySizeSpec().Length > 1)
				throw new NotImplementedException(); // TODO: реализовать многомерные массивы

			var arrySizeGettingExpr = ParseArraySizeSpec(context.arraySizeSpec()[0]);

			if (context.IDENTIFIER()?.GetText() == null || arrySizeGettingExpr == null)
				throw new NotImplementedException();

			return _semanticAnalyzer.CreateArrayInHeap(context.IDENTIFIER().GetText(), arrySizeGettingExpr.Value);
		}

		public override Expr? VisitStackNewExpr([NotNull] CEvolParser.StackNewExprContext context)
		{
			return base.VisitStackNewExpr(context);
		}

		public override Expr? VisitStackNewArrayExpr([NotNull] CEvolParser.StackNewArrayExprContext context)
		{
			return base.VisitStackNewArrayExpr(context);
		}


		public override Expr? VisitIndexExpr([NotNull] CEvolParser.IndexExprContext context)
		{
			if (context.expression() == null || context.args() == null)
				throw new NotImplementedException();

			Expr? expr = Visit(context.expression());
			if (expr == null)
				throw new NotImplementedException();

			Expr[] args = ParseArgs(context.args());

			if (args.Length != 1)  // TODO: реализовать многомерные массивы
				throw new NotImplementedException();

			return _semanticAnalyzer.ArrayCellAccess(expr.Value, args[0]);
		}

		public Expr? ParseArraySizeSpec([NotNull] CEvolParser.ArraySizeSpecContext context)
		{
			if (context.expression().Length > 1)
				throw new NotImplementedException(); // TODO: реализовать многомерные массивы

			return Visit(context.expression()[0]);
		}

		public override Expr? VisitMemberAccess([NotNull] CEvolParser.MemberAccessContext context)
		{
			var expr = Visit(context.expression());
			if (expr == null) throw new NotImplementedException();

			string? memberName = context.IDENTIFIER().GetText();
			if (memberName == null) throw new NotImplementedException();

			if (context.LPAREN() == null)
			{
				return _semanticAnalyzer.ClassFieldAccess(expr.Value, memberName);
			}
			else
			{
				var arguments = ParseArgs(context.args());
				return _semanticAnalyzer.CallClassMethod(memberName, expr.Value, arguments);
			}
		}

		private TypeSpec ParseTypeSpec([NotNull] CEvolParser.TypeSpecContext context)
		{
			// TODO: поля классов парсить здесь повторно смысла нет. Надо как-то это все кэшировать
			var typeName = context.IDENTIFIER().GetText();
			if (string.IsNullOrEmpty(typeName))
				throw new NotImplementedException();

			var qualifiers = new List<string>();
			foreach (var qualifier in context.qualifier())
			{
				qualifiers.Add(qualifier.GetText());
			}

			foreach (var arr in context.arraySpec())
			{
				qualifiers.Add(ParseArraySpec(arr));
			}

			return new TypeSpec(_membersFinder.FindType(typeName), Qualifier.FromString(qualifiers));
		}

		public string ParseArraySpec([NotNull] CEvolParser.ArraySpecContext context)
		{
			if (context.COMMA().Length > 0)
				throw new NotImplementedException(); // TODO: сделать многомерные массив

			return "array";
		}

		public override Expr? VisitNumberExpr([NotNull] CEvolParser.NumberExprContext context)
		{
			var value = context.NUMBER().GetText();
			var num = BigInteger.Parse(value);

			if (context.MINUS() != null) num *= -1;

			if (num >= 0 && num <= 255)
			{
				return _semanticAnalyzer.CreateByte((byte)num);
			}
			else
			{
				return _semanticAnalyzer.CreateInt((int)num);
			}
		}

		public override Expr? VisitReturnStmt([NotNull] CEvolParser.ReturnStmtContext context)
		{
			var result = Visit(context.expression());
			if (result == null) throw new NotImplementedException();

			_semanticAnalyzer.AddReturn(result.Value);
			return null;
		}

		public override Expr? VisitIfStmt([NotNull] CEvolParser.IfStmtContext context)
		{
			var ctx = context.ifStatement();

			var condition = Visit(ctx.expression());
			if (condition == null) throw new NotImplementedException();

			_semanticAnalyzer.StartIfBlock(condition.Value);
			foreach (var statement in ctx.statement())
			{
				Visit(statement);
			}

			_semanticAnalyzer.EndIfBlock();

			return null;
		}

		public override Expr? VisitExprStmt([NotNull] CEvolParser.ExprStmtContext context)
		{
			return base.VisitExprStmt(context);
		}

		public override Expr? VisitIdExpr([NotNull] CEvolParser.IdExprContext context)
		{
			var varName = context.IDENTIFIER().ToString();
			if (varName == null) throw new NotImplementedException();

			return _semanticAnalyzer.VariableAccess(varName);
		}

		public override Expr? VisitAddSubExpr([NotNull] CEvolParser.AddSubExprContext context)
		{
			var expressions = context.expression();
			if (expressions.Length != 2) throw new NotImplementedException();

			var leftValue = Visit(expressions[0]);
			var rightValue = Visit(expressions[1]);

			if (leftValue == null || rightValue == null) throw new NotImplementedException();

			if (context.MINUS() != null) // это минус
			{
				return _semanticAnalyzer.Sub(leftValue.Value, rightValue.Value);
			}
			else // это плюс
			{
				return _semanticAnalyzer.Sum(leftValue.Value, rightValue.Value);
			}
		}

		public override Expr? VisitParenExpr([NotNull] CEvolParser.ParenExprContext context)
		{
			return Visit(context.expression());
		}

		public override Expr? VisitEqNeqExpr([NotNull] CEvolParser.EqNeqExprContext context)
		{
			(Expr left, Expr right) = ParseBinaryExpression(context.expression());

			CompareOperator compareOperator;
			if (context.NEQ() != null)
				compareOperator = CompareOperator.NotEqual;
			else if (context.EQ() != null)
				compareOperator = CompareOperator.Equal;
			else
				throw new NotImplementedException();

			return _semanticAnalyzer.Compare(left, right, compareOperator);
		}

		public override Expr? VisitLtGtExpr([NotNull] CEvolParser.LtGtExprContext context)
		{
			(Expr left, Expr right) = ParseBinaryExpression(context.expression());

			CompareOperator compareOperator;
			if (context.LT() != null)
				compareOperator = CompareOperator.LessThan;
			else if (context.GT() != null)
				compareOperator = CompareOperator.GreaterThan;
			else
				throw new NotImplementedException();

			return _semanticAnalyzer.Compare(left, right, compareOperator);
		}

		public override Expr? VisitBitAndExpr([NotNull] CEvolParser.BitAndExprContext context)
		{
			(Expr left, Expr right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.BitAnd(left, right);
		}

		public override Expr? VisitBitXorExpr([NotNull] CEvolParser.BitXorExprContext context)
		{
			(Expr left, Expr right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.BitXor(left, right);
		}

		public override Expr? VisitBitOrExpr([NotNull] CEvolParser.BitOrExprContext context)
		{
			(Expr left, Expr right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.BitOr(left, right);
		}

		public override Expr? VisitLogicalAndExpr([NotNull] CEvolParser.LogicalAndExprContext context)
		{
			(Expr left, Expr right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.LogicalAnd(left, right);
		}

		private (Expr left, Expr right) ParseBinaryExpression(CEvolParser.ExpressionContext[]? expressions)
		{
			if (expressions == null || expressions.Length != 2) throw new NotImplementedException();

			var leftValue = Visit(expressions[0]);
			var rightValue = Visit(expressions[1]);

			if (leftValue == null || rightValue == null) throw new NotImplementedException();

			return (leftValue.Value, rightValue.Value);
		}

	}
}
