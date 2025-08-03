using System;
using System.Collections;
using System.Linq;
using DigitalRuby.ThunderAndLightning;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class PatcherTool : GrabbableObject
{
	[Space(15f)]
	public float gunAnomalyDamage = 1f;

	public bool isShocking;

	public IShockableWithGun shockedTargetScript;

	[Space(15f)]
	public Light flashlightBulb;

	public Light flashlightBulbGlow;

	public AudioSource mainAudio;

	public AudioSource shockAudio;

	public AudioSource gunAudio;

	public AudioClip[] activateClips;

	public AudioClip[] beginShockClips;

	public AudioClip[] overheatClips;

	public AudioClip[] finishShockClips;

	public AudioClip outOfBatteriesClip;

	public AudioClip detectAnomaly;

	public AudioClip scanAnomaly;

	public Material bulbLight;

	public Material bulbDark;

	public Animator effectAnimator;

	public Animator gunAnimator;

	public ParticleSystem overheatParticle;

	private Coroutine scanGunCoroutine;

	private Coroutine beginShockCoroutine;

	public Transform aimDirection;

	private int anomalyMask = 524296;

	private int roomMask = 256;

	private RaycastHit hit;

	private Ray ray;

	public GameObject lightningObject;

	public Transform lightningDest;

	public Transform lightningBend1;

	public Transform lightningBend2;

	private Vector3 shockVectorMidpoint;

	[Header("Shock difficulty variables")]
	public float bendStrengthCap = 3f;

	public float endStrengthCap = 4.25f;

	private float currentEndStrengthCap;

	public float bendChangeSpeedMultiplier = 10f;

	public float endChangeSpeedMultiplier = 17f;

	private float currentEndChangeSpeedMultiplier;

	public float pullStrength;

	public float endPullStrength = 4.25f;

	private float currentEndPullStrength;

	public float maxChangePerFrame = 0.15f;

	public float endChangePerFrame = 2.5f;

	private float currentEndChangePerFrame;

	[HideInInspector]
	public float bendMultiplier;

	[HideInInspector]
	private float bendRandomizerShift;

	[HideInInspector]
	private Vector3 bendVector;

	public float gunOverheat;

	[HideInInspector]
	private bool sentStopShockingRPC;

	[HideInInspector]
	private bool wasShockingPreviousFrame;

	private LightningSplineScript lightningScript;

	private System.Random gunRandom;

	private int timesUsed;

	private bool lightningVisible;

	private float minigameChecksInterval;

	private float timeSpentShocking;

	private float makeAudibleNoiseTimer;

	public static int finishedShockMinigame;

	private RaycastHit[] raycastEnemies;

	private bool isScanning;

	private float currentDifficultyMultiplier;

	private PlayerControllerB previousPlayerHeldBy;

	public override void Start()
	{
		base.Start();
		raycastEnemies = new RaycastHit[12];
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (lightningDest != null && lightningDest.gameObject != null)
		{
			UnityEngine.Object.Destroy(lightningDest.gameObject);
		}
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		gunOverheat = 0f;
		if (playerHeldBy == null)
		{
			return;
		}
		if (scanGunCoroutine != null)
		{
			StopCoroutine(scanGunCoroutine);
			scanGunCoroutine = null;
		}
		if (beginShockCoroutine != null)
		{
			StopCoroutine(beginShockCoroutine);
			beginShockCoroutine = null;
		}
		if (isShocking)
		{
			Debug.Log("Stop shocking gun");
			StopShockingAnomalyOnClient(failed: true);
		}
		else if (isScanning)
		{
			SwitchFlashlight(on: false);
			gunAudio.Stop();
			currentUseCooldown = 0.5f;
			if (scanGunCoroutine != null)
			{
				StopCoroutine(scanGunCoroutine);
				scanGunCoroutine = null;
			}
			isScanning = false;
		}
		else
		{
			Debug.Log("Start scanning gun");
			isScanning = true;
			sentStopShockingRPC = false;
			scanGunCoroutine = StartCoroutine(ScanGun());
			currentUseCooldown = 0.5f;
			Debug.Log("Use patcher tool");
			PlayRandomAudio(mainAudio, activateClips);
			SwitchFlashlight(on: true);
		}
	}

	private void PlayRandomAudio(AudioSource audioSource, AudioClip[] audioClips)
	{
		if (audioClips.Length != 0)
		{
			audioSource.PlayOneShot(audioClips[UnityEngine.Random.Range(0, audioClips.Length)]);
		}
	}

	private bool GunMeetsConditionsToShock(PlayerControllerB playerUsingGun, Vector3 targetPosition, float maxAngle = 80f)
	{
		Debug.Log($"Target position: {targetPosition}");
		Vector3 position = playerUsingGun.gameplayCamera.transform.position;
		Vector3 vector = position;
		vector.y = targetPosition.y;
		if (Vector3.Angle(playerUsingGun.transform.forward, targetPosition - position) > maxAngle)
		{
			return false;
		}
		if (gunOverheat > 2f || Vector3.Distance(position, targetPosition) < 0.7f || Vector3.Distance(position, targetPosition) > 13f || Physics.Linecast(position, targetPosition, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
		{
			if (Physics.Linecast(position, targetPosition, out var hitInfo, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
			{
				Debug.Log(hitInfo.transform.name);
				Debug.Log(hitInfo.transform.gameObject.name);
				Debug.DrawLine(position, targetPosition, Color.green, 25f);
			}
			Debug.Log($"Gun not meeting conditions to zap; {gunOverheat > 2f}; {Vector3.Distance(position, targetPosition) < 0.7f}; {Physics.Linecast(position, targetPosition, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)}");
			return false;
		}
		return true;
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if (!lightningVisible)
		{
			return;
		}
		if (isShocking && shockedTargetScript != null && playerHeldBy != null && !playerHeldBy.isPlayerDead && !playerHeldBy.inAnimationWithEnemy && !insertedBattery.empty)
		{
			timeSpentShocking += Time.deltaTime / 8f;
			if (makeAudibleNoiseTimer <= 0f)
			{
				makeAudibleNoiseTimer = 0.8f;
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 20f, 0.92f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed, 11);
			}
			else
			{
				makeAudibleNoiseTimer -= Time.deltaTime;
			}
			Vector3 shockablePosition = shockedTargetScript.GetShockablePosition();
			lightningDest.position = shockablePosition + new Vector3(UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(-0.3f, 0.3f), UnityEngine.Random.Range(-0.3f, 0.3f));
			shockVectorMidpoint = Vector3.Normalize(shockedTargetScript.GetShockableTransform().position - aimDirection.position);
			bendVector = playerHeldBy.transform.right * bendMultiplier;
			lightningBend1.position = aimDirection.position + 0.3f * shockVectorMidpoint + bendVector;
			lightningBend2.position = aimDirection.position + 0.6f * shockVectorMidpoint + bendVector;
			if (bendRandomizerShift < 0f)
			{
				float num = Mathf.Clamp(RandomFloatInRange(gunRandom, -1f + Mathf.Round(bendRandomizerShift), 1f) * (bendChangeSpeedMultiplier * Time.deltaTime), 0f - maxChangePerFrame, maxChangePerFrame);
				bendMultiplier = Mathf.Clamp(bendMultiplier + num, 0f - bendStrengthCap, bendStrengthCap);
			}
			else
			{
				float num2 = Mathf.Clamp(RandomFloatInRange(gunRandom, -1f, 1f + Mathf.Round(bendRandomizerShift)) * (bendChangeSpeedMultiplier * Time.deltaTime), 0f - maxChangePerFrame, maxChangePerFrame);
				bendMultiplier = Mathf.Clamp(bendMultiplier + num2, 0f - bendStrengthCap, bendStrengthCap);
			}
			ShiftBendRandomizer();
			AdjustDifficultyValues();
			if (!base.IsOwner)
			{
				return;
			}
			wasShockingPreviousFrame = true;
			float num3 = Mathf.Abs(Mathf.Clamp(bendMultiplier * 0.5f, -0.5f, 0.5f) - playerHeldBy.shockMinigamePullPosition * 2f);
			playerHeldBy.turnCompass.Rotate(Vector3.up * 100f * bendMultiplier * pullStrength * Time.deltaTime);
			RoundManager.Instance.tempTransform.eulerAngles = new Vector3(0f, playerHeldBy.gameplayCamera.transform.eulerAngles.y, playerHeldBy.gameplayCamera.transform.eulerAngles.z);
			if (Vector3.Angle(RoundManager.Instance.tempTransform.forward, new Vector3(lightningDest.position.x, playerHeldBy.gameplayCamera.transform.position.y, lightningDest.position.z) - playerHeldBy.gameplayCamera.transform.position) > 90f)
			{
				gunOverheat += Time.deltaTime * 10f;
			}
			if (bendMultiplier < -0.3f)
			{
				if (playerHeldBy.shockMinigamePullPosition < 0f)
				{
					gunOverheat = Mathf.Clamp(gunOverheat - Time.deltaTime * 3f, 0f, 10f);
				}
				else
				{
					gunOverheat += Time.deltaTime * (num3 * 2f);
				}
				HUDManager.Instance.SetTutorialArrow(2);
			}
			else if (bendMultiplier > 0.3f)
			{
				if (playerHeldBy.shockMinigamePullPosition > 0f)
				{
					gunOverheat = Mathf.Clamp(gunOverheat - Time.deltaTime * 3f, 0f, 10f);
				}
				else
				{
					gunOverheat += Time.deltaTime * (num3 * 2f);
				}
				HUDManager.Instance.SetTutorialArrow(1);
			}
			else
			{
				HUDManager.Instance.SetTutorialArrow(0);
			}
			minigameChecksInterval -= Time.deltaTime;
			if (minigameChecksInterval <= 0f)
			{
				minigameChecksInterval = 0.15f;
				if (shockedTargetScript == null || !GunMeetsConditionsToShock(playerHeldBy, shockablePosition))
				{
					StopShockingAnomalyOnClient(failed: true);
					return;
				}
			}
			if (gunOverheat > 0.75f)
			{
				gunAudio.volume = Mathf.Lerp(gunAudio.volume, 1f, 13f * Time.deltaTime);
				gunAnimator.SetBool("Overheating", value: true);
			}
			else
			{
				gunAudio.volume = Mathf.Lerp(gunAudio.volume, 0f, 7f * Time.deltaTime);
				gunAnimator.SetBool("Overheating", value: false);
			}
		}
		else if (wasShockingPreviousFrame)
		{
			wasShockingPreviousFrame = false;
			timeSpentShocking = 0f;
			if (base.IsOwner)
			{
				StopShockingAnomalyOnClient();
			}
		}
	}

	private void AdjustDifficultyValues()
	{
		bendStrengthCap = Mathf.Lerp(0.4f, currentEndStrengthCap, timeSpentShocking * currentDifficultyMultiplier);
		bendChangeSpeedMultiplier = Mathf.Lerp(3.5f, currentEndChangeSpeedMultiplier, timeSpentShocking * currentDifficultyMultiplier);
		pullStrength = Mathf.Lerp(0.4f, currentEndPullStrength, timeSpentShocking * currentDifficultyMultiplier);
		maxChangePerFrame = Mathf.Lerp(0.13f, currentEndChangePerFrame, timeSpentShocking * currentDifficultyMultiplier);
		lightningScript.Forkedness = Mathf.Lerp(0.11f, 0.45f, timeSpentShocking * currentDifficultyMultiplier);
		lightningScript.ForkLengthMultiplier = Mathf.Lerp(0.11f, 1.1f, timeSpentShocking * currentDifficultyMultiplier);
		lightningScript.ForkLengthVariance = Mathf.Lerp(0.08f, 4f, timeSpentShocking * currentDifficultyMultiplier);
		shockAudio.volume = Mathf.Lerp(0.1f, 1f, timeSpentShocking * currentDifficultyMultiplier);
	}

	private void InitialDifficultyValues()
	{
		currentEndStrengthCap = SetCurrentDifficultyValue(endStrengthCap, 1.4f);
		currentEndChangeSpeedMultiplier = SetCurrentDifficultyValue(endChangeSpeedMultiplier, 7f);
		currentEndPullStrength = SetCurrentDifficultyValue(endPullStrength, 0.4f);
		currentEndChangePerFrame = SetCurrentDifficultyValue(endChangePerFrame, 0.12f);
	}

	private float SetCurrentDifficultyValue(float max, float min)
	{
		return shockedTargetScript.GetDifficultyMultiplier() * (max - min) + min;
	}

	public void ShiftBendRandomizer()
	{
		if (bendMultiplier < 0f)
		{
			if (bendMultiplier < -0.5f)
			{
				bendRandomizerShift += 1f * Time.deltaTime;
			}
			else
			{
				bendRandomizerShift -= 1f * Time.deltaTime;
			}
		}
		else if (bendMultiplier > 0.5f)
		{
			bendRandomizerShift -= 1f * Time.deltaTime;
		}
		else
		{
			bendRandomizerShift += 1f * Time.deltaTime;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(waitForStartOfRoundInstance());
	}

	private IEnumerator waitForStartOfRoundInstance()
	{
		yield return new WaitUntil(() => StartOfRound.Instance != null && StartOfRound.Instance.CameraSwitchEvent != null && StartOfRound.Instance.activeCamera != null);
		StartOfRound.Instance.CameraSwitchEvent.AddListener(OnSwitchCamera);
		if (StartOfRound.Instance != null && StartOfRound.Instance.activeCamera != null)
		{
			lightningScript = lightningObject.GetComponent<LightningSplineScript>();
			lightningScript.Camera = StartOfRound.Instance.activeCamera;
		}
	}

	private void OnDisable()
	{
		StartOfRound.Instance.CameraSwitchEvent.RemoveListener(OnSwitchCamera);
	}

	private void OnSwitchCamera()
	{
		lightningObject.GetComponent<LightningSplineScript>().Camera = StartOfRound.Instance.activeCamera;
	}

	private IEnumerator ScanGun()
	{
		effectAnimator.SetTrigger("Scan");
		gunAudio.PlayOneShot(scanAnomaly);
		lightningScript = lightningObject.GetComponent<LightningSplineScript>();
		lightningDest.SetParent(null);
		lightningBend1.SetParent(null);
		lightningBend2.SetParent(null);
		Debug.Log("Scan A");
		for (int i = 0; i < 12; i++)
		{
			if (base.IsOwner)
			{
				Debug.Log("Scan B");
				if (isPocketed)
				{
					yield break;
				}
				ray = new Ray(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * 3f, playerHeldBy.gameplayCamera.transform.forward);
				Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position - playerHeldBy.gameplayCamera.transform.forward * 3f, playerHeldBy.gameplayCamera.transform.forward * 6f, Color.red, 5f);
				int num = Physics.SphereCastNonAlloc(ray, 5f, raycastEnemies, 5f, anomalyMask, QueryTriggerInteraction.Collide);
				raycastEnemies = raycastEnemies.OrderBy((RaycastHit x) => x.distance).ToArray();
				for (int j = 0; j < num; j++)
				{
					if (j >= raycastEnemies.Length)
					{
						continue;
					}
					hit = raycastEnemies[j];
					if (!(hit.transform == null) && hit.transform.gameObject.TryGetComponent<IShockableWithGun>(out var component) && component.CanBeShocked())
					{
						Vector3 shockablePosition = component.GetShockablePosition();
						Debug.Log("Got shockable transform name : " + component.GetShockableTransform().gameObject.name);
						if (GunMeetsConditionsToShock(playerHeldBy, shockablePosition, 60f))
						{
							gunAudio.Stop();
							BeginShockingAnomalyOnClient(component);
							yield break;
						}
					}
				}
			}
			yield return new WaitForSeconds(0.125f);
		}
		Debug.Log("Zap gun light off!!!");
		SwitchFlashlight(on: false);
		isScanning = false;
	}

	public void BeginShockingAnomalyOnClient(IShockableWithGun shockableScript)
	{
		timesUsed++;
		sentStopShockingRPC = false;
		gunRandom = new System.Random(playerHeldBy.playersManager.randomMapSeed + timesUsed);
		gunOverheat = 0f;
		shockedTargetScript = shockableScript;
		currentDifficultyMultiplier = shockableScript.GetDifficultyMultiplier();
		InitialDifficultyValues();
		bendMultiplier = 0f;
		bendRandomizerShift = 0f;
		if (beginShockCoroutine != null)
		{
			StopCoroutine(beginShockCoroutine);
		}
		beginShockCoroutine = StartCoroutine(beginShockGame(shockableScript));
	}

	private IEnumerator beginShockGame(IShockableWithGun shockableScript)
	{
		if (shockableScript == null || shockableScript.GetNetworkObject() == null)
		{
			Debug.LogError($"Zap gun: The shockable script was null when starting the minigame! ; {shockableScript == null}; {shockableScript.GetNetworkObject() == null}");
			isScanning = false;
			yield break;
		}
		effectAnimator.SetTrigger("Shock");
		gunAudio.PlayOneShot(detectAnomaly);
		isShocking = true;
		isScanning = false;
		playerHeldBy.inShockingMinigame = true;
		Transform shockableTransform = shockableScript.GetShockableTransform();
		playerHeldBy.shockingTarget = shockableTransform;
		playerHeldBy.isCrouching = false;
		playerHeldBy.playerBodyAnimator.SetBool("crouching", value: false);
		playerHeldBy.turnCompass.LookAt(shockableTransform);
		Vector3 zero = Vector3.zero;
		zero.y = playerHeldBy.turnCompass.localEulerAngles.y;
		playerHeldBy.turnCompass.localEulerAngles = zero;
		yield return new WaitForSeconds(0.55f);
		StartShockAudios();
		isBeingUsed = true;
		shockedTargetScript.ShockWithGun(playerHeldBy);
		playerHeldBy.inSpecialInteractAnimation = true;
		playerHeldBy.playerBodyAnimator.SetBool("HoldPatcherTool", value: true);
		SwitchFlashlight(on: false);
		gunAnimator.SetTrigger("Shock");
		lightningObject.SetActive(value: true);
		lightningVisible = true;
		ShockPatcherToolServerRpc(shockableScript.GetNetworkObject());
	}

	private void StartShockAudios()
	{
		PlayRandomAudio(mainAudio, beginShockClips);
		gunAudio.Play();
		mainAudio.Play();
		mainAudio.volume = 1f;
		shockAudio.Play();
		shockAudio.volume = 0f;
	}

	public void StopShockingAnomalyOnClient(bool failed = false)
	{
		if (scanGunCoroutine != null)
		{
			StopCoroutine(scanGunCoroutine);
			scanGunCoroutine = null;
		}
		timeSpentShocking = 0f;
		wasShockingPreviousFrame = false;
		lightningVisible = false;
		lightningObject.SetActive(value: false);
		isBeingUsed = false;
		SwitchFlashlight(on: false);
		gunAnimator.SetBool("Overheating", value: false);
		gunAnimator.SetBool("Shock", value: false);
		if (shockedTargetScript != null)
		{
			shockedTargetScript.StopShockingWithGun();
		}
		gunOverheat = 0f;
		gunAudio.Stop();
		gunAudio.volume = 1f;
		mainAudio.Stop();
		shockAudio.Stop();
		if (base.IsOwner && playerHeldBy != null && !sentStopShockingRPC)
		{
			HUDManager.Instance.SetTutorialArrow(0);
			sentStopShockingRPC = true;
			StopShockingServerRpc();
			playerHeldBy.playerBodyAnimator.SetTrigger("Overheat");
			Vector3 localEulerAngles = playerHeldBy.thisPlayerBody.localEulerAngles;
			localEulerAngles.x = 0f;
			localEulerAngles.z = 0f;
			playerHeldBy.thisPlayerBody.localEulerAngles = localEulerAngles;
		}
		if (failed)
		{
			PlayRandomAudio(gunAudio, overheatClips);
			overheatParticle.Play();
			currentUseCooldown = 5f;
			effectAnimator.SetTrigger("FailGame");
			if (timeSpentShocking > 0.75f)
			{
				SetFinishedShockMinigameTutorial();
			}
		}
		else
		{
			currentUseCooldown = 0.25f;
			effectAnimator.SetTrigger("FinishGame");
			if (base.IsOwner)
			{
				playerHeldBy.playerBodyAnimator.SetTrigger("Overheat");
			}
			SetFinishedShockMinigameTutorial();
		}
		playerHeldBy.PlayQuickSpecialAnimation(3f);
		PlayRandomAudio(mainAudio, finishShockClips);
		if (base.IsOwner)
		{
			if (playerHeldBy != null)
			{
				playerHeldBy.playerBodyAnimator.SetBool("HoldPatcherTool", value: false);
				StartCoroutine(stopShocking(playerHeldBy));
			}
			else
			{
				Debug.LogError("Error: playerHeldBy is null for owner of zap gun when stopping shock, in client rpc");
			}
			return;
		}
		isShocking = false;
		if (playerHeldBy != null)
		{
			playerHeldBy.inSpecialInteractAnimation = false;
			playerHeldBy.inShockingMinigame = false;
		}
	}

	private void SetFinishedShockMinigameTutorial()
	{
		if (HUDManager.Instance.setTutorialArrow)
		{
			finishedShockMinigame++;
			if (finishedShockMinigame >= 2)
			{
				HUDManager.Instance.setTutorialArrow = false;
			}
		}
	}

	private IEnumerator stopShocking(PlayerControllerB playerController)
	{
		yield return new WaitForSeconds(0.4f);
		isShocking = false;
		playerController.inSpecialInteractAnimation = false;
		playerController.inShockingMinigame = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void ShockPatcherToolServerRpc(NetworkObjectReference netObject)
			{
				ShockPatcherToolClientRpc(netObject);
				Debug.Log("Patcher tool server rpc received");
			}

	[ClientRpc]
	public void ShockPatcherToolClientRpc(NetworkObjectReference netObject)
{		Debug.Log("Shock patcher tool client rpc received");
		if (base.IsOwner || previousPlayerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			return;
		}
		Debug.Log("Running shock patcher tool function");
		timesUsed++;
		gunRandom = new System.Random(playerHeldBy.playersManager.randomMapSeed + timesUsed);
		if (scanGunCoroutine != null)
		{
			StopCoroutine(scanGunCoroutine);
		}
		if (netObject.TryGet(out var networkObject))
		{
			isShocking = true;
			isScanning = false;
			shockedTargetScript = networkObject.gameObject.GetComponentInChildren<IShockableWithGun>();
			if (shockedTargetScript != null)
			{
				shockedTargetScript.ShockWithGun(playerHeldBy);
				StartShockAudios();
				lightningObject.SetActive(value: true);
				SwitchFlashlight(on: false);
				gunAnimator.SetTrigger("UseGun");
				effectAnimator.SetTrigger("Shock");
				lightningVisible = true;
				playerHeldBy.inShockingMinigame = true;
				playerHeldBy.inSpecialInteractAnimation = true;
			}
			else
			{
				Debug.LogError("Zap gun: Unable to get IShockableWithGun interface from networkobject on client rpc!");
			}
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void StopShockingServerRpc()
			{
				StopShockingClientRpc();
			}

	[ClientRpc]
	public void StopShockingClientRpc()
{		{
			Debug.Log("Running client rpc stopping shock");
			if (!(GameNetworkManager.Instance.localPlayerController == null) && !(NetworkManager.Singleton == null) && !base.IsOwner && !(previousPlayerHeldBy == GameNetworkManager.Instance.localPlayerController))
			{
				Debug.Log($"{base.IsOwner} ; {previousPlayerHeldBy}");
				StopShockingAnomalyOnClient();
			}
		}
}
	public override void UseUpBatteries()
	{
		base.UseUpBatteries();
		SwitchFlashlight(on: false);
		gunAudio.PlayOneShot(outOfBatteriesClip, 1f);
	}

	public override void PocketItem()
	{
		isBeingUsed = false;
		if (playerHeldBy != null)
		{
			DisablePatcherGun();
		}
		else
		{
			Debug.Log("Could not find what player was holding this item");
		}
		base.PocketItem();
	}

	public override void DiscardItem()
	{
		DisablePatcherGun();
		base.DiscardItem();
	}

	private void DisablePatcherGun()
	{
		SwitchFlashlight(on: false);
		if (scanGunCoroutine != null)
		{
			StopCoroutine(scanGunCoroutine);
			scanGunCoroutine = null;
		}
		if (beginShockCoroutine != null)
		{
			StopCoroutine(beginShockCoroutine);
			beginShockCoroutine = null;
		}
		if (playerHeldBy != null && isShocking)
		{
			StopShockingAnomalyOnClient(failed: true);
		}
		isBeingUsed = false;
		wasShockingPreviousFrame = false;
	}

	public override void EquipItem()
	{
		base.EquipItem();
		if (playerHeldBy != null)
		{
			previousPlayerHeldBy = playerHeldBy;
		}
	}

	public void SwitchFlashlight(bool on)
	{
		flashlightBulb.enabled = on;
		flashlightBulbGlow.enabled = on;
	}

	private float RandomFloatInRange(System.Random rand, float min, float max)
	{
		return (float)(rand.NextDouble() * (double)(max - min) + (double)min);
	}
}
