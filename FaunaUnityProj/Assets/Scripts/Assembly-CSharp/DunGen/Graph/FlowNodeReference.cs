using System;

namespace DunGen.Graph
{
	[Serializable]
	public sealed class FlowNodeReference : FlowGraphObjectReference
	{
		public GraphNode Node
		{
			get
			{
				return flow.Nodes[index];
			}
			set
			{
				index = flow.Nodes.IndexOf(value);
			}
		}

		public FlowNodeReference(DungeonFlow flowGraph, GraphNode node)
		{
			flow = flowGraph;
			Node = node;
		}
	}
}
