using System;
using System.Collections.Generic;
using System.Text;

namespace EvolZero.Parsing.Models
{
	internal record TypeDeclaring(string TypeName, string[] Qualifiers, string[] Modifiers);
	internal record FuncSignature(string Name, TypeDeclaring ReturnType, List<(TypeDeclaring Type, string Name)>? Arguments, string[] modifiers);
	internal record ConstructorSignature(List<(TypeDeclaring Type, string Name)>? Arguments, string[] modifiers);
	internal record VariableSignature(string Name, TypeDeclaring Type);
	internal record ClassSignature(string Name, List<ConstructorSignature> Ctors, Dictionary<string, List<FuncSignature>> Functions, Dictionary<string, VariableSignature> Fields);
}
