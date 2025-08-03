using System;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class LightningGenerator
	{
		internal const float oneOver255 = 0.003921569f;

		internal const float mainTrunkMultiplier = 0.003921569f;

		public static readonly LightningGenerator GeneratorInstance = new LightningGenerator();

		private void GetPerpendicularVector(ref Vector3 directionNormalized, out Vector3 side)
		{
			if (directionNormalized == Vector3.zero)
			{
				side = Vector3.right;
				return;
			}
			float x = directionNormalized.x;
			float y = directionNormalized.y;
			float z = directionNormalized.z;
			float num = Mathf.Abs(x);
			float num2 = Mathf.Abs(y);
			float num3 = Mathf.Abs(z);
			float num4;
			float num5;
			float num6;
			if (num >= num2 && num2 >= num3)
			{
				num4 = 1f;
				num5 = 1f;
				num6 = (0f - (y * num4 + z * num5)) / x;
			}
			else if (num2 >= num3)
			{
				num6 = 1f;
				num5 = 1f;
				num4 = (0f - (x * num6 + z * num5)) / y;
			}
			else
			{
				num6 = 1f;
				num4 = 1f;
				num5 = (0f - (x * num6 + y * num4)) / z;
			}
			side = new Vector3(num6, num4, num5).normalized;
		}

		protected virtual void OnGenerateLightningBolt(LightningBolt bolt, Vector3 start, Vector3 end, LightningBoltParameters parameters)
		{
			GenerateLightningBoltStandard(bolt, start, end, parameters.Generations, parameters.Generations, 0f, parameters);
		}

		public bool ShouldCreateFork(LightningBoltParameters parameters, int generation, int totalGenerations)
		{
			if (generation > parameters.generationWhereForksStop && generation >= totalGenerations - parameters.forkednessCalculated)
			{
				return (float)parameters.Random.NextDouble() < parameters.Forkedness;
			}
			return false;
		}

		public void CreateFork(LightningBolt bolt, LightningBoltParameters parameters, int generation, int totalGenerations, Vector3 start, Vector3 midPoint)
		{
			if (ShouldCreateFork(parameters, generation, totalGenerations))
			{
				Vector3 vector = (midPoint - start) * parameters.ForkMultiplier();
				Vector3 end = midPoint + vector;
				GenerateLightningBoltStandard(bolt, midPoint, end, generation, totalGenerations, 0f, parameters);
			}
		}

		public void GenerateLightningBoltStandard(LightningBolt bolt, Vector3 start, Vector3 end, int generation, int totalGenerations, float offsetAmount, LightningBoltParameters parameters)
		{
			if (generation < 1)
			{
				return;
			}
			LightningBoltSegmentGroup lightningBoltSegmentGroup = bolt.AddGroup();
			lightningBoltSegmentGroup.Segments.Add(new LightningBoltSegment
			{
				Start = start,
				End = end
			});
			float num = (float)generation / (float)totalGenerations;
			num *= num;
			lightningBoltSegmentGroup.LineWidth = parameters.TrunkWidth * num;
			lightningBoltSegmentGroup.Generation = generation;
			lightningBoltSegmentGroup.Color = parameters.Color;
			if (generation == parameters.Generations && (parameters.MainTrunkTintColor.r != byte.MaxValue || parameters.MainTrunkTintColor.g != byte.MaxValue || parameters.MainTrunkTintColor.b != byte.MaxValue || parameters.MainTrunkTintColor.a != byte.MaxValue))
			{
				lightningBoltSegmentGroup.Color.r = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.r * (float)(int)parameters.MainTrunkTintColor.r);
				lightningBoltSegmentGroup.Color.g = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.g * (float)(int)parameters.MainTrunkTintColor.g);
				lightningBoltSegmentGroup.Color.b = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.b * (float)(int)parameters.MainTrunkTintColor.b);
				lightningBoltSegmentGroup.Color.a = (byte)(0.003921569f * (float)(int)lightningBoltSegmentGroup.Color.a * (float)(int)parameters.MainTrunkTintColor.a);
			}
			lightningBoltSegmentGroup.Color.a = (byte)(255f * num);
			lightningBoltSegmentGroup.EndWidthMultiplier = parameters.EndWidthMultiplier * parameters.ForkEndWidthMultiplier;
			if (offsetAmount <= 0f)
			{
				offsetAmount = (end - start).magnitude * ((generation == totalGenerations) ? parameters.ChaosFactor : parameters.ChaosFactorForks);
			}
			while (generation-- > 0)
			{
				int startIndex = lightningBoltSegmentGroup.StartIndex;
				lightningBoltSegmentGroup.StartIndex = lightningBoltSegmentGroup.Segments.Count;
				for (int i = startIndex; i < lightningBoltSegmentGroup.StartIndex; i++)
				{
					start = lightningBoltSegmentGroup.Segments[i].Start;
					end = lightningBoltSegmentGroup.Segments[i].End;
					Vector3 vector = (start + end) * 0.5f;
					RandomVector(bolt, ref start, ref end, offsetAmount, parameters.Random, out var result);
					vector += result;
					lightningBoltSegmentGroup.Segments.Add(new LightningBoltSegment
					{
						Start = start,
						End = vector
					});
					lightningBoltSegmentGroup.Segments.Add(new LightningBoltSegment
					{
						Start = vector,
						End = end
					});
					CreateFork(bolt, parameters, generation, totalGenerations, start, vector);
				}
				offsetAmount *= 0.5f;
			}
		}

		public Vector3 RandomDirection3D(System.Random random)
		{
			float num = 2f * (float)random.NextDouble() - 1f;
			Vector3 result = RandomDirection2D(random) * Mathf.Sqrt(1f - num * num);
			result.z = num;
			return result;
		}

		public Vector3 RandomDirection2D(System.Random random)
		{
			float f = (float)random.NextDouble() * 2f * MathF.PI;
			return new Vector3(Mathf.Cos(f), Mathf.Sin(f), 0f);
		}

		public Vector3 RandomDirection2DXZ(System.Random random)
		{
			float f = (float)random.NextDouble() * 2f * MathF.PI;
			return new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
		}

		public void RandomVector(LightningBolt bolt, ref Vector3 start, ref Vector3 end, float offsetAmount, System.Random random, out Vector3 result)
		{
			if (bolt.CameraMode == CameraMode.Perspective)
			{
				Vector3 directionNormalized = (end - start).normalized;
				Vector3 side = Vector3.Cross(start, end);
				if (side == Vector3.zero)
				{
					GetPerpendicularVector(ref directionNormalized, out side);
				}
				else
				{
					side.Normalize();
				}
				float num = ((float)random.NextDouble() + 0.1f) * offsetAmount;
				float num2 = (float)random.NextDouble() * MathF.PI;
				directionNormalized *= (float)Math.Sin(num2);
				Quaternion quaternion = default(Quaternion);
				quaternion.x = directionNormalized.x;
				quaternion.y = directionNormalized.y;
				quaternion.z = directionNormalized.z;
				quaternion.w = (float)Math.Cos(num2);
				result = quaternion * side * num;
			}
			else if (bolt.CameraMode == CameraMode.OrthographicXY)
			{
				end.z = start.z;
				Vector3 normalized = (end - start).normalized;
				Vector3 vector = new Vector3(0f - normalized.y, normalized.x, 0f);
				float num3 = (float)random.NextDouble() * offsetAmount * 2f - offsetAmount;
				result = vector * num3;
			}
			else
			{
				end.y = start.y;
				Vector3 normalized2 = (end - start).normalized;
				Vector3 vector2 = new Vector3(0f - normalized2.z, 0f, normalized2.x);
				float num4 = (float)random.NextDouble() * offsetAmount * 2f - offsetAmount;
				result = vector2 * num4;
			}
		}

		public void GenerateLightningBolt(LightningBolt bolt, LightningBoltParameters parameters)
		{
			GenerateLightningBolt(bolt, parameters, out var _, out var _);
		}

		public void GenerateLightningBolt(LightningBolt bolt, LightningBoltParameters parameters, out Vector3 start, out Vector3 end)
		{
			start = parameters.ApplyVariance(parameters.Start, parameters.StartVariance);
			end = parameters.ApplyVariance(parameters.End, parameters.EndVariance);
			OnGenerateLightningBolt(bolt, start, end, parameters);
		}
	}
}
