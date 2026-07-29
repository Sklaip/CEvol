using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CEvol.Core;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using CEvol.Generation;
using LLVMSharp.Interop;
using System.Runtime.InteropServices;

namespace CEvol.Parsing
{
	internal class Praser
	{
		public void Prase(string sourceCode)
		{
			ICharStream stream = CharStreams.fromString(sourceCode);

			var lexer = new CEvolLexer(stream);

			ITokenStream tokenStream = new CommonTokenStream(lexer);

			var parser = new CEvolParser(tokenStream);

			IParseTree tree = parser.program();

			var analyzer = new MembersVisitor();
			analyzer.Visit(tree);

			var codeGenerator = new CodeGenerator(analyzer.CurrentNameSpace);
			var table = analyzer.Build(BuildBaseMembersList(codeGenerator), codeGenerator);

			var finder = new MembersFinder(table);
			finder.AddNamespace(analyzer.CurrentNameSpace);

			var test = new LogicVisitor(finder);
			test.Visit(tree);
			var statement = test.ResultStatement;

			var emmitter = new Emitter(codeGenerator);
			emmitter.Build((NamespaceStatement)statement);

			var module = emmitter.CodeGenerator.GetModule();
			module.Dump();
			codeGenerator.VerifyModule();
			Compile(module);

		}

		private MembersTable BuildBaseMembersList(CodeGenerator codeGenerator)
		{
			var types = new Dictionary<string, TypeDesc>();
			types["void"] = new TypeDesc("void", codeGenerator.GetType(BaseTypes.Void));
			types["bool"] = new TypeDesc("bool", codeGenerator.GetType(BaseTypes.Bool));
			types["byte"] = new TypeDesc("byte", codeGenerator.GetType(BaseTypes.Byte));
			types["short"] = new TypeDesc("short", codeGenerator.GetType(BaseTypes.Short));
			types["int"] = new TypeDesc("int", codeGenerator.GetType(BaseTypes.Int));
			types["sbyte"] = new TypeDesc("sbyte", codeGenerator.GetType(BaseTypes.Byte));
			types["ushort"] = new TypeDesc("ushort", codeGenerator.GetType(BaseTypes.Short));
			types["uint"] = new TypeDesc("uint", codeGenerator.GetType(BaseTypes.Int));


			//types["ref"] = new TypeDesc("ref", codeGenerator.GetType(BaseTypes.Pointer), new CascadingTypeBehavior(CascadingTypeBehavior.Dereference.Auto));
			//types["sharedRef"] = new TypeDesc("sharedRef", codeGenerator.GetType(BaseTypes.Pointer), new CascadingTypeBehavior(CascadingTypeBehavior.Dereference.Auto));
			//types["borrowerRef"] = new TypeDesc("borrowerRef", codeGenerator.GetType(BaseTypes.Pointer), new CascadingTypeBehavior(CascadingTypeBehavior.Dereference.Auto));
			//types["array"] = new TypeDesc("array", codeGenerator.GetType(BaseTypes.Pointer), new CascadingTypeBehavior(CascadingTypeBehavior.Dereference.RequiresClarification));

			types["short"].InheritedTypes.Add(types["int"]);
			types["sbyte"].InheritedTypes.Add(types["short"]);

			types["ushort"].InheritedTypes.AddRange(types["uint"], types["int"]);
			types["byte"].InheritedTypes.AddRange(types["short"], types["ushort"]);

			return new MembersTable([], types);
		}

		private static void Compile(LLVMModuleRef module)
		{
			// 1. Инициализируем подсистему генерации кода LLVM
			LLVM.InitializeAllTargetInfos();
			LLVM.InitializeAllTargets();
			LLVM.InitializeAllTargetMCs();
			LLVM.InitializeAllAsmPrinters();

			// 2. Получаем целевую платформу по умолчанию (Target Triple)
			// Например: "x86_64-pc-windows-msvc" или "x86_64-unknown-linux-gnu"
			string triple = LLVMTargetRef.DefaultTriple;
			var target = LLVMTargetRef.GetTargetFromTriple(triple);

			// 3. Создаем Target Machine (настройки компиляции под конкретный процессор)
			var targetMachine = target.CreateTargetMachine(
				triple,
				cpu: "generic",
				features: "",
				LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
				LLVMRelocMode.LLVMRelocDefault,
				LLVMCodeModel.LLVMCodeModelDefault
			);

			// Устанавливаем triple для модуля, чтобы он соответствовал машине
			module.Target = triple;

			// 4. Генерируем объектный файл (.obj или .o)
			string objFileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "output.obj" : "output.o";

			if (targetMachine.TryEmitToFile(module, objFileName, LLVMCodeGenFileType.LLVMObjectFile, out string errorMessage))
			{
				Console.WriteLine($"Объектный файл успешно создан: {objFileName}");
			}
			else
			{
				Console.WriteLine($"Ошибка генерации: {errorMessage}");
				return;
			}

			Console.WriteLine("");
			Console.WriteLine();

			LinkExecutable(objFileName, "my_program.exe");
		}

		static void LinkExecutable(string objFile, string exeFile)
		{
			using var process = new System.Diagnostics.Process();

			process.StartInfo.FileName = "D:\\Programs\\LLVM\\bin\\clang.exe";
			process.StartInfo.Arguments = $"{objFile} -o {exeFile} -llegacy_stdio_definitions";

			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardError = true;

			try
			{
				process.Start();
				process.WaitForExit();
				if (process.ExitCode != 0)
				{
					string errors = process.StandardError.ReadToEnd();
					Console.WriteLine($"Ошибка линковщика:\n{errors}");
				}
				else
				{
					Console.WriteLine($"Исполняемый файл {exeFile} успешно собран!");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Пиздец: {ex.Message}");
			}
		}

	}
}
