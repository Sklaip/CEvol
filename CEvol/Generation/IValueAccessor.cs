using LLVMSharp.Interop;

namespace CEvol.Generation
{
	internal interface IValueAccessor
	{
		LLVMValueRef GetValue();
		LLVMValueRef GetRealValue();
		void SetValue(LLVMValueRef value);
	}
}
