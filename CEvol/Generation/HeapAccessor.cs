using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Generation
{
	internal class HeapAccessor : IValueAccessor
	{
		private readonly LLVMValueRef _constRef;

		public HeapAccessor(LLVMValueRef constRef)
		{
			_constRef = constRef;
		}

		public LLVMValueRef GetRealValue()
		{
			throw new NotImplementedException();
		}

		public LLVMValueRef GetValue()
		{
			return _constRef;
		}

		public void SetValue(LLVMValueRef value)
		{
			throw new NotImplementedException();
		}
	}
}
