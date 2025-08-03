using System;
using System.Collections.Generic;
using System.Linq;

namespace DunGen.Analysis
{
	public class GenerationAnalysis
	{
		private readonly List<GenerationStats> statsSet = new List<GenerationStats>();

		public int TargetIterationCount { get; private set; }

		public int IterationCount { get; private set; }

		public NumberSetData MainPathRoomCount { get; private set; }

		public NumberSetData BranchPathRoomCount { get; private set; }

		public NumberSetData TotalRoomCount { get; private set; }

		public NumberSetData MaxBranchDepth { get; private set; }

		public NumberSetData TotalRetries { get; private set; }

		public NumberSetData PreProcessTime { get; private set; }

		public NumberSetData MainPathGenerationTime { get; private set; }

		public NumberSetData BranchPathGenerationTime { get; private set; }

		public NumberSetData PostProcessTime { get; private set; }

		public NumberSetData TotalTime { get; private set; }

		public float AnalysisTime { get; private set; }

		public int SuccessCount { get; private set; }

		public float SuccessPercentage => (float)SuccessCount / (float)TargetIterationCount * 100f;

		public GenerationAnalysis(int targetIterationCount)
		{
			TargetIterationCount = targetIterationCount;
		}

		public void Clear()
		{
			IterationCount = 0;
			AnalysisTime = 0f;
			SuccessCount = 0;
			statsSet.Clear();
		}

		public void Add(GenerationStats stats)
		{
			statsSet.Add(stats.Clone());
			AnalysisTime += stats.TotalTime;
			IterationCount++;
		}

		public void IncrementSuccessCount()
		{
			SuccessCount++;
		}

		public void Analyze()
		{
			MainPathRoomCount = new NumberSetData(((IEnumerable<GenerationStats>)statsSet).Select((Func<GenerationStats, float>)((GenerationStats x) => x.MainPathRoomCount)));
			BranchPathRoomCount = new NumberSetData(((IEnumerable<GenerationStats>)statsSet).Select((Func<GenerationStats, float>)((GenerationStats x) => x.BranchPathRoomCount)));
			TotalRoomCount = new NumberSetData(((IEnumerable<GenerationStats>)statsSet).Select((Func<GenerationStats, float>)((GenerationStats x) => x.TotalRoomCount)));
			MaxBranchDepth = new NumberSetData(((IEnumerable<GenerationStats>)statsSet).Select((Func<GenerationStats, float>)((GenerationStats x) => x.MaxBranchDepth)));
			TotalRetries = new NumberSetData(((IEnumerable<GenerationStats>)statsSet).Select((Func<GenerationStats, float>)((GenerationStats x) => x.TotalRetries)));
			PreProcessTime = new NumberSetData(statsSet.Select((GenerationStats x) => x.PreProcessTime));
			MainPathGenerationTime = new NumberSetData(statsSet.Select((GenerationStats x) => x.MainPathGenerationTime));
			BranchPathGenerationTime = new NumberSetData(statsSet.Select((GenerationStats x) => x.BranchPathGenerationTime));
			PostProcessTime = new NumberSetData(statsSet.Select((GenerationStats x) => x.PostProcessTime));
			TotalTime = new NumberSetData(statsSet.Select((GenerationStats x) => x.TotalTime));
		}
	}
}
