using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.LogicModels.Expressions
{
	public class StructureFieldAccessExpression : Expression
	{
		public readonly uint FiledNum;
		public readonly Expression StructureGetting;
		public readonly bool ByRef;

		public StructureFieldAccessExpression(uint filedNum, bool byRef, Expression structureGetting, TypeSpec resultTypeSpec, PositionInSources pos) : base(resultTypeSpec, pos)
		{
			FiledNum = filedNum;
			ByRef = byRef;
			StructureGetting = structureGetting;
		}
	}
}
