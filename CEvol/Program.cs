using CEvol.CLI;
using CEvol.Parsing;
using LLVMSharp;
using LLVMSharp.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CEvol
{
	internal class Program
	{
		static void Main(string[] args)
		{
			//var programText = File.ReadAllText("test2.cev");

			//var parser = new Praser();
			//parser.Prase(programText);

			//Console.WriteLine();
			//Console.WriteLine();

			var manager = new CommandLineManager(args, [new CompileCommandExecutor()]);
			manager.DefineExecutor();

		}
	}
}
