using System;
using System.Collections.Generic;
using System.Linq;

namespace DunGen.Graph
{
	[Serializable]
	public class GraphLine
	{
		public DungeonFlow Graph;

		public List<DungeonArchetype> DungeonArchetypes = new List<DungeonArchetype>();

		public float Position;

		public float Length;

		public List<KeyLockPlacement> Keys = new List<KeyLockPlacement>();

		public List<KeyLockPlacement> Locks = new List<KeyLockPlacement>();

		public GraphLine(DungeonFlow graph)
		{
			Graph = graph;
		}

		public DungeonArchetype GetRandomArchetype(RandomStream randomStream, IList<DungeonArchetype> usedArchetypes)
		{
			IEnumerable<DungeonArchetype> source = DungeonArchetypes.Where((DungeonArchetype a) => !a.Unique || !usedArchetypes.Contains(a));
			if (!source.Any())
			{
				source = DungeonArchetypes;
			}
			int index = randomStream.Next(0, source.Count());
			return source.ElementAt(index);
		}
	}
}
