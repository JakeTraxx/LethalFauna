using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class LightningSplineScript : LightningBoltPathScriptBase
	{
		public const int MaxSplineGenerations = 5;

		[Header("Lightning Spline Properties")]
		[Tooltip("The distance hint for each spline segment. Set to <= 0 to use the generations to determine how many spline segments to use. If > 0, it will be divided by Generations before being applied. This value is a guideline and is approximate, and not uniform on the spline.")]
		public float DistancePerSegmentHint;

		private readonly List<Vector3> prevSourcePoints = new List<Vector3>(new Vector3[1] { Vector3.zero });

		private readonly List<Vector3> sourcePoints = new List<Vector3>();

		private List<Vector3> savedSplinePoints = new List<Vector3>();

		private int previousGenerations = -1;

		private float previousDistancePerSegment = -1f;

		private bool SourceChanged()
		{
			if (sourcePoints.Count != prevSourcePoints.Count)
			{
				return true;
			}
			for (int i = 0; i < sourcePoints.Count; i++)
			{
				if (sourcePoints[i] != prevSourcePoints[i])
				{
					return true;
				}
			}
			return false;
		}

		protected override void Start()
		{
			base.Start();
		}

		protected override void Update()
		{
			base.Update();
		}

		public override void CreateLightningBolt(LightningBoltParameters parameters)
		{
			if (LightningPath == null)
			{
				return;
			}
			sourcePoints.Clear();
			try
			{
				foreach (GameObject item in LightningPath)
				{
					if (item != null)
					{
						sourcePoints.Add(item.transform.position);
					}
				}
			}
			catch (NullReferenceException)
			{
				return;
			}
			if (sourcePoints.Count < 4)
			{
				Debug.LogError("To create spline lightning, you need a lightning path with at least " + 4 + " points.");
				return;
			}
			int generations = (parameters.Generations = Mathf.Clamp(Generations, 1, 5));
			Generations = generations;
			parameters.Points.Clear();
			if (previousGenerations != Generations || previousDistancePerSegment != DistancePerSegmentHint || SourceChanged())
			{
				previousGenerations = Generations;
				previousDistancePerSegment = DistancePerSegmentHint;
				PopulateSpline(parameters.Points, sourcePoints, Generations, DistancePerSegmentHint, Camera);
				prevSourcePoints.Clear();
				prevSourcePoints.AddRange(sourcePoints);
				savedSplinePoints.Clear();
				savedSplinePoints.AddRange(parameters.Points);
			}
			else
			{
				parameters.Points.AddRange(savedSplinePoints);
			}
			parameters.SmoothingFactor = (parameters.Points.Count - 1) / sourcePoints.Count;
			base.CreateLightningBolt(parameters);
		}

		protected override LightningBoltParameters OnCreateParameters()
		{
			LightningBoltParameters orCreateParameters = LightningBoltParameters.GetOrCreateParameters();
			orCreateParameters.Generator = LightningGeneratorPath.PathGeneratorInstance;
			return orCreateParameters;
		}

		public void Trigger(List<Vector3> points, bool spline)
		{
			if (points.Count >= 2)
			{
				Generations = Mathf.Clamp(Generations, 1, 5);
				LightningBoltParameters lightningBoltParameters = CreateParameters();
				lightningBoltParameters.Points.Clear();
				if (spline && points.Count > 3)
				{
					PopulateSpline(lightningBoltParameters.Points, points, Generations, DistancePerSegmentHint, Camera);
					lightningBoltParameters.SmoothingFactor = (lightningBoltParameters.Points.Count - 1) / points.Count;
				}
				else
				{
					lightningBoltParameters.Points.AddRange(points);
					lightningBoltParameters.SmoothingFactor = 1;
				}
				base.CreateLightningBolt(lightningBoltParameters);
				CreateLightningBoltsNow();
			}
		}

		public static void PopulateSpline(List<Vector3> splinePoints, List<Vector3> sourcePoints, int generations, float distancePerSegmentHit, Camera camera)
		{
			splinePoints.Clear();
			PathGenerator.Is2D = camera != null && camera.orthographic;
			if (distancePerSegmentHit > 0f)
			{
				PathGenerator.CreateSplineWithSegmentDistance(splinePoints, sourcePoints, distancePerSegmentHit / (float)generations, closePath: false);
			}
			else
			{
				PathGenerator.CreateSpline(splinePoints, sourcePoints, sourcePoints.Count * generations * generations, closePath: false);
			}
		}
	}
}
