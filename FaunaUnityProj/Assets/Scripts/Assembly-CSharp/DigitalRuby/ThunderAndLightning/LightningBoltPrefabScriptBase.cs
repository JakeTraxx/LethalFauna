using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public abstract class LightningBoltPrefabScriptBase : LightningBoltScript
	{
		private readonly List<LightningBoltParameters> batchParameters = new List<LightningBoltParameters>();

		private readonly System.Random random = new System.Random();

		[Header("Lightning Spawn Properties")]
		[SingleLineClamp("How long to wait before creating another round of lightning bolts in seconds", 0.001, double.MaxValue)]
		public RangeOfFloats IntervalRange = new RangeOfFloats
		{
			Minimum = 0.05f,
			Maximum = 0.1f
		};

		[SingleLineClamp("How many lightning bolts to emit for each interval", 0.0, 100.0)]
		public RangeOfIntegers CountRange = new RangeOfIntegers
		{
			Minimum = 1,
			Maximum = 1
		};

		[Tooltip("Reduces the probability that additional bolts from CountRange will actually happen (0 - 1).")]
		[Range(0f, 1f)]
		public float CountProbabilityModifier = 1f;

		public RangeOfFloats DelayRange = new RangeOfFloats
		{
			Minimum = 0f,
			Maximum = 0f
		};

		[SingleLineClamp("For each bolt emitted, how long should it stay in seconds", 0.01, 10.0)]
		public RangeOfFloats DurationRange = new RangeOfFloats
		{
			Minimum = 0.06f,
			Maximum = 0.12f
		};

		[Header("Lightning Appearance Properties")]
		[SingleLineClamp("The trunk width range in unity units (x = min, y = max)", 0.0001, 100.0)]
		public RangeOfFloats TrunkWidthRange = new RangeOfFloats
		{
			Minimum = 0.1f,
			Maximum = 0.2f
		};

		[Tooltip("How long (in seconds) this game object should live before destroying itself. Leave as 0 for infinite.")]
		[Range(0f, 1000f)]
		public float LifeTime;

		[Tooltip("Generations (1 - 8, higher makes more detailed but more expensive lightning)")]
		[Range(1f, 8f)]
		public int Generations = 6;

		[Tooltip("The chaos factor that determines how far the lightning main trunk can spread out, higher numbers spread out more. 0 - 1.")]
		[Range(0f, 1f)]
		public float ChaosFactor = 0.075f;

		[Tooltip("The chaos factor that determines how far the forks of the lightning can spread out, higher numbers spread out more. 0 - 1.")]
		[Range(0f, 1f)]
		public float ChaosFactorForks = 0.095f;

		[Tooltip("Intensity of the lightning")]
		[Range(0f, 10f)]
		public float Intensity = 1f;

		[Tooltip("The intensity of the glow")]
		[Range(0f, 10f)]
		public float GlowIntensity = 0.1f;

		[Tooltip("The width multiplier for the glow, 0 - 64")]
		[Range(0f, 64f)]
		public float GlowWidthMultiplier = 4f;

		[Tooltip("What percent of time the lightning should fade in and out. For example, 0.15 fades in 15% of the time and fades out 15% of the time, with full visibility 70% of the time.")]
		[Range(0f, 0.5f)]
		public float FadePercent = 0.15f;

		[Tooltip("Modify the duration of lightning fade in.")]
		[Range(0f, 1f)]
		public float FadeInMultiplier = 1f;

		[Tooltip("Modify the duration of fully lit lightning.")]
		[Range(0f, 1f)]
		public float FadeFullyLitMultiplier = 1f;

		[Tooltip("Modify the duration of lightning fade out.")]
		[Range(0f, 1f)]
		public float FadeOutMultiplier = 1f;

		[Tooltip("0 - 1, how slowly the lightning should grow. 0 for instant, 1 for slow.")]
		[Range(0f, 1f)]
		public float GrowthMultiplier;

		[Tooltip("How much smaller the lightning should get as it goes towards the end of the bolt. For example, 0.5 will make the end 50% the width of the start.")]
		[Range(0f, 10f)]
		public float EndWidthMultiplier = 0.5f;

		[Tooltip("How forked should the lightning be? (0 - 1, 0 for none, 1 for lots of forks)")]
		[Range(0f, 1f)]
		public float Forkedness = 0.25f;

		[Range(0f, 10f)]
		[Tooltip("Minimum distance multiplier for forks")]
		public float ForkLengthMultiplier = 0.6f;

		[Range(0f, 10f)]
		[Tooltip("Fork distance multiplier variance. Random range of 0 to n that is added to Fork Length Multiplier.")]
		public float ForkLengthVariance = 0.2f;

		[Tooltip("Forks have their EndWidthMultiplier multiplied by this value")]
		[Range(0f, 10f)]
		public float ForkEndWidthMultiplier = 1f;

		[Header("Lightning Light Properties")]
		[Tooltip("Light parameters")]
		public LightningLightParameters LightParameters;

		[Tooltip("Maximum number of lights that can be created per batch of lightning")]
		[Range(0f, 64f)]
		public int MaximumLightsPerBatch = 8;

		[Header("Lightning Trigger Type")]
		[Tooltip("Manual or automatic mode. Manual requires that you call the Trigger method in script. Automatic uses the interval to create lightning continuously.")]
		public bool ManualMode;

		[Tooltip("Turns lightning into automatic mode for this number of seconds, then puts it into manual mode.")]
		[Range(0f, 120f)]
		public float AutomaticModeSeconds;

		[Header("Lightning custom transform handler")]
		[Tooltip("Custom handler to modify the transform of each lightning bolt, useful if it will be alive longer than a few frames and needs to scale and rotate based on the position of other objects.")]
		public LightningCustomTransformDelegate CustomTransformHandler;

		private float nextLightningTimestamp;

		private float lifeTimeRemaining;

		public System.Random RandomOverride { get; set; }

		private void CalculateNextLightningTimestamp(float offset)
		{
			nextLightningTimestamp = ((IntervalRange.Minimum == IntervalRange.Maximum) ? IntervalRange.Minimum : (offset + IntervalRange.Random()));
		}

		private void CustomTransform(LightningCustomTransformStateInfo state)
		{
			if (CustomTransformHandler != null)
			{
				CustomTransformHandler.Invoke(state);
			}
		}

		private void CallLightning()
		{
			CallLightning(null, null);
		}

		private void CallLightning(Vector3? start, Vector3? end)
		{
			System.Random r = RandomOverride ?? random;
			int num = CountRange.Random(r);
			for (int i = 0; i < num; i++)
			{
				LightningBoltParameters lightningBoltParameters = CreateParameters();
				if (CountProbabilityModifier >= 0.9999f || i == 0 || (float)lightningBoltParameters.Random.NextDouble() <= CountProbabilityModifier)
				{
					lightningBoltParameters.CustomTransform = ((CustomTransformHandler == null) ? null : new Action<LightningCustomTransformStateInfo>(CustomTransform));
					CreateLightningBolt(lightningBoltParameters);
					if (start.HasValue)
					{
						lightningBoltParameters.Start = start.Value;
					}
					if (end.HasValue)
					{
						lightningBoltParameters.End = end.Value;
					}
				}
				else
				{
					LightningBoltParameters.ReturnParametersToCache(lightningBoltParameters);
				}
			}
			CreateLightningBoltsNow();
		}

		protected void CreateLightningBoltsNow()
		{
			int maximumLightsPerBatch = LightningBolt.MaximumLightsPerBatch;
			LightningBolt.MaximumLightsPerBatch = MaximumLightsPerBatch;
			CreateLightningBolts(batchParameters);
			LightningBolt.MaximumLightsPerBatch = maximumLightsPerBatch;
			batchParameters.Clear();
		}

		protected override void PopulateParameters(LightningBoltParameters parameters)
		{
			base.PopulateParameters(parameters);
			parameters.RandomOverride = RandomOverride;
			float lifeTime = DurationRange.Random(parameters.Random);
			float trunkWidth = TrunkWidthRange.Random(parameters.Random);
			parameters.Generations = Generations;
			parameters.LifeTime = lifeTime;
			parameters.ChaosFactor = ChaosFactor;
			parameters.ChaosFactorForks = ChaosFactorForks;
			parameters.TrunkWidth = trunkWidth;
			parameters.Intensity = Intensity;
			parameters.GlowIntensity = GlowIntensity;
			parameters.GlowWidthMultiplier = GlowWidthMultiplier;
			parameters.Forkedness = Forkedness;
			parameters.ForkLengthMultiplier = ForkLengthMultiplier;
			parameters.ForkLengthVariance = ForkLengthVariance;
			parameters.FadePercent = FadePercent;
			parameters.FadeInMultiplier = FadeInMultiplier;
			parameters.FadeOutMultiplier = FadeOutMultiplier;
			parameters.FadeFullyLitMultiplier = FadeFullyLitMultiplier;
			parameters.GrowthMultiplier = GrowthMultiplier;
			parameters.EndWidthMultiplier = EndWidthMultiplier;
			parameters.ForkEndWidthMultiplier = ForkEndWidthMultiplier;
			parameters.DelayRange = DelayRange;
			parameters.LightParameters = LightParameters;
		}

		protected override void Start()
		{
			base.Start();
			CalculateNextLightningTimestamp(0f);
			lifeTimeRemaining = ((LifeTime <= 0f) ? float.MaxValue : LifeTime);
		}

		protected override void Update()
		{
			base.Update();
			if (Time.timeScale <= 0f)
			{
				return;
			}
			if ((lifeTimeRemaining -= LightningBoltScript.DeltaTime) < 0f)
			{
				UnityEngine.Object.Destroy(base.gameObject);
			}
			if ((nextLightningTimestamp -= LightningBoltScript.DeltaTime) <= 0f)
			{
				CalculateNextLightningTimestamp(nextLightningTimestamp);
				if (!ManualMode)
				{
					CallLightning();
				}
			}
			if (AutomaticModeSeconds > 0f)
			{
				AutomaticModeSeconds = Mathf.Max(0f, AutomaticModeSeconds - LightningBoltScript.DeltaTime);
				ManualMode = AutomaticModeSeconds == 0f;
			}
		}

		protected virtual void OnDrawGizmos()
		{
		}

		public override void CreateLightningBolt(LightningBoltParameters p)
		{
			batchParameters.Add(p);
		}

		public void Trigger()
		{
			Trigger(-1f);
		}

		public void Trigger(float seconds)
		{
			CallLightning();
			if (seconds >= 0f)
			{
				AutomaticModeSeconds = Mathf.Max(0f, seconds);
			}
		}

		public void Trigger(Vector3? start, Vector3? end)
		{
			CallLightning(start, end);
		}
	}
}
