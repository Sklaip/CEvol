using CEvol.Parsing.Operations.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Parsing.Operations
{
	internal class VarAccessing : IOperation
	{
		public IOperation? LeftOpearion { get; set; }

		public IOperation? RightOpearion { get; set; }
		public Member Member { get; }

		public IOperationResult? OperationResult => new VarAccessingResult();

		public VarAccessing(Member member)
		{
			Member = member;
		}

	}

}
