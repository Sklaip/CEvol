using CEvol.Analysis.Semantic;
using CEvol.Core;
using CEvol.Core.LogicModels;
using CEvol.Core.LogicModels.Expressions;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using System.Numerics;
using static CEvol.Core.MemebersModels.Qualifier;

namespace CEvol.Analysis
{
	internal class SemanticTreeBuilder
	{
		private readonly MembersFinder _membersFinder;
		private readonly TypeAnalyzer _typeAnalyzer;

		private Stack<CodeBlock> _blocks = new();

		private class CodeBlock
		{
			// TODO: здесь наверное сделать параметр показывающий текущий тип блока (функция, класс и тп) чтобы понимать можно ли сюда пихать выражение
			public Statement CurentStatement;
			public List<ILogicModel> StatementChilds;
			public Dictionary<string, Expression> Variables = new();
			public FunctionStatement? CurrentFunction;
			public ClassStatement? CurrentClass;

			public CodeBlock(Statement curentStatement, List<ILogicModel> statementChilds, Dictionary<string, Expression> variables, FunctionStatement? currentFunction, ClassStatement? currentClass)
			{
				CurentStatement = curentStatement;
				StatementChilds = statementChilds;
				Variables = variables;
				CurrentFunction = currentFunction;
				CurrentClass = currentClass;
			}
		}

		public SemanticTreeBuilder(MembersFinder membersFinder, TypeAnalyzer typeAnalyzer)
		{
			_membersFinder = membersFinder;
			_typeAnalyzer = typeAnalyzer;
		}

		public void EnterToNameSpace(string nameSpace)
		{
			var childs = new List<ILogicModel>();
			var statement = new NamespaceStatement(nameSpace, childs);

			_blocks.Push(new CodeBlock(statement, childs, [], null, null));
		}

		public void EnterToClass(string className)
		{
			var typeDesc = _membersFinder.FindType(className);
			var childs = new List<ILogicModel>();
			var statement = new ClassStatement(typeDesc, childs);

			CodeBlock block = _blocks.Peek();
			block.StatementChilds.Add(statement);

			_blocks.Push(new CodeBlock(statement, childs, [], null, statement));
		}

		public void EnterToFunction(string funcName, List<(TypeSpec Type, string Name)> parameters)
		{
			CodeBlock block = _blocks.Peek();
			var currentClass = block.CurrentClass;

			FuncDesc funcDesc;
			if (currentClass != null)
			{
				funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(currentClass.TypeDesc, funcName), parameters.Select(x => x.Type));
			}
			else
			{
				funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(funcName), parameters.Select(x => x.Type));
			}

			var childs = new List<ILogicModel>();
			var statement = new FunctionStatement(funcDesc, childs);

			block.StatementChilds.Add(statement);

			var variables = new Dictionary<string, Expression>();

			foreach(var param in parameters)
			{
				variables[param.Name] = new VariableAccessExpression(param.Name, param.Type);
			}

			_blocks.Push(new CodeBlock(statement, childs, variables, statement, currentClass));
		}

		public void EnterToIfBlock(Expression condition)
		{
			if (condition.ResultTypeSpec.Type != TypeNameToTypeDesc("bool"))
				throw new NotImplementedException();

			var childs = new List<ILogicModel>();
			var statement = new IfStatement(childs, condition, null, null);

			CodeBlock block = _blocks.Peek();
			block.StatementChilds.Add(statement);
			var currentFunction = block.CurrentFunction;
			if (currentFunction == null) throw new NotImplementedException();
			var currentClass = block.CurrentClass;
			var variables = new Dictionary<string, Expression>(block.Variables);

			_blocks.Push(new CodeBlock(statement, childs, variables, currentFunction, currentClass));
		}

		public Statement ExitFromBlock()
		{
			return _blocks.Pop().CurentStatement;
		}

		public void InserToCurrentBlock(Expression expression)
		{
			_blocks.Peek().StatementChilds.Add(expression);
		}

		public void BuildReturn(Expression returnResult)
		{
			CodeBlock block = _blocks.Peek();
			var currentFunction = block.CurrentFunction;
			if (currentFunction == null) throw new NotImplementedException();

			if (!_typeAnalyzer.StrictCheckTypeMatching(currentFunction.FunctionSignature.ReturnType.Value.Type, returnResult.ResultTypeSpec.Type))
				throw new NotImplementedException();
			// TODO: так же проверить AdditionalTypes

			_blocks.Peek().StatementChilds.Add(new ReturnStatement(returnResult));
		}

		public Expression CallFunction(string name, Expression[] arguments)
		{
			FuncDesc funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(name), arguments.Select(x => x.ResultTypeSpec));
			return new CallFunctionExpression(arguments, funcDesc);
		}

		public Expression CallClassMethod(string name, Expression instanceGetting, Expression[] arguments)
		{
			// TODO: проверить instanceGetting на валидность
			var func = _membersFinder.FindFunction(instanceGetting.ResultTypeSpec.Type, name);
			var funcDesc = _typeAnalyzer.FindSuitableFunction(func, arguments.Select(x => x.ResultTypeSpec));

			Expression[] realArgs = new Expression[arguments.Length + 1];
			realArgs[0] = instanceGetting;

			for (int i = 1; i <= arguments.Length; i++)
			{
				realArgs[i] = arguments[i - 1];
			}

			return new CallFunctionExpression(realArgs, funcDesc);
		}

		public Expression CallHeapConstructor(string typeName, Expression[] arguments)
		{
			var typeDesc = _membersFinder.FindType(typeName);
			return new AllocateHeapMemoryToType(new TypeSpec(typeDesc, [new Qualifier(QKind.Reference)]));
		}

		//public Expression CallStackConstructor(string typeName, Expression[] arguments)
		//{
		//	var typeDesc = _membersFinder.FindType(typeName);
		//	return new Expr(new TypeSpec(typeDesc), _codeGenerator.AllocateHeapMemory(typeDesc.TypeRef));
		//}

		public Expression CreateArrayInHeap(string typeName, Expression arraySize)
		{
			// TODO: тут првоерить что arraySize действительно число
			var typeDesc = _membersFinder.FindType(typeName);
			return new AllocateHeapMemoryToType(new TypeSpec(typeDesc, [new Qualifier(QKind.Reference), new Qualifier(QKind.Array)]), arraySize);
		}

		public Expression ClassFieldAccess(Expression instanceGetting, string fieldName)
		{
			if (!instanceGetting.ResultTypeSpec.Type.Variables.TryGetValue(fieldName, out VariableDesc variable))
				throw new NotImplementedException();

			return new StructureFieldAccessExpression(variable.Order, instanceGetting.ResultTypeSpec.IsRef, instanceGetting, variable.Declaring);
		}

		public ArrayCellAccessExpression ArrayCellAccess(Expression arrayGetting, Expression indexGetting)
		{
			// TODO: сделать проверки на типы
			return new ArrayCellAccessExpression(arrayGetting, indexGetting);
		}

		public GetPointerToVarExpression GetPointerToVar(Expression variable)
		{
			// TODO: сделать проверку на что что это реально переменная, а не какая-нибудь хуета, чтобы нельзя было написать ref 1
			return new GetPointerToVarExpression(variable);
		}

		public SimpleBinaryOperationExpression Sum(Expression left, Expression right)
		{
			if (!_typeAnalyzer.CheckTypeMatching(left.ResultTypeSpec.Type, right.ResultTypeSpec.Type)) throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			// TODO: нормально здесь определять возвращаемый тип, как и в других банарных операциях (вычитание и тп) ибо мы можем складывать 2 числа разных типов
			return new SimpleBinaryOperationExpression(BinaryOperation.Sum, leftAccessor, rightAccessor, left.ResultTypeSpec);
		}

		public SimpleBinaryOperationExpression Sub(Expression left, Expression right)
		{
			if (!_typeAnalyzer.CheckTypeMatching(left.ResultTypeSpec.Type, right.ResultTypeSpec.Type)) throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			return new SimpleBinaryOperationExpression(BinaryOperation.Sub, leftAccessor, rightAccessor, left.ResultTypeSpec);
		}

		public CompareOperationExpression Compare(Expression left, Expression right, CompareOperator compareOperator)
		{
			var uIntType = _membersFinder.FindType("uint");
			var intType = _membersFinder.FindType("int");

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			var boolTypeSpec = new TypeSpec(TypeNameToTypeDesc("bool"));

			//сравнение беззнаковых чисел
			if (_typeAnalyzer.StrictCheckTypeMatching(uIntType, left.ResultTypeSpec.Type) && _typeAnalyzer.StrictCheckTypeMatching(uIntType, right.ResultTypeSpec.Type))
			{
				return new CompareOperationExpression(compareOperator, false, leftAccessor, rightAccessor, boolTypeSpec);
			}
			else if (_typeAnalyzer.StrictCheckTypeMatching(intType, left.ResultTypeSpec.Type) && _typeAnalyzer.StrictCheckTypeMatching(intType, right.ResultTypeSpec.Type))
			{
				return new CompareOperationExpression(compareOperator, true, leftAccessor, rightAccessor, boolTypeSpec);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		public SimpleBinaryOperationExpression LogicalAnd(Expression left, Expression right)
		{
			var boolType = _membersFinder.FindType("bool");
			if (left.ResultTypeSpec.Type != boolType || right.ResultTypeSpec.Type != boolType)
				throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			return new SimpleBinaryOperationExpression(BinaryOperation.LogicalAnd, leftAccessor, rightAccessor, new TypeSpec(boolType));
		}

		//public Expr LogicalOr(Expr left, Expr right)
		//{
		//	var boolType = _membersFinder.FindType("bool");
		//	if (left.Declaring.Type != boolType || right.Declaring.Type != boolType)
		//		throw new NotImplementedException();

		//	var leftAccessor = AutoDereferenceIfPointer(left);
		//	var rightAccessor = AutoDereferenceIfPointer(right);

		//	throw new NotImplementedException();
		//}

		private SimpleBinaryOperationExpression BitOperationPrepeare(BinaryOperation operation, Expression left, Expression right)
		{
			var boolType = _membersFinder.FindType("bool");
			var intType = _membersFinder.FindType("int");

			if (left.ResultTypeSpec.Type != right.ResultTypeSpec.Type ||
				(left.ResultTypeSpec.Type != boolType && _typeAnalyzer.CheckTypeMatching(left.ResultTypeSpec.Type, intType)))
				throw new NotImplementedException();

			var leftExpr = AutoDereferenceIfPointer(left);
			var rightExpr = AutoDereferenceIfPointer(right);

			return new SimpleBinaryOperationExpression(operation, leftExpr, rightExpr, new TypeSpec(left.ResultTypeSpec.Type));
		}

		public SimpleBinaryOperationExpression BitAnd(Expression leftExpr, Expression rightExpr)
		{
			return BitOperationPrepeare(BinaryOperation.BitAnd, leftExpr, rightExpr);
		}

		public SimpleBinaryOperationExpression BitXor(Expression leftExpr, Expression rightExpr)
		{
			return BitOperationPrepeare(BinaryOperation.BitXor, leftExpr, rightExpr);
		}

		public SimpleBinaryOperationExpression BitOr(Expression leftExpr, Expression rightExpr)
		{
			return BitOperationPrepeare(BinaryOperation.BitOr, leftExpr, rightExpr);
		}

		public NotExpression BitNot(Expression expr)
		{
			var boolType = _membersFinder.FindType("bool");
			var intType = _membersFinder.FindType("int");
			if (expr.ResultTypeSpec.Type != boolType && _typeAnalyzer.CheckTypeMatching(expr.ResultTypeSpec.Type, intType))
				throw new NotImplementedException();

			var accessor = AutoDereferenceIfPointer(expr);

			return new NotExpression(expr);
		}

		public Expression CreateInt(BigInteger num)
		{
			return new NumConstExpression(new TypeSpec(TypeNameToTypeDesc("int")), BaseTypes.Int, num);
		}

		public Expression CreateShort(BigInteger num)
		{
			return new NumConstExpression(new TypeSpec(TypeNameToTypeDesc("short")), BaseTypes.Short, num);
		}

		public Expression CreateByte(BigInteger num)
		{
			return new NumConstExpression(new TypeSpec(TypeNameToTypeDesc("byte")), BaseTypes.Byte, num);
		}

		public Expression CreateLocalVariable(string name, TypeSpec declaring)
		{
			CodeBlock block = _blocks.Peek();
			if (block.Variables.ContainsKey(name)) throw new NotImplementedException();

			var varExpr = new VariableCreatingExpression(name, declaring);
			block.Variables[name] = new VariableAccessExpression(name, declaring);

			return varExpr;
		}

		public Expression VariableAssing(Expression varExpr, Expression expr, Qualifier? assignQualifier)
		{
			//if (varAccess is not VariableAccessExpression varExpr) throw new NotImplementedException();

			if (!_typeAnalyzer.CheckTypeMatching(varExpr.ResultTypeSpec.Type, expr.ResultTypeSpec.Type)) throw new NotImplementedException();

			// TODO: вынести это в какую-нибудь константу или прямо в класс занести, чтобы везде такую хуню не писать
			if (varExpr.ResultTypeSpec.IsRef && !expr.ResultTypeSpec.IsRef)
			{
				var realVar = new PointerDereferenceExpression(varExpr);

				return new SimpleBinaryOperationExpression(BinaryOperation.Assing, realVar,
					GetAccessorOfRequiredType(expr, varExpr.ResultTypeSpec), realVar.ResultTypeSpec);
			}
			else if (varExpr.ResultTypeSpec.IsRef && expr.ResultTypeSpec.IsRef)
			{
				if (!assignQualifier.HasValue)
				{
					throw new NotImplementedException(); // TODO: разрешить это в usafe контексте

					var realValue = new PointerDereferenceExpression(expr);
					var realVar = new PointerDereferenceExpression(varExpr);

					return new SimpleBinaryOperationExpression(BinaryOperation.Assing, realVar,
						GetAccessorOfRequiredType(realValue, expr.ResultTypeSpec, varExpr.ResultTypeSpec), realVar.ResultTypeSpec);
				}
				else
				{
					if (assignQualifier.Value.Kind != QKind.Reference) throw new NotImplementedException();

					return new SimpleBinaryOperationExpression(BinaryOperation.Assing, varExpr,
						GetAccessorOfRequiredType(expr, varExpr.ResultTypeSpec), varExpr.ResultTypeSpec);

				}
			}

			return new SimpleBinaryOperationExpression(BinaryOperation.Assing, varExpr,
						GetAccessorOfRequiredType(expr, varExpr.ResultTypeSpec), varExpr.ResultTypeSpec);
		}

		private Expression GetAccessorOfRequiredType(Expression expr, TypeSpec valueForOrientation)
		{
			return GetAccessorOfRequiredType(expr, expr.ResultTypeSpec, valueForOrientation);
		}

		private Expression GetAccessorOfRequiredType(Expression expr, TypeSpec exprDeclaring, TypeSpec valueForOrientation)
		{
			if (exprDeclaring.QualifiersExists || !exprDeclaring.Type.IsBaseType) return expr;

			//проверяем что exprDeclaring имеет числовой тип
			var intType = _membersFinder.FindType("int");
			if (_typeAnalyzer.StrictCheckTypeMatching(intType, exprDeclaring.Type))
			{
				if (exprDeclaring.Type == intType) return expr;
				return new NumTruncExpression(expr, valueForOrientation);
			}

			return expr;
		}

		public Expression AutoDereferenceIfPointer(Expression expr)
		{
			if (!expr.ResultTypeSpec.IsRef) return expr;
			return new PointerDereferenceExpression(expr);
		}

		public Expression VariableAccess(string name)
		{
			CodeBlock block = _blocks.Peek();
			if (!block.Variables.TryGetValue(name, out var value))
			{
				var currentClass = block.CurrentClass?.TypeDesc;
				if (currentClass == null) throw new NotImplementedException();
				if (!currentClass.Variables.TryGetValue(name, out var field)) throw new NotImplementedException();

				TypeSpec fieldDeclaring = field.Declaring;

				var thisGetting = new AppealToThisExpression(currentClass);
				return new StructureFieldAccessExpression(field.Order, true, thisGetting, fieldDeclaring);
			}

			return value;
		}

		private BaseTypes TypeToBaseType(TypeDesc desc)
		{
			switch (desc.Name)
			{
				case "int": return BaseTypes.Int;
				case "byte": return BaseTypes.Byte;
				case "bool": return BaseTypes.Bool;
				default: throw new NotImplementedException();
			}
		}

		private TypeDesc TypeNameToTypeDesc(string typeName)
		{
			return _membersFinder.FindType(typeName);
		}

	}
}
