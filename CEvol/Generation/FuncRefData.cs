using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Generation
{
	public class FuncRefData
	{
		public LLVMTypeRef TypeRef { get; set; }
		public LLVMValueRef FuncRef { get; set; }
		public LLVMTypeRef[] ArgumentsTypes { get; set; }
	}
}
