using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Generation
{
	internal class LogicalOperationAccessor : IValueAccessor
	{
		private readonly Func<LLVMValueRef> _builder;

		public LogicalOperationAccessor(Func<LLVMValueRef> builder)
		{
			_builder = builder;
		}

		public LLVMValueRef GetRealValue()
		{
			throw new NotImplementedException();
		}

		public LLVMValueRef GetValue()
		{
			return _builder();
		}

		public void SetValue(LLVMValueRef value)
		{
			throw new NotImplementedException();
		}
	}
}
