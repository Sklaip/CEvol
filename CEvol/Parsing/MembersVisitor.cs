using Antlr4.Runtime.Misc;
using CEvol.Core;
using CEvol.Core.MemebersModels;
using CEvol.Generation;
using static CEvol.Core.MemebersModels.Qualifier;

namespace CEvol.Parsing
{
	internal class MembersVisitor : CEvolParserBaseVisitor<object?>
	{
		public string CurrentNameSpace { get; private set; } = null!;
		private Dictionary<string, ClassSignature> _classes = new();

		private Dictionary<string, VariableSignature>? _currentClassVariables = null;
		private Dictionary<string, List<FuncSignature>>? _currentClassFunctions = null;
		private List<ConstructorSignature>? _currentClassConstructors = null;
		private Dictionary<string, List<FuncSignature>> _singleFunctions = new();

		private record TypeDeclaring(string TypeName, string[] Qualifiers, string[] Modifiers);
		private record FuncSignature(string Name, TypeDeclaring ReturnType, List<(TypeDeclaring Type, string Name)>? Arguments, string[] modifiers);
		private record ConstructorSignature(List<(TypeDeclaring Type, string Name)>? Arguments, string[] modifiers);
		private record VariableSignature(string Name, TypeDeclaring Type);
		private record ClassSignature(string Name, List<ConstructorSignature> Ctors, Dictionary<string, List<FuncSignature>> Functions, Dictionary<string, VariableSignature> Fields);

		public override object? VisitNamespaceDecl([NotNull] CEvolParser.NamespaceDeclContext context)
		{
			CurrentNameSpace = context.IDENTIFIER().GetText();
			if (CurrentNameSpace == null)
				throw new NotImplementedException();

			return VisitChildren(context);
		}

		public override object? VisitClassDecl([NotNull] CEvolParser.ClassDeclContext context)
		{
			_currentClassVariables = new();
			_currentClassFunctions = new();
			_currentClassConstructors = new();

			var typeName = context.IDENTIFIER().ToString();
			var fullTypeName = $"{CurrentNameSpace}.{typeName}";
			if (typeName == null || _classes.ContainsKey(fullTypeName))
				throw new NotImplementedException();

			foreach (var fieldDecl in context.fieldDecl())
			{
				Visit(fieldDecl);
			}

			foreach (var funcDecl in context.functionDecl())
			{
				Visit(funcDecl);
			}

			foreach (var funcDecl in context.constructorDecl())
			{
				Visit(funcDecl);
			}

			var currentClassDesc = new ClassSignature(fullTypeName, _currentClassConstructors, _currentClassFunctions, _currentClassVariables);
			_classes[fullTypeName] = currentClassDesc;

			_currentClassVariables = null;
			_currentClassFunctions = null;
			_currentClassConstructors = null;

			return null;
		}

		public override object? VisitFieldDecl([NotNull] CEvolParser.FieldDeclContext context)
		{
			var fieldName = context.IDENTIFIER().ToString();
			if (fieldName == null || _currentClassVariables.ContainsKey(fieldName))
				throw new NotImplementedException();

			var fieldSpec = ParseTypeSpec(context.typeSpec());

			var desc = new VariableSignature(fieldName, fieldSpec);
			_currentClassVariables.Add(fieldName, desc);

			return null;
		}

		public override object VisitFunctionDecl([NotNull] CEvolParser.FunctionDeclContext context)
		{
			var prms = context.@params();

			List<(TypeDeclaring Type, string Name)>? parameters = null;

			if (prms != null)
			{
				parameters = ParseParams(prms);
			}

			TypeDeclaring typeSpec = ParseTypeSpec(context.typeSpec());
			string? funcName = context.IDENTIFIER().ToString();
			if (funcName == null) throw new NotImplementedException();

			var funcsList = _currentClassFunctions ?? _singleFunctions;

			if (!funcsList.TryGetValue(funcName, out List<FuncSignature>? functions))
			{
				functions = new();
				funcsList[funcName] = functions;
			}

			functions.Add(new FuncSignature(funcName, typeSpec, parameters, []));

			return null;
		}

		public override object VisitAbstractFunctionDecl([NotNull] CEvolParser.AbstractFunctionDeclContext context)
		{
			var prms = context.@params();

			List<(TypeDeclaring Type, string Name)>? parameters = null;

			if (prms != null)
			{
				parameters = ParseParams(prms);
			}

			TypeDeclaring typeSpec = ParseTypeSpec(context.typeSpec());
			string? funcName = context.IDENTIFIER().ToString();
			if (funcName == null) throw new NotImplementedException();

			string[] modifers = context.extraModifier()?.Select(x => x?.GetText() ?? "").ToArray() ?? [];

			if (!modifers.Contains("extern")) throw new NotImplementedException();

			var funcsList = _currentClassFunctions ?? _singleFunctions;

			if (!funcsList.TryGetValue(funcName, out List<FuncSignature>? functions))
			{
				functions = new();
				funcsList[funcName] = functions;
			}

			functions.Add(new FuncSignature(funcName, typeSpec, parameters, modifers));

			return null;
		}

		public override object VisitConstructorDecl([NotNull] CEvolParser.ConstructorDeclContext context)
		{
			var prms = context.@params();

			List<(TypeDeclaring Type, string Name)>? parameters = null;

			if (prms != null)
			{
				parameters = ParseParams(prms);
			}

			if (_currentClassConstructors == null) throw new NotImplementedException();

			_currentClassConstructors.Add(new ConstructorSignature(parameters, []));

			return null;
		}
		private List<(TypeDeclaring Type, string Name)> ParseParams([NotNull] CEvolParser.ParamsContext context)
		{
			var parameters = new List<(TypeDeclaring Type, string Name)>();

			int count = context.typeSpec().Length;

			for (int i = 0; i < count; i++)
			{
				TypeDeclaring paramDecl = ParseTypeSpec(context.typeSpec(i));
				string paramName = context.IDENTIFIER(i).GetText();

				parameters.Add((paramDecl, paramName));
			}

			return parameters;
		}

		private TypeDeclaring ParseTypeSpec([NotNull] CEvolParser.TypeSpecContext context)
		{
			var typeName = context.IDENTIFIER().GetText();
			if (string.IsNullOrEmpty(typeName))
				throw new NotImplementedException();

			var qualifiers = new List<string>();
			foreach (var qualifier in context.qualifier())
			{
				qualifiers.Add(qualifier.GetText());
			}

			foreach (var arr in context.arraySpec())
			{
				qualifiers.Add(ParseArraySpec(arr));
			}

			return new TypeDeclaring(typeName, qualifiers.ToArray(), []);
		}

		public string ParseArraySpec([NotNull] CEvolParser.ArraySpecContext context)
		{
			return "array";
		}

		private TypeDesc FindTypeForDeclaring(Dictionary<string, TypeDesc> currentClasses, TypeDeclaring typeDecl)
		{
			if (!currentClasses.TryGetValue(typeDecl.TypeName, out TypeDesc? type) && !currentClasses.TryGetValue($"{CurrentNameSpace}.{typeDecl.TypeName}", out type))
			{
				// этого типа не существует
				throw new NotImplementedException();
			}

			return type;
		}

		private void ConstructorsAnalyze(List<ConstructorSignature> constructorsList,
			Dictionary<string, TypeDesc> typesList, CodeGenerator codeGenerator, TypeDesc currentClass)
		{
			foreach (var ctor in constructorsList)
			{
				var arguments = new List<Argument>();
				var agrumentsRefs = new List<TypeRef>();

				string funcName = $"{currentClass.Name}_ctor";
				agrumentsRefs.Add(codeGenerator.PointerType);

				if (ctor.Arguments != null)
				{
					foreach ((TypeDeclaring Type, string Name) funcArgument in ctor.Arguments)
					{
						TypeDesc argumentType = FindTypeForDeclaring(typesList, funcArgument.Type);
						var declaring = new TypeSpec(argumentType, Qualifier.FromString(funcArgument.Type.Qualifiers));
						arguments.Add(new Argument(declaring, funcArgument.Name));
						agrumentsRefs.Add(declaring.QualifiersExists ? codeGenerator.PointerType : argumentType.TypeRef);
					}
				}

				FuncRefData funcRefs;
				bool infArgs = ctor.modifiers.Contains("infargs"); // TODO: енумом модификаторы сделать что ли, или флагами
				funcRefs = codeGenerator.CreateFunctionSiganture(funcName, typesList["void"].TypeRef, agrumentsRefs, infArgs);

				currentClass.Constructors.Add(new ConstructorDesc(arguments.ToArray(), funcRefs));
			}
		}

		private void FunctionsAnalyze(Dictionary<string, List<FuncSignature>> rawFunctionsList,
			Dictionary<string, TypeDesc> typesList, Dictionary<string, FuncDesc[]> listToAdd, CodeGenerator codeGenerator, TypeDesc? currentClass = null)
		{
			foreach (var funcsKey in rawFunctionsList.Keys)
			{
				var funcList = new List<FuncDesc>();

				foreach (var func in rawFunctionsList[funcsKey])
				{
					TypeDesc returnType = FindTypeForDeclaring(typesList, func.ReturnType);
					var returnTypeQualifers = Qualifier.FromString(func.ReturnType.Qualifiers);

					var arguments = new List<Argument>();
					var agrumentsRefs = new List<TypeRef>();

					string funcName;
					if (currentClass != null)
					{
						funcName = $"{currentClass.Name}_{func.Name}"; // TODO: если неколько функций с одним именем, то это в названии надо учитывать
						agrumentsRefs.Add(codeGenerator.PointerType);
					}
					else
					{
						funcName = $"{func.Name}";
					}

					if (func.Arguments != null)
					{
						foreach ((TypeDeclaring Type, string Name) funcArgument in func.Arguments)
						{
							TypeDesc argumentType = FindTypeForDeclaring(typesList, funcArgument.Type);
							var declaring = new TypeSpec(argumentType, Qualifier.FromString(funcArgument.Type.Qualifiers));
							arguments.Add(new Argument(declaring, funcArgument.Name));
							agrumentsRefs.Add(declaring.QualifiersExists ? codeGenerator.PointerType : argumentType.TypeRef);
						}
					}

					FuncRefData funcRefs;
					bool infArgs = func.modifiers.Contains("infargs"); // TODO: енумом модификаторы сделать что ли, или флагами
					if (func.ReturnType.Qualifiers == null || func.ReturnType.Qualifiers.Length < 1)
					{
						funcRefs = codeGenerator.CreateFunctionSiganture(funcName, returnType.TypeRef, agrumentsRefs, infArgs);
					}
					else
					{
						funcRefs = codeGenerator.CreateFunctionSiganture(funcName, QKindToTypeRef(returnTypeQualifers[0].Kind, codeGenerator), agrumentsRefs, infArgs);
					}

					var funcDesc = new FuncDesc(new TypeSpec(returnType, returnTypeQualifers), func.Name, arguments.ToArray(), funcRefs, infArgs);
					funcList.Add(funcDesc);
				}

				// TODO: сделать проверку на дубликаты методов
				listToAdd.Add(funcsKey, funcList.ToArray());
			}
		}

		private Dictionary<string, TypeDesc> BuildClassesList(MembersTable existsMembers, CodeGenerator codeGenerator)
		{
			var currentClasses = new Dictionary<string, TypeDesc>(existsMembers.Types);
			foreach (var currentClass in _classes.Values)
			{
				if (existsMembers.Types.ContainsKey(currentClass.Name))
				{
					//такой класс уже существует
					throw new NotImplementedException();
				}

				var classStructure = codeGenerator.CreateStructure(currentClass.Name);

				var classDesc = new TypeDesc(currentClass.Name, classStructure, [], [], []);
				currentClasses.Add(currentClass.Name, classDesc);
			}

			foreach (var currentClass in _classes.Values)
			{
				var currentClassTypeDesc = currentClasses[currentClass.Name];
				var filedTypes = new List<TypeRef>();
				uint fieldNum = 0;
				foreach (var field in currentClass.Fields.Values)
				{
					TypeDesc fieldType = FindTypeForDeclaring(currentClasses, field.Type);
					var qualifers = Qualifier.FromString(field.Type.Qualifiers);

					if (!fieldType.IsBaseType && (qualifers == null || qualifers.Length < 1)) throw new NotImplementedException(); // TODO: сделать возможность пихать класс в класс по значению

					currentClassTypeDesc.Variables.Add(field.Name, new VariableDesc(new TypeSpec(fieldType, qualifers), field.Name, fieldNum));

					if (qualifers == null || qualifers.Length < 1)
					{
						filedTypes.Add(fieldType.TypeRef);
					}
					else
					{
						filedTypes.Add(QKindToTypeRef(qualifers[0].Kind, codeGenerator));
					}

					fieldNum++;
				}

				codeGenerator.FillStructureBody(currentClassTypeDesc.TypeRef, filedTypes);

				FunctionsAnalyze(currentClass.Functions, currentClasses, currentClassTypeDesc.Functions, codeGenerator, currentClassTypeDesc);
				ConstructorsAnalyze(currentClass.Ctors, currentClasses, codeGenerator, currentClassTypeDesc);
			}

			return currentClasses;
		}

		// TODO: это куда-то вынести, код дублирует с SemanticAnalyzer
		private TypeRef QKindToTypeRef(QKind qKind, CodeGenerator codeGenerator)
		{
			switch (qKind)
			{
				case QKind.Reference:
				case QKind.Array:
				case QKind.BorrowReference:
					return codeGenerator.PointerType;
				default:
					throw new NotImplementedException();
			}
		}


		public MembersTable Build(MembersTable existsMembers, CodeGenerator codeGenerator)
		{
			var classes = BuildClassesList(existsMembers, codeGenerator);
			var singleFunctions = new Dictionary<string, FuncDesc[]>();
			FunctionsAnalyze(_singleFunctions, classes, singleFunctions, codeGenerator);

			return new MembersTable(singleFunctions, classes);
		}

	}
}
