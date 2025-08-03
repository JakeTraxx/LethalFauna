using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dissonance;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace GameNetcodeStuff
{
	public class PlayerControllerB : NetworkBehaviour, IHittable, IShockableWithGun, IVisibleThreat
	{
		public bool isTestingPlayer;

		[Header("MODELS / ANIMATIONS")]
		public Transform[] bodyParts;

		public Transform thisPlayerBody;

		public SkinnedMeshRenderer thisPlayerModel;

		public SkinnedMeshRenderer thisPlayerModelLOD1;

		public SkinnedMeshRenderer thisPlayerModelLOD2;

		public SkinnedMeshRenderer thisPlayerModelArms;

		public Transform playerGlobalHead;

		public Transform playerModelArmsMetarig;

		public Transform localArmsRotationTarget;

		public Transform meshContainer;

		public Transform lowerSpine;

		public Transform upperSpine;

		public Transform upperSpineLocalPoint;

		public Transform lowerTorsoCostumeContainer;

		public Transform lowerTorsoCostumeContainerBeltBagOffset;

		public Transform headCostumeContainer;

		public Transform headCostumeContainerLocal;

		public Camera gameplayCamera;

		public Transform cameraContainerTransform;

		public Transform playerEye;

		public float targetFOV = 66f;

		public Camera visorCamera;

		public CharacterController thisController;

		public Animator playerBodyAnimator;

		public MeshFilter playerBadgeMesh;

		public MeshRenderer playerBetaBadgeMesh;

		public int playerLevelNumber;

		public Transform localVisor;

		public Transform localVisorTargetPoint;

		private bool isSidling;

		private bool wasMovingForward;

		public MultiRotationConstraint cameraLookRig1;

		public MultiRotationConstraint cameraLookRig2;

		public Transform playerHudUIContainer;

		public Transform playerHudBaseRotation;

		public ChainIKConstraint rightArmNormalRig;

		public ChainIKConstraint rightArmProceduralRig;

		public ChainIKConstraint rightArmRig;

		public ChainIKConstraint leftArmRig;

		public TwoBoneIKConstraint leftArmRigSecondary;

		public TwoBoneIKConstraint rightArmRigSecondary;

		public ChainIKConstraint leftArmRigSecondaryLocal;

		public ChainIKConstraint rightArmRigSecondaryLocal;

		public Transform rightArmProceduralTarget;

		private Vector3 rightArmProceduralTargetBasePosition;

		public Transform leftHandItemTarget;

		public Light nightVision;

		public Light nightVisionRadar;

		public int currentSuitID;

		public bool performingEmote;

		public float emoteLayerWeight;

		public float timeSinceStartingEmote;

		public ParticleSystem beamUpParticle;

		public ParticleSystem beamOutParticle;

		public ParticleSystem beamOutBuildupParticle;

		public bool localArmsMatchCamera;

		public Transform localArmsTransform;

		public Collider playerCollider;

		public Collider[] bodyPartSpraypaintColliders;

		[Header("AUDIOS")]
		public AudioSource movementAudio;

		public AudioSource itemAudio;

		public AudioSource statusEffectAudio;

		public AudioSource waterBubblesAudio;

		public int currentFootstepSurfaceIndex;

		private int previousFootstepClip;

		[HideInInspector]
		public Dictionary<AudioSource, AudioReverbTrigger> audioCoroutines = new Dictionary<AudioSource, AudioReverbTrigger>();

		[HideInInspector]
		public Dictionary<AudioSource, IEnumerator> audioCoroutines2 = new Dictionary<AudioSource, IEnumerator>();

		[HideInInspector]
		public AudioReverbTrigger currentAudioTrigger;

		public AudioReverbTrigger currentAudioTriggerB;

		public float targetDryLevel;

		public float targetRoom;

		public float targetHighFreq;

		public float targetLowFreq;

		public float targetDecayTime;

		public ReverbPreset reverbPreset;

		public AudioListener activeAudioListener;

		public AudioReverbFilter activeAudioReverbFilter;

		public ParticleSystem bloodParticle;

		public bool playingQuickSpecialAnimation;

		private Coroutine quickSpecialAnimationCoroutine;

		[Header("INPUT / MOVEMENT")]
		public float movementSpeed = 0.5f;

		public PlayerActions playerActions;

		private bool isWalking;

		private bool movingForward;

		public Vector2 moveInputVector;

		public Vector3 velocityLastFrame;

		private float sprintMultiplier = 1f;

		public bool isSprinting;

		public float sprintTime = 5f;

		public Image sprintMeterUI;

		[HideInInspector]
		public float sprintMeter;

		[HideInInspector]
		public bool isExhausted;

		private float exhaustionEffectLerp;

		public float jumpForce = 5f;

		private bool isJumping;

		private bool isFallingFromJump;

		private Coroutine jumpCoroutine;

		public float fallValue;

		public bool isGroundedOnServer;

		public bool isPlayerSliding;

		private float playerSlidingTimer;

		public Vector3 playerGroundNormal;

		public float maxSlideFriction;

		private float slideFriction;

		public float fallValueUncapped;

		public bool takingFallDamage;

		public float minVelocityToTakeDamage;

		public bool isCrouching;

		private float crouchMeter;

		private bool isFallingNoJump;

		public int isMovementHindered;

		private int movementHinderedPrev;

		public float hinderedMultiplier = 1f;

		public int sourcesCausingSinking;

		public bool isSinking;

		public bool isUnderwater;

		private float syncUnderwaterInterval;

		private bool isFaceUnderwaterOnServer;

		public Collider underwaterCollider;

		private bool wasUnderwaterLastFrame;

		public float sinkingValue;

		public float sinkingSpeedMultiplier;

		public int statusEffectAudioIndex;

		private float cameraUp;

		public float lookSensitivity = 0.4f;

		public bool disableLookInput;

		public bool disableMoveInput;

		private float oldLookRot;

		private float targetLookRot;

		private float previousYRot;

		private float targetYRot;

		public Vector3 syncFullRotation;

		public Vector3 syncFullCameraRotation;

		private Vector3 walkForce;

		public Vector3 externalForces;

		public Vector3 externalForceAutoFade;

		private Vector3 movementForcesLastFrame;

		public Rigidbody playerRigidbody;

		public float averageVelocity;

		public int velocityMovingAverageLength = 20;

		public int velocityAverageCount;

		public float getAverageVelocityInterval;

		public bool jetpackControls;

		public bool disablingJetpackControls;

		private bool disabledJetpackControlsThisFrame;

		public Transform jetpackTurnCompass;

		private bool startedJetpackControls;

		public float maxJetpackAngle = -1f;

		public float jetpackRandomIntensity;

		public float jetpackCounteractiveForce = -16f;

		private float previousFrameDeltaTime;

		private Collider[] nearByPlayers = new Collider[4];

		private bool teleportingThisFrame;

		public bool teleportedLastFrame;

		[Header("LOCATION")]
		public bool isInElevator;

		public bool isInHangarShipRoom;

		public bool isInsideFactory;

		[Space(5f)]
		public bool parentedToElevatorLastFrame;

		public Vector3 previousElevatorPosition;

		[Header("CONTROL / NETWORKING")]
		public ulong playerClientId;

		public string playerUsername = "Player";

		public ulong playerSteamId;

		public ulong actualClientId;

		public bool isPlayerControlled;

		public bool justConnected = true;

		public bool disconnectedMidGame;

		[Space(5f)]
		private bool isCameraDisabled;

		public StartOfRound playersManager;

		public bool isHostPlayerObject;

		public Vector3 oldPlayerPosition;

		private int previousAnimationState;

		public Vector3 serverPlayerPosition;

		public Transform lastSyncedPhysicsParent;

		public Transform physicsParent;

		public Transform overridePhysicsParent;

		public bool snapToServerPosition;

		private float oldCameraUp;

		public float ladderCameraHorizontal;

		private float updatePlayerAnimationsInterval;

		private float updatePlayerLookInterval;

		private List<int> currentAnimationStateHash = new List<int>();

		private List<int> previousAnimationStateHash = new List<int>();

		private float currentAnimationSpeed;

		private float previousAnimationSpeed;

		private int previousAnimationServer;

		private int oldConnectedPlayersAmount;

		private int playerMask = 8;

		public RawImage playerScreen;

		public VoicePlayerState voicePlayerState;

		public AudioSource currentVoiceChatAudioSource;

		public PlayerVoiceIngameSettings currentVoiceChatIngameSettings;

		private float voiceChatUpdateInterval;

		public bool isTypingChat;

		public bool sentPlayerValues;

		[Header("DEATH")]
		private Vector3 positionOfDeath;

		public int health;

		public float healthRegenerateTimer;

		public bool criticallyInjured;

		public bool hasBeenCriticallyInjured;

		private float limpMultiplier = 0.2f;

		public float timeSinceTakingDamage;

		public float timeSinceFearLevelUp;

		public CauseOfDeath causeOfDeath;

		public bool isPlayerDead;

		[HideInInspector]
		public bool setPositionOfDeadPlayer;

		[HideInInspector]
		public Vector3 placeOfDeath;

		public Transform spectateCameraPivot;

		public Transform overrideGameOverSpectatePivot;

		public PlayerControllerB spectatedPlayerScript;

		public DeadBodyInfo deadBody;

		public GameObject[] bodyBloodDecals;

		private int currentBloodIndex;

		public List<GameObject> playerBloodPooledObjects = new List<GameObject>();

		public bool bleedingHeavily;

		private float bloodDropTimer;

		private bool alternatePlaceFootprints;

		public EnemyAI inAnimationWithEnemy;

		[Header("UI/MENU")]
		public bool inTerminalMenu;

		public QuickMenuManager quickMenuManager;

		public TextMeshProUGUI usernameBillboardText;

		public Transform usernameBillboard;

		public CanvasGroup usernameAlpha;

		public Canvas usernameCanvas;

		[Header("ITEM INTERACTION")]
		public float grabDistance = 5f;

		public float throwPower = 17f;

		public bool isHoldingObject;

		private bool hasThrownObject;

		public bool twoHanded;

		public bool twoHandedAnimation;

		public float carryWeight = 1f;

		public bool isGrabbingObjectAnimation;

		public bool activatingItem;

		private bool waitingToDropItem;

		public float grabObjectAnimationTime;

		public Transform localItemHolder;

		public Transform serverItemHolder;

		public Transform serverItemHolderNonTorsoRelative;

		public Transform propThrowPosition;

		public GrabbableObject currentlyHeldObject;

		private GrabbableObject currentlyGrabbingObject;

		public GrabbableObject currentlyHeldObjectServer;

		public GameObject heldObjectServerCopy;

		private Coroutine grabObjectCoroutine;

		private Ray interactRay;

		private int grabbableObjectsMask = 64;

		private int interactableObjectsMask = 1073742656;

		private int walkableSurfacesNoPlayersMask = 1342179585;

		private RaycastHit hit;

		private float upperBodyAnimationsWeight;

		public float doingUpperBodyEmote;

		private float handsOnWallWeight;

		public Light helmetLight;

		public Light[] allHelmetLights;

		private bool grabbedObjectValidated;

		private bool grabInvalidated;

		private bool throwingObject;

		[Space(5f)]
		public GrabbableObject[] ItemSlots;

		public int currentItemSlot;

		private MeshRenderer[] itemRenderers;

		private float timeSinceSwitchingSlots;

		[HideInInspector]
		public bool grabSetParentServer;

		[Header("TRIGGERS AND SPECIAL")]
		public Image cursorIcon;

		public TextMeshProUGUI cursorTip;

		public Sprite grabItemIcon;

		private bool hoveringOverItem;

		public InteractTrigger hoveringOverTrigger;

		public InteractTrigger previousHoveringOverTrigger;

		public InteractTrigger currentTriggerInAnimationWith;

		public bool isHoldingInteract;

		public bool inSpecialInteractAnimation;

		public bool disableInteract;

		public bool inVehicleAnimation;

		public bool freeRotationInInteractAnimation;

		public bool disableSyncInAnimation;

		public float specialAnimationWeight;

		public bool isClimbingLadder;

		public bool clampLooking;

		public float minVerticalClamp;

		public float maxVerticalClamp;

		public float horizontalClamp;

		public bool enteringSpecialAnimation;

		public float climbSpeed = 4f;

		public Vector3 clampCameraRotation;

		public Transform lineOfSightCube;

		public bool voiceMuffledByEnemy;

		[Header("SPECIAL ITEMS")]
		public bool inSpecialMenu;

		public int shipTeleporterId;

		public EnemyAI redirectToEnemy;

		public MeshRenderer mapRadarDirectionIndicator;

		public Animator mapRadarDotAnimator;

		public int enemiesOnPerson;

		public bool equippedUsableItemQE;

		public bool IsInspectingItem;

		public bool isFirstFrameLateUpdate = true;

		public GrabbableObject pocketedFlashlight;

		public bool isFreeCamera;

		public bool isSpeedCheating;

		public bool inShockingMinigame;

		public Transform shockingTarget;

		public Transform turnCompass;

		public Transform smoothLookTurnCompass;

		public float smoothLookMultiplier = 25f;

		private bool smoothLookEnabledLastFrame;

		public Camera turnCompassCamera;

		[HideInInspector]
		public Vector3 targetScreenPos;

		[HideInInspector]
		public float shockMinigamePullPosition;

		[Space(5f)]
		public bool speakingToWalkieTalkie;

		public bool holdingWalkieTalkie;

		public float isInGameOverAnimation;

		[HideInInspector]
		public bool hasBegunSpectating;

		private Coroutine timeSpecialAnimationCoroutine;

		private float spectatedPlayerDeadTimer;

		public float insanityLevel;

		public float maxInsanityLevel = 50f;

		public float insanitySpeedMultiplier = 1f;

		public bool isPlayerAlone;

		public float timeSincePlayerMoving;

		public Scrollbar terminalScrollVertical;

		private bool updatePositionForNewlyJoinedClient;

		private float timeSinceTakingGravityDamage;

		[Space(5f)]
		public float drunkness;

		public float drunknessInertia = 1f;

		public float drunknessSpeed;

		public bool increasingDrunknessThisFrame;

		public float timeSinceMakingLoudNoise;

		public float slopeIntensity = 2f;

		private float slopeModifier;

		public float slopeModifierSpeed = 1f;

		private Coroutine waitForPhysicsParentCoroutine;

		private Vector3 controllerCollisionPoint;

		ThreatType IVisibleThreat.type => ThreatType.Player;

		bool IVisibleThreat.IsThreatDead()
		{
			if (!isPlayerDead)
			{
				return !isPlayerControlled;
			}
			return true;
		}

		GrabbableObject IVisibleThreat.GetHeldObject()
		{
			return currentlyHeldObjectServer;
		}

		int IVisibleThreat.SendSpecialBehaviour(int id)
		{
			return 0;
		}

		float IVisibleThreat.GetVisibility()
		{
			if (isPlayerDead)
			{
				return 0f;
			}
			float num = 1f;
			if (isCrouching)
			{
				num -= 0.25f;
			}
			if (timeSincePlayerMoving > 0.5f)
			{
				num -= 0.16f;
			}
			return num;
		}

		int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
		{
			int num = 0;
			if (isHoldingObject && currentlyHeldObjectServer != null && currentlyHeldObjectServer.itemProperties.isDefensiveWeapon)
			{
				num += 2;
			}
			if (timeSinceMakingLoudNoise < 0.8f)
			{
				num++;
			}
			float num2 = LineOfSightToPositionAngle(seenByPosition);
			if (num2 == -361f || num2 > 100f)
			{
				num--;
			}
			else if (num2 < 45f)
			{
				num++;
			}
			if (TimeOfDay.Instance.normalizedTimeOfDay < 0.2f)
			{
				num++;
			}
			else if (TimeOfDay.Instance.normalizedTimeOfDay > 0.8f)
			{
				num--;
			}
			if (isInHangarShipRoom)
			{
				num++;
			}
			else if (Vector3.Distance(base.transform.position, StartOfRound.Instance.elevatorTransform.position) > 30f)
			{
				num--;
			}
			int num3 = Physics.OverlapSphereNonAlloc(base.transform.position, 12f, nearByPlayers, StartOfRound.Instance.playersMask);
			for (int i = 0; i < num3; i++)
			{
				if (!(Vector3.Distance(base.transform.position, nearByPlayers[i].transform.position) > 6f) || !Physics.Linecast(gameplayCamera.transform.position, nearByPlayers[i].transform.position + Vector3.up * 0.6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					num++;
				}
			}
			if (health >= 30)
			{
				num++;
			}
			else if (criticallyInjured)
			{
				num -= 2;
			}
			if (StartOfRound.Instance.connectedPlayersAmount <= 0)
			{
				num++;
			}
			return num;
		}

		Vector3 IVisibleThreat.GetThreatVelocity()
		{
			if (base.IsOwner)
			{
				return Vector3.Normalize(thisController.velocity * 100f);
			}
			if (timeSincePlayerMoving < 0.25f)
			{
				return Vector3.Normalize((serverPlayerPosition - oldPlayerPosition) * 100f);
			}
			return Vector3.zero;
		}

		int IVisibleThreat.GetInterestLevel()
		{
			int num = 0;
			if (currentlyHeldObjectServer != null && currentlyHeldObjectServer.itemProperties.isScrap)
			{
				num++;
			}
			if (carryWeight > 1.22f)
			{
				num++;
			}
			if (carryWeight > 1.5f)
			{
				num++;
			}
			return num;
		}

		Transform IVisibleThreat.GetThreatLookTransform()
		{
			return gameplayCamera.transform;
		}

		Transform IVisibleThreat.GetThreatTransform()
		{
			return base.transform;
		}

		private void Awake()
		{
			isHostPlayerObject = base.gameObject == playersManager.allPlayerObjects[0];
			playerActions = new PlayerActions();
			previousAnimationState = 0;
			serverPlayerPosition = base.transform.position;
			gameplayCamera.enabled = false;
			visorCamera.enabled = false;
			thisPlayerModel.enabled = true;
			thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
			thisPlayerModelArms.enabled = false;
			gameplayCamera.enabled = false;
			previousAnimationStateHash = new List<int>(new int[playerBodyAnimator.layerCount]);
			currentAnimationStateHash = new List<int>(new int[playerBodyAnimator.layerCount]);
			if (playerBodyAnimator.runtimeAnimatorController != playersManager.otherClientsAnimatorController)
			{
				playerBodyAnimator.runtimeAnimatorController = playersManager.otherClientsAnimatorController;
			}
			isCameraDisabled = true;
			sprintMeter = 1f;
			ItemSlots = new GrabbableObject[4];
			rightArmProceduralTargetBasePosition = rightArmProceduralTarget.localPosition;
			playerUsername = $"Player #{playerClientId}";
			previousElevatorPosition = playersManager.elevatorTransform.position;
			if ((bool)base.gameObject.GetComponent<Rigidbody>())
			{
				base.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
			}
			base.gameObject.GetComponent<CharacterController>().enabled = false;
			syncFullRotation = base.transform.eulerAngles;
		}

		private void Start()
		{
			InstantiateBloodPooledObjects();
			StartCoroutine(PlayIntroTip());
			jetpackTurnCompass.SetParent(null);
			terminalScrollVertical.value += 500f;
		}

		private IEnumerator PlayIntroTip()
		{
			yield return new WaitForSeconds(4f);
			QuickMenuManager quickMenu = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
			yield return new WaitUntil(() => !quickMenu.isMenuOpen);
			HUDManager.Instance.DisplayTip("Welcome!", "Right-click to scan objects in the ship for info.", isWarning: false, useSave: true, "LC_IntroTip1");
		}

		private void InstantiateBloodPooledObjects()
		{
			int num = 50;
			for (int i = 0; i < num; i++)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(playersManager.playerBloodPrefab, playersManager.bloodObjectsContainer);
				gameObject.SetActive(value: false);
				playerBloodPooledObjects.Add(gameObject);
			}
		}

		public void ResetPlayerBloodObjects(bool resetBodyBlood = true)
		{
			if (playerBloodPooledObjects != null)
			{
				for (int i = 0; i < playerBloodPooledObjects.Count; i++)
				{
					playerBloodPooledObjects[i].SetActive(value: false);
				}
			}
			if (resetBodyBlood)
			{
				for (int j = 0; j < bodyBloodDecals.Length; j++)
				{
					bodyBloodDecals[j].SetActive(value: false);
				}
			}
		}

		private void OnEnable()
		{
			InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
			try
			{
				playerActions.Movement.Look.performed += Look_performed;
				actions.FindAction("Jump").performed += Jump_performed;
				actions.FindAction("Crouch").performed += Crouch_performed;
				actions.FindAction("Interact").performed += Interact_performed;
				actions.FindAction("ItemSecondaryUse").performed += ItemSecondaryUse_performed;
				actions.FindAction("ItemTertiaryUse").performed += ItemTertiaryUse_performed;
				actions.FindAction("ActivateItem").performed += ActivateItem_performed;
				actions.FindAction("ActivateItem").canceled += ActivateItem_canceled;
				actions.FindAction("Discard").performed += Discard_performed;
				actions.FindAction("SwitchItem").performed += ScrollMouse_performed;
				actions.FindAction("OpenMenu").performed += OpenMenu_performed;
				actions.FindAction("InspectItem").performed += InspectItem_performed;
				actions.FindAction("SpeedCheat").performed += SpeedCheat_performed;
				actions.FindAction("Emote1").performed += Emote1_performed;
				actions.FindAction("Emote2").performed += Emote2_performed;
				playerActions.Movement.Enable();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error while subscribing to input in PlayerController!: {arg}");
			}
			playerActions.Movement.Enable();
		}

		private void OnDisable()
		{
			InputActionAsset actions = IngamePlayerSettings.Instance.playerInput.actions;
			try
			{
				playerActions.Movement.Look.performed -= Look_performed;
				actions.FindAction("Jump").performed -= Jump_performed;
				actions.FindAction("Crouch").performed -= Crouch_performed;
				actions.FindAction("Interact").performed -= Interact_performed;
				actions.FindAction("ItemSecondaryUse").performed -= ItemSecondaryUse_performed;
				actions.FindAction("ItemTertiaryUse").performed -= ItemTertiaryUse_performed;
				actions.FindAction("ActivateItem").performed -= ActivateItem_performed;
				actions.FindAction("ActivateItem").canceled -= ActivateItem_canceled;
				actions.FindAction("Discard").performed -= Discard_performed;
				actions.FindAction("SwitchItem").performed -= ScrollMouse_performed;
				actions.FindAction("OpenMenu").performed -= OpenMenu_performed;
				actions.FindAction("InspectItem").performed -= InspectItem_performed;
				actions.FindAction("SpeedCheat").performed -= SpeedCheat_performed;
				actions.FindAction("Emote1").performed -= Emote1_performed;
				actions.FindAction("Emote2").performed -= Emote2_performed;
				playerActions.Movement.Enable();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error while unsubscribing from input in PlayerController!: {arg}");
			}
			playerActions.Movement.Disable();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
		}

		private void SpeedCheat_performed(InputAction.CallbackContext context)
		{
			if (((base.IsOwner && (isPlayerControlled || isPlayerDead) && !inTerminalMenu && !isTypingChat && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && context.performed && !(HUDManager.Instance == null) && IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>() > 0.5f)
			{
				HUDManager.Instance.ToggleErrorConsole();
			}
		}

		public bool AllowPlayerDeath()
		{
			if (!StartOfRound.Instance.allowLocalPlayerDeath)
			{
				return false;
			}
			if (playersManager.testRoom == null)
			{
				if (StartOfRound.Instance.timeSinceRoundStarted < 2f)
				{
					return false;
				}
				if (!playersManager.shipDoorsEnabled)
				{
					return false;
				}
			}
			return true;
		}

		public void DamagePlayer(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default(Vector3))
		{
			if (!base.IsOwner || isPlayerDead || !AllowPlayerDeath())
			{
				return;
			}
			if (health - damageNumber <= 0 && !criticallyInjured && damageNumber < 50)
			{
				health = 5;
			}
			else
			{
				health = Mathf.Clamp(health - damageNumber, 0, 100);
			}
			HUDManager.Instance.UpdateHealthUI(health);
			if (health <= 0)
			{
				bool spawnBody = deathAnimation != -1;
				KillPlayer(force, spawnBody, causeOfDeath, deathAnimation);
			}
			else
			{
				if (health < 10 && !criticallyInjured)
				{
					HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
					MakeCriticallyInjured(enable: true);
				}
				else
				{
					if (damageNumber >= 10)
					{
						sprintMeter = Mathf.Clamp(sprintMeter + (float)damageNumber / 125f, 0f, 1f);
					}
					if (callRPC)
					{
						if (base.IsServer)
						{
							DamagePlayerClientRpc(damageNumber, health);
						}
						else
						{
							DamagePlayerServerRpc(damageNumber, health);
						}
					}
				}
				if (fallDamage)
				{
					HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
					WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.fallDamageSFX);
					BreakLegsSFXClientRpc();
				}
				else if (hasDamageSFX)
				{
					HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.damageSFX, 1f);
				}
			}
			StartOfRound.Instance.LocalPlayerDamagedEvent.Invoke();
			takingFallDamage = false;
			if (!inSpecialInteractAnimation && !twoHandedAnimation)
			{
				playerBodyAnimator.SetTrigger("Damage");
			}
			specialAnimationWeight = 1f;
			PlayQuickSpecialAnimation(0.7f);
		}

		[ServerRpc]
		public void BreakLegsSFXServerRpc()
{			{
				BreakLegsSFXClientRpc();
			}
}
		[ClientRpc]
		public void BreakLegsSFXClientRpc()
{if(!base.IsOwner)				{
					movementAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
					WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.fallDamageSFX);
				}
}
		public void MakeCriticallyInjured(bool enable)
		{
			if (enable)
			{
				criticallyInjured = true;
				playerBodyAnimator.SetBool("Limp", value: true);
				bleedingHeavily = true;
				if (base.IsServer)
				{
					MakeCriticallyInjuredClientRpc();
				}
				else
				{
					MakeCriticallyInjuredServerRpc();
				}
			}
			else
			{
				criticallyInjured = false;
				playerBodyAnimator.SetBool("Limp", value: false);
				bleedingHeavily = false;
				if (base.IsServer)
				{
					HealClientRpc();
				}
				else
				{
					HealServerRpc();
				}
			}
		}

		[ServerRpc]
		public void DamagePlayerServerRpc(int damageNumber, int newHealthAmount)
{			{
				DamagePlayerClientRpc(damageNumber, newHealthAmount);
			}
}
		[ClientRpc]
		public void DamagePlayerClientRpc(int damageNumber, int newHealthAmount)
				{
					DamageOnOtherClients(damageNumber, newHealthAmount);
				}

		private void DamageOnOtherClients(int damageNumber, int newHealthAmount)
		{
			playersManager.gameStats.allPlayerStats[playerClientId].damageTaken += damageNumber;
			health = newHealthAmount;
			timeSinceTakingDamage = Time.realtimeSinceStartup;
			if (!base.IsOwner)
			{
				PlayQuickSpecialAnimation(0.7f);
			}
		}

		public void PlayQuickSpecialAnimation(float animTime)
		{
			if (quickSpecialAnimationCoroutine != null)
			{
				StopCoroutine(quickSpecialAnimationCoroutine);
			}
			quickSpecialAnimationCoroutine = StartCoroutine(playQuickSpecialAnimation(animTime));
		}

		private IEnumerator playQuickSpecialAnimation(float animTime)
		{
			playingQuickSpecialAnimation = true;
			yield return new WaitForSeconds(animTime);
			playingQuickSpecialAnimation = false;
		}

		[ServerRpc]
		public void StartSinkingServerRpc(float sinkingSpeed, int audioClipIndex)
{			{
				StartSinkingClientRpc(sinkingSpeed, audioClipIndex);
			}
}
		[ClientRpc]
		public void StartSinkingClientRpc(float sinkingSpeed, int audioClipIndex)
				{
					sinkingSpeedMultiplier = sinkingSpeed;
					isSinking = true;
					statusEffectAudio.clip = StartOfRound.Instance.statusEffectClips[audioClipIndex];
					statusEffectAudio.Play();
				}

		[ServerRpc]
		public void StopSinkingServerRpc()
{			{
				StopSinkingClientRpc();
			}
}
		[ClientRpc]
		public void StopSinkingClientRpc()
{			statusEffectAudio.Stop();
			isSinking = false;
			voiceMuffledByEnemy = false;
			if (!base.IsOwner)
			{
				if (currentVoiceChatIngameSettings == null)
				{
					StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
				}
				if (currentVoiceChatIngameSettings != null)
				{
					currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
				}
			}
}
		[ServerRpc]
		public void MakeCriticallyInjuredServerRpc()
{			{
				MakeCriticallyInjuredClientRpc();
			}
}
		[ClientRpc]
		public void MakeCriticallyInjuredClientRpc()
				{
					bleedingHeavily = true;
					criticallyInjured = true;
					hasBeenCriticallyInjured = true;
				}

		[ServerRpc]
		public void HealServerRpc()
{			{
				HealClientRpc();
			}
}
		[ClientRpc]
		public void HealClientRpc()
{if(!base.IsOwner)				{
					bleedingHeavily = false;
					criticallyInjured = false;
				}
}
		public void DropBlood(Vector3 direction = default(Vector3), bool leaveBlood = true, bool leaveFootprint = false)
		{
			bool flag = false;
			if (leaveBlood)
			{
				if (bloodDropTimer >= 0f && !isPlayerDead)
				{
					return;
				}
				bloodDropTimer = 0.4f;
				if (direction == Vector3.zero)
				{
					direction = Vector3.down;
				}
				Transform transform = playerBloodPooledObjects[currentBloodIndex].transform;
				transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
				if (isInElevator)
				{
					transform.SetParent(playersManager.elevatorTransform);
				}
				else
				{
					transform.SetParent(playersManager.bloodObjectsContainer);
				}
				if (isPlayerDead)
				{
					if (deadBody == null || !deadBody.gameObject.activeSelf)
					{
						return;
					}
					interactRay = new Ray(deadBody.bodyParts[3].transform.position + Vector3.up * 0.5f, direction);
				}
				else
				{
					interactRay = new Ray(base.transform.position + base.transform.up * 2f, direction);
				}
				if (Physics.Raycast(interactRay, out hit, 6f, playersManager.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					flag = true;
					transform.position = hit.point - direction.normalized * 0.45f;
					RandomizeBloodRotationAndScale(transform);
					transform.gameObject.SetActive(value: true);
				}
				currentBloodIndex = (currentBloodIndex + 1) % playerBloodPooledObjects.Count;
			}
			if (!leaveFootprint || isPlayerDead || playersManager.snowFootprintsPooledObjects == null || playersManager.snowFootprintsPooledObjects.Count <= 0)
			{
				return;
			}
			alternatePlaceFootprints = !alternatePlaceFootprints;
			if (alternatePlaceFootprints)
			{
				return;
			}
			Transform transform2 = playersManager.snowFootprintsPooledObjects[playersManager.currentFootprintIndex].transform;
			transform2.rotation = Quaternion.LookRotation(direction, Vector3.up);
			if (!flag)
			{
				interactRay = new Ray(base.transform.position + base.transform.up * 0.3f, direction);
				if (Physics.Raycast(interactRay, out hit, 6f, playersManager.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					transform2.position = hit.point - direction.normalized * 0.45f;
				}
			}
			else
			{
				transform2.position = hit.point - direction.normalized * 0.45f;
			}
			transform2.transform.eulerAngles = new Vector3(transform2.transform.eulerAngles.x, base.transform.eulerAngles.y, transform2.transform.eulerAngles.z);
			playersManager.snowFootprintsPooledObjects[playersManager.currentFootprintIndex].enabled = true;
			playersManager.currentFootprintIndex = (playersManager.currentFootprintIndex + 1) % playersManager.snowFootprintsPooledObjects.Count;
		}

		private void RandomizeBloodRotationAndScale(Transform blood)
		{
			Vector3 localEulerAngles = blood.localEulerAngles;
			localEulerAngles.z = UnityEngine.Random.Range(-180f, 180f);
			blood.localEulerAngles = localEulerAngles;
			blood.localScale = new Vector3(UnityEngine.Random.Range(0.15f, 0.7f), UnityEngine.Random.Range(0.15f, 0.7f), 0.55f);
		}

		private void Emote1_performed(InputAction.CallbackContext context)
		{
			PerformEmote(context, 1);
		}

		private void Emote2_performed(InputAction.CallbackContext context)
		{
			PerformEmote(context, 2);
		}

		public void PerformEmote(InputAction.CallbackContext context, int emoteID)
		{
			if (context.performed && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && CheckConditionsForEmote() && !(timeSinceStartingEmote < 0.5f))
			{
				timeSinceStartingEmote = 0f;
				performingEmote = true;
				playerBodyAnimator.SetInteger("emoteNumber", emoteID);
				StartPerformingEmoteServerRpc();
			}
		}

		[ServerRpc]
		public void StartPerformingEmoteServerRpc()
{			{
				StartPerformingEmoteClientRpc();
			}
}
		[ClientRpc]
		public void StartPerformingEmoteClientRpc()
				{
					performingEmote = true;
				}

		[ServerRpc]
		public void StopPerformingEmoteServerRpc()
{			{
				StopPerformingEmoteClientRpc();
			}
}
		[ClientRpc]
		public void StopPerformingEmoteClientRpc()
				{
					performingEmote = false;
				}

		public bool CheckConditionsForSinkingInQuicksand()
		{
			if (!thisController.isGrounded)
			{
				return false;
			}
			if (inSpecialInteractAnimation || (bool)inAnimationWithEnemy || isClimbingLadder)
			{
				return false;
			}
			if (physicsParent != null)
			{
				return false;
			}
			if (isInHangarShipRoom)
			{
				return false;
			}
			if (isInElevator)
			{
				return false;
			}
			if (currentFootstepSurfaceIndex != 1 && currentFootstepSurfaceIndex != 4 && currentFootstepSurfaceIndex != 8 && currentFootstepSurfaceIndex != 7 && (!isInsideFactory || currentFootstepSurfaceIndex != 5))
			{
				return false;
			}
			return true;
		}

		private bool CheckConditionsForEmote()
		{
			if (inSpecialInteractAnimation)
			{
				return false;
			}
			if (isPlayerDead || isJumping || isWalking)
			{
				return false;
			}
			if (isCrouching || isClimbingLadder)
			{
				return false;
			}
			if (isGrabbingObjectAnimation || inTerminalMenu || isTypingChat)
			{
				return false;
			}
			return true;
		}

		private void ActivateItem_performed(InputAction.CallbackContext context)
		{
			if (!context.performed || inSpecialMenu)
			{
				return;
			}
			if (base.IsOwner && isPlayerDead && (!base.IsServer || isHostPlayerObject))
			{
				if (!StartOfRound.Instance.overrideSpectateCamera && spectatedPlayerScript != null && !spectatedPlayerScript.isPlayerDead)
				{
					SpectateNextPlayer();
				}
			}
			else if (CanUseItem() && !(timeSinceSwitchingSlots < 0.075f))
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				currentlyHeldObjectServer.gameObject.GetComponent<GrabbableObject>().UseItemOnClient();
				timeSinceSwitchingSlots = 0f;
			}
		}

		private void ActivateItem_canceled(InputAction.CallbackContext context)
		{
			if (CanUseItem() && currentlyHeldObjectServer.itemProperties.holdButtonUse)
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				currentlyHeldObjectServer.gameObject.GetComponent<GrabbableObject>().UseItemOnClient(buttonDown: false);
			}
		}

		private bool CanUseItem()
		{
			if ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer)
			{
				return false;
			}
			if (!isHoldingObject || currentlyHeldObjectServer == null)
			{
				return false;
			}
			if (quickMenuManager.isMenuOpen)
			{
				return false;
			}
			if (isPlayerDead)
			{
				return false;
			}
			if (!currentlyHeldObjectServer.itemProperties.usableInSpecialAnimations && (isGrabbingObjectAnimation || inTerminalMenu || isTypingChat || (inSpecialInteractAnimation && !inShockingMinigame)))
			{
				return false;
			}
			return true;
		}

		private int FirstEmptyItemSlot()
		{
			int result = -1;
			if (ItemSlots[currentItemSlot] == null)
			{
				result = currentItemSlot;
			}
			else
			{
				for (int i = 0; i < ItemSlots.Length; i++)
				{
					if (ItemSlots[i] == null)
					{
						result = i;
						break;
					}
				}
			}
			return result;
		}

		private int NextItemSlot(bool forward)
		{
			if (forward)
			{
				return (currentItemSlot + 1) % ItemSlots.Length;
			}
			if (currentItemSlot == 0)
			{
				return ItemSlots.Length - 1;
			}
			return currentItemSlot - 1;
		}

		private void SwitchToItemSlot(int slot, GrabbableObject fillSlotWithItem = null)
		{
			currentItemSlot = slot;
			if (base.IsOwner)
			{
				for (int i = 0; i < HUDManager.Instance.itemSlotIconFrames.Length; i++)
				{
					HUDManager.Instance.itemSlotIconFrames[i].GetComponent<Animator>().SetBool("selectedSlot", value: false);
				}
				HUDManager.Instance.itemSlotIconFrames[slot].GetComponent<Animator>().SetBool("selectedSlot", value: true);
			}
			if (fillSlotWithItem != null)
			{
				ItemSlots[slot] = fillSlotWithItem;
				if (base.IsOwner)
				{
					HUDManager.Instance.itemSlotIcons[slot].sprite = fillSlotWithItem.itemProperties.itemIcon;
					HUDManager.Instance.itemSlotIcons[currentItemSlot].enabled = true;
				}
			}
			if (currentlyHeldObjectServer != null)
			{
				currentlyHeldObjectServer.playerHeldBy = this;
				if (base.IsOwner)
				{
					SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
				}
				currentlyHeldObjectServer.PocketItem();
				if (ItemSlots[slot] != null && !string.IsNullOrEmpty(ItemSlots[slot].itemProperties.pocketAnim))
				{
					playerBodyAnimator.SetTrigger(ItemSlots[slot].itemProperties.pocketAnim);
				}
			}
			if (ItemSlots[slot] != null)
			{
				ItemSlots[slot].playerHeldBy = this;
				ItemSlots[slot].EquipItem();
				if (base.IsOwner)
				{
					SetSpecialGrabAnimationBool(setTrue: true, ItemSlots[slot]);
				}
				if (currentlyHeldObjectServer != null)
				{
					if (ItemSlots[slot].itemProperties.twoHandedAnimation || currentlyHeldObjectServer.itemProperties.twoHandedAnimation)
					{
						playerBodyAnimator.ResetTrigger("SwitchHoldAnimationTwoHanded");
						playerBodyAnimator.SetTrigger("SwitchHoldAnimationTwoHanded");
					}
					playerBodyAnimator.ResetTrigger("SwitchHoldAnimation");
					playerBodyAnimator.SetTrigger("SwitchHoldAnimation");
				}
				twoHandedAnimation = ItemSlots[slot].itemProperties.twoHandedAnimation;
				twoHanded = ItemSlots[slot].itemProperties.twoHanded;
				playerBodyAnimator.SetBool("GrabValidated", value: true);
				playerBodyAnimator.SetBool("cancelHolding", value: false);
				isHoldingObject = true;
				currentlyHeldObjectServer = ItemSlots[slot];
			}
			else
			{
				if (!base.IsOwner && heldObjectServerCopy != null)
				{
					heldObjectServerCopy.SetActive(value: false);
				}
				if (base.IsOwner)
				{
					HUDManager.Instance.ClearControlTips();
				}
				currentlyHeldObjectServer = null;
				currentlyHeldObject = null;
				isHoldingObject = false;
				twoHanded = false;
				playerBodyAnimator.SetBool("cancelHolding", value: true);
			}
			if (base.IsOwner)
			{
				if (twoHanded)
				{
					HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 0.1f, 0.13f, 0.13f);
					HUDManager.Instance.holdingTwoHandedItem.enabled = true;
				}
				else
				{
					HUDManager.Instance.PingHUDElement(HUDManager.Instance.Inventory, 1.5f, 1f, 0.13f);
					HUDManager.Instance.holdingTwoHandedItem.enabled = false;
				}
			}
		}

		private void ScrollMouse_performed(InputAction.CallbackContext context)
		{
			if (inTerminalMenu)
			{
				float num = context.ReadValue<float>();
				terminalScrollVertical.value += num / 3f;
			}
			else if (((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && !(timeSinceSwitchingSlots < 0.3f) && !isGrabbingObjectAnimation && !quickMenuManager.isMenuOpen && !inSpecialInteractAnimation && !throwingObject && !isTypingChat && !twoHanded && !activatingItem && !jetpackControls && !disablingJetpackControls)
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				playerBodyAnimator.SetBool("GrabValidated", value: false);
				if (context.ReadValue<float>() > 0f)
				{
					SwitchToItemSlot(NextItemSlot(forward: true));
					SwitchItemSlotsServerRpc(forward: true);
				}
				else
				{
					SwitchToItemSlot(NextItemSlot(forward: false));
					SwitchItemSlotsServerRpc(forward: false);
				}
				if (currentlyHeldObjectServer != null)
				{
					currentlyHeldObjectServer.gameObject.GetComponent<AudioSource>().PlayOneShot(currentlyHeldObjectServer.itemProperties.grabSFX, 0.6f);
				}
				timeSinceSwitchingSlots = 0f;
			}
		}

		[ServerRpc]
		private void SwitchItemSlotsServerRpc(bool forward)
{			{
				SwitchItemSlotsClientRpc(forward);
			}
}
		[ClientRpc]
		private void SwitchItemSlotsClientRpc(bool forward)
{if(!base.IsOwner)			{
				SwitchToItemSlot(NextItemSlot(forward));
				if (currentlyHeldObjectServer != null)
				{
					currentlyHeldObjectServer.gameObject.GetComponent<AudioSource>().PlayOneShot(currentlyHeldObjectServer.itemProperties.grabSFX, 0.6f);
				}
			}
}
		private bool InteractTriggerUseConditionsMet()
		{
			if (sinkingValue > 0.73f)
			{
				return false;
			}
			if (jetpackControls && (hoveringOverTrigger.specialCharacterAnimation || hoveringOverTrigger.isLadder))
			{
				return false;
			}
			if (isClimbingLadder)
			{
				if (hoveringOverTrigger.isLadder)
				{
					if (!hoveringOverTrigger.usingLadder)
					{
						return false;
					}
				}
				else if (hoveringOverTrigger.specialCharacterAnimation)
				{
					return false;
				}
			}
			else if (inSpecialInteractAnimation && !hoveringOverTrigger.allowUseWhileInAnimation)
			{
				return false;
			}
			if (disableInteract)
			{
				return false;
			}
			if (hoveringOverTrigger.isPlayingSpecialAnimation)
			{
				return false;
			}
			return true;
		}

		private void InspectItem_performed(InputAction.CallbackContext context)
		{
			if (!ShipBuildModeManager.Instance.InBuildMode && context.performed && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && !isGrabbingObjectAnimation && !isTypingChat && !inSpecialInteractAnimation && !throwingObject && !activatingItem)
			{
				ShipBuildModeManager.Instance.CancelBuildMode();
				if (currentlyHeldObjectServer != null)
				{
					currentlyHeldObjectServer.InspectItem();
				}
			}
		}

		private void QEItemInteract_performed(InputAction.CallbackContext context)
		{
			if (!equippedUsableItemQE || ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer) || !context.performed || isGrabbingObjectAnimation || isTypingChat || inTerminalMenu || inSpecialInteractAnimation || throwingObject || timeSinceSwitchingSlots < 0.2f)
			{
				return;
			}
			float num = context.ReadValue<float>();
			if (currentlyHeldObjectServer != null)
			{
				timeSinceSwitchingSlots = 0f;
				if (num < 0f)
				{
					currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: false);
				}
				else
				{
					currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: true);
				}
			}
		}

		private void ItemSecondaryUse_performed(InputAction.CallbackContext context)
		{
			Debug.Log("secondary use A");
			if (!equippedUsableItemQE)
			{
				return;
			}
			Debug.Log("secondary use B");
			if ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer)
			{
				return;
			}
			Debug.Log("secondary use C");
			if (!context.performed)
			{
				return;
			}
			Debug.Log("secondary use D");
			if (isGrabbingObjectAnimation || isTypingChat || inTerminalMenu || inSpecialInteractAnimation || throwingObject)
			{
				return;
			}
			Debug.Log("secondary use E");
			if (!(timeSinceSwitchingSlots < 0.2f))
			{
				Debug.Log("secondary use F");
				if (currentlyHeldObjectServer != null)
				{
					Debug.Log("secondary use G");
					timeSinceSwitchingSlots = 0f;
					currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: false);
				}
			}
		}

		private void ItemTertiaryUse_performed(InputAction.CallbackContext context)
		{
			if (equippedUsableItemQE && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && context.performed && !isGrabbingObjectAnimation && !isTypingChat && !inTerminalMenu && !inSpecialInteractAnimation && !throwingObject && !(timeSinceSwitchingSlots < 0.2f) && currentlyHeldObjectServer != null)
			{
				timeSinceSwitchingSlots = 0f;
				currentlyHeldObjectServer.ItemInteractLeftRightOnClient(right: true);
			}
		}

		private void Interact_performed(InputAction.CallbackContext context)
		{
			if (base.IsOwner && isPlayerDead && (!base.IsServer || isHostPlayerObject))
			{
				if (!StartOfRound.Instance.overrideSpectateCamera && spectatedPlayerScript != null && !spectatedPlayerScript.isPlayerDead)
				{
					SpectateNextPlayer();
				}
			}
			else
			{
				if (((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer) || !context.performed || timeSinceSwitchingSlots < 0.2f || inSpecialMenu)
				{
					return;
				}
				ShipBuildModeManager.Instance.CancelBuildMode();
				if (!isGrabbingObjectAnimation && !isTypingChat && !inTerminalMenu && !throwingObject && !IsInspectingItem && !(inAnimationWithEnemy != null) && !jetpackControls && !disablingJetpackControls && !StartOfRound.Instance.suckingPlayersOutOfShip)
				{
					if (!activatingItem && !waitingToDropItem)
					{
						BeginGrabObject();
					}
					if (!(hoveringOverTrigger == null) && !hoveringOverTrigger.holdInteraction && (!isHoldingObject || hoveringOverTrigger.oneHandedItemAllowed) && (!twoHanded || (hoveringOverTrigger.twoHandedItemAllowed && !hoveringOverTrigger.specialCharacterAnimation)) && InteractTriggerUseConditionsMet())
					{
						hoveringOverTrigger.Interact(thisPlayerBody);
					}
				}
			}
		}

		private void BeginGrabObject()
		{
			interactRay = new Ray(gameplayCamera.transform.position, gameplayCamera.transform.forward);
			if (!Physics.Raycast(interactRay, out hit, grabDistance, interactableObjectsMask) || hit.collider.gameObject.layer == 8 || !(hit.collider.tag == "PhysicsProp") || twoHanded || sinkingValue > 0.73f || Physics.Linecast(gameplayCamera.transform.position, hit.collider.transform.position + base.transform.up * 0.16f, 1073741824, QueryTriggerInteraction.Ignore))
			{
				return;
			}
			currentlyGrabbingObject = hit.collider.transform.gameObject.GetComponent<GrabbableObject>();
			if (!GameNetworkManager.Instance.gameHasStarted && !currentlyGrabbingObject.itemProperties.canBeGrabbedBeforeGameStart && StartOfRound.Instance.testRoom == null)
			{
				return;
			}
			grabInvalidated = false;
			if (currentlyGrabbingObject == null || inSpecialInteractAnimation || currentlyGrabbingObject.isHeld || currentlyGrabbingObject.isPocketed)
			{
				return;
			}
			NetworkObject networkObject = currentlyGrabbingObject.NetworkObject;
			if (networkObject == null || !networkObject.IsSpawned)
			{
				return;
			}
			currentlyGrabbingObject.InteractItem();
			if (currentlyGrabbingObject.grabbable && FirstEmptyItemSlot() != -1)
			{
				playerBodyAnimator.SetBool("GrabInvalidated", value: false);
				playerBodyAnimator.SetBool("GrabValidated", value: false);
				playerBodyAnimator.SetBool("cancelHolding", value: false);
				playerBodyAnimator.ResetTrigger("Throw");
				SetSpecialGrabAnimationBool(setTrue: true);
				isGrabbingObjectAnimation = true;
				cursorIcon.enabled = false;
				cursorTip.text = "";
				twoHanded = currentlyGrabbingObject.itemProperties.twoHanded;
				carryWeight = Mathf.Clamp(carryWeight + (currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
				if (currentlyGrabbingObject.itemProperties.grabAnimationTime > 0f)
				{
					grabObjectAnimationTime = currentlyGrabbingObject.itemProperties.grabAnimationTime;
				}
				else
				{
					grabObjectAnimationTime = 0.4f;
				}
				if (!isTestingPlayer)
				{
					GrabObjectServerRpc(networkObject);
				}
				if (grabObjectCoroutine != null)
				{
					StopCoroutine(grabObjectCoroutine);
				}
				grabObjectCoroutine = StartCoroutine(GrabObject());
			}
		}

		private IEnumerator GrabObject()
		{
			grabbedObjectValidated = false;
			yield return new WaitForSeconds(0.1f);
			currentlyGrabbingObject.parentObject = localItemHolder;
			if (currentlyGrabbingObject.itemProperties.grabSFX != null)
			{
				itemAudio.PlayOneShot(currentlyGrabbingObject.itemProperties.grabSFX, 1f);
			}
			while ((currentlyGrabbingObject != currentlyHeldObjectServer || !currentlyHeldObjectServer.wasOwnerLastFrame) && !grabInvalidated)
			{
				Debug.Log($"grabInvalidated: {grabInvalidated}");
				yield return null;
			}
			if (grabInvalidated)
			{
				grabInvalidated = false;
				Debug.Log("Grab was invalidated on object: " + currentlyGrabbingObject.name);
				if (currentlyGrabbingObject.playerHeldBy != null)
				{
					Debug.Log($"playerHeldBy on currentlyGrabbingObject 2: {currentlyGrabbingObject.playerHeldBy}");
				}
				if (currentlyGrabbingObject.parentObject == localItemHolder)
				{
					if (currentlyGrabbingObject.playerHeldBy != null)
					{
						Debug.Log($"Grab invalidated; giving grabbed object to the client who got it first; {currentlyGrabbingObject.playerHeldBy}");
						currentlyGrabbingObject.parentObject = currentlyGrabbingObject.playerHeldBy.serverItemHolder;
					}
					else
					{
						Debug.Log("Grab invalidated; no other client has possession of it, so set its parent object to null.");
						currentlyGrabbingObject.parentObject = null;
					}
				}
				twoHanded = false;
				SetSpecialGrabAnimationBool(setTrue: false);
				if (currentlyHeldObjectServer != null)
				{
					playerBodyAnimator.SetBool("Grab", value: true);
				}
				playerBodyAnimator.SetBool("GrabInvalidated", value: true);
				carryWeight = Mathf.Clamp(carryWeight - (currentlyGrabbingObject.itemProperties.weight - 1f), 1f, 10f);
				isGrabbingObjectAnimation = false;
				currentlyGrabbingObject = null;
			}
			else
			{
				grabbedObjectValidated = true;
				currentlyHeldObjectServer.GrabItemOnClient();
				isHoldingObject = true;
				yield return new WaitForSeconds(grabObjectAnimationTime - 0.2f);
				playerBodyAnimator.SetBool("GrabValidated", value: true);
				isGrabbingObjectAnimation = false;
			}
		}

		private void SetSpecialGrabAnimationBool(bool setTrue, GrabbableObject currentItem = null)
		{
			if (currentItem == null)
			{
				currentItem = currentlyGrabbingObject;
			}
			if (!base.IsOwner)
			{
				return;
			}
			playerBodyAnimator.SetBool("Grab", setTrue);
			if (string.IsNullOrEmpty(currentItem.itemProperties.grabAnim))
			{
				return;
			}
			try
			{
				playerBodyAnimator.SetBool(currentItem.itemProperties.grabAnim, setTrue);
			}
			catch (Exception)
			{
				Debug.LogError("An item tried to set an animator bool which does not exist: " + currentItem.itemProperties.grabAnim);
			}
		}

		[ServerRpc]
		private void GrabObjectServerRpc(NetworkObjectReference grabbedObject)
{			bool flag = true;
			if (grabbedObject.TryGet(out var networkObject) && (bool)networkObject.GetComponentInChildren<GrabbableObject>())
			{
				if (networkObject.GetComponentInChildren<GrabbableObject>().heldByPlayerOnServer)
				{
					flag = false;
					Debug.Log("Invalidated grab on " + base.gameObject.name + " on client; another player was already grabbing the same object");
				}
			}
			else
			{
				flag = false;
			}
			if (flag)
			{
				networkObject.GetComponentInChildren<GrabbableObject>().heldByPlayerOnServer = true;
				networkObject.ChangeOwnership(actualClientId);
				GrabObjectClientRpc(grabValidated: true, networkObject);
			}
			else
			{
				GrabObjectClientRpc(flag, grabbedObject);
			}
}
		[ClientRpc]
		private void GrabObjectClientRpc(bool grabValidated, NetworkObjectReference grabbedObject)
{			if (grabValidated)
			{
				if (!grabbedObject.TryGet(out var networkObject))
				{
					Debug.Log($"Error! Networkobject grabbed was not found on client: {networkObject.NetworkObjectId}");
					return;
				}
				SwitchToItemSlot(FirstEmptyItemSlot(), networkObject.gameObject.GetComponentInChildren<GrabbableObject>());
				currentlyHeldObjectServer.EnablePhysics(enable: false);
				currentlyHeldObjectServer.isHeld = true;
				currentlyHeldObjectServer.hasHitGround = false;
				currentlyHeldObjectServer.isInFactory = isInsideFactory;
				SetItemInElevator(isInHangarShipRoom, isInElevator, currentlyHeldObjectServer);
				if (currentlyHeldObjectServer.transform.parent != playersManager.propsContainer && currentlyHeldObjectServer.transform.parent != StartOfRound.Instance.elevatorTransform)
				{
					Debug.Log("Picked up scrap " + currentlyHeldObjectServer.itemProperties.itemName + " was not in the normal places; reparenting it now");
					if (currentlyHeldObjectServer.isInElevator)
					{
						Debug.Log("Picked up scrap " + currentlyHeldObjectServer.itemProperties.itemName + " is in the elevator; parenting it to elevator");
						currentlyHeldObjectServer.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
					}
					else
					{
						Debug.Log("Picked up scrap " + currentlyHeldObjectServer.itemProperties.itemName + " is not in the elevator; parenting it to spawnedScrapContainer");
						currentlyHeldObjectServer.transform.SetParent(playersManager.propsContainer, worldPositionStays: true);
					}
				}
				twoHanded = currentlyHeldObjectServer.itemProperties.twoHanded;
				twoHandedAnimation = currentlyHeldObjectServer.itemProperties.twoHandedAnimation;
				if (!base.IsOwner)
				{
					currentlyHeldObjectServer.parentObject = serverItemHolder;
					isHoldingObject = true;
					carryWeight = Mathf.Clamp(carryWeight + (currentlyHeldObjectServer.itemProperties.weight - 1f), 1f, 10f);
					if (currentlyHeldObjectServer.itemProperties.grabSFX != null)
					{
						itemAudio.PlayOneShot(currentlyHeldObjectServer.itemProperties.grabSFX, 1f);
					}
					if (currentlyHeldObjectServer.playerHeldBy != null)
					{
						Debug.Log($"playerHeldBy on grabbed object: {currentlyHeldObjectServer.playerHeldBy}");
					}
					else
					{
						Debug.Log("grabbed object playerHeldBy is null");
					}
				}
			}
			else
			{
				Debug.Log($"Player #{playerClientId}: Was grabbing object {grabbedObject.NetworkObjectId} validated by server? : {grabValidated}");
				if (base.IsOwner)
				{
					Debug.Log("Local client got grab invalidated");
					grabInvalidated = true;
				}
			}
}
		private void Discard_performed(InputAction.CallbackContext context)
		{
			if (!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject) || !context.performed)
			{
				return;
			}
			if (StartOfRound.Instance.localPlayerUsingController && ShipBuildModeManager.Instance.InBuildMode)
			{
				ShipBuildModeManager.Instance.StoreObjectLocalClient();
			}
			else
			{
				if (timeSinceSwitchingSlots < 0.2f || isGrabbingObjectAnimation || timeSinceSwitchingSlots < 0.2f || isTypingChat || inSpecialInteractAnimation || activatingItem)
				{
					return;
				}
				ShipBuildModeManager.Instance.CancelBuildMode();
				if (throwingObject || !isHoldingObject || !(currentlyHeldObjectServer != null))
				{
					return;
				}
				if (UnityEngine.Object.FindObjectOfType<DepositItemsDesk>() != null && currentlyHeldObjectServer != null)
				{
					DepositItemsDesk depositItemsDesk = UnityEngine.Object.FindObjectOfType<DepositItemsDesk>();
					if (UnityEngine.Object.FindObjectOfType<DepositItemsDesk>().triggerCollider.bounds.Contains(currentlyHeldObjectServer.transform.position))
					{
						depositItemsDesk.PlaceItemOnCounter(this);
						return;
					}
				}
				StartCoroutine(waitToEndOfFrameToDiscard());
			}
		}

		private IEnumerator waitToEndOfFrameToDiscard()
		{
			yield return new WaitForEndOfFrame();
			DiscardHeldObject();
		}

		public void DespawnHeldObject()
		{
			if (currentlyHeldObjectServer != null)
			{
				SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
				playerBodyAnimator.SetBool("cancelHolding", value: true);
				playerBodyAnimator.SetTrigger("Throw");
				if (base.IsOwner)
				{
					HUDManager.Instance.itemSlotIcons[currentItemSlot].enabled = false;
					HUDManager.Instance.holdingTwoHandedItem.enabled = false;
				}
				throwingObject = true;
				DespawnHeldObjectOnClient();
				DespawnHeldObjectServerRpc();
			}
		}

		private void DespawnHeldObjectOnClient()
		{
			ItemSlots[currentItemSlot] = null;
			twoHanded = false;
			twoHandedAnimation = false;
			carryWeight = Mathf.Clamp(carryWeight - (currentlyHeldObjectServer.itemProperties.weight - 1f), 1f, 10f);
			isHoldingObject = false;
			hasThrownObject = true;
		}

		[ServerRpc]
		private void DespawnHeldObjectServerRpc()
{			{
				if (currentlyHeldObjectServer != null)
				{
					currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>().Despawn();
				}
				DespawnHeldObjectClientRpc();
			}
}
		[ClientRpc]
		private void DespawnHeldObjectClientRpc()
{			{
				if (!base.IsOwner)
				{
					DespawnHeldObjectOnClient();
				}
				else
				{
					throwingObject = false;
				}
			}
}
		public void DiscardHeldObject(bool placeObject = false, NetworkObject parentObjectTo = null, Vector3 placePosition = default(Vector3), bool matchRotationOfParent = true)
		{
			if (!base.IsOwner)
			{
				return;
			}
			SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
			playerBodyAnimator.SetBool("cancelHolding", value: true);
			playerBodyAnimator.SetTrigger("Throw");
			HUDManager.Instance.itemSlotIcons[currentItemSlot].enabled = false;
			HUDManager.Instance.holdingTwoHandedItem.enabled = false;
			if (!placeObject)
			{
				Vector3 hitPoint;
				NetworkObject physicsRegionOfDroppedObject = currentlyHeldObjectServer.GetPhysicsRegionOfDroppedObject(this, out hitPoint);
				if (physicsRegionOfDroppedObject != null)
				{
					placePosition = hitPoint;
					parentObjectTo = physicsRegionOfDroppedObject;
					placeObject = true;
					matchRotationOfParent = false;
				}
			}
			if (placeObject)
			{
				if (parentObjectTo == null)
				{
					throwingObject = true;
					placePosition = ((!isInElevator) ? StartOfRound.Instance.propsContainer.InverseTransformPoint(placePosition) : StartOfRound.Instance.elevatorTransform.InverseTransformPoint(placePosition));
					int floorYRot = (int)base.transform.localEulerAngles.y;
					SetObjectAsNoLongerHeld(isInElevator, isInHangarShipRoom, placePosition, currentlyHeldObjectServer, floorYRot);
					currentlyHeldObjectServer.DiscardItemOnClient();
					ThrowObjectServerRpc(currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>(), isInElevator, isInHangarShipRoom, placePosition, floorYRot);
				}
				else
				{
					PlaceGrabbableObject(parentObjectTo.transform, placePosition, matchRotationOfParent, currentlyHeldObjectServer);
					currentlyHeldObjectServer.DiscardItemOnClient();
					PlaceObjectServerRpc(currentlyHeldObjectServer.gameObject.GetComponent<NetworkObject>(), parentObjectTo, placePosition, matchRotationOfParent);
				}
				return;
			}
			throwingObject = true;
			bool droppedInElevator = isInElevator;
			Vector3 targetFloorPosition;
			if (!isInElevator)
			{
				Vector3 vector = ((!currentlyHeldObjectServer.itemProperties.allowDroppingAheadOfPlayer) ? currentlyHeldObjectServer.GetItemFloorPosition() : DropItemAheadOfPlayer());
				if (!playersManager.shipBounds.bounds.Contains(vector))
				{
					targetFloorPosition = playersManager.propsContainer.InverseTransformPoint(vector);
				}
				else
				{
					droppedInElevator = true;
					targetFloorPosition = playersManager.elevatorTransform.InverseTransformPoint(vector);
				}
			}
			else
			{
				Vector3 vector = currentlyHeldObjectServer.GetItemFloorPosition();
				if (!playersManager.shipBounds.bounds.Contains(vector))
				{
					droppedInElevator = false;
					targetFloorPosition = playersManager.propsContainer.InverseTransformPoint(vector);
				}
				else
				{
					targetFloorPosition = playersManager.elevatorTransform.InverseTransformPoint(vector);
				}
			}
			int floorYRot2 = (int)base.transform.localEulerAngles.y;
			SetObjectAsNoLongerHeld(droppedInElevator, isInHangarShipRoom, targetFloorPosition, currentlyHeldObjectServer, floorYRot2);
			currentlyHeldObjectServer.DiscardItemOnClient();
			ThrowObjectServerRpc(currentlyHeldObjectServer.NetworkObject, droppedInElevator, isInHangarShipRoom, targetFloorPosition, floorYRot2);
		}

		private Vector3 DropItemAheadOfPlayer()
		{
			Vector3 zero = Vector3.zero;
			Ray ray = new Ray(base.transform.position + Vector3.up * 0.4f, gameplayCamera.transform.forward);
			zero = ((!Physics.Raycast(ray, out hit, 1.7f, 268438273, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hit.distance - 0.3f, 0.01f, 2f)));
			Vector3 itemFloorPosition = currentlyHeldObjectServer.GetItemFloorPosition(zero);
			if (itemFloorPosition == zero)
			{
				itemFloorPosition = currentlyHeldObjectServer.GetItemFloorPosition();
			}
			return itemFloorPosition;
		}

		[ServerRpc]
		private void ThrowObjectServerRpc(NetworkObjectReference grabbedObject, bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, int floorYRot)
{			{
				if (grabbedObject.TryGet(out var _))
				{
					ThrowObjectClientRpc(droppedInElevator, droppedInShipRoom, targetFloorPosition, grabbedObject, floorYRot);
				}
				else
				{
					Debug.LogError("Object was not thrown because it does not exist on the server.");
				}
			}
}
		[ClientRpc]
		private void ThrowObjectClientRpc(bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, NetworkObjectReference grabbedObject, int floorYRot)
{			if (grabbedObject.TryGet(out var networkObject))
			{
				GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
				if (!base.IsOwner)
				{
					SetObjectAsNoLongerHeld(droppedInElevator, droppedInShipRoom, targetFloorPosition, component);
				}
				if (!component.itemProperties.syncDiscardFunction)
				{
					component.playerHeldBy = null;
				}
				if (component == currentlyHeldObjectServer)
				{
					currentlyHeldObjectServer = null;
				}
				else
				{
					string arg = "null";
					if (currentlyHeldObjectServer != null)
					{
						arg = currentlyHeldObjectServer.gameObject.name;
					}
					Debug.LogError($"ThrowObjectClientRpc called for an object which is not the same as currentlyHeldObjectServer which is {arg}, on player #{playerClientId}.");
				}
			}
			else
			{
				Debug.LogError("The server did not have a reference to the held object (when attempting to THROW on client.)");
			}
			if (base.IsOwner)
			{
				throwingObject = false;
			}
}
		public void SetObjectAsNoLongerHeld(bool droppedInElevator, bool droppedInShipRoom, Vector3 targetFloorPosition, GrabbableObject dropObject, int floorYRot = -1)
		{
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] == dropObject)
				{
					ItemSlots[i] = null;
				}
			}
			dropObject.heldByPlayerOnServer = false;
			dropObject.parentObject = null;
			if (droppedInElevator)
			{
				dropObject.transform.SetParent(playersManager.elevatorTransform, worldPositionStays: true);
			}
			else
			{
				dropObject.transform.SetParent(playersManager.propsContainer, worldPositionStays: true);
			}
			SetItemInElevator(droppedInShipRoom, droppedInElevator, dropObject);
			dropObject.EnablePhysics(enable: true);
			dropObject.EnableItemMeshes(enable: true);
			dropObject.transform.localScale = dropObject.originalScale;
			dropObject.isHeld = false;
			dropObject.isPocketed = false;
			dropObject.fallTime = 0f;
			dropObject.startFallingPosition = dropObject.transform.parent.InverseTransformPoint(dropObject.transform.position);
			dropObject.targetFloorPosition = targetFloorPosition;
			dropObject.floorYRot = floorYRot;
			twoHanded = false;
			twoHandedAnimation = false;
			carryWeight = Mathf.Clamp(carryWeight - (dropObject.itemProperties.weight - 1f), 1f, 10f);
			isHoldingObject = false;
			hasThrownObject = true;
		}

		public void SetAllItemsInElevator(bool inShipRoom, bool inElevator)
		{
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] != null)
				{
					SetItemInElevator(inShipRoom, inElevator, ItemSlots[i]);
				}
			}
		}

		public void SetItemInElevator(bool droppedInShipRoom, bool droppedInElevator, GrabbableObject gObject)
		{
			gObject.isInElevator = droppedInElevator;
			if (gObject.isInShipRoom == droppedInShipRoom)
			{
				return;
			}
			gObject.isInShipRoom = droppedInShipRoom;
			if (!gObject.scrapPersistedThroughRounds)
			{
				if (droppedInShipRoom)
				{
					RoundManager.Instance.scrapCollectedInLevel += gObject.scrapValue;
					StartOfRound.Instance.gameStats.allPlayerStats[playerClientId].profitable += gObject.scrapValue;
					RoundManager.Instance.CollectNewScrapForThisRound(gObject);
					gObject.OnBroughtToShip();
					if (gObject.itemProperties.isScrap && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, gObject.transform.position) < 12f)
					{
						HUDManager.Instance.DisplayTip("Got scrap!", "To sell, use the terminal to route the ship to the company building.", isWarning: false, useSave: true, "LCTip_SellScrap");
					}
				}
				else
				{
					if (gObject.scrapPersistedThroughRounds)
					{
						return;
					}
					RoundManager.Instance.scrapCollectedInLevel -= gObject.scrapValue;
					StartOfRound.Instance.gameStats.allPlayerStats[playerClientId].profitable -= gObject.scrapValue;
				}
				HUDManager.Instance.SetQuota(RoundManager.Instance.scrapCollectedInLevel);
			}
			if (droppedInShipRoom)
			{
				StartOfRound.Instance.currentShipItemCount++;
			}
			else
			{
				StartOfRound.Instance.currentShipItemCount--;
			}
		}

		[ServerRpc]
		private void PlaceObjectServerRpc(NetworkObjectReference grabbedObject, NetworkObjectReference parentObject, Vector3 placePositionOffset = default(Vector3), bool matchRotationOfParent = true)
{			{
				NetworkObject networkObject3;
				NetworkObject networkObject4;
				if (grabbedObject.TryGet(out var _) && parentObject.TryGet(out var _))
				{
					PlaceObjectClientRpc(parentObject, placePositionOffset, matchRotationOfParent, grabbedObject);
				}
				else if (!grabbedObject.TryGet(out networkObject3))
				{
					Debug.LogError($"Object placement not synced to clients, missing reference to a network object: placing object with id: {grabbedObject.NetworkObjectId}; player #{playerClientId}");
				}
				else if (!parentObject.TryGet(out networkObject4))
				{
					Debug.LogError($"Object placement not synced to clients, missing reference to a network object: parent object with id: {grabbedObject.NetworkObjectId}; player #{playerClientId}");
				}
			}
}
		public void PlaceGrabbableObject(Transform parentObject, Vector3 positionOffset, bool matchRotationOfParent, GrabbableObject placeObject)
		{
			PlayerPhysicsRegion componentInChildren = parentObject.GetComponentInChildren<PlayerPhysicsRegion>();
			if (componentInChildren != null && componentInChildren.allowDroppingItems)
			{
				parentObject = componentInChildren.physicsTransform;
			}
			placeObject.EnablePhysics(enable: true);
			placeObject.EnableItemMeshes(enable: true);
			placeObject.isHeld = false;
			placeObject.isPocketed = false;
			placeObject.heldByPlayerOnServer = false;
			SetItemInElevator(isInHangarShipRoom, isInElevator, placeObject);
			placeObject.parentObject = null;
			placeObject.transform.SetParent(parentObject, worldPositionStays: true);
			placeObject.startFallingPosition = placeObject.transform.localPosition;
			placeObject.transform.localScale = placeObject.originalScale;
			placeObject.transform.localPosition = positionOffset;
			placeObject.targetFloorPosition = positionOffset;
			if (!matchRotationOfParent)
			{
				placeObject.fallTime = 0f;
			}
			else
			{
				placeObject.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
				placeObject.fallTime = 1.1f;
			}
			placeObject.OnPlaceObject();
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] == placeObject)
				{
					ItemSlots[i] = null;
				}
			}
			twoHanded = false;
			twoHandedAnimation = false;
			carryWeight = Mathf.Clamp(carryWeight - (placeObject.itemProperties.weight - 1f), 1f, 10f);
			isHoldingObject = false;
			hasThrownObject = true;
		}

		[ClientRpc]
		private void PlaceObjectClientRpc(NetworkObjectReference parentObjectReference, Vector3 placePositionOffset, bool matchRotationOfParent, NetworkObjectReference grabbedObject)
{			if (grabbedObject.TryGet(out var networkObject))
			{
				GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
				if (!base.IsOwner)
				{
					if (parentObjectReference.TryGet(out var networkObject2))
					{
						PlaceGrabbableObject(networkObject2.transform, placePositionOffset, matchRotationOfParent, component);
					}
					else
					{
						PlaceGrabbableObject(null, placePositionOffset, matchRotationOfParent, component);
						Debug.LogError($"Reference to parent object when placing was missing. object: {component} placed by {base.gameObject.name}");
					}
				}
				if (!component.itemProperties.syncDiscardFunction)
				{
					component.playerHeldBy = null;
				}
				if (currentlyHeldObjectServer == component)
				{
					currentlyHeldObjectServer = null;
				}
				else
				{
					string arg = "null";
					if (currentlyHeldObjectServer != null)
					{
						arg = currentlyHeldObjectServer.gameObject.name;
					}
					Debug.LogError($"ThrowObjectClientRpc called for an object which is not the same as currentlyHeldObjectServer which is {arg}, on player #{playerClientId}.");
				}
			}
			else
			{
				Debug.LogError("The server did not have a reference to the held object (when attempting to PLACE object on client.)");
			}
			if (base.IsOwner)
			{
				throwingObject = false;
				HUDManager.Instance.itemSlotIcons[currentItemSlot].enabled = false;
			}
}
		private void SetFreeCamera_performed(InputAction.CallbackContext context)
		{
			if (base.IsServer && base.IsOwner && context.performed)
			{
				_ = isTypingChat;
			}
		}

		public void ChangeHelmetLight(int lightNumber, bool enable = true)
		{
			for (int i = 0; i < allHelmetLights.Length; i++)
			{
				allHelmetLights[i].enabled = false;
			}
			allHelmetLights[lightNumber].enabled = enable;
			helmetLight = allHelmetLights[lightNumber];
		}

		public void SetInSpecialMenu(bool setInMenu, SpecialHUDMenu menuType = SpecialHUDMenu.BeltBagInventory)
		{
			inSpecialMenu = setInMenu;
			if (setInMenu)
			{
				Cursor.lockState = CursorLockMode.None;
				cursorIcon.enabled = false;
				cursorTip.text = "";
				if (hoveringOverTrigger != null)
				{
					previousHoveringOverTrigger = hoveringOverTrigger;
				}
				hoveringOverTrigger = null;
				HUDManager.Instance.OpenSpecialMenu(menuType);
			}
			else
			{
				Cursor.lockState = CursorLockMode.Locked;
				if (!StartOfRound.Instance.localPlayerUsingController)
				{
					Cursor.visible = false;
				}
				HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.defaultCursorTex);
				HUDManager.Instance.CloseSpecialMenus();
			}
		}

		private void OpenMenu_performed(InputAction.CallbackContext context)
		{
			if (!context.performed)
			{
				return;
			}
			if (NetworkManager.Singleton == null)
			{
				if (!isTestingPlayer)
				{
					return;
				}
			}
			else if (!base.IsOwner || (!isPlayerControlled && !isPlayerDead) || (base.IsServer && !isHostPlayerObject))
			{
				return;
			}
			if (!inTerminalMenu && (!UnityEngine.Object.FindObjectOfType<Terminal>() || !(UnityEngine.Object.FindObjectOfType<Terminal>().timeSinceTerminalInUse < 0.25f)) && !isTypingChat)
			{
				if (inSpecialMenu)
				{
					SetInSpecialMenu(setInMenu: false);
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
				else if (!quickMenuManager.isMenuOpen)
				{
					quickMenuManager.OpenQuickMenu();
				}
				else if (IngamePlayerSettings.Instance.changesNotApplied)
				{
					IngamePlayerSettings.Instance.DisplayConfirmChangesScreen(visible: true);
				}
				else
				{
					quickMenuManager.CloseQuickMenu();
				}
			}
		}

		private void Jump_performed(InputAction.CallbackContext context)
		{
			if (!quickMenuManager.isMenuOpen && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && !inSpecialInteractAnimation && !isTypingChat && (isMovementHindered <= 0 || isUnderwater) && !isExhausted && (thisController.isGrounded || (!isJumping && IsPlayerNearGround())) && !isJumping && (!isPlayerSliding || playerSlidingTimer > 2.5f) && !isCrouching)
			{
				playerSlidingTimer = 0f;
				isJumping = true;
				sprintMeter = Mathf.Clamp(sprintMeter - 0.08f, 0f, 1f);
				StartOfRound.Instance.PlayerJumpEvent.Invoke(this);
				PlayJumpAudio();
				if (jumpCoroutine != null)
				{
					StopCoroutine(jumpCoroutine);
				}
				jumpCoroutine = StartCoroutine(PlayerJump());
				if (StartOfRound.Instance.connectedPlayersAmount != 0)
				{
					PlayerJumpedServerRpc();
				}
			}
		}

		private IEnumerator PlayerJump()
		{
			playerBodyAnimator.SetBool("Jumping", value: true);
			yield return new WaitForSeconds(0.15f);
			fallValue = jumpForce;
			fallValueUncapped = jumpForce;
			yield return new WaitForSeconds(0.1f);
			isJumping = false;
			isFallingFromJump = true;
			yield return new WaitUntil(() => thisController.isGrounded);
			playerBodyAnimator.SetBool("Jumping", value: false);
			isFallingFromJump = false;
			PlayerHitGroundEffects();
			jumpCoroutine = null;
		}

		[ServerRpc]
		public void PlayerJumpedServerRpc()
{			{
				PlayerJumpedClientRpc();
			}
}
		[ClientRpc]
		public void PlayerJumpedClientRpc()
{if(!base.IsOwner)				{
					StartOfRound.Instance.PlayerJumpEvent.Invoke(this);
					PlayJumpAudio();
				}
}
		private void PlayJumpAudio()
		{
			if (StartOfRound.Instance.unlockablesList.unlockables[currentSuitID].jumpAudio != null)
			{
				movementAudio.PlayOneShot(StartOfRound.Instance.unlockablesList.unlockables[currentSuitID].jumpAudio);
			}
			else
			{
				movementAudio.PlayOneShot(StartOfRound.Instance.playerJumpSFX);
			}
		}

		public void ResetFallGravity()
		{
			takingFallDamage = false;
			fallValue = 0f;
			fallValueUncapped = 0f;
		}

		private void PlayerLookInput()
		{
			if (quickMenuManager.isMenuOpen || inSpecialMenu || StartOfRound.Instance.newGameIsLoading || disableLookInput)
			{
				return;
			}
			Vector2 vector = playerActions.Movement.Look.ReadValue<Vector2>() * 0.008f * IngamePlayerSettings.Instance.settings.lookSensitivity;
			if (IngamePlayerSettings.Instance.settings.invertYAxis)
			{
				vector.y *= -1f;
			}
			if (isFreeCamera)
			{
				StartOfRound.Instance.freeCinematicCameraTurnCompass.Rotate(new Vector3(0f, vector.x, 0f));
				cameraUp -= vector.y;
				cameraUp = Mathf.Clamp(cameraUp, -80f, 80f);
				StartOfRound.Instance.freeCinematicCameraTurnCompass.transform.localEulerAngles = new Vector3(cameraUp, StartOfRound.Instance.freeCinematicCameraTurnCompass.transform.localEulerAngles.y, 0f);
				return;
			}
			if (IsInspectingItem)
			{
				vector *= 0.01f;
				Vector3 localPosition = rightArmProceduralTarget.localPosition;
				localPosition.x = Mathf.Clamp(localPosition.x + vector.x, rightArmProceduralTargetBasePosition.x - 0.1f, rightArmProceduralTargetBasePosition.x + 0.1f);
				localPosition.y = Mathf.Clamp(localPosition.y + vector.y, rightArmProceduralTargetBasePosition.y - 0.3f, rightArmProceduralTargetBasePosition.y + 0.3f);
				rightArmProceduralTarget.localPosition = new Vector3(localPosition.x, localPosition.y, rightArmProceduralTarget.localPosition.z);
				return;
			}
			if (base.IsOwner && isPlayerDead && (!base.IsServer || isHostPlayerObject))
			{
				spectateCameraPivot.Rotate(new Vector3(0f, vector.x, 0f));
				cameraUp -= vector.y;
				cameraUp = Mathf.Clamp(cameraUp, -80f, 80f);
				spectateCameraPivot.transform.localEulerAngles = new Vector3(cameraUp, spectateCameraPivot.transform.localEulerAngles.y, 0f);
			}
			if ((!base.IsOwner || !isPlayerControlled || (base.IsServer && !isHostPlayerObject)) && !isTestingPlayer)
			{
				return;
			}
			StartOfRound.Instance.playerLookMagnitudeThisFrame = vector.magnitude * Time.deltaTime;
			if (inSpecialInteractAnimation && (isClimbingLadder || clampLooking))
			{
				if (isClimbingLadder)
				{
					minVerticalClamp = 25f;
					maxVerticalClamp = -60f;
					horizontalClamp = 60f;
				}
				LookClamped(vector);
				SyncFullRotWithClients(inVehicleAnimation);
				return;
			}
			if (smoothLookMultiplier != 25f)
			{
				CalculateSmoothLookingInput(vector);
			}
			else
			{
				CalculateNormalLookingInput(vector);
			}
			if (isTestingPlayer || (base.IsServer && playersManager.connectedPlayersAmount < 1))
			{
				return;
			}
			if (jetpackControls || disablingJetpackControls)
			{
				SyncFullRotWithClients();
			}
			else if (updatePlayerLookInterval > 0.1f && Physics.OverlapSphere(base.transform.position, 35f, playerMask).Length != 0)
			{
				updatePlayerLookInterval = 0f;
				if (Mathf.Abs(oldCameraUp + previousYRot - (cameraUp + thisPlayerBody.eulerAngles.y)) > 3f && !playersManager.newGameIsLoading)
				{
					UpdatePlayerRotationServerRpc((short)cameraUp, (short)thisPlayerBody.localEulerAngles.y);
					oldCameraUp = cameraUp;
					previousYRot = thisPlayerBody.localEulerAngles.y;
				}
			}
		}

		private void SyncFullRotWithClients(bool syncLookDirection = false)
		{
			if ((jetpackControls || disablingJetpackControls || isClimbingLadder || syncLookDirection) && updatePlayerLookInterval > 0.15f)
			{
				updatePlayerLookInterval = 0f;
				if (syncLookDirection)
				{
					UpdatePlayerRotationFullServerRpc(base.transform.eulerAngles, gameplayCamera.transform.localEulerAngles, syncingCameraRotation: true);
				}
				else
				{
					UpdatePlayerRotationFullServerRpc(base.transform.eulerAngles, Vector3.zero, syncingCameraRotation: false);
				}
				syncFullRotation = base.transform.eulerAngles;
			}
		}

		private void CalculateSmoothLookingInput(Vector2 inputVector)
		{
			if (!smoothLookEnabledLastFrame)
			{
				smoothLookEnabledLastFrame = true;
				smoothLookTurnCompass.rotation = gameplayCamera.transform.rotation;
				smoothLookTurnCompass.SetParent(null);
			}
			smoothLookTurnCompass.Rotate(new Vector3(0f, inputVector.x, 0f), Space.Self);
			cameraUp -= inputVector.y;
			cameraUp = Mathf.Clamp(cameraUp, -80f, 60f);
			smoothLookTurnCompass.localEulerAngles = new Vector3(cameraUp, smoothLookTurnCompass.localEulerAngles.y, smoothLookTurnCompass.localEulerAngles.z);
			smoothLookTurnCompass.eulerAngles = new Vector3(smoothLookTurnCompass.eulerAngles.x, smoothLookTurnCompass.eulerAngles.y, thisPlayerBody.transform.eulerAngles.z);
			thisPlayerBody.eulerAngles = new Vector3(thisPlayerBody.eulerAngles.x, Mathf.LerpAngle(thisPlayerBody.eulerAngles.y, smoothLookTurnCompass.eulerAngles.y, smoothLookMultiplier * Time.deltaTime), thisPlayerBody.eulerAngles.z);
			gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(gameplayCamera.transform.localEulerAngles.x, cameraUp, smoothLookMultiplier * Time.deltaTime), gameplayCamera.transform.localEulerAngles.y, gameplayCamera.transform.localEulerAngles.z);
		}

		private void CalculateNormalLookingInput(Vector2 inputVector)
		{
			if (smoothLookEnabledLastFrame)
			{
				smoothLookEnabledLastFrame = false;
			}
			if (inShockingMinigame)
			{
				inputVector.x = Mathf.Clamp(inputVector.x, -15f, 15f);
				inputVector.y = Mathf.Clamp(inputVector.y, -15f, 15f);
				turnCompass.Rotate(new Vector3(0f, inputVector.x, 0f));
			}
			else if (jetpackControls)
			{
				jetpackTurnCompass.Rotate(new Vector3(0f, inputVector.x, 0f), Space.Self);
			}
			else
			{
				thisPlayerBody.Rotate(new Vector3(0f, inputVector.x, 0f), Space.Self);
			}
			cameraUp -= inputVector.y;
			cameraUp = Mathf.Clamp(cameraUp, -80f, 80f);
			gameplayCamera.transform.localEulerAngles = new Vector3(cameraUp, gameplayCamera.transform.localEulerAngles.y, gameplayCamera.transform.localEulerAngles.z);
			playerHudUIContainer.Rotate(new Vector3(inputVector.y / 4f, (0f - inputVector.x) / 8f, 0f) * Mathf.Clamp(Time.deltaTime * 15f, 0.02f, 4f));
		}

		private void Look_performed(InputAction.CallbackContext context)
		{
			if (quickMenuManager.isMenuOpen || inSpecialMenu)
			{
				if (context.ReadValue<Vector2>().magnitude > 0.001f)
				{
					Cursor.visible = true;
				}
			}
			else if ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer)
			{
				StartOfRound.Instance.localPlayerUsingController = !InputControlPath.MatchesPrefix("<Mouse>", context.control);
			}
		}

		bool IShockableWithGun.CanBeShocked()
		{
			return !isPlayerDead;
		}

		float IShockableWithGun.GetDifficultyMultiplier()
		{
			return 1.5f;
		}

		NetworkObject IShockableWithGun.GetNetworkObject()
		{
			return base.NetworkObject;
		}

		Transform IShockableWithGun.GetShockableTransform()
		{
			return base.transform;
		}

		Vector3 IShockableWithGun.GetShockablePosition()
		{
			return gameplayCamera.transform.position;
		}

		void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
		{
			isMovementHindered++;
			hinderedMultiplier *= 3.5f;
			if (inSpecialInteractAnimation && currentTriggerInAnimationWith != null)
			{
				MoveToExitSpecialAnimation componentInChildren = currentTriggerInAnimationWith.GetComponentInChildren<MoveToExitSpecialAnimation>();
				if (componentInChildren.electricChair)
				{
					componentInChildren.PowerSurgeChairServerRpc();
				}
			}
		}

		void IShockableWithGun.StopShockingWithGun()
		{
			isMovementHindered = Mathf.Clamp(isMovementHindered - 1, 0, 1000);
			hinderedMultiplier /= 3.5f;
		}

		public void ForceTurnTowardsTarget()
		{
			if (inSpecialInteractAnimation && inShockingMinigame && shockingTarget != null)
			{
				targetScreenPos = turnCompassCamera.WorldToViewportPoint(shockingTarget.position);
				shockMinigamePullPosition = targetScreenPos.x - 0.5f;
				float num = Mathf.Clamp(Time.deltaTime, 0f, 0.1f);
				if (targetScreenPos.x > 0.54f)
				{
					turnCompass.Rotate(Vector3.up * 2000f * num * Mathf.Abs(shockMinigamePullPosition));
					playerBodyAnimator.SetBool("PullingCameraRight", value: false);
					playerBodyAnimator.SetBool("PullingCameraLeft", value: true);
				}
				else if (targetScreenPos.x < 0.46f)
				{
					turnCompass.Rotate(Vector3.up * -2000f * num * Mathf.Abs(shockMinigamePullPosition));
					playerBodyAnimator.SetBool("PullingCameraLeft", value: false);
					playerBodyAnimator.SetBool("PullingCameraRight", value: true);
				}
				else
				{
					playerBodyAnimator.SetBool("PullingCameraLeft", value: false);
					playerBodyAnimator.SetBool("PullingCameraRight", value: false);
				}
				targetScreenPos = gameplayCamera.WorldToViewportPoint(shockingTarget.position + Vector3.up * 0.35f);
				if (targetScreenPos.y > 0.6f)
				{
					cameraUp = Mathf.Clamp(Mathf.Lerp(cameraUp, cameraUp - 25f, 25f * num * Mathf.Abs(targetScreenPos.y - 0.5f)), -89f, 89f);
				}
				else if (targetScreenPos.y < 0.35f)
				{
					cameraUp = Mathf.Clamp(Mathf.Lerp(cameraUp, cameraUp + 25f, 25f * num * Mathf.Abs(targetScreenPos.y - 0.5f)), -89f, 89f);
				}
				gameplayCamera.transform.localEulerAngles = new Vector3(cameraUp, gameplayCamera.transform.localEulerAngles.y, gameplayCamera.transform.localEulerAngles.z);
				Vector3 zero = Vector3.zero;
				zero.y = turnCompass.eulerAngles.y;
				thisPlayerBody.rotation = Quaternion.Lerp(thisPlayerBody.rotation, Quaternion.Euler(zero), Time.deltaTime * 20f * (1f - Mathf.Abs(shockMinigamePullPosition)));
			}
		}

		private void LookClamped(Vector2 lookInput)
		{
			lookInput *= 2f;
			ladderCameraHorizontal += lookInput.x;
			ladderCameraHorizontal = Mathf.Clamp(ladderCameraHorizontal, 0f - horizontalClamp, horizontalClamp);
			cameraUp -= lookInput.y;
			cameraUp = Mathf.Clamp(cameraUp, maxVerticalClamp, minVerticalClamp);
			gameplayCamera.transform.localEulerAngles = new Vector3(cameraUp, ladderCameraHorizontal, gameplayCamera.transform.localEulerAngles.z);
		}

		private void Crouch_performed(InputAction.CallbackContext context)
		{
			if (context.performed && !quickMenuManager.isMenuOpen && ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer) && !inSpecialInteractAnimation && thisController.isGrounded && !isTypingChat && !isJumping && !isSprinting)
			{
				crouchMeter = Mathf.Min(crouchMeter + 0.3f, 1.3f);
				Crouch(!isCrouching);
			}
		}

		public void Crouch(bool crouch)
		{
			if (crouch)
			{
				if (sourcesCausingSinking <= 0 || !(sinkingValue > 0.6f))
				{
					isCrouching = true;
					StartOfRound.Instance.timeAtMakingLastPersonalMovement = Time.realtimeSinceStartup;
					playerBodyAnimator.SetTrigger("startCrouching");
					playerBodyAnimator.SetBool("crouching", value: true);
				}
			}
			else if (!Physics.Raycast(gameplayCamera.transform.position, Vector3.up, out hit, 1.1f, playersManager.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
			{
				isCrouching = false;
				StartOfRound.Instance.timeAtMakingLastPersonalMovement = Time.realtimeSinceStartup;
				playerBodyAnimator.SetBool("crouching", value: false);
			}
		}

		[ServerRpc]
		private void UpdatePlayerRotationServerRpc(short newRot, short newYRot)
{			try
			{
				UpdatePlayerRotationClientRpc(newRot, newYRot);
			}
			catch (Exception arg)
			{
				Debug.Log($"Client rpc parameters were likely not correct, so an RPC was skipped: {arg}");
			}
}
		[ClientRpc]
		private void UpdatePlayerRotationClientRpc(short newRot, short newYRot)
{			{
				playersManager.gameStats.allPlayerStats[playerClientId].turnAmount++;
				if (!base.IsOwner)
				{
					targetYRot = newYRot;
					targetLookRot = newRot;
				}
			}
}
		[ServerRpc]
		private void UpdatePlayerRotationFullServerRpc(Vector3 playerEulers, Vector3 cameraRotation, bool syncingCameraRotation)
{			try
			{
				if (syncingCameraRotation)
				{
					UpdatePlayerRotationFullWithCameraClientRpc(playerEulers, cameraRotation);
				}
				else
				{
					UpdatePlayerRotationFullClientRpc(playerEulers);
				}
			}
			catch (Exception arg)
			{
				Debug.Log($"Client rpc parameters were likely not correct, so an RPC was skipped: {arg}");
			}
}
		[ClientRpc]
		private void UpdatePlayerRotationFullWithCameraClientRpc(Vector3 playerEulers, Vector3 cameraRotation)
{if(!base.IsOwner)				{
					syncFullRotation = playerEulers;
					syncFullCameraRotation = cameraRotation;
				}
}
		[ClientRpc]
		private void UpdatePlayerRotationFullClientRpc(Vector3 playerEulers)
{if(!base.IsOwner)				{
					syncFullRotation = playerEulers;
				}
}
		private void UpdatePlayerAnimationsToOtherClients(Vector2 moveInputVector)
		{
			updatePlayerAnimationsInterval += Time.deltaTime;
			if (!inSpecialInteractAnimation && !(updatePlayerAnimationsInterval > 0.14f))
			{
				return;
			}
			updatePlayerAnimationsInterval = 0f;
			currentAnimationSpeed = playerBodyAnimator.GetFloat("animationSpeed");
			for (int i = 0; i < playerBodyAnimator.layerCount; i++)
			{
				currentAnimationStateHash[i] = playerBodyAnimator.GetCurrentAnimatorStateInfo(i).fullPathHash;
				if (previousAnimationStateHash[i] != currentAnimationStateHash[i])
				{
					previousAnimationStateHash[i] = currentAnimationStateHash[i];
					previousAnimationSpeed = currentAnimationSpeed;
					UpdatePlayerAnimationServerRpc(currentAnimationStateHash[i], currentAnimationSpeed);
					return;
				}
			}
			if (previousAnimationSpeed != currentAnimationSpeed)
			{
				previousAnimationSpeed = currentAnimationSpeed;
				UpdatePlayerAnimationServerRpc(0, currentAnimationSpeed);
			}
		}

		public void DisableInVehicleAnimationSync()
		{
			DisableInVehicleAnimationServerRpc();
		}

		[ServerRpc(RequireOwnership = false)]
		public void DisableInVehicleAnimationServerRpc()
				{
					Debug.Log("Sending disable invehicleanimation serverrpc");
					DisableInVehicleAnimationClientRpc();
				}

		[ClientRpc]
		public void DisableInVehicleAnimationClientRpc()
				{
					Debug.Log("Client got DisableInVehicleAnimationClientRpc for player " + base.gameObject.name);
					inVehicleAnimation = false;
				}

		[ServerRpc]
		private void UpdatePlayerAnimationServerRpc(int animationState, float animationSpeed)
{			try
			{
				UpdatePlayerAnimationClientRpc(animationState, animationSpeed);
			}
			catch (Exception arg)
			{
				Debug.Log($"Client rpc parameters were likely not correct, so an RPC was skipped: {arg}");
			}
}
		[ClientRpc]
		private void UpdatePlayerAnimationClientRpc(int animationState, float animationSpeed)
{if(base.IsOwner)			{
				return;
			}
			if (playerBodyAnimator.GetFloat("animationSpeed") != animationSpeed)
			{
				playerBodyAnimator.SetFloat("animationSpeed", animationSpeed);
			}
			if (animationState == 0 || playerBodyAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash == animationState)
			{
				return;
			}
			for (int i = 0; i < playerBodyAnimator.layerCount; i++)
			{
				if (playerBodyAnimator.HasState(i, animationState))
				{
					playerBodyAnimator.CrossFadeInFixedTime(animationState, 0.1f);
					break;
				}
			}
}
		public void UpdateSpecialAnimationValue(bool specialAnimation, short yVal = 0, float timed = 0f, bool climbingLadder = false)
		{
			IsInSpecialAnimationServerRpc(specialAnimation, timed, climbingLadder);
			ResetZAndXRotation();
			if (specialAnimation)
			{
				UpdatePlayerRotationServerRpc(0, yVal);
			}
		}

		public void ResetZAndXRotation()
		{
			Vector3 localEulerAngles = thisPlayerBody.localEulerAngles;
			localEulerAngles.x = 0f;
			localEulerAngles.z = 0f;
			thisPlayerBody.localEulerAngles = localEulerAngles;
		}

		[ServerRpc]
		private void IsInSpecialAnimationServerRpc(bool specialAnimation, float timed = 0f, bool climbingLadder = false)
{			try
			{
				IsInSpecialAnimationClientRpc(specialAnimation, timed, climbingLadder);
			}
			catch (Exception arg)
			{
				Debug.Log($"Client rpc parameters were likely not correct, so an RPC was skipped: {arg}");
			}
}
		[ClientRpc]
		private void IsInSpecialAnimationClientRpc(bool specialAnimation, float timed = 0f, bool climbingLadder = false)
{if(base.IsOwner)			{
				return;
			}
			Debug.Log("Setting animation on client");
			inSpecialInteractAnimation = specialAnimation;
			isClimbingLadder = climbingLadder;
			if (!specialAnimation && !climbingLadder)
			{
				ResetZAndXRotation();
			}
			if (timed > 0f)
			{
				if (timeSpecialAnimationCoroutine != null)
				{
					StopCoroutine(timeSpecialAnimationCoroutine);
				}
				timeSpecialAnimationCoroutine = StartCoroutine(timeSpecialAnimation(timed));
			}
}
		private IEnumerator timeSpecialAnimation(float time)
		{
			yield return new WaitForSeconds(time);
			inSpecialInteractAnimation = false;
			timeSpecialAnimationCoroutine = null;
		}

		public void GetCurrentMaterialStandingOn()
		{
			interactRay = new Ray(thisPlayerBody.position + Vector3.up, -Vector3.up);
			if (!Physics.Raycast(interactRay, out hit, 6f, StartOfRound.Instance.walkableSurfacesMask, QueryTriggerInteraction.Ignore) || hit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].surfaceTag))
			{
				return;
			}
			for (int i = 0; i < StartOfRound.Instance.footstepSurfaces.Length; i++)
			{
				if (hit.collider.CompareTag(StartOfRound.Instance.footstepSurfaces[i].surfaceTag))
				{
					currentFootstepSurfaceIndex = i;
					break;
				}
			}
		}

		public void PlayFootstepSound()
		{
			GetCurrentMaterialStandingOn();
			int num = UnityEngine.Random.Range(0, StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length);
			if (num == previousFootstepClip)
			{
				num = (num + 1) % StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips.Length;
			}
			movementAudio.pitch = UnityEngine.Random.Range(0.93f, 1.07f);
			bool flag = ((!base.IsOwner) ? playerBodyAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Sprinting") : isSprinting);
			float num2 = 0.9f;
			if (!flag)
			{
				num2 = 0.6f;
			}
			movementAudio.PlayOneShot(StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips[num], num2);
			previousFootstepClip = num;
			WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.footstepSurfaces[currentFootstepSurfaceIndex].clips[num], num2);
		}

		public void PlayFootstepServer()
		{
			if (!isClimbingLadder && !inSpecialInteractAnimation && !base.IsOwner && isPlayerControlled)
			{
				bool noiseIsInsideClosedShip = isInHangarShipRoom && playersManager.hangarDoorsClosed;
				if (isSprinting)
				{
					RoundManager.Instance.PlayAudibleNoise(base.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 7);
				}
				else
				{
					RoundManager.Instance.PlayAudibleNoise(base.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 7);
				}
				PlayFootstepSound();
			}
		}

		public void PlayFootstepLocal()
		{
			if (!isClimbingLadder && !inSpecialInteractAnimation && (isTestingPlayer || (base.IsOwner && isPlayerControlled)))
			{
				bool noiseIsInsideClosedShip = isInHangarShipRoom && playersManager.hangarDoorsClosed;
				if (isSprinting)
				{
					RoundManager.Instance.PlayAudibleNoise(base.transform.position, 22f, 0.6f, 0, noiseIsInsideClosedShip, 6);
				}
				else
				{
					RoundManager.Instance.PlayAudibleNoise(base.transform.position, 17f, 0.4f, 0, noiseIsInsideClosedShip, 6);
				}
				PlayFootstepSound();
			}
		}

		[ServerRpc]
		private void UpdatePlayerPositionServerRpc(Vector3 newPos, bool inElevator, bool inShipRoom, bool exhausted, bool isPlayerGrounded)
{			try
			{
				UpdatePlayerPositionClientRpc(newPos, inElevator, inShipRoom, exhausted, isPlayerGrounded);
			}
			catch (Exception arg)
			{
				Debug.Log($"Caught an error when sending player position RPC; likely a player disconnected to cause this. Error: {arg}");
			}
}
		[ClientRpc]
		private void UpdatePlayerPositionClientRpc(Vector3 newPos, bool inElevator, bool isInShip, bool exhausted, bool isPlayerGrounded)
{			playersManager.gameStats.allPlayerStats[playerClientId].stepsTaken++;
			playersManager.gameStats.allStepsTaken++;
			bool flag = currentFootstepSurfaceIndex == 8 && ((base.IsOwner && thisController.isGrounded) || isPlayerGrounded);
			if (bleedingHeavily || flag)
			{
				DropBlood(Vector3.down, bleedingHeavily, flag);
			}
			timeSincePlayerMoving = 0f;
			if (base.IsOwner)
			{
				return;
			}
			if (!inElevator)
			{
				isInHangarShipRoom = false;
			}
			isExhausted = exhausted;
			isInElevator = inElevator;
			isInHangarShipRoom = isInShip;
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] != null && ItemSlots[i].isHeld)
				{
					if (ItemSlots[i].isInShipRoom != isInShip)
					{
						SetItemInElevator(isInShip, isInElevator, ItemSlots[i]);
					}
					ItemSlots[i].isInElevator = inElevator;
				}
			}
			oldPlayerPosition = serverPlayerPosition;
			if (!disableSyncInAnimation && !inVehicleAnimation)
			{
				serverPlayerPosition = newPos;
			}
			if (overridePhysicsParent != null)
			{
				if (overridePhysicsParent != lastSyncedPhysicsParent)
				{
					lastSyncedPhysicsParent = overridePhysicsParent;
					base.transform.SetParent(overridePhysicsParent);
				}
			}
			else if (physicsParent != null)
			{
				if (physicsParent != lastSyncedPhysicsParent)
				{
					lastSyncedPhysicsParent = physicsParent;
					base.transform.SetParent(physicsParent);
				}
			}
			else if (lastSyncedPhysicsParent != null)
			{
				lastSyncedPhysicsParent = null;
			}
			else if (isInElevator)
			{
				if (!parentedToElevatorLastFrame)
				{
					parentedToElevatorLastFrame = true;
					base.transform.SetParent(playersManager.elevatorTransform);
				}
			}
			else if (parentedToElevatorLastFrame)
			{
				parentedToElevatorLastFrame = false;
				base.transform.SetParent(playersManager.playersContainer);
				base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
			}
}
		[ServerRpc]
		private void RemovePlayerPhysicsParentServerRpc(Vector3 newPos, bool removeOverride, bool removeBoth, bool inElevator, bool isInShip)
{			try
			{
				RemovePlayerPhysicsParentClientRpc(newPos, removeOverride, removeBoth, inElevator, isInShip);
			}
			catch (Exception arg)
			{
				Debug.Log($"Caught an error when sending RemovePhysicsParent RPC. Error: {arg}");
			}
}
		[ClientRpc]
		private void RemovePlayerPhysicsParentClientRpc(Vector3 newPos, bool removeOverride, bool removeBoth, bool inElevator, bool isInShip)
{if(base.IsOwner)			{
				return;
			}
			if (waitForPhysicsParentCoroutine != null)
			{
				StopCoroutine(waitForPhysicsParentCoroutine);
			}
			if (!inElevator)
			{
				isInHangarShipRoom = false;
			}
			isInElevator = inElevator;
			isInHangarShipRoom = isInShip;
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] != null && ItemSlots[i].isHeld)
				{
					if (ItemSlots[i].isInShipRoom != isInShip)
					{
						SetItemInElevator(isInShip, isInElevator, ItemSlots[i]);
					}
					ItemSlots[i].isInElevator = inElevator;
				}
			}
			oldPlayerPosition = serverPlayerPosition;
			if (!disableSyncInAnimation && !inVehicleAnimation)
			{
				serverPlayerPosition = newPos;
			}
			if (removeBoth)
			{
				physicsParent = null;
				overridePhysicsParent = null;
			}
			else if (removeOverride)
			{
				overridePhysicsParent = null;
			}
			else
			{
				physicsParent = null;
			}
			if (isPlayerDead)
			{
				return;
			}
			if (overridePhysicsParent != null)
			{
				if (overridePhysicsParent != lastSyncedPhysicsParent)
				{
					lastSyncedPhysicsParent = overridePhysicsParent;
					base.transform.SetParent(overridePhysicsParent);
				}
			}
			else if (physicsParent != null)
			{
				if (physicsParent != lastSyncedPhysicsParent)
				{
					lastSyncedPhysicsParent = physicsParent;
					base.transform.SetParent(physicsParent);
				}
			}
			else if (isInElevator)
			{
				parentedToElevatorLastFrame = true;
				base.transform.SetParent(playersManager.elevatorTransform);
			}
			else
			{
				parentedToElevatorLastFrame = false;
				base.transform.SetParent(playersManager.playersContainer);
				base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
			}
}
		[ServerRpc]
		private void UpdatePlayerPhysicsParentServerRpc(Vector3 newPos, NetworkObjectReference setPhysicsParent, bool isOverride, bool inElevator, bool isInShip)
{			try
			{
				UpdatePlayerPhysicsParentClientRpc(newPos, setPhysicsParent, isOverride, inElevator, isInShip);
			}
			catch (Exception arg)
			{
				Debug.Log($"Caught an error when sending UpdatePhysicsParent RPC. Error: {arg}");
			}
}
		[ClientRpc]
		private void UpdatePlayerPhysicsParentClientRpc(Vector3 newPos, NetworkObjectReference setPhysicsParent, bool isOverride, bool inElevator, bool isInShip)
{if(base.IsOwner)			{
				return;
			}
			if (!inElevator)
			{
				isInHangarShipRoom = false;
			}
			isInElevator = inElevator;
			isInHangarShipRoom = isInShip;
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				if (ItemSlots[i] != null && ItemSlots[i].isHeld)
				{
					if (ItemSlots[i].isInShipRoom != isInShip)
					{
						SetItemInElevator(isInShip, isInElevator, ItemSlots[i]);
					}
					ItemSlots[i].isInElevator = inElevator;
				}
			}
			oldPlayerPosition = serverPlayerPosition;
			if (!disableSyncInAnimation && !inVehicleAnimation)
			{
				serverPlayerPosition = newPos;
			}
			if (!setPhysicsParent.TryGet(out var _))
			{
				if (waitForPhysicsParentCoroutine != null)
				{
					StopCoroutine(waitForPhysicsParentCoroutine);
				}
				waitForPhysicsParentCoroutine = StartCoroutine(waitForPhysicsParentToSpawn(newPos, setPhysicsParent, isOverride, inElevator, isInShip));
			}
			else
			{
				UpdatePlayerPhysicsParentLocal(setPhysicsParent, isOverride);
			}
}
		public void UpdatePlayerPhysicsParentLocal(NetworkObjectReference setPhysicsParent, bool isOverride)
		{
			if (base.IsOwner)
			{
				return;
			}
			if (setPhysicsParent.TryGet(out var networkObject))
			{
				if (isOverride)
				{
					overridePhysicsParent = networkObject.transform;
				}
				else
				{
					overridePhysicsParent = null;
					if (networkObject.GetComponentInChildren<PlayerPhysicsRegion>() != null)
					{
						physicsParent = networkObject.GetComponentInChildren<PlayerPhysicsRegion>().physicsTransform;
					}
				}
			}
			if (isPlayerDead)
			{
				return;
			}
			if (overridePhysicsParent != null)
			{
				if (overridePhysicsParent != lastSyncedPhysicsParent)
				{
					lastSyncedPhysicsParent = overridePhysicsParent;
					base.transform.SetParent(overridePhysicsParent);
				}
				return;
			}
			if (physicsParent != null)
			{
				if (physicsParent != lastSyncedPhysicsParent)
				{
					lastSyncedPhysicsParent = physicsParent;
					base.transform.SetParent(physicsParent);
				}
				return;
			}
			if (lastSyncedPhysicsParent != null)
			{
				lastSyncedPhysicsParent = null;
			}
			if (isInElevator)
			{
				parentedToElevatorLastFrame = true;
				base.transform.SetParent(playersManager.elevatorTransform);
			}
			else
			{
				parentedToElevatorLastFrame = false;
				base.transform.SetParent(playersManager.playersContainer);
				base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
			}
		}

		private IEnumerator waitForPhysicsParentToSpawn(Vector3 newPos, NetworkObjectReference setPhysicsParent, bool isOverride, bool inElevator, bool isInShip)
		{
			float time = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup - time < 5f)
			{
				yield return new WaitForSeconds(0.5f);
				if (setPhysicsParent.TryGet(out var _))
				{
					UpdatePlayerPhysicsParentLocal(setPhysicsParent, isOverride);
				}
			}
			waitForPhysicsParentCoroutine = null;
		}

		[ServerRpc]
		public void LandFromJumpServerRpc(bool fallHard)
{			{
				LandFromJumpClientRpc(fallHard);
			}
}
		[ClientRpc]
		public void LandFromJumpClientRpc(bool fallHard)
{if(!base.IsOwner)			{
				if (fallHard)
				{
					movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
				}
				else
				{
					movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 0.7f);
				}
			}
}
		public void LimpAnimationSpeed()
		{
			if (base.IsOwner)
			{
				limpMultiplier = 1.1f;
			}
		}

		public void SpawnPlayerAnimation()
		{
			UpdateSpecialAnimationValue(specialAnimation: true, 0);
			inSpecialInteractAnimation = true;
			playerBodyAnimator.ResetTrigger("SpawnPlayer");
			playerBodyAnimator.SetTrigger("SpawnPlayer");
			StartCoroutine(spawnPlayerAnimTimer());
		}

		private IEnumerator spawnPlayerAnimTimer()
		{
			yield return new WaitForSeconds(3f);
			inSpecialInteractAnimation = false;
			UpdateSpecialAnimationValue(specialAnimation: false, 0);
		}

		[ServerRpc(RequireOwnership = false)]
		private void SendNewPlayerValuesServerRpc(ulong newPlayerSteamId)
{if(sentPlayerValues)			{
				return;
			}
			sentPlayerValues = true;
			if (!GameNetworkManager.Instance.disableSteam && GameNetworkManager.Instance.currentLobby.HasValue)
			{
				if (!GameNetworkManager.Instance.steamIdsInLobby.Contains(newPlayerSteamId))
				{
					NetworkManager.Singleton.DisconnectClient(actualClientId);
					return;
				}
				if (StartOfRound.Instance.KickedClientIds.Contains(newPlayerSteamId))
				{
					NetworkManager.Singleton.DisconnectClient(actualClientId);
					return;
				}
			}
			List<ulong> list = new List<ulong>();
			for (int i = 0; i < 4; i++)
			{
				if (i == (int)playerClientId)
				{
					list.Add(newPlayerSteamId);
				}
				else
				{
					list.Add(playersManager.allPlayerScripts[i].playerSteamId);
				}
			}
			SendNewPlayerValuesClientRpc(list.ToArray());
}
		[ClientRpc]
		private void SendNewPlayerValuesClientRpc(ulong[] playerSteamIds)
{			for (int i = 0; i < playerSteamIds.Length; i++)
			{
				if (playersManager.allPlayerScripts[i].isPlayerControlled || playersManager.allPlayerScripts[i].isPlayerDead)
				{
					string input = NoPunctuation(new Friend(playerSteamIds[i]).Name);
					input = Regex.Replace(input, "[^\\w\\._]", "");
					if (input == string.Empty || input.Length == 0)
					{
						input = "Nameless";
					}
					else if (input.Length <= 2)
					{
						input += "0";
					}
					playersManager.allPlayerScripts[i].playerSteamId = playerSteamIds[i];
					playersManager.allPlayerScripts[i].playerUsername = input;
					playersManager.allPlayerScripts[i].usernameBillboardText.text = input;
					string playerName = input;
					int numberOfDuplicateNamesInLobby = GetNumberOfDuplicateNamesInLobby();
					if (numberOfDuplicateNamesInLobby > 0)
					{
						playerName = $"{input}{numberOfDuplicateNamesInLobby}";
					}
					quickMenuManager.AddUserToPlayerList(playerSteamIds[i], playerName, i);
					StartOfRound.Instance.mapScreen.radarTargets[i].name = playerName;
				}
			}
			StartOfRound.Instance.StartTrackingAllPlayerVoices();
			if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
			{
				GameNetworkManager.Instance.localPlayerController.updatePositionForNewlyJoinedClient = true;
			}
}
		private int GetNumberOfDuplicateNamesInLobby()
		{
			int num = 0;
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				if ((StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled || playersManager.allPlayerScripts[i].isPlayerDead) && !(StartOfRound.Instance.allPlayerScripts[i] == this) && StartOfRound.Instance.allPlayerScripts[i].playerUsername == playerUsername)
				{
					num++;
				}
			}
			for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
			{
				if ((StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled || playersManager.allPlayerScripts[j].isPlayerDead) && !(StartOfRound.Instance.allPlayerScripts[j] == this) && StartOfRound.Instance.allPlayerScripts[j].playerUsername == $"{StartOfRound.Instance.allPlayerScripts[j].playerUsername}{num}")
				{
					num++;
				}
			}
			return num;
		}

		private string NoPunctuation(string input)
		{
			return new string(input.Where((char c) => char.IsLetter(c)).ToArray());
		}

		public void ConnectClientToPlayerObject()
		{
			if (!isTestingPlayer)
			{
				actualClientId = NetworkManager.Singleton.LocalClientId;
				playersManager.thisClientPlayerId = (int)playerClientId;
			}
			if (GameNetworkManager.Instance != null)
			{
				GameNetworkManager.Instance.localPlayerController = this;
			}
			playersManager.localPlayerController = this;
			for (int i = 0; i < playersManager.allPlayerObjects.Length; i++)
			{
				PlayerControllerB component = playersManager.allPlayerObjects[i].GetComponent<PlayerControllerB>();
				if (!component.isPlayerControlled && !component.isTestingPlayer)
				{
					component.TeleportPlayer(playersManager.notSpawnedPosition.position);
				}
				if (component != playersManager.localPlayerController)
				{
					playersManager.OtherClients.Add(component);
				}
			}
			playersManager.localClientHasControl = true;
			if (playerBodyAnimator.runtimeAnimatorController != playersManager.localClientAnimatorController)
			{
				playerBodyAnimator.runtimeAnimatorController = playersManager.localClientAnimatorController;
			}
			if (!isTestingPlayer)
			{
				if (!GameNetworkManager.Instance.disableSteam)
				{
					playerUsername = GameNetworkManager.Instance.username;
					SendNewPlayerValuesServerRpc(SteamClient.SteamId);
				}
				else if (base.IsServer)
				{
					UnityEngine.Object.FindObjectOfType<QuickMenuManager>().AddUserToPlayerList(0uL, "Player #0", 0);
				}
				HUDManager.Instance.AddTextToChatOnServer(playerUsername + " joined the ship.");
				usernameAlpha.alpha = 0f;
				usernameBillboardText.enabled = false;
			}
		}

		private void ChangeAudioListenerToObject(GameObject addToObject)
		{
			activeAudioListener.transform.SetParent(addToObject.transform);
			activeAudioListener.transform.localEulerAngles = Vector3.zero;
			activeAudioListener.transform.localPosition = Vector3.zero;
			StartOfRound.Instance.audioListener = activeAudioListener;
		}

		private void PlayerHitGroundEffects()
		{
			GetCurrentMaterialStandingOn();
			if (fallValue < -9f)
			{
				if (fallValue < -16f)
				{
					movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundHard, 1f);
					WalkieTalkie.TransmitOneShotAudio(movementAudio, StartOfRound.Instance.playerHitGroundHard);
				}
				else if (fallValue < -2f)
				{
					movementAudio.PlayOneShot(StartOfRound.Instance.playerHitGroundSoft, 1f);
				}
				LandFromJumpServerRpc(fallValue < -16f);
			}
			float num = fallValueUncapped;
			if (disabledJetpackControlsThisFrame && Vector3.Angle(base.transform.up, Vector3.up) > 80f)
			{
				num -= 10f;
			}
			if (takingFallDamage && !isSpeedCheating)
			{
				if (fallValueUncapped < -48.5f)
				{
					DamagePlayer(100, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
				}
				else if (fallValueUncapped < -45f)
				{
					DamagePlayer(80, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
				}
				else if (fallValueUncapped < -40f)
				{
					DamagePlayer(50, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
				}
				else if (fallValue < -38f)
				{
					DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity);
				}
			}
			if (fallValue < -16f)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 7f);
			}
		}

		public void OnControllerColliderHit(ControllerColliderHit hit)
		{
			if (base.IsOwner)
			{
				controllerCollisionPoint = hit.point;
			}
		}

		private void CalculateGroundNormal()
		{
			Vector3[] array = new Vector3[5]
			{
				base.transform.position + Vector3.up * 1.5f,
				base.transform.position + Vector3.up * 1.5f + Vector3.right * thisController.radius,
				base.transform.position + Vector3.up * 1.5f + -Vector3.right * thisController.radius,
				base.transform.position + Vector3.up * 1.5f + Vector3.forward * thisController.radius,
				base.transform.position + Vector3.up * 1.5f + -Vector3.forward * thisController.radius
			};
			for (int i = 0; i < array.Length; i++)
			{
				if (Physics.Raycast(array[i], -Vector3.up, out hit, 1.8f, 268438273, QueryTriggerInteraction.Ignore))
				{
					playerGroundNormal = hit.normal;
					return;
				}
				Debug.DrawRay(array[i], -Vector3.up, Color.red);
				playerGroundNormal = Vector3.up;
			}
			if (controllerCollisionPoint != Vector3.zero)
			{
				if (Physics.Raycast(controllerCollisionPoint + Vector3.up * 0.3f, -Vector3.up, out hit, 1f, 268438273, QueryTriggerInteraction.Ignore))
				{
					playerGroundNormal = hit.normal;
				}
				else
				{
					playerGroundNormal = Vector3.up;
				}
				controllerCollisionPoint = Vector3.zero;
			}
		}

		private bool IsPlayerNearGround()
		{
			interactRay = new Ray(base.transform.position, Vector3.down);
			return Physics.Raycast(interactRay, 0.15f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore);
		}

		[ServerRpc]
		public void DisableJetpackModeServerRpc()
{			{
				DisableJetpackModeClientRpc();
			}
}
		[ClientRpc]
		public void DisableJetpackModeClientRpc()
{if(!base.IsOwner)				{
					DisableJetpackControlsLocally();
				}
}
		public void DisableJetpackControlsLocally()
		{
			jetpackControls = false;
			thisController.radius = 0.4f;
			jetpackTurnCompass.rotation = base.transform.rotation;
			startedJetpackControls = false;
			disablingJetpackControls = false;
			maxJetpackAngle = -1f;
			jetpackRandomIntensity = 0f;
		}

		private void Update()
		{
			if ((base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject)) || isTestingPlayer)
			{
				if (isCameraDisabled)
				{
					isCameraDisabled = false;
					Debug.Log("Taking control of player " + base.gameObject.name + " and enabling camera!");
					StartOfRound.Instance.SwitchCamera(gameplayCamera);
					thisPlayerModel.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
					mapRadarDirectionIndicator.enabled = true;
					thisPlayerModelArms.enabled = true;
					playerScreen.enabled = true;
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
					base.gameObject.GetComponent<CharacterController>().enabled = true;
					activeAudioReverbFilter = activeAudioListener.GetComponent<AudioReverbFilter>();
					activeAudioReverbFilter.enabled = true;
					ChangeAudioListenerToObject(gameplayCamera.gameObject);
					if (playerBodyAnimator.runtimeAnimatorController != playersManager.localClientAnimatorController)
					{
						playerBodyAnimator.runtimeAnimatorController = playersManager.localClientAnimatorController;
						if (!playerBodyAnimator.GetCurrentAnimatorStateInfo(5).IsTag("notInSpecialAnim"))
						{
							playerBodyAnimator.SetTrigger("SA_stopAnimation");
						}
					}
					if (justConnected)
					{
						justConnected = false;
						ConnectClientToPlayerObject();
					}
					SpawnPlayerAnimation();
					Debug.Log("!!!! ENABLING CAMERA FOR PLAYER: " + base.gameObject.name);
					Debug.Log($"!!!! connectedPlayersAmount: {playersManager.connectedPlayersAmount}");
				}
				hasBegunSpectating = false;
				playerHudUIContainer.rotation = Quaternion.Lerp(playerHudUIContainer.rotation, playerHudBaseRotation.rotation, 24f * Time.deltaTime);
				SetNightVisionEnabled(isNotLocalClient: false);
				timeSinceFearLevelUp += Time.deltaTime;
				ForceTurnTowardsTarget();
				if (inTerminalMenu)
				{
					targetFOV = 60f;
				}
				else if (IsInspectingItem)
				{
					rightArmProceduralRig.weight = Mathf.Lerp(rightArmProceduralRig.weight, 1f, 25f * Time.deltaTime);
					targetFOV = 46f;
				}
				else
				{
					rightArmProceduralRig.weight = Mathf.Lerp(rightArmProceduralRig.weight, 0f, 25f * Time.deltaTime);
					if (isSprinting)
					{
						targetFOV = 68f;
					}
					else
					{
						targetFOV = 66f;
					}
				}
				gameplayCamera.fieldOfView = Mathf.Lerp(gameplayCamera.fieldOfView, targetFOV, 6f * Time.deltaTime);
				moveInputVector = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Move").ReadValue<Vector2>();
				if (moveInputVector.magnitude > 0.2f)
				{
					StartOfRound.Instance.PlayerMoveInputEvent.Invoke(this);
				}
				float num = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Sprint").ReadValue<float>();
				if (quickMenuManager.isMenuOpen || isTypingChat || disableMoveInput || (inSpecialInteractAnimation && !isClimbingLadder && !inShockingMinigame))
				{
					moveInputVector = Vector2.zero;
				}
				SetFaceUnderwaterFilters();
				if (isWalking)
				{
					if (isFreeCamera || moveInputVector.sqrMagnitude <= 0.19f || (inSpecialInteractAnimation && !isClimbingLadder && !inShockingMinigame))
					{
						isWalking = false;
						isSprinting = false;
						playerBodyAnimator.SetBool("Walking", value: false);
						playerBodyAnimator.SetBool("Sprinting", value: false);
						playerBodyAnimator.SetBool("Sideways", value: false);
					}
					else if (num > 0.3f && movementHinderedPrev <= 0 && !criticallyInjured && sprintMeter > 0.1f)
					{
						if (!isSprinting && sprintMeter < 0.3f)
						{
							if (!isExhausted)
							{
								isExhausted = true;
							}
						}
						else
						{
							if (isCrouching)
							{
								Crouch(crouch: false);
							}
							isSprinting = true;
							playerBodyAnimator.SetBool("Sprinting", value: true);
						}
					}
					else
					{
						isSprinting = false;
						if (sprintMeter < 0.1f)
						{
							isExhausted = true;
						}
						playerBodyAnimator.SetBool("Sprinting", value: false);
					}
					if (isSprinting)
					{
						sprintMultiplier = Mathf.Lerp(sprintMultiplier, 2.25f, Time.deltaTime * 1f);
					}
					else
					{
						sprintMultiplier = Mathf.Lerp(sprintMultiplier, 1f, 10f * Time.deltaTime);
					}
					if (moveInputVector.y < 0.2f && moveInputVector.y > -0.2f && !inSpecialInteractAnimation)
					{
						playerBodyAnimator.SetBool("Sideways", value: true);
						isSidling = true;
					}
					else
					{
						playerBodyAnimator.SetBool("Sideways", value: false);
						isSidling = false;
					}
					if (!inVehicleAnimation)
					{
						if (enteringSpecialAnimation)
						{
							playerBodyAnimator.SetFloat("animationSpeed", 1f);
						}
						else if (moveInputVector.y < 0.5f && moveInputVector.x < 0.5f)
						{
							playerBodyAnimator.SetFloat("animationSpeed", -1f * Mathf.Clamp(slopeModifier + 1f, 0.7f, 1.4f));
							movingForward = false;
						}
						else
						{
							playerBodyAnimator.SetFloat("animationSpeed", 1f * Mathf.Clamp(slopeModifier + 1f, 0.7f, 1.4f));
							movingForward = true;
						}
					}
				}
				else
				{
					if (enteringSpecialAnimation)
					{
						playerBodyAnimator.SetFloat("animationSpeed", 1f);
					}
					else if (isClimbingLadder)
					{
						playerBodyAnimator.SetFloat("animationSpeed", 0f);
					}
					if (!isFreeCamera && moveInputVector.sqrMagnitude >= 0.19f && (!inSpecialInteractAnimation || isClimbingLadder || inShockingMinigame))
					{
						isWalking = true;
						playerBodyAnimator.SetBool("Walking", value: true);
					}
				}
				if (performingEmote && !CheckConditionsForEmote())
				{
					performingEmote = false;
					StopPerformingEmoteServerRpc();
					timeSinceStartingEmote = 0f;
				}
				timeSinceStartingEmote += Time.deltaTime;
				playerBodyAnimator.SetBool("hinderedMovement", isMovementHindered > 0);
				if (sourcesCausingSinking == 0)
				{
					if (isSinking)
					{
						isSinking = false;
						StopSinkingServerRpc();
					}
				}
				else
				{
					if (isSinking)
					{
						GetCurrentMaterialStandingOn();
						if (!CheckConditionsForSinkingInQuicksand())
						{
							isSinking = false;
							StopSinkingServerRpc();
						}
					}
					else if (!isSinking && CheckConditionsForSinkingInQuicksand())
					{
						isSinking = true;
						StartSinkingServerRpc(sinkingSpeedMultiplier, statusEffectAudioIndex);
					}
					if (sinkingValue >= 1f)
					{
						KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Suffocation);
					}
					else if (sinkingValue > 0.5f)
					{
						Crouch(crouch: false);
					}
				}
				if (isCrouching)
				{
					thisController.center = Vector3.Lerp(thisController.center, new Vector3(thisController.center.x, 0.72f, thisController.center.z), 8f * Time.deltaTime);
					thisController.height = Mathf.Lerp(thisController.height, 1.5f, 8f * Time.deltaTime);
				}
				else
				{
					crouchMeter = Mathf.Max(crouchMeter - Time.deltaTime * 2f, 0f);
					thisController.center = Vector3.Lerp(thisController.center, new Vector3(thisController.center.x, 1.28f, thisController.center.z), 8f * Time.deltaTime);
					thisController.height = Mathf.Lerp(thisController.height, 2.5f, 8f * Time.deltaTime);
				}
				if (isFreeCamera)
				{
					float num2 = movementSpeed / 1.75f;
					if (num > 0.5f)
					{
						num2 *= 5f;
					}
					Vector3 vector = (playersManager.freeCinematicCameraTurnCompass.transform.right * moveInputVector.x + playersManager.freeCinematicCameraTurnCompass.transform.forward * moveInputVector.y) * num2;
					playersManager.freeCinematicCameraTurnCompass.transform.position += vector * Time.deltaTime;
					StartOfRound.Instance.freeCinematicCamera.transform.position = Vector3.Lerp(StartOfRound.Instance.freeCinematicCamera.transform.position, StartOfRound.Instance.freeCinematicCameraTurnCompass.transform.position, 3f * Time.deltaTime);
					StartOfRound.Instance.freeCinematicCamera.transform.rotation = Quaternion.Slerp(StartOfRound.Instance.freeCinematicCamera.transform.rotation, StartOfRound.Instance.freeCinematicCameraTurnCompass.rotation, 3f * Time.deltaTime);
				}
				if (disabledJetpackControlsThisFrame)
				{
					disabledJetpackControlsThisFrame = false;
				}
				if (jetpackControls)
				{
					if (disablingJetpackControls && thisController.isGrounded)
					{
						disabledJetpackControlsThisFrame = true;
						DisableJetpackControlsLocally();
						DisableJetpackModeServerRpc();
					}
					else if (!thisController.isGrounded)
					{
						if (!startedJetpackControls)
						{
							startedJetpackControls = true;
							jetpackTurnCompass.rotation = base.transform.rotation;
						}
						thisController.radius = Mathf.Lerp(thisController.radius, 1.25f, 10f * Time.deltaTime);
						Quaternion rotation = jetpackTurnCompass.rotation;
						jetpackTurnCompass.Rotate(new Vector3(0f, 0f, 0f - moveInputVector.x) * (180f * Time.deltaTime), Space.Self);
						if (maxJetpackAngle != -1f && Vector3.Angle(jetpackTurnCompass.up, Vector3.up) > maxJetpackAngle)
						{
							jetpackTurnCompass.rotation = rotation;
						}
						rotation = jetpackTurnCompass.rotation;
						jetpackTurnCompass.Rotate(new Vector3(moveInputVector.y, 0f, 0f) * (180f * Time.deltaTime), Space.Self);
						if (maxJetpackAngle != -1f && Vector3.Angle(jetpackTurnCompass.up, Vector3.up) > maxJetpackAngle)
						{
							jetpackTurnCompass.rotation = rotation;
						}
						if (jetpackRandomIntensity != -1f)
						{
							rotation = jetpackTurnCompass.rotation;
							Vector3 vector2 = new Vector3(Mathf.Clamp(UnityEngine.Random.Range(0f - jetpackRandomIntensity, jetpackRandomIntensity), 0f - maxJetpackAngle, maxJetpackAngle), Mathf.Clamp(UnityEngine.Random.Range(0f - jetpackRandomIntensity, jetpackRandomIntensity), 0f - maxJetpackAngle, maxJetpackAngle), Mathf.Clamp(UnityEngine.Random.Range(0f - jetpackRandomIntensity, jetpackRandomIntensity), 0f - maxJetpackAngle, maxJetpackAngle));
							jetpackTurnCompass.Rotate(vector2 * Time.deltaTime, Space.Self);
							if (maxJetpackAngle != -1f && Vector3.Angle(jetpackTurnCompass.up, Vector3.up) > maxJetpackAngle)
							{
								jetpackTurnCompass.rotation = rotation;
							}
						}
						base.transform.rotation = Quaternion.Slerp(base.transform.rotation, jetpackTurnCompass.rotation, 8f * Time.deltaTime);
					}
				}
				else if (!isClimbingLadder)
				{
					Vector3 localEulerAngles = base.transform.localEulerAngles;
					localEulerAngles.x = Mathf.LerpAngle(localEulerAngles.x, 0f, 15f * Time.deltaTime);
					localEulerAngles.z = Mathf.LerpAngle(localEulerAngles.z, 0f, 15f * Time.deltaTime);
					base.transform.localEulerAngles = localEulerAngles;
				}
				if (!inSpecialInteractAnimation || inShockingMinigame || StartOfRound.Instance.suckingPlayersOutOfShip)
				{
					if (isFreeCamera)
					{
						moveInputVector = Vector2.zero;
					}
					CalculateGroundNormal();
					float num3 = movementSpeed / carryWeight;
					if (sinkingValue > 0.73f)
					{
						num3 = 0f;
					}
					else
					{
						if (isCrouching)
						{
							num3 /= 1.5f;
						}
						else if (criticallyInjured && !isCrouching)
						{
							num3 *= limpMultiplier;
						}
						if (isSpeedCheating)
						{
							num3 *= 15f;
						}
						if (movementHinderedPrev > 0)
						{
							num3 /= 2f * hinderedMultiplier;
						}
						if (drunkness > 0f)
						{
							num3 *= StartOfRound.Instance.drunknessSpeedEffect.Evaluate(drunkness) / 5f + 1f;
						}
						if (!isCrouching && crouchMeter > 1.2f)
						{
							num3 *= 0.5f;
						}
						if (!isCrouching)
						{
							float num4 = Vector3.Dot(playerGroundNormal, walkForce);
							if (num4 > 0.05f)
							{
								slopeModifier = Mathf.MoveTowards(slopeModifier, num4, (slopeModifierSpeed + 0.45f) * Time.deltaTime);
							}
							else
							{
								slopeModifier = Mathf.MoveTowards(slopeModifier, num4, slopeModifierSpeed / 2f * Time.deltaTime);
							}
							num3 = Mathf.Max(num3 * 0.8f, num3 + slopeIntensity * slopeModifier);
						}
					}
					if (isTypingChat || (jetpackControls && !thisController.isGrounded) || StartOfRound.Instance.suckingPlayersOutOfShip)
					{
						moveInputVector = Vector2.zero;
					}
					Vector3 vector3 = new Vector3(0f, 0f, 0f);
					int num5 = Physics.OverlapSphereNonAlloc(base.transform.position, 0.65f, nearByPlayers, StartOfRound.Instance.playersMask);
					for (int i = 0; i < num5; i++)
					{
						vector3 += Vector3.Normalize((base.transform.position - nearByPlayers[i].transform.position) * 100f) * 1.2f;
					}
					int num6 = Physics.OverlapSphereNonAlloc(base.transform.position, 1.25f, nearByPlayers, 524288);
					for (int j = 0; j < num6; j++)
					{
						EnemyAICollisionDetect component = nearByPlayers[j].gameObject.GetComponent<EnemyAICollisionDetect>();
						if (component != null && component.mainScript != null && !component.mainScript.isEnemyDead && Vector3.Distance(base.transform.position, nearByPlayers[j].transform.position) < component.mainScript.enemyType.pushPlayerDistance)
						{
							vector3 += Vector3.Normalize((base.transform.position - nearByPlayers[j].transform.position) * 100f) * component.mainScript.enemyType.pushPlayerForce;
						}
					}
					float num7 = 1f;
					walkForce = Vector3.MoveTowards(maxDistanceDelta: ((isFallingFromJump || isFallingNoJump) ? 1.33f : ((drunkness > 0.3f) ? Mathf.Clamp(Mathf.Abs(drunkness - 2.25f), 0.3f, 2.5f) : ((!isCrouching && crouchMeter > 1f) ? 15f : ((!isSprinting) ? (10f / carryWeight) : (5f / (carryWeight * 1.5f)))))) * Time.deltaTime, current: walkForce, target: base.transform.right * moveInputVector.x + base.transform.forward * moveInputVector.y);
					Vector3 vector4 = walkForce * num3 * sprintMultiplier + new Vector3(0f, fallValue, 0f) + vector3;
					vector4 += externalForces;
					if (externalForceAutoFade.magnitude > 0.05f)
					{
						vector4 += externalForceAutoFade;
						externalForceAutoFade = Vector3.Lerp(externalForceAutoFade, Vector3.zero, 2f * Time.deltaTime);
					}
					if (isPlayerSliding && thisController.isGrounded)
					{
						playerSlidingTimer += Time.deltaTime;
						if (slideFriction > maxSlideFriction)
						{
							slideFriction -= 35f * Time.deltaTime;
						}
						vector4 = new Vector3(vector4.x + (1f - playerGroundNormal.y) * playerGroundNormal.x * (1f - slideFriction), vector4.y, vector4.z + (1f - playerGroundNormal.y) * playerGroundNormal.z * (1f - slideFriction));
					}
					else
					{
						playerSlidingTimer = 0f;
						slideFriction = 0f;
					}
					_ = thisController.velocity.magnitude;
					thisController.Move(vector4 * Time.deltaTime);
					if (!inSpecialInteractAnimation || inShockingMinigame)
					{
						if (!thisController.isGrounded)
						{
							if (jetpackControls && !disablingJetpackControls)
							{
								fallValue = Mathf.MoveTowards(fallValue, jetpackCounteractiveForce, 9f * Time.deltaTime);
								fallValueUncapped = -8f;
							}
							else
							{
								fallValue = Mathf.Clamp(fallValue - 38f * Time.deltaTime, -150f, jumpForce);
								if (Mathf.Abs(externalForceAutoFade.y) - Mathf.Abs(fallValue) < 5f)
								{
									if (disablingJetpackControls)
									{
										fallValueUncapped -= 26f * Time.deltaTime;
									}
									else
									{
										fallValueUncapped -= 38f * Time.deltaTime;
									}
								}
							}
							if (!isJumping && !isFallingFromJump)
							{
								if (!isFallingNoJump)
								{
									isFallingNoJump = true;
									fallValue = -7f;
									fallValueUncapped = -7f;
								}
								else if (fallValue < -20f)
								{
									isCrouching = false;
									playerBodyAnimator.SetBool("crouching", value: false);
									playerBodyAnimator.SetBool("FallNoJump", value: true);
								}
							}
							if (fallValueUncapped < -35f)
							{
								takingFallDamage = true;
							}
						}
						else
						{
							movementHinderedPrev = isMovementHindered;
							if (!isJumping)
							{
								if (isFallingNoJump)
								{
									isFallingNoJump = false;
									if (!isCrouching && fallValue < -9f)
									{
										playerBodyAnimator.SetTrigger("ShortFallLanding");
									}
									PlayerHitGroundEffects();
								}
								if (!isFallingFromJump)
								{
									fallValue = -7f - Mathf.Clamp(12f * slopeModifier, 0f, 100f);
									fallValueUncapped = -7f - Mathf.Clamp(12f * slopeModifier, 0f, 100f);
								}
							}
							playerBodyAnimator.SetBool("FallNoJump", value: false);
						}
					}
					externalForces = Vector3.zero;
					if (!teleportingThisFrame && teleportedLastFrame)
					{
						teleportedLastFrame = false;
					}
					if (jetpackControls || disablingJetpackControls)
					{
						if (!teleportingThisFrame && !inSpecialInteractAnimation && !enteringSpecialAnimation && !isClimbingLadder && (StartOfRound.Instance.timeSinceRoundStarted > 1f || StartOfRound.Instance.testRoom != null))
						{
							float magnitude = thisController.velocity.magnitude;
							if (getAverageVelocityInterval <= 0f)
							{
								getAverageVelocityInterval = 0.04f;
								velocityAverageCount++;
								if (velocityAverageCount > velocityMovingAverageLength)
								{
									averageVelocity += (magnitude - averageVelocity) / (float)(velocityMovingAverageLength + 1);
								}
								else
								{
									averageVelocity += magnitude;
									if (velocityAverageCount == velocityMovingAverageLength)
									{
										averageVelocity /= velocityAverageCount;
									}
								}
							}
							else
							{
								getAverageVelocityInterval -= Time.deltaTime;
							}
							Debug.Log($"Average velocity: {averageVelocity}");
							if (timeSinceTakingGravityDamage > 0.6f && velocityAverageCount > 4)
							{
								float num8 = Vector3.Angle(base.transform.up, Vector3.up);
								if (Physics.CheckSphere(gameplayCamera.transform.position, 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) || (num8 > 65f && Physics.CheckSphere(lowerSpine.position, 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
								{
									if (averageVelocity > 17f)
									{
										Debug.Log("Take damage a");
										timeSinceTakingGravityDamage = 0f;
										DamagePlayer(Mathf.Clamp(85, 20, 100), hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity, 0, fallDamage: true, Vector3.ClampMagnitude(velocityLastFrame, 50f));
									}
									else if (averageVelocity > 9f)
									{
										Debug.Log("Take damage b");
										DamagePlayer(Mathf.Clamp(30, 20, 100), hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity, 0, fallDamage: true, Vector3.ClampMagnitude(velocityLastFrame, 50f));
										timeSinceTakingGravityDamage = 0.35f;
									}
									else if (num8 > 60f && averageVelocity > 6f)
									{
										Debug.Log("Take damage c");
										DamagePlayer(Mathf.Clamp(30, 20, 100), hasDamageSFX: true, callRPC: true, CauseOfDeath.Gravity, 0, fallDamage: true, Vector3.ClampMagnitude(velocityLastFrame, 50f));
										timeSinceTakingGravityDamage = 0f;
									}
								}
							}
							else
							{
								timeSinceTakingGravityDamage += Time.deltaTime;
							}
							velocityLastFrame = thisController.velocity;
							previousFrameDeltaTime = Time.deltaTime;
						}
						else
						{
							teleportingThisFrame = false;
						}
					}
					else
					{
						averageVelocity = 0f;
						velocityAverageCount = 0;
						timeSinceTakingGravityDamage = 0f;
					}
					isPlayerSliding = Vector3.Angle(Vector3.up, playerGroundNormal) >= thisController.slopeLimit;
				}
				else if (isClimbingLadder)
				{
					Vector3 direction = thisPlayerBody.up;
					Vector3 origin = gameplayCamera.transform.position + thisPlayerBody.up * 0.07f;
					if ((externalForces + externalForceAutoFade).magnitude > 8f)
					{
						CancelSpecialTriggerAnimations();
					}
					externalForces = Vector3.zero;
					externalForceAutoFade = Vector3.Lerp(externalForceAutoFade, Vector3.zero, 5f * Time.deltaTime);
					if (moveInputVector.y < 0f)
					{
						direction = -thisPlayerBody.up;
						origin = base.transform.position;
					}
					if (!Physics.Raycast(origin, direction, 0.15f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore))
					{
						thisPlayerBody.transform.position += thisPlayerBody.up * (moveInputVector.y * climbSpeed * Time.deltaTime);
					}
				}
				if (inVehicleAnimation && (externalForces + externalForceAutoFade).magnitude > 50f)
				{
					CancelSpecialTriggerAnimations();
				}
				teleportingThisFrame = false;
				playerEye.position = gameplayCamera.transform.position;
				playerEye.rotation = gameplayCamera.transform.rotation;
				if (isHoldingObject && currentlyHeldObjectServer == null)
				{
					DropAllHeldItems();
				}
				if ((NetworkManager.Singleton != null && !base.IsServer) || (!isTestingPlayer && playersManager.connectedPlayersAmount > 0) || oldConnectedPlayersAmount >= 1)
				{
					updatePlayerLookInterval += Time.deltaTime;
					UpdatePlayerAnimationsToOtherClients(moveInputVector);
				}
				ClickHoldInteraction();
			}
			else
			{
				if (!isCameraDisabled)
				{
					isCameraDisabled = true;
					gameplayCamera.enabled = false;
					visorCamera.enabled = false;
					thisPlayerModel.shadowCastingMode = ShadowCastingMode.On;
					thisPlayerModelArms.enabled = false;
					mapRadarDirectionIndicator.enabled = false;
					base.gameObject.GetComponent<CharacterController>().enabled = false;
					if (playerBodyAnimator.runtimeAnimatorController != playersManager.otherClientsAnimatorController)
					{
						playerBodyAnimator.runtimeAnimatorController = playersManager.otherClientsAnimatorController;
					}
					if (!isPlayerDead)
					{
						for (int k = 0; k < playersManager.allPlayerObjects.Length && !playersManager.allPlayerObjects[k].GetComponent<PlayerControllerB>().gameplayCamera.enabled; k++)
						{
							if (k == 4)
							{
								Debug.LogWarning("!!! No cameras are enabled !!!");
								playerScreen.enabled = false;
							}
						}
					}
					if ((bool)base.gameObject.GetComponent<Rigidbody>())
					{
						base.gameObject.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.None;
					}
					Debug.Log("!!!! DISABLING CAMERA FOR PLAYER: " + base.gameObject.name);
					Debug.Log($"!!!! connectedPlayersAmount: {playersManager.connectedPlayersAmount}");
				}
				SetNightVisionEnabled(isNotLocalClient: true);
				if (!isTestingPlayer && !isPlayerDead && isPlayerControlled)
				{
					if (!disableSyncInAnimation && !inVehicleAnimation)
					{
						if (snapToServerPosition)
						{
							base.transform.localPosition = Vector3.Lerp(base.transform.localPosition, serverPlayerPosition, 16f * Time.deltaTime);
						}
						else
						{
							float num9 = 8f;
							if (jetpackControls)
							{
								num9 = 15f;
							}
							float num10 = Mathf.Clamp(num9 * Vector3.Distance(base.transform.localPosition, serverPlayerPosition), 0.9f, 300f);
							base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, serverPlayerPosition, num10 * Time.deltaTime);
						}
					}
					if (inVehicleAnimation)
					{
						gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(gameplayCamera.transform.localEulerAngles.x, syncFullCameraRotation.x, 14f * Time.deltaTime), Mathf.LerpAngle(gameplayCamera.transform.localEulerAngles.y, syncFullCameraRotation.y, 14f * Time.deltaTime), Mathf.LerpAngle(gameplayCamera.transform.localEulerAngles.z, syncFullCameraRotation.z, 14f * Time.deltaTime));
					}
					else
					{
						gameplayCamera.transform.localEulerAngles = new Vector3(Mathf.LerpAngle(gameplayCamera.transform.localEulerAngles.x, targetLookRot, 14f * Time.deltaTime), 0f, 0f);
					}
					if (jetpackControls || disablingJetpackControls || isClimbingLadder)
					{
						if (!disableSyncInAnimation && !inVehicleAnimation)
						{
							base.transform.rotation = Quaternion.Lerp(Quaternion.Euler(base.transform.eulerAngles), Quaternion.Euler(syncFullRotation), 8f * Time.deltaTime);
						}
					}
					else
					{
						syncFullRotation = base.transform.eulerAngles;
						if (!disableSyncInAnimation && !inVehicleAnimation)
						{
							base.transform.localEulerAngles = new Vector3(base.transform.localEulerAngles.x, Mathf.LerpAngle(base.transform.localEulerAngles.y, targetYRot, 14f * Time.deltaTime), base.transform.localEulerAngles.z);
						}
						if (!inSpecialInteractAnimation && !disableSyncInAnimation && !inVehicleAnimation)
						{
							Vector3 localEulerAngles2 = base.transform.localEulerAngles;
							localEulerAngles2.x = Mathf.LerpAngle(localEulerAngles2.x, 0f, 25f * Time.deltaTime);
							localEulerAngles2.z = Mathf.LerpAngle(localEulerAngles2.z, 0f, 25f * Time.deltaTime);
							base.transform.localEulerAngles = localEulerAngles2;
						}
					}
					playerEye.position = gameplayCamera.transform.position;
					playerEye.localEulerAngles = new Vector3(targetLookRot, 0f, 0f);
					playerEye.eulerAngles = new Vector3(playerEye.eulerAngles.x, targetYRot, playerEye.eulerAngles.z);
				}
				else if ((isPlayerDead || !isPlayerControlled) && setPositionOfDeadPlayer)
				{
					base.transform.position = playersManager.notSpawnedPosition.position;
				}
				if (isInGameOverAnimation > 0f && deadBody != null && deadBody.gameObject.activeSelf)
				{
					isInGameOverAnimation -= Time.deltaTime;
				}
				else if (!hasBegunSpectating)
				{
					isInGameOverAnimation = 0f;
					hasBegunSpectating = true;
				}
			}
			timeSincePlayerMoving += Time.deltaTime;
			timeSinceMakingLoudNoise += Time.deltaTime;
			if (!inSpecialInteractAnimation)
			{
				if (playingQuickSpecialAnimation)
				{
					specialAnimationWeight = 1f;
				}
				else
				{
					specialAnimationWeight = Mathf.Lerp(specialAnimationWeight, 0f, Time.deltaTime * 12f);
				}
				if (!localArmsMatchCamera)
				{
					localArmsTransform.position = playerModelArmsMetarig.position + playerModelArmsMetarig.forward * -0.445f;
					playerModelArmsMetarig.rotation = Quaternion.Lerp(playerModelArmsMetarig.rotation, localArmsRotationTarget.rotation, 15f * Time.deltaTime);
				}
			}
			else
			{
				if (((!isClimbingLadder && !inShockingMinigame) || freeRotationInInteractAnimation) && !clampLooking && !inVehicleAnimation)
				{
					cameraUp = Mathf.Lerp(cameraUp, 0f, 5f * Time.deltaTime);
					gameplayCamera.transform.localEulerAngles = new Vector3(cameraUp, gameplayCamera.transform.localEulerAngles.y, gameplayCamera.transform.localEulerAngles.z);
				}
				specialAnimationWeight = Mathf.Lerp(specialAnimationWeight, 1f, Time.deltaTime * 20f);
				playerModelArmsMetarig.localEulerAngles = new Vector3(-90f, 0f, 0f);
			}
			interactRay = new Ray(base.transform.position + Vector3.up * 2.3f, base.transform.forward);
			if (doingUpperBodyEmote > 0f || (!twoHanded && (currentlyHeldObjectServer == null || !currentlyHeldObjectServer.itemProperties.disableHandsOnWall) && Physics.Raycast(interactRay, out hit, 0.53f, walkableSurfacesNoPlayersMask, QueryTriggerInteraction.Ignore)))
			{
				doingUpperBodyEmote -= Time.deltaTime;
				handsOnWallWeight = Mathf.Lerp(handsOnWallWeight, 1f, 15f * Time.deltaTime);
			}
			else
			{
				handsOnWallWeight = Mathf.Lerp(handsOnWallWeight, 0f, 15f * Time.deltaTime);
			}
			playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("UpperBodyEmotes"), handsOnWallWeight);
			if (performingEmote)
			{
				emoteLayerWeight = Mathf.Lerp(emoteLayerWeight, 1f, 10f * Time.deltaTime);
			}
			else
			{
				emoteLayerWeight = Mathf.Lerp(emoteLayerWeight, 0f, 10f * Time.deltaTime);
			}
			playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("EmotesNoArms"), emoteLayerWeight);
			meshContainer.position = Vector3.Lerp(base.transform.position, base.transform.position - Vector3.up * 2.8f, StartOfRound.Instance.playerSinkingCurve.Evaluate(sinkingValue));
			if (isSinking && !inSpecialInteractAnimation && inAnimationWithEnemy == null)
			{
				sinkingValue = Mathf.Clamp(sinkingValue + Time.deltaTime * sinkingSpeedMultiplier, 0f, 1f);
			}
			else
			{
				sinkingValue = Mathf.Clamp(sinkingValue - Time.deltaTime * 0.75f, 0f, 1f);
			}
			if (sinkingValue > 0.73f || isUnderwater)
			{
				if (!wasUnderwaterLastFrame)
				{
					wasUnderwaterLastFrame = true;
					if (!base.IsOwner)
					{
						waterBubblesAudio.Play();
					}
				}
				voiceMuffledByEnemy = true;
				if (!base.IsOwner)
				{
					statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 0f, 4f * Time.deltaTime);
					if (currentVoiceChatIngameSettings != null)
					{
						OccludeAudio component2 = currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>();
						component2.overridingLowPass = true;
						component2.lowPassOverride = 600f;
						waterBubblesAudio.volume = Mathf.Clamp(currentVoiceChatIngameSettings._playerState.Amplitude * 120f, 0f, 1f);
					}
					else
					{
						StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
					}
				}
				else if (sinkingValue > 0.73f)
				{
					HUDManager.Instance.sinkingCoveredFace = true;
				}
			}
			else if (base.IsOwner)
			{
				HUDManager.Instance.sinkingCoveredFace = false;
			}
			else if (wasUnderwaterLastFrame)
			{
				waterBubblesAudio.Stop();
				if (currentVoiceChatIngameSettings != null)
				{
					wasUnderwaterLastFrame = false;
					currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
					voiceMuffledByEnemy = false;
				}
				else
				{
					StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
					StartOfRound.Instance.UpdatePlayerVoiceEffects();
				}
			}
			else
			{
				statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 1f, 4f * Time.deltaTime);
			}
			if (activeAudioReverbFilter == null)
			{
				activeAudioReverbFilter = activeAudioListener.GetComponent<AudioReverbFilter>();
				activeAudioReverbFilter.enabled = true;
			}
			if (reverbPreset != null && GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null && ((GameNetworkManager.Instance.localPlayerController == this && (!isPlayerDead || StartOfRound.Instance.overrideSpectateCamera)) || (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == this && !StartOfRound.Instance.overrideSpectateCamera)))
			{
				activeAudioReverbFilter.dryLevel = Mathf.Lerp(activeAudioReverbFilter.dryLevel, reverbPreset.dryLevel, 15f * Time.deltaTime);
				activeAudioReverbFilter.roomLF = Mathf.Lerp(activeAudioReverbFilter.roomLF, reverbPreset.lowFreq, 15f * Time.deltaTime);
				activeAudioReverbFilter.roomHF = Mathf.Lerp(activeAudioReverbFilter.roomHF, reverbPreset.highFreq, 15f * Time.deltaTime);
				activeAudioReverbFilter.decayTime = Mathf.Lerp(activeAudioReverbFilter.decayTime, reverbPreset.decayTime, 15f * Time.deltaTime);
				activeAudioReverbFilter.room = Mathf.Lerp(activeAudioReverbFilter.room, reverbPreset.room, 15f * Time.deltaTime);
				SoundManager.Instance.SetEchoFilter(reverbPreset.hasEcho);
			}
			if (isHoldingObject || isGrabbingObjectAnimation || inShockingMinigame)
			{
				upperBodyAnimationsWeight = Mathf.Lerp(upperBodyAnimationsWeight, 1f, 25f * Time.deltaTime);
				playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsRightHand"), upperBodyAnimationsWeight);
				if (twoHandedAnimation || inShockingMinigame)
				{
					playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), upperBodyAnimationsWeight);
				}
				else
				{
					playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), Mathf.Abs(upperBodyAnimationsWeight - 1f));
				}
			}
			else
			{
				upperBodyAnimationsWeight = Mathf.Lerp(upperBodyAnimationsWeight, 0f, 25f * Time.deltaTime);
				playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsRightHand"), upperBodyAnimationsWeight);
				playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("HoldingItemsBothHands"), upperBodyAnimationsWeight);
			}
			playerBodyAnimator.SetLayerWeight(playerBodyAnimator.GetLayerIndex("SpecialAnimations"), specialAnimationWeight);
			if (inSpecialInteractAnimation && !inShockingMinigame)
			{
				cameraLookRig1.weight = Mathf.Lerp(cameraLookRig1.weight, 0f, Time.deltaTime * 25f);
				cameraLookRig2.weight = Mathf.Lerp(cameraLookRig1.weight, 0f, Time.deltaTime * 25f);
			}
			else
			{
				cameraLookRig1.weight = 0.45f;
				cameraLookRig2.weight = 1f;
			}
			if (inVehicleAnimation)
			{
				cameraLookRig1.weight = 0.33f;
				cameraLookRig2.weight = 1f;
				leftArmRigSecondary.weight = 1f;
				rightArmRigSecondary.weight = 1f;
				leftArmRig.weight = 0f;
				rightArmRig.weight = 0f;
			}
			else
			{
				leftArmRigSecondary.weight = 0f;
				rightArmRigSecondary.weight = 0f;
				leftArmRig.weight = 1f;
				rightArmRig.weight = 1f;
			}
			if (isExhausted)
			{
				exhaustionEffectLerp = Mathf.Lerp(exhaustionEffectLerp, 1f, 10f * Time.deltaTime);
			}
			else
			{
				exhaustionEffectLerp = Mathf.Lerp(exhaustionEffectLerp, 0f, 10f * Time.deltaTime);
			}
			playerBodyAnimator.SetFloat("tiredAmount", exhaustionEffectLerp);
			if (isPlayerDead)
			{
				drunkness = 0f;
				drunknessInertia = 0f;
			}
			else
			{
				drunkness = Mathf.Clamp(drunkness + Time.deltaTime / 12f * drunknessSpeed * drunknessInertia, 0f, 1f);
				if (!increasingDrunknessThisFrame)
				{
					if (drunkness > 0f)
					{
						drunknessInertia = Mathf.Clamp(drunknessInertia - Time.deltaTime / 3f * drunknessSpeed / Mathf.Clamp(Mathf.Abs(drunknessInertia), 0.2f, 1f), -2.5f, 2.5f);
					}
					else
					{
						drunknessInertia = 0f;
					}
				}
				else
				{
					increasingDrunknessThisFrame = false;
				}
				float num11 = StartOfRound.Instance.drunknessSideEffect.Evaluate(drunkness);
				if (num11 > 0.15f)
				{
					SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f + num11;
				}
				else
				{
					SoundManager.Instance.playerVoicePitchTargets[playerClientId] = 1f;
				}
			}
			smoothLookMultiplier = 25f * Mathf.Clamp(Mathf.Abs(drunkness - 1.5f), 0.15f, 1f);
			if (bleedingHeavily && bloodDropTimer >= 0f)
			{
				bloodDropTimer -= Time.deltaTime;
			}
			if (Physics.Raycast(lineOfSightCube.position, lineOfSightCube.forward, out hit, 10f, playersManager.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
			{
				lineOfSightCube.localScale = new Vector3(1.5f, 1.5f, hit.distance);
			}
			else
			{
				lineOfSightCube.localScale = new Vector3(1.5f, 1.5f, 10f);
			}
			SetPlayerSanityLevel();
		}

		private void SetFaceUnderwaterFilters()
		{
			if (isPlayerDead)
			{
				return;
			}
			if (isUnderwater && underwaterCollider != null && underwaterCollider.bounds.Contains(gameplayCamera.transform.position))
			{
				HUDManager.Instance.setUnderwaterFilter = true;
				statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 0f, 4f * Time.deltaTime);
				StartOfRound.Instance.drowningTimer -= Time.deltaTime / 10f;
				if (StartOfRound.Instance.drowningTimer < 0f)
				{
					StartOfRound.Instance.drowningTimer = 1f;
					KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Drowning);
				}
				else if (StartOfRound.Instance.drowningTimer <= 0.3f)
				{
					if (!StartOfRound.Instance.playedDrowningSFX)
					{
						StartOfRound.Instance.playedDrowningSFX = true;
						HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.HUDSystemAlertSFX);
					}
					HUDManager.Instance.DisplayStatusEffect("Oxygen critically low!");
				}
			}
			else
			{
				statusEffectAudio.volume = Mathf.Lerp(statusEffectAudio.volume, 1f, 4f * Time.deltaTime);
				StartOfRound.Instance.playedDrowningSFX = false;
				StartOfRound.Instance.drowningTimer = Mathf.Clamp(StartOfRound.Instance.drowningTimer + Time.deltaTime, 0.1f, 1f);
				HUDManager.Instance.setUnderwaterFilter = false;
			}
			if (syncUnderwaterInterval <= 0f)
			{
				if (HUDManager.Instance.setUnderwaterFilter)
				{
					if (!isFaceUnderwaterOnServer)
					{
						isFaceUnderwaterOnServer = true;
						SetFaceUnderwaterServerRpc();
					}
				}
				else if (isFaceUnderwaterOnServer)
				{
					isFaceUnderwaterOnServer = false;
					SetFaceOutOfWaterServerRpc();
				}
			}
			else
			{
				syncUnderwaterInterval = 0.5f;
			}
		}

		[ServerRpc]
		private void SetFaceUnderwaterServerRpc()
{			{
				SetFaceUnderwaterClientRpc();
			}
}
		[ClientRpc]
		private void SetFaceUnderwaterClientRpc()
{if(!base.IsOwner)				{
					isUnderwater = true;
				}
}
		[ServerRpc]
		private void SetFaceOutOfWaterServerRpc()
{			{
				SetFaceOutOfWaterClientRpc();
			}
}
		[ClientRpc]
		private void SetFaceOutOfWaterClientRpc()
{if(!base.IsOwner)				{
					isUnderwater = false;
				}
}
		public void IncreaseFearLevelOverTime(float amountMultiplier = 1f, float cap = 1f)
		{
			playersManager.fearLevelIncreasing = true;
			if (!(playersManager.fearLevel > cap))
			{
				playersManager.fearLevel += Time.deltaTime * amountMultiplier;
				if (playersManager.fearLevel > 0.6f && timeSinceFearLevelUp > 8f)
				{
					timeSinceFearLevelUp = 0f;
				}
			}
		}

		public void JumpToFearLevel(float targetFearLevel, bool onlyGoUp = true)
		{
			if (!onlyGoUp || !(targetFearLevel - playersManager.fearLevel < 0.05f))
			{
				playersManager.fearLevel = targetFearLevel;
				playersManager.fearLevelIncreasing = true;
				if (timeSinceFearLevelUp > 8f)
				{
					timeSinceFearLevelUp = 0f;
				}
			}
		}

		private void SetPlayerSanityLevel()
		{
			if (StartOfRound.Instance.inShipPhase || !TimeOfDay.Instance.currentDayTimeStarted)
			{
				insanityLevel = 0f;
				return;
			}
			if (!NearOtherPlayers(this, 17f) && !PlayerIsHearingOthersThroughWalkieTalkie(this))
			{
				if (isInsideFactory)
				{
					insanitySpeedMultiplier = 0.8f;
				}
				else if (isInHangarShipRoom)
				{
					insanitySpeedMultiplier = 0.2f;
				}
				else if (StartOfRound.Instance.connectedPlayersAmount == 0)
				{
					insanitySpeedMultiplier = -2f;
				}
				else if (TimeOfDay.Instance.dayMode >= DayMode.Sundown)
				{
					insanitySpeedMultiplier = 0.5f;
				}
				else
				{
					insanitySpeedMultiplier = 0.3f;
				}
				isPlayerAlone = true;
			}
			else
			{
				insanitySpeedMultiplier = -3f;
				isPlayerAlone = false;
			}
			if (insanitySpeedMultiplier < 0f)
			{
				insanityLevel = Mathf.MoveTowards(insanityLevel, 0f, Time.deltaTime * (0f - insanitySpeedMultiplier));
				return;
			}
			if (insanityLevel > maxInsanityLevel)
			{
				insanityLevel = Mathf.MoveTowards(insanityLevel, maxInsanityLevel, Time.deltaTime * 2f);
				return;
			}
			if (StartOfRound.Instance.connectedPlayersAmount == 0)
			{
				insanitySpeedMultiplier /= 2f;
			}
			insanityLevel = Mathf.MoveTowards(insanityLevel, maxInsanityLevel, Time.deltaTime * insanitySpeedMultiplier * 0.6f);
		}

		private void SetNightVisionEnabled(bool isNotLocalClient)
		{
			nightVision.enabled = false;
			if (!(GameNetworkManager.Instance == null) && !(GameNetworkManager.Instance.localPlayerController == null) && (!isNotLocalClient || GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript == this) && isInsideFactory)
			{
				nightVision.enabled = true;
			}
		}

		public void ClickHoldInteraction()
		{
			if (!(isHoldingInteract = IngamePlayerSettings.Instance.playerInput.actions.FindAction("Interact").IsPressed()))
			{
				StopHoldInteractionOnTrigger();
			}
			else if (hoveringOverTrigger == null || !hoveringOverTrigger.interactable)
			{
				StopHoldInteractionOnTrigger();
			}
			else if (hoveringOverTrigger == null || !hoveringOverTrigger.gameObject.activeInHierarchy || !hoveringOverTrigger.holdInteraction || hoveringOverTrigger.currentCooldownValue > 0f || (isHoldingObject && !hoveringOverTrigger.oneHandedItemAllowed) || (twoHanded && !hoveringOverTrigger.twoHandedItemAllowed))
			{
				StopHoldInteractionOnTrigger();
			}
			else if (isGrabbingObjectAnimation || isTypingChat || (inSpecialInteractAnimation && !hoveringOverTrigger.allowUseWhileInAnimation) || throwingObject)
			{
				StopHoldInteractionOnTrigger();
			}
			else if (!HUDManager.Instance.HoldInteractionFill(hoveringOverTrigger.timeToHold, hoveringOverTrigger.timeToHoldSpeedMultiplier))
			{
				hoveringOverTrigger.HoldInteractNotFilled();
			}
			else
			{
				hoveringOverTrigger.Interact(thisPlayerBody);
			}
		}

		private void StopHoldInteractionOnTrigger()
		{
			HUDManager.Instance.holdFillAmount = 0f;
			if (previousHoveringOverTrigger != null)
			{
				previousHoveringOverTrigger.StopInteraction();
			}
			if (hoveringOverTrigger != null)
			{
				hoveringOverTrigger.StopInteraction();
			}
		}

		public void CancelSpecialTriggerAnimations()
		{
			Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
			if (terminal.terminalInUse)
			{
				terminal.QuitTerminal();
			}
			else if (currentTriggerInAnimationWith != null)
			{
				currentTriggerInAnimationWith.StopSpecialAnimation();
			}
		}

		public void TeleportPlayer(Vector3 pos, bool withRotation = false, float rot = 0f, bool allowInteractTrigger = false, bool enableController = true)
		{
			Debug.Log("Called teleport function on " + base.gameObject.name);
			if (base.IsOwner && !allowInteractTrigger)
			{
				CancelSpecialTriggerAnimations();
			}
			else if (!allowInteractTrigger && currentTriggerInAnimationWith != null)
			{
				currentTriggerInAnimationWith.onCancelAnimation.Invoke(this);
				currentTriggerInAnimationWith.SetInteractTriggerNotInAnimation();
			}
			if ((bool)inAnimationWithEnemy)
			{
				inAnimationWithEnemy.CancelSpecialAnimationWithPlayer();
			}
			StartOfRound.Instance.playerTeleportedEvent.Invoke(this);
			if (withRotation)
			{
				targetYRot = rot;
				base.transform.localEulerAngles = new Vector3(0f, targetYRot, 0f);
			}
			serverPlayerPosition = pos;
			thisController.enabled = false;
			base.transform.position = pos;
			if (enableController)
			{
				thisController.enabled = true;
			}
			teleportingThisFrame = true;
			teleportedLastFrame = true;
			timeSinceTakingGravityDamage = 1f;
			averageVelocity = 0f;
			if (!isUnderwater && !isSinking)
			{
				return;
			}
			Debug.Log("Player is sinking; disable all quicksand locally");
			QuicksandTrigger[] array = UnityEngine.Object.FindObjectsByType<QuicksandTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].sinkingLocalPlayer)
				{
					array[i].OnExit(base.gameObject.GetComponent<Collider>());
					break;
				}
			}
		}

		public void KillPlayer(Vector3 bodyVelocity, bool spawnBody = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, Vector3 positionOffset = default(Vector3))
		{
			if (base.IsOwner && !isPlayerDead && AllowPlayerDeath())
			{
				isPlayerDead = true;
				isPlayerControlled = false;
				thisPlayerModelArms.enabled = false;
				localVisor.position = playersManager.notSpawnedPosition.position;
				DisablePlayerModel(base.gameObject);
				isInsideFactory = false;
				IsInspectingItem = false;
				inTerminalMenu = false;
				twoHanded = false;
				carryWeight = 1f;
				fallValue = 0f;
				fallValueUncapped = 0f;
				takingFallDamage = false;
				isSinking = false;
				isUnderwater = false;
				StartOfRound.Instance.drowningTimer = 1f;
				HUDManager.Instance.setUnderwaterFilter = false;
				wasUnderwaterLastFrame = false;
				sourcesCausingSinking = 0;
				sinkingValue = 0f;
				hinderedMultiplier = 1f;
				isMovementHindered = 0;
				inAnimationWithEnemy = null;
				positionOfDeath = base.transform.position;
				if (spawnBody)
				{
					Debug.DrawRay(base.transform.position, base.transform.up * 3f, Color.red, 10f);
					SpawnDeadBody((int)playerClientId, bodyVelocity, (int)causeOfDeath, this, deathAnimation, null, positionOffset);
				}
				SetInSpecialMenu(setInMenu: false);
				physicsParent = null;
				overridePhysicsParent = null;
				lastSyncedPhysicsParent = null;
				StartOfRound.Instance.CurrentPlayerPhysicsRegions.Clear();
				base.transform.SetParent(playersManager.playersContainer);
				CancelSpecialTriggerAnimations();
				ChangeAudioListenerToObject(playersManager.spectateCamera.gameObject);
				SoundManager.Instance.SetDiageticMixerSnapshot();
				HUDManager.Instance.SetNearDepthOfFieldEnabled(enabled: true);
				HUDManager.Instance.HUDAnimator.SetBool("biohazardDamage", value: false);
				Debug.Log("Running kill player function for LOCAL client, player object: " + base.gameObject.name);
				HUDManager.Instance.gameOverAnimator.SetTrigger("gameOver");
				HUDManager.Instance.HideHUD(hide: true);
				StopHoldInteractionOnTrigger();
				KillPlayerServerRpc((int)playerClientId, spawnBody, bodyVelocity, (int)causeOfDeath, deathAnimation, positionOffset);
				StartOfRound.Instance.SwitchCamera(StartOfRound.Instance.spectateCamera);
				isInGameOverAnimation = 1.5f;
				cursorTip.text = "";
				cursorIcon.enabled = false;
				DropAllHeldItems(spawnBody);
				DisableJetpackControlsLocally();
			}
		}

		[ServerRpc]
		private void KillPlayerServerRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation, Vector3 positionOffset)
{			playersManager.livingPlayers--;
			if (playersManager.livingPlayers == 0)
			{
				playersManager.allPlayersDead = true;
				playersManager.ShipLeaveAutomatically();
			}
			base.transform.SetParent(playersManager.playersContainer);
			if (!spawnBody)
			{
				PlayerControllerB component = playersManager.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
				for (int i = 0; i < component.ItemSlots.Length; i++)
				{
					GrabbableObject grabbableObject = component.ItemSlots[i];
					if (grabbableObject != null)
					{
						grabbableObject.gameObject.GetComponent<NetworkObject>().Despawn();
					}
				}
			}
			else if (deathAnimation != 9)
			{
				GameObject obj = UnityEngine.Object.Instantiate(StartOfRound.Instance.ragdollGrabbableObjectPrefab, playersManager.propsContainer);
				obj.GetComponent<NetworkObject>().Spawn();
				obj.GetComponent<RagdollGrabbableObject>().bodyID.Value = playerId;
			}
			KillPlayerClientRpc(playerId, spawnBody, bodyVelocity, causeOfDeath, deathAnimation, positionOffset);
}
		[ClientRpc]
		private void KillPlayerClientRpc(int playerId, bool spawnBody, Vector3 bodyVelocity, int causeOfDeath, int deathAnimation, Vector3 positionOffset)
{			StartOfRound.Instance.gameStats.deaths++;
			Debug.Log("A player died. player object: " + base.gameObject.name);
			if (!base.IsServer)
			{
				Debug.Log("Setting living players minus one.");
				playersManager.livingPlayers--;
				Debug.Log(playersManager.livingPlayers);
				if (playersManager.livingPlayers == 0)
				{
					playersManager.allPlayersDead = true;
					playersManager.ShipLeaveAutomatically();
				}
			}
			PlayerControllerB component = playersManager.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
			component.physicsParent = null;
			component.overridePhysicsParent = null;
			component.lastSyncedPhysicsParent = null;
			component.bleedingHeavily = false;
			statusEffectAudio.Stop();
			if (!base.IsOwner && spawnBody)
			{
				SpawnDeadBody(playerId, bodyVelocity, causeOfDeath, component, deathAnimation, null, positionOffset);
				DropAllHeldItems(spawnBody);
			}
			placeOfDeath = component.transform.position;
			DisablePlayerModel(playersManager.allPlayerObjects[playerId]);
			component.setPositionOfDeadPlayer = true;
			component.isPlayerDead = true;
			component.isPlayerControlled = false;
			component.snapToServerPosition = false;
			component.isUnderwater = false;
			component.isHoldingObject = false;
			component.currentlyHeldObjectServer = null;
			component.transform.SetParent(playersManager.playersContainer);
			SoundManager.Instance.playerVoicePitchTargets[playerId] = 1f;
			SoundManager.Instance.playerVoicePitchLerpSpeed[playerId] = 3f;
			component.causeOfDeath = (CauseOfDeath)causeOfDeath;
			if (!base.IsOwner && GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				HUDManager.Instance.UpdateBoxesSpectateUI();
			}
			StartOfRound.Instance.UpdatePlayerVoiceEffects();
}
		public void SpawnDeadBody(int playerId, Vector3 bodyVelocity, int causeOfDeath, PlayerControllerB deadPlayerController, int deathAnimation = 0, Transform overridePosition = null, Vector3 positionOffset = default(Vector3))
		{
			float num = 1.32f;
			if (positionOffset != Vector3.zero)
			{
				num = 0f;
			}
			Transform parent = null;
			if (deathAnimation == 9)
			{
				if (RoundManager.Instance.mapPropsContainer != null)
				{
					parent = RoundManager.Instance.mapPropsContainer.transform;
				}
			}
			else if (isInElevator)
			{
				parent = playersManager.elevatorTransform;
			}
			GameObject gameObject = ((!(overridePosition != null)) ? UnityEngine.Object.Instantiate(playersManager.playerRagdolls[deathAnimation], deadPlayerController.thisPlayerBody.position + Vector3.up * num + positionOffset, deadPlayerController.thisPlayerBody.rotation, parent) : UnityEngine.Object.Instantiate(playersManager.playerRagdolls[deathAnimation], overridePosition.position + Vector3.up * num + positionOffset, overridePosition.rotation, parent));
			Debug.DrawRay(gameObject.transform.position, gameObject.transform.up * 2f, Color.green, 10f);
			DeadBodyInfo component = gameObject.GetComponent<DeadBodyInfo>();
			if (component != null)
			{
				if ((bool)overridePosition)
				{
					component.overrideSpawnPosition = true;
				}
				if (deadPlayerController.physicsParent != null)
				{
					component.SetPhysicsParent(deadPlayerController.physicsParent);
				}
				component.parentedToShip = isInElevator;
				component.playerObjectId = playerId;
				deadBody = component;
			}
			Rigidbody[] componentsInChildren = gameObject.GetComponentsInChildren<Rigidbody>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].velocity = bodyVelocity;
			}
			if (deadBody != null)
			{
				for (int j = 0; j < bodyBloodDecals.Length; j++)
				{
					deadBody.bodyBloodDecals[j].SetActive(bodyBloodDecals[j].activeSelf);
				}
			}
			CauseOfDeath causeOfDeath2 = (CauseOfDeath)causeOfDeath;
			if (component != null)
			{
				ScanNodeProperties componentInChildren = component.gameObject.GetComponentInChildren<ScanNodeProperties>();
				componentInChildren.headerText = "Body of " + deadPlayerController.playerUsername;
				componentInChildren.subText = "Cause of death: " + causeOfDeath2;
			}
			if (deadBody != null)
			{
				deadBody.causeOfDeath = causeOfDeath2;
				if (causeOfDeath2 == CauseOfDeath.Bludgeoning || causeOfDeath2 == CauseOfDeath.Mauling || causeOfDeath2 == CauseOfDeath.Gunshots)
				{
					deadBody.MakeCorpseBloody();
				}
				if (causeOfDeath2 == CauseOfDeath.Gravity)
				{
					deadBody.bodyAudio.PlayOneShot(StartOfRound.Instance.playerFallDeath);
					WalkieTalkie.TransmitOneShotAudio(deadBody.bodyAudio, StartOfRound.Instance.playerFallDeath);
				}
			}
		}

		public void DestroyItemInSlotAndSync(int itemSlot)
		{
			if (base.IsOwner)
			{
				if (itemSlot >= ItemSlots.Length || ItemSlots[itemSlot] == null)
				{
					Debug.LogError($"Destroy item in slot called for a slot (slot {itemSlot}) which is empty or incorrect");
				}
				timeSinceSwitchingSlots = 0f;
				DestroyItemInSlot(itemSlot);
				DestroyItemInSlotServerRpc(itemSlot);
			}
		}

		[ServerRpc]
		public void DestroyItemInSlotServerRpc(int itemSlot)
{			{
				DestroyItemInSlotClientRpc(itemSlot);
			}
}
		[ClientRpc]
		public void DestroyItemInSlotClientRpc(int itemSlot)
{if(!base.IsOwner)				{
					DestroyItemInSlot(itemSlot);
				}
}
		public void DestroyItemInSlot(int itemSlot)
		{
			if (GameNetworkManager.Instance.localPlayerController == null || NetworkManager.Singleton == null || NetworkManager.Singleton.ShutdownInProgress)
			{
				return;
			}
			Debug.Log($"Destroying item in slot {itemSlot}; {currentItemSlot}; is currentlyheldobjectserver null: {currentlyHeldObjectServer == null}");
			if (currentlyHeldObjectServer != null)
			{
				Debug.Log("currentlyHeldObjectServer: " + currentlyHeldObjectServer.itemProperties.itemName);
			}
			GrabbableObject grabbableObject = ItemSlots[itemSlot];
			if (isHoldingObject)
			{
				if (currentItemSlot == itemSlot)
				{
					carryWeight = Mathf.Clamp(carryWeight - (currentlyHeldObjectServer.itemProperties.weight - 1f), 1f, 10f);
					isHoldingObject = false;
					twoHanded = false;
					if (base.IsOwner)
					{
						playerBodyAnimator.SetBool("cancelHolding", value: true);
						playerBodyAnimator.SetTrigger("Throw");
						HUDManager.Instance.holdingTwoHandedItem.enabled = false;
						HUDManager.Instance.ClearControlTips();
						activatingItem = false;
					}
				}
				if (base.IsOwner)
				{
					HUDManager.Instance.itemSlotIcons[itemSlot].enabled = false;
				}
				if (currentlyHeldObjectServer != null && currentlyHeldObjectServer == ItemSlots[itemSlot])
				{
					if (base.IsOwner)
					{
						SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
						currentlyHeldObjectServer.DiscardItemOnClient();
					}
					currentlyHeldObjectServer = null;
				}
			}
			ItemSlots[itemSlot] = null;
			if (base.IsServer)
			{
				grabbableObject.NetworkObject.Despawn();
			}
		}

		public void DropAllHeldItems(bool itemsFall = true, bool disconnecting = false)
		{
			for (int i = 0; i < ItemSlots.Length; i++)
			{
				GrabbableObject grabbableObject = ItemSlots[i];
				if (!(grabbableObject != null))
				{
					continue;
				}
				if (itemsFall)
				{
					grabbableObject.parentObject = null;
					grabbableObject.heldByPlayerOnServer = false;
					if (isInElevator)
					{
						grabbableObject.transform.SetParent(playersManager.elevatorTransform, worldPositionStays: true);
					}
					else
					{
						grabbableObject.transform.SetParent(playersManager.propsContainer, worldPositionStays: true);
					}
					SetItemInElevator(isInHangarShipRoom, isInElevator, grabbableObject);
					grabbableObject.EnablePhysics(enable: true);
					grabbableObject.EnableItemMeshes(enable: true);
					grabbableObject.transform.localScale = grabbableObject.originalScale;
					grabbableObject.isHeld = false;
					grabbableObject.isPocketed = false;
					Debug.Log($"Drop all held items A {grabbableObject.startFallingPosition}; item parent: {grabbableObject.transform.parent}; {grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position)}; {grabbableObject.transform.position}");
					Debug.DrawRay(grabbableObject.transform.position, Vector3.up, Color.red, 25f);
					grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(grabbableObject.transform.position);
					grabbableObject.FallToGround(randomizePosition: true);
					grabbableObject.fallTime = UnityEngine.Random.Range(-0.3f, 0.05f);
					if (base.IsOwner)
					{
						grabbableObject.DiscardItemOnClient();
					}
					else if (!grabbableObject.itemProperties.syncDiscardFunction)
					{
						grabbableObject.playerHeldBy = null;
					}
				}
				if (base.IsOwner && !disconnecting)
				{
					HUDManager.Instance.holdingTwoHandedItem.enabled = false;
					HUDManager.Instance.itemSlotIcons[i].enabled = false;
					HUDManager.Instance.ClearControlTips();
					activatingItem = false;
				}
				ItemSlots[i] = null;
			}
			if (isHoldingObject)
			{
				isHoldingObject = false;
				if (currentlyHeldObjectServer != null)
				{
					SetSpecialGrabAnimationBool(setTrue: false, currentlyHeldObjectServer);
				}
				playerBodyAnimator.SetBool("cancelHolding", value: true);
				playerBodyAnimator.SetTrigger("Throw");
			}
			activatingItem = false;
			twoHanded = false;
			carryWeight = 1f;
			currentlyHeldObjectServer = null;
		}

		public void DropAllHeldItemsAndSync()
		{
			DropAllHeldItems();
			DropAllHeldItemsServerRpc();
		}

		[ServerRpc(RequireOwnership = false)]
		public void DropAllHeldItemsServerRpc()
				{
					DropAllHeldItemsClientRpc();
				}

		[ClientRpc]
		public void DropAllHeldItemsClientRpc()
				{
					DropAllHeldItems();
				}

		private bool NearOtherPlayers(PlayerControllerB playerScript = null, float checkRadius = 10f)
		{
			if (playerScript == null)
			{
				playerScript = this;
			}
			base.gameObject.layer = 0;
			bool result = Physics.CheckSphere(playerScript.transform.position, checkRadius, 8, QueryTriggerInteraction.Ignore);
			base.gameObject.layer = 3;
			return result;
		}

		private bool PlayerIsHearingOthersThroughWalkieTalkie(PlayerControllerB playerScript = null)
		{
			if (playerScript == null)
			{
				playerScript = this;
			}
			if (!playerScript.holdingWalkieTalkie)
			{
				return false;
			}
			for (int i = 0; i < WalkieTalkie.allWalkieTalkies.Count; i++)
			{
				if (WalkieTalkie.allWalkieTalkies[i].clientIsHoldingAndSpeakingIntoThis && WalkieTalkie.allWalkieTalkies[i] != playerScript.currentlyHeldObjectServer as WalkieTalkie)
				{
					return true;
				}
			}
			return false;
		}

		public void DisablePlayerModel(GameObject playerObject, bool enable = false, bool disableLocalArms = false)
		{
			SkinnedMeshRenderer[] componentsInChildren = playerObject.GetComponentsInChildren<SkinnedMeshRenderer>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].enabled = enable;
			}
			if (disableLocalArms)
			{
				thisPlayerModelArms.enabled = false;
			}
		}

		public void SyncBodyPositionWithClients()
		{
			if (deadBody != null)
			{
				SyncBodyPositionClientRpc(deadBody.transform.position);
			}
		}

		[ClientRpc]
		public void SyncBodyPositionClientRpc(Vector3 newBodyPosition)
{if(!(Vector3.Distance(deadBody.transform.position, newBodyPosition) < 1.5f))				{
					StartCoroutine(WaitUntilPlayerHasLeftBodyToTeleport(newBodyPosition));
				}
}
		private IEnumerator WaitUntilPlayerHasLeftBodyToTeleport(Vector3 newBodyPosition)
		{
			yield return new WaitUntil(() => deadBody == null || !Physics.CheckSphere(deadBody.transform.position, 8f, playersManager.playersMask));
			if (!(deadBody == null))
			{
				deadBody.SetRagdollPositionSafely(newBodyPosition);
			}
		}

		private void LateUpdate()
		{
			if (isFirstFrameLateUpdate)
			{
				isFirstFrameLateUpdate = false;
				previousElevatorPosition = playersManager.elevatorTransform.position;
			}
			else if (base.IsOwner && isPlayerControlled && (!base.IsServer || isHostPlayerObject))
			{
				int num = 0;
				Transform transform = null;
				NetworkObject networkObject = null;
				for (int i = 0; i < StartOfRound.Instance.CurrentPlayerPhysicsRegions.Count; i++)
				{
					if (StartOfRound.Instance.CurrentPlayerPhysicsRegions[i].priority > num)
					{
						num = StartOfRound.Instance.CurrentPlayerPhysicsRegions[i].priority;
						transform = StartOfRound.Instance.CurrentPlayerPhysicsRegions[i].physicsTransform;
						networkObject = StartOfRound.Instance.CurrentPlayerPhysicsRegions[i].parentNetworkObject;
					}
				}
				if (isInElevator && num <= 0)
				{
					transform = null;
				}
				physicsParent = transform;
				if (overridePhysicsParent != null)
				{
					if (overridePhysicsParent != lastSyncedPhysicsParent)
					{
						parentedToElevatorLastFrame = false;
						lastSyncedPhysicsParent = overridePhysicsParent;
						base.transform.SetParent(overridePhysicsParent);
						UpdatePlayerPhysicsParentServerRpc(thisPlayerBody.localPosition, overridePhysicsParent.GetComponent<NetworkObject>(), isOverride: true, isInElevator, isInHangarShipRoom);
					}
				}
				else if (physicsParent != null && networkObject != null)
				{
					if (physicsParent != lastSyncedPhysicsParent)
					{
						parentedToElevatorLastFrame = false;
						lastSyncedPhysicsParent = physicsParent;
						base.transform.SetParent(physicsParent);
						UpdatePlayerPhysicsParentServerRpc(thisPlayerBody.localPosition, networkObject.GetComponent<NetworkObject>(), isOverride: false, isInElevator, isInHangarShipRoom);
					}
				}
				else
				{
					if (lastSyncedPhysicsParent != null)
					{
						lastSyncedPhysicsParent = null;
						base.transform.SetParent(playersManager.playersContainer);
						RemovePlayerPhysicsParentServerRpc(thisPlayerBody.localPosition, removeOverride: false, removeBoth: true, isInElevator, isInHangarShipRoom);
					}
					if (isInElevator)
					{
						if (!parentedToElevatorLastFrame)
						{
							parentedToElevatorLastFrame = true;
							base.transform.SetParent(playersManager.elevatorTransform);
						}
					}
					else if (parentedToElevatorLastFrame)
					{
						parentedToElevatorLastFrame = false;
						base.transform.SetParent(playersManager.playersContainer);
					}
				}
			}
			previousElevatorPosition = playersManager.elevatorTransform.position;
			if (!isTestingPlayer)
			{
				if (NetworkManager.Singleton == null)
				{
					return;
				}
				if (!base.IsOwner && usernameAlpha.alpha >= 0f && GameNetworkManager.Instance.localPlayerController != null)
				{
					usernameAlpha.alpha -= Time.deltaTime;
					usernameBillboard.LookAt(GameNetworkManager.Instance.localPlayerController.localVisorTargetPoint);
				}
				else if (usernameCanvas.gameObject.activeSelf)
				{
					usernameCanvas.gameObject.SetActive(value: false);
				}
			}
			if (base.IsOwner && (!base.IsServer || isHostPlayerObject))
			{
				PlayerLookInput();
				if (RoundManager.Instance.indoorFog.gameObject.activeSelf)
				{
					RoundManager.Instance.indoorFog.transform.position = StartOfRound.Instance.audioListener.transform.position;
					PlayerControllerB playerControllerB = this;
					if (isPlayerDead && spectatedPlayerScript != null)
					{
						playerControllerB = spectatedPlayerScript;
					}
					RoundManager.Instance.indoorFog.enabled = playerControllerB.isInsideFactory;
				}
				if (isPlayerControlled && !isPlayerDead)
				{
					if (GameNetworkManager.Instance != null)
					{
						float num2 = 0.14f;
						num2 = (inSpecialInteractAnimation ? 0.06f : ((!NearOtherPlayers(this)) ? 0.24f : 0.1f));
						if ((oldPlayerPosition - base.transform.localPosition).sqrMagnitude > num2 || updatePositionForNewlyJoinedClient)
						{
							updatePositionForNewlyJoinedClient = false;
							if (!playersManager.newGameIsLoading)
							{
								UpdatePlayerPositionServerRpc(thisPlayerBody.localPosition, isInElevator, isInHangarShipRoom, isExhausted, thisController.isGrounded);
								oldPlayerPosition = base.transform.localPosition;
							}
						}
						if (currentlyHeldObjectServer != null && isHoldingObject && grabbedObjectValidated)
						{
							currentlyHeldObjectServer.transform.localPosition = currentlyHeldObjectServer.itemProperties.positionOffset;
							currentlyHeldObjectServer.transform.localEulerAngles = currentlyHeldObjectServer.itemProperties.rotationOffset;
						}
					}
					localVisor.position = localVisorTargetPoint.position;
					localVisor.rotation = Quaternion.Lerp(localVisor.rotation, localVisorTargetPoint.rotation, 53f * Mathf.Clamp(Time.deltaTime, 0.0167f, 20f));
					float num3 = 1f;
					if (drunkness > 0.02f)
					{
						num3 *= Mathf.Abs(StartOfRound.Instance.drunknessSpeedEffect.Evaluate(drunkness) - 1.25f);
					}
					if (isSprinting)
					{
						sprintMeter = Mathf.Clamp(sprintMeter - Time.deltaTime / sprintTime * carryWeight * num3, 0f, 1f);
					}
					else if (isMovementHindered > 0)
					{
						if (isWalking)
						{
							sprintMeter = Mathf.Clamp(sprintMeter - Time.deltaTime / sprintTime * num3 * 0.5f, 0f, 1f);
						}
					}
					else
					{
						if (!isWalking)
						{
							sprintMeter = Mathf.Clamp(sprintMeter + Time.deltaTime / (sprintTime + 4f) * num3, 0f, 1f);
						}
						else
						{
							sprintMeter = Mathf.Clamp(sprintMeter + Time.deltaTime / (sprintTime + 9f) * num3, 0f, 1f);
						}
						if (isExhausted && sprintMeter > 0.2f)
						{
							isExhausted = false;
						}
					}
					sprintMeterUI.fillAmount = sprintMeter;
					float num4;
					if (isHoldingObject && currentlyHeldObjectServer != null && currentlyHeldObjectServer.itemProperties.requiresBattery)
					{
						HUDManager.Instance.batteryMeter.fillAmount = currentlyHeldObjectServer.insertedBattery.charge / 1.3f;
						HUDManager.Instance.batteryMeter.gameObject.SetActive(value: true);
						HUDManager.Instance.batteryIcon.enabled = true;
						num4 = currentlyHeldObjectServer.insertedBattery.charge / 1.3f;
					}
					else if (helmetLight.enabled)
					{
						HUDManager.Instance.batteryMeter.fillAmount = pocketedFlashlight.insertedBattery.charge / 1.3f;
						HUDManager.Instance.batteryMeter.gameObject.SetActive(value: true);
						HUDManager.Instance.batteryIcon.enabled = true;
						num4 = pocketedFlashlight.insertedBattery.charge / 1.3f;
					}
					else
					{
						HUDManager.Instance.batteryMeter.gameObject.SetActive(value: false);
						HUDManager.Instance.batteryIcon.enabled = false;
						num4 = 1f;
					}
					HUDManager.Instance.batteryBlinkUI.SetBool("blink", num4 < 0.2f && num4 > 0f);
					timeSinceSwitchingSlots += Time.deltaTime;
					if (limpMultiplier > 0f)
					{
						limpMultiplier -= Time.deltaTime / 1.8f;
					}
					if (health < 20)
					{
						if (healthRegenerateTimer <= 0f)
						{
							healthRegenerateTimer = 1f;
							health++;
							if (health >= 20)
							{
								MakeCriticallyInjured(enable: false);
							}
							HUDManager.Instance.UpdateHealthUI(health, hurtPlayer: false);
						}
						else
						{
							healthRegenerateTimer -= Time.deltaTime;
						}
					}
					SetHoverTipAndCurrentInteractTrigger();
				}
			}
			if (!inSpecialInteractAnimation && localArmsMatchCamera)
			{
				localArmsTransform.position = cameraContainerTransform.transform.position + gameplayCamera.transform.up * -0.5f;
				playerModelArmsMetarig.rotation = localArmsRotationTarget.rotation;
			}
			if (playersManager.overrideSpectateCamera || !base.IsOwner || !isPlayerDead || (base.IsServer && !isHostPlayerObject))
			{
				return;
			}
			if (isInGameOverAnimation > 0f && deadBody != null)
			{
				if (overrideGameOverSpectatePivot != null)
				{
					spectateCameraPivot.position = overrideGameOverSpectatePivot.position;
				}
				else
				{
					spectateCameraPivot.position = deadBody.bodyParts[0].position;
				}
				RaycastSpectateCameraAroundPivot();
			}
			else if (spectatedPlayerScript != null)
			{
				if (spectatedPlayerScript.isPlayerDead)
				{
					if (StartOfRound.Instance.allPlayersDead)
					{
						StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: true);
					}
					if (!(spectatedPlayerDeadTimer >= 1.5f))
					{
						spectatedPlayerDeadTimer += Time.deltaTime;
						if (spectatedPlayerScript.deadBody != null)
						{
							spectateCameraPivot.position = spectatedPlayerScript.deadBody.bodyParts[0].position;
							RaycastSpectateCameraAroundPivot();
						}
						return;
					}
					spectatedPlayerDeadTimer = 0f;
					SpectateNextPlayer(getClosest: true);
				}
				spectateCameraPivot.position = spectatedPlayerScript.lowerSpine.position + Vector3.up * 0.7f;
				RaycastSpectateCameraAroundPivot();
			}
			else if (StartOfRound.Instance.allPlayersDead)
			{
				StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: true);
				SetSpectatedPlayerEffects(allPlayersDead: true);
			}
			else
			{
				SpectateNextPlayer(getClosest: true);
			}
		}

		private void RaycastSpectateCameraAroundPivot()
		{
			interactRay = new Ray(spectateCameraPivot.position, -spectateCameraPivot.forward);
			if (Physics.Raycast(interactRay, out hit, 1.4f, walkableSurfacesNoPlayersMask, QueryTriggerInteraction.Ignore))
			{
				playersManager.spectateCamera.transform.position = interactRay.GetPoint(hit.distance - 0.25f);
			}
			else
			{
				playersManager.spectateCamera.transform.position = interactRay.GetPoint(1.3f);
			}
			playersManager.spectateCamera.transform.LookAt(spectateCameraPivot);
		}

		private void SetHoverTipAndCurrentInteractTrigger()
		{
			if (!isGrabbingObjectAnimation && !inSpecialMenu && !quickMenuManager.isMenuOpen)
			{
				interactRay = new Ray(gameplayCamera.transform.position, gameplayCamera.transform.forward);
				if (Physics.Raycast(interactRay, out hit, grabDistance, interactableObjectsMask) && hit.collider.gameObject.layer != 8 && hit.collider.gameObject.layer != 30)
				{
					string text = hit.collider.tag;
					if (!(text == "PhysicsProp"))
					{
						if (text == "InteractTrigger")
						{
							InteractTrigger component = hit.transform.gameObject.GetComponent<InteractTrigger>();
							if (component != previousHoveringOverTrigger && previousHoveringOverTrigger != null)
							{
								previousHoveringOverTrigger.isBeingHeldByPlayer = false;
							}
							if (!(component == null))
							{
								hoveringOverTrigger = component;
								if (!component.interactable)
								{
									cursorIcon.sprite = component.disabledHoverIcon;
									cursorIcon.enabled = component.disabledHoverIcon != null;
									cursorTip.text = component.disabledHoverTip;
								}
								else if (component.isPlayingSpecialAnimation || (inSpecialInteractAnimation && component.specialCharacterAnimation))
								{
									cursorIcon.enabled = false;
									cursorTip.text = "";
								}
								else if (isHoldingInteract)
								{
									if (twoHanded)
									{
										cursorTip.text = "[Hands full]";
									}
									else if (!string.IsNullOrEmpty(component.holdTip))
									{
										cursorTip.text = component.holdTip;
									}
								}
								else
								{
									cursorIcon.enabled = true;
									cursorIcon.sprite = component.hoverIcon;
									cursorTip.text = component.hoverTip;
								}
							}
						}
					}
					else
					{
						if (FirstEmptyItemSlot() == -1)
						{
							cursorTip.text = "Inventory full!";
							goto IL_01d3;
						}
						GrabbableObject component2 = hit.collider.gameObject.GetComponent<GrabbableObject>();
						if (Physics.Linecast(gameplayCamera.transform.position, component2.transform.position, 1073741824, QueryTriggerInteraction.Ignore))
						{
							cursorIcon.enabled = false;
							cursorTip.text = "";
						}
						else
						{
							if (GameNetworkManager.Instance.gameHasStarted || component2.itemProperties.canBeGrabbedBeforeGameStart || !(StartOfRound.Instance.testRoom == null))
							{
								if (component2 != null && !string.IsNullOrEmpty(component2.customGrabTooltip))
								{
									cursorTip.text = component2.customGrabTooltip;
								}
								else
								{
									cursorTip.text = "Grab : [E]";
								}
								goto IL_01d3;
							}
							cursorTip.text = "(Cannot hold until ship has landed)";
						}
					}
				}
				else
				{
					cursorIcon.enabled = false;
					cursorTip.text = "";
					if (hoveringOverTrigger != null)
					{
						previousHoveringOverTrigger = hoveringOverTrigger;
					}
					hoveringOverTrigger = null;
				}
				goto IL_037a;
			}
			goto IL_03c5;
			IL_037a:
			if (!isFreeCamera && Physics.Raycast(interactRay, out hit, 5f, playerMask))
			{
				PlayerControllerB component3 = hit.collider.gameObject.GetComponent<PlayerControllerB>();
				if (component3 != null)
				{
					component3.ShowNameBillboard();
				}
			}
			goto IL_03c5;
			IL_01d3:
			cursorIcon.enabled = true;
			cursorIcon.sprite = grabItemIcon;
			goto IL_037a;
			IL_03c5:
			if (StartOfRound.Instance.localPlayerUsingController)
			{
				StringBuilder stringBuilder = new StringBuilder(cursorTip.text);
				stringBuilder.Replace("[E]", "[X]");
				stringBuilder.Replace("[LMB]", "[X]");
				stringBuilder.Replace("[RMB]", "[R-Trigger]");
				stringBuilder.Replace("[F]", "[R-Shoulder]");
				stringBuilder.Replace("[Z]", "[L-Shoulder]");
				cursorTip.text = stringBuilder.ToString();
			}
			else
			{
				cursorTip.text = cursorTip.text.Replace("[LMB]", "[E]");
			}
		}

		public void ShowNameBillboard()
		{
			usernameAlpha.alpha = 1f;
			usernameCanvas.gameObject.SetActive(value: true);
		}

		public bool IsPlayerServer()
		{
			return base.IsServer;
		}

		private void SpectateNextPlayer(bool getClosest = false)
		{
			int num = 0;
			if (spectatedPlayerScript != null)
			{
				num = (int)spectatedPlayerScript.playerClientId;
			}
			bool flag = false;
			if (getClosest)
			{
				Vector3 position = positionOfDeath;
				if (deadBody != null)
				{
					position = deadBody.bodyParts[0].transform.position;
				}
				float num2 = 10000f;
				float num3 = 10000f;
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
				{
					if (!playersManager.allPlayerScripts[num].isPlayerDead && playersManager.allPlayerScripts[num].isPlayerControlled && playersManager.allPlayerScripts[num] != this)
					{
						num3 = Vector3.Distance(position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
						if (num3 < num2)
						{
							num2 = num3;
							num = i;
							flag = true;
						}
					}
				}
			}
			if (!flag)
			{
				for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
				{
					num = (num + 1) % StartOfRound.Instance.allPlayerScripts.Length;
					if (!playersManager.allPlayerScripts[num].isPlayerDead && playersManager.allPlayerScripts[num].isPlayerControlled && playersManager.allPlayerScripts[num] != this)
					{
						spectatedPlayerScript = playersManager.allPlayerScripts[num];
						StartOfRound.Instance.SetPlayerSafeInShip();
						SetSpectatedPlayerEffects();
						return;
					}
				}
				if (deadBody != null && deadBody.gameObject.activeSelf)
				{
					spectateCameraPivot.position = deadBody.bodyParts[0].position;
					RaycastSpectateCameraAroundPivot();
				}
				StartOfRound.Instance.SetPlayerSafeInShip();
			}
			else
			{
				spectatedPlayerScript = playersManager.allPlayerScripts[num];
				StartOfRound.Instance.SetPlayerSafeInShip();
				SetSpectatedPlayerEffects();
			}
		}

		public void SetSpectatedPlayerEffects(bool allPlayersDead = false)
		{
			try
			{
				if (spectatedPlayerScript != null)
				{
					HUDManager.Instance.SetSpectatingTextToPlayer(spectatedPlayerScript);
				}
				else
				{
					HUDManager.Instance.spectatingPlayerText.text = "";
				}
				TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
				if (allPlayersDead)
				{
					for (int i = 0; i < timeOfDay.effects.Length; i++)
					{
						timeOfDay.effects[i].effectEnabled = false;
					}
					if (timeOfDay.sunDirect != null)
					{
						timeOfDay.sunDirect.enabled = true;
						timeOfDay.sunIndirect.GetComponent<HDAdditionalLightData>().lightDimmer = 1f;
					}
					AudioReverbPresets audioReverbPresets = UnityEngine.Object.FindObjectOfType<AudioReverbPresets>();
					if (audioReverbPresets != null && audioReverbPresets.audioPresets.Length > 3)
					{
						GameNetworkManager.Instance.localPlayerController.reverbPreset = audioReverbPresets.audioPresets[3].reverbPreset;
					}
					SoundManager.Instance.SetEchoFilter(setEcho: false);
					return;
				}
				AudioReverbTrigger audioReverbTrigger = spectatedPlayerScript.currentAudioTrigger;
				AudioReverbPresets audioReverbPresets2 = UnityEngine.Object.FindObjectOfType<AudioReverbPresets>();
				if (audioReverbPresets2 != null && audioReverbPresets2.audioPresets.Length >= 4 && !spectatedPlayerScript.isInsideFactory && !spectatedPlayerScript.isInElevator)
				{
					audioReverbPresets2.audioPresets[3].ChangeAudioReverbForPlayer(spectatedPlayerScript);
				}
				if (audioReverbTrigger == null)
				{
					TimeOfDay.Instance.SetInsideLightingDimness(doNotLerp: true, spectatedPlayerScript.isInsideFactory || spectatedPlayerScript.isInHangarShipRoom);
					return;
				}
				if (audioReverbTrigger.localFog != null)
				{
					if (audioReverbTrigger.toggleLocalFog)
					{
						audioReverbTrigger.localFog.parameters.meanFreePath = audioReverbTrigger.fogEnabledAmount;
					}
					else
					{
						audioReverbTrigger.localFog.parameters.meanFreePath = 200f;
					}
				}
				TimeOfDay.Instance.SetInsideLightingDimness(doNotLerp: true, audioReverbTrigger.setInsideAtmosphere && audioReverbTrigger.insideLighting);
				if (audioReverbTrigger.disableAllWeather || spectatedPlayerScript.isInsideFactory)
				{
					TimeOfDay.Instance.DisableAllWeather();
				}
				else
				{
					if (audioReverbTrigger.enableCurrentLevelWeather && TimeOfDay.Instance.currentLevelWeather != LevelWeatherType.None)
					{
						TimeOfDay.Instance.effects[(int)TimeOfDay.Instance.currentLevelWeather].effectEnabled = true;
					}
					if (audioReverbTrigger.weatherEffect != -1)
					{
						TimeOfDay.Instance.effects[audioReverbTrigger.weatherEffect].effectEnabled = audioReverbTrigger.effectEnabled;
					}
				}
				StartOfRound.Instance.UpdatePlayerVoiceEffects();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error caught in SpectatedPlayerEffects: {arg}");
			}
		}

		public void AddBloodToBody()
		{
			for (int i = 0; i < bodyBloodDecals.Length; i++)
			{
				if (!bodyBloodDecals[i].activeSelf)
				{
					bodyBloodDecals[i].SetActive(value: true);
					break;
				}
			}
		}

		public void RemoveBloodFromBody()
		{
			for (int i = 0; i < bodyBloodDecals.Length; i++)
			{
				bodyBloodDecals[i].SetActive(value: false);
			}
		}

		bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX = false, int hitID = -1)
		{
			if (!AllowPlayerDeath())
			{
				return false;
			}
			CentipedeAI[] array = UnityEngine.Object.FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].clingingToPlayer == this)
				{
					return false;
				}
			}
			if ((bool)inAnimationWithEnemy)
			{
				return false;
			}
			if (force <= 2)
			{
				DamagePlayerFromOtherClientServerRpc(20, hitDirection, (int)playerWhoHit.playerClientId);
			}
			else if (force <= 4)
			{
				DamagePlayerFromOtherClientServerRpc(30, hitDirection, (int)playerWhoHit.playerClientId);
			}
			else
			{
				DamagePlayerFromOtherClientServerRpc(100, hitDirection, (int)playerWhoHit.playerClientId);
			}
			return true;
		}

		[ServerRpc(RequireOwnership = false)]
		public void DamagePlayerFromOtherClientServerRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit)
				{
					DamagePlayerFromOtherClientClientRpc(damageAmount, hitDirection, playerWhoHit, health - damageAmount);
				}

		[ClientRpc]
		public void DamagePlayerFromOtherClientClientRpc(int damageAmount, Vector3 hitDirection, int playerWhoHit, int newHealthAmount)
{if(!AllowPlayerDeath())			{
				return;
			}
			if (base.IsOwner && isPlayerControlled)
			{
				CentipedeAI[] array = UnityEngine.Object.FindObjectsByType<CentipedeAI>(FindObjectsSortMode.None);
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].clingingToPlayer == this)
					{
						return;
					}
				}
				DamagePlayer(damageAmount, hasDamageSFX: true, callRPC: true, CauseOfDeath.Bludgeoning);
			}
			movementAudio.PlayOneShot(StartOfRound.Instance.hitPlayerSFX);
			if (health < 6)
			{
				DropBlood(hitDirection);
				bodyBloodDecals[0].SetActive(value: true);
				playersManager.allPlayerScripts[playerWhoHit].AddBloodToBody();
				playersManager.allPlayerScripts[playerWhoHit].movementAudio.PlayOneShot(StartOfRound.Instance.bloodGoreSFX);
				WalkieTalkie.TransmitOneShotAudio(playersManager.allPlayerScripts[playerWhoHit].movementAudio, StartOfRound.Instance.bloodGoreSFX);
			}
}
		public bool HasLineOfSightToPosition(Vector3 pos, float width = 45f, int range = 60, float proximityAwareness = -1f)
		{
			float num = Vector3.Distance(base.transform.position, pos);
			if (num < (float)range && (Vector3.Angle(playerEye.transform.forward, pos - gameplayCamera.transform.position) < width || num < proximityAwareness) && !Physics.Linecast(playerEye.transform.position, pos, out hit, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
			{
				return true;
			}
			return false;
		}

		public float LineOfSightToPositionAngle(Vector3 pos, int range = 60, float proximityAwareness = -1f)
		{
			if (Vector3.Distance(base.transform.position, pos) < (float)range && !Physics.Linecast(playerEye.transform.position, pos, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore))
			{
				return Vector3.Angle(playerEye.transform.forward, pos - gameplayCamera.transform.position);
			}
			return -361f;
		}
	}
}
