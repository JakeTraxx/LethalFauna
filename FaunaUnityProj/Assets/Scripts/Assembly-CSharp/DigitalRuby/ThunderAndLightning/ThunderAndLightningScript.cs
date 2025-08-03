using System;
using System.Collections;
using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class ThunderAndLightningScript : MonoBehaviour
	{
		private class LightningBoltHandler
		{
			private ThunderAndLightningScript script;

			private readonly System.Random random = new System.Random();

			public float VolumeMultiplier { get; set; }

			public LightningBoltHandler(ThunderAndLightningScript script)
			{
				this.script = script;
				CalculateNextLightningTime();
			}

			private void UpdateLighting()
			{
				if (script.lightningInProgress)
				{
					return;
				}
				if (script.ModifySkyboxExposure)
				{
					script.skyboxExposureStorm = 0.35f;
					if (script.skyboxMaterial != null && script.skyboxMaterial.HasProperty("_Exposure"))
					{
						script.skyboxMaterial.SetFloat("_Exposure", script.skyboxExposureStorm);
					}
				}
				CheckForLightning();
			}

			private void CalculateNextLightningTime()
			{
				script.nextLightningTime = DigitalRuby.ThunderAndLightning.LightningBoltScript.TimeSinceStart + script.LightningIntervalTimeRange.Random(random);
				script.lightningInProgress = false;
				if (script.ModifySkyboxExposure && script.skyboxMaterial.HasProperty("_Exposure"))
				{
					script.skyboxMaterial.SetFloat("_Exposure", script.skyboxExposureStorm);
				}
			}

			public IEnumerator ProcessLightning(Vector3? _start, Vector3? _end, bool intense, bool visible)
			{
				script.lightningInProgress = true;
				float intensity;
				float time;
				AudioClip[] sounds;
				if (intense)
				{
					float t = UnityEngine.Random.Range(0f, 1f);
					intensity = Mathf.Lerp(2f, 8f, t);
					time = 5f / intensity;
					sounds = script.ThunderSoundsIntense;
				}
				else
				{
					float t2 = UnityEngine.Random.Range(0f, 1f);
					intensity = Mathf.Lerp(0f, 2f, t2);
					time = 30f / intensity;
					sounds = script.ThunderSoundsNormal;
				}
				if (script.skyboxMaterial != null && script.ModifySkyboxExposure)
				{
					script.skyboxMaterial.SetFloat("_Exposure", Mathf.Max(intensity * 0.5f, script.skyboxExposureStorm));
				}
				Strike(_start, _end, intense, intensity, script.Camera, visible ? script.Camera : null);
				CalculateNextLightningTime();
				if (intensity >= 1f && sounds != null && sounds.Length != 0)
				{
					yield return new WaitForSecondsLightning(time);
					AudioClip audioClip;
					do
					{
						audioClip = sounds[UnityEngine.Random.Range(0, sounds.Length - 1)];
					}
					while (sounds.Length > 1 && audioClip == script.lastThunderSound);
					script.lastThunderSound = audioClip;
					script.audioSourceThunder.PlayOneShot(audioClip, intensity * 0.5f * VolumeMultiplier);
				}
			}

			private void Strike(Vector3? _start, Vector3? _end, bool intense, float intensity, Camera camera, Camera visibleInCamera)
			{
				float minInclusive = (intense ? (-1000f) : (-5000f));
				float maxInclusive = (intense ? 1000f : 5000f);
				float num = (intense ? 500f : 2500f);
				float num2 = ((UnityEngine.Random.Range(0, 2) == 0) ? UnityEngine.Random.Range(minInclusive, 0f - num) : UnityEngine.Random.Range(num, maxInclusive));
				float lightningYStart = script.LightningYStart;
				float num3 = ((UnityEngine.Random.Range(0, 2) == 0) ? UnityEngine.Random.Range(minInclusive, 0f - num) : UnityEngine.Random.Range(num, maxInclusive));
				Vector3 vector = script.Camera.transform.position;
				vector.x += num2;
				vector.y = lightningYStart;
				vector.z += num3;
				if (visibleInCamera != null)
				{
					Quaternion rotation = visibleInCamera.transform.rotation;
					visibleInCamera.transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
					float x = UnityEngine.Random.Range((float)visibleInCamera.pixelWidth * 0.1f, (float)visibleInCamera.pixelWidth * 0.9f);
					float z = UnityEngine.Random.Range(visibleInCamera.nearClipPlane + num + num, maxInclusive);
					vector = visibleInCamera.ScreenToWorldPoint(new Vector3(x, 0f, z));
					vector.y = lightningYStart;
					visibleInCamera.transform.rotation = rotation;
				}
				Vector3 vector2 = vector;
				num2 = UnityEngine.Random.Range(-100f, 100f);
				lightningYStart = ((UnityEngine.Random.Range(0, 4) == 0) ? UnityEngine.Random.Range(-1f, 600f) : (-1f));
				num3 += UnityEngine.Random.Range(-100f, 100f);
				vector2.x += num2;
				vector2.y = lightningYStart;
				vector2.z += num3;
				vector2.x += num * camera.transform.forward.x;
				vector2.z += num * camera.transform.forward.z;
				while ((vector - vector2).magnitude < 500f)
				{
					vector2.x += num * camera.transform.forward.x;
					vector2.z += num * camera.transform.forward.z;
				}
				vector = _start ?? vector;
				vector2 = _end ?? vector2;
				if (Physics.Raycast(vector, (vector - vector2).normalized, out var hitInfo, float.MaxValue))
				{
					vector2 = hitInfo.point;
				}
				int generations = script.LightningBoltScript.Generations;
				RangeOfFloats trunkWidthRange = script.LightningBoltScript.TrunkWidthRange;
				if (UnityEngine.Random.value < script.CloudLightningChance)
				{
					script.LightningBoltScript.TrunkWidthRange = default(RangeOfFloats);
					script.LightningBoltScript.Generations = 1;
				}
				script.LightningBoltScript.LightParameters.LightIntensity = intensity * 0.5f;
				script.LightningBoltScript.Trigger(vector, vector2);
				script.LightningBoltScript.TrunkWidthRange = trunkWidthRange;
				script.LightningBoltScript.Generations = generations;
			}

			private void CheckForLightning()
			{
				if (Time.time >= script.nextLightningTime)
				{
					bool intense = UnityEngine.Random.value < script.LightningIntenseProbability;
					script.StartCoroutine(ProcessLightning(null, null, intense, script.LightningAlwaysVisible));
				}
			}

			public void Update()
			{
				UpdateLighting();
			}
		}

		[Tooltip("Lightning bolt script - optional, leave null if you don't want lightning bolts")]
		public LightningBoltPrefabScript LightningBoltScript;

		[Tooltip("Camera where the lightning should be centered over. Defaults to main camera.")]
		public Camera Camera;

		[SingleLine("Random interval between strikes.")]
		public RangeOfFloats LightningIntervalTimeRange = new RangeOfFloats
		{
			Minimum = 10f,
			Maximum = 25f
		};

		[Tooltip("Probability (0-1) of an intense lightning bolt that hits really close. Intense lightning has increased brightness and louder thunder compared to normal lightning, and the thunder sounds plays a lot sooner.")]
		[Range(0f, 1f)]
		public float LightningIntenseProbability = 0.2f;

		[Tooltip("Sounds to play for normal thunder. One will be chosen at random for each lightning strike. Depending on intensity, some normal lightning may not play a thunder sound.")]
		public AudioClip[] ThunderSoundsNormal;

		[Tooltip("Sounds to play for intense thunder. One will be chosen at random for each lightning strike.")]
		public AudioClip[] ThunderSoundsIntense;

		[Tooltip("Whether lightning strikes should always try to be in the camera view")]
		public bool LightningAlwaysVisible = true;

		[Tooltip("The chance lightning will simply be in the clouds with no visible bolt")]
		[Range(0f, 1f)]
		public float CloudLightningChance = 0.5f;

		[Tooltip("Whether to modify the skybox exposure when lightning is created")]
		public bool ModifySkyboxExposure;

		[Tooltip("Base point light range for lightning bolts. Increases as intensity increases.")]
		[Range(1f, 10000f)]
		public float BaseLightRange = 2000f;

		[Tooltip("Starting y value for the lightning strikes")]
		[Range(0f, 100000f)]
		public float LightningYStart = 500f;

		[Tooltip("Volume multiplier")]
		[Range(0f, 1f)]
		public float VolumeMultiplier = 1f;

		private float skyboxExposureOriginal;

		private float skyboxExposureStorm;

		private float nextLightningTime;

		private bool lightningInProgress;

		private AudioSource audioSourceThunder;

		private LightningBoltHandler lightningBoltHandler;

		private Material skyboxMaterial;

		private AudioClip lastThunderSound;

		public float SkyboxExposureOriginal => skyboxExposureOriginal;

		public bool EnableLightning { get; set; }

		private void Start()
		{
			EnableLightning = true;
			if (Camera == null)
			{
				Camera = Camera.main;
			}
			if (RenderSettings.skybox != null)
			{
				skyboxMaterial = (RenderSettings.skybox = new Material(RenderSettings.skybox));
			}
			skyboxExposureOriginal = (skyboxExposureStorm = ((skyboxMaterial == null || !skyboxMaterial.HasProperty("_Exposure")) ? 1f : skyboxMaterial.GetFloat("_Exposure")));
			audioSourceThunder = base.gameObject.AddComponent<AudioSource>();
			lightningBoltHandler = new LightningBoltHandler(this);
			lightningBoltHandler.VolumeMultiplier = VolumeMultiplier;
		}

		private void Update()
		{
			if (lightningBoltHandler != null && EnableLightning)
			{
				lightningBoltHandler.VolumeMultiplier = VolumeMultiplier;
				lightningBoltHandler.Update();
			}
		}

		public void CallNormalLightning()
		{
			CallNormalLightning(null, null);
		}

		public void CallNormalLightning(Vector3? start, Vector3? end)
		{
			StartCoroutine(lightningBoltHandler.ProcessLightning(start, end, intense: false, visible: true));
		}

		public void CallIntenseLightning()
		{
			CallIntenseLightning(null, null);
		}

		public void CallIntenseLightning(Vector3? start, Vector3? end)
		{
			StartCoroutine(lightningBoltHandler.ProcessLightning(start, end, intense: true, visible: true));
		}
	}
}
