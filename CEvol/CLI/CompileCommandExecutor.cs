using CEvol.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CEvol.CLI
{
	internal class CompileCommandExecutor : ICommandExecutor
	{
		public bool IsDefault => true;

		public string Name => "compile";

		private readonly List<string> _rawInputPaths = new();
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
					if (currentArgumentType == null) throw new InvalidOperationException("Аргумент передан без флага.");
					ExecuteConsumeArgument(currentArgumentType, arg);
				}
			}

			// Разворачиваем маски и путевые паттерны (*.cev и т.д.)
			var resolvedFiles = ResolveInputFiles(_rawInputPaths);

			if (resolvedFiles.Count < 1)
				throw new InvalidOperationException("Не найдено ни одного файла для компиляции.");

			if (string.IsNullOrWhiteSpace(_outputFile))
				_outputFile = "output.exe";

			var compiler = new Compiler();
			compiler.Execute(resolvedFiles, _outputFile);

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
					_rawInputPaths.Add(argument);
					return true;
				case "--output":
					_outputFile = argument;
					return true;
				default:
					return false;
			}
		}

		private List<string> ResolveInputFiles(IEnumerable<string> paths)
		{
			var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var path in paths)
			{
				if (path.Contains('*') || path.Contains('?'))
				{
					string directory = Path.GetDirectoryName(path);
					if (string.IsNullOrEmpty(directory))
					{
						directory = Directory.GetCurrentDirectory();
					}

					string searchPattern = Path.GetFileName(path);
					if (Directory.Exists(directory))
					{
						var files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
						foreach (var f in files)
						{
							result.Add(Path.GetFullPath(f));
						}
					}
				}
				else if (File.Exists(path))
				{
					result.Add(Path.GetFullPath(path));
				}
				else
				{
					Console.WriteLine($"Предупреждение: Файл не найден: {path}");
				}
			}

			return result.ToList();
		}
	}
}