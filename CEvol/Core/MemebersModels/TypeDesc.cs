using CEvol.Generation;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core.MemebersModels
{
	public class TypeDesc
	{
		public readonly string Name;
		public readonly Dictionary<string, VariableDesc> Variables;
		public readonly Dictionary<string, FuncDesc[]> Functions;
		public readonly List<ConstructorDesc> Constructors;
		public readonly List<TypeDesc> InheritedTypes = [];

		public readonly TypeRef TypeRef;

		public readonly bool IsBaseType = false;

		public TypeDesc(string name, TypeRef typeRef, Dictionary<string, VariableDesc> variables, 
			Dictionary<string, FuncDesc[]> functions, List<ConstructorDesc> constructors)
		{
			Name = name;
			Variables = variables;
			Functions = functions;
			TypeRef = typeRef;
			Constructors = constructors;
		}

		public TypeDesc(string name, TypeRef typeRef)
		{
			Name = name;
			Variables = [];
			Functions = [];
			Constructors = [];
			IsBaseType = true;
			TypeRef = typeRef;
		}

		public BaseTypes GetBaseType()
		{
			switch (Name)
			{
				case "int": return BaseTypes.Int;
				case "bool": return BaseTypes.Bool;
				case "ref":
				case "sharedRef":
				case "borrowerRef":
					return BaseTypes.Pointer;
				default: throw new NotImplementedException();
			}
		}
	}
}
