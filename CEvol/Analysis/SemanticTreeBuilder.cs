using Antlr4.Runtime.Misc;
using CEvol.Analysis.Semantic;
using CEvol.Core;
using CEvol.Core.LogicModels;
using CEvol.Core.LogicModels.Expressions;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using System.Numerics;
using System.Text;
using System.Xml.Linq;
using static CEvol.Core.MemebersModels.Qualifier;

namespace CEvol.Analysis
{
	internal class SemanticTreeBuilder
	{
		public const string COMPILATION_LAYER = "BasicSemanticsValidator";

		private readonly MembersFinder _membersFinder;
		private readonly TypeAnalyzer _typeAnalyzer;
		private readonly ErrorsBag _errorsBag;

		private Stack<CodeBlock> _blocks = new();

		public PositionInSources CurrentPosition { get; set; }

		private class CodeBlock
		{
			// TODO: здесь наверное сделать параметр показывающий текущий тип блока (функция, класс и тп) чтобы понимать можно ли сюда пихать выражение
			public Statement CurentStatement;
			public List<ILogicModel> StatementChilds;
			public Dictionary<string, Expression> Variables = new();
			public IFunctionalBlockStatement? CurrentFunction;
			public ClassStatement? CurrentClass;

			public CodeBlock(Statement curentStatement, List<ILogicModel> statementChilds, Dictionary<string, Expression> variables, IFunctionalBlockStatement? currentFunction, ClassStatement? currentClass)
			{
				CurentStatement = curentStatement;
				StatementChilds = statementChilds;
				Variables = variables;
				CurrentFunction = currentFunction;
				CurrentClass = currentClass;
			}
		}

		public SemanticTreeBuilder(MembersFinder membersFinder, TypeAnalyzer typeAnalyzer, ErrorsBag errorsBag)
		{
			_membersFinder = membersFinder;
			_typeAnalyzer = typeAnalyzer;
			_errorsBag = errorsBag;
		}

		public void EnterToNameSpace(string nameSpace)
		{
			_membersFinder.AddUsing(nameSpace);
			var childs = new List<ILogicModel>();
			var statement = new NamespaceStatement(nameSpace, childs);

			_blocks.Push(new CodeBlock(statement, childs, [], null, null));
		}

		public void Using(string nameSpace)
		{
			if (!_membersFinder.AddUsing(nameSpace))
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "A namespace was not found", CurrentPosition);
			}
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

			FuncDesc? funcDesc = null;
			if (currentClass != null)
			{
				var functions = _membersFinder.FindFunction(currentClass.TypeDesc, funcName);
				if (functions == null)
				{
					_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "A function with that name was not found", CurrentPosition);
					return;
				}

				funcDesc = _typeAnalyzer.FindSuitableFunction(functions, parameters.Select(x => x.Type), out _);
			}
			else
			{
				var functions = _membersFinder.FindFunction(funcName);
				if (functions == null)
				{
					_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "A function with that name was not found", CurrentPosition);
					return;
				}

				funcDesc = _typeAnalyzer.FindSuitableFunction(functions, parameters.Select(x => x.Type), out _);
			}

			if (funcDesc == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Function overload with specified arguments could not be found", CurrentPosition);
				return;
			}

			var childs = new List<ILogicModel>();
			var statement = new FunctionStatement(funcDesc, childs);

			block.StatementChilds.Add(statement);

			var variables = new Dictionary<string, Expression>();

			foreach (var param in parameters)
			{
				variables[param.Name] = new VariableAccessExpression(param.Name, param.Type);
			}

			_blocks.Push(new CodeBlock(statement, childs, variables, statement, currentClass));
		}

		public void EnterToConstructor(List<(TypeSpec Type, string Name)> parameters)
		{
			CodeBlock block = _blocks.Peek();
			var currentClass = block.CurrentClass;

			if (currentClass == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Declaring a constructor outside of a class is not allowed", CurrentPosition);
				return;
			}

			ConstructorDesc? ctorDesc = null;
			var constructors = _membersFinder.FindConstructors(currentClass.TypeDesc);

			ctorDesc = _typeAnalyzer.FindSuitableConstructor(constructors, parameters.Select(x => x.Type));

			if (ctorDesc == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Constructor with specified arguments could not be found", CurrentPosition);
				return;
			}

			var childs = new List<ILogicModel>();
			var statement = new ConstructorStatement(ctorDesc, childs, new TypeSpec(_membersFinder.FindType("void")));

			block.StatementChilds.Add(statement);

			var variables = new Dictionary<string, Expression>();

			foreach (var param in parameters)
			{
				variables[param.Name] = new VariableAccessExpression(param.Name, param.Type);
			}

			_blocks.Push(new CodeBlock(statement, childs, variables, statement, currentClass));
		}

		public void EnterToIfBlock(Expression condition)
		{
			if (condition.ResultTypeSpec.Type != TypeNameToTypeDesc("bool"))
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "The expression passed to 'if' must be of type bool", CurrentPosition);
				return;
			}

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

		public void EnterToWhileBlock(Expression condition)
		{
			if (condition.ResultTypeSpec.Type != TypeNameToTypeDesc("bool"))
				throw new NotImplementedException();

			var childs = new List<ILogicModel>();
			var statement = new WhileStatement(childs, condition);

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
			var block = _blocks.Pop();
			Statement statement = block.CurentStatement;

			if (statement is IFunctionalBlockStatement fnStatement)
			{
				var voidType = _membersFinder.FindType("void");
				if (fnStatement.ReturnType.Type == voidType)
				{
					block.StatementChilds.Add(new ReturnStatement(new SimpleTypeExpression(new TypeSpec(voidType))));
				}
			}

			return statement;
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

			returnResult = AutoDereferenceIfPointer(returnResult);

			if (!_typeAnalyzer.CheckTypeMatching(currentFunction.ReturnType.Type, returnResult.ResultTypeSpec.Type, out bool needCast)
				|| !currentFunction.ReturnType.QualifiersEquals(returnResult.ResultTypeSpec))
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Invalid return type", CurrentPosition);
				return;
			}

			if (needCast && (returnResult.ResultTypeSpec.Type is IntegerTypeDesc))
			{
				returnResult = ImplicitIntExtenssion(returnResult, currentFunction.ReturnType);
			}

			_blocks.Peek().StatementChilds.Add(new ReturnStatement(returnResult));
		}

		public Expression CallFunction(string name, Expression[] args)
		{
			if (CheckStubForError(args)) return new StubForErrorExpression();

			var arguments = args.Select(AutoDereferenceIfPointer).ToArray();

			var functions = _membersFinder.FindFunction(name);
			if (functions == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "A function with that name was not found", CurrentPosition);
				return new StubForErrorExpression();
			}

			FuncDesc? funcDesc = _typeAnalyzer.FindSuitableFunction(functions, arguments.Select(x => x.ResultTypeSpec), out TypeSpec?[] casts);

			if (funcDesc == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Function overload with specified arguments could not be found", CurrentPosition);
				return new StubForErrorExpression();
			}

			for (int i = 0; i < casts.Length; i++)
			{
				TypeSpec? cast = casts[i];
				if (!cast.HasValue || !(arguments[i].ResultTypeSpec.Type is IntegerTypeDesc)) continue;
				arguments[i] = ImplicitIntExtenssion(arguments[i], cast.Value);
			}

			return new CallFunctionExpression(arguments, funcDesc);
		}

		public Expression CallClassMethod(string name, Expression instanceGetting, Expression[] args)
		{
			if (CheckStubForError(args) || CheckStubForError(instanceGetting)) return new StubForErrorExpression();

			var arguments = args.Select(AutoDereferenceIfPointer).ToArray();

			// TODO: проверить instanceGetting на валидность
			var func = _membersFinder.FindFunction(instanceGetting.ResultTypeSpec.Type, name);
			if (func == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "A method with that name was not found", CurrentPosition);
				return new StubForErrorExpression();
			}

			var funcDesc = _typeAnalyzer.FindSuitableFunction(func, arguments.Select(x => x.ResultTypeSpec), out TypeSpec?[] casts);

			if (funcDesc == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Function overload with specified arguments could not be found", CurrentPosition);
				return new StubForErrorExpression();
			}

			Expression[] realArgs = new Expression[arguments.Length + 1];
			realArgs[0] = instanceGetting.ResultTypeSpec.IsRef ? instanceGetting : new GetPointerToVarExpression(instanceGetting);

			for (int i = 1; i <= arguments.Length; i++)
			{
				var sourceArgumentIndex = i - 1;
				var currentCast = casts[sourceArgumentIndex];
				var currentArg = arguments[sourceArgumentIndex];

				if (currentCast.HasValue && (currentArg.ResultTypeSpec.Type is IntegerTypeDesc))
				{
					realArgs[i] = ImplicitIntExtenssion(currentArg, currentCast.Value);
				}
				else
				{
					realArgs[i] = currentArg;
				}
			}

			return new CallFunctionExpression(realArgs, funcDesc);
		}

		public Expression CallHeapConstructor(string typeName, Expression[] args)
		{
			var typeDesc = _membersFinder.FindType(typeName);
			var arguments = args.Select(AutoDereferenceIfPointer).ToArray();

			ConstructorDesc? ctorDesc = null;
			var constructors = _membersFinder.FindConstructors(typeDesc);

			// TODO: сделать автоприведение чисел
			ctorDesc = _typeAnalyzer.FindSuitableConstructor(constructors, arguments.Select(x => x.ResultTypeSpec));

			if (ctorDesc == null)
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Constructor with specified arguments could not be found", CurrentPosition);
				return new StubForErrorExpression();
			}

			var memory = new AllocateHeapMemoryToType(new TypeSpec(typeDesc, [new Qualifier(QKind.Reference)]));
			return new CallConstructorExpression(memory, ctorDesc, arguments);
		}

		public Expression CreateArrayInHeap(string typeName, Expression arraySize)
		{
			// TODO: тут првоерить что arraySize действительно число
			var typeDesc = _membersFinder.FindType(typeName);
			return new AllocateHeapMemoryToType(new TypeSpec(typeDesc, [new Qualifier(QKind.Reference), new Qualifier(QKind.Array)]), arraySize);
		}

		public Expression ClassFieldAccess(Expression instanceGetting, string fieldName)
		{
			if (!instanceGetting.ResultTypeSpec.Type.Variables.TryGetValue(fieldName, out VariableDesc variable))
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "The class field does not exist", CurrentPosition);
				return new StubForErrorExpression();
			}

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
			var expr = new GetPointerToVarExpression(variable);

			return expr;
		}

		public SimpleBinaryOperationExpression Sum(Expression left, Expression right)
		{
			if (!_typeAnalyzer.SoftCheckTypeMatching(left.ResultTypeSpec.Type, right.ResultTypeSpec.Type)) throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			// TODO: нормально здесь определять возвращаемый тип, как и в других банарных операциях (вычитание и тп) ибо мы можем складывать 2 числа разных типов
			return new SimpleBinaryOperationExpression(BinaryOperation.Sum, leftAccessor, rightAccessor, left.ResultTypeSpec);
		}

		public SimpleBinaryOperationExpression Sub(Expression left, Expression right)
		{
			if (!_typeAnalyzer.SoftCheckTypeMatching(left.ResultTypeSpec.Type, right.ResultTypeSpec.Type)) throw new NotImplementedException();

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

			
			if (_typeAnalyzer.CheckTypeMatching(uIntType, left.ResultTypeSpec.Type, out _) 
				&& _typeAnalyzer.CheckTypeMatching(uIntType, right.ResultTypeSpec.Type, out _))
			{
				//сравнение беззнаковых чисел
				return new CompareOperationExpression(compareOperator, false, leftAccessor, rightAccessor, boolTypeSpec);
			}
			else if (_typeAnalyzer.CheckTypeMatching(intType, left.ResultTypeSpec.Type, out _) 
				&& _typeAnalyzer.CheckTypeMatching(intType, right.ResultTypeSpec.Type, out _))
			{
				//сравнение знаковых чисел
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
				(left.ResultTypeSpec.Type != boolType && _typeAnalyzer.SoftCheckTypeMatching(left.ResultTypeSpec.Type, intType)))
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
			if (expr.ResultTypeSpec.Type != boolType && _typeAnalyzer.SoftCheckTypeMatching(expr.ResultTypeSpec.Type, intType))
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

		public Expression CreateString(string str)
		{
			if (str[0] != '"' || str[str.Length - 1] != '"')
			{
				_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "Ivalid string", CurrentPosition);
				return new StubForErrorExpression();
			}

			byte[] strBytes = Encoding.UTF8.GetBytes($"{str.Replace(@"\n", Environment.NewLine)[1..^1]}\0");
			return new GlobalArrayExpression(strBytes, new TypeSpec(TypeNameToTypeDesc("byte"), [new Qualifier(QKind.Reference), new Qualifier(QKind.Array)]));
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
			if (CheckStubForError(varExpr, expr)) return new StubForErrorExpression();

			if (!_typeAnalyzer.CheckTypeMatching(varExpr.ResultTypeSpec.Type, expr.ResultTypeSpec.Type, out bool needCast)) 
				throw new NotImplementedException();

			// TODO: вынести это в какую-нибудь константу или прямо в класс занести, чтобы везде такую хуню не писать
			if (varExpr.ResultTypeSpec.IsRef && !expr.ResultTypeSpec.IsRef)
			{
				varExpr = new PointerDereferenceExpression(varExpr);
			}
			else if (varExpr.ResultTypeSpec.IsRef && expr.ResultTypeSpec.IsRef)
			{
				if (!assignQualifier.HasValue)
				{
					throw new NotImplementedException(); // TODO: разрешить это в usafe контексте

					expr = new PointerDereferenceExpression(expr);
					varExpr = new PointerDereferenceExpression(varExpr);
				}
				else
				{
					if (assignQualifier.Value.Kind != QKind.Reference) throw new NotImplementedException();
				}
			}

			if (needCast && (expr.ResultTypeSpec.Type is IntegerTypeDesc))
			{
				expr = ImplicitIntExtenssion(expr, varExpr.ResultTypeSpec);
			}

			return new SimpleBinaryOperationExpression(BinaryOperation.Assing, varExpr, expr, varExpr.ResultTypeSpec);
		}

		public Expression AutoDereferenceIfPointer(Expression expr)
		{
			if (CheckStubForError(expr)) return new StubForErrorExpression();

			if (!expr.ResultTypeSpec.IsRef || expr is GetPointerToVarExpression || expr is DoNotAutoDereferenceIfPointerExpression) return expr;
			return new PointerDereferenceExpression(expr);
		}

		public Expression VariableAccess(string name)
		{
			CodeBlock block = _blocks.Peek();
			if (!block.Variables.TryGetValue(name, out var value))
			{
				var currentClass = block.CurrentClass?.TypeDesc;
				if (currentClass == null)
				{
					_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "No variable with that name was found", CurrentPosition);
					return new StubForErrorExpression();
				}

				if (!currentClass.Variables.TryGetValue(name, out var field))
				{
					_errorsBag.AddError(COMPILATION_LAYER, "DOLBAEB", "No variable or field with that name was found", CurrentPosition);
					return new StubForErrorExpression();
				}

				TypeSpec fieldDeclaring = field.Declaring;

				var thisGetting = new AppealToThisExpression(currentClass);
				return new StructureFieldAccessExpression(field.Order, true, thisGetting, fieldDeclaring);
			}

			return value;
		}

		public Expression SetRefQualifier(Expression expr)
		{
			if (CheckStubForError(expr)) return new StubForErrorExpression();
			return new DoNotAutoDereferenceIfPointerExpression(expr);
		}

		/// <summary>
		/// Создает <see cref="Expression"/> для каста целого числа к переданному <see cref="TypeDesc"/>.
		/// Подразумивается что <paramref name="expr"/> является целым числом, то есть может быть раширен до long, а
		/// <paramref name="resultType"/> либо целоче число, либо число с плвающей точкой.
		/// </summary>
		/// <param name="expr">Выражение, которое нужно привести к типу <paramref name="resultType"/>. 
		/// Должно иметь тип либо целого числа, либо числа с плавающей точкой</param>
		/// <param name="resultType"></param>
		/// <returns></returns>
		private Expression ImplicitIntExtenssion(Expression expr, TypeSpec resultType)
		{
			var ulongType = _membersFinder.FindType("ulong");
			var doubleType = _membersFinder.FindType("double");

			bool isSigned = !_typeAnalyzer.CheckTypeMatching(expr.ResultTypeSpec.Type, ulongType, out _);
			bool resultTypeIsFloat = _typeAnalyzer.CheckTypeMatching(resultType.Type, doubleType, out _);

			if (resultTypeIsFloat)
			{
				return new IntToFloatExtensionExpression(expr, isSigned, resultType);
			}
			else
			{
				return new IntToIntExtensionExpression(expr, isSigned, resultType);
			}
		}

		private TypeDesc TypeNameToTypeDesc(string typeName)
		{
			return _membersFinder.FindType(typeName);
		}

		private bool CheckStubForError(params Expression[] expressions)
		{
			return expressions.Any(x => x is StubForErrorExpression);
		}

	}
}
