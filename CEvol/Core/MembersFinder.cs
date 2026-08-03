using CEvol.Core.MemebersModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core
{
	internal class MembersFinder
	{
		private readonly MembersTable _membersTable;
		private HashSet<string> _namespaces = [];

		public MembersFinder(MembersTable membersTable)
		{
			_membersTable = membersTable;
		}

		public void AddNamespace(string nameSpace)
		{
			_namespaces.Add(nameSpace);
		}

		public TypeDesc FindType(string name)
		{
			if (_membersTable.Types.TryGetValue(name, out TypeDesc typeDesc))
				return typeDesc;

			foreach (string nameSpace in _namespaces)
			{
				if (_membersTable.Types.TryGetValue($"{nameSpace}.{name}", out typeDesc))
					return typeDesc;
			}

			throw new NotImplementedException();
		}

		public FuncDesc[]? FindFunction(string name)
		{
			return FindFunction(_membersTable.Functions, name);
		}

		public FuncDesc[]? FindFunction(TypeDesc parentType, string name)
		{
			return FindFunction(parentType.Functions, name);
		}

		private FuncDesc[]? FindFunction(Dictionary<string, FuncDesc[]> functionsList, string name)
		{
			if (!functionsList.TryGetValue(name, out FuncDesc[] functions))
			{
				return null;
			}

			return functions;
		}

		public IReadOnlyCollection<ConstructorDesc> FindConstructors(TypeDesc parentType)
		{
			return parentType.Constructors;
		}
	}
}
