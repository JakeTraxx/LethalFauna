using System;
using System.Collections.Generic;

namespace DunGen.Graph
{
	[Serializable]
	public class GraphNode
	{
		public DungeonFlow Graph;

		public List<TileSet> TileSets = new List<TileSet>();

		public NodeType NodeType;

		public float Position;

		public string Label;

		public List<KeyLockPlacement> Keys = new List<KeyLockPlacement>();

		public List<KeyLockPlacement> Locks = new List<KeyLockPlacement>();

		public NodeLockPlacement LockPlacement;

		public GraphNode(DungeonFlow graph)
		{
			Graph = graph;
		}
	}
}
