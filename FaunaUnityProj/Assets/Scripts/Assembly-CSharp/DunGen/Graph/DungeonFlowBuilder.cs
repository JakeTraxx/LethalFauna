using System;
using System.Collections.Generic;
using System.Linq;

namespace DunGen.Graph
{
	public sealed class DungeonFlowBuilder
	{
		private List<GraphLine> lines = new List<GraphLine>();

		private List<GraphNode> nodes = new List<GraphNode>();

		private float currentPosition;

		public DungeonFlow Flow { get; private set; }

		public DungeonFlowBuilder(DungeonFlow flow)
		{
			Flow = flow;
		}

		public DungeonFlowBuilder AddLine(DungeonArchetype archetype, float length = 1f, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			return AddLine(new DungeonArchetype[1] { archetype }, length, locks, keys);
		}

		public DungeonFlowBuilder AddLine(IEnumerable<DungeonArchetype> archetypes, float length = 1f, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			if (length <= 0f)
			{
				throw new ArgumentOutOfRangeException("Length must be grater than zero");
			}
			GraphLine graphLine = new GraphLine(Flow);
			graphLine.Position = currentPosition;
			graphLine.Length = length;
			if (archetypes != null && archetypes.Any())
			{
				graphLine.DungeonArchetypes.AddRange(archetypes);
			}
			if (locks != null && locks.Any())
			{
				graphLine.Locks.AddRange(locks);
			}
			if (keys != null && keys.Any())
			{
				graphLine.Keys.AddRange(keys);
			}
			lines.Add(graphLine);
			currentPosition += length;
			return this;
		}

		public DungeonFlowBuilder ContinueLine(float length = 1f)
		{
			if (lines.Count == 0)
			{
				throw new Exception("Cannot call ContinueLine(..) before AddLine(..)");
			}
			lines.Last().Length += length;
			currentPosition += length;
			return this;
		}

		public DungeonFlowBuilder AddNode(TileSet tileSet, string label = null, bool allowLocksOnEntrance = false, bool allowLocksOnExit = false, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			return AddNode(new TileSet[1] { tileSet }, label, allowLocksOnEntrance, allowLocksOnExit, locks, keys);
		}

		public DungeonFlowBuilder AddNode(IEnumerable<TileSet> tileSets, string label = null, bool allowLocksOnEntrance = false, bool allowLocksOnExit = false, IEnumerable<KeyLockPlacement> locks = null, IEnumerable<KeyLockPlacement> keys = null)
		{
			GraphNode graphNode = new GraphNode(Flow);
			graphNode.Label = ((label == null) ? "Node" : label);
			graphNode.Position = currentPosition;
			graphNode.NodeType = NodeType.Normal;
			if (allowLocksOnEntrance)
			{
				graphNode.LockPlacement |= NodeLockPlacement.Entrance;
			}
			if (allowLocksOnExit)
			{
				graphNode.LockPlacement |= NodeLockPlacement.Exit;
			}
			if (tileSets != null && tileSets.Any())
			{
				graphNode.TileSets.AddRange(tileSets);
			}
			if (locks != null && locks.Any())
			{
				graphNode.Locks.AddRange(locks);
			}
			if (keys != null && keys.Any())
			{
				graphNode.Keys.AddRange(keys);
			}
			nodes.Add(graphNode);
			return this;
		}

		public DungeonFlowBuilder Complete()
		{
			if (lines.Count == 0)
			{
				throw new Exception("DungeonFlowBuilder must have at least one line added before finalizing");
			}
			if (nodes.Count < 2)
			{
				throw new Exception("DungeonFlowBuilder must have at least two nodes added before finalizing");
			}
			float num = currentPosition;
			currentPosition = 1f;
			foreach (GraphLine line in lines)
			{
				line.Position /= num;
				line.Length /= num;
			}
			foreach (GraphNode node in nodes)
			{
				node.Position /= num;
			}
			nodes.First().NodeType = NodeType.Start;
			nodes.Last().NodeType = NodeType.Goal;
			Flow.Lines.Clear();
			Flow.Nodes.Clear();
			Flow.Lines.AddRange(lines);
			Flow.Nodes.AddRange(nodes);
			return this;
		}
	}
}
