using System;

namespace DunGen.Tags
{
	[Serializable]
	public sealed class TagPair
	{
		public Tag TagA;

		public Tag TagB;

		public TagPair()
		{
		}

		public TagPair(Tag a, Tag b)
		{
			TagA = a;
			TagB = b;
		}

		public override string ToString()
		{
			return $"{TagA.Name} <-> {TagB.Name}";
		}

		public bool Matches(Tag a, Tag b, bool twoWay)
		{
			if (twoWay)
			{
				if (!(a == TagA) || !(b == TagB))
				{
					if (a == TagB)
					{
						return b == TagA;
					}
					return false;
				}
				return true;
			}
			if (a == TagA)
			{
				return b == TagB;
			}
			return false;
		}
	}
}
