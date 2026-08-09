using CEvol.Core;
using CEvol.Generation.Accessors;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.Build.Utilities;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static CEvol.Core.MemebersModels.Qualifier;
using static CEvol.Generation.FuncAccessData;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CEvol.Generation
{
	internal class CodeGenerator
	{
		private readonly string _moduleName;
		private readonly LLVMContextRef _context;
		private readonly LLVMModuleRef _module;
		private readonly LLVMBuilderRef _builder;

		//private Dictionary<string, LLVMValueRef> _variablesPointers = new();
		private LLVMValueRef CurrentFunction => _currentFunction.Value;
		private LLVMValueRef? _currentFunction;
		private LLVMValueRef? _funcReturnValuePtr;
		private LLVMBasicBlockRef? _funcReturnBlock;
		private LLVMTypeRef? _funcReturnType;
		private LLVMTypeRef? _retType;

		LLVMTypeRef _mallocType;
		LLVMValueRef _mallocFunc;

		private Stack<LLVMBasicBlockRef> _activeBlocks = new();

		public readonly TypeRef PointerType;

		public CodeGenerator(string moduleName)
		{
			_moduleName = moduleName;

			_context = LLVMContextRef.Create();
			_module = _context.CreateModuleWithName(moduleName);
			_builder = _context.CreateBuilder();

			DeclareMalloc();
			PointerType = new TypeRef(GetPointerType());
		}

		public TypeRef GetType(BaseTypes type) => new TypeRef(BaseTypeToLLVMType(type));

		public TypeRef CreateStructure(string name)
		{
			LLVMTypeRef structureType = _context.CreateNamedStruct(name);
			return new TypeRef(structureType);
		}

		public void FillStructureBody(TypeRef structure, IEnumerable<TypeRef> types)
		{
			structure.Type.StructSetBody(types.Select(x => x.Type).ToArray(), false);
		}

		public FuncRefData CreateFunctionSiganture(string funcName, TypeRef resultType, IEnumerable<TypeRef> argumentsTypes, bool infArgs = false)
		{
			var argumentsTypesArray = argumentsTypes.Select(x => x.Type).ToArray();
			var funcType = LLVMTypeRef.CreateFunction(resultType.Type, argumentsTypesArray, infArgs);
			var func = _module.AddFunction(funcName, funcType);

			return new FuncRefData
			{
				FuncRef = func,
				TypeRef = funcType,
				ArgumentsTypes = argumentsTypesArray
			};
		}

		public FuncAccessData StartFunctionBodyFill(FuncRefData funcData, string funcName, TypeRef resultType, IEnumerable<TypeRef> argumentsTypes)
		{
			// TODO: как-то сделать чтобы инфа уже переданная в метод CreateFunctionSiganture сюда не передавалась, чисто FuncRefData
			_retType = resultType.Type;
			// TODO: чтобы функцию можно было вызывать из других исполняемых файлов func.DLLStorageClass = LLVMDLLStorageClass.LLVMDLLExportStorageClass;
			_currentFunction = funcData.FuncRef;
			LLVMBasicBlockRef funcEntry = _context.AppendBasicBlock(funcData.FuncRef, $"{funcName}.entry");
			_funcReturnBlock = _context.AppendBasicBlock(funcData.FuncRef, $"{funcName}.end");
			_builder.PositionAtEnd(funcEntry);

			if (_retType != _context.VoidType)
			{
				_funcReturnValuePtr = _builder.BuildAlloca(resultType.Type, $"{funcName}.return.value");
			}
			else
			{
				_funcReturnValuePtr = null;
			}

			int count = argumentsTypes.Count();
			var result = new IValueAccessor[count];
			uint i = 0;
			foreach (var arg in argumentsTypes)
			{
				LLVMValueRef ptr = _builder.BuildAlloca(arg.Type, $"args{i}");

				var accessor = new VarAccessor(_builder, ptr, arg.Type);
				accessor.SetValue(funcData.FuncRef.GetParam(i));
				result[i] = accessor;
				i++;
			}

			return new FuncAccessData(result, funcData);
		}

		public void AddReturn(IValueAccessor? returnValue)
		{
			if (_funcReturnBlock == null)
				throw new NotImplementedException();

			if (_funcReturnValuePtr != null)
			{
				if (returnValue == null)
					throw new NotImplementedException();
				_builder.BuildStore(returnValue.GetValue(), _funcReturnValuePtr.Value);
			}

			_builder.BuildBr(_funcReturnBlock.Value);
		}

		public void StopFunctionBodyFill()
		{
			if (_funcReturnBlock == null)
				throw new NotImplementedException();

			_builder.PositionAtEnd(_funcReturnBlock.Value);

			if (_retType != null && _funcReturnValuePtr != null)
			{
				var returnValue = _builder.BuildLoad2(_retType.Value, _funcReturnValuePtr.Value);
				_builder.BuildRet(returnValue);
			}
			else
			{
				_builder.BuildRetVoid();
			}

		}

		public IValueAccessor FunctionCall(FuncRefData funcDesc, IValueAccessor[] valueAccessors)
		{
			var args = new LLVMValueRef[valueAccessors.Length];
			for (int i = 0; i < valueAccessors.Length; i++)
			{
				IValueAccessor accessor = valueAccessors[i];
				if (funcDesc.ArgumentsTypes.Length < i)
				{
					accessor = TruncIfInt(funcDesc.ArgumentsTypes[i], accessor);
				}

				args[i] = accessor.GetValue();
			}

			var res = _builder.BuildCall2(funcDesc.TypeRef, funcDesc.FuncRef, args, "");

			return new SimpleValueAccessor(res, funcDesc.TypeRef);
		}

		public FuncAccessData DeclareMalloc()
		{
			_mallocType = LLVMTypeRef.CreateFunction(GetPointerType(), new[] { _context.Int64Type }, false);
			_mallocFunc = _module.AddFunction("malloc", _mallocType);

			return new FuncAccessData(null, new FuncRefData // TODO: че-то с нулом придумать
			{
				FuncRef = _mallocFunc,
				TypeRef = _mallocType
			});
		}

		public FuncAccessData DeclareFree()
		{
			var freeType = LLVMTypeRef.CreateFunction(_context.VoidType, new[] { GetPointerType() }, false);
			var freeFunc = _module.AddFunction("free", freeType);

			return new FuncAccessData(null, new FuncRefData // TODO: че-то с нулом придумать
			{
				FuncRef = freeFunc,
				TypeRef = freeType
			});
		}

		public IValueAccessor AllocateHeapMemory(TypeRef type)
		{
			var memorySize = type.Type.SizeOf;
			var ptr = _builder.BuildCall2(_mallocType, _mallocFunc, new[] { memorySize }, "malloc");
			return new SimpleValueAccessor(ptr, GetPointerType());
		}

		public IValueAccessor AllocateHeapMemory(TypeRef type, IValueAccessor countGetter)
		{
			var memorySize = type.Type.SizeOf;
			var n_i64 = _builder.BuildIntCast(countGetter.GetValue(), _context.Int64Type, "n_i64");
			var totalBytes = _builder.BuildMul(n_i64, memorySize, "total_bytes");

			var ptr = _builder.BuildCall2(_mallocType, _mallocFunc, new[] { totalBytes }, "malloc");
			return new SimpleValueAccessor(ptr, GetPointerType());
		}

		public IValueAccessor LogicalAnd(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() =>
			{
				var func = CurrentFunction;

				var firstResult = firstOperation.GetValue();

				LLVMBasicBlockRef startBlock = _builder.InsertBlock;
				LLVMBasicBlockRef ifBlock = _context.AppendBasicBlock(func, "logicalAnd");
				LLVMBasicBlockRef exitIfBlock = _context.AppendBasicBlock(func, "logicalAnd.Exit");

				_builder.BuildCondBr(firstResult, ifBlock, exitIfBlock);

				_builder.PositionAtEnd(ifBlock);
				var secondResult = secondOperation.GetValue();

				LLVMBasicBlockRef ifBlockEnd = _builder.InsertBlock;
				_builder.BuildBr(exitIfBlock);

				_builder.PositionAtEnd(exitIfBlock);

				LLVMValueRef phiNode = _builder.BuildPhi(_context.Int1Type, "logicalAnd.phi");
				LLVMValueRef constFalse = LLVMValueRef.CreateConstInt(_context.Int1Type, 0);

				phiNode.AddIncoming(new[] { secondResult }, new[] { ifBlockEnd }, 1);
				phiNode.AddIncoming(new[] { constFalse }, new[] { startBlock }, 1);

				return phiNode;
			}, firstOperation.GetInnerType());
		}

		public IValueAccessor BitAnd(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildAnd(firstOperation.GetValue(), secondOperation.GetValue(), "bit_and"),
				firstOperation.GetInnerType());
		}

		public IValueAccessor BitOr(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildOr(firstOperation.GetValue(), secondOperation.GetValue(), "bit_or"),
				firstOperation.GetInnerType());
		}

		public IValueAccessor BitXor(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildXor(firstOperation.GetValue(), secondOperation.GetValue(), "bit_xor"),
				firstOperation.GetInnerType());
		}

		public IValueAccessor BitNot(IValueAccessor operation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildNot(operation.GetValue(), "bit_not"),
				operation.GetInnerType());
		}

		public void CreateIfBlock(IValueAccessor condition)
		{
			if (_currentFunction == null) throw new NotImplementedException();
			var func = _currentFunction.Value;

			LLVMBasicBlockRef ifBlock = _context.AppendBasicBlock(func, "if.then");
			LLVMBasicBlockRef endIfBlock = _context.AppendBasicBlock(func, "if.merge");

			_builder.BuildCondBr(condition.GetValue(), ifBlock, endIfBlock);
			_builder.PositionAtEnd(ifBlock);

			_activeBlocks.Push(endIfBlock);
		}

		public void EndIfBlock()
		{
			LLVMBasicBlockRef endIfBlock = _activeBlocks.Pop();
			_builder.BuildBr(endIfBlock);
			_builder.PositionAtEnd(endIfBlock);
		}

		public void CreateWhileBlock(IValueAccessor condition)
		{
			if (_currentFunction == null) throw new NotImplementedException();
			var func = _currentFunction.Value;

			LLVMBasicBlockRef conditionBlock = _context.AppendBasicBlock(func, "while.condition");
			LLVMBasicBlockRef bodyBlock = _context.AppendBasicBlock(func, "while.body");
			LLVMBasicBlockRef endBlock = _context.AppendBasicBlock(func, "while.merge");

			_builder.BuildBr(conditionBlock);
			_builder.PositionAtEnd(conditionBlock);

			_builder.BuildCondBr(condition.GetValue(), bodyBlock, endBlock);
			_builder.PositionAtEnd(bodyBlock);

			_activeBlocks.Push(endBlock);
			_activeBlocks.Push(conditionBlock);
		}

		public void EndWhileBlock()
		{
			LLVMBasicBlockRef conditionBlock = _activeBlocks.Pop();
			LLVMBasicBlockRef endWhileBlock = _activeBlocks.Pop();

			_builder.BuildBr(conditionBlock);
			_builder.PositionAtEnd(endWhileBlock);
		}

		// TODO: надо сделать оптимизацию чтобы все BuildAlloca вызывались в самом начале метода
		public IValueAccessor CreateVar(string name, TypeRef type)
		{
			var varType = type.Type;
			LLVMValueRef ptr = _builder.BuildAlloca(varType, name);

			return new VarAccessor(_builder, ptr, varType);
		}

		public IValueAccessor GetHeapStructureField(IValueAccessor structurePointer, TypeRef structureType, TypeRef fieldType, uint fieldNum)
		{
			var ptrToFiled = _builder.BuildStructGEP2(structureType.Type, structurePointer.GetValue(), fieldNum);
			return new VarAccessor(_builder, ptrToFiled, fieldType.Type);
		}

		public IValueAccessor GetStackStructureField(IValueAccessor structurePointer, TypeRef structureType, TypeRef fieldType, uint fieldNum)
		{
			var ptrToFiled = _builder.BuildStructGEP2(structureType.Type, structurePointer.GetRealValue(), fieldNum);
			return new VarAccessor(_builder, ptrToFiled, fieldType.Type);
		}

		public IValueAccessor GetArrayCell(IValueAccessor arrayPointer, IValueAccessor indexGetter, TypeRef arrayType)
		{
			var elemPtr = _builder.BuildGEP2(arrayType.Type, arrayPointer.GetValue(), new[] { indexGetter.GetValue() });
			return new VarAccessor(_builder, elemPtr, arrayType.Type);
		}

		public IValueAccessor PointerDereference(IValueAccessor pointer, TypeRef type)
		{
			//var originalPointer = _builder.BuildLoad2(type.Type, pointer.GetValue());
			//return new VarAccessor(_builder, originalPointer, type.Type);

			return new VarAccessor(_builder, pointer.GetValue(), type.Type);
		}

		public IValueAccessor GetPointerToVar(IValueAccessor var)
		{
			return new SimpleValueAccessor(var.GetRealValue(), GetPointerType());
		}

		public IValueAccessor CreateIntConst(ulong value, BaseTypes type)
		{
			LLVMTypeRef typeRef;
			switch (type)
			{
				case BaseTypes.Byte:
				case BaseTypes.SByte:
				case BaseTypes.Short:
				case BaseTypes.UShort:
				case BaseTypes.Int:
				case BaseTypes.UInt:
					typeRef = _context.Int32Type;
					break;
				case BaseTypes.Long:
					typeRef = _context.Int64Type;
					break;
				default:
					throw new NotImplementedException();
			}

			LLVMValueRef constValue = LLVMValueRef.CreateConstInt(typeRef, value);
			return new SimpleValueAccessor(constValue, typeRef);
		}

		public IValueAccessor CreateGlobalArray(byte[] bytes)
		{
			LLVMTypeRef arrayType = LLVMTypeRef.CreateArray(_context.Int8Type, (uint)bytes.Length);

			LLVMValueRef global = _module.AddGlobal(arrayType, "");

			LLVMValueRef[] values = bytes.Select(b => LLVMValueRef.CreateConstInt(_context.Int8Type, b, false)).ToArray();

			global.Initializer = LLVMValueRef.CreateConstArray(_context.Int8Type, values);

			return new SimpleValueAccessor(global, GetPointerType());
		}

		public void Assign(IValueAccessor to, IValueAccessor from)
		{
			var value = TruncIfInt(to, from);
			to.SetValue(value.GetValue());
		}

		public IValueAccessor GetValueByPointer(IValueAccessor ponter, TypeRef type)
		{
			var originalPointer = ponter.GetValue();
			return new SimpleValueAccessor(_builder.BuildLoad2(type.Type, originalPointer), type.Type);
		}

		public IValueAccessor Sum(IValueAccessor a, IValueAccessor b)
		{
			var aValue = a.GetValue();
			var bValue = b.GetValue();

			LLVMTypeRef resultType = a.GetInnerType();
			if (a.GetInnerType().IntWidth < b.GetInnerType().IntWidth)
			{
				aValue = _builder.BuildSExt(aValue, b.GetInnerType(), "sext");
				resultType = b.GetInnerType();
			}
			else if (a.GetInnerType().IntWidth > b.GetInnerType().IntWidth)
			{
				bValue = _builder.BuildSExt(bValue, a.GetInnerType(), "sext");
			}

			LLVMValueRef xNew = _builder.BuildAdd(aValue, bValue);
			return new SimpleValueAccessor(xNew, resultType);
		}

		public IValueAccessor Sub(IValueAccessor a, IValueAccessor b)
		{
			LLVMValueRef xNew = _builder.BuildSub(a.GetValue(), b.GetValue());
			return new SimpleValueAccessor(xNew, a.GetInnerType());
		}

		public IValueAccessor Compare(IValueAccessor a, IValueAccessor b, bool signed, CompareOperator compareType)
		{
			LLVMIntPredicate predicate;
			switch (compareType)
			{
				case CompareOperator.Equal:
					predicate = LLVMIntPredicate.LLVMIntEQ;
					break;
				case CompareOperator.GreaterThan:
					predicate = signed ? LLVMIntPredicate.LLVMIntSGT : LLVMIntPredicate.LLVMIntUGT;
					break;
				case CompareOperator.GreaterThanOrEqual:
					predicate = signed ? LLVMIntPredicate.LLVMIntSGE : LLVMIntPredicate.LLVMIntUGE;
					break;
				case CompareOperator.LessThan:
					predicate = signed ? LLVMIntPredicate.LLVMIntSLT : LLVMIntPredicate.LLVMIntULT;
					break;
				case CompareOperator.LessThanOrEqual:
					predicate = signed ? LLVMIntPredicate.LLVMIntSLE : LLVMIntPredicate.LLVMIntULE;
					break;
				default:
					throw new NotImplementedException();
			}

			return new LogicalOperationAccessor(() => _builder.BuildICmp(predicate, a.GetValue(), b.GetValue()), _context.Int1Type);
		}

		public IValueAccessor IntToIntExtension(IValueAccessor value, bool isSigned, TypeRef type)
		{
			LLVMValueRef res;
			if (isSigned)
			{
				res = _builder.BuildSExt(value.GetValue(), type.Type);
			}
			else
			{
				res = _builder.BuildZExt(value.GetValue(), type.Type);
			}

			return new SimpleValueAccessor(res, type.Type);
		}

		public IValueAccessor IntToFloatExtension(IValueAccessor value, bool isSigned, TypeRef type)
		{
			LLVMValueRef res;
			if (isSigned)
			{
				res = _builder.BuildSIToFP(value.GetValue(), type.Type);
			}
			else
			{
				res = _builder.BuildUIToFP(value.GetValue(), type.Type);
			}

			return new SimpleValueAccessor(res, type.Type);
		}

		/// <summary>
		/// Если оба параметры числа, то обрезает <paramref name="value"/> до типа <paramref name="dest"/>
		/// Это нужно потому что все числовые константы мы создаем изначально с типом int или long. 
		/// То есть если мы создали константу 10 и хотим записать ее в byte, то нужно сначала это число обрезать, 
		/// ибо мы ему выделили не 1 байт, а 4.
		/// </summary>
		/// <param name="dest">Акссесор на который ориентироваться для усечения <paramref name="dest"/>, из него будет взят только тип</param>
		/// <param name="value">Значение которое возможно будет усечено</param>
		/// <returns>Либо тот же <see cref="IValueAccessor"/> что и был передан, либо <see cref="SimpleValueAccessor"/> с усеченным значением</returns>
		private IValueAccessor TruncIfInt(IValueAccessor dest, IValueAccessor value)
		{
			var destType = dest.GetInnerType();
			return TruncIfInt(destType, value);
		}

		private IValueAccessor TruncIfInt(LLVMTypeRef destType, IValueAccessor value)
		{
			var fromType = value.GetInnerType();

			if ((destType == _context.Int8Type || destType == _context.Int16Type) && fromType == _context.Int32Type)
			{
				LLVMValueRef truncated = _builder.BuildTrunc(value.GetValue(), destType, "narrow");
				return new SimpleValueAccessor(truncated, destType);
			}

			return value;
		}

		private int GetTypeRank(LLVMTypeRef type) => type.Kind switch
		{
			LLVMTypeKind.LLVMIntegerTypeKind => (int)type.IntWidth,
			LLVMTypeKind.LLVMHalfTypeKind => 100,
			LLVMTypeKind.LLVMFloatTypeKind => 200,
			LLVMTypeKind.LLVMDoubleTypeKind => 300,
			LLVMTypeKind.LLVMFP128TypeKind => 400,
			_ => throw new NotImplementedException()
		};

		private LLVMValueRef ReduceToOneType(LLVMBuilderRef builder, LLVMValueRef left, LLVMValueRef right)
		{
			int rankL = GetTypeRank(left.TypeOf);
			int rankR = GetTypeRank(right.TypeOf);

			// Приводим к типу с наибольшим рангом
			if (rankL < rankR)
				left = CastTo(builder, left, right.TypeOf);
			else if (rankR < rankL)
				right = CastTo(builder, right, left.TypeOf);

			// Выбираем правильное сложение (Float или Int)
			return left.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind
				? builder.BuildAdd(left, right, "add_int")
				: builder.BuildFAdd(left, right, "add_float");
		}

		private LLVMValueRef CastTo(LLVMBuilderRef builder, LLVMValueRef val, LLVMTypeRef targetType)
		{
			var srcKind = val.TypeOf.Kind;
			var dstKind = targetType.Kind;

			// Int -> Int (расширение)
			if (srcKind == LLVMTypeKind.LLVMIntegerTypeKind && dstKind == LLVMTypeKind.LLVMIntegerTypeKind)
				return builder.BuildZExt(val, targetType, "zext"); // или BuildSExt для знаковых

			// Int -> Float / Double
			if (srcKind == LLVMTypeKind.LLVMIntegerTypeKind && dstKind != LLVMTypeKind.LLVMIntegerTypeKind)
				return builder.BuildSIToFP(val, targetType, "sitofp"); // или BuildUIToFP

			// Float -> Float (например, float -> double)
			if (srcKind != LLVMTypeKind.LLVMIntegerTypeKind && dstKind != LLVMTypeKind.LLVMIntegerTypeKind)
				return builder.BuildFPExt(val, targetType, "fpext");

			throw new InvalidOperationException("Неподдерживаемое приведение");
		}

		private LLVMTypeRef BaseTypeToLLVMType(BaseTypes type)
		{
			switch (type)
			{
				case BaseTypes.Void:
					return _context.VoidType;
				case BaseTypes.Byte:
				case BaseTypes.SByte:
					return _context.Int8Type;
				case BaseTypes.Short:
				case BaseTypes.UShort:
					return _context.Int16Type;
				case BaseTypes.Int:
				case BaseTypes.UInt:
					return _context.Int32Type;
				case BaseTypes.Long:
					return _context.Int64Type;
				case BaseTypes.Float:
					return _context.FloatType;
				case BaseTypes.Double:
					return _context.DoubleType;
				case BaseTypes.Bool:
					return _context.Int1Type;
				case BaseTypes.Pointer:
					return GetPointerType();
				default:
					throw new NotImplementedException();
			}
		}

		private LLVMTypeRef GetPointerType()
		{
			return LLVMTypeRef.CreatePointer(_context.Int32Type, 0);
		}

		private LLVMTypeRef[] BaseTypesToLLVMTypes(BaseTypes[] type)
		{
			return type.Select(BaseTypeToLLVMType).ToArray();
		}

		public void VerifyModule()
		{
			_module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
		}

		public LLVMModuleRef GetModule() => _module;
	}
}
