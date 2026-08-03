
using CEvol.Core.MemebersModels;
using CEvol.Generation;

namespace CEvol.Core.LogicModels.Statements
{
	public class FunctionStatement : Statement, IFunctionalBlockStatement
	{
		public readonly FuncDesc FunctionSignature;

		public FunctionStatement(FuncDesc functionSignature, IReadOnlyCollection<ILogicModel> childs) : base(childs)
		{
			FunctionSignature = functionSignature;
		}

		public TypeSpec ReturnType => FunctionSignature.ReturnType;

		public Argument[] Arguments => FunctionSignature.Arguments;

		public FuncRefData RefData => FunctionSignature.RefData;

		public string Name => FunctionSignature.Name;
	}
}
