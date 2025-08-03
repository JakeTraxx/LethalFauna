using System.Diagnostics;
using System.Text;
using DunGen.Analysis;
using DunGen.Graph;
using UnityEngine;

namespace DunGen.Editor
{
	[AddComponentMenu("DunGen/Analysis/Runtime Analyzer")]
	public sealed class RuntimeAnalyzer : MonoBehaviour
	{
		public DungeonFlow DungeonFlow;

		public int Iterations = 100;

		public int MaxFailedAttempts = 20;

		public bool RunOnStart = true;

		public float MaximumAnalysisTime;

		private DungeonGenerator generator = new DungeonGenerator();

		private GenerationAnalysis analysis;

		private StringBuilder infoText = new StringBuilder();

		private bool finishedEarly;

		private bool prevShouldRandomizeSeed;

		private int targetIterations;

		private int remainingIterations;

		private Stopwatch analysisTime;

		private bool generateNextFrame;

		private int currentIterations => targetIterations - remainingIterations;

		private void Start()
		{
			if (RunOnStart)
			{
				Analyze();
			}
		}

		public void Analyze()
		{
			bool flag = false;
			if (DungeonFlow == null)
			{
				UnityEngine.Debug.LogError("No DungeonFlow assigned to analyzer");
			}
			else if (Iterations <= 0)
			{
				UnityEngine.Debug.LogError("Iteration count must be greater than 0");
			}
			else if (MaxFailedAttempts <= 0)
			{
				UnityEngine.Debug.LogError("Max failed attempt count must be greater than 0");
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				prevShouldRandomizeSeed = generator.ShouldRandomizeSeed;
				generator.IsAnalysis = true;
				generator.DungeonFlow = DungeonFlow;
				generator.MaxAttemptCount = MaxFailedAttempts;
				generator.ShouldRandomizeSeed = true;
				analysis = new GenerationAnalysis(Iterations);
				analysisTime = Stopwatch.StartNew();
				remainingIterations = (targetIterations = Iterations);
				generator.OnGenerationStatusChanged += OnGenerationStatusChanged;
				generator.Generate();
			}
		}

		private void Update()
		{
			if (MaximumAnalysisTime > 0f && analysisTime.Elapsed.TotalSeconds >= (double)MaximumAnalysisTime)
			{
				remainingIterations = 0;
				finishedEarly = true;
			}
			if (generateNextFrame)
			{
				generateNextFrame = false;
				generator.Generate();
			}
		}

		private void CompleteAnalysis()
		{
			analysisTime.Stop();
			analysis.Analyze();
			UnityUtil.Destroy(generator.Root);
			OnAnalysisComplete();
		}

		private void OnGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			if (status == GenerationStatus.Complete)
			{
				analysis.IncrementSuccessCount();
				analysis.Add(generator.GenerationStats);
				remainingIterations--;
				if (remainingIterations <= 0)
				{
					generator.OnGenerationStatusChanged -= OnGenerationStatusChanged;
					CompleteAnalysis();
				}
				else
				{
					generateNextFrame = true;
				}
			}
		}

		private void OnAnalysisComplete()
		{
			generator.ShouldRandomizeSeed = prevShouldRandomizeSeed;
			infoText.Length = 0;
			if (finishedEarly)
			{
				infoText.AppendLine("[ Reached maximum analysis time before the target number of iterations was reached ]");
			}
			infoText.AppendFormat("Iterations: {0}, Max Failed Attempts: {1}", finishedEarly ? analysis.IterationCount : analysis.TargetIterationCount, MaxFailedAttempts);
			infoText.AppendFormat("\nTotal Analysis Time: {0:0.00} seconds", analysisTime.Elapsed.TotalSeconds);
			infoText.AppendFormat("\nDungeons successfully generated: {0}% ({1} failed)", Mathf.RoundToInt(analysis.SuccessPercentage), analysis.TargetIterationCount - analysis.SuccessCount);
			infoText.AppendLine();
			infoText.AppendLine();
			infoText.Append("## TIME TAKEN (in milliseconds) ##");
			infoText.AppendFormat("\n\tPre-Processing:\t\t\t\t\t{0}", analysis.PreProcessTime);
			infoText.AppendFormat("\n\tMain Path Generation:\t\t{0}", analysis.MainPathGenerationTime);
			infoText.AppendFormat("\n\tBranch Path Generation:\t\t{0}", analysis.BranchPathGenerationTime);
			infoText.AppendFormat("\n\tPost-Processing:\t\t\t\t{0}", analysis.PostProcessTime);
			infoText.Append("\n\t-------------------------------------------------------");
			infoText.AppendFormat("\n\tTotal:\t\t\t\t\t\t\t\t{0}", analysis.TotalTime);
			infoText.AppendLine();
			infoText.AppendLine();
			infoText.AppendLine("## ROOM DATA ##");
			infoText.AppendFormat("\n\tMain Path Rooms: {0}", analysis.MainPathRoomCount);
			infoText.AppendFormat("\n\tBranch Path Rooms: {0}", analysis.BranchPathRoomCount);
			infoText.Append("\n\t-------------------");
			infoText.AppendFormat("\n\tTotal: {0}", analysis.TotalRoomCount);
			infoText.AppendLine();
			infoText.AppendLine();
			infoText.AppendFormat("Retry Count: {0}", analysis.TotalRetries);
		}

		private void OnGUI()
		{
			if (analysis == null || infoText == null || infoText.Length == 0)
			{
				string text = ((analysis.SuccessCount < analysis.IterationCount) ? ("\nFailed Dungeons: " + (analysis.IterationCount - analysis.SuccessCount)) : "");
				GUILayout.Label($"Analysing... {currentIterations} / {targetIterations} ({(float)currentIterations / (float)targetIterations * 100f:0.0}%){text}");
			}
			else
			{
				GUILayout.Label(infoText.ToString());
			}
		}
	}
}
