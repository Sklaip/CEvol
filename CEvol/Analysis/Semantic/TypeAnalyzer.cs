using CEvol.Core;
using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CEvol.Analysis.Semantic
{
	internal class TypeAnalyzer
	{
		private readonly MembersFinder _membersFinder;

		private class VarDeclaringComparer : IEqualityComparer<TypeSpec>
		{
			private readonly TypeAnalyzer _typeAnalyzer;

			public VarDeclaringComparer(TypeAnalyzer typeAnalyzer)
			{
				_typeAnalyzer = typeAnalyzer;
			}

			public bool Equals(TypeSpec x, TypeSpec y)
			{
				return _typeAnalyzer.StrictCheckTypeMatching(x.Type, y.Type) && x.QualifiersEquals(y);
			}

			public int GetHashCode([DisallowNull] TypeSpec obj)
			{
				return obj.GetHashCode();
			}
		}

		public TypeAnalyzer(MembersFinder membersFinder)
		{
			_membersFinder = membersFinder;
		}

		public bool StrictCheckTypeMatching(TypeDesc to, TypeDesc from)
		{
			return Is(to, from);
		}

		public bool CheckTypeMatching(TypeDesc first, TypeDesc second)
		{
			if (Is(first, second)) return true;
			return Is(second, first);
		}

		public FuncDesc? FindSuitableFunction(FuncDesc[] functions, IEnumerable<TypeSpec> arguments)
		{
			foreach (var func in functions)
			{
				var funcArgs = func.Arguments.Select(x => x.Declaring);
				if (funcArgs.SequenceEqual(arguments, new VarDeclaringComparer(this)))
					return func;

				if (func.IsInfArgs)
				{
					var funcArgsArr = funcArgs.ToArray();
					var argsArray = arguments.Take(funcArgs.Count()).ToArray();
					if (funcArgsArr.SequenceEqual(argsArray, new VarDeclaringComparer(this)))
						return func;
				}
			}

			return null;
		}

		public ConstructorDesc? FindSuitableConstructor(IEnumerable<ConstructorDesc> constructors, IEnumerable<TypeSpec> arguments)
		{
			foreach (var ctor in constructors)
			{
				var funcArgs = ctor.Arguments.Select(x => x.Declaring);
				if (funcArgs.SequenceEqual(arguments, new VarDeclaringComparer(this)))
					return ctor;
			}

			return null;
		}

		private bool Is(TypeDesc to, TypeDesc from)
		{
			if (to == from) return true;
			foreach (var inheritedType in from.InheritedTypes)
			{
				if (Is(to, inheritedType)) return true;
			}

			return false;
		}
	}
}
