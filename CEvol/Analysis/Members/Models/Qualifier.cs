using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Analysis.Members.Models
{
	public readonly record struct Qualifier
	{
		public enum QKind
		{
			Reference,
			Array
		}

		public readonly QKind Kind;

		public Qualifier(QKind kind)
		{
			this.Kind = kind;
		}

		public static Qualifier FromString(string str)
		{
			switch (str)
			{
				case "ref": return new Qualifier(QKind.Reference);
				case "array": return new Qualifier(QKind.Array);
				default: throw new NotImplementedException();
			}
		}

		public static Qualifier[] FromString(IEnumerable<string> str) => str.Select(FromString).ToArray();
	}
}
