using System.Collections.Generic;

namespace DunGen
{
	public class DungeonGraph
	{
		public readonly List<DungeonGraphNode> Nodes = new List<DungeonGraphNode>();

		public readonly List<DungeonGraphConnection> Connections = new List<DungeonGraphConnection>();

		public DungeonGraph(Dungeon dungeon)
		{
			Dictionary<Tile, DungeonGraphNode> dictionary = new Dictionary<Tile, DungeonGraphNode>();
			foreach (Tile allTile in dungeon.AllTiles)
			{
				DungeonGraphNode item = (dictionary[allTile] = new DungeonGraphNode(allTile));
				Nodes.Add(item);
			}
			foreach (DoorwayConnection connection in dungeon.Connections)
			{
				DungeonGraphConnection item2 = new DungeonGraphConnection(dictionary[connection.A.Tile], dictionary[connection.B.Tile], connection.A, connection.B);
				Connections.Add(item2);
			}
		}
	}
}
