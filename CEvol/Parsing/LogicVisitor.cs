using Antlr4.Runtime.Misc;
using CEvol.Analysis;
using CEvol.Analysis.Semantic;
using CEvol.Core;
using CEvol.Core.LogicModels.Expressions;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using System.Numerics;


namespace CEvol.Parsing
{
	internal class LogicVisitor : CEvolParserBaseVisitor<Expression?>
	{
		private readonly MembersFinder _membersFinder;
		private readonly SemanticTreeBuilder _semanticAnalyzer;
		private readonly TypeAnalyzer _typeAnalyzer;

		public Statement ResultStatement { get; private set; }

		public LogicVisitor(MembersFinder membersFinder)
		{
			_typeAnalyzer = new TypeAnalyzer(membersFinder);
			_semanticAnalyzer = new SemanticTreeBuilder(membersFinder, _typeAnalyzer);
			_membersFinder = membersFinder;
		}

		public override Expression? VisitProgram(CEvolParser.ProgramContext context)
		{
			VisitChildren(context);
			ResultStatement = _semanticAnalyzer.ExitFromBlock();
			return null;
		}

		public override Expression? VisitNamespaceDecl([NotNull] CEvolParser.NamespaceDeclContext context)
		{
			string name = context.IDENTIFIER().GetText();
			_semanticAnalyzer.EnterToNameSpace(name);

			VisitChildren(context);

			return null;
		}

		public override Expression? VisitClassDecl([NotNull] CEvolParser.ClassDeclContext context)
		{
			_semanticAnalyzer.EnterToClass(context.IDENTIFIER().GetText());
			base.VisitClassDecl(context);
			_semanticAnalyzer.ExitFromBlock();

			return null;
		}

		public override Expression? VisitFunctionDecl([NotNull] CEvolParser.FunctionDeclContext context)
		{
			var prms = context.@params();

			List<(TypeSpec Type, string Name)> parameters = null!;

			if (prms != null)
			{
				parameters = ParseParams(prms);
			}

			if (parameters == null) parameters = [];

			string? funcName = context.IDENTIFIER().ToString();
			if (funcName == null) throw new NotImplementedException();

			_semanticAnalyzer.EnterToFunction(funcName, parameters);

			Visit(context.block());

			_semanticAnalyzer.ExitFromBlock();

			return null;
		}

		public override Expression? VisitBlock([NotNull] CEvolParser.BlockContext context)
		{
			foreach (var statement in context.statement())
			{
				Expression? expr = Visit(statement);
				if (expr != null)
				{
					_semanticAnalyzer.InserToCurrentBlock(expr);
				}
			}

			return null;
		}

		public override Expression? VisitVarDeclStmt([NotNull] CEvolParser.VarDeclStmtContext context)
		{
			var ctx = context.fieldDecl();

			var typeSpec = ParseTypeSpec(ctx.typeSpec());

			var varName = ctx.IDENTIFIER().ToString();
			if (varName == null) throw new NotImplementedException();

			Expression varAccessing = _semanticAnalyzer.CreateLocalVariable(varName, typeSpec);

			if (ctx.ASSIGN() != null)
			{
				var value = Visit(ctx.expression());
				if (value == null) throw new NotImplementedException();

				Qualifier? qaliffer = typeSpec.QualifiersExists ? typeSpec.Qualifiers[0] : null;
				return _semanticAnalyzer.VariableAssing(varAccessing, value, qaliffer);
			}

			return varAccessing;
		}

		public override Expression? VisitExprStmt([NotNull] CEvolParser.ExprStmtContext context)
		{
			return Visit(context.expression());
		}

		public override Expression VisitAssignStmt([NotNull] CEvolParser.AssignStmtContext ctx)
		{
			var context = ctx.assignment();
			var expressions = context.expression();
			if (expressions.Length != 2)
				throw new NotImplementedException();

			var leftExpression = Visit(expressions[0]);
			var rightExpression = Visit(expressions[1]);

			var qualiffer = context.qualifier()?.GetText();

			if (leftExpression == null || rightExpression == null) throw new NotImplementedException();

			// TODO: где-то сделать проверку что это выражение - доступ к переменной, а не каккая-то хуета
			return _semanticAnalyzer.VariableAssing(leftExpression, rightExpression, qualiffer != null ? Qualifier.FromString(qualiffer) : null);
		}

		public override Expression? VisitCallExpr([NotNull] CEvolParser.CallExprContext context)
		{
			string? funcName = context.IDENTIFIER().ToString();
			if (funcName == null) throw new NotImplementedException();
			var args = context.args();

			var arguments = args != null ? ParseArgs(context.args()) : Array.Empty<Expression>();

			return _semanticAnalyzer.CallFunction(funcName, arguments);
		}

		private Expression[] ParseArgs(CEvolParser.ArgsContext context)
		{
			var ars = context?.expression();
			if (ars == null) return Array.Empty<Expression>();
			//return ars.Select(x => (IValueAccessor)Visit(x)).ToArray();
			Expression[] result = new Expression[ars.Length];

			for (int i = 0; i < ars.Length; i++)
			{
				var expr = Visit(ars[i]);
				if (expr == null) throw new NotImplementedException();

				result[i] = expr;
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

		public override Expression? VisitLocExpr([NotNull] CEvolParser.LocExprContext context)
		{
			var value = Visit(context.expression());
			if (value == null) throw new NotImplementedException();

			return _semanticAnalyzer.GetPointerToVar(value);
		}

		public override Expression? VisitNewExpr([NotNull] CEvolParser.NewExprContext context)
		{
			string? className = context.IDENTIFIER()?.GetText();
			if (className == null) throw new NotImplementedException();

			// TODO: пропарсить аргументы
			return _semanticAnalyzer.CallHeapConstructor(className, new Expression[0]);
		}

		public override Expression? VisitNewArrayExpr([NotNull] CEvolParser.NewArrayExprContext context)
		{
			if (context.arraySizeSpec().Length > 1)
				throw new NotImplementedException(); // TODO: реализовать многомерные массивы

			var arrySizeGettingExpr = ParseArraySizeSpec(context.arraySizeSpec()[0]);

			if (context.IDENTIFIER()?.GetText() == null || arrySizeGettingExpr == null)
				throw new NotImplementedException();

			return _semanticAnalyzer.CreateArrayInHeap(context.IDENTIFIER().GetText(), arrySizeGettingExpr);
		}

		public override Expression? VisitStackNewExpr([NotNull] CEvolParser.StackNewExprContext context)
		{
			return base.VisitStackNewExpr(context);
		}

		public override Expression? VisitStackNewArrayExpr([NotNull] CEvolParser.StackNewArrayExprContext context)
		{
			return base.VisitStackNewArrayExpr(context);
		}


		public override Expression? VisitIndexExpr([NotNull] CEvolParser.IndexExprContext context)
		{
			if (context.expression() == null || context.args() == null)
				throw new NotImplementedException();

			Expression? expr = Visit(context.expression());
			if (expr == null)
				throw new NotImplementedException();

			Expression[] args = ParseArgs(context.args());

			if (args.Length != 1)  // TODO: реализовать многомерные массивы
				throw new NotImplementedException();

			return _semanticAnalyzer.ArrayCellAccess(expr, args[0]);
		}

		public Expression? ParseArraySizeSpec([NotNull] CEvolParser.ArraySizeSpecContext context)
		{
			if (context.expression().Length > 1)
				throw new NotImplementedException(); // TODO: реализовать многомерные массивы

			return Visit(context.expression()[0]);
		}

		public override Expression? VisitMemberAccess([NotNull] CEvolParser.MemberAccessContext context)
		{
			var expr = Visit(context.expression());
			if (expr == null) throw new NotImplementedException();

			string? memberName = context.IDENTIFIER().GetText();
			if (memberName == null) throw new NotImplementedException();

			if (context.LPAREN() == null)
			{
				return _semanticAnalyzer.ClassFieldAccess(expr, memberName);
			}
			else
			{
				var arguments = ParseArgs(context.args());
				return _semanticAnalyzer.CallClassMethod(memberName, expr, arguments);
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

		public override Expression? VisitNumberExpr([NotNull] CEvolParser.NumberExprContext context)
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

		public override Expression? VisitReturnStmt([NotNull] CEvolParser.ReturnStmtContext context)
		{
			var result = Visit(context.expression());
			if (result == null) throw new NotImplementedException();

			_semanticAnalyzer.BuildReturn(result);
			return null;
		}

		public override Expression? VisitIfStmt([NotNull] CEvolParser.IfStmtContext context)
		{
			var ctx = context.ifStatement();

			var condition = Visit(ctx.expression());
			if (condition == null) throw new NotImplementedException();

			_semanticAnalyzer.EnterToIfBlock(condition);
			foreach (var statement in ctx.statement())
			{
				Expression? expr = Visit(statement);
				if (expr != null)
				{
					_semanticAnalyzer.InserToCurrentBlock(expr);
				}
			}

			_semanticAnalyzer.ExitFromBlock();

			return null;
		}

		public override Expression? VisitWhileStmt([NotNull] CEvolParser.WhileStmtContext context)
		{
			var ctx = context.whileStatement();

			var condition = Visit(ctx.expression());
			if (condition == null) throw new NotImplementedException();

			_semanticAnalyzer.EnterToWhileBlock(condition);
			Visit(ctx.block());
			_semanticAnalyzer.ExitFromBlock();

			return null;
		}

		public override Expression? VisitIdExpr([NotNull] CEvolParser.IdExprContext context)
		{
			var varName = context.IDENTIFIER().ToString();
			if (varName == null) throw new NotImplementedException();

			return _semanticAnalyzer.VariableAccess(varName);
		}

		public override Expression? VisitAddSubExpr([NotNull] CEvolParser.AddSubExprContext context)
		{
			var expressions = context.expression();
			if (expressions.Length != 2) throw new NotImplementedException();

			var leftValue = Visit(expressions[0]);
			var rightValue = Visit(expressions[1]);

			if (leftValue == null || rightValue == null) throw new NotImplementedException();

			if (context.MINUS() != null) // это минус
			{
				return _semanticAnalyzer.Sub(leftValue, rightValue);
			}
			else // это плюс
			{
				return _semanticAnalyzer.Sum(leftValue, rightValue);
			}
		}

		public override Expression? VisitParenExpr([NotNull] CEvolParser.ParenExprContext context)
		{
			return Visit(context.expression());
		}

		public override Expression? VisitEqNeqExpr([NotNull] CEvolParser.EqNeqExprContext context)
		{
			(Expression left, Expression right) = ParseBinaryExpression(context.expression());

			CompareOperator compareOperator;
			if (context.NEQ() != null)
				compareOperator = CompareOperator.NotEqual;
			else if (context.EQ() != null)
				compareOperator = CompareOperator.Equal;
			else
				throw new NotImplementedException();

			return _semanticAnalyzer.Compare(left, right, compareOperator);
		}

		public override Expression? VisitLtGtExpr([NotNull] CEvolParser.LtGtExprContext context)
		{
			(Expression left, Expression right) = ParseBinaryExpression(context.expression());

			CompareOperator compareOperator;
			if (context.LT() != null)
				compareOperator = CompareOperator.LessThan;
			else if (context.GT() != null)
				compareOperator = CompareOperator.GreaterThan;
			else
				throw new NotImplementedException();

			return _semanticAnalyzer.Compare(left, right, compareOperator);
		}

		public override Expression? VisitBitAndExpr([NotNull] CEvolParser.BitAndExprContext context)
		{
			(Expression left, Expression right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.BitAnd(left, right);
		}

		public override Expression? VisitBitXorExpr([NotNull] CEvolParser.BitXorExprContext context)
		{
			(Expression left, Expression right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.BitXor(left, right);
		}

		public override Expression? VisitBitOrExpr([NotNull] CEvolParser.BitOrExprContext context)
		{
			(Expression left, Expression right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.BitOr(left, right);
		}

		public override Expression? VisitLogicalAndExpr([NotNull] CEvolParser.LogicalAndExprContext context)
		{
			(Expression left, Expression right) = ParseBinaryExpression(context.expression());
			return _semanticAnalyzer.LogicalAnd(left, right);
		}

		private (Expression left, Expression right) ParseBinaryExpression(CEvolParser.ExpressionContext[]? expressions)
		{
			if (expressions == null || expressions.Length != 2) throw new NotImplementedException();

			var leftValue = Visit(expressions[0]);
			var rightValue = Visit(expressions[1]);

			if (leftValue == null || rightValue == null) throw new NotImplementedException();

			return (leftValue, rightValue);
		}

	}
}
