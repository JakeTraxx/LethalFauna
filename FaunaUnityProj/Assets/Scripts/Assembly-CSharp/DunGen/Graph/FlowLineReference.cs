using System;

namespace DunGen.Graph
{
	[Serializable]
	public sealed class FlowLineReference : FlowGraphObjectReference
	{
		public GraphLine Line
		{
			get
			{
				return flow.Lines[index];
			}
			set
			{
				index = flow.Lines.IndexOf(value);
			}
		}

		public FlowLineReference(DungeonFlow flowGraph, GraphLine line)
		{
			flow = flowGraph;
			Line = line;
		}
	}
}
