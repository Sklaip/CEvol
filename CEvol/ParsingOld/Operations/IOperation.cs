using CEvol.Parsing.Operations.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Parsing.Operations
{
	internal interface IOperation
	{

		public IOperation? LeftOpearion { get; set; }
		public IOperation? RightOpearion { get; set; }

		public IOperationResult? OperationResult { get; }
	}
}
