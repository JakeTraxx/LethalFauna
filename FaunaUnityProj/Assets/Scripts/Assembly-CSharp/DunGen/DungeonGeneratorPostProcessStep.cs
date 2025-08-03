using System;

namespace DunGen
{
	public struct DungeonGeneratorPostProcessStep
	{
		public Action<DungeonGenerator> PostProcessCallback;

		public PostProcessPhase Phase;

		public int Priority;

		public DungeonGeneratorPostProcessStep(Action<DungeonGenerator> postProcessCallback, int priority, PostProcessPhase phase)
		{
			PostProcessCallback = postProcessCallback;
			Priority = priority;
			Phase = phase;
		}
	}
}
