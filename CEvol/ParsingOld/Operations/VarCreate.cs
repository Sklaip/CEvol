using CEvol.Parsing.Operations.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Parsing.Operations
{
	internal class VarCreate : IOperation
	{
		public string Name { get; }

		public DataType Type { get; }

		public IOperation? LeftOpearion { get; set; }

		public IOperation? RightOpearion { get; set; }

		public IOperationResult? OperationResult => new VarAccessingResult();

		public VarCreate(string name, DataType type)
		{
			Name = name;
			Type = type;
		}

	}
}
