using CEvol.Generation;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Linq;

namespace CEvol.Core.MemebersModels
{
	public readonly struct TypeSpec : IEquatable<TypeSpec>
	{
		public readonly TypeDesc Type;
		public readonly Qualifier[] Qualifiers;

		public bool QualifiersExists => Qualifiers != null && Qualifiers.Length > 0;
		public bool IsRef => QualifiersExists && Qualifiers[0].Kind == Qualifier.QKind.Reference;

		public TypeSpec()
		{
			throw new NotImplementedException();
		}

		public TypeSpec(TypeDesc type)
		{
			Type = type;
			Qualifiers = Array.Empty<Qualifier>();
		}

		public TypeSpec(TypeDesc type, Qualifier[] qualifiers)
		{
			Type = type;
			Qualifiers = qualifiers;
		}

		public bool QualifiersEquals(TypeSpec other)
		{
			return other.Qualifiers != null && Qualifiers != null && Qualifiers.SequenceEqual(other.Qualifiers);
		}


		public bool Equals(TypeSpec other)
		{
			return Type == other.Type && QualifiersEquals(other);
		}

		public bool Equals(TypeSpec other, Qualifier passQualifier)
		{
			if (Type != other.Type || other.Qualifiers == null || Qualifiers == null) return false;
			if (Qualifiers.Length < 1) throw new NotImplementedException();

			int i = 0;
			if (Qualifiers[0].Kind == passQualifier.Kind) i++;

			var segment = new ArraySegment<Qualifier>(Qualifiers, i, Qualifiers.Length - i);

			return segment.SequenceEqual(other.Qualifiers);
		}

		public override bool Equals(object obj)
		{
			return obj is TypeSpec && Equals((TypeSpec)obj);
		}

		public override int GetHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(Type);

			if (Qualifiers != null)
			{
				foreach (var t in Qualifiers)
				{
					hashCode.Add(t);
				}
			}

			return hashCode.ToHashCode();
		}
	}
}
