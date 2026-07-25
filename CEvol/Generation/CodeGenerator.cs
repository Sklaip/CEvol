using CEvol.Analysis;
using CEvol.Parsing;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.Build.Utilities;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using static CEvol.Analysis.Members.Models.Qualifier;
using static CEvol.Generation.FuncAccessData;

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

		private Stack<LLVMBasicBlockRef> _nestedBlocks = new();

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
			var funcType = LLVMTypeRef.CreateFunction(resultType.Type, argumentsTypes.Select(x => x.Type).ToArray(), infArgs);
			var func = _module.AddFunction(funcName, funcType);

			return new FuncRefData
			{
				FuncRef = func,
				TypeRef = funcType
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

			_funcReturnValuePtr = _builder.BuildAlloca(_retType.Value, $"{funcName}.return.value");

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

		public void AddReturn(IValueAccessor returnValue)
		{
			if (_funcReturnValuePtr == null || _funcReturnBlock == null)
				throw new NotImplementedException();

			_builder.BuildStore(returnValue.GetValue(), _funcReturnValuePtr.Value);
			_builder.BuildBr(_funcReturnBlock.Value);
		}

		public void StopFunctionBodyFill()
		{
			if (_funcReturnValuePtr == null || _funcReturnBlock == null || _retType == null)
				throw new NotImplementedException();

			//_builder.BuildBr(_funcReturnBlock.Value);
			_builder.PositionAtEnd(_funcReturnBlock.Value);
			var returnValue = _builder.BuildLoad2(_retType.Value, _funcReturnValuePtr.Value);
			_builder.BuildRet(returnValue);
		}

		public IValueAccessor FunctionCall(FuncRefData funcDesc, IValueAccessor[] valueAccessors)
		{
			var res = _builder.BuildCall2(funcDesc.TypeRef, funcDesc.FuncRef, valueAccessors.Select(x => x.GetValue()).ToArray(), "");

			return new SimpleValueAccessor(res);
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
			return new SimpleValueAccessor(ptr);
		}

		public IValueAccessor AllocateHeapMemory(TypeRef type, IValueAccessor countGetter)
		{
			var memorySize = type.Type.SizeOf;
			var n_i64 = _builder.BuildIntCast(countGetter.GetValue(), _context.Int64Type, "n_i64");
			var totalBytes = _builder.BuildMul(n_i64, memorySize, "total_bytes");

			var ptr = _builder.BuildCall2(_mallocType, _mallocFunc, new[] { totalBytes }, "malloc");
			return new SimpleValueAccessor(ptr);
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
			});
		}

		public IValueAccessor BitAnd(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildAnd(firstOperation.GetValue(), secondOperation.GetValue(), "bit_and"));
		}

		public IValueAccessor BitOr(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildOr(firstOperation.GetValue(), secondOperation.GetValue(), "bit_or"));
		}

		public IValueAccessor BitXor(IValueAccessor firstOperation, IValueAccessor secondOperation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildXor(firstOperation.GetValue(), secondOperation.GetValue(), "bit_xor"));
		}

		public IValueAccessor BitNot(IValueAccessor operation)
		{
			return new LogicalOperationAccessor(() => _builder.BuildNot(operation.GetValue(), "bit_not"));
		}

		public void CreateIfBlock(IValueAccessor condition)
		{
			if (_currentFunction == null) throw new NotImplementedException();
			var func = _currentFunction.Value;

			LLVMBasicBlockRef ifBlock = _context.AppendBasicBlock(func, "if.then");
			LLVMBasicBlockRef endIfBlock = _context.AppendBasicBlock(func, "if.merge");

			_builder.BuildCondBr(condition.GetValue(), ifBlock, endIfBlock);
			_builder.PositionAtEnd(ifBlock);

			_nestedBlocks.Push(endIfBlock);

		}

		public void EndIfBlock()
		{
			LLVMBasicBlockRef endIfBlock = _nestedBlocks.Pop();
			_builder.BuildBr(endIfBlock);
			_builder.PositionAtEnd(endIfBlock);
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
			return new SimpleValueAccessor(var.GetRealValue());
		}

		public IValueAccessor CreateIntConst(ulong value, BaseTypes type)
		{
			LLVMValueRef constValue = LLVMValueRef.CreateConstInt(BaseTypeToLLVMType(type), value);
			return new SimpleValueAccessor(constValue);
		}

		public void Assign(IValueAccessor to, IValueAccessor from)
		{
			to.SetValue(from.GetValue());
		}

		public IValueAccessor GetValueByPointer(IValueAccessor ponter, TypeRef type)
		{
			var originalPointer = ponter.GetValue();
			return new SimpleValueAccessor(_builder.BuildLoad2(type.Type, originalPointer));
		}

		public IValueAccessor Sum(IValueAccessor a, IValueAccessor b)
		{
			LLVMValueRef xNew = _builder.BuildAdd(a.GetValue(), b.GetValue());
			return new SimpleValueAccessor(xNew);
		}

		public IValueAccessor Sub(IValueAccessor a, IValueAccessor b)
		{
			LLVMValueRef xNew = _builder.BuildSub(a.GetValue(), b.GetValue());
			return new SimpleValueAccessor(xNew);
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

			return new LogicalOperationAccessor(() => _builder.BuildICmp(predicate, a.GetValue(), b.GetValue()));
		}

		public IValueAccessor NumTrunc(IValueAccessor value, BaseTypes detType)
		{
			LLVMValueRef truncated = _builder.BuildTrunc(value.GetValue(), BaseTypeToLLVMType(detType), "narrow");
			return new SimpleValueAccessor(truncated);
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
