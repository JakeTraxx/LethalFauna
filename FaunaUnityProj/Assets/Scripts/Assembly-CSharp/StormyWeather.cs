using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DigitalRuby.ThunderAndLightning;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class StormyWeather : MonoBehaviour
{
	private float randomThunderTime;

	private float timeAtLastStrike;

	private Vector3 lastRandomStrikePosition;

	private System.Random seed;

	public AudioClip[] strikeSFX;

	public AudioClip[] distantThunderSFX;

	public LightningBoltPrefabScript randomThunder;

	public LightningBoltPrefabScript targetedThunder;

	public AudioSource randomStrikeAudio;

	public AudioSource randomStrikeAudioB;

	private bool lastStrikeAudioUsed;

	public AudioSource targetedStrikeAudio;

	private RaycastHit rayHit;

	private GameObject[] outsideNodes;

	private NavMeshHit navHit;

	public ParticleSystem explosionEffectParticle;

	private List<GrabbableObject> metalObjects = new List<GrabbableObject>();

	private GrabbableObject targetingMetalObject;

	private float getObjectToTargetInterval;

	private float strikeMetalObjectTimer;

	private bool hasShownStrikeWarning;

	public ParticleSystem staticElectricityParticle;

	private GameObject setStaticToObject;

	private GrabbableObject setStaticGrabbableObject;

	public AudioClip staticElectricityAudio;

	private float lastGlobalTimeUsed;

	private float globalTimeAtLastStrike;

	private System.Random targetedThunderRandom;

	private void OnEnable()
	{
		TimeOfDay.Instance.onTimeSync.AddListener(OnGlobalTimeSync);
		BeginDay();
	}

	private void OnDisable()
	{
		TimeOfDay.Instance.onTimeSync.RemoveListener(OnGlobalTimeSync);
	}

	public void BeginDay()
	{
		lastRandomStrikePosition = Vector3.zero;
		targetedThunderRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
		globalTimeAtLastStrike = TimeOfDay.Instance.globalTime;
		lastGlobalTimeUsed = 0f;
		randomThunderTime = TimeOfDay.Instance.globalTime + 7f;
		timeAtLastStrike = TimeOfDay.Instance.globalTime;
		navHit = default(NavMeshHit);
		outsideNodes = (from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
			orderby x.transform.position.x + x.transform.position.z
			select x).ToArray();
		if (StartOfRound.Instance.spectateCamera.enabled)
		{
			SwitchCamera(StartOfRound.Instance.spectateCamera);
		}
		else
		{
			SwitchCamera(GameNetworkManager.Instance.localPlayerController.gameplayCamera);
		}
		seed = new System.Random(StartOfRound.Instance.randomMapSeed);
		DetermineNextStrikeInterval();
		StartCoroutine(GetMetalObjectsAfterDelay());
	}

	private void OnGlobalTimeSync()
	{
		float num = RoundUpToNearestTen(TimeOfDay.Instance.globalTime);
		if (num != lastGlobalTimeUsed)
		{
			lastGlobalTimeUsed = num;
			seed = new System.Random((int)num + StartOfRound.Instance.randomMapSeed);
			timeAtLastStrike = TimeOfDay.Instance.globalTime;
		}
	}

	private IEnumerator GetMetalObjectsAfterDelay()
	{
		yield return new WaitForSeconds(15f);
		GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].itemProperties.isConductiveMetal)
			{
				metalObjects.Add(array[i]);
			}
		}
	}

	public void SwitchCamera(Camera newCamera)
	{
		randomThunder.Camera = newCamera;
		targetedThunder.Camera = newCamera;
	}

	private void DetermineNextStrikeInterval()
	{
		timeAtLastStrike = randomThunderTime;
		float num = seed.Next(-5, 110);
		randomThunderTime += Mathf.Clamp(num * 0.25f, 0.6f, 110f) / Mathf.Clamp(TimeOfDay.Instance.currentWeatherVariable, 1f, 100f);
	}

	private int RoundUpToNearestTen(float x)
	{
		return (int)(x / 10f) * 10;
	}

	private void Update()
	{
		if (!base.gameObject.activeInHierarchy)
		{
			return;
		}
		if (TimeOfDay.Instance.globalTime > randomThunderTime)
		{
			LightningStrikeRandom();
			DetermineNextStrikeInterval();
		}
		if (setStaticToObject != null && setStaticGrabbableObject != null)
		{
			if (setStaticGrabbableObject.isInFactory || setStaticGrabbableObject.targetFloorPosition.x == 3000f)
			{
				staticElectricityParticle.Stop();
			}
			staticElectricityParticle.transform.position = setStaticToObject.transform.position;
		}
		if (!RoundManager.Instance.IsOwner)
		{
			return;
		}
		if (targetingMetalObject == null)
		{
			if (metalObjects.Count <= 0)
			{
				return;
			}
			if (getObjectToTargetInterval <= 4f)
			{
				getObjectToTargetInterval += Time.deltaTime;
				return;
			}
			hasShownStrikeWarning = false;
			strikeMetalObjectTimer = Mathf.Clamp(UnityEngine.Random.Range(1f, 28f), 0f, 20f);
			getObjectToTargetInterval = 0f;
			float num = 1000f;
			for (int i = 0; i < metalObjects.Count; i++)
			{
				if (metalObjects[i] == null)
				{
					metalObjects.RemoveAt(i);
				}
				else
				{
					if (metalObjects[i].isInFactory || metalObjects[i].targetFloorPosition.x == 3000f || metalObjects[i].isInShipRoom)
					{
						continue;
					}
					for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
					{
						if (StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
						{
							float num2 = Vector3.Distance(metalObjects[i].transform.position, StartOfRound.Instance.allPlayerScripts[j].transform.position);
							if (num2 < num)
							{
								targetingMetalObject = metalObjects[i];
								num = num2;
								break;
							}
						}
					}
					if (UnityEngine.Random.Range(0, 100) < 20)
					{
						break;
					}
				}
			}
			if (targetingMetalObject != null && targetingMetalObject.isHeld)
			{
				strikeMetalObjectTimer = Mathf.Clamp(strikeMetalObjectTimer + Time.deltaTime, 4f, 20f);
			}
			return;
		}
		strikeMetalObjectTimer -= Time.deltaTime;
		if (strikeMetalObjectTimer <= 0f)
		{
			if (!targetingMetalObject.isInFactory && targetingMetalObject.targetFloorPosition.x != 3000f)
			{
				RoundManager.Instance.LightningStrikeServerRpc(targetingMetalObject.transform.position);
			}
			getObjectToTargetInterval = 5f;
			targetingMetalObject = null;
		}
		else if (strikeMetalObjectTimer < 10f && !hasShownStrikeWarning)
		{
			hasShownStrikeWarning = true;
			float timeLeft = Mathf.Abs(strikeMetalObjectTimer - 10f);
			RoundManager.Instance.ShowStaticElectricityWarningServerRpc(targetingMetalObject.gameObject.GetComponent<NetworkObject>(), timeLeft);
		}
	}

	public void SetStaticElectricityWarning(NetworkObject warningObject, float particleTime)
	{
		setStaticToObject = warningObject.gameObject;
		GrabbableObject component = warningObject.gameObject.GetComponent<GrabbableObject>();
		if (component != null)
		{
			setStaticGrabbableObject = warningObject.gameObject.GetComponent<GrabbableObject>();
			for (int i = 0; i < GameNetworkManager.Instance.localPlayerController.ItemSlots.Length; i++)
			{
				if (GameNetworkManager.Instance.localPlayerController.ItemSlots[i] == component)
				{
					HUDManager.Instance.DisplayTip("ALERT!", "Drop your metallic items now! A static charge has been detected. You have seconds left to live.", isWarning: true, useSave: true, "LC_LightningTip");
				}
			}
		}
		ParticleSystem.ShapeModule shape = staticElectricityParticle.shape;
		shape.meshRenderer = setStaticToObject.GetComponentInChildren<MeshRenderer>();
		staticElectricityParticle.time = particleTime;
		staticElectricityParticle.Play();
		staticElectricityParticle.time = particleTime;
		staticElectricityParticle.gameObject.GetComponent<AudioSource>().clip = staticElectricityAudio;
		staticElectricityParticle.gameObject.GetComponent<AudioSource>().Play();
		staticElectricityParticle.gameObject.GetComponent<AudioSource>().time = particleTime;
	}

	private void LightningStrikeRandom()
	{
		Vector3 randomNavMeshPositionInBoxPredictable;
		if (seed.Next(0, 100) < 60 && (randomThunderTime - timeAtLastStrike) * TimeOfDay.Instance.currentWeatherVariable < 3f)
		{
			randomNavMeshPositionInBoxPredictable = lastRandomStrikePosition;
		}
		else
		{
			int num = seed.Next(0, outsideNodes.Length);
			if (outsideNodes == null || outsideNodes[num] == null)
			{
				outsideNodes = (from x in GameObject.FindGameObjectsWithTag("OutsideAINode")
					orderby x.transform.position.x + x.transform.position.z
					select x).ToArray();
				num = seed.Next(0, outsideNodes.Length);
			}
			randomNavMeshPositionInBoxPredictable = outsideNodes[num].transform.position;
			randomNavMeshPositionInBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(randomNavMeshPositionInBoxPredictable, 15f, navHit, seed);
		}
		lastRandomStrikePosition = randomNavMeshPositionInBoxPredictable;
		LightningStrike(randomNavMeshPositionInBoxPredictable, useTargetedObject: false);
	}

	public void LightningStrike(Vector3 strikePosition, bool useTargetedObject)
	{
		System.Random random;
		AudioSource audioSource;
		LightningBoltPrefabScript lightningBoltPrefabScript;
		if (useTargetedObject)
		{
			random = targetedThunderRandom;
			staticElectricityParticle.Stop();
			staticElectricityParticle.GetComponent<AudioSource>().Stop();
			setStaticToObject = null;
			audioSource = targetedStrikeAudio;
			lightningBoltPrefabScript = targetedThunder;
		}
		else
		{
			random = new System.Random(seed.Next(0, 10000));
			audioSource = ((!lastStrikeAudioUsed) ? randomStrikeAudio : randomStrikeAudioB);
			lastStrikeAudioUsed = !lastStrikeAudioUsed;
			lightningBoltPrefabScript = randomThunder;
		}
		bool flag = false;
		Vector3 vector = Vector3.zero;
		for (int i = 0; i < 7; i++)
		{
			if (i == 6)
			{
				vector = strikePosition + Vector3.up * 80f;
			}
			else
			{
				float x = random.Next(-32, 32);
				float z = random.Next(-32, 32);
				vector = strikePosition + Vector3.up * 80f + new Vector3(x, 0f, z);
			}
			if (!Physics.Linecast(vector, strikePosition + Vector3.up * 0.5f, out rayHit, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (!Physics.Raycast(vector, strikePosition - vector, out rayHit, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				return;
			}
			strikePosition = rayHit.point;
		}
		Debug.DrawLine(vector, strikePosition, Color.red, 10f);
		lightningBoltPrefabScript.Source.transform.position = vector;
		lightningBoltPrefabScript.Destination.transform.position = strikePosition;
		lightningBoltPrefabScript.AutomaticModeSeconds = 0.2f;
		audioSource.transform.position = strikePosition + Vector3.up * 0.5f;
		Landmine.SpawnExplosion(strikePosition + Vector3.up * 0.25f, spawnExplosionEffect: false, 2.4f, 5f);
		explosionEffectParticle.transform.position = strikePosition + Vector3.up * 0.25f;
		explosionEffectParticle.Play();
		PlayThunderEffects(strikePosition, audioSource);
	}

	private void PlayThunderEffects(Vector3 strikePosition, AudioSource audio)
	{
		PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
		if (playerControllerB.isPlayerDead && playerControllerB.spectatedPlayerScript != null)
		{
			playerControllerB = playerControllerB.spectatedPlayerScript;
		}
		float num = Vector3.Distance(playerControllerB.gameplayCamera.transform.position, strikePosition);
		bool flag = false;
		if (num < 40f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
		else if (num < 110f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
		}
		else
		{
			flag = true;
		}
		AudioClip[] array = ((!flag) ? strikeSFX : distantThunderSFX);
		if (!playerControllerB.isInsideFactory)
		{
			RoundManager.PlayRandomClip(audio, array);
		}
		WalkieTalkie.TransmitOneShotAudio(audio, array[UnityEngine.Random.Range(0, array.Length)]);
		if (StartOfRound.Instance.shipBounds.bounds.Contains(strikePosition))
		{
			StartOfRound.Instance.shipAnimatorObject.GetComponent<Animator>().SetTrigger("shipShake");
			RoundManager.PlayRandomClip(StartOfRound.Instance.ship3DAudio, StartOfRound.Instance.shipCreakSFX, randomize: false);
			StartOfRound.Instance.PowerSurgeShip();
		}
	}
}
