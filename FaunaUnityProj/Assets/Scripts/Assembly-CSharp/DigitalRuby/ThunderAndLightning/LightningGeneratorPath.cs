using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class LightningGeneratorPath : LightningGenerator
	{
		public static readonly LightningGeneratorPath PathGeneratorInstance = new LightningGeneratorPath();

		public void GenerateLightningBoltPath(LightningBolt bolt, Vector3 start, Vector3 end, LightningBoltParameters parameters)
		{
			if (parameters.Points.Count < 2)
			{
				Debug.LogError("Lightning path should have at least two points");
				return;
			}
			int generations = parameters.Generations;
			int totalGenerations = generations;
			float num = ((generations == parameters.Generations) ? parameters.ChaosFactor : parameters.ChaosFactorForks);
			int num2 = parameters.SmoothingFactor - 1;
			LightningBoltSegmentGroup lightningBoltSegmentGroup = bolt.AddGroup();
			lightningBoltSegmentGroup.LineWidth = parameters.TrunkWidth;
			lightningBoltSegmentGroup.Generation = generations--;
			lightningBoltSegmentGroup.EndWidthMultiplier = parameters.EndWidthMultiplier;
			lightningBoltSegmentGroup.Color = parameters.Color;
			if (generations == parameters.Generations && (parameters.MainTrunkTintColor.r != byte.MaxValue || parameters.MainTrunkTintColor.g != byte.MaxValue || parameters.MainTrunkTintColor.b != byte.MaxValue || parameters.MainTrunkTintColor.a != byte.MaxValue))
			{
				lightningBoltSegmentGroup.Color.r = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.r * (float)(int)parameters.MainTrunkTintColor.r);
				lightningBoltSegmentGroup.Color.g = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.g * (float)(int)parameters.MainTrunkTintColor.g);
				lightningBoltSegmentGroup.Color.b = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.b * (float)(int)parameters.MainTrunkTintColor.b);
				lightningBoltSegmentGroup.Color.a = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.a * (float)(int)parameters.MainTrunkTintColor.a);
			}
			parameters.Start = parameters.Points[0] + start;
			parameters.End = parameters.Points[parameters.Points.Count - 1] + end;
			end = parameters.Start;
			for (int i = 1; i < parameters.Points.Count; i++)
			{
				start = end;
				end = parameters.Points[i];
				Vector3 vector = end - start;
				float num3 = PathGenerator.SquareRoot(vector.sqrMagnitude);
				if (num > 0f)
				{
					if (bolt.CameraMode == CameraMode.Perspective)
					{
						end += num3 * num * RandomDirection3D(parameters.Random);
					}
					else if (bolt.CameraMode == CameraMode.OrthographicXY)
					{
						end += num3 * num * RandomDirection2D(parameters.Random);
					}
					else
					{
						end += num3 * num * RandomDirection2DXZ(parameters.Random);
					}
					vector = end - start;
				}
				lightningBoltSegmentGroup.Segments.Add(new LightningBoltSegment
				{
					Start = start,
					End = end
				});
				float offsetAmount = num3 * num;
				RandomVector(bolt, ref start, ref end, offsetAmount, parameters.Random, out var result);
				if (ShouldCreateFork(parameters, generations, totalGenerations))
				{
					Vector3 vector2 = vector * parameters.ForkMultiplier() * num2 * 0.5f;
					Vector3 end2 = end + vector2 + result;
					GenerateLightningBoltStandard(bolt, start, end2, generations, totalGenerations, 0f, parameters);
				}
				if (--num2 == 0)
				{
					num2 = parameters.SmoothingFactor - 1;
				}
			}
		}

		protected override void OnGenerateLightningBolt(LightningBolt bolt, Vector3 start, Vector3 end, LightningBoltParameters parameters)
		{
			GenerateLightningBoltPath(bolt, start, end, parameters);
		}
	}
}
