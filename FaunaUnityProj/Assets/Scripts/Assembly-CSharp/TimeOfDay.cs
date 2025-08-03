using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.HighDefinition;

public class TimeOfDay : NetworkBehaviour
{
	[Header("Time")]
	public SelectableLevel currentLevel;

	public float globalTimeSpeedMultiplier = 1f;

	public float currentDayTime;

	public int hour;

	private int previousHour;

	public float normalizedTimeOfDay;

	public bool timeHasStarted;

	[Space(5f)]
	public float globalTime;

	public float globalTimeAtEndOfDay;

	public bool movingGlobalTimeForward;

	[Space(10f)]
	private bool reachedQuota;

	public QuotaSettings quotaVariables;

	public int profitQuota;

	public int quotaFulfilled;

	public int timesFulfilledQuota;

	public float timeUntilDeadline;

	public int daysUntilDeadline;

	public int hoursUntilDeadline;

	[Space(5f)]
	public float lengthOfHours = 100f;

	public int numberOfHours = 7;

	public float totalTime;

	public const int startingGlobalTime = 100;

	[Space(3f)]
	public float shipLeaveAutomaticallyTime = 0.996f;

	[Space(5f)]
	public bool currentDayTimeStarted;

	private bool timeStartedThisFrame = true;

	public StartOfRound playersManager;

	public Animator sunAnimator;

	public Light sunIndirect;

	public Light sunDirect;

	public bool insideLighting = true;

	public DayMode dayMode;

	private DayMode dayModeLastTimePlayerWasOutside;

	public AudioClip[] timeOfDayCues;

	public AudioSource TimeOfDayMusic;

	private HDAdditionalLightData indirectLightData;

	[Header("Weather")]
	public WeatherEffect[] effects;

	public LevelWeatherType currentLevelWeather = LevelWeatherType.None;

	public float currentWeatherVariable;

	public float currentWeatherVariable2;

	[Space(2f)]
	public LocalVolumetricFog foggyWeather;

	[Space(4f)]
	public CompanyMood currentCompanyMood;

	public CompanyMood[] CommonCompanyMoods;

	[Space(4f)]
	private float changeHUDTimeInterval;

	private float nextTimeSync;

	public bool shipLeavingAlertCalled;

	public DialogueSegment[] shipLeavingSoonDialogue;

	public DialogueSegment[] shipLeavingEarlyDialogue;

	private bool shipLeavingOnMidnight;

	private bool shipFullCapacityAtMidnightMessage;

	private Coroutine playDelayedMusicCoroutine;

	public int votesForShipToLeaveEarly;

	public bool votedShipToLeaveEarlyThisRound;

	public UnityEvent onTimeSync = new UnityEvent();

	public UnityEvent onHourChanged = new UnityEvent();

	public float meteorShowerAtTime = -1f;

	public MeteorShowers MeteorWeather;

	public int overrideMeteorChance = -1;

	public List<int> furniturePlacedAtQuotaStart = new List<int>();

	public float luckValue;

	public bool hasShownAdThisQuota;

	public float normalizedTimeToShowAd = -1f;

	private float adWaitInterval = 10f;

	private int gotInfoFromClientsForAd;

	private bool checkingIfClientsAreReadyForAd;

	private bool doClientsMeetRequirementsForAd;

	private Coroutine adGetClientInfoCoroutine;

	private Collider[] playerColliders = new Collider[4];

	public static TimeOfDay Instance { get; private set; }

	public void SetWeatherBasedOnVariables()
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 101);
		if (currentLevelWeather == LevelWeatherType.Foggy)
		{
			foggyWeather.parameters.meanFreePath = random.Next((int)currentWeatherVariable, (int)currentWeatherVariable2);
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			return;
		}
		UnityEngine.Object.Destroy(Instance.gameObject);
		Instance = this;
	}

	private void Start()
	{
		playersManager = UnityEngine.Object.FindObjectOfType<StartOfRound>();
		totalTime = lengthOfHours * (float)numberOfHours;
		SetCompanyMood();
		try
		{
			hasShownAdThisQuota = ES3.Load("ShownAdThisQuota", GameNetworkManager.Instance.currentSaveFileName, defaultValue: false);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	public void DecideRandomDayEvents()
	{
		if (base.IsServer)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 28);
			int num = 7;
			if ((float)overrideMeteorChance != -1f)
			{
				num = overrideMeteorChance;
			}
			if (random.Next(0, 1000) < num)
			{
				meteorShowerAtTime = (float)random.Next(5, 80) / 100f;
			}
			else
			{
				meteorShowerAtTime = -1f;
			}
		}
	}

	private void Update()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		_ = movingGlobalTimeForward;
		if (currentDayTimeStarted)
		{
			if (timeStartedThisFrame)
			{
				timeStartedThisFrame = false;
				nextTimeSync = 0f;
				TimeOfDayMusic.volume = 0.7f;
				dayModeLastTimePlayerWasOutside = DayMode.None;
				shipLeavingOnMidnight = false;
				shipLeavingAlertCalled = false;
				votedShipToLeaveEarlyThisRound = false;
				shipLeaveAutomaticallyTime = 0.998f;
				votesForShipToLeaveEarly = 0;
				currentDayTime = CalculatePlanetTime(currentLevel);
				hour = (int)(currentDayTime / lengthOfHours);
				previousHour = hour;
				indirectLightData = null;
				globalTimeAtEndOfDay = globalTime + (totalTime - currentDayTime) / currentLevel.DaySpeedMultiplier;
				normalizedTimeOfDay = currentDayTime / totalTime;
				SetTimeForAdToPlay();
				RefreshClockUI();
				if (base.IsServer)
				{
					DecideRandomDayEvents();
				}
				timeHasStarted = true;
			}
			else
			{
				MoveTimeOfDay();
				TimeOfDayEvents();
				SetWeatherEffects();
			}
		}
		else
		{
			timeStartedThisFrame = true;
			timeHasStarted = false;
			if (MeteorWeather.meteorsEnabled)
			{
				MeteorWeather.ResetMeteorWeather();
			}
		}
	}

	public void MoveGlobalTime()
	{
		float num = globalTime;
		globalTime = Mathf.Clamp(globalTime + Time.deltaTime * globalTimeSpeedMultiplier, 0f, globalTimeAtEndOfDay);
		num = globalTime - num;
		timeUntilDeadline -= num;
	}

	public float CalculatePlanetTime(SelectableLevel level)
	{
		return (globalTime + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (totalTime + 1f);
	}

	public float CalculatePlanetTimeClampToEndOfDay(SelectableLevel level)
	{
		return (Mathf.Clamp(globalTime, 0f, globalTimeAtEndOfDay) + level.OffsetFromGlobalTime) * level.DaySpeedMultiplier % (totalTime + 1f);
	}

	private void MoveTimeOfDay()
	{
		try
		{
			MoveGlobalTime();
			SyncGlobalTimeOnNetwork();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error updating time of day: {arg}");
		}
		currentDayTime = CalculatePlanetTime(currentLevel);
		hour = (int)(currentDayTime / lengthOfHours);
		if (hour != previousHour)
		{
			previousHour = hour;
			OnHourChanged();
			StartOfRound.Instance.SetDiscordStatusDetails();
		}
		if (sunAnimator != null)
		{
			normalizedTimeOfDay = currentDayTime / totalTime;
			sunAnimator.SetFloat("timeOfDay", Mathf.Clamp(normalizedTimeOfDay, 0f, 0.99f));
			if (changeHUDTimeInterval > 3f)
			{
				changeHUDTimeInterval = 0f;
				HUDManager.Instance.SetClock(normalizedTimeOfDay, numberOfHours);
			}
			else
			{
				changeHUDTimeInterval += Time.deltaTime;
			}
			SetInsideLightingDimness();
		}
		if (base.IsServer && meteorShowerAtTime > 0f && normalizedTimeOfDay >= meteorShowerAtTime)
		{
			meteorShowerAtTime = -1f;
			MeteorWeather.SetStartMeteorShower();
		}
	}

	public void SetInsideLightingDimness(bool doNotLerp = false, bool setValueTo = false)
	{
		if (sunDirect == null || sunIndirect == null)
		{
			return;
		}
		if (indirectLightData == null)
		{
			indirectLightData = sunIndirect.GetComponent<HDAdditionalLightData>();
		}
		HUDManager.Instance.SetClockVisible(!insideLighting);
		if (GameNetworkManager.Instance != null)
		{
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
				{
					sunDirect.enabled = !GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInsideFactory;
				}
			}
			else
			{
				sunDirect.enabled = !GameNetworkManager.Instance.localPlayerController.isInsideFactory;
			}
		}
		PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
		{
			playerControllerB = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
		}
		if (playerControllerB.isInsideFactory)
		{
			sunIndirect.enabled = false;
		}
		if (insideLighting)
		{
			indirectLightData.lightDimmer = Mathf.Lerp(indirectLightData.lightDimmer, 0f, 5f * Time.deltaTime);
			return;
		}
		sunIndirect.enabled = true;
		indirectLightData.lightDimmer = Mathf.Lerp(indirectLightData.lightDimmer, 1f, 5f * Time.deltaTime);
	}

	private int RoundUpToNearestTen(float x)
	{
		return (int)(x / 10f) * 10;
	}

	private void SyncGlobalTimeOnNetwork()
	{
		if (base.IsServer && (float)RoundUpToNearestTen(globalTime) >= nextTimeSync)
		{
			nextTimeSync = RoundUpToNearestTen(globalTime + 10f);
			SyncTimeClientRpc(globalTime, (int)timeUntilDeadline);
		}
	}

	[ClientRpc]
	public void SyncTimeClientRpc(float time, int deadline)
			{
				globalTime = time;
				timeUntilDeadline = deadline;
				onTimeSync.Invoke();
			}

	public void SetTimeForAdToPlay()
	{
		if (base.IsServer && !hasShownAdThisQuota && timesFulfilledQuota != 0)
		{
			float num = 33f;
			if (daysUntilDeadline <= 1)
			{
				num = 60f;
			}
			if ((float)UnityEngine.Random.Range(0, 100) < num)
			{
				normalizedTimeToShowAd = UnityEngine.Random.Range(0.04f, 0.7f);
			}
		}
	}

	private bool MeetsRequirementsToShowAd()
	{
		if (StartOfRound.Instance.timeSinceRoundStarted <= 15f)
		{
			return false;
		}
		if (timesFulfilledQuota == 0)
		{
			return false;
		}
		if (StartOfRound.Instance.livingPlayers <= 1)
		{
			return false;
		}
		return true;
	}

	private IEnumerator GetClientInfo()
	{
		float timeAtChecking = Time.realtimeSinceStartup;
		checkingIfClientsAreReadyForAd = true;
		doClientsMeetRequirementsForAd = true;
		Debug.Log("Ad system: Setting gotinfo to 0");
		gotInfoFromClientsForAd = 0;
		SendHostInfoForShowingAdClientRpc();
		yield return new WaitUntil(() => gotInfoFromClientsForAd >= StartOfRound.Instance.livingPlayers || Time.realtimeSinceStartup - timeAtChecking > 5f);
		if (doClientsMeetRequirementsForAd)
		{
			hasShownAdThisQuota = true;
			HUDManager.Instance.ChooseAdItem();
			normalizedTimeToShowAd = -1f;
		}
		else
		{
			checkingIfClientsAreReadyForAd = false;
			adWaitInterval = 15f;
		}
		adGetClientInfoCoroutine = null;
	}

	[ClientRpc]
	public void SendHostInfoForShowingAdClientRpc()
{		Debug.Log("Received client rpc Ad");
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			return;
		}
		bool doesntMeetRequirements = false;
		float num = Mathf.Max(GameNetworkManager.Instance.localPlayerController.timeSinceTakingDamage, GameNetworkManager.Instance.localPlayerController.timeSinceFearLevelUp);
		if (num > 12f)
		{
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory && Physics.CheckSphere(GameNetworkManager.Instance.localPlayerController.transform.position, 20f, 524288, QueryTriggerInteraction.Collide))
			{
				doesntMeetRequirements = true;
				Debug.Log("Can't show ad client; 1");
			}
		}
		else if (num < 1.7f && GameNetworkManager.Instance.localPlayerController.isInsideFactory && Physics.CheckSphere(GameNetworkManager.Instance.localPlayerController.transform.position, 12f, 524288, QueryTriggerInteraction.Collide))
		{
			doesntMeetRequirements = true;
			Debug.Log("Can't show ad client; 2");
		}
		ReceiveInfoFromClientForShowingAdServerRpc(doesntMeetRequirements);
}
	[ServerRpc(RequireOwnership = false)]
	public void ReceiveInfoFromClientForShowingAdServerRpc(bool doesntMeetRequirements)
{if(checkingIfClientsAreReadyForAd)		{
			Debug.Log("Adding 1 to gotInfoFromClients");
			gotInfoFromClientsForAd++;
			if (doesntMeetRequirements)
			{
				doClientsMeetRequirementsForAd = false;
			}
		}
}
	private void DisplayAdAtScheduledTime()
	{
		if (!base.IsServer || hasShownAdThisQuota || normalizedTimeToShowAd == -1f || normalizedTimeOfDay >= 0.9f || checkingIfClientsAreReadyForAd || StartOfRound.Instance.livingPlayers <= 1)
		{
			return;
		}
		if (adWaitInterval <= 0f)
		{
			if (!(normalizedTimeOfDay > normalizedTimeToShowAd))
			{
				return;
			}
			if (MeetsRequirementsToShowAd())
			{
				if (!checkingIfClientsAreReadyForAd && adGetClientInfoCoroutine == null)
				{
					adGetClientInfoCoroutine = StartCoroutine(GetClientInfo());
				}
			}
			else
			{
				adWaitInterval = 15f;
			}
		}
		else
		{
			adWaitInterval -= Time.deltaTime;
		}
	}

	public void TimeOfDayEvents()
	{
		dayMode = GetDayPhase(currentDayTime / totalTime);
		if (currentLevel.planetHasTime && !StartOfRound.Instance.shipIsLeaving)
		{
			if (!shipLeavingAlertCalled && currentDayTime / totalTime > 0.9f)
			{
				shipLeavingAlertCalled = true;
				HUDManager.Instance.ReadDialogue(shipLeavingSoonDialogue);
				HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
			}
			DisplayAdAtScheduledTime();
			if (base.IsServer && !shipLeavingOnMidnight && currentDayTime / totalTime >= shipLeaveAutomaticallyTime)
			{
				shipLeavingOnMidnight = true;
				SetShipToLeaveOnMidnightClientRpc();
			}
		}
		if (dayMode > dayModeLastTimePlayerWasOutside)
		{
			PlayerSeesNewTimeOfDay();
		}
	}

	public void CalculateLuckValue()
	{
		if (timesFulfilledQuota == 0)
		{
			AutoParentToShip[] array = UnityEngine.Object.FindObjectsByType<AutoParentToShip>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].unlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
				{
					furniturePlacedAtQuotaStart.Add(array[i].unlockableID);
				}
			}
		}
		luckValue = 0f;
		for (int j = 0; j < furniturePlacedAtQuotaStart.Count; j++)
		{
			if (furniturePlacedAtQuotaStart[j] > StartOfRound.Instance.unlockablesList.unlockables.Count)
			{
				Debug.LogError($"'Lucky' furniture with id {furniturePlacedAtQuotaStart[j]} exceeded the unlockables list size; skipping");
			}
			luckValue = Mathf.Clamp(luckValue + StartOfRound.Instance.unlockablesList.unlockables[furniturePlacedAtQuotaStart[j]].luckValue, -0.5f, 1f);
		}
		Debug.Log($"Luck calculated: {luckValue}");
	}

	public void SetNewProfitQuota()
	{
		if (!base.IsServer)
		{
			return;
		}
		timesFulfilledQuota++;
		int num = quotaFulfilled - profitQuota;
		float num2 = Mathf.Clamp(1f + (float)timesFulfilledQuota * ((float)timesFulfilledQuota / quotaVariables.increaseSteepness), 0f, 10000f);
		CalculateLuckValue();
		float num3 = UnityEngine.Random.Range(0f, 1f);
		Debug.Log($"Randomizer amount before: {num3}");
		num3 *= Mathf.Abs(luckValue - 1f);
		Debug.Log($"Randomizer amount after: {num3}");
		num2 = quotaVariables.baseIncrease * num2 * (quotaVariables.randomizerCurve.Evaluate(num3) * quotaVariables.randomizerMultiplier + 1f);
		Debug.Log($"Amount to increase quota:{num2}");
		profitQuota = (int)Mathf.Clamp((float)profitQuota + num2, 0f, 1E+09f);
		quotaFulfilled = 0;
		timeUntilDeadline = totalTime * 4f;
		int overtimeBonus = num / 5 + 15 * daysUntilDeadline;
		furniturePlacedAtQuotaStart.Clear();
		AutoParentToShip[] array = UnityEngine.Object.FindObjectsByType<AutoParentToShip>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].unlockableID != -1 && StartOfRound.Instance.unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
			{
				furniturePlacedAtQuotaStart.Add(array[i].unlockableID);
			}
		}
		hasShownAdThisQuota = false;
		SyncNewProfitQuotaClientRpc(profitQuota, overtimeBonus, timesFulfilledQuota);
	}

	[ClientRpc]
	public void SyncNewProfitQuotaClientRpc(int newProfitQuota, int overtimeBonus, int fulfilledQuota)
			{
				quotaFulfilled = 0;
				profitQuota = newProfitQuota;
				timeUntilDeadline = totalTime * (float)quotaVariables.deadlineDaysAmount;
				timesFulfilledQuota = fulfilledQuota;
				StartOfRound.Instance.companyBuyingRate = 0.3f;
				Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
				terminal.groupCredits = Mathf.Clamp(terminal.groupCredits + overtimeBonus, terminal.groupCredits, 100000000);
				terminal.RotateShipDecorSelection();
				HUDManager.Instance.DisplayNewDeadline(overtimeBonus);
			}

	public void UpdateProfitQuotaCurrentTime()
	{
		daysUntilDeadline = (int)Mathf.Floor(timeUntilDeadline / totalTime);
		hoursUntilDeadline = (int)(timeUntilDeadline / lengthOfHours) - daysUntilDeadline * numberOfHours;
		if (StartOfRound.Instance.isChallengeFile)
		{
			StartOfRound.Instance.deadlineMonitorBGImage.color = new Color(0.5294118f, 1f / 51f, 0.8f, 1f);
			StartOfRound.Instance.profitQuotaMonitorBGImage.color = new Color(0.5294118f, 1f / 51f, 0.8f, 1f);
			StartOfRound.Instance.deadlineMonitorText.text = "AS MUCH PROFIT AS POSSIBLE";
			StartOfRound.Instance.profitQuotaMonitorText.text = "Welcome to\n" + GameNetworkManager.Instance.GetNameForWeekNumber();
			StartOfRound.Instance.profitQuotaMonitorText.fontSize = 62f;
		}
		else
		{
			if (timeUntilDeadline <= 0f)
			{
				StartOfRound.Instance.deadlineMonitorText.text = "DEADLINE:\n NOW";
			}
			else
			{
				StartOfRound.Instance.deadlineMonitorText.text = $"DEADLINE:\n{daysUntilDeadline} Days";
			}
			StartOfRound.Instance.profitQuotaMonitorText.text = $"PROFIT QUOTA:\n${quotaFulfilled} / ${profitQuota}";
		}
	}

	[ClientRpc]
	public void SetShipToLeaveOnMidnightClientRpc()
			{
				StartOfRound.Instance.ShipLeaveAutomatically(leavingOnMidnight: true);
			}

	public void VoteShipToLeaveEarly()
	{
		if (!votedShipToLeaveEarlyThisRound)
		{
			votedShipToLeaveEarlyThisRound = true;
			SetShipLeaveEarlyServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetShipLeaveEarlyServerRpc()
{		{
			votesForShipToLeaveEarly++;
			int num = StartOfRound.Instance.connectedPlayersAmount + 1 - StartOfRound.Instance.livingPlayers;
			if (votesForShipToLeaveEarly >= num)
			{
				SetShipLeaveEarlyClientRpc(normalizedTimeOfDay + 0.1f, votesForShipToLeaveEarly);
			}
			else
			{
				AddVoteForShipToLeaveEarlyClientRpc();
			}
		}
}
	[ClientRpc]
	public void AddVoteForShipToLeaveEarlyClientRpc()
{if(!base.IsServer)			{
				votesForShipToLeaveEarly++;
				HUDManager.Instance.SetShipLeaveEarlyVotesText(votesForShipToLeaveEarly);
			}
}
	[ClientRpc]
	public void SetShipLeaveEarlyClientRpc(float timeToLeaveEarly, int votes)
			{
				votesForShipToLeaveEarly = votes;
				HUDManager.Instance.SetShipLeaveEarlyVotesText(votes);
				shipLeaveAutomaticallyTime = timeToLeaveEarly;
				shipLeavingAlertCalled = true;
				shipLeavingEarlyDialogue[0].bodyText = "WARNING! Please return by " + HUDManager.Instance.SetClock(timeToLeaveEarly, numberOfHours, createNewLine: false) + ". A vote has been cast, and the autopilot ship will leave early.";
				HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
				HUDManager.Instance.shipLeavingEarlyIcon.enabled = true;
			}

	[ClientRpc]
	public void ShipFullCapacityMidnightClientRpc()
			{
				shipLeavingEarlyDialogue[0].bodyText = "ALERT! The ship has reached full carrying capacity and cannot leave until items are removed!";
				HUDManager.Instance.ReadDialogue(shipLeavingEarlyDialogue);
			}

	public DayMode GetDayPhase(float time)
	{
		if (time >= 0.9f)
		{
			return DayMode.Midnight;
		}
		if (time >= 0.63f)
		{
			return DayMode.Sundown;
		}
		if (time >= 0.33f)
		{
			return DayMode.Noon;
		}
		return DayMode.Dawn;
	}

	private void PlayerSeesNewTimeOfDay()
	{
		if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory && !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && playersManager.shipHasLanded)
		{
			dayModeLastTimePlayerWasOutside = dayMode;
			HUDManager.Instance.SetClockIcon(dayMode);
			if (currentLevel.planetHasTime)
			{
				PlayTimeMusicDelayed(timeOfDayCues[(int)dayMode], 0.5f, playRandomDaytimeMusic: true);
			}
		}
	}

	public void PlayTimeMusicDelayed(AudioClip clip, float delay, bool playRandomDaytimeMusic = false)
	{
		if (playDelayedMusicCoroutine != null)
		{
			Debug.Log("Already playing music; cancelled starting new music");
		}
		else
		{
			playDelayedMusicCoroutine = StartCoroutine(playSoundDelayed(clip, delay, playRandomDaytimeMusic));
		}
	}

	private IEnumerator playSoundDelayed(AudioClip clip, float delay, bool playRandomDaytimeMusic)
	{
		Debug.Log("Play time of day sfx");
		yield return new WaitForSeconds(delay);
		TimeOfDayMusic.PlayOneShot(clip, 1f);
		Debug.Log($"Play music!; {TimeOfDayMusic.clip}; {TimeOfDayMusic.volume}");
		if (!playRandomDaytimeMusic || !currentLevel.planetHasTime)
		{
			yield break;
		}
		yield return new WaitForSeconds(3f);
		yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 8f));
		if (insideLighting || GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom || StartOfRound.Instance.fearLevel > 0.03f)
		{
			yield break;
		}
		if (UnityEngine.Random.Range(0, 100) < 20 || ES3.Load("TimesLanded", "LCGeneralSaveData", 0) <= 1)
		{
			if (ES3.Load("TimesLanded", "LCGeneralSaveData", 0) <= 1)
			{
				ES3.Save("TimesLanded", 2, "LCGeneralSaveData");
			}
			SoundManager.Instance.PlayRandomOutsideMusic(dayMode >= DayMode.Sundown);
		}
		playDelayedMusicCoroutine = null;
	}

	private IEnumerator fadeOutEffect(WeatherEffect effect, Vector3 moveFromPosition)
	{
		if (effect.effectObject != null)
		{
			for (int i = 0; i < 270; i++)
			{
				effect.effectObject.transform.position = Vector3.Lerp(effect.effectObject.transform.position, moveFromPosition - Vector3.up * 50f, (float)i / 270f);
				yield return null;
				if (effect.effectObject == null || !effect.transitioning)
				{
					yield break;
				}
			}
		}
		DisableWeatherEffect(effect);
	}

	private void SetWeatherEffects()
	{
		Vector3 vector = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead) ? StartOfRound.Instance.localPlayerController.transform.position : StartOfRound.Instance.spectateCamera.transform.position);
		for (int i = 0; i < effects.Length; i++)
		{
			if (effects[i].effectEnabled)
			{
				if (!string.IsNullOrEmpty(effects[i].sunAnimatorBool) && sunAnimator != null)
				{
					sunAnimator.SetBool(effects[i].sunAnimatorBool, value: true);
				}
				effects[i].transitioning = false;
				if (effects[i].effectObject != null)
				{
					effects[i].effectObject.SetActive(value: true);
					if (effects[i].lerpPosition)
					{
						effects[i].effectObject.transform.position = Vector3.Lerp(effects[i].effectObject.transform.position, vector, Time.deltaTime);
					}
					else
					{
						effects[i].effectObject.transform.position = vector;
					}
				}
			}
			else if (!effects[i].transitioning)
			{
				effects[i].transitioning = true;
				if (effects[i].lerpPosition)
				{
					StartCoroutine(fadeOutEffect(effects[i], vector));
				}
				else
				{
					DisableWeatherEffect(effects[i]);
				}
			}
		}
	}

	private void DisableWeatherEffect(WeatherEffect effect)
	{
		if (!(effect.effectObject == null))
		{
			effect.effectObject.SetActive(value: false);
		}
	}

	public void DisableAllWeather(bool deactivateObjects = false)
	{
		for (int i = 0; i < effects.Length; i++)
		{
			effects[i].effectEnabled = false;
		}
		if (!deactivateObjects)
		{
			return;
		}
		for (int j = 0; j < effects.Length; j++)
		{
			if (effects[j].effectObject != null)
			{
				effects[j].effectObject.SetActive(value: false);
			}
		}
	}

	public void RefreshClockUI()
	{
		HUDManager.Instance.SetClockIcon(dayMode);
		HUDManager.Instance.SetClock(normalizedTimeOfDay, numberOfHours);
	}

	public void OnHourChanged(int amount = 1)
	{
		onHourChanged.Invoke();
	}

	public void OnDayChanged()
	{
		if (!StartOfRound.Instance.isChallengeFile)
		{
			StartOfRound.Instance.SetPlanetsWeather();
			SetBuyingRateForDay();
			SetCompanyMood();
		}
	}

	public void SetCompanyMood()
	{
		if (timesFulfilledQuota <= 0)
		{
			currentCompanyMood = CommonCompanyMoods[0];
			return;
		}
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 164);
		currentCompanyMood = CommonCompanyMoods[random.Next(0, CommonCompanyMoods.Length)];
	}

	public void SetBuyingRateForDay()
	{
		daysUntilDeadline = (int)Mathf.Floor(timeUntilDeadline / totalTime);
		if (daysUntilDeadline == 0)
		{
			StartOfRound.Instance.companyBuyingRate = 1f;
			return;
		}
		float num = 0.3f;
		float num2 = (1f - num) / (float)quotaVariables.deadlineDaysAmount;
		StartOfRound.Instance.companyBuyingRate = num2 * (float)(quotaVariables.deadlineDaysAmount - daysUntilDeadline) + num;
	}

	[ClientRpc]
	public void SetBeginMeteorShowerClientRpc()
{if(!base.IsServer)			{
				MeteorWeather.gameObject.SetActive(value: true);
				MeteorWeather.meteorsEnabled = true;
				HUDManager.Instance.MeteorShowerWarningHUD();
			}
}}
