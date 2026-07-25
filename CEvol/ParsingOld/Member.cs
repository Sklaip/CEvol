using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CEvol.Parsing
{
	internal struct Member
	{
		public string Name { get; }
		public MemberType Type { get; }

		public Member(string name)
		{
			Name = name;
			Type = MemberType.Unknown;
		}

		public Member(string name, MemberType type)
		{
			Name = name;
			Type = type;
		}

		public override readonly bool Equals([NotNullWhen(true)] object? obj)
		{
			return obj is Member other && Name.Equals(other.Name);
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}
