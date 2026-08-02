using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CEvol.Core.LogicModels.Statements;
using CEvol.Core.MemebersModels;
using CEvol.Generation;
using CEvol.Parsing;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace CEvol.Core
{
	internal class Compiler
	{
		public void Execute(string inputFile, string outputFile)
		{
			// 1. Инициализируем подсистемы LLVM В САМОМ НАЧАЛЕ
			LLVM.InitializeNativeTarget();
			LLVM.InitializeNativeAsmPrinter();
			LLVM.InitializeNativeAsmParser();

			LLVM.InitializeAllTargetInfos();
			LLVM.InitializeAllTargets();
			LLVM.InitializeAllTargetMCs();
			LLVM.InitializeAllAsmPrinters();

			var fileContent = File.ReadAllText(inputFile);

			ICharStream stream = CharStreams.fromString(fileContent);
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

			var errorsBag = new ErrorsBag(new Dictionary<string, string>()
			{
				[inputFile] = fileContent
			});

			var test = new LogicVisitor(finder, errorsBag, inputFile);
			test.Visit(tree);
			var statement = test.ResultStatement;

			if (!errorsBag.HasErrors)
			{
				var emmitter = new Emitter(codeGenerator);
				emmitter.Build((NamespaceStatement)statement);

				var module = emmitter.CodeGenerator.GetModule();

				Console.WriteLine("================ ИСХОДНЫЙ IR ================");
				module.Dump();

				Optimize(module);

				codeGenerator.VerifyModule();

				Compile(module);
			}
			else
			{
				Console.WriteLine(errorsBag.BuildErrorsMessage());
			}
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

		private unsafe void Optimize(LLVMModuleRef module)
		{
			// Создаем TargetMachine
			var triple = LLVMTargetRef.DefaultTriple;
			var target = LLVMTargetRef.GetTargetFromTriple(triple);

			// Внимание: CreateTargetMachine возвращает LLVMTargetMachineRef struct
			var targetMachine = target.CreateTargetMachine(
				triple,
				"generic",
				"",
				LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
				LLVMRelocMode.LLVMRelocDefault,
				LLVMCodeModel.LLVMCodeModelDefault
			);

			// Создаем опции пасс-билдера
			LLVMOpaquePassBuilderOptions* passOptions = LLVM.CreatePassBuilderOptions();

			// Подготавливаем строку с пайплайном в формате C-string (sbyte*)
			byte[] passesBytes = Encoding.UTF8.GetBytes("default<O2>\0"); // обязательно null-terminated

			LLVMOpaqueError* error = null;

			fixed (byte* pPasses = passesBytes)
			{
				// Вызываем RunPasses с приведением типов к сырым указателям (*):
				error = LLVM.RunPasses(
					(LLVMOpaqueModule*)module.Handle,               // module -> LLVMOpaqueModule*
					(sbyte*)pPasses,                                // string -> sbyte*
					(LLVMOpaqueTargetMachine*)targetMachine.Handle, // targetMachine -> LLVMOpaqueTargetMachine*
					passOptions                                     // options
				);
			}

			// Проверяем на ошибки
			if (error != null)
			{
				sbyte* errMsg = LLVM.GetErrorMessage(error);
				string message = Marshal.PtrToStringUTF8((IntPtr)errMsg);
				Console.WriteLine($"Ошибка оптимизации: {message}");
				LLVM.DisposeErrorMessage(errMsg);
			}
			else
			{
				string optimizedIR = module.PrintToString();
				Console.WriteLine("\n================ ОПТИМИЗИРОВАННЫЙ IR ================");
				Console.WriteLine(optimizedIR);
			}

			// Освобождать нужно ВСЕГДА после завершения
			LLVM.DisposePassBuilderOptions(passOptions);
			//targetMachine.Dispose();
		}

		private void Compile(LLVMModuleRef module)
		{
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

		private void LinkExecutable(string objFile, string exeFile)
		{
			using var process = new System.Diagnostics.Process();

			process.StartInfo.FileName = "clang";
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
