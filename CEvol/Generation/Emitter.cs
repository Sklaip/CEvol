using Antlr4.Runtime.Atn;
using CEvol.Analysis;
using CEvol.Analysis.Semantic;
using CEvol.Core;
using CEvol.Core.LogicModels.Expressions;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using CEvol.Generation.Accessors;

namespace CEvol.Generation
{
	internal class Emitter
	{
		private CodeGenerator _codeGenerator = null!;

		public Emitter(CodeGenerator codeGenerator)
		{
			_codeGenerator = codeGenerator;
		}

		private TypeDesc? _currentClass = null;
		private Dictionary<string, IValueAccessor> _functionLocalVariables = null!;
		private IValueAccessor? _currentFunctionParentClassRef = null;

		public CodeGenerator CodeGenerator { get => _codeGenerator; }

		public void Build(NamespaceStatement namespaceStatement)
		{
			foreach (var child in namespaceStatement.Childs)
			{
				if (!(child is Statement stm)) throw new NotImplementedException();
				HandleStatement(stm);
			}
		}

		private void HandleStatement(Statement statement)
		{
			switch (statement)
			{
				case ClassStatement classStatement:
					HandleClass(classStatement);
					break;
				case FunctionStatement functionStatement:
					HandleFunctionalBlock(functionStatement);
					break;
				case ConstructorStatement contructorStatetment:
					HandleFunctionalBlock(contructorStatetment);
					break;
				case IfStatement ifStatement:
					HandleIfStatement(ifStatement);
					break;
				case ReturnStatement returnStatement:
					HandleReturnStatement(returnStatement);
					break;
				case WhileStatement whileStatement:
					HandleWhileStatement(whileStatement);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		private void HandleClass(ClassStatement statement)
		{
			_currentClass = statement.TypeDesc;
			foreach (var child in statement.Childs)
			{
				if (!(child is Statement stm)) throw new NotImplementedException();
				HandleStatement(stm);
			}
			_currentClass = null;
		}

		private void HandleFunctionalBlock<TBlock>(TBlock statement) where TBlock : Statement, IFunctionalBlockStatement
		{
			_functionLocalVariables = new();

			var argumentsTypes = new List<TypeRef>();
			if (_currentClass != null) argumentsTypes.Add(_codeGenerator.PointerType);
			argumentsTypes.AddRange(statement.Arguments.Select(x => GetActualTypeRef(x.Declaring)));

			FuncRefData refData = statement.RefData;
			string name = statement.Name;
			TypeRef returnType = GetActualTypeRef(statement.ReturnType);

			var funcData = _codeGenerator.StartFunctionBodyFill(refData, name, returnType, argumentsTypes);

			if (_currentClass != null)
			{
				_currentFunctionParentClassRef = funcData.Arguments[0];

				for (int i = 1; i < funcData.Arguments.Length; i++)
				{
					IValueAccessor? accessor = funcData.Arguments[i];
					var arg = statement.Arguments[i - 1];
					_functionLocalVariables.Add(arg.Name, accessor);
				}
			}
			else
			{
				for (int i = 0; i < funcData.Arguments.Length; i++)
				{
					IValueAccessor? accessor = funcData.Arguments[i];
					var arg = statement.Arguments[i];
					_functionLocalVariables.Add(arg.Name, accessor);
				}
			}

			foreach (var child in statement.Childs)
			{
				if ((child is Statement stm)) HandleStatement(stm);
				else if (child is Expression expr) HandleExpression(expr);
				else throw new NotImplementedException();
			}

			_functionLocalVariables = null!;

			_codeGenerator.StopFunctionBodyFill();
		}

		private void HandleIfStatement(IfStatement statement)
		{
			var condition = HandleExpression(statement.Condition);
			_codeGenerator.CreateIfBlock(condition);

			foreach (var child in statement.Childs)
			{
				if ((child is Statement stm)) HandleStatement(stm);
				else if (child is Expression expr) HandleExpression(expr);
				else throw new NotImplementedException();
			}

			_codeGenerator.EndIfBlock();
		}

		private void HandleReturnStatement(ReturnStatement statement)
		{
			Expression resExpr = statement.Value;
			if(resExpr.ResultTypeSpec.Type.Name != "void")
			{
				_codeGenerator.AddReturn(HandleExpression(resExpr));
			}
			else
			{
				_codeGenerator.AddReturn(null);
			}			
		}

		private void HandleWhileStatement(WhileStatement statement)
		{
			var condition = HandleExpression(statement.Condition);
			_codeGenerator.CreateWhileBlock(condition);

			foreach (var child in statement.Childs)
			{
				if ((child is Statement stm)) HandleStatement(stm);
				else if (child is Expression expr) HandleExpression(expr);
				else throw new NotImplementedException();
			}

			_codeGenerator.EndWhileBlock();
		}

		private IValueAccessor HandleExpression(Expression expression)
		{
			switch (expression)
			{
				case NumConstExpression numConstExpression:
					return CreateNum(numConstExpression);
				case VariableCreatingExpression varCreate:
					return CreateVar(varCreate);
				case VariableAccessExpression varAccess:
					return VarAccess(varAccess);
				case AllocateHeapMemoryToType allocateHeapMemoryToType:
					return AllocateHeapMemory(allocateHeapMemoryToType);
				case AppealToThisExpression appealToThis:
					return AppealToThis(appealToThis);
				case ArrayCellAccessExpression arrayCellAccess:
					return ArrayCellAccess(arrayCellAccess);
				case SimpleBinaryOperationExpression simpleBinaryOperation:
					return SimpleBinaryOperationHandle(simpleBinaryOperation);
				case CallFunctionExpression callFunction:
					return CallFunction(callFunction);
				case CompareOperationExpression compareOperation:
					return CompareOperationHandle(compareOperation);
				case GetPointerToVarExpression getPointerToVar:
					return GetPointerToVar(getPointerToVar);
				case NotExpression notExpression:
					return Not(notExpression);
				case NumTruncExpression numTrunc:
					return NumTrunc(numTrunc);
				case PointerDereferenceExpression pointerDereference:
					return PointerDereference(pointerDereference);
				case StructureFieldAccessExpression structureFieldAccess:
					return StructureFiledAccess(structureFieldAccess);
				case DoNotAutoDereferenceIfPointerExpression doNotAutoDereferenceIfPointerExpression:
					return HandleExpression(doNotAutoDereferenceIfPointerExpression.Expression);
				case CallConstructorExpression callConstructorExpression:
					return CallConstructor(callConstructorExpression);
				default:
					throw new NotImplementedException();
			}

		}

		private IValueAccessor CreateNum(NumConstExpression expr)
		{
			var num = (ulong)(expr.Value & ulong.MaxValue);
			return _codeGenerator.CreateIntConst(num, BaseTypes.Int);
		}

		private IValueAccessor CreateVar(VariableCreatingExpression expr)
		{
			var varAccessor = _codeGenerator.CreateVar(expr.Name, GetActualTypeRef(expr.ResultTypeSpec));
			_functionLocalVariables[expr.Name] = varAccessor;

			return varAccessor;
		}

		private IValueAccessor VarAccess(VariableAccessExpression expr)
		{
			return _functionLocalVariables[expr.Name];
		}

		private IValueAccessor AppealToThis(AppealToThisExpression expr)
		{
			if (_currentClass == null || _currentFunctionParentClassRef == null
				|| expr.ResultTypeSpec.Type != _currentClass) throw new NotImplementedException();

			return _currentFunctionParentClassRef;
		}

		private IValueAccessor AllocateHeapMemory(AllocateHeapMemoryToType expr)
		{
			if (expr.Multiper == null)
			{
				return _codeGenerator.AllocateHeapMemory(expr.ResultTypeSpec.Type.TypeRef);
			}
			else
			{
				var size = HandleExpression(expr.Multiper);
				return _codeGenerator.AllocateHeapMemory(expr.ResultTypeSpec.Type.TypeRef, size);
			}

		}

		private IValueAccessor ArrayCellAccess(ArrayCellAccessExpression expr)
		{
			var arrayGetting = HandleExpression(expr.ArrayGetting);
			var indexGetting = HandleExpression(expr.IndexGetting);
			return _codeGenerator.GetArrayCell(arrayGetting, indexGetting, expr.ResultTypeSpec.Type.TypeRef);
		}

		private IValueAccessor SimpleBinaryOperationHandle(SimpleBinaryOperationExpression expr)
		{
			var left = HandleExpression(expr.LeftExpression);
			var right = HandleExpression(expr.RightExpression);

			switch (expr.OperationType)
			{
				case BinaryOperation.Sum:
					return _codeGenerator.Sum(left, right);
				case BinaryOperation.Sub:
					return _codeGenerator.Sub(left, right);
				case BinaryOperation.Assing:
					_codeGenerator.Assign(left, right);
					return right;
				case BinaryOperation.BitAnd:
					return _codeGenerator.BitAnd(left, right);
				case BinaryOperation.BitOr:
					return _codeGenerator.BitOr(left, right);
				case BinaryOperation.BitXor:
					return _codeGenerator.BitXor(left, right);
				case BinaryOperation.LogicalAnd:
					return _codeGenerator.LogicalAnd(left, right);
				default:
					throw new NotImplementedException();
			}
		}

		private IValueAccessor CallFunction(CallFunctionExpression expr)
		{
			var accessors = expr.Arguments.Select(HandleExpression).ToArray();
			return _codeGenerator.FunctionCall(expr.Function.RefData, accessors);
		}

		private IValueAccessor CompareOperationHandle(CompareOperationExpression expr)
		{
			var left = HandleExpression(expr.LeftExpression);
			var right = HandleExpression(expr.RightExpression);

			return _codeGenerator.Compare(left, right, expr.IsSigned, expr.CompareOperator);
		}

		private IValueAccessor GetPointerToVar(GetPointerToVarExpression expr)
		{
			return _codeGenerator.GetPointerToVar(HandleExpression(expr.Variable));
		}

		private IValueAccessor Not(NotExpression expr)
		{
			return _codeGenerator.BitNot(HandleExpression(expr));
		}

		private IValueAccessor NumTrunc(NumTruncExpression expr)
		{
			return _codeGenerator.NumTrunc(HandleExpression(expr.NumGetting), TypeToBaseType(expr.ResultTypeSpec.Type));
		}

		private IValueAccessor PointerDereference(PointerDereferenceExpression expr)
		{
			return _codeGenerator.PointerDereference(HandleExpression(expr.Target), expr.ResultTypeSpec.Type.TypeRef);
		}

		private IValueAccessor StructureFiledAccess(StructureFieldAccessExpression expr)
		{
			var instanceGetting = HandleExpression(expr.StructureGetting);
			var structureType = expr.StructureGetting.ResultTypeSpec.Type.TypeRef;
			var typeRef = GetActualTypeRef(expr.ResultTypeSpec);

			if (expr.ByRef)
			{
				return _codeGenerator.GetHeapStructureField(instanceGetting, structureType, typeRef, expr.FiledNum);
			}
			else
			{
				return _codeGenerator.GetStackStructureField(instanceGetting, structureType, typeRef, expr.FiledNum);
			}
		}

		private IValueAccessor CallConstructor(CallConstructorExpression expr)
		{
			var memoryGetting = HandleExpression(expr.MemoryGetting);

			IValueAccessor[] accessors = [memoryGetting, ..expr.Arguments.Select(HandleExpression)];
			_codeGenerator.FunctionCall(expr.Constructor.RefData, accessors);

			return memoryGetting;
		}

		private TypeRef GetActualTypeRef(TypeSpec varDeclaring)
		{
			if (varDeclaring.QualifiersExists) return _codeGenerator.PointerType;
			return varDeclaring.Type.TypeRef;
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
	}
}
