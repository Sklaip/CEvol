using CEvol.Generation;
using CEvol.Parsing;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Analysis.Members.Models
{
	public class TypeDesc
	{
		public readonly string Name;
		public readonly Dictionary<string, VariableDesc> Variables;
		public readonly Dictionary<string, FuncDesc[]> Functions;
		public readonly List<TypeDesc> InheritedTypes = [];

		public readonly TypeRef TypeRef;

		public readonly bool IsBaseType = false;

		public TypeDesc(string name, TypeRef typeRef, Dictionary<string, VariableDesc> variables, Dictionary<string, FuncDesc[]> functions)
		{
			Name = name;
			Variables = variables;
			Functions = functions;
			TypeRef = typeRef;
		}

		public TypeDesc(string name, TypeRef typeRef)
		{
			Name = name;
			Variables = [];
			Functions = [];
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
