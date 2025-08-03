using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dissonance;
using Dissonance.Integrations.Unity_NFGO;
using GameNetcodeStuff;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class StartOfRound : NetworkBehaviour
{
	public bool shouldApproveConnection;

	public bool allowLocalPlayerDeath = true;

	[Space(3f)]
	public int connectedPlayersAmount;

	public int thisClientPlayerId;

	public List<ulong> fullyLoadedPlayers = new List<ulong>(4);

	public int livingPlayers = 4;

	private bool mostRecentlyJoinedClient;

	public bool allPlayersDead;

	public Dictionary<ulong, int> ClientPlayerList = new Dictionary<ulong, int>();

	public List<ulong> KickedClientIds = new List<ulong>();

	public int daysPlayersSurvivedInARow;

	[Space(5f)]
	private bool hasHostSpawned;

	public bool inShipPhase = true;

	public float timeSinceRoundStarted;

	public bool shipIsLeaving;

	public bool displayedLevelResults;

	public bool newGameIsLoading;

	private int playersRevived;

	public EndOfGameStats gameStats;

	private bool localPlayerWasMostProfitableThisRound;

	[Header("Important objects")]
	public Camera spectateCamera;

	public AudioListener audioListener;

	[HideInInspector]
	public bool overrideSpectateCamera;

	public GameObject[] allPlayerObjects;

	public PlayerControllerB[] allPlayerScripts;

	public Transform[] playerSpawnPositions;

	public Transform outsideShipSpawnPosition;

	public Transform notSpawnedPosition;

	public Transform propsContainer;

	public Transform elevatorTransform;

	public Transform playersContainer;

	public Transform groundOutsideShipSpawnPosition;

	public Transform shipLandingPosition;

	public Transform outsideDoorPosition;

	public PlayerControllerB localPlayerController;

	public List<PlayerControllerB> OtherClients = new List<PlayerControllerB>();

	[Space(3f)]
	public UnlockablesList unlockablesList;

	public AudioClip changeSuitSFX;

	public GameObject suitPrefab;

	public int suitsPlaced;

	public Transform rightmostSuitPosition;

	[Space(5f)]
	public GameObject playerPrefab;

	public GameObject ragdollGrabbableObjectPrefab;

	public List<GameObject> playerRagdolls = new List<GameObject>();

	public GameObject playerBloodPrefab;

	public Transform bloodObjectsContainer;

	public Camera introCamera;

	public Camera activeCamera;

	public SimpleEvent CameraSwitchEvent = new SimpleEvent();

	public SimpleEvent StartNewRoundEvent = new SimpleEvent();

	public PlayerEvent PlayerJumpEvent = new PlayerEvent();

	public PlayerEvent PlayerMoveInputEvent = new PlayerEvent();

	public SimpleEvent LocalPlayerDamagedEvent = new SimpleEvent();

	public SimpleEvent ShipPowerSurgedEvent = new SimpleEvent();

	public GameObject testRoom;

	public GameObject testRoomPrefab;

	public Transform testRoomSpawnPosition;

	public bool localClientHasControl;

	public RuntimeAnimatorController localClientAnimatorController;

	public RuntimeAnimatorController otherClientsAnimatorController;

	public int playersMask = 8;

	[NonSerialized]
	public int collidersAndRoomMask = 1107298560;

	[NonSerialized]
	public int collidersAndRoomMaskAndPlayers = 1107298568;

	[NonSerialized]
	public int collidersAndRoomMaskAndDefault = 1107298561;

	[NonSerialized]
	public int collidersRoomMaskDefaultAndPlayers = 1107298569;

	[NonSerialized]
	public int collidersRoomDefaultAndFoliage = 1107299585;

	[NonSerialized]
	public int allPlayersCollideWithMask = -1111790665;

	[NonSerialized]
	public int walkableSurfacesMask = 1375734025;

	[Header("Physics")]
	public Collider[] PlayerPhysicsColliders;

	public List<PlayerPhysicsRegion> CurrentPlayerPhysicsRegions = new List<PlayerPhysicsRegion>();

	[Header("Ship Animations")]
	public NetworkObject shipAnimatorObject;

	public Animator shipAnimator;

	public AudioSource shipAmbianceAudio;

	public AudioSource ship3DAudio;

	public AudioClip shipDepartSFX;

	public AudioClip shipArriveSFX;

	public AudioSource shipDoorAudioSource;

	public AudioSource speakerAudioSource;

	public AudioClip suckedIntoSpaceSFX;

	public AudioClip airPressureSFX;

	public AudioClip[] shipCreakSFX;

	public AudioClip alarmSFX;

	public AudioClip firedVoiceSFX;

	public AudioClip openingHangarDoorAudio;

	public AudioClip allPlayersDeadAudio;

	public AudioClip shipIntroSpeechSFX;

	public AudioClip disableSpeakerSFX;

	public AudioClip zeroDaysLeftAlertSFX;

	public bool shipLeftAutomatically;

	public DialogueSegment[] openingDoorDialogue;

	public DialogueSegment[] gameOverDialogue;

	public DialogueSegment[] shipLeavingOnMidnightDialogue;

	public bool shipDoorsEnabled;

	public bool shipHasLanded;

	public Animator shipDoorsAnimator;

	public bool hangarDoorsClosed = true;

	private Coroutine shipTravelCoroutine;

	public ShipLights shipRoomLights;

	public AnimatedObjectTrigger closetLeftDoor;

	public AnimatedObjectTrigger closetRightDoor;

	public GameObject starSphereObject;

	public Dictionary<int, GameObject> SpawnedShipUnlockables = new Dictionary<int, GameObject>();

	public Transform gameOverCameraHandle;

	public Transform freeCinematicCameraTurnCompass;

	public Camera freeCinematicCamera;

	[Header("Players fired animation")]
	public bool firingPlayersCutsceneRunning;

	public bool suckingPlayersOutOfShip;

	private bool choseRandomFlyDirForPlayer;

	private Vector3 randomFlyDir = Vector3.zero;

	public float suckingPower;

	public bool suckingFurnitureOutOfShip;

	public Transform middleOfShipNode;

	public Transform shipDoorNode;

	public Transform middleOfSpaceNode;

	public Transform moveAwayFromShipNode;

	[Header("Level selection")]
	public GameObject currentPlanetPrefab;

	public Animator currentPlanetAnimator;

	public Animator outerSpaceSunAnimator;

	public Transform planetContainer;

	public SelectableLevel[] levels;

	public SelectableLevel currentLevel;

	public int currentLevelID;

	public bool isChallengeFile;

	public bool hasSubmittedChallengeRank;

	public int defaultPlanet;

	public bool travellingToNewLevel;

	public AnimationCurve planetsWeatherRandomCurve;

	public int maxShipItemCapacity = 45;

	public int currentShipItemCount;

	[Header("Ship Monitors")]
	public TextMeshProUGUI screenLevelDescription;

	public VideoPlayer screenLevelVideoReel;

	public TextMeshProUGUI mapScreenPlayerName;

	public Image mapScreenPlayerNameBG;

	public ManualCameraRenderer mapScreen;

	public GameObject objectCodePrefab;

	public GameObject itemRadarIconPrefab;

	public GameObject keyRadarIconPrefab;

	[Space(5f)]
	public Image deadlineMonitorBGImage;

	public Image profitQuotaMonitorBGImage;

	public TextMeshProUGUI deadlineMonitorText;

	public TextMeshProUGUI profitQuotaMonitorText;

	public GameObject upperMonitorsCanvas;

	public Canvas radarCanvas;

	[Header("Randomization")]
	public int randomMapSeed;

	public bool overrideRandomSeed;

	public int overrideSeedNumber;

	public AnimationCurve objectFallToGroundCurve;

	public AnimationCurve objectFallToGroundCurveNoBounce;

	public AnimationCurve playerSinkingCurve;

	[Header("Voice chat")]
	public DissonanceComms voiceChatModule;

	public float averageVoiceAmplitude;

	public int movingAverageLength = 20;

	public int averageCount;

	private float voiceChatNoiseCooldown;

	public bool updatedPlayerVoiceEffectsThisFrame;

	[Header("Player Audios")]
	public AudioMixerGroup playersVoiceMixerGroup;

	public FootstepSurface[] footstepSurfaces;

	public string[] naturalSurfaceTags;

	public AudioClip[] statusEffectClips;

	public AudioClip HUDSystemAlertSFX;

	public AudioClip playerJumpSFX;

	public AudioClip playerHitGroundSoft;

	public AudioClip playerHitGroundHard;

	public AudioClip damageSFX;

	public AudioClip fallDamageSFX;

	public AudioClip bloodGoreSFX;

	[Space(5f)]
	public float drowningTimer;

	[HideInInspector]
	public bool playedDrowningSFX;

	public AudioClip[] bodyCollisionSFX;

	public AudioClip playerFallDeath;

	public AudioClip hitPlayerSFX;

	private Coroutine fadeVolumeCoroutine;

	public List<DecalProjector> snowFootprintsPooledObjects = new List<DecalProjector>();

	public GameObject footprintDecal;

	public int currentFootprintIndex;

	public GameObject explosionPrefab;

	public float fearLevel;

	public bool fearLevelIncreasing;

	[Header("Company building game loop")]
	public float companyBuyingRate = 1f;

	public int hoursSinceLastCompanyVisit;

	public AudioClip companyVisitMusic;

	public bool localPlayerUsingController;

	private bool subscribedToConnectionApproval;

	public Collider shipBounds;

	public Collider shipInnerRoomBounds;

	public Collider shipStrictInnerRoomBounds;

	private Coroutine updateVoiceEffectsCoroutine;

	public ReverbPreset shipReverb;

	public AnimationCurve drunknessSpeedEffect;

	public AnimationCurve drunknessSideEffect;

	private float updatePlayerVoiceInterval;

	public Volume blackSkyVolume;

	[Space(5f)]
	public AllItemsList allItemsList;

	public InteractEvent playerTeleportedEvent;

	[Space(3f)]
	public string[] randomNames;

	public float timeAtStartOfRun;

	public float playerLookMagnitudeThisFrame;

	public float timeAtMakingLastPersonalMovement;

	public Transform[] insideShipPositions;

	public int scrapCollectedLastRound;

	[Header("Vehicles")]
	public bool magnetOn;

	public ParticleSystem magnetParticle;

	public AudioSource magnetAudio;

	public Transform magnetPoint;

	public AudioClip magnetOffAudio;

	public AudioClip magnetOnAudio;

	public bool isObjectAttachedToMagnet;

	public AnimatedObjectTrigger magnetLever;

	[Space(3f)]
	public GameObject[] VehiclesList;

	public VehicleController attachedVehicle;

	public static StartOfRound Instance { get; private set; }

	public void SetMagnetOn(bool on)
	{
		if (magnetOn != on)
		{
			magnetOn = on;
			if (on)
			{
				magnetParticle.Play(withChildren: true);
				magnetAudio.clip = magnetOnAudio;
			}
			else
			{
				isObjectAttachedToMagnet = false;
				magnetAudio.clip = magnetOffAudio;
			}
			magnetAudio.Play();
			if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, magnetAudio.transform.position) < 8f && !GameNetworkManager.Instance.localPlayerController.isInElevator)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
				GameNetworkManager.Instance.localPlayerController.externalForceAutoFade += magnetAudio.transform.position - GameNetworkManager.Instance.localPlayerController.transform.position;
			}
			SetMagnetOnServerRpc(on);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetMagnetOnServerRpc(bool on)
			{
				SetMagnetOnClientRpc(on);
			}

	[ClientRpc]
	public void SetMagnetOnClientRpc(bool on)
{if(magnetOn != on)		{
			magnetOn = on;
			if (on)
			{
				magnetParticle.Play(withChildren: true);
				magnetAudio.clip = magnetOnAudio;
			}
			else
			{
				isObjectAttachedToMagnet = false;
				magnetAudio.clip = magnetOffAudio;
			}
			magnetAudio.Play();
		}
}
	public void InstantiateFootprintsPooledObjects()
	{
		int num = 250;
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(footprintDecal, bloodObjectsContainer);
			snowFootprintsPooledObjects.Add(gameObject.GetComponent<DecalProjector>());
		}
	}

	private void ResetPooledObjects(bool destroy = false)
	{
		for (int i = 0; i < snowFootprintsPooledObjects.Count; i++)
		{
			if (!(snowFootprintsPooledObjects[i] == null))
			{
				snowFootprintsPooledObjects[i].enabled = false;
			}
		}
		if (SprayPaintItem.sprayPaintDecals == null)
		{
			return;
		}
		for (int num = SprayPaintItem.sprayPaintDecals.Count - 1; num >= 0; num--)
		{
			if ((destroy || !(SprayPaintItem.sprayPaintDecals[num] != null) || !SprayPaintItem.sprayPaintDecals[num].transform.IsChildOf(elevatorTransform)) && !(SprayPaintItem.sprayPaintDecals[num] == null))
			{
				UnityEngine.Object.Destroy(SprayPaintItem.sprayPaintDecals[num]);
				SprayPaintItem.sprayPaintDecals.RemoveAt(num);
			}
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("RadarCodeUIObject");
		for (int num2 = array.Length - 1; num2 >= 0; num2--)
		{
			UnityEngine.Object.Destroy(array[num2]);
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			timeAtStartOfRun = Time.realtimeSinceStartup;
		}
		else
		{
			UnityEngine.Object.Destroy(Instance.gameObject);
			Instance = this;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayerLoadedServerRpc(ulong clientId)
			{
				fullyLoadedPlayers.Add(clientId);
				PlayerLoadedClientRpc(clientId);
			}

	[ClientRpc]
	private void PlayerLoadedClientRpc(ulong clientId)
{if(!base.IsServer)			{
				fullyLoadedPlayers.Add(clientId);
			}
}
	[ClientRpc]
	private void ResetPlayersLoadedValueClientRpc(bool landingShip = false)
{if(base.IsServer)		{
			return;
		}
		fullyLoadedPlayers.Clear();
		if (landingShip)
		{
			if (currentPlanetAnimator != null)
			{
				currentPlanetAnimator.SetTrigger("LandOnPlanet");
			}
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.interactable = false;
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.disabledHoverTip = "[Wait for ship to land]";
		}
}
	private void SceneManager_OnLoadComplete1(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
	{
		DisableSpatializationOnAllAudio();
		if (sceneName == currentLevel.sceneName)
		{
			if (!shipDoorsEnabled)
			{
				HUDManager.Instance.loadingText.enabled = true;
				HUDManager.Instance.loadingDarkenScreen.enabled = true;
			}
			HUDManager.Instance.loadingText.text = "Waiting for crew...";
		}
		ClientPlayerList.TryGetValue(clientId, out var value);
		if (value == 0 || !base.IsServer)
		{
			PlayerLoadedServerRpc(clientId);
		}
	}

	private void SceneManager_OnUnloadComplete(ulong clientId, string sceneName)
	{
		if (sceneName == currentLevel.sceneName)
		{
			if (currentPlanetPrefab != null)
			{
				currentPlanetPrefab.SetActive(value: true);
				outerSpaceSunAnimator.gameObject.SetActive(value: true);
				currentPlanetAnimator.SetTrigger("LeavePlanet");
			}
			ClientPlayerList.TryGetValue(clientId, out var value);
			if (value == 0 || !base.IsServer)
			{
				PlayerLoadedServerRpc(clientId);
			}
		}
	}

	private void SceneManager_OnLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
	{
		Debug.Log("Loading scene");
		Debug.Log("Scene that began loading: " + sceneName);
		if (!(sceneName != "SampleSceneRelay") || !(sceneName != "MainMenu"))
		{
			return;
		}
		if (currentPlanetPrefab != null)
		{
			currentPlanetPrefab.SetActive(value: false);
		}
		outerSpaceSunAnimator.gameObject.SetActive(value: false);
		if (currentLevel.sceneName != sceneName)
		{
			for (int i = 0; i < levels.Length; i++)
			{
				if (levels[i].sceneName == sceneName)
				{
					ChangeLevel(i);
				}
			}
		}
		HUDManager.Instance.loadingText.enabled = true;
		HUDManager.Instance.loadingText.text = "LOADING WORLD...";
	}

	private void OnEnable()
	{
		Debug.Log("Enabling connection callbacks in StartOfRound");
		if (NetworkManager.Singleton != null)
		{
			Debug.Log("Began listening to SceneManager_OnLoadComplete1 on this client");
			try
			{
				NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete1;
				NetworkManager.Singleton.SceneManager.OnLoad += SceneManager_OnLoad;
				NetworkManager.Singleton.SceneManager.OnUnloadComplete += SceneManager_OnUnloadComplete;
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error returned when subscribing to scenemanager callbacks!: {arg}");
				GameNetworkManager.Instance.disconnectionReasonMessage = "An error occured when syncing the scene! The host might not have loaded in.";
				GameNetworkManager.Instance.Disconnect();
				return;
			}
			_ = base.IsServer;
		}
		else
		{
			GameNetworkManager.Instance.disconnectionReasonMessage = "Your connection timed out before you could load in. Try again?";
			GameNetworkManager.Instance.Disconnect();
		}
	}

	private void OnDisable()
	{
		Debug.Log("DISABLING connection callbacks in round manager");
		if (NetworkManager.Singleton != null)
		{
			_ = subscribedToConnectionApproval;
		}
	}

	private void Start()
	{
		Debug.Log(1107298560);
		Debug.Log(collidersAndRoomMask);
		TimeOfDay.Instance.globalTime = 100f;
		IngamePlayerSettings.Instance.RefreshAndDisplayCurrentMicrophone();
		HUDManager.Instance.SetNearDepthOfFieldEnabled(enabled: true);
		StartCoroutine(StartSpatialVoiceChat());
		NetworkObject[] array = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DontDestroyWithOwner = true;
		}
		if (base.IsServer)
		{
			SetTimeAndPlanetToSavedSettings();
			LoadUnlockables();
			LoadShipGrabbableItems();
			LoadAttachedVehicle();
			SetMapScreenInfoToCurrentLevel();
			UnityEngine.Object.FindObjectOfType<Terminal>().RotateShipDecorSelection();
			TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
			if (currentLevel.planetHasTime && timeOfDay.GetDayPhase(timeOfDay.CalculatePlanetTime(currentLevel) / timeOfDay.totalTime) == DayMode.Midnight)
			{
				UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.disabledHoverTip = "Too late on moon to land!";
				UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.interactable = false;
			}
			else
			{
				UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.interactable = true;
			}
		}
		SwitchMapMonitorPurpose(displayInfo: true);
		DisableSpatializationOnAllAudio();
		SetDiscordStatusDetails();
	}

	private void DisableSpatializationOnAllAudio()
	{
		AudioSource[] array = UnityEngine.Object.FindObjectsOfType<AudioSource>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].spatialize = false;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BuyShipUnlockableServerRpc(int unlockableID, int newGroupCreditsAmount)
{		{
			Debug.Log($"Purchasing ship unlockable on host: {unlockableID}");
			if (unlockablesList.unlockables[unlockableID].hasBeenUnlockedByPlayer || newGroupCreditsAmount > UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits)
			{
				Debug.Log("Unlockable was already unlocked! Setting group credits back to server's amount on all clients.");
				BuyShipUnlockableClientRpc(UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits);
			}
			else
			{
				UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = newGroupCreditsAmount;
				BuyShipUnlockableClientRpc(newGroupCreditsAmount, unlockableID);
				UnlockShipObject(unlockableID);
			}
		}
}
	[ClientRpc]
	public void BuyShipUnlockableClientRpc(int newGroupCreditsAmount, int unlockableID = -1)
{if(!(NetworkManager.Singleton == null) && !base.NetworkManager.ShutdownInProgress && !base.IsServer)		{
			if (unlockableID != -1)
			{
				unlockablesList.unlockables[unlockableID].hasBeenUnlockedByPlayer = true;
			}
			UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = newGroupCreditsAmount;
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void ReturnUnlockableFromStorageServerRpc(int unlockableID)
{if(!unlockablesList.unlockables[unlockableID].inStorage)		{
			return;
		}
		if (unlockablesList.unlockables[unlockableID].spawnPrefab)
		{
			if (SpawnedShipUnlockables.ContainsKey(unlockableID))
			{
				return;
			}
			PlaceableShipObject[] array = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].unlockableID == unlockableID)
				{
					return;
				}
			}
			SpawnUnlockable(unlockableID, useSavedPositionData: false);
		}
		else
		{
			PlaceableShipObject[] array2 = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
			for (int j = 0; j < array2.Length; j++)
			{
				if (array2[j].unlockableID == unlockableID)
				{
					array2[j].parentObject.disableObject = false;
					break;
				}
			}
		}
		unlockablesList.unlockables[unlockableID].inStorage = false;
		ReturnUnlockableFromStorageClientRpc(unlockableID);
}
	[ClientRpc]
	public void ReturnUnlockableFromStorageClientRpc(int unlockableID)
{if(NetworkManager.Singleton == null || base.NetworkManager.ShutdownInProgress || base.IsServer)		{
			return;
		}
		unlockablesList.unlockables[unlockableID].inStorage = false;
		PlaceableShipObject[] array = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].unlockableID == unlockableID)
			{
				array[i].parentObject.disableObject = false;
			}
		}
}
	private void UnlockShipObject(int unlockableID)
	{
		if (!unlockablesList.unlockables[unlockableID].hasBeenUnlockedByPlayer && !unlockablesList.unlockables[unlockableID].alreadyUnlocked)
		{
			Debug.Log($"Set unlockable #{unlockableID}: {unlockablesList.unlockables[unlockableID].unlockableName}, to unlocked!");
			unlockablesList.unlockables[unlockableID].hasBeenUnlockedByPlayer = true;
			SpawnUnlockable(unlockableID);
		}
	}

	private void LoadAttachedVehicle()
	{
		int num = ES3.Load("AttachedVehicleID", GameNetworkManager.Instance.currentSaveFileName, -1);
		Debug.Log($"Loaded vehicle ID: {num}");
		if (num != -1)
		{
			if (num > VehiclesList.Length)
			{
				Debug.LogError($"Saved Vehicle ID of {num} was larger than the vehicles array length. you buffoon");
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(VehiclesList[num], magnetPoint.position + magnetPoint.forward * 5f, Quaternion.identity, RoundManager.Instance.VehiclesContainer);
			attachedVehicle = gameObject.GetComponent<VehicleController>();
			isObjectAttachedToMagnet = true;
			attachedVehicle.NetworkObject.Spawn();
			magnetOn = true;
			magnetLever.initialBoolState = true;
			magnetLever.setInitialState = true;
			magnetLever.SetInitialState();
		}
	}

	private void LoadUnlockables()
	{
		try
		{
			if (ES3.KeyExists("LuckyFurniture", GameNetworkManager.Instance.currentSaveFileName))
			{
				int[] array = ES3.Load<int[]>("LuckyFurniture", GameNetworkManager.Instance.currentSaveFileName);
				if (array != null && array.Length != 0)
				{
					TimeOfDay.Instance.furniturePlacedAtQuotaStart = array.ToList();
					TimeOfDay.Instance.CalculateLuckValue();
				}
			}
			if (ES3.KeyExists("UnlockedShipObjects", GameNetworkManager.Instance.currentSaveFileName))
			{
				int[] array2 = ES3.Load<int[]>("UnlockedShipObjects", GameNetworkManager.Instance.currentSaveFileName);
				for (int i = 0; i < array2.Length; i++)
				{
					if (unlockablesList.unlockables[array2[i]].alreadyUnlocked && !unlockablesList.unlockables[array2[i]].IsPlaceable)
					{
						continue;
					}
					UnlockableItem unlockableItem = unlockablesList.unlockables[array2[i]];
					if (!unlockableItem.alreadyUnlocked)
					{
						unlockableItem.hasBeenUnlockedByPlayer = true;
					}
					Debug.Log("item name: " + unlockableItem.unlockableName);
					if (ES3.KeyExists("ShipUnlockStored_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName) && ES3.Load("ShipUnlockStored_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName, defaultValue: false))
					{
						Debug.Log("Item unlock is stored");
						if (ES3.KeyExists("ShipUnlockMoved_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName))
						{
							Debug.Log("Item was moved previously, has position/rotation data.");
							unlockableItem.placedPosition = ES3.Load("ShipUnlockPos_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName, Vector3.zero);
							unlockableItem.placedRotation = ES3.Load("ShipUnlockRot_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName, Vector3.zero);
							Debug.Log($"Item {unlockableItem.unlockableName} pos: {unlockableItem.placedPosition}; {unlockableItem.placedRotation}; spawn prefab: {unlockableItem.spawnPrefab}");
							if (!unlockableItem.spawnPrefab)
							{
								Debug.Log("Looking for corresponding object in scene");
								PlaceableShipObject[] array3 = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
								for (int j = 0; j < array3.Length; j++)
								{
									if (array3[j].unlockableID == array2[i])
									{
										Debug.Log("Placing stored object");
										ShipBuildModeManager.Instance.PlaceShipObject(unlockableItem.placedPosition, unlockableItem.placedRotation, array3[j], placementSFX: false);
									}
								}
							}
						}
						unlockableItem.inStorage = true;
					}
					else
					{
						SpawnUnlockable(array2[i]);
					}
				}
				PlaceableShipObject[] array4 = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
				for (int k = 0; k < array4.Length; k++)
				{
					if (!unlockablesList.unlockables[array4[k].unlockableID].spawnPrefab && unlockablesList.unlockables[array4[k].unlockableID].inStorage)
					{
						array4[k].parentObject.disableObject = true;
						Debug.Log("DISABLE OBJECT A");
					}
				}
			}
			for (int l = 0; l < unlockablesList.unlockables.Count; l++)
			{
				if ((l != 0 || !isChallengeFile) && (unlockablesList.unlockables[l].alreadyUnlocked || (unlockablesList.unlockables[l].unlockedInChallengeFile && isChallengeFile)) && !unlockablesList.unlockables[l].IsPlaceable)
				{
					SpawnUnlockable(l);
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error attempting to load ship unlockables on the host: {arg}");
		}
	}

	private void SpawnUnlockable(int unlockableIndex, bool useSavedPositionData = true)
	{
		GameObject gameObject = null;
		UnlockableItem unlockableItem = unlockablesList.unlockables[unlockableIndex];
		Debug.Log($"unlockable index: {unlockableIndex}; {unlockablesList.unlockables[unlockableIndex].unlockableName}");
		switch (unlockableItem.unlockableType)
		{
		case 0:
		{
			gameObject = UnityEngine.Object.Instantiate(suitPrefab, rightmostSuitPosition.position + rightmostSuitPosition.forward * 0.18f * suitsPlaced, rightmostSuitPosition.rotation, null);
			gameObject.GetComponent<UnlockableSuit>().syncedSuitID.Value = unlockableIndex;
			gameObject.GetComponent<NetworkObject>().Spawn();
			AutoParentToShip component = gameObject.gameObject.GetComponent<AutoParentToShip>();
			component.overrideOffset = true;
			component.positionOffset = new Vector3(-2.45f, 2.75f, -8.41f) + rightmostSuitPosition.forward * 0.18f * suitsPlaced;
			component.rotationOffset = new Vector3(0f, 90f, 0f);
			SyncSuitsServerRpc();
			suitsPlaced++;
			break;
		}
		case 1:
			if (unlockableItem.spawnPrefab)
			{
				Debug.Log("Instantiating prefab for item " + unlockableItem.unlockableName);
				gameObject = UnityEngine.Object.Instantiate(unlockableItem.prefabObject, elevatorTransform.position, Quaternion.identity, null);
			}
			else
			{
				Debug.Log("Placing scene object at saved position: " + unlockablesList.unlockables[unlockableIndex].unlockableName);
				PlaceableShipObject[] array = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].unlockableID == unlockableIndex)
					{
						gameObject = array[i].parentObject.gameObject;
					}
				}
				if (gameObject == null)
				{
					return;
				}
			}
			if (!useSavedPositionData && unlockablesList.unlockables[unlockableIndex].placedPosition != Vector3.zero && unlockablesList.unlockables[unlockableIndex].placedRotation != Vector3.zero)
			{
				Vector3 placedPosition = unlockablesList.unlockables[unlockableIndex].placedPosition;
				Vector3 placedRotation = unlockablesList.unlockables[unlockableIndex].placedRotation;
				Debug.Log($"use saved position data false; pos: {placedPosition}; rot: {placedRotation}");
				ShipBuildModeManager.Instance.PlaceShipObject(placedPosition, placedRotation, gameObject.GetComponentInChildren<PlaceableShipObject>(), placementSFX: false);
			}
			else if (ES3.KeyExists("ShipUnlockMoved_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName))
			{
				Vector3 placedPosition = ES3.Load("ShipUnlockPos_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName, Vector3.zero);
				Vector3 placedRotation = ES3.Load("ShipUnlockRot_" + unlockableItem.unlockableName, GameNetworkManager.Instance.currentSaveFileName, Vector3.zero);
				Debug.Log($"Loading placed object position as: {placedPosition}");
				ShipBuildModeManager.Instance.PlaceShipObject(placedPosition, placedRotation, gameObject.GetComponentInChildren<PlaceableShipObject>(), placementSFX: false);
			}
			if (!gameObject.GetComponent<NetworkObject>().IsSpawned)
			{
				gameObject.GetComponent<NetworkObject>().Spawn();
			}
			break;
		}
		if (gameObject != null)
		{
			SpawnedShipUnlockables.Add(unlockableIndex, gameObject);
		}
	}

	[ServerRpc]
	public void SyncSuitsServerRpc()
{		{
			SyncSuitsClientRpc();
		}
}
	[ClientRpc]
	public void SyncSuitsClientRpc()
			{
				PositionSuitsOnRack();
			}

	private void LoadShipGrabbableItems()
	{
		if (!ES3.KeyExists("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName))
		{
			Debug.Log("Key 'shipGrabbableItems' does not exist");
			return;
		}
		int[] array = ES3.Load<int[]>("shipGrabbableItemIDs", GameNetworkManager.Instance.currentSaveFileName);
		Vector3[] array2 = ES3.Load<Vector3[]>("shipGrabbableItemPos", GameNetworkManager.Instance.currentSaveFileName);
		if (array == null || array2 == null)
		{
			Debug.LogError("Ship items list loaded from file returns a null value!");
			return;
		}
		Debug.Log($"Ship grabbable items list loaded. Count: {array.Length}");
		bool flag = ES3.KeyExists("shipScrapValues", GameNetworkManager.Instance.currentSaveFileName);
		int[] array3 = null;
		if (flag)
		{
			array3 = ES3.Load<int[]>("shipScrapValues", GameNetworkManager.Instance.currentSaveFileName);
		}
		int[] array4 = null;
		bool flag2 = false;
		if (ES3.KeyExists("shipItemSaveData", GameNetworkManager.Instance.currentSaveFileName))
		{
			flag2 = true;
			array4 = ES3.Load<int[]>("shipItemSaveData", GameNetworkManager.Instance.currentSaveFileName);
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (allItemsList.itemsList.Count >= array[i])
			{
				Debug.Log($"Item {allItemsList.itemsList[array[i]].itemName} #{i} spawn position: {array2[i]}");
				if (!shipBounds.bounds.Contains(array2[i]))
				{
					array2[i] = playerSpawnPositions[1].position;
					array2[i].x += UnityEngine.Random.Range(-0.7f, 0.7f);
					array2[i].z += UnityEngine.Random.Range(2f, 2f);
					array2[i].y += 0.5f;
					Debug.Log($"Item was outside ship bounds; new position: {array2[i]}");
				}
				Debug.DrawRay(array2[i], Vector3.up * 0.5f, Color.magenta, 10f);
				GrabbableObject component = UnityEngine.Object.Instantiate(allItemsList.itemsList[array[i]].spawnPrefab, array2[i], Quaternion.identity, elevatorTransform).GetComponent<GrabbableObject>();
				component.fallTime = 0f;
				component.scrapPersistedThroughRounds = true;
				component.isInElevator = true;
				component.isInShipRoom = true;
				if (flag && allItemsList.itemsList[array[i]].isScrap)
				{
					Debug.Log($"Setting scrap value for item: {component.gameObject.name}: {array3[num]}");
					component.SetScrapValue(array3[num]);
					num++;
				}
				if (flag2 && component.itemProperties.saveItemVariable && num2 < array4.Length)
				{
					Debug.Log($"Loading item save data for item: {component.gameObject}: {array4[num2]}");
					component.LoadItemSaveData(array4[num2]);
					num2++;
				}
				component.NetworkObject.Spawn();
			}
		}
	}

	private void SetTimeAndPlanetToSavedSettings()
	{
		string currentSaveFileName = GameNetworkManager.Instance.currentSaveFileName;
		int levelID;
		if (currentSaveFileName == "LCChallengeFile")
		{
			System.Random random = new System.Random(GameNetworkManager.Instance.GetWeekNumber() + 2);
			randomMapSeed = random.Next(0, 100000000);
			hasSubmittedChallengeRank = ES3.Load("SubmittedScore", currentSaveFileName, defaultValue: false);
			isChallengeFile = true;
			UnlockableSuit.SwitchSuitForAllPlayers(24);
			SelectableLevel[] array = levels.Where((SelectableLevel x) => x.planetHasTime).ToArray();
			levelID = array[random.Next(0, array.Length)].levelID;
		}
		else
		{
			isChallengeFile = false;
			randomMapSeed = ES3.Load("RandomSeed", currentSaveFileName, 0);
			levelID = ES3.Load("CurrentPlanetID", currentSaveFileName, defaultPlanet);
		}
		ChangeLevel(levelID);
		ChangePlanet();
		if (isChallengeFile)
		{
			TimeOfDay.Instance.totalTime = TimeOfDay.Instance.lengthOfHours * (float)TimeOfDay.Instance.numberOfHours;
			TimeOfDay.Instance.timeUntilDeadline = TimeOfDay.Instance.totalTime;
			TimeOfDay.Instance.profitQuota = 200;
		}
		else
		{
			TimeOfDay.Instance.timesFulfilledQuota = ES3.Load("QuotasPassed", currentSaveFileName, 0);
			TimeOfDay.Instance.profitQuota = ES3.Load("ProfitQuota", currentSaveFileName, TimeOfDay.Instance.quotaVariables.startingQuota);
			TimeOfDay.Instance.totalTime = TimeOfDay.Instance.lengthOfHours * (float)TimeOfDay.Instance.numberOfHours;
			TimeOfDay.Instance.timeUntilDeadline = ES3.Load("DeadlineTime", currentSaveFileName, (int)(TimeOfDay.Instance.totalTime * (float)TimeOfDay.Instance.quotaVariables.deadlineDaysAmount));
			TimeOfDay.Instance.quotaFulfilled = ES3.Load("QuotaFulfilled", currentSaveFileName, 0);
			TimeOfDay.Instance.SetBuyingRateForDay();
			gameStats.daysSpent = ES3.Load("Stats_DaysSpent", currentSaveFileName, 0);
			gameStats.deaths = ES3.Load("Stats_Deaths", currentSaveFileName, 0);
			gameStats.scrapValueCollected = ES3.Load("Stats_ValueCollected", currentSaveFileName, 0);
			gameStats.allStepsTaken = ES3.Load("Stats_StepsTaken", currentSaveFileName, 0);
		}
		TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
		LoadPlanetsMoldSpreadData();
		SetPlanetsWeather();
		UnityEngine.Object.FindObjectOfType<Terminal>().SetItemSales();
		if (gameStats.daysSpent == 0 && !isChallengeFile)
		{
			PlayFirstDayShipAnimation(waitForMenuToClose: true);
		}
		if (TimeOfDay.Instance.timeUntilDeadline > 0f && TimeOfDay.Instance.daysUntilDeadline <= 0 && TimeOfDay.Instance.timesFulfilledQuota <= 0)
		{
			StartCoroutine(playDaysLeftAlertSFXDelayed());
		}
	}

	private IEnumerator StartSpatialVoiceChat()
	{
		yield return new WaitUntil(() => localClientHasControl && GameNetworkManager.Instance.localPlayerController != null);
		for (int i = 0; i < allPlayerObjects.Length; i++)
		{
			if ((bool)allPlayerObjects[i].GetComponent<NfgoPlayer>() && !allPlayerObjects[i].GetComponent<NfgoPlayer>().IsTracking)
			{
				allPlayerObjects[i].GetComponent<NfgoPlayer>().VoiceChatTrackingStart();
			}
		}
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => HUDManager.Instance.hasSetSavedValues || Time.realtimeSinceStartup - startTime > 5f);
		if (!HUDManager.Instance.hasSetSavedValues)
		{
			Debug.LogError("Failure to set local player level! Skipping sync.");
		}
		else
		{
			HUDManager.Instance.SyncAllPlayerLevelsServerRpc(HUDManager.Instance.localPlayerLevel, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
		HUDManager.Instance.DisableLocalBadgeMesh();
		yield return new WaitForSeconds(12f);
		UpdatePlayerVoiceEffects();
	}

	private IEnumerator UpdatePlayerVoiceEffectsOnDelay()
	{
		yield return new WaitForSeconds(12f);
		UpdatePlayerVoiceEffects();
	}

	public void KickPlayer(int playerObjToKick)
	{
		if ((!allPlayerScripts[playerObjToKick].isPlayerControlled && !allPlayerScripts[playerObjToKick].isPlayerDead) || !base.IsServer)
		{
			return;
		}
		if (!GameNetworkManager.Instance.disableSteam)
		{
			ulong playerSteamId = Instance.allPlayerScripts[playerObjToKick].playerSteamId;
			if (!KickedClientIds.Contains(playerSteamId))
			{
				KickedClientIds.Add(playerSteamId);
			}
		}
		NetworkManager.Singleton.DisconnectClient(allPlayerScripts[playerObjToKick].actualClientId);
		HUDManager.Instance.AddTextToChatOnServer($"[playerNum{playerObjToKick}] was kicked.");
	}

	public void OnLocalDisconnect()
	{
		if (!GameNetworkManager.Instance.disableSteam)
		{
			SteamFriends.ClearRichPresence();
		}
		if (NetworkManager.Singleton != null)
		{
			if (NetworkManager.Singleton.SceneManager != null)
			{
				NetworkManager.Singleton.SceneManager.OnLoadComplete -= SceneManager_OnLoadComplete1;
				NetworkManager.Singleton.SceneManager.OnLoad -= SceneManager_OnLoad;
			}
			else
			{
				Debug.Log("Scene manager is null");
			}
		}
	}

	public void OnClientDisconnect(ulong clientId)
	{
		if (ClientPlayerList == null || !ClientPlayerList.ContainsKey(clientId))
		{
			Debug.Log("Disconnection callback called for a client id which isn't in ClientPlayerList; ignoring. This is likely due to an unapproved connection.");
			return;
		}
		if (NetworkManager.Singleton == null || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			GameNetworkManager.Instance.disconnectReason = 1;
			GameNetworkManager.Instance.Disconnect();
			return;
		}
		if (clientId == NetworkManager.Singleton.LocalClientId || clientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
		{
			Debug.Log("Disconnect callback called for local client; ignoring.");
			return;
		}
		Debug.Log("Client disconnected from server");
		if (!ClientPlayerList.TryGetValue(clientId, out var value))
		{
			Debug.LogError("Could not get player object number from client id on disconnect!");
		}
		if (!base.IsServer)
		{
			Debug.Log($"player disconnected c; {clientId}");
			Debug.Log(ClientPlayerList.Count);
			for (int i = 0; i < ClientPlayerList.Count; i++)
			{
				ClientPlayerList.TryGetValue((ulong)i, out var value2);
				Debug.Log($"client id: {i} ; player object id: {value2}");
			}
			Debug.Log($"disconnecting client id: {clientId}");
			if (ClientPlayerList.TryGetValue(clientId, out var value3) && value3 == 0)
			{
				Debug.Log("Host disconnected!");
				Debug.Log(GameNetworkManager.Instance.isDisconnecting);
				if (!GameNetworkManager.Instance.isDisconnecting)
				{
					Debug.Log("Host quit! Ending game for client.");
					GameNetworkManager.Instance.disconnectReason = 1;
					GameNetworkManager.Instance.Disconnect();
					return;
				}
			}
			OnPlayerDC(value, clientId);
			return;
		}
		if (fullyLoadedPlayers.Contains(clientId))
		{
			fullyLoadedPlayers.Remove(clientId);
		}
		if (RoundManager.Instance.playersFinishedGeneratingFloor.Contains(clientId))
		{
			RoundManager.Instance.playersFinishedGeneratingFloor.Remove(clientId);
		}
		GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
		for (int j = 0; j < array.Length; j++)
		{
			if (!array[j].isHeld)
			{
				array[j].heldByPlayerOnServer = false;
			}
		}
		if (!base.IsServer)
		{
			return;
		}
		List<ulong> list = new List<ulong>();
		foreach (KeyValuePair<ulong, int> clientPlayer in ClientPlayerList)
		{
			if (clientPlayer.Key != clientId)
			{
				list.Add(clientPlayer.Key);
			}
		}
		ClientRpcParams clientRpcParams = default(ClientRpcParams);
		clientRpcParams.Send = new ClientRpcSendParams
		{
			TargetClientIds = list.ToArray()
		};
		ClientRpcParams clientRpcParams2 = clientRpcParams;
		OnPlayerDC(value, clientId);
		OnClientDisconnectClientRpc(value, clientId, clientRpcParams2);
	}

	[ClientRpc]
	public void OnClientDisconnectClientRpc(int playerObjectNumber, ulong clientId, ClientRpcParams clientRpcParams = default(ClientRpcParams))
			{
				OnPlayerDC(playerObjectNumber, clientId);
			}

	public void OnPlayerDC(int playerObjectNumber, ulong clientId)
	{
		Debug.Log("Calling OnPlayerDC!");
		if (!ClientPlayerList.ContainsKey(clientId))
		{
			Debug.Log("disconnect: clientId key already removed!");
			return;
		}
		if (GameNetworkManager.Instance.localPlayerController != null && clientId == GameNetworkManager.Instance.localPlayerController.actualClientId)
		{
			Debug.Log("OnPlayerDC: Local client is disconnecting so return.");
			return;
		}
		if (base.NetworkManager.ShutdownInProgress || NetworkManager.Singleton == null)
		{
			Debug.Log("Shutdown is in progress, returning");
			return;
		}
		Debug.Log("Player DC'ing 2");
		if (base.IsServer && ClientPlayerList.TryGetValue(clientId, out var value))
		{
			HUDManager.Instance.AddTextToChatOnServer($"[playerNum{allPlayerScripts[value].playerClientId}] disconnected.");
		}
		if (!allPlayerScripts[playerObjectNumber].isPlayerDead)
		{
			livingPlayers--;
		}
		ClientPlayerList.Remove(clientId);
		connectedPlayersAmount--;
		Debug.Log("Player DC'ing 3");
		PlayerControllerB component = allPlayerObjects[playerObjectNumber].GetComponent<PlayerControllerB>();
		try
		{
			component.sentPlayerValues = false;
			component.isPlayerControlled = false;
			component.isPlayerDead = false;
			if (!inShipPhase)
			{
				component.disconnectedMidGame = true;
				if (livingPlayers == 0)
				{
					allPlayersDead = true;
					ShipLeaveAutomatically();
				}
			}
			component.DropAllHeldItems(itemsFall: true, disconnecting: true);
			Debug.Log("Teleporting disconnected player out");
			component.TeleportPlayer(notSpawnedPosition.position);
			UnlockableSuit.SwitchSuitForPlayer(component, 0, playAudio: false);
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				HUDManager.Instance.UpdateBoxesSpectateUI();
			}
			if (!NetworkManager.Singleton.ShutdownInProgress && base.IsServer)
			{
				component.gameObject.GetComponent<NetworkObject>().RemoveOwnership();
			}
			QuickMenuManager quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
			if (quickMenuManager != null)
			{
				quickMenuManager.RemoveUserFromPlayerList(playerObjectNumber);
			}
			Debug.Log($"Current players after dc: {connectedPlayersAmount}");
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while handling player disconnect!: {arg}");
		}
	}

	public void OnClientConnect(ulong clientId)
	{
		if (!base.IsServer)
		{
			return;
		}
		Debug.Log("player connected");
		Debug.Log($"connected players #: {connectedPlayersAmount}");
		try
		{
			List<int> list = ClientPlayerList.Values.ToList();
			Debug.Log($"Connecting new player on host; clientId: {clientId}");
			int num = 0;
			for (int i = 1; i < 4; i++)
			{
				if (!list.Contains(i))
				{
					num = i;
					break;
				}
			}
			allPlayerScripts[num].actualClientId = clientId;
			allPlayerObjects[num].GetComponent<NetworkObject>().ChangeOwnership(clientId);
			Debug.Log($"New player assigned object id: {allPlayerObjects[num]}");
			List<ulong> list2 = new List<ulong>();
			for (int j = 0; j < allPlayerObjects.Length; j++)
			{
				NetworkObject component = allPlayerObjects[j].GetComponent<NetworkObject>();
				if (!component.IsOwnedByServer)
				{
					list2.Add(component.OwnerClientId);
				}
				else if (j == 0)
				{
					list2.Add(NetworkManager.Singleton.LocalClientId);
				}
				else
				{
					list2.Add(999uL);
				}
			}
			int groupCredits = UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits;
			int profitQuota = TimeOfDay.Instance.profitQuota;
			int quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
			int timeUntilDeadline = (int)TimeOfDay.Instance.timeUntilDeadline;
			OnPlayerConnectedClientRpc(clientId, connectedPlayersAmount, list2.ToArray(), num, groupCredits, currentLevelID, profitQuota, timeUntilDeadline, quotaFulfilled, randomMapSeed, isChallengeFile);
			ClientPlayerList.Add(clientId, num);
			Debug.Log($"client id connecting: {clientId} ; their corresponding player object id: {num}");
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error occured in OnClientConnected! Shutting server down. clientId: {clientId}. Error: {arg}");
			GameNetworkManager.Instance.disconnectionReasonMessage = "Error occured when a player attempted to join the server! Restart the application and please report the glitch!";
			GameNetworkManager.Instance.Disconnect();
		}
	}

	[ClientRpc]
	private void OnPlayerConnectedClientRpc(ulong clientId, int connectedPlayers, ulong[] connectedPlayerIdsOrdered, int assignedPlayerObjectId, int serverMoneyAmount, int levelID, int profitQuota, int timeUntilDeadline, int quotaFulfilled, int randomSeed, bool isChallenge)
{		try
		{
			Debug.Log($"NEW CLIENT JOINED THE SERVER!!; clientId: {clientId}");
			if (NetworkManager.Singleton == null)
			{
				return;
			}
			if (clientId == NetworkManager.Singleton.LocalClientId && GameNetworkManager.Instance.localClientWaitingForApproval)
			{
				GameNetworkManager.Instance.localClientWaitingForApproval = false;
			}
			if (!base.IsServer)
			{
				ClientPlayerList.Clear();
				for (int i = 0; i < connectedPlayerIdsOrdered.Length; i++)
				{
					if (connectedPlayerIdsOrdered[i] == 999)
					{
						Debug.Log($"Skipping at index {i}");
						continue;
					}
					ClientPlayerList.Add(connectedPlayerIdsOrdered[i], i);
					Debug.Log($"adding value to ClientPlayerList at value of index {i}: {connectedPlayerIdsOrdered[i]}");
				}
				if (!ClientPlayerList.ContainsKey(clientId))
				{
					Debug.Log($"Successfully added new client id {clientId} and connected to object {assignedPlayerObjectId}");
					ClientPlayerList.Add(clientId, assignedPlayerObjectId);
				}
				else
				{
					Debug.Log("ClientId already in ClientPlayerList!");
				}
				Debug.Log($"clientplayerlist count for client: {ClientPlayerList.Count}");
				Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
				terminal.groupCredits = serverMoneyAmount;
				TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
				timeOfDay.globalTime = 100f;
				ChangeLevel(levelID);
				ChangePlanet();
				isChallengeFile = isChallenge;
				randomMapSeed = randomSeed;
				terminal.RotateShipDecorSelection();
				SetPlanetsWeather();
				UnityEngine.Object.FindObjectOfType<Terminal>().SetItemSales();
				SetMapScreenInfoToCurrentLevel();
				TimeOfDay.Instance.profitQuota = profitQuota;
				TimeOfDay.Instance.timeUntilDeadline = timeUntilDeadline;
				timeOfDay.SetBuyingRateForDay();
				TimeOfDay.Instance.quotaFulfilled = quotaFulfilled;
				TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
			}
			connectedPlayersAmount = connectedPlayers + 1;
			Debug.Log("New player: " + allPlayerObjects[assignedPlayerObjectId].name);
			PlayerControllerB playerControllerB = allPlayerScripts[assignedPlayerObjectId];
			Vector3 vector = (playerControllerB.serverPlayerPosition = GetPlayerSpawnPosition(assignedPlayerObjectId));
			playerControllerB.actualClientId = clientId;
			playerControllerB.isInElevator = true;
			playerControllerB.isInHangarShipRoom = true;
			playerControllerB.parentedToElevatorLastFrame = false;
			allPlayerScripts[assignedPlayerObjectId].TeleportPlayer(vector);
			StartCoroutine(setPlayerToSpawnPosition(allPlayerObjects[assignedPlayerObjectId].transform, vector));
			for (int j = 0; j < connectedPlayersAmount + 1; j++)
			{
				if (j == 0 || !allPlayerScripts[j].IsOwnedByServer)
				{
					allPlayerScripts[j].isPlayerControlled = true;
				}
			}
			playerControllerB.isPlayerControlled = true;
			livingPlayers = connectedPlayersAmount + 1;
			Debug.Log($"Connected players (joined clients) amount after connection: {connectedPlayersAmount}");
			if (NetworkManager.Singleton.LocalClientId == clientId)
			{
				Debug.Log($"Asking server to sync already-held objects. Our client id: {NetworkManager.Singleton.LocalClientId}");
				mostRecentlyJoinedClient = true;
				if (isChallengeFile)
				{
					UnlockableSuit.SwitchSuitForAllPlayers(24);
				}
				HUDManager.Instance.SetSavedValues(assignedPlayerObjectId);
				SyncAlreadyHeldObjectsServerRpc((int)NetworkManager.Singleton.LocalClientId);
			}
			else
			{
				Debug.Log($"This client is not the client who just joined. Our client id: {NetworkManager.Singleton.LocalClientId}; joining client id: {clientId}");
				mostRecentlyJoinedClient = false;
				if (updateVoiceEffectsCoroutine != null)
				{
					StopCoroutine(updateVoiceEffectsCoroutine);
				}
				updateVoiceEffectsCoroutine = StartCoroutine(UpdatePlayerVoiceEffectsOnDelay());
				if (!playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().IsTracking)
				{
					// playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();
				}
			}
			if (GameNetworkManager.Instance.disableSteam)
			{
				QuickMenuManager quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
				for (int k = 0; k < allPlayerScripts.Length; k++)
				{
					if (allPlayerScripts[k].isPlayerControlled || allPlayerScripts[k].isPlayerDead)
					{
						quickMenuManager.AddUserToPlayerList(0uL, allPlayerScripts[k].playerUsername, (int)allPlayerScripts[k].playerClientId);
					}
				}
			}
			SetDiscordStatusDetails();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Failed to assign new player with client id #{clientId}: {arg}");
			GameNetworkManager.Instance.disconnectionReasonMessage = "An error occured while spawning into the game. Please report the glitch!";
			GameNetworkManager.Instance.Disconnect();
		}
}
	private Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport = false)
	{
		if (simpleTeleport)
		{
			return playerSpawnPositions[0].position;
		}
		Debug.DrawRay(playerSpawnPositions[playerNum].position, Vector3.up, Color.red, 15f);
		if (!Physics.CheckSphere(playerSpawnPositions[playerNum].position, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
		{
			return playerSpawnPositions[playerNum].position;
		}
		if (!Physics.CheckSphere(playerSpawnPositions[playerNum].position + Vector3.up, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
		{
			return playerSpawnPositions[playerNum].position + Vector3.up * 0.5f;
		}
		for (int i = 0; i < playerSpawnPositions.Length; i++)
		{
			if (i != playerNum)
			{
				Debug.DrawRay(playerSpawnPositions[i].position, Vector3.up, Color.green, 15f);
				if (!Physics.CheckSphere(playerSpawnPositions[i].position, 0.12f, -67108865, QueryTriggerInteraction.Ignore))
				{
					return playerSpawnPositions[i].position;
				}
				if (!Physics.CheckSphere(playerSpawnPositions[i].position + Vector3.up, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
				{
					return playerSpawnPositions[i].position + Vector3.up * 0.5f;
				}
			}
		}
		System.Random random = new System.Random(65);
		float y = playerSpawnPositions[0].position.y;
		for (int j = 0; j < 15; j++)
		{
			Vector3 vector = new Vector3(random.Next((int)shipInnerRoomBounds.bounds.min.x, (int)shipInnerRoomBounds.bounds.max.x), y, random.Next((int)shipInnerRoomBounds.bounds.min.z, (int)shipInnerRoomBounds.bounds.max.z));
			vector = shipInnerRoomBounds.transform.InverseTransformPoint(vector);
			Debug.DrawRay(vector, Vector3.up, Color.yellow, 15f);
			if (!Physics.CheckSphere(vector, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
			{
				return playerSpawnPositions[j].position;
			}
		}
		return playerSpawnPositions[0].position + Vector3.up * 0.5f;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncAlreadyHeldObjectsServerRpc(int joiningClientId)
{		Debug.Log("Syncing already-held objects on server");
		try
		{
			GrabbableObject[] array = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			List<NetworkObjectReference> list = new List<NetworkObjectReference>();
			List<int> list2 = new List<int>();
			List<int> list3 = new List<int>();
			List<int> list4 = new List<int>();
			for (int i = 0; i < array.Length; i++)
			{
				if (!array[i].isHeld)
				{
					continue;
				}
				list2.Add((int)array[i].playerHeldBy.playerClientId);
				list.Add(array[i].NetworkObject);
				Debug.Log($"Object #{i} is held");
				for (int j = 0; j < array[i].playerHeldBy.ItemSlots.Length; j++)
				{
					if (array[i].playerHeldBy.ItemSlots[j] == array[i])
					{
						list3.Add(j);
						Debug.Log($"Item slot index for item #{i}: {j}");
					}
				}
				if (array[i].isPocketed)
				{
					list4.Add(list.Count - 1);
					Debug.Log($"Object #{i} is pocketed");
				}
			}
			Debug.Log($"pocketed objects count: {list4.Count}");
			Debug.Log($"held objects count: {list.Count}");
			List<int> list5 = new List<int>();
			for (int k = 0; k < array.Length; k++)
			{
				if (array[k].itemProperties.isScrap)
				{
					list5.Add(array[k].scrapValue);
				}
			}
			if (list.Count > 0)
			{
				SyncAlreadyHeldObjectsClientRpc(list.ToArray(), list2.ToArray(), list3.ToArray(), list4.ToArray(), joiningClientId);
			}
			else
			{
				SyncShipUnlockablesServerRpc();
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while syncing players' already held objects in server! Skipping. Error: {arg}");
			SyncShipUnlockablesServerRpc();
		}
}
	[ClientRpc]
	public void SyncAlreadyHeldObjectsClientRpc(NetworkObjectReference[] gObjects, int[] playersHeldBy, int[] itemSlotNumbers, int[] isObjectPocketed, int syncWithClient)
{if(syncWithClient != (int)NetworkManager.Singleton.LocalClientId)		{
			return;
		}
		Debug.Log("Syncing already-held objects on client");
		Debug.Log($"held objects count: {gObjects.Length}");
		Debug.Log($"pocketed objects count: {isObjectPocketed.Length}");
		try
		{
			for (int i = 0; i < gObjects.Length; i++)
			{
				if (gObjects[i].TryGet(out var networkObject))
				{
					GrabbableObject component = networkObject.gameObject.GetComponent<GrabbableObject>();
					component.isHeld = true;
					allPlayerScripts[playersHeldBy[i]].ItemSlots[itemSlotNumbers[i]] = component;
					component.parentObject = allPlayerScripts[playersHeldBy[i]].serverItemHolder;
					bool flag = false;
					Debug.Log($"isObjectPocketed length: {isObjectPocketed.Length}");
					Debug.Log($"iii {i}");
					for (int j = 0; j < isObjectPocketed.Length; j++)
					{
						Debug.Log($"bbb {j} ; {isObjectPocketed[j]}");
						if (isObjectPocketed[j] == i)
						{
							Debug.Log("Pocketing object for player: " + allPlayerScripts[playersHeldBy[i]].gameObject.name);
							component.isPocketed = true;
							component.EnableItemMeshes(enable: false);
							component.EnablePhysics(enable: false);
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						allPlayerScripts[playersHeldBy[i]].currentlyHeldObjectServer = component;
						allPlayerScripts[playersHeldBy[i]].isHoldingObject = true;
						allPlayerScripts[playersHeldBy[i]].twoHanded = component.itemProperties.twoHanded;
						allPlayerScripts[playersHeldBy[i]].twoHandedAnimation = component.itemProperties.twoHandedAnimation;
						allPlayerScripts[playersHeldBy[i]].currentItemSlot = itemSlotNumbers[i];
					}
				}
				else
				{
					Debug.LogError($"Syncing already held objects: Unable to get network object from reference for GObject; net object id: {gObjects[i].NetworkObjectId}");
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while syncing players' already held objects to client from server: {arg}");
		}
		SyncShipUnlockablesServerRpc();
}
	[ServerRpc(RequireOwnership = false)]
	public void SyncShipUnlockablesServerRpc()
{		try
		{
			int[] array = new int[4];
			for (int i = 0; i < 4; i++)
			{
				array[i] = allPlayerScripts[i].currentSuitID;
			}
			bool[] array2 = new bool[unlockablesList.unlockables.Count];
			List<Vector3> list = new List<Vector3>();
			List<Vector3> list2 = new List<Vector3>();
			List<int> list3 = new List<int>();
			for (int j = 0; j < unlockablesList.unlockables.Count; j++)
			{
				if (unlockablesList.unlockables[j].hasBeenUnlockedByPlayer)
				{
					array2[j] = true;
				}
				else
				{
					array2[j] = false;
				}
				list.Add(unlockablesList.unlockables[j].placedPosition);
				list2.Add(unlockablesList.unlockables[j].placedRotation);
				if (unlockablesList.unlockables[j].unlockableType != 0 && (unlockablesList.unlockables[j].hasBeenUnlockedByPlayer || unlockablesList.unlockables[j].alreadyUnlocked) && unlockablesList.unlockables[j].inStorage)
				{
					Debug.Log("Server: '" + unlockablesList.unlockables[j].unlockableName + "' is in storage");
					list3.Add(j);
				}
			}
			GrabbableObject[] array3 = (from x in UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				orderby Vector3.Distance(x.transform.position, Vector3.zero)
				select x).ToArray();
			List<int> list4 = new List<int>();
			List<int> list5 = new List<int>();
			for (int k = 0; k < array3.Length; k++)
			{
				if (k > 500)
				{
					Debug.Log("Attempted to sync more than 500 scrap values which is not allowed");
					break;
				}
				if (array3[k].itemProperties.saveItemVariable)
				{
					list5.Add(array3[k].GetItemDataToSave());
				}
				if (array3[k].itemProperties.isScrap)
				{
					list4.Add(array3[k].scrapValue);
				}
			}
			int vehicleID = -1;
			if (attachedVehicle != null)
			{
				vehicleID = attachedVehicle.vehicleID;
			}
			for (int l = 0; l < allPlayerScripts.Length; l++)
			{
				UnlockableSuit.SwitchSuitForPlayer(allPlayerScripts[l], array[l], playAudio: false);
			}
			SyncShipUnlockablesClientRpc(array, shipRoomLights.areLightsOn, list.ToArray(), list2.ToArray(), array2, list3.ToArray(), list4.ToArray(), list5.ToArray(), vehicleID);
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while syncing unlockables in server. Quitting server: {arg}");
			GameNetworkManager.Instance.disconnectionReasonMessage = "An error occured while syncing ship objects! The file may be corrupted. Please report the glitch!";
			GameNetworkManager.Instance.Disconnect();
		}
}
	private void PositionSuitsOnRack()
	{
		UnlockableSuit[] array = UnityEngine.Object.FindObjectsOfType<UnlockableSuit>();
		Debug.Log($"Suits: {array.Length}");
		for (int i = 0; i < array.Length; i++)
		{
			Debug.Log($"Suit #{i}: {array[i].suitID}");
			AutoParentToShip component = array[i].gameObject.GetComponent<AutoParentToShip>();
			component.overrideOffset = true;
			component.positionOffset = new Vector3(-2.45f, 2.75f, -8.41f) + rightmostSuitPosition.forward * 0.18f * i;
			component.rotationOffset = new Vector3(0f, 90f, 0f);
			Debug.Log($"pos: {component.positionOffset}; rot: {component.rotationOffset}");
		}
		UnityEngine.Object.FindObjectsOfType<UnlockableSuit>(includeInactive: true);
	}

	[ClientRpc]
	public void SyncShipUnlockablesClientRpc(int[] playerSuitIDs, bool shipLightsOn, Vector3[] placeableObjectPositions, Vector3[] placeableObjectRotations, bool[] unlockedObjects, int[] storedItems, int[] scrapValues, int[] itemSaveData, int vehicleID = -1)
{		if (!base.IsServer)
		{
			GrabbableObject[] array = (from x in UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
				orderby Vector3.Distance(x.transform.position, Vector3.zero)
				select x).ToArray();
			try
			{
				int num = 0;
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].itemProperties.saveItemVariable)
					{
						array[i].LoadItemSaveData(itemSaveData[num]);
						num++;
					}
				}
			}
			catch (Exception arg)
			{
				Debug.Log($"Error while attempting to sync item save data from host: {arg}");
			}
			try
			{
				int num2 = 0;
				for (int j = 0; j < array.Length; j++)
				{
					if (array[j].itemProperties.isScrap)
					{
						if (num2 >= scrapValues.Length)
						{
							break;
						}
						array[j].SetScrapValue(scrapValues[num2]);
						num2++;
					}
				}
				for (int k = 0; k < array.Length; k++)
				{
					if (array[k].transform.parent == null)
					{
						Vector3 position = array[k].transform.position;
						array[k].transform.parent = elevatorTransform;
						array[k].targetFloorPosition = elevatorTransform.InverseTransformPoint(position);
					}
				}
			}
			catch (Exception arg2)
			{
				Debug.LogError($"Error while syncing scrap objects to this client from server: {arg2}");
			}
			try
			{
				for (int l = 0; l < allPlayerScripts.Length; l++)
				{
					UnlockableSuit.SwitchSuitForPlayer(allPlayerScripts[l], playerSuitIDs[l], playAudio: false);
				}
				PositionSuitsOnRack();
				bool flag = false;
				PlaceableShipObject[] array2 = UnityEngine.Object.FindObjectsByType<PlaceableShipObject>(FindObjectsSortMode.None);
				Debug.Log("syncing unlockables...");
				for (int m = 0; m < unlockablesList.unlockables.Count; m++)
				{
					Debug.Log($"Index: {m} position: {placeableObjectPositions[m]}; {placeableObjectRotations[m]}");
					if (unlockedObjects[m])
					{
						unlockablesList.unlockables[m].hasBeenUnlockedByPlayer = true;
					}
					if (unlockablesList.unlockables[m].unlockableType == 0)
					{
						continue;
					}
					unlockablesList.unlockables[m].placedPosition = placeableObjectPositions[m];
					unlockablesList.unlockables[m].placedRotation = placeableObjectRotations[m];
					if (storedItems.Contains(m))
					{
						unlockablesList.unlockables[m].inStorage = true;
					}
					for (int n = 0; n < array2.Length; n++)
					{
						if (array2[n].unlockableID == m)
						{
							Debug.Log($"Found existing object {n} with unlockable id {m}: {array2[n].parentObject.gameObject.name}. Synced position: {placeableObjectPositions[m]} with rotation {placeableObjectRotations[m]}.");
							if (unlockablesList.unlockables[m].placedPosition != Vector3.zero)
							{
								ShipBuildModeManager.Instance.PlaceShipObject(placeableObjectPositions[m], placeableObjectRotations[m], array2[n], placementSFX: false);
							}
							if (unlockablesList.unlockables[m].inStorage && !unlockablesList.unlockables[m].spawnPrefab)
							{
								array2[n].parentObject.disableObject = true;
							}
							break;
						}
					}
				}
				if (mostRecentlyJoinedClient && flag && GameNetworkManager.Instance.localPlayerController != null)
				{
					GameNetworkManager.Instance.localPlayerController.TeleportPlayer(GetPlayerSpawnPosition((int)GameNetworkManager.Instance.localPlayerController.playerClientId));
				}
			}
			catch (Exception arg3)
			{
				Debug.LogError($"Error while syncing unlockables in ship to this client from server: {arg3}");
			}
			try
			{
				if (vehicleID != -1)
				{
					isObjectAttachedToMagnet = true;
					magnetOn = true;
					magnetLever.initialBoolState = true;
					magnetLever.setInitialState = true;
					magnetLever.SetInitialState();
					VehicleController vehicleController = UnityEngine.Object.FindObjectOfType<VehicleController>();
					if (vehicleController.vehicleID == vehicleID)
					{
						attachedVehicle = vehicleController;
						attachedVehicle.magnetedToShip = true;
						attachedVehicle.StartMagneting();
					}
				}
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
		}
		try
		{
			for (int num3 = 0; num3 < 4; num3++)
			{
				if (!allPlayerScripts[num3].isPlayerControlled && !allPlayerScripts[num3].isPlayerDead)
				{
					return;
				}
				allPlayerScripts[num3].currentSuitID = playerSuitIDs[num3];
				Material suitMaterial = unlockablesList.unlockables[playerSuitIDs[num3]].suitMaterial;
				allPlayerScripts[num3].thisPlayerModel.sharedMaterial = suitMaterial;
				allPlayerScripts[num3].thisPlayerModelLOD1.sharedMaterial = suitMaterial;
				allPlayerScripts[num3].thisPlayerModelLOD2.sharedMaterial = suitMaterial;
				allPlayerScripts[num3].thisPlayerModelArms.sharedMaterial = suitMaterial;
			}
		}
		catch (Exception arg4)
		{
			Debug.LogError($"Error while syncing player suit materials from server to client: {arg4}");
		}
		HUDManager.Instance.SyncAllPlayerLevelsServerRpc();
		shipRoomLights.SetShipLightsOnLocalClientOnly(shipLightsOn);
		if (UnityEngine.Object.FindObjectOfType<TVScript>() != null)
		{
			UnityEngine.Object.FindObjectOfType<TVScript>().SyncTVServerRpc();
		}
}
	public void StartTrackingAllPlayerVoices()
	{
		for (int i = 0; i < allPlayerScripts.Length; i++)
		{
			if ((allPlayerScripts[i].isPlayerControlled || Instance.allPlayerScripts[i].isPlayerDead) && !allPlayerScripts[i].gameObject.GetComponentInChildren<NfgoPlayer>().IsTracking)
			{
				Debug.Log("Starting voice tracking for player: " + allPlayerScripts[i].playerUsername);
				// allPlayerScripts[i].gameObject.GetComponentInChildren<NfgoPlayer>().VoiceChatTrackingStart();
			}
		}
	}

	private IEnumerator setPlayerToSpawnPosition(Transform playerBody, Vector3 spawnPos)
	{
		for (int i = 0; i < 50; i++)
		{
			yield return null;
			yield return null;
			playerBody.position = spawnPos;
			if (Vector3.Distance(playerBody.position, spawnPos) < 6f)
			{
				break;
			}
		}
	}

	private void Update()
	{
		if (GameNetworkManager.Instance == null)
		{
			return;
		}
		if (GameNetworkManager.Instance.localPlayerController != null)
		{
			PlayerControllerB spectatedPlayerScript = GameNetworkManager.Instance.localPlayerController;
			if (spectatedPlayerScript.isPlayerDead && spectatedPlayerScript.spectatedPlayerScript != null)
			{
				spectatedPlayerScript = spectatedPlayerScript.spectatedPlayerScript;
			}
			if (spectatedPlayerScript.isInsideFactory)
			{
				blackSkyVolume.weight = 1f;
			}
			else
			{
				blackSkyVolume.weight = 0f;
			}
			if (suckingPlayersOutOfShip)
			{
				upperMonitorsCanvas.SetActive(value: false);
				SuckLocalPlayerOutOfShipDoor();
			}
			else if (!inShipPhase)
			{
				timeSinceRoundStarted += Time.deltaTime;
				upperMonitorsCanvas.SetActive(GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom);
			}
			else
			{
				upperMonitorsCanvas.SetActive(value: true);
			}
			if (IngamePlayerSettings.Instance.settings.pushToTalk)
			{
				// voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.playerInput.actions.FindAction("VoiceButton").IsPressed() && !GameNetworkManager.Instance.localPlayerController.speakingToWalkieTalkie;
				HUDManager.Instance.PTTIcon.enabled = IngamePlayerSettings.Instance.settings.micEnabled && !voiceChatModule.IsMuted;
			}
			else
			{
				// voiceChatModule.IsMuted = !IngamePlayerSettings.Instance.settings.micEnabled;
				HUDManager.Instance.PTTIcon.enabled = false;
			}
			DetectVoiceChatAmplitude();
		}
		if (base.IsServer && !hasHostSpawned)
		{
			hasHostSpawned = true;
			ClientPlayerList.Add(NetworkManager.Singleton.LocalClientId, connectedPlayersAmount);
			allPlayerObjects[0].GetComponent<NetworkObject>().ChangeOwnership(NetworkManager.Singleton.LocalClientId);
			allPlayerObjects[0].GetComponent<PlayerControllerB>().isPlayerControlled = true;
			livingPlayers = connectedPlayersAmount + 1;
			allPlayerObjects[0].GetComponent<PlayerControllerB>().TeleportPlayer(GetPlayerSpawnPosition(0));
			GameNetworkManager.Instance.SetLobbyJoinable(joinable: true);
		}
	}

	private string NoPunctuation(string input)
	{
		return new string(input.Where((char c) => char.IsLetter(c)).ToArray());
	}

	private void SuckLocalPlayerOutOfShipDoor()
	{
		suckingPower += Time.deltaTime * 2f;
		GameNetworkManager.Instance.localPlayerController.fallValue = 0f;
		GameNetworkManager.Instance.localPlayerController.fallValueUncapped = 0f;
		if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, middleOfShipNode.position) < 25f)
		{
			if (Physics.Linecast(GameNetworkManager.Instance.localPlayerController.transform.position, shipDoorNode.position, collidersAndRoomMask))
			{
				GameNetworkManager.Instance.localPlayerController.externalForces = Vector3.Normalize(middleOfShipNode.position - GameNetworkManager.Instance.localPlayerController.transform.position) * 350f;
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.externalForces = Vector3.Normalize(middleOfSpaceNode.position - GameNetworkManager.Instance.localPlayerController.transform.position) * (350f / Vector3.Distance(moveAwayFromShipNode.position, GameNetworkManager.Instance.localPlayerController.transform.position)) * (suckingPower / 2.25f);
			}
			return;
		}
		if (!choseRandomFlyDirForPlayer)
		{
			choseRandomFlyDirForPlayer = true;
			randomFlyDir = new Vector3(-1f, 0f, UnityEngine.Random.Range(-0.7f, 0.7f));
		}
		GameNetworkManager.Instance.localPlayerController.externalForces = Vector3.Scale(Vector3.one, randomFlyDir) * 70f;
	}

	private void DetectVoiceChatAmplitude()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isPlayerDead || voiceChatModule == null)
		{
			return;
		}
		VoicePlayerState voicePlayerState = voiceChatModule.FindPlayer(voiceChatModule.LocalPlayerName);
		averageCount++;
		if (averageCount > movingAverageLength)
		{
			averageVoiceAmplitude += (voicePlayerState.Amplitude - averageVoiceAmplitude) / (float)(movingAverageLength + 1);
		}
		else
		{
			averageVoiceAmplitude += voicePlayerState.Amplitude;
			if (averageCount == movingAverageLength)
			{
				averageVoiceAmplitude /= averageCount;
			}
		}
		float num = voicePlayerState.Amplitude / Mathf.Clamp(averageVoiceAmplitude, 0.008f, 0.5f);
		if (voicePlayerState.IsSpeaking && voiceChatNoiseCooldown <= 0f && num > 3f)
		{
			RoundManager.Instance.PlayAudibleNoise(GameNetworkManager.Instance.localPlayerController.transform.position, Mathf.Clamp(3f * num, 3f, 36f), Mathf.Clamp(num / 7f, 0.6f, 0.9f), 0, hangarDoorsClosed && GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom, 75);
			voiceChatNoiseCooldown = 0.2f;
		}
		voiceChatNoiseCooldown -= Time.deltaTime;
	}

	public void ShipLeaveAutomatically(bool leavingOnMidnight = false)
	{
		if (!shipLeftAutomatically && !shipIsLeaving)
		{
			shipLeftAutomatically = true;
			StartCoroutine(gameOverAnimation(leavingOnMidnight));
		}
	}

	public void SetSpectateCameraToGameOverMode(bool enableGameOver, PlayerControllerB localPlayer = null)
	{
		overrideSpectateCamera = enableGameOver;
		if (enableGameOver)
		{
			spectateCamera.transform.SetParent(gameOverCameraHandle, worldPositionStays: false);
		}
		else
		{
			spectateCamera.transform.SetParent(localPlayer.spectateCameraPivot, worldPositionStays: false);
		}
		spectateCamera.transform.localEulerAngles = Vector3.zero;
		spectateCamera.transform.localPosition = Vector3.zero;
	}

	public void SwitchCamera(Camera newCamera)
	{
		if (newCamera != spectateCamera)
		{
			spectateCamera.enabled = false;
		}
		newCamera.enabled = true;
		activeCamera = newCamera;
		UnityEngine.Object.FindObjectOfType<StormyWeather>(includeInactive: true).SwitchCamera(newCamera);
		CameraSwitchEvent.Invoke();
	}

	private IEnumerator gameOverAnimation(bool leavingOnMidnight)
	{
		yield return new WaitUntil(() => shipHasLanded);
		if (leavingOnMidnight)
		{
			HUDManager.Instance.ReadDialogue(shipLeavingOnMidnightDialogue);
		}
		HUDManager.Instance.shipLeavingEarlyIcon.enabled = false;
		StartMatchLever startMatchLever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
		startMatchLever.triggerScript.animationString = "SA_PushLeverBack";
		startMatchLever.leverHasBeenPulled = false;
		startMatchLever.triggerScript.interactable = false;
		startMatchLever.leverAnimatorObject.SetBool("pullLever", value: false);
		ShipLeave();
		yield return new WaitForSeconds(1.5f);
		SetSpectateCameraToGameOverMode(enableGameOver: true);
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			GameNetworkManager.Instance.localPlayerController.SetSpectatedPlayerEffects(allPlayersDead: true);
		}
		yield return new WaitForSeconds(1f);
		if (!leavingOnMidnight)
		{
			HUDManager.Instance.ReadDialogue(gameOverDialogue);
		}
		Debug.Log($"Is in elevator D?: {GameNetworkManager.Instance.localPlayerController.isInElevator}");
		yield return new WaitForSeconds(9.5f);
		if (!leavingOnMidnight)
		{
			HUDManager.Instance.UIAudio.PlayOneShot(allPlayersDeadAudio);
			HUDManager.Instance.gameOverAnimator.SetTrigger("allPlayersDead");
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StartGameServerRpc()
{		{
			if (fullyLoadedPlayers.Count >= connectedPlayersAmount + 1 && !travellingToNewLevel)
			{
				StartGame();
			}
			else
			{
				UnityEngine.Object.FindObjectOfType<StartMatchLever>().CancelStartGameClientRpc();
			}
		}
}
	public void StartGame()
	{
		if (!base.IsServer)
		{
			return;
		}
		if (inShipPhase)
		{
			if (!GameNetworkManager.Instance.gameHasStarted)
			{
				GameNetworkManager.Instance.LeaveLobbyAtGameStart();
				GameNetworkManager.Instance.gameHasStarted = true;
			}
			inShipPhase = false;
			fullyLoadedPlayers.Clear();
			ResetPlayersLoadedValueClientRpc(landingShip: true);
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.disabledHoverTip = "[Wait for ship to land]";
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.interactable = false;
			currentPlanetAnimator.SetTrigger("LandOnPlanet");
			if (overrideRandomSeed)
			{
				randomMapSeed = overrideSeedNumber;
			}
			else if (isChallengeFile)
			{
				randomMapSeed = new System.Random(GameNetworkManager.Instance.GetWeekNumber() + 51016).Next(0, 100000000);
				Debug.Log($"RANDOM MAP SEED: {randomMapSeed}");
			}
			else
			{
				ChooseNewRandomMapSeed();
			}
			base.NetworkManager.SceneManager.LoadScene(currentLevel.sceneName, LoadSceneMode.Additive);
			Debug.Log("LOADING GAME!!!!!");
			StartCoroutine(OpenShipDoors());
		}
		else
		{
			Debug.Log("Attempted to start game on server but we are not in ship phase");
		}
	}

	public void ChooseNewRandomMapSeed()
	{
		randomMapSeed = UnityEngine.Random.Range(1, 100000000);
	}

	private IEnumerator OpenShipDoors()
	{
		Debug.Log("Waiting for all players to load!");
		yield return new WaitUntil(() => fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
		yield return new WaitForSeconds(0.5f);
		RoundManager.Instance.LoadNewLevel(randomMapSeed, currentLevel);
	}

	public IEnumerator openingDoorsSequence()
	{
		StartNewRoundEvent.Invoke();
		yield return new WaitForSeconds(1f);
		HUDManager.Instance.LevellingAudio.Stop();
		StartMatchLever leverScript = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
		leverScript.triggerScript.timeToHold = 0.7f;
		leverScript.triggerScript.interactable = false;
		displayedLevelResults = false;
		Instance.StartTrackingAllPlayerVoices();
		if (!GameNetworkManager.Instance.gameHasStarted)
		{
			GameNetworkManager.Instance.LeaveLobbyAtGameStart();
			GameNetworkManager.Instance.gameHasStarted = true;
		}
		UnityEngine.Object.FindObjectOfType<QuickMenuManager>().DisableInviteFriendsButton();
		if (!GameNetworkManager.Instance.disableSteam)
		{
			SteamFriends.SetRichPresence("moonname", currentLevel.PlanetName);
			GameNetworkManager.Instance.SetSteamFriendGrouping(GameNetworkManager.Instance.steamLobbyName, connectedPlayersAmount + 1, "#Status_Landed");
		}
		timeSinceRoundStarted = 0f;
		shipLeftAutomatically = false;
		ResetStats();
		inShipPhase = false;
		SwitchMapMonitorPurpose();
		SetPlayerObjectExtrapolate(enable: false);
		shipAnimatorObject.gameObject.GetComponent<Animator>().SetTrigger("OpenShip");
		if (currentLevel.currentWeather != LevelWeatherType.None)
		{
			WeatherEffect weatherEffect = TimeOfDay.Instance.effects[(int)currentLevel.currentWeather];
			weatherEffect.effectEnabled = true;
			if (weatherEffect.effectPermanentObject != null)
			{
				weatherEffect.effectPermanentObject.SetActive(value: true);
			}
		}
		yield return null;
		yield return new WaitForSeconds(0.2f);
		if (TimeOfDay.Instance.currentLevelWeather != LevelWeatherType.None && !currentLevel.overrideWeather)
		{
			TimeOfDay.Instance.effects[(int)TimeOfDay.Instance.currentLevelWeather].effectEnabled = true;
		}
		shipDoorsEnabled = true;
		if (currentLevel.planetHasTime)
		{
			TimeOfDay.Instance.currentDayTimeStarted = true;
			TimeOfDay.Instance.movingGlobalTimeForward = true;
		}
		UnityEngine.Object.FindObjectOfType<HangarShipDoor>().SetDoorButtonsEnabled(doorButtonsEnabled: true);
		TeleportPlayerInShipIfOutOfRoomBounds();
		yield return new WaitForSeconds(0.05f);
		Debug.Log($"startofround: {currentLevel.levelID}; {hoursSinceLastCompanyVisit}");
		if (currentLevel.levelID == 3 && hoursSinceLastCompanyVisit >= 0)
		{
			hoursSinceLastCompanyVisit = 0;
			TimeOfDay.Instance.TimeOfDayMusic.volume = 0.6f;
			Debug.Log("Playing time of day music");
			TimeOfDay.Instance.PlayTimeMusicDelayed(companyVisitMusic, 1f);
		}
		HUDManager.Instance.loadingText.enabled = false;
		HUDManager.Instance.loadingDarkenScreen.enabled = false;
		shipDoorAudioSource.PlayOneShot(openingHangarDoorAudio, 1f);
		yield return new WaitForSeconds(0.8f);
		shipDoorsAnimator.SetBool("Closed", value: false);
		yield return new WaitForSeconds(5f);
		HUDManager.Instance.planetIntroAnimator.SetTrigger("introAnimation");
		if (isChallengeFile)
		{
			HUDManager.Instance.planetInfoHeaderText.text = "CELESTIAL BODY: " + GameNetworkManager.Instance.GetNameForWeekNumber();
		}
		else
		{
			HUDManager.Instance.planetInfoHeaderText.text = "CELESTIAL BODY: " + currentLevel.PlanetName;
		}
		HUDManager.Instance.planetInfoSummaryText.text = currentLevel.LevelDescription;
		HUDManager.Instance.planetRiskLevelText.text = currentLevel.riskLevel;
		yield return new WaitForSeconds(10f);
		if (currentLevel.spawnEnemiesAndScrap && currentLevel.planetHasTime)
		{
			HUDManager.Instance.quotaAnimator.SetBool("visible", value: true);
			TimeOfDay.Instance.currentDayTime = TimeOfDay.Instance.CalculatePlanetTime(currentLevel);
			TimeOfDay.Instance.RefreshClockUI();
		}
		yield return new WaitForSeconds(4f);
		OnShipLandedMiscEvents();
		SetPlayerObjectExtrapolate(enable: false);
		shipHasLanded = true;
		leverScript.triggerScript.animationString = "SA_PushLeverBack";
		leverScript.triggerScript.interactable = true;
		leverScript.hasDisplayedTimeWarning = false;
	}

	private void OnShipLandedMiscEvents()
	{
		if (TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Eclipsed)
		{
			HUDManager.Instance.DisplayTip("Weather alert!", "You have landed in an eclipse. Exercise caution!", isWarning: true, useSave: true, "LC_EclipseTip");
		}
		int num = ES3.Load("TimesLanded", "LCGeneralSaveData", 0);
		ES3.Save("TimesLanded", num + 1, "LCGeneralSaveData");
	}

	public void ForcePlayerIntoShip()
	{
		if (!GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
		{
			GameNetworkManager.Instance.localPlayerController.isInElevator = true;
			GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = true;
			GameNetworkManager.Instance.localPlayerController.TeleportPlayer(GetPlayerSpawnPosition((int)GameNetworkManager.Instance.localPlayerController.playerClientId));
		}
	}

	public void SetPlayerObjectExtrapolate(bool enable)
	{
		if (enable)
		{
			localPlayerController.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Extrapolate;
		}
		else
		{
			localPlayerController.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void EndGameServerRpc(int playerClientId)
{if(shipHasLanded && !shipLeftAutomatically && (!shipIsLeaving || playerClientId == 0))			{
				UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.interactable = false;
				shipHasLanded = false;
				EndGameClientRpc(playerClientId);
			}
}
	[ClientRpc]
	public void EndGameClientRpc(int playerClientId)
			{
				HUDManager.Instance.AddTextToChatOnServer($"[playerNum{playerClientId}] started the ship.");
				ShipLeave();
			}

	private void ShipLeave()
	{
		shipHasLanded = false;
		shipIsLeaving = true;
		shipAnimator.ResetTrigger("ShipLeave");
		shipAnimator.SetTrigger("ShipLeave");
		_ = localPlayerController.isInElevator;
	}

	public void ShipHasLeft()
	{
		shipDoorsAnimator.SetBool("Closed", value: true);
		UnityEngine.Object.FindObjectOfType<HangarShipDoor>().SetDoorButtonsEnabled(doorButtonsEnabled: false);
		if (base.IsServer)
		{
			StartCoroutine(unloadSceneForAllPlayers());
		}
	}

	private IEnumerator unloadSceneForAllPlayers()
	{
		yield return new WaitForSeconds(2f);
		fullyLoadedPlayers.Clear();
		base.NetworkManager.SceneManager.UnloadScene(SceneManager.GetSceneAt(1));
		yield return null;
		yield return new WaitUntil(() => fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers);
		mapScreen.gotCaveNodes = false;
		playersRevived = 0;
		int bodiesInShip = GetBodiesInShip();
		if (connectedPlayersAmount + 1 - livingPlayers == 0 && RoundManager.Instance.valueOfFoundScrapItems > 30)
		{
			daysPlayersSurvivedInARow++;
		}
		else
		{
			daysPlayersSurvivedInARow = 0;
		}
		EndOfGameClientRpc(scrapCollectedOnServer: scrapCollectedLastRound = ((livingPlayers != 0) ? GetValueOfAllScrap(onlyScrapCollected: true, onlyNewScrap: true) : 0), bodiesInsured: bodiesInShip, daysPlayersSurvived: daysPlayersSurvivedInARow, connectedPlayersOnServer: connectedPlayersAmount);
	}

	private int GetBodiesInShip()
	{
		int num = 0;
		DeadBodyInfo[] array = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].isInShip)
			{
				num++;
			}
		}
		return num;
	}

	[ClientRpc]
	public void EndOfGameClientRpc(int bodiesInsured, int daysPlayersSurvived, int connectedPlayersOnServer, int scrapCollectedOnServer)
{		{
			SoundManager.Instance.playingOutsideMusic = false;
			scrapCollectedLastRound = scrapCollectedOnServer;
			UnityEngine.Object.FindObjectOfType<AudioListener>().enabled = true;
			if (currentLevel.planetHasTime)
			{
				WritePlayerNotes();
				HUDManager.Instance.FillEndGameStats(gameStats, scrapCollectedOnServer);
			}
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.animationString = "SA_PullLever";
			daysPlayersSurvivedInARow = daysPlayersSurvived;
			StartCoroutine(EndOfGame(bodiesInsured, connectedPlayersOnServer, scrapCollectedOnServer));
		}
}
	private IEnumerator fadeVolume(float finalVolume)
	{
		float initialVolume = AudioListener.volume;
		for (int i = 0; i < 20; i++)
		{
			yield return new WaitForSeconds(0.015f);
			AudioListener.volume = Mathf.Lerp(initialVolume, finalVolume, (float)i / 20f);
		}
	}

	public void ResetStats()
	{
		for (int i = 0; i < gameStats.allPlayerStats.Length; i++)
		{
			gameStats.allPlayerStats[i].damageTaken = 0;
			gameStats.allPlayerStats[i].jumps = 0;
			gameStats.allPlayerStats[i].playerNotes.Clear();
			gameStats.allPlayerStats[i].stepsTaken = 0;
			gameStats.allPlayerStats[i].turnAmount = 0;
		}
	}

	public void WritePlayerNotes()
	{
		for (int i = 0; i < gameStats.allPlayerStats.Length; i++)
		{
			gameStats.allPlayerStats[i].isActivePlayer = allPlayerScripts[i].disconnectedMidGame || allPlayerScripts[i].isPlayerDead || allPlayerScripts[i].isPlayerControlled;
		}
		int num = 0;
		int num2 = 0;
		for (int j = 0; j < gameStats.allPlayerStats.Length; j++)
		{
			if (gameStats.allPlayerStats[j].isActivePlayer && (j == 0 || gameStats.allPlayerStats[j].stepsTaken < num))
			{
				num = gameStats.allPlayerStats[j].stepsTaken;
				num2 = j;
			}
		}
		if (connectedPlayersAmount > 0 && num > 10)
		{
			gameStats.allPlayerStats[num2].playerNotes.Add("The laziest employee.");
		}
		num = 0;
		for (int k = 0; k < gameStats.allPlayerStats.Length; k++)
		{
			if (gameStats.allPlayerStats[k].isActivePlayer && gameStats.allPlayerStats[k].turnAmount > num)
			{
				num = gameStats.allPlayerStats[k].turnAmount;
				num2 = k;
			}
		}
		if (connectedPlayersAmount > 0)
		{
			gameStats.allPlayerStats[num2].playerNotes.Add("The most paranoid employee.");
		}
		num = 0;
		for (int l = 0; l < gameStats.allPlayerStats.Length; l++)
		{
			if (gameStats.allPlayerStats[l].isActivePlayer && !allPlayerScripts[l].isPlayerDead && gameStats.allPlayerStats[l].damageTaken > num)
			{
				num = gameStats.allPlayerStats[l].damageTaken;
				num2 = l;
			}
		}
		if (connectedPlayersAmount > 0)
		{
			gameStats.allPlayerStats[num2].playerNotes.Add("Sustained the most injuries.");
		}
		num = 0;
		for (int m = 0; m < gameStats.allPlayerStats.Length; m++)
		{
			if (gameStats.allPlayerStats[m].isActivePlayer && gameStats.allPlayerStats[m].profitable > num)
			{
				num = gameStats.allPlayerStats[m].profitable;
				num2 = m;
			}
		}
		if (connectedPlayersAmount > 0 && num > 50)
		{
			if (num2 == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
			{
				localPlayerWasMostProfitableThisRound = true;
			}
			gameStats.allPlayerStats[num2].playerNotes.Add("Most profitable");
		}
	}

	private IEnumerator EndOfGame(int bodiesInsured = 0, int connectedPlayersOnServer = 0, int scrapCollected = 0)
	{
		if (!GameNetworkManager.Instance.disableSteam)
		{
			SteamFriends.SetRichPresence("moonname", currentLevel.PlanetName);
			GameNetworkManager.Instance.SetSteamFriendGrouping(GameNetworkManager.Instance.steamLobbyName, connectedPlayersAmount + 1, "#Status_Orbiting");
		}
		shipDoorsEnabled = false;
		Debug.Log($"Scrap collected: {scrapCollected}");
		if (currentLevel.currentWeather != LevelWeatherType.None)
		{
			WeatherEffect weatherEffect = TimeOfDay.Instance.effects[(int)currentLevel.currentWeather];
			if (weatherEffect != null && weatherEffect.effectPermanentObject != null)
			{
				weatherEffect.effectPermanentObject.SetActive(value: false);
			}
		}
		TimeOfDay.Instance.currentWeatherVariable = 0f;
		TimeOfDay.Instance.currentWeatherVariable2 = 0f;
		TimeOfDay.Instance.DisableAllWeather(deactivateObjects: true);
		TimeOfDay.Instance.currentLevelWeather = LevelWeatherType.None;
		TimeOfDay.Instance.movingGlobalTimeForward = false;
		TimeOfDay.Instance.currentDayTimeStarted = false;
		TimeOfDay.Instance.currentDayTime = 0f;
		TimeOfDay.Instance.dayMode = DayMode.Dawn;
		gameStats.daysSpent++;
		HUDManager.Instance.shipLeavingEarlyIcon.enabled = false;
		HUDManager.Instance.HideHUD(hide: true);
		HUDManager.Instance.quotaAnimator.SetBool("visible", value: false);
		yield return new WaitForSeconds(1f);
		if (currentLevel.planetHasTime)
		{
			if (isChallengeFile)
			{
				HUDManager.Instance.endgameStatsAnimator.SetTrigger("displayStatsChallenge");
			}
			else
			{
				HUDManager.Instance.endgameStatsAnimator.SetTrigger("displayStats");
			}
		}
		SwitchMapMonitorPurpose(displayInfo: true);
		yield return new WaitForSeconds(1f);
		RoundManager.Instance.DespawnPropsAtEndOfRound(isChallengeFile);
		RoundManager.Instance.bakedNavMesh = false;
		try
		{
			UnityEngine.Object.FindObjectOfType<MoldSpreadManager>().RemoveAllMold();
		}
		catch (Exception arg)
		{
			Debug.Log($"Error despawning mold: {arg}");
		}
		if (isChallengeFile)
		{
			ResetShipFurniture(onlyClearBoughtFurniture: true, despawnProps: false);
		}
		if (isChallengeFile)
		{
			Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
			terminal.groupCredits = terminal.startingCreditsAmount;
		}
		RoundManager.Instance.scrapCollectedThisRound.Clear();
		ResetPooledObjects();
		if (currentLevel.planetHasTime)
		{
			yield return new WaitForSeconds(8f);
			HUDManager.Instance.SetPlayerLevel(GameNetworkManager.Instance.localPlayerController.isPlayerDead, localPlayerWasMostProfitableThisRound, allPlayersDead);
			if (isChallengeFile)
			{
				HUDManager.Instance.FillChallengeResultsStats(scrapCollected);
				yield return new WaitForSeconds(2f);
			}
			displayedLevelResults = true;
		}
		localPlayerWasMostProfitableThisRound = false;
		int playersDead = connectedPlayersAmount + 1 - livingPlayers;
		ReviveDeadPlayers();
		RoundManager.Instance.ResetEnemyVariables();
		yield return new WaitForSeconds(3f);
		if (playersDead > 0 && !isChallengeFile)
		{
			HUDManager.Instance.endgameStatsAnimator.SetTrigger("displayPenalty");
			HUDManager.Instance.ApplyPenalty(playersDead, bodiesInsured);
			yield return new WaitForSeconds(4f);
		}
		PassTimeToNextDay(connectedPlayersOnServer);
		yield return new WaitForSeconds(1.7f);
		HUDManager.Instance.HideHUD(hide: false);
		shipIsLeaving = false;
		if (base.IsServer)
		{
			playersRevived++;
			yield return new WaitUntil(() => playersRevived >= GameNetworkManager.Instance.connectedPlayers);
			playersRevived = 0;
			bool flag = TimeOfDay.Instance.timeUntilDeadline <= 0f;
			if ((float)(TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled) <= 0f || isChallengeFile)
			{
				if (!isChallengeFile)
				{
					TimeOfDay.Instance.SetNewProfitQuota();
				}
				AllPlayersHaveRevivedClientRpc();
			}
			else if (flag)
			{
				FirePlayersAfterDeadlineClientRpc(GetEndgameStatsInOrder());
			}
			else
			{
				AllPlayersHaveRevivedClientRpc();
			}
		}
		else
		{
			PlayerHasRevivedServerRpc();
		}
	}

	private int[] GetEndgameStatsInOrder()
	{
		return new int[4] { gameStats.daysSpent, gameStats.scrapValueCollected, gameStats.deaths, gameStats.allStepsTaken };
	}

	private void PassTimeToNextDay(int connectedPlayersOnServer = 0)
	{
		if (isChallengeFile)
		{
			TimeOfDay.Instance.globalTime = 100f;
			SetMapScreenInfoToCurrentLevel();
			return;
		}
		float num = TimeOfDay.Instance.globalTimeAtEndOfDay - TimeOfDay.Instance.globalTime;
		_ = TimeOfDay.Instance.totalTime / TimeOfDay.Instance.lengthOfHours;
		if (currentLevel.planetHasTime || TimeOfDay.Instance.daysUntilDeadline <= 0)
		{
			TimeOfDay.Instance.timeUntilDeadline -= num;
			TimeOfDay.Instance.OnDayChanged();
		}
		TimeOfDay.Instance.globalTime = 100f;
		TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
		if (currentLevel.planetHasTime)
		{
			HUDManager.Instance.DisplayDaysLeft((int)Mathf.Floor(TimeOfDay.Instance.timeUntilDeadline / TimeOfDay.Instance.totalTime));
		}
		UnityEngine.Object.FindObjectOfType<Terminal>().SetItemSales();
		SetMapScreenInfoToCurrentLevel();
		if (TimeOfDay.Instance.timeUntilDeadline > 0f && TimeOfDay.Instance.daysUntilDeadline <= 0 && TimeOfDay.Instance.timesFulfilledQuota <= 0)
		{
			StartCoroutine(playDaysLeftAlertSFXDelayed());
		}
	}

	private IEnumerator playDaysLeftAlertSFXDelayed()
	{
		yield return new WaitForSeconds(3f);
		Instance.speakerAudioSource.PlayOneShot(zeroDaysLeftAlertSFX);
	}

	[ClientRpc]
	public void AllPlayersHaveRevivedClientRpc()
			{
				SetShipReadyToLand();
			}

	private void AutoSaveShipData()
	{
		HUDManager.Instance.saveDataIconAnimatorB.SetTrigger("save");
		GameNetworkManager.Instance.SaveGame();
	}

	[ServerRpc]
	public void ManuallyEjectPlayersServerRpc()
{if(inShipPhase && !isChallengeFile && !firingPlayersCutsceneRunning && fullyLoadedPlayers.Count >= GameNetworkManager.Instance.connectedPlayers)		{
			GameNetworkManager.Instance.gameHasStarted = true;
			firingPlayersCutsceneRunning = true;
			FirePlayersAfterDeadlineClientRpc(GetEndgameStatsInOrder());
		}
}
	[ClientRpc]
	public void FirePlayersAfterDeadlineClientRpc(int[] endGameStats, bool abridgedVersion = false)
{		{
			firingPlayersCutsceneRunning = true;
			if (UnityEngine.Object.FindObjectOfType<Terminal>().terminalInUse)
			{
				UnityEngine.Object.FindObjectOfType<Terminal>().QuitTerminal();
			}
			if (GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation && GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith != null)
			{
				GameNetworkManager.Instance.localPlayerController.currentTriggerInAnimationWith.StopSpecialAnimation();
			}
			HUDManager.Instance.EndOfRunStatsText.text = $"Days on the job: {endGameStats[0]}\n" + $"Scrap value collected: {endGameStats[1]}\n" + $"Deaths: {endGameStats[2]}\n" + $"Steps taken: {endGameStats[3]}";
			gameStats.daysSpent = 0;
			gameStats.scrapValueCollected = 0;
			gameStats.deaths = 0;
			gameStats.allStepsTaken = 0;
			SetDiscordStatusDetails();
			StartCoroutine(playersFiredGameOver(abridgedVersion));
		}
}
	private IEnumerator playersFiredGameOver(bool abridgedVersion)
	{
		yield return new WaitForSeconds(5f);
		shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", value: true);
		shipRoomLights.SetShipLightsOnLocalClientOnly(setLightsOn: false);
		speakerAudioSource.PlayOneShot(firedVoiceSFX);
		shipDoorAudioSource.PlayOneShot(alarmSFX);
		yield return new WaitForSeconds(9.37f);
		shipDoorsAnimator.SetBool("OpenInOrbit", value: true);
		shipDoorAudioSource.PlayOneShot(airPressureSFX);
		starSphereObject.SetActive(value: true);
		starSphereObject.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
		yield return new WaitForSeconds(0.25f);
		suckingPlayersOutOfShip = true;
		suckingFurnitureOutOfShip = true;
		PlaceableShipObject[] array = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].parentObject == null)
			{
				Debug.Log("Error! No parentObject for placeable object: " + unlockablesList.unlockables[array[i].unlockableID].unlockableName);
				continue;
			}
			array[i].parentObject.StartSuckingOutOfShip();
			if (unlockablesList.unlockables[array[i].unlockableID].spawnPrefab)
			{
				Collider[] componentsInChildren = array[i].parentObject.GetComponentsInChildren<Collider>();
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].enabled = false;
				}
			}
		}
		GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation = true;
		GameNetworkManager.Instance.localPlayerController.DropAllHeldItems();
		HUDManager.Instance.UIAudio.PlayOneShot(suckedIntoSpaceSFX);
		yield return new WaitForSeconds(6f);
		SoundManager.Instance.SetDiageticMixerSnapshot(3, 2f);
		HUDManager.Instance.ShowPlayersFiredScreen(show: true);
		UnityEngine.Object.FindObjectOfType<Terminal>().ClearBoughtItems();
		yield return new WaitForSeconds(2f);
		starSphereObject.SetActive(value: false);
		shipDoorAudioSource.Stop();
		speakerAudioSource.Stop();
		suckingFurnitureOutOfShip = false;
		if (base.IsServer)
		{
			GameNetworkManager.Instance.ResetSavedGameValues();
		}
		Debug.Log("Calling reset ship!");
		ResetShip();
		UnityEngine.Object.FindObjectOfType<Terminal>().SetItemSales();
		yield return new WaitForSeconds(6f);
		shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("AlarmRinging", value: false);
		GameNetworkManager.Instance.localPlayerController.TeleportPlayer(playerSpawnPositions[GameNetworkManager.Instance.localPlayerController.playerClientId].position);
		shipDoorsAnimator.SetBool("OpenInOrbit", value: false);
		currentPlanetPrefab.transform.position = planetContainer.transform.position;
		suckingPlayersOutOfShip = false;
		choseRandomFlyDirForPlayer = false;
		suckingPower = 0f;
		shipRoomLights.SetShipLightsOnLocalClientOnly(setLightsOn: true);
		yield return new WaitForSeconds(2f);
		if (base.IsServer)
		{
			playersRevived++;
			yield return new WaitUntil(() => playersRevived >= GameNetworkManager.Instance.connectedPlayers);
			playersRevived = 0;
			EndPlayersFiredSequenceClientRpc();
		}
		else
		{
			PlayerHasRevivedServerRpc();
		}
	}

	public void ResetShip()
	{
		TimeOfDay.Instance.globalTime = 100f;
		TimeOfDay.Instance.profitQuota = TimeOfDay.Instance.quotaVariables.startingQuota;
		TimeOfDay.Instance.quotaFulfilled = 0;
		TimeOfDay.Instance.timesFulfilledQuota = 0;
		TimeOfDay.Instance.timeUntilDeadline = (int)(TimeOfDay.Instance.totalTime * (float)TimeOfDay.Instance.quotaVariables.deadlineDaysAmount);
		TimeOfDay.Instance.UpdateProfitQuotaCurrentTime();
		randomMapSeed++;
		Debug.Log("Reset ship 0");
		companyBuyingRate = 0.3f;
		ChangeLevel(defaultPlanet);
		ChangePlanet();
		SetMapScreenInfoToCurrentLevel();
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		if (terminal != null)
		{
			terminal.groupCredits = TimeOfDay.Instance.quotaVariables.startingCredits;
		}
		ResetShipFurniture();
		ResetMoldStates();
		ResetPooledObjects(destroy: true);
		TimeOfDay.Instance.OnDayChanged();
	}

	private void ResetMoldStates()
	{
		UnityEngine.Object.FindObjectOfType<MoldSpreadManager>().ResetMoldData();
		for (int i = 0; i < levels.Length; i++)
		{
			levels[i].moldSpreadIterations = 0;
			levels[i].moldStartPosition = -1;
		}
	}

	private void ResetShipFurniture(bool onlyClearBoughtFurniture = false, bool despawnProps = true)
	{
		Debug.Log("Resetting ship furniture");
		if (base.IsServer)
		{
			for (int i = 0; i < unlockablesList.unlockables.Count; i++)
			{
				if (unlockablesList.unlockables[i].alreadyUnlocked || !unlockablesList.unlockables[i].spawnPrefab)
				{
					continue;
				}
				if (!SpawnedShipUnlockables.TryGetValue(i, out var value))
				{
					SpawnedShipUnlockables.Remove(i);
					continue;
				}
				if (value == null)
				{
					SpawnedShipUnlockables.Remove(i);
					continue;
				}
				SpawnedShipUnlockables.Remove(i);
				NetworkObject component = value.GetComponent<NetworkObject>();
				if (component != null && component.IsSpawned)
				{
					component.Despawn();
				}
			}
			if (despawnProps)
			{
				RoundManager.Instance.DespawnPropsAtEndOfRound(despawnAllItems: true);
			}
			closetLeftDoor.SetBoolOnClientOnly(setTo: false);
			closetRightDoor.SetBoolOnClientOnly(setTo: false);
		}
		ShipTeleporter.hasBeenSpawnedThisSession = false;
		ShipTeleporter.hasBeenSpawnedThisSessionInverse = false;
		if (!onlyClearBoughtFurniture)
		{
			PlaceableShipObject[] array = UnityEngine.Object.FindObjectsOfType<PlaceableShipObject>();
			for (int j = 0; j < array.Length; j++)
			{
				if (unlockablesList.unlockables[array[j].unlockableID].alreadyUnlocked && !unlockablesList.unlockables[array[j].unlockableID].spawnPrefab)
				{
					array[j].parentObject.disableObject = false;
					ShipBuildModeManager.Instance.ResetShipObjectToDefaultPosition(array[j]);
				}
			}
		}
		GameNetworkManager.Instance.ResetUnlockablesListValues(onlyClearBoughtFurniture);
		for (int k = 0; k < allPlayerScripts.Length; k++)
		{
			SoundManager.Instance.playerVoicePitchTargets[k] = 1f;
			allPlayerScripts[k].ResetPlayerBloodObjects();
			if (isChallengeFile)
			{
				UnlockableSuit.SwitchSuitForPlayer(allPlayerScripts[k], 24);
			}
			else
			{
				UnlockableSuit.SwitchSuitForPlayer(allPlayerScripts[k], 0);
			}
		}
	}

	[ClientRpc]
	public void EndPlayersFiredSequenceClientRpc()
{		{
			firingPlayersCutsceneRunning = false;
			timeAtStartOfRun = Time.realtimeSinceStartup;
			ReviveDeadPlayers();
			SoundManager.Instance.SetDiageticMixerSnapshot(0, 0.25f);
			HUDManager.Instance.ShowPlayersFiredScreen(show: false);
			GameNetworkManager.Instance.localPlayerController.inSpecialInteractAnimation = false;
			SetShipReadyToLand();
			SetDiscordStatusDetails();
			if (!isChallengeFile)
			{
				PlayFirstDayShipAnimation();
			}
		}
}
	private void PlayFirstDayShipAnimation(bool waitForMenuToClose = false)
	{
		StartCoroutine(firstDayAnimation(waitForMenuToClose));
	}

	private IEnumerator firstDayAnimation(bool waitForMenuToClose)
	{
		yield return new WaitForSeconds(5.5f);
		if (waitForMenuToClose)
		{
			QuickMenuManager quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
			yield return new WaitUntil(() => !quickMenu.isMenuOpen);
			yield return new WaitForSeconds(0.2f);
		}
		speakerAudioSource.PlayOneShot(shipIntroSpeechSFX);
	}

	public void DisableShipSpeaker()
	{
		DisableShipSpeakerLocalClient();
		StopShipSpeakerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopShipSpeakerServerRpc(int playerWhoTriggered)
			{
				StopShipSpeakerClientRpc(playerWhoTriggered);
			}

	[ClientRpc]
	public void StopShipSpeakerClientRpc(int playerWhoTriggered)
			{
				DisableShipSpeakerLocalClient();
			}

	private void DisableShipSpeakerLocalClient()
	{
		if (speakerAudioSource.isPlaying)
		{
			speakerAudioSource.Stop();
			speakerAudioSource.PlayOneShot(disableSpeakerSFX);
		}
	}

	public void LoadPlanetsMoldSpreadData()
	{
		for (int i = 0; i < levels.Length; i++)
		{
			int num = ES3.Load($"Level{levels[i].levelID}Mold", GameNetworkManager.Instance.currentSaveFileName, 0);
			levels[i].moldSpreadIterations = num;
			int num2 = ((num > 0) ? ES3.Load($"Level{levels[i].levelID}MoldOrigin", GameNetworkManager.Instance.currentSaveFileName, -1) : (-1));
			levels[i].moldStartPosition = num2;
			Debug.Log($"Loading from data: for {levels[i].PlanetName}: iterations at {num}, starting at node #{num2}");
		}
	}

	public void SetPlanetsWeather(int connectedPlayersOnServer = 0)
	{
		for (int i = 0; i < levels.Length; i++)
		{
			levels[i].currentWeather = LevelWeatherType.None;
			if (levels[i].overrideWeather)
			{
				levels[i].currentWeather = levels[i].overrideWeatherType;
			}
		}
		System.Random random = new System.Random(randomMapSeed + 35);
		List<SelectableLevel> list = levels.ToList();
		float num = 1f;
		if (connectedPlayersOnServer + 1 > 1 && daysPlayersSurvivedInARow > 2 && daysPlayersSurvivedInARow % 3 == 0)
		{
			num = (float)random.Next(15, 25) / 10f;
		}
		float num2 = Mathf.Clamp(planetsWeatherRandomCurve.Evaluate((float)random.NextDouble()) * num, 0f, 1f);
		Debug.Log($"Weather chance: {num2}; {num}");
		int num3 = Mathf.Clamp((int)(num2 * ((float)levels.Length - 2f)), 0, levels.Length);
		for (int j = 0; j < num3; j++)
		{
			SelectableLevel selectableLevel = list[random.Next(0, list.Count)];
			if (selectableLevel.randomWeathers != null && selectableLevel.randomWeathers.Length != 0)
			{
				selectableLevel.currentWeather = selectableLevel.randomWeathers[random.Next(0, selectableLevel.randomWeathers.Length)].weatherType;
			}
			list.Remove(selectableLevel);
		}
	}

	private void SetShipReadyToLand()
	{
		if (Instance.isChallengeFile)
		{
			hasSubmittedChallengeRank = true;
			TimeOfDay.Instance.timeUntilDeadline = TimeOfDay.Instance.totalTime;
		}
		inShipPhase = true;
		shipLeftAutomatically = false;
		SetDiscordStatusDetails();
		if (currentLevel.planetHasTime && TimeOfDay.Instance.GetDayPhase(TimeOfDay.Instance.CalculatePlanetTime(currentLevel) / TimeOfDay.Instance.totalTime) == DayMode.Midnight)
		{
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.disabledHoverTip = "Too late on moon to land!";
		}
		else
		{
			UnityEngine.Object.FindObjectOfType<StartMatchLever>().triggerScript.interactable = true;
		}
		HUDManager.Instance.loadingText.text = "";
		AutoSaveShipData();
		StartCoroutine(playRandomShipAudio());
		SoundManager.Instance.ResetRandomSeed();
	}

	private IEnumerator playRandomShipAudio()
	{
		System.Random shipRandom = new System.Random(randomMapSeed);
		if (shipRandom.Next(0, 100) <= 4)
		{
			yield return new WaitForSeconds(shipRandom.Next(7, 35));
			if (inShipPhase)
			{
				RoundManager.PlayRandomClip(shipAmbianceAudio, shipCreakSFX, randomize: false, (float)shipRandom.Next(0, 10) / 10f);
			}
		}
	}

	private IEnumerator ResetDissonanceCommsComponent()
	{
		voiceChatModule.enabled = false;
		yield return new WaitForSeconds(3f);
		voiceChatModule.enabled = true;
		voiceChatModule.ResetMicrophoneCapture();
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerHasRevivedServerRpc()
			{
				playersRevived++;
			}

	private IEnumerator waitingForOtherPlayersToRevive()
	{
		yield return new WaitForSeconds(2f);
		if (!inShipPhase)
		{
			HUDManager.Instance.loadingText.enabled = true;
			HUDManager.Instance.loadingText.text = "Waiting for crew...";
		}
	}

	public void ReviveDeadPlayers()
	{
		allPlayersDead = false;
		for (int i = 0; i < allPlayerScripts.Length; i++)
		{
			Debug.Log("Reviving players A");
			allPlayerScripts[i].ResetPlayerBloodObjects(allPlayerScripts[i].isPlayerDead);
			if (!allPlayerScripts[i].isPlayerDead && !allPlayerScripts[i].isPlayerControlled)
			{
				continue;
			}
			allPlayerScripts[i].isClimbingLadder = false;
			if (!allPlayerScripts[i].inSpecialInteractAnimation || allPlayerScripts[i].currentTriggerInAnimationWith == null || !allPlayerScripts[i].currentTriggerInAnimationWith.GetComponentInChildren<MoveToExitSpecialAnimation>())
			{
				allPlayerScripts[i].clampLooking = false;
				allPlayerScripts[i].gameplayCamera.transform.localEulerAngles = new Vector3(allPlayerScripts[i].gameplayCamera.transform.localEulerAngles.x, 0f, allPlayerScripts[i].gameplayCamera.transform.localEulerAngles.z);
				allPlayerScripts[i].inVehicleAnimation = false;
			}
			allPlayerScripts[i].disableMoveInput = false;
			allPlayerScripts[i].ResetZAndXRotation();
			allPlayerScripts[i].thisController.enabled = true;
			allPlayerScripts[i].health = 100;
			allPlayerScripts[i].hasBeenCriticallyInjured = false;
			allPlayerScripts[i].disableLookInput = false;
			allPlayerScripts[i].disableInteract = false;
			allPlayerScripts[i].nightVisionRadar.enabled = false;
			Debug.Log("Reviving players B");
			if (allPlayerScripts[i].isPlayerDead)
			{
				allPlayerScripts[i].isPlayerDead = false;
				allPlayerScripts[i].isPlayerControlled = true;
				allPlayerScripts[i].isInElevator = true;
				allPlayerScripts[i].isInHangarShipRoom = true;
				allPlayerScripts[i].isInsideFactory = false;
				allPlayerScripts[i].parentedToElevatorLastFrame = false;
				allPlayerScripts[i].overrideGameOverSpectatePivot = null;
				SetPlayerObjectExtrapolate(enable: false);
				allPlayerScripts[i].TeleportPlayer(GetPlayerSpawnPosition(i));
				allPlayerScripts[i].setPositionOfDeadPlayer = false;
				allPlayerScripts[i].DisablePlayerModel(allPlayerObjects[i], enable: true, disableLocalArms: true);
				allPlayerScripts[i].helmetLight.enabled = false;
				Debug.Log("Reviving players C");
				allPlayerScripts[i].Crouch(crouch: false);
				allPlayerScripts[i].criticallyInjured = false;
				if (allPlayerScripts[i].playerBodyAnimator != null)
				{
					allPlayerScripts[i].playerBodyAnimator.SetBool("Limp", value: false);
				}
				allPlayerScripts[i].bleedingHeavily = false;
				allPlayerScripts[i].activatingItem = false;
				allPlayerScripts[i].twoHanded = false;
				allPlayerScripts[i].inShockingMinigame = false;
				allPlayerScripts[i].inSpecialInteractAnimation = false;
				allPlayerScripts[i].freeRotationInInteractAnimation = false;
				allPlayerScripts[i].disableSyncInAnimation = false;
				allPlayerScripts[i].inAnimationWithEnemy = null;
				allPlayerScripts[i].holdingWalkieTalkie = false;
				allPlayerScripts[i].speakingToWalkieTalkie = false;
				Debug.Log("Reviving players D");
				allPlayerScripts[i].isSinking = false;
				allPlayerScripts[i].isUnderwater = false;
				allPlayerScripts[i].sinkingValue = 0f;
				allPlayerScripts[i].statusEffectAudio.Stop();
				allPlayerScripts[i].DisableJetpackControlsLocally();
				allPlayerScripts[i].health = 100;
				Debug.Log("Reviving players E");
				allPlayerScripts[i].mapRadarDotAnimator.SetBool("dead", value: false);
				allPlayerScripts[i].externalForceAutoFade = Vector3.zero;
				if (allPlayerScripts[i].IsOwner)
				{
					HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
					allPlayerScripts[i].hasBegunSpectating = false;
					HUDManager.Instance.RemoveSpectateUI();
					HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
					allPlayerScripts[i].hinderedMultiplier = 1f;
					allPlayerScripts[i].isMovementHindered = 0;
					allPlayerScripts[i].sourcesCausingSinking = 0;
					Debug.Log("Reviving players E2");
					allPlayerScripts[i].reverbPreset = shipReverb;
				}
			}
			Debug.Log("Reviving players F");
			SoundManager.Instance.earsRingingTimer = 0f;
			allPlayerScripts[i].voiceMuffledByEnemy = false;
			SoundManager.Instance.playerVoicePitchTargets[i] = 1f;
			SoundManager.Instance.SetPlayerPitch(1f, i);
			if (allPlayerScripts[i].currentVoiceChatIngameSettings == null)
			{
				RefreshPlayerVoicePlaybackObjects();
			}
			if (allPlayerScripts[i].currentVoiceChatIngameSettings != null)
			{
				if (allPlayerScripts[i].currentVoiceChatIngameSettings.voiceAudio == null)
				{
					allPlayerScripts[i].currentVoiceChatIngameSettings.InitializeComponents();
				}
				if (allPlayerScripts[i].currentVoiceChatIngameSettings.voiceAudio == null)
				{
					return;
				}
				allPlayerScripts[i].currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
			}
			Debug.Log("Reviving players G");
		}
		PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
		playerControllerB.bleedingHeavily = false;
		playerControllerB.criticallyInjured = false;
		playerControllerB.playerBodyAnimator.SetBool("Limp", value: false);
		playerControllerB.health = 100;
		HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);
		playerControllerB.spectatedPlayerScript = null;
		HUDManager.Instance.audioListenerLowPass.enabled = false;
		Debug.Log("Reviving players H");
		SetSpectateCameraToGameOverMode(enableGameOver: false, playerControllerB);
		RagdollGrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>();
		for (int j = 0; j < array.Length; j++)
		{
			if (!array[j].isHeld)
			{
				if (base.IsServer)
				{
					if (array[j].NetworkObject.IsSpawned)
					{
						array[j].NetworkObject.Despawn();
					}
					else
					{
						UnityEngine.Object.Destroy(array[j].gameObject);
					}
				}
			}
			else if (array[j].isHeld && array[j].playerHeldBy != null)
			{
				array[j].playerHeldBy.DropAllHeldItems();
			}
		}
		DeadBodyInfo[] array2 = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>(includeInactive: true);
		for (int k = 0; k < array2.Length; k++)
		{
			UnityEngine.Object.Destroy(array2[k].gameObject);
		}
		livingPlayers = connectedPlayersAmount + 1;
		allPlayersDead = false;
		UpdatePlayerVoiceEffects();
		ResetMiscValues();
	}

	private void ResetMiscValues()
	{
		shipAnimator.ResetTrigger("ShipLeave");
	}

	public void RefreshPlayerVoicePlaybackObjects()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		PlayerVoiceIngameSettings[] array = UnityEngine.Object.FindObjectsOfType<PlayerVoiceIngameSettings>(includeInactive: true);
		Debug.Log($"Refreshing voice playback objects. Number of voice objects found: {array.Length}");
		for (int i = 0; i < allPlayerScripts.Length; i++)
		{
			PlayerControllerB playerControllerB = allPlayerScripts[i];
			if (!playerControllerB.isPlayerControlled && !playerControllerB.isPlayerDead)
			{
				Debug.Log($"Skipping player #{i} as they are not controlled or dead");
				continue;
			}
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j]._playerState == null)
				{
					array[j].FindPlayerIfNull();
					if (array[j]._playerState == null)
					{
						Debug.LogError($"Unable to connect player to voice B #{i}; {array[j].isActiveAndEnabled}; {array[j]._playerState == null}");
					}
				}
				else if (!array[j].isActiveAndEnabled)
				{
					Debug.LogError($"Unable to connect player to voice A #{i}; {array[j].isActiveAndEnabled}; {array[j]._playerState == null}");
				}
				else if (array[j]._playerState.Name == playerControllerB.gameObject.GetComponentInChildren<NfgoPlayer>().PlayerId)
				{
					Debug.Log($"Found a match for voice object #{j} and player object #{i}");
					playerControllerB.voicePlayerState = array[j]._playerState;
					playerControllerB.currentVoiceChatAudioSource = array[j].voiceAudio;
					playerControllerB.currentVoiceChatIngameSettings = array[j];
					playerControllerB.currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB.playerClientId];
					Debug.Log($"player voice chat audiosource: {playerControllerB.currentVoiceChatAudioSource}; set audiomixer to {SoundManager.Instance.playerVoiceMixers[playerControllerB.playerClientId]} ; {playerControllerB.currentVoiceChatAudioSource.outputAudioMixerGroup} ; {playerControllerB.playerClientId}");
				}
			}
		}
	}

	public void UpdatePlayerVoiceEffects()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		updatePlayerVoiceInterval = 2f;
		PlayerControllerB playerControllerB = ((!GameNetworkManager.Instance.localPlayerController.isPlayerDead || !(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)) ? GameNetworkManager.Instance.localPlayerController : GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript);
		for (int i = 0; i < allPlayerScripts.Length; i++)
		{
			PlayerControllerB playerControllerB2 = allPlayerScripts[i];
			if ((!playerControllerB2.isPlayerControlled && !playerControllerB2.isPlayerDead) || playerControllerB2 == GameNetworkManager.Instance.localPlayerController)
			{
				continue;
			}
			if (playerControllerB2.voicePlayerState == null || playerControllerB2.currentVoiceChatIngameSettings._playerState == null || playerControllerB2.currentVoiceChatAudioSource == null)
			{
				RefreshPlayerVoicePlaybackObjects();
				if (playerControllerB2.voicePlayerState == null || playerControllerB2.currentVoiceChatAudioSource == null)
				{
					Debug.Log($"Was not able to access voice chat object for player #{i}; {playerControllerB2.voicePlayerState == null}; {playerControllerB2.currentVoiceChatAudioSource == null}");
					continue;
				}
			}
			AudioSource currentVoiceChatAudioSource = allPlayerScripts[i].currentVoiceChatAudioSource;
			bool flag = playerControllerB2.speakingToWalkieTalkie && playerControllerB.holdingWalkieTalkie && playerControllerB2 != playerControllerB;
			if (playerControllerB2.isPlayerDead)
			{
				currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().enabled = false;
				currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = false;
				currentVoiceChatAudioSource.panStereo = 0f;
				SoundManager.Instance.playerVoicePitchTargets[playerControllerB2.playerClientId] = 1f;
				SoundManager.Instance.SetPlayerPitch(1f, (int)playerControllerB2.playerClientId);
				if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
				{
					currentVoiceChatAudioSource.spatialBlend = 0f;
					playerControllerB2.currentVoiceChatIngameSettings.set2D = true;
					playerControllerB2.voicePlayerState.Volume = 1f;
				}
				else
				{
					currentVoiceChatAudioSource.spatialBlend = 1f;
					playerControllerB2.currentVoiceChatIngameSettings.set2D = false;
					playerControllerB2.voicePlayerState.Volume = 0f;
				}
				continue;
			}
			AudioLowPassFilter component = currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>();
			OccludeAudio component2 = currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
			component.enabled = true;
			component2.overridingLowPass = flag || allPlayerScripts[i].voiceMuffledByEnemy;
			currentVoiceChatAudioSource.GetComponent<AudioHighPassFilter>().enabled = flag;
			if (!flag)
			{
				currentVoiceChatAudioSource.spatialBlend = 1f;
				playerControllerB2.currentVoiceChatIngameSettings.set2D = false;
				currentVoiceChatAudioSource.bypassListenerEffects = false;
				currentVoiceChatAudioSource.bypassEffects = false;
				currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
				component.lowpassResonanceQ = 1f;
			}
			else
			{
				currentVoiceChatAudioSource.spatialBlend = 0f;
				playerControllerB2.currentVoiceChatIngameSettings.set2D = true;
				if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
				{
					currentVoiceChatAudioSource.panStereo = 0f;
					currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
					currentVoiceChatAudioSource.bypassListenerEffects = false;
					currentVoiceChatAudioSource.bypassEffects = false;
				}
				else
				{
					currentVoiceChatAudioSource.panStereo = 0.4f;
					currentVoiceChatAudioSource.bypassListenerEffects = false;
					currentVoiceChatAudioSource.bypassEffects = false;
					currentVoiceChatAudioSource.outputAudioMixerGroup = SoundManager.Instance.playerVoiceMixers[playerControllerB2.playerClientId];
				}
				component2.lowPassOverride = 4000f;
				component.lowpassResonanceQ = 3f;
			}
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				playerControllerB2.voicePlayerState.Volume = 0.8f;
			}
			else
			{
				playerControllerB2.voicePlayerState.Volume = 1f;
			}
		}
	}

	[ServerRpc]
	public void SetShipDoorsOverheatServerRpc()
{		{
			SetShipDoorsOverheatClientRpc();
		}
}
	[ClientRpc]
	public void SetShipDoorsOverheatClientRpc()
{if(!base.IsServer)			{
				SetShipDoorsOverheatLocalClient();
			}
}
	public void SetShipDoorsOverheatLocalClient()
	{
		HangarShipDoor hangarShipDoor = UnityEngine.Object.FindObjectOfType<HangarShipDoor>();
		hangarShipDoor.PlayDoorAnimation(closed: false);
		hangarShipDoor.overheated = true;
		hangarShipDoor.triggerScript.interactable = false;
	}

	public void SetShipDoorsClosed(bool closed)
	{
		hangarDoorsClosed = closed;
		SetPlayerSafeInShip();
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetDoorsClosedServerRpc(bool closed)
			{
				SetDoorsClosedClientRpc(closed);
			}

	[ClientRpc]
	public void SetDoorsClosedClientRpc(bool closed)
			{
				SetShipDoorsClosed(closed);
			}

	public void SetPlayerSafeInShip()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
		if (playerControllerB.isPlayerDead && playerControllerB.spectatedPlayerScript != null)
		{
			playerControllerB = playerControllerB.spectatedPlayerScript;
		}
		EnemyAI[] array = UnityEngine.Object.FindObjectsOfType<EnemyAI>();
		if (hangarDoorsClosed && GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i].EnableEnemyMesh(array[i].isInsidePlayerShip);
			}
		}
		else
		{
			for (int j = 0; j < array.Length; j++)
			{
				array[j].EnableEnemyMesh(enable: true);
			}
		}
	}

	public bool CanChangeLevels()
	{
		if (!travellingToNewLevel)
		{
			return inShipPhase;
		}
		return false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void ChangeLevelServerRpc(int levelID, int newGroupCreditsAmount)
{		{
			Debug.Log($"Changing level server rpc {levelID}");
			if (!travellingToNewLevel && inShipPhase && newGroupCreditsAmount <= UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits && !isChallengeFile)
			{
				UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = newGroupCreditsAmount;
				travellingToNewLevel = true;
				ChangeLevelClientRpc(levelID, newGroupCreditsAmount);
			}
			else
			{
				CancelChangeLevelClientRpc(UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits);
			}
		}
}
	[ClientRpc]
	public void CancelChangeLevelClientRpc(int groupCreditsAmount)
			{
				UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = groupCreditsAmount;
				UnityEngine.Object.FindObjectOfType<Terminal>().useCreditsCooldown = false;
			}

	[ClientRpc]
	public void ChangeLevelClientRpc(int levelID, int newGroupCreditsAmount)
{		{
			UnityEngine.Object.FindObjectOfType<Terminal>().useCreditsCooldown = false;
			ChangeLevel(levelID);
			travellingToNewLevel = true;
			if (shipTravelCoroutine != null)
			{
				StopCoroutine(shipTravelCoroutine);
			}
			shipTravelCoroutine = StartCoroutine(TravelToLevelEffects());
			if (!base.IsServer)
			{
				UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits = newGroupCreditsAmount;
			}
		}
}
	public void ChangeLevel(int levelID)
	{
		Debug.Log($"level id: {levelID}");
		Debug.Log("Changing level");
		currentLevel = levels[levelID];
		currentLevelID = levelID;
		TimeOfDay.Instance.currentLevel = currentLevel;
		RoundManager.Instance.currentLevel = levels[levelID];
		SoundManager.Instance.ResetSoundType();
	}

	private IEnumerator TravelToLevelEffects()
	{
		StartMatchLever lever = UnityEngine.Object.FindObjectOfType<StartMatchLever>();
		lever.triggerScript.interactable = false;
		shipAmbianceAudio.PlayOneShot(shipDepartSFX);
		currentPlanetAnimator.SetTrigger("FlyAway");
		shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("FlyingToNewPlanet", value: true);
		HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
		yield return new WaitForSeconds(2f);
		if (currentPlanetPrefab != null)
		{
			UnityEngine.Object.Destroy(currentPlanetPrefab);
		}
		yield return new WaitForSeconds(currentLevel.timeToArrive);
		ArriveAtLevel();
		if (base.IsServer || GameNetworkManager.Instance.gameHasStarted)
		{
			lever.triggerScript.interactable = true;
		}
		for (int i = 0; i < 20; i++)
		{
			shipAmbianceAudio.volume -= 0.05f;
			yield return null;
		}
		yield return new WaitForSeconds(0.02f);
		shipAmbianceAudio.Stop();
		shipAmbianceAudio.volume = 1f;
		shipAmbianceAudio.PlayOneShot(shipArriveSFX);
	}

	public void ArriveAtLevel()
	{
		Debug.Log($"Level id: {currentLevel.levelID}");
		TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
		outerSpaceSunAnimator.SetFloat("currentTime", timeOfDay.CalculatePlanetTime(currentLevel) / timeOfDay.totalTime + 1f);
		timeOfDay.currentLevel = currentLevel;
		travellingToNewLevel = false;
		ChangePlanet();
		currentPlanetAnimator.SetTrigger("FlyTo");
		shipAnimatorObject.gameObject.GetComponent<Animator>().SetBool("FlyingToNewPlanet", value: false);
		SetMapScreenInfoToCurrentLevel();
		UnityEngine.Object.FindObjectOfType<StartMatchLever>().hasDisplayedTimeWarning = false;
		Debug.Log($"Current level null?: {currentLevel == null}");
		Debug.Log("Current level planet name: " + currentLevel.PlanetName);
		if (!GameNetworkManager.Instance.disableSteam)
		{
			SteamFriends.SetRichPresence("moonname", currentLevel.PlanetName);
			GameNetworkManager.Instance.SetSteamFriendGrouping(GameNetworkManager.Instance.steamLobbyName, connectedPlayersAmount + 1, "#Status_Orbiting");
		}
		SetDiscordStatusDetails();
	}

	public void ChangePlanet()
	{
		if (currentPlanetPrefab != null)
		{
			UnityEngine.Object.Destroy(currentPlanetPrefab);
		}
		currentPlanetPrefab = UnityEngine.Object.Instantiate(currentLevel.planetPrefab, planetContainer, worldPositionStays: false);
		currentPlanetAnimator = currentPlanetPrefab.GetComponentInChildren<Animator>();
		UnityEngine.Object.FindObjectOfType<TimeOfDay>().currentLevel = currentLevel;
		SetMapScreenInfoToCurrentLevel();
	}

	public void SetMapScreenInfoToCurrentLevel()
	{
		screenLevelVideoReel.enabled = false;
		screenLevelVideoReel.gameObject.SetActive(value: false);
		screenLevelVideoReel.clip = currentLevel.videoReel;
		TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
		if (timeOfDay.totalTime == 0f)
		{
			timeOfDay.totalTime = (float)timeOfDay.numberOfHours * timeOfDay.lengthOfHours;
		}
		string text = ((currentLevel.currentWeather == LevelWeatherType.None) ? "" : ("Weather: " + currentLevel.currentWeather));
		string levelDescription = currentLevel.LevelDescription;
		if (isChallengeFile)
		{
			screenLevelDescription.text = "Orbiting: " + GameNetworkManager.Instance.GetNameForWeekNumber() + "\n" + levelDescription + "\n" + text;
		}
		else
		{
			screenLevelDescription.text = "Orbiting: " + currentLevel.PlanetName + "\n" + levelDescription + "\n" + text;
		}
		mapScreen.overrideCameraForOtherUse = true;
		mapScreen.cam.transform.position = new Vector3(0f, 100f, 0f);
		screenLevelDescription.enabled = true;
		mapScreen.mapCamera.nearClipPlane = -0.96f;
		mapScreen.mapCamera.farClipPlane = 7.52f;
		radarCanvas.planeDistance = -0.93f;
		if (currentLevel.videoReel != null && !isChallengeFile)
		{
			screenLevelVideoReel.enabled = true;
			screenLevelVideoReel.gameObject.SetActive(value: true);
			screenLevelVideoReel.Play();
		}
	}

	public void SwitchMapMonitorPurpose(bool displayInfo = false)
	{
		if (displayInfo)
		{
			screenLevelVideoReel.enabled = true;
			screenLevelVideoReel.gameObject.SetActive(value: true);
			screenLevelDescription.enabled = true;
			mapScreenPlayerName.enabled = false;
			mapScreen.localPlayerPlaceholder.enabled = false;
			mapScreenPlayerNameBG.enabled = false;
			mapScreen.overrideCameraForOtherUse = true;
			mapScreen.SwitchScreenOn();
			mapScreen.cam.enabled = true;
			mapScreen.compassRose.enabled = false;
			Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
			terminal.displayingPersistentImage = null;
			terminal.terminalImage.enabled = false;
		}
		else
		{
			screenLevelVideoReel.enabled = false;
			screenLevelVideoReel.gameObject.SetActive(value: false);
			screenLevelDescription.enabled = false;
			mapScreenPlayerName.enabled = true;
			mapScreenPlayerNameBG.enabled = true;
			mapScreen.overrideCameraForOtherUse = false;
			mapScreen.compassRose.enabled = true;
		}
	}

	public void PowerSurgeShip()
	{
		mapScreen.SwitchScreenOn(on: false);
		if (base.IsServer)
		{
			if ((bool)UnityEngine.Object.FindObjectOfType<TVScript>())
			{
				UnityEngine.Object.FindObjectOfType<TVScript>().TurnOffTVServerRpc();
			}
			shipRoomLights.SetShipLightsServerRpc(setLightsOn: false);
			SurgePowerServerRpc();
		}
	}

	[ServerRpc]
	public void SurgePowerServerRpc()
{		{
			SurgePowerClientRpc();
		}
}
	[ClientRpc]
	public void SurgePowerClientRpc()
			{
				ShipPowerSurgedEvent.Invoke();
			}

	[ServerRpc(RequireOwnership = false)]
	public void SyncCompanyBuyingRateServerRpc()
			{
				SyncCompanyBuyingRateClientRpc(companyBuyingRate);
			}

	[ClientRpc]
	public void SyncCompanyBuyingRateClientRpc(float buyingRate)
			{
				companyBuyingRate = buyingRate;
			}

	private void TeleportPlayerInShipIfOutOfRoomBounds()
	{
		if (!(testRoom != null) && !shipInnerRoomBounds.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position))
		{
			GameNetworkManager.Instance.localPlayerController.TeleportPlayer(GetPlayerSpawnPosition((int)GameNetworkManager.Instance.localPlayerController.playerClientId, simpleTeleport: true));
		}
	}

	public void LateUpdate()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		if (updatePlayerVoiceInterval > 5f)
		{
			updatePlayerVoiceInterval = 0f;
			UpdatePlayerVoiceEffects();
		}
		else
		{
			updatePlayerVoiceInterval += Time.deltaTime;
		}
		if (!inShipPhase && shipDoorsEnabled && !suckingPlayersOutOfShip)
		{
			if (GameNetworkManager.Instance.localPlayerController.transform.position.y < -600f)
			{
				GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Gravity);
			}
			else if (GameNetworkManager.Instance.localPlayerController.isInElevator && !shipBounds.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position) && GameNetworkManager.Instance.localPlayerController.thisController.isGrounded)
			{
				GameNetworkManager.Instance.localPlayerController.SetAllItemsInElevator(inShipRoom: false, inElevator: false);
				GameNetworkManager.Instance.localPlayerController.isInElevator = false;
				GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
			}
			else if (!GameNetworkManager.Instance.localPlayerController.isInElevator && shipBounds.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position) && GameNetworkManager.Instance.localPlayerController.thisController.isGrounded)
			{
				GameNetworkManager.Instance.localPlayerController.isInElevator = true;
				if (shipInnerRoomBounds.bounds.Contains(GameNetworkManager.Instance.localPlayerController.transform.position) && GameNetworkManager.Instance.localPlayerController.thisController.isGrounded)
				{
					GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = true;
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.SetAllItemsInElevator(inShipRoom: false, inElevator: true);
				}
			}
		}
		else if (!suckingPlayersOutOfShip)
		{
			TeleportPlayerInShipIfOutOfRoomBounds();
		}
		if (suckingPlayersOutOfShip)
		{
			starSphereObject.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
			currentPlanetPrefab.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position + new Vector3(-101f, -65f, 160f);
		}
		if (fearLevelIncreasing)
		{
			fearLevelIncreasing = false;
		}
		else if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			fearLevel -= Time.deltaTime * 0.5f;
		}
		else
		{
			fearLevel -= Time.deltaTime * 0.055f;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
	}

	[ServerRpc]
	public void Debug_EnableTestRoomServerRpc(bool enable)
{if(!Application.isEditor)		{
			return;
		}
		if (enable)
		{
			testRoom = UnityEngine.Object.Instantiate(testRoomPrefab, testRoomSpawnPosition.position, testRoomSpawnPosition.rotation, testRoomSpawnPosition);
			testRoom.GetComponent<NetworkObject>().Spawn();
		}
		else if (Instance.testRoom != null)
		{
			if (!testRoom.GetComponent<NetworkObject>().IsSpawned)
			{
				UnityEngine.Object.Destroy(testRoom);
			}
			else
			{
				testRoom.GetComponent<NetworkObject>().Despawn();
			}
		}
		if (enable)
		{
			Debug_EnableTestRoomClientRpc(enable, testRoom.GetComponent<NetworkObject>());
		}
		else
		{
			Debug_EnableTestRoomClientRpc(enable);
		}
}
	public bool IsClientFriendsWithHost()
	{
		if (!GameNetworkManager.Instance.disableSteam && !NetworkManager.Singleton.IsServer)
		{
			SteamFriends.GetFriends().ToList();
			Friend friend = new Friend(allPlayerScripts[0].playerSteamId);
			Debug.Log($"Host steam friend id: {allPlayerScripts[0].playerSteamId}, user: {friend.Name}; is friend?: {friend.IsFriend}");
			if (!friend.IsFriend)
			{
				return false;
			}
		}
		return true;
	}

	[ClientRpc]
	public void Debug_EnableTestRoomClientRpc(bool enable, NetworkObjectReference objectRef = default(NetworkObjectReference))
{if(!(GameNetworkManager.Instance.localPlayerController == null) && IsClientFriendsWithHost())		{
			QuickMenuManager quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
			for (int i = 0; i < quickMenuManager.doorGameObjects.Length; i++)
			{
				quickMenuManager.doorGameObjects[i].SetActive(!enable);
			}
			quickMenuManager.outOfBoundsCollider.enabled = !enable;
			if (enable)
			{
				StartCoroutine(SetTestRoomDebug(objectRef));
			}
			else if (testRoom != null)
			{
				UnityEngine.Object.Destroy(testRoom);
			}
		}
}
	private IEnumerator SetTestRoomDebug(NetworkObjectReference objectRef)
	{
		NetworkObject testRoomNetObject = null;
		yield return new WaitUntil(() => objectRef.TryGet(out testRoomNetObject));
		if (!(testRoomNetObject == null))
		{
			Instance.testRoom = testRoomNetObject.gameObject;
		}
	}

	[ServerRpc]
	public void Debug_ToggleAllowDeathServerRpc()
{if(Application.isEditor)		{
			allowLocalPlayerDeath = !allowLocalPlayerDeath;
			Debug_ToggleAllowDeathClientRpc(allowLocalPlayerDeath);
		}
}
	[ClientRpc]
	public void Debug_ToggleAllowDeathClientRpc(bool allowDeath)
{if(IsClientFriendsWithHost() && !base.IsServer)			{
				allowLocalPlayerDeath = allowDeath;
			}
}
	[ServerRpc]
	public void Debug_ReviveAllPlayersServerRpc()
{if(Application.isEditor)		{
			Debug_ReviveAllPlayersClientRpc();
		}
}
	[ClientRpc]
	public void Debug_ReviveAllPlayersClientRpc()
{if(IsClientFriendsWithHost() && !base.IsServer)			{
				ReviveDeadPlayers();
				HUDManager.Instance.HideHUD(hide: false);
			}
}
	public void SetDiscordStatusDetails()
	{
		if (DiscordController.Instance == null || GameNetworkManager.Instance.disableSteam)
		{
			return;
		}
		DiscordController.Instance.status_largeText = "";
		if (currentLevel != null)
		{
			if (firingPlayersCutsceneRunning)
			{
				DiscordController.Instance.status_Details = "Getting fired";
				DiscordController.Instance.status_largeImage = "mapfired";
			}
			else if (!GameNetworkManager.Instance.gameHasStarted)
			{
				DiscordController.Instance.status_Details = "In orbit (Waiting for crew)";
				DiscordController.Instance.status_largeImage = "mapshipicon";
			}
			else if (inShipPhase)
			{
				DiscordController.Instance.status_Details = "Orbiting " + currentLevel.PlanetName;
				DiscordController.Instance.status_largeImage = "mapshipicon";
			}
			else
			{
				DiscordController.Instance.status_Details = HUDManager.Instance.SetClock(TimeOfDay.Instance.normalizedTimeOfDay, TimeOfDay.Instance.numberOfHours, createNewLine: false);
				DiscordController.Instance.status_largeText = "On " + currentLevel.PlanetName;
				if (currentLevel.levelIconString != "")
				{
					DiscordController.Instance.status_largeImage = currentLevel.levelIconString;
				}
			}
			if (GameNetworkManager.Instance.localPlayerController != null)
			{
				if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
				{
					DiscordController.Instance.status_smallText = "Deceased";
					DiscordController.Instance.status_smallImage = "faceiconwhitev1big";
				}
				else
				{
					DiscordController.Instance.status_smallText = "";
					DiscordController.Instance.status_smallImage = "faceiconorangev1big";
				}
			}
		}
		else
		{
			DiscordController.Instance.status_Details = "In orbit";
			DiscordController.Instance.status_smallText = "";
			DiscordController.Instance.status_smallImage = "faceiconorangev1big";
		}
		DiscordController.Instance.currentPartySize = connectedPlayersAmount + 1;
		DiscordController.Instance.maxPartySize = GameNetworkManager.Instance.maxAllowedPlayers;
		if (RoundManager.Instance != null && inShipPhase)
		{
			float num = (float)GetValueOfAllScrap() / (float)TimeOfDay.Instance.profitQuota * 100f;
			DiscordController.Instance.status_State = $"{(int)num}% of quota | {TimeOfDay.Instance.daysUntilDeadline} days left";
		}
		DiscordController.Instance.timeElapsed = (int)(Time.realtimeSinceStartup - timeAtStartOfRun) / 60;
		if (GameNetworkManager.Instance.currentLobby.HasValue)
		{
			DiscordController.Instance.status_partyId = Convert.ToString(GameNetworkManager.Instance.currentLobby.Value.Owner.Id);
		}
		DiscordController.Instance.UpdateStatus(clear: false);
	}

	public int GetValueOfAllScrap(bool onlyScrapCollected = true, bool onlyNewScrap = false)
	{
		GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
		int num = 0;
		for (int i = 0; i < array.Length; i++)
		{
			if (shipInnerRoomBounds.bounds.Contains(array[i].transform.position))
			{
				array[i].isInShipRoom = true;
			}
		}
		for (int j = 0; j < array.Length; j++)
		{
			if ((!onlyNewScrap || !array[j].scrapPersistedThroughRounds) && array[j].itemProperties.isScrap && !array[j].deactivated && !array[j].itemUsedUp && (array[j].isInShipRoom || !onlyScrapCollected))
			{
				num += array[j].scrapValue;
			}
		}
		return num;
	}
}
