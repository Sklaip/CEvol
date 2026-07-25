using CEvol.Parsing.Operations.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Parsing.Operations
{
	internal class ValueCreate : IOperation
	{
		public string Value { get; }

		public DataType Type { get; }

		public IOperation? LeftOpearion { get; set; }

		public IOperation? RightOpearion { get; set; }

		public IOperationResult? OperationResult => new ValueExtraction();

		public ValueCreate(string value, DataType type)
		{
			Value = value;
			Type = type;
		}

	}
}
