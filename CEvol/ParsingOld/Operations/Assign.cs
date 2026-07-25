using CEvol.Parsing.Operations.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Parsing.Operations
{
	internal class Assign : IOperation
	{
		public IOperation? LeftOpearion { get; set;  }

		public IOperation? RightOpearion { get; set; }

		public IOperationResult? OperationResult => new ValueExtraction();
	}
}
