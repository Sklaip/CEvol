
using EvolZero.Core.MemebersModels;
using EvolZero.Generation;

namespace EvolZero.Core.LogicModels.Statements
{
	public class FunctionStatement : Statement, IFunctionalBlockStatement
	{
		public readonly FuncDesc FunctionSignature;

		public FunctionStatement(FuncDesc functionSignature, IReadOnlyCollection<ILogicModel> childs, PositionInSources pos) : base(childs, pos)
		{
			FunctionSignature = functionSignature;
		}

		public TypeSpec ReturnType => FunctionSignature.ReturnType;

		public Argument[] Arguments => FunctionSignature.Arguments;

		public FuncRefData RefData => FunctionSignature.RefData;

		public string Name => FunctionSignature.Name;
	}
}
