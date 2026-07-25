using CEvol.Analysis.Members;
using CEvol.Analysis.Members.Models;
using CEvol.Generation;
using CEvol.Parsing;
using static CEvol.Analysis.Members.Models.Qualifier;

namespace CEvol.Analysis.Semantic
{
	internal class SemanticAnalyzer
	{
		private readonly MembersFinder _membersFinder;
		private readonly CodeGenerator _codeGenerator;
		private readonly TypeAnalyzer _typeAnalyzer;

		private TypeDesc? _currentClass = null;

		private TypeSpec? _currentFunctionReturnType = null;
		private Dictionary<string, Expr> _functionLocalVariables = null!;
		private IValueAccessor? _currentFunctionParentClassRef = null;

		public readonly struct Expr(TypeSpec Type, IValueAccessor Accessor)
		{
			public readonly TypeSpec Declaring = Type;
			public readonly IValueAccessor Accessor = Accessor;
		}

		public SemanticAnalyzer(MembersFinder membersFinder, CodeGenerator codeGenerator, TypeAnalyzer typeAnalyzer)
		{
			_membersFinder = membersFinder;
			_codeGenerator = codeGenerator;
			_typeAnalyzer = typeAnalyzer;
		}

		public void StartClassFill(TypeDesc cls)
		{
			_currentClass = cls;
		}

		public void StopClassFill()
		{
			_currentClass = null;
		}

		public FuncAccessData StartFunctionBodyFill(FuncDesc desc, string name, TypeSpec returnType, List<(TypeSpec Type, string Name)> arguments)
		{
			var argumentsTypes = new List<TypeRef>();
			if (_currentClass != null)
			{
				argumentsTypes.Add(_codeGenerator.PointerType);
			}

			argumentsTypes.AddRange(arguments.Select(x => GetActualTypeRef(x.Type)));

			// TODO: сделать чтобы если это структура, то она передавалась по ссылке с атрибутом byval
			FuncAccessData funcData = _codeGenerator.StartFunctionBodyFill(desc.RefData, name, GetActualTypeRef(returnType), argumentsTypes);
			// TODO: учитывать что функция может быть внутри класса

			_functionLocalVariables = new();
			_currentFunctionReturnType = returnType;

			if (_currentClass != null)
			{
				_currentFunctionParentClassRef = funcData.Arguments[0];

				for (int i = 1; i < funcData.Arguments.Length; i++)
				{
					IValueAccessor? accessor = funcData.Arguments[i];
					_functionLocalVariables.Add(arguments[i - 1].Name, new Expr(arguments[i - 1].Type, accessor));
				}

				return funcData;
			}

			for (int i = 0; i < funcData.Arguments.Length; i++)
			{
				IValueAccessor? accessor = funcData.Arguments[i];
				_functionLocalVariables.Add(arguments[i].Name, new Expr(arguments[i].Type, accessor));
			}

			return funcData;
		}

		public void StopFuncCreating()
		{
			_codeGenerator.StopFunctionBodyFill();
			_functionLocalVariables = null!;
			_currentFunctionReturnType = null;
			_currentFunctionParentClassRef = null;
		}

		public void AddReturn(Expr returnResult)
		{
			if (!_typeAnalyzer.StrictCheckTypeMatching(_currentFunctionReturnType.Value.Type, returnResult.Declaring.Type)) throw new NotImplementedException();
			// TODO: так же проверить AdditionalTypes
			_codeGenerator.AddReturn(returnResult.Accessor);
		}

		public void StartIfBlock(Expr condition)
		{
			if (condition.Declaring.Type != TypeNameToTypeDesc("bool"))
				throw new NotImplementedException();

			_codeGenerator.CreateIfBlock(condition.Accessor);
		}

		public void EndIfBlock()
		{
			_codeGenerator.EndIfBlock();
		}

		public Expr CallFunction(string name, Expr[] arguments)
		{
			FuncDesc funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(name), arguments.Select(x => x.Declaring));

			var functionResult = _codeGenerator.FunctionCall(funcDesc.RefData, arguments.Select(x => x.Accessor).ToArray());

			return new Expr(funcDesc.ReturnType.Value, functionResult);
		}

		public Expr CallClassMethod(string name, Expr instanceGetting, Expr[] arguments)
		{
			// TODO: проверить instanceGetting на валидность
			var funcDesc = _typeAnalyzer.FindSuitableFunction(_membersFinder.FindFunction(instanceGetting.Declaring.Type, name), arguments.Select(x => x.Declaring));

			IValueAccessor[] accessors = new IValueAccessor[arguments.Length + 1];
			accessors[0] = instanceGetting.Accessor;

			for (int i = 1; i <= arguments.Length; i++)
			{
				Expr arg = arguments[i - 1];
				accessors[i] = arg.Accessor;
			}

			var functionResult = _codeGenerator.FunctionCall(funcDesc.RefData, accessors);

			return new Expr(funcDesc.ReturnType.Value, functionResult);
		}

		public Expr CallHeapConstructor(string typeName, Expr[] arguments)
		{
			var typeDesc = _membersFinder.FindType(typeName);
			return new Expr(new TypeSpec(typeDesc, [new Qualifier(QKind.Reference)]), _codeGenerator.AllocateHeapMemory(typeDesc.TypeRef));
		}

		public Expr CallStackConstructor(string typeName, Expr[] arguments)
		{
			var typeDesc = _membersFinder.FindType(typeName);
			return new Expr(new TypeSpec(typeDesc), _codeGenerator.AllocateHeapMemory(typeDesc.TypeRef));
		}

		public Expr CreateArrayInHeap(string typeName, Expr arraySize)
		{
			// TODO: тут првоерить что arraySize действительно число
			var typeDesc = _membersFinder.FindType(typeName);
			var accessor = _codeGenerator.AllocateHeapMemory(typeDesc.TypeRef, arraySize.Accessor);

			return new Expr(new TypeSpec(typeDesc, [new Qualifier(QKind.Reference), new Qualifier(QKind.Array)]), accessor);
		}

		public Expr VariableAccess(string name)
		{
			return GetVariable(name);
		}

		public Expr ClassFieldAccess(Expr instanceGetting, string fieldName)
		{
			if (!instanceGetting.Declaring.Type.Variables.TryGetValue(fieldName, out VariableDesc variable))
				throw new NotImplementedException();

			if (instanceGetting.Declaring.IsRef)
			{
				var fieldAccessor = _codeGenerator.GetHeapStructureField(instanceGetting.Accessor, instanceGetting.Declaring.Type.TypeRef, GetActualTypeRef(variable.Declaring), variable.Order);
				return new Expr(variable.Declaring, fieldAccessor);
			}
			else
			{
				var fieldAccessor = _codeGenerator.GetStackStructureField(instanceGetting.Accessor, instanceGetting.Declaring.Type.TypeRef, GetActualTypeRef(variable.Declaring), variable.Order);
				return new Expr(variable.Declaring, fieldAccessor);
			}
		}

		public Expr ArrayCellAccess(Expr arrayGetting, Expr indexGetting)
		{
			// TODO: сделать проверки на типы
			var valueType = arrayGetting.Declaring.Type;
			var accessor = _codeGenerator.GetArrayCell(arrayGetting.Accessor, indexGetting.Accessor, valueType.TypeRef);
			return new Expr(new TypeSpec(valueType), accessor);
		}

		public Expr GetPointerToVar(Expr variable)
		{
			// TODO: сделать проверку на что что это реально переменная, а не какая-нибудь хуета, чтобы нельзя было написать ref 1
			var accessor = _codeGenerator.GetPointerToVar(variable.Accessor);
			return new Expr(new TypeSpec(variable.Declaring.Type, [new Qualifier(QKind.Reference)]), accessor);
		}

		public Expr Sum(Expr left, Expr right)
		{
			if (!_typeAnalyzer.CheckTypeMatching(left.Declaring.Type, right.Declaring.Type)) throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			var accessor = _codeGenerator.Sum(leftAccessor, rightAccessor);
			return new Expr(new TypeSpec(left.Declaring.Type), accessor);
		}

		public Expr Sub(Expr left, Expr right)
		{
			if (!_typeAnalyzer.CheckTypeMatching(left.Declaring.Type, right.Declaring.Type)) throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			var accessor = _codeGenerator.Sub(leftAccessor, rightAccessor);
			return new Expr(new TypeSpec(left.Declaring.Type), accessor);
		}

		public Expr Compare(Expr left, Expr right, CompareOperator compareOperator)
		{
			var uIntType = _membersFinder.FindType("uint");
			var intType = _membersFinder.FindType("int");

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			IValueAccessor accessor;

			//сравнение беззнаковых чисел
			if (_typeAnalyzer.StrictCheckTypeMatching(uIntType, left.Declaring.Type) && _typeAnalyzer.StrictCheckTypeMatching(uIntType, right.Declaring.Type))
			{
				accessor = _codeGenerator.Compare(leftAccessor, rightAccessor, false, compareOperator);
			}
			else if (_typeAnalyzer.StrictCheckTypeMatching(intType, left.Declaring.Type) && _typeAnalyzer.StrictCheckTypeMatching(intType, right.Declaring.Type))
			{
				accessor = _codeGenerator.Compare(leftAccessor, rightAccessor, true, compareOperator);
			}
			else
			{
				throw new NotImplementedException();
			}

			return new Expr(new TypeSpec(TypeNameToTypeDesc("bool")), accessor);
		}

		public Expr LogicalAnd(Expr left, Expr right)
		{
			var boolType = _membersFinder.FindType("bool");
			if (left.Declaring.Type != boolType || right.Declaring.Type != boolType)
				throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			return new Expr(new TypeSpec(boolType), _codeGenerator.LogicalAnd(leftAccessor, rightAccessor));
		}

		public Expr LogicalOr(Expr left, Expr right)
		{
			var boolType = _membersFinder.FindType("bool");
			if (left.Declaring.Type != boolType || right.Declaring.Type != boolType)
				throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			throw new NotImplementedException();
		}

		private (IValueAccessor leftAccessor, IValueAccessor rightAccessor) BitOperationPrepeare(Expr left, Expr right)
		{
			var boolType = _membersFinder.FindType("bool");
			var intType = _membersFinder.FindType("int");
			if (left.Declaring.Type != right.Declaring.Type || (left.Declaring.Type != boolType && _typeAnalyzer.CheckTypeMatching(left.Declaring.Type, intType)))
				throw new NotImplementedException();

			var leftAccessor = AutoDereferenceIfPointer(left);
			var rightAccessor = AutoDereferenceIfPointer(right);

			return (leftAccessor, rightAccessor);
		}

		public Expr BitAnd(Expr left, Expr right)
		{
			(IValueAccessor leftAccessor, IValueAccessor rightAccessor) = BitOperationPrepeare(left, right);
			return new Expr(new TypeSpec(left.Declaring.Type), _codeGenerator.BitAnd(leftAccessor, rightAccessor));
		}

		public Expr BitXor(Expr left, Expr right)
		{
			(IValueAccessor leftAccessor, IValueAccessor rightAccessor) = BitOperationPrepeare(left, right);
			return new Expr(new TypeSpec(left.Declaring.Type), _codeGenerator.BitXor(leftAccessor, rightAccessor));
		}

		public Expr BitOr(Expr left, Expr right)
		{
			(IValueAccessor leftAccessor, IValueAccessor rightAccessor) = BitOperationPrepeare(left, right);
			return new Expr(new TypeSpec(left.Declaring.Type), _codeGenerator.BitOr(leftAccessor, rightAccessor));
		}

		public Expr BitNot(Expr expr)
		{
			var boolType = _membersFinder.FindType("bool");
			var intType = _membersFinder.FindType("int");
			if (expr.Declaring.Type != boolType && _typeAnalyzer.CheckTypeMatching(expr.Declaring.Type, intType))
				throw new NotImplementedException();

			var accessor = AutoDereferenceIfPointer(expr);

			return new Expr(new TypeSpec(expr.Declaring.Type), _codeGenerator.BitNot(accessor));
		}

		public Expr CreateInt(int num)
		{
			var valueAccessor = _codeGenerator.CreateIntConst((ulong)num, BaseTypes.Int);
			return new Expr(new TypeSpec(TypeNameToTypeDesc("int")), valueAccessor);
		}

		public Expr CreateByte(byte num)
		{
			var valueAccessor = _codeGenerator.CreateIntConst((ulong)num, BaseTypes.Int);
			return new Expr(new TypeSpec(TypeNameToTypeDesc("byte")), valueAccessor);
		}

		public Expr CreateLocalVariable(string name, TypeSpec declaring)
		{
			if (_functionLocalVariables.ContainsKey(name)) throw new NotImplementedException();

			var varAccessor = _codeGenerator.CreateVar(name, GetActualTypeRef(declaring));

			var varExpr = new Expr(declaring, varAccessor);
			_functionLocalVariables[name] = varExpr;

			return varExpr;
		}

		public Expr VariableAssing(Expr varExpr, Expr expr, Qualifier? assignQualifier)
		{
			if (!_typeAnalyzer.CheckTypeMatching(varExpr.Declaring.Type, expr.Declaring.Type)) throw new NotImplementedException();

			// TODO: вынести это в какую-нибудь константу или прямо в класс занести, чтобы везде такую хуню не писать
			if (varExpr.Declaring.IsRef && !expr.Declaring.IsRef)
			{
				var realVar = _codeGenerator.PointerDereference(varExpr.Accessor, varExpr.Declaring.Type.TypeRef);
				_codeGenerator.Assign(realVar, GetAccessorOfRequiredType(expr, varExpr.Declaring));

				return varExpr;
			}
			else if (varExpr.Declaring.IsRef && expr.Declaring.IsRef)
			{
				if (!assignQualifier.HasValue)
				{
					throw new NotImplementedException(); // TODO: разрешить это в usafe контексте
					var realValue = _codeGenerator.PointerDereference(expr.Accessor, expr.Declaring.Type.TypeRef);
					var realVar = _codeGenerator.PointerDereference(varExpr.Accessor, varExpr.Declaring.Type.TypeRef);
					_codeGenerator.Assign(realVar, GetAccessorOfRequiredType(realValue, expr.Declaring, varExpr.Declaring));

					return varExpr;
				}
				else
				{
					if (assignQualifier.Value.Kind != QKind.Reference) throw new NotImplementedException();

					_codeGenerator.Assign(varExpr.Accessor, GetAccessorOfRequiredType(expr, varExpr.Declaring));
					return varExpr;
				}
			}

			_codeGenerator.Assign(varExpr.Accessor, GetAccessorOfRequiredType(expr, varExpr.Declaring));
			return varExpr;
		}

		private IValueAccessor GetAccessorOfRequiredType(Expr expr, TypeSpec valueForOrientation)
		{
			return GetAccessorOfRequiredType(expr.Accessor, expr.Declaring, valueForOrientation);
		}

		private IValueAccessor GetAccessorOfRequiredType(IValueAccessor exprAccessor, TypeSpec exprDeclaring, TypeSpec valueForOrientation)
		{
			if (exprDeclaring.QualifiersExists || !exprDeclaring.Type.IsBaseType) return exprAccessor;

			//проверяем что exprDeclaring имеет числовой тип
			var intType = _membersFinder.FindType("int");
			if (_typeAnalyzer.StrictCheckTypeMatching(intType, exprDeclaring.Type))
			{
				if (exprDeclaring.Type == intType) return exprAccessor;

				return _codeGenerator.NumTrunc(exprAccessor, TypeToBaseType(valueForOrientation.Type));
			}

			return exprAccessor;
		}

		public IValueAccessor AutoDereferenceIfPointer(Expr expr)
		{
			if (!expr.Declaring.IsRef) return expr.Accessor;
			return _codeGenerator.PointerDereference(expr.Accessor, expr.Declaring.Type.TypeRef);
		}

		private Expr GetVariable(string name)
		{
			if (!_functionLocalVariables.TryGetValue(name, out var value))
			{
				if (_currentClass == null) throw new NotImplementedException();
				if (!_currentClass.Variables.TryGetValue(name, out var field)) throw new NotImplementedException();

				var fieldDeclaring = field.Declaring;

				var fieldAccessor = _codeGenerator.GetHeapStructureField(_currentFunctionParentClassRef!, _currentClass.TypeRef,
					GetActualTypeRef(fieldDeclaring),
					field.Order);

				return new Expr(fieldDeclaring, fieldAccessor);
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

		private TypeRef GetActualTypeRef(TypeSpec varDeclaring)
		{
			if (varDeclaring.QualifiersExists) return _codeGenerator.PointerType;
			return varDeclaring.Type.TypeRef;
		}
	}
}
