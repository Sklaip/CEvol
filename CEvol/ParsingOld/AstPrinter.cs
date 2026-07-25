using System;
using System.Collections.Generic;
using CEvol.Parsing.Operations;

namespace CEvol.Parsing
{
	internal static class AstPrinter
	{
		// Метод для вывода всего списка корневых операций, возвращаемых Parser.Load()
		public static void PrintTree(List<IOperation> operations)
		{
			Console.WriteLine("=== AST Start ===");
			for (int i = 0; i < operations.Count; i++)
			{
				Console.WriteLine($"[Root Operation #{i}]");
				PrintNode(operations[i], "");
			}
			Console.WriteLine("=== AST End ===");
		}

		// Рекурсивный метод для вывода конкретного узла дерева
		private static void PrintNode(IOperation? operation, string indent)
		{
			if (operation == null)
			{
				return;
			}

			// Получаем понятную текстовую информацию об узле в зависимости от его реализации
			string nodeInfo = operation switch
			{
				VarCreate vc => $"VarCreate: Name='{vc.Name}', Type='{vc.Type?.Name}'",
				VarAccessing va => $"VarAccessing: MemberName='{va.Member.Name}', MemberType={va.Member.Type}",
				ValueCreate valC => $"ValueCreate: Value='{valC.Value}', Type='{valC.Type?.Name}'",
				Assign => "Assign (=)",
				Sum => "Sum (+)",
				Multiple => "Multiple (*)",
				_ => operation.GetType().Name
			};

			// Выводим текущий узел
			Console.WriteLine($"{indent}└── {nodeInfo}");

			// Выводим дочерние узлы с увеличением отступа
			string childIndent = indent + "    ";

			if (operation.LeftOpearion != null || operation.RightOpearion != null)
			{
				if (operation.LeftOpearion != null)
				{
					Console.WriteLine($"{childIndent}L:");
					PrintNode(operation.LeftOpearion, childIndent);
				}

				if (operation.RightOpearion != null)
				{
					Console.WriteLine($"{childIndent}R:");
					PrintNode(operation.RightOpearion, childIndent);
				}
			}
		}
	}
}