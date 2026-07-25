using CEvol.Analysis.Members.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Analysis.Members
{
	internal class MembersTable
	{
		public readonly Dictionary<string, FuncDesc[]> Functions;
		public readonly Dictionary<string, TypeDesc> Types;

		public MembersTable(Dictionary<string, FuncDesc[]> functions, Dictionary<string, TypeDesc> types)
		{
			Functions = functions;
			Types = types;
		}

		public void Merge(MembersTable other)
		{
			foreach (var func in other.Functions)
			{
				Functions[func.Key] = func.Value;
			}

			foreach (var type in other.Types)
			{
				Types[type.Key] = type.Value;
			}
		}
	}
}
