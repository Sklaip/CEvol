using EvolZero.CLI;
using EvolZero.Parsing;
using LLVMSharp;
using LLVMSharp.Interop;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EvolZero
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
