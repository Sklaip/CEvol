using CEvol.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.CLI
{
	internal class CompileCommandExecutor : ICommandExecutor
	{
		public bool IsDefault => true;

		public string Name => "compile";

		private List<string> _inputFiles = new();
		private string? _outputFile = null;

		public string Execute(IEnumerable<string> arguments)
		{
			string? currentArgumentType = null;
			foreach (var arg in arguments)
			{
				if (IsArgumentType(arg))
				{
					currentArgumentType = arg;
				}
				else
				{
					if (currentArgumentType == null) throw new NotImplementedException();
					ExecuteConsumeArgument(currentArgumentType, arg);
				}
			}

			if (_inputFiles.Count < 1 || string.IsNullOrWhiteSpace(_outputFile))
				throw new NotImplementedException();

			var compiler = new Compiler();
			compiler.Execute(_inputFiles[0], _outputFile);

			return "successfully";
		}

		private bool IsArgumentType(string argument)
		{
			return argument == "--input" || argument == "--output";
		}

		private bool ExecuteConsumeArgument(string argumentType, string argument)
		{
			switch (argumentType)
			{
				case "--input":
					_inputFiles.Add(argument);
					return true;
				case "--output":
					_outputFile = argument;
					return true;
				default:
					return false;
			}
		}
	}
}
