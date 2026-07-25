using CEvol.Generation.Accessors;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Generation
{
	internal class FuncAccessData
	{

		public readonly FuncRefData Refs = new FuncRefData();
		public IValueAccessor[] Arguments { get; set; }

		public FuncAccessData(IValueAccessor[] arguments, FuncRefData refs)
		{
			Refs = refs;
			Arguments = arguments;
		}
	}
}
