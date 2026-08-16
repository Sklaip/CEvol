using LLVMSharp.Interop;

namespace EvolZero.Generation.Accessors
{
	internal interface IValueAccessor
	{
		LLVMValueRef GetValue();
		LLVMValueRef GetRealValue();
		void SetValue(LLVMValueRef value);
		LLVMTypeRef GetInnerType();
	}
}
