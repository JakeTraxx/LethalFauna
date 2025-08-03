using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class CaveDwellerAI : EnemyAI
{
	public AudioSource walkingAudio;

	public AudioClip growlSFX;

	public AudioClip[] fakeCrySFX;

	public AudioSource clickingAudio1;

	public AudioSource clickingAudio2;

	public AudioSource screamAudio;

	public AudioSource screamAudioNonDiagetic;

	private bool isFakingBabyVoice = true;

	[Header("Maneater Variables")]
	public float sneakSpeed;

	public float attackDistance;

	public float leapSpeed;

	public float leapTime;

	public float screamTime;

	public float cooldownTime;

	public float chaseSpeed;

	public float baseSearchWidth;

	private float currentSearchWidth;

	[Space(3f)]
	public static float CaveDwellerDeafenAmount;

	private float caveDwellerDeafenAmountSmoothed;

	public float deafeningMaxDistance;

	public float deafeningMinDistance;

	public float maxDeafenAmount;

	public AISearchRoutine searchRoutine;

	private bool inKillAnimation;

	public List<Transform> ignoredNodes = new List<Transform>();

	private Vector3 caveHidingSpot;

	private bool screaming;

	private bool leaping;

	private float screamTimer;

	private float leapTimer;

	private bool chasingAfterLeap;

	private int previousBehaviourState = -1;

	private bool startingKillAnimationLocalClient;

	private Coroutine killAnimationCoroutine;

	private DeadBodyInfo bodyBeingCarried;

	public Transform bodyRagdollPoint;

	public ParticleSystem killPlayerParticle1;

	public ParticleSystem killPlayerParticle2;

	private bool startedLeapingThisFrame;

	private Vector3 agentLocalVelocity;

	private Vector3 previousPosition;

	public Transform animationContainer;

	private bool movedSinceLastCheck;

	public DampedTransform headRig;

	public Transform[] bodyPoints;

	private float noPlayersTimer;

	private bool wasOutsideLastFrame;

	private bool beganCooldown;

	public AudioClip cooldownSFX;

	[Header("Baby AI")]
	public AudioSource babyCryingAudio;

	public AudioSource babyVoice;

	public AudioClip squirmingSFX;

	public AudioClip transformationSFX;

	public AudioClip[] scaredBabyVoiceSFX;

	public AudioClip biteSFX;

	public ParticleSystem babyTearsParticle;

	public ParticleSystem babyFoamAtMouthParticle;

	public BabyState babyState;

	private int previousBabyState = -1;

	[Space(5f)]
	public float lonelinessMeter = 0.5f;

	public float stressMeter;

	public float growthMeter;

	public float growthSpeedMultiplier = 1f;

	public float moodinessMultiplier = 1f;

	[Space(3f)]
	public float decreaseLonelinessMultiplier = 0.07f;

	public float increaseLonelinessMultiplier = 0.03f;

	[Space(5f)]
	public List<BabyPlayerMemory> playerMemory = new List<BabyPlayerMemory>();

	private int playersSeen;

	private float currentActivityTimer;

	private float activityTimerOffset;

	public GameObject observingObject;

	public bool sittingDown;

	public bool babyRunning;

	public bool babyCrying;

	private bool stopCryingWhenReleased;

	public int rockingBaby;

	public bool holdingBaby;

	public float rockBabyTimer;

	public PlayerControllerB playerHolding;

	private Coroutine dropBabyCoroutine;

	public CaveDwellerPhysicsProp propScript;

	public bool hasPlayerFoundBaby;

	private bool pathingTowardsNearestPlayer;

	public AISearchRoutine babySearchRoutine;

	public MultiAimConstraint babyLookRig;

	public Transform babyLookTarget;

	private float pingAttentionTimer;

	private int focusLevel;

	public Vector3 pingAttentionPosition;

	private float timeAtLastPingAttention;

	private float fallOverWhileSittingChanceInterval;

	private float cantFindObservedObjectTimer;

	private float timeSpentObservingTimer;

	private Vector3 runningFromPosition;

	public bool eatingScrap;

	public GrabbableObject observingScrap;

	private float timeSinceEatingScrap;

	private float eatScrapRandomCheckInterval;

	private PlayerControllerB observingPlayer;

	private bool gettingFarthestNodeFromSpotAsync;

	private Transform farthestNodeFromRunningSpot;

	public Transform BabyEye;

	public Animator babyCreatureAnimator;

	public float lookVerticalOffset = 1f;

	private float timeAtLastHeardNoise;

	private float babyCryLonelyThreshold = 0.9f;

	private float babyFollowPlayersThreshold = 0.6f;

	private float babyRoamFromPlayersThreshold = 0.34f;

	private float babyCrySquirmingThreshold = 0.07f;

	private float timeSinceCheckingForScrap;

	private int scrapEaten;

	public Transform spine2;

	public float rollOverTimer;

	public bool rolledOver;

	public bool babySquirming;

	private int[] ignoreItemIds = new int[1] { 819501 };

	private List<GameObject> seenScrap = new List<GameObject>();

	public GameObject babyContainer;

	public GameObject adultContainer;

	private float timeSinceCryingFromScarySight;

	private RaycastHit hit;

	private float fakeCryTimer;

	private bool clickingMandibles;

	private float timeSinceBiting;

	private bool wasBodyVisible;

	private Vector3 positionAtLeavingLOS;

	private float shakeCameraInterval;

	private float checkMovingInterval;

	private bool pursuingPlayerInSneakMode;

	private float changeOwnershipInterval;

	private float scareBabyWhileHoldingCooldown;

	public AudioClip pukeSFX;

	private bool nearTransforming;

	private bool babyPuked;

	private float checkIfTrappedInterval;

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (currentBehaviourStateIndex == 0)
		{
			if (base.IsOwner)
			{
				if (playerWhoHit != null)
				{
					ScareBaby(playerWhoHit.transform.position);
				}
				else
				{
					ScareBaby(base.transform.position);
				}
				growthMeter += 0.6f;
			}
		}
		else if (!inSpecialAnimation)
		{
			enemyHP--;
			if (base.IsOwner && enemyHP <= 0)
			{
				KillEnemyOnOwnerClient();
			}
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		if (currentBehaviourStateIndex == 0)
		{
			if (base.IsOwner)
			{
				ScareBaby(base.transform.position);
				growthMeter += 0.6f;
			}
		}
		else if (!inSpecialAnimation && adultContainer.activeSelf)
		{
			if (inKillAnimation)
			{
				FinishKillAnimation(completed: false);
			}
			walkingAudio.Stop();
			creatureVoice.Stop();
			creatureSFX.Stop();
			screamAudio.Stop();
			base.KillEnemy();
		}
	}

	private bool IsBodyVisibleToTarget()
	{
		if (targetPlayer == null)
		{
			return false;
		}
		for (int i = 0; i < bodyPoints.Length; i++)
		{
			if (targetPlayer.HasLineOfSightToPosition(bodyPoints[i].transform.position, 45f, 15))
			{
				return true;
			}
		}
		return false;
	}

	private void InitializeBabyValues()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			playerMemory.Add(new BabyPlayerMemory((ulong)i));
		}
		HoarderBugAI.RefreshGrabbableObjectsInMapList();
	}

	private BabyPlayerMemory GetBabyMemoryOfPlayer(PlayerControllerB player)
	{
		for (int i = 0; i < playerMemory.Count; i++)
		{
			if (playerMemory[i].playerId == player.playerClientId)
			{
				return playerMemory[i];
			}
		}
		return null;
	}

	private void IncreaseBabyGrowthMeter()
	{
		float num = 1.3f;
		if (rockingBaby == 2 && stopCryingWhenReleased)
		{
			num = 4f;
		}
		else if (babyRunning)
		{
			num = 1.8f;
		}
		if (babyCrying)
		{
			if (!hasPlayerFoundBaby)
			{
				growthMeter += 0.1f * growthSpeedMultiplier * Time.deltaTime;
			}
			else
			{
				growthMeter = Mathf.Clamp(growthMeter + growthSpeedMultiplier * num * Time.deltaTime, 0f, 25f);
			}
		}
		if (!nearTransforming && growthMeter > 0.8f)
		{
			nearTransforming = true;
			SetBabyNearTransformingServerRpc();
		}
		if (growthMeter > 1f)
		{
			TransformIntoAdult();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetBabyNearTransformingServerRpc()
			{
				SetBabyNearTransformingClientRpc();
			}

	[ClientRpc]
	public void SetBabyNearTransformingClientRpc()
{if(!base.IsServer)			{
				nearTransforming = true;
			}
}
	public void PickUpBabyLocalClient()
	{
		Debug.Log($"Player held by null?: {propScript.playerHeldBy == null}");
		Debug.Log($"Player held by: {propScript.playerHeldBy}");
		Debug.Log($"Player held by playerclientid: {propScript.playerHeldBy.playerClientId}");
		currentOwnershipOnThisClient = (int)propScript.playerHeldBy.playerClientId;
		Debug.Log($"Set currentownershiponthisclient to {currentOwnershipOnThisClient}");
		inSpecialAnimation = true;
		agent.enabled = false;
		holdingBaby = true;
		if (dropBabyCoroutine != null)
		{
			StopCoroutine(dropBabyCoroutine);
		}
		if (base.IsServer && babyState == BabyState.RolledOver)
		{
			babyState = BabyState.Roaming;
		}
		SetRolledOverLocalClient(setRolled: false, scared: false);
		playerHolding = propScript.playerHeldBy;
	}

	public void DropBabyLocalClient()
	{
		holdingBaby = false;
		rockingBaby = 0;
		playerHolding = null;
		Debug.Log("Drop baby A");
		if (currentBehaviourStateIndex != 0)
		{
			return;
		}
		if (base.IsOwner)
		{
			Debug.Log($"Set ownership of creature. Currentownershiponthisclient: {currentOwnershipOnThisClient}");
			ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
		}
		serverPosition = base.transform.position;
		bool flag = true;
		Vector3 itemFloorPosition = propScript.GetItemFloorPosition(propScript.previousPlayerHeldBy.transform.position + Vector3.up * 0.5f);
		Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(itemFloorPosition, default(NavMeshHit), 10f);
		Debug.Log($"Drop on position: {navMeshPosition}");
		Debug.DrawRay(propScript.startFallingPosition, Vector3.up * 1f, Color.white, 10f);
		Debug.DrawRay(navMeshPosition, Vector3.up * 0.75f, Color.red, 10f);
		if (!RoundManager.Instance.GotNavMeshPositionResult || DebugEnemy)
		{
			flag = false;
			itemFloorPosition = propScript.startFallingPosition;
			Debug.Log($"Drop Baby C; {propScript.startFallingPosition}");
			if (propScript.transform.parent != null)
			{
				itemFloorPosition = propScript.transform.parent.TransformPoint(propScript.startFallingPosition);
			}
			Debug.Log($"Drop Baby C global; {propScript.startFallingPosition}");
			Transform transform = ChooseClosestNodeToPositionNoPathCheck(itemFloorPosition);
			navMeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.transform.position);
			Debug.Log($"Got nav mesh position : {RoundManager.Instance.GotNavMeshPositionResult}; {navMeshPosition}; dist: {Vector3.Distance(base.transform.position, transform.transform.position)}");
			Debug.DrawRay(navMeshPosition, Vector3.up * 1.2f, Color.magenta, 10f);
		}
		Debug.Log("Drop baby D");
		if (propScript.transform.parent == null)
		{
			propScript.targetFloorPosition = navMeshPosition;
		}
		else
		{
			propScript.targetFloorPosition = propScript.transform.parent.InverseTransformPoint(navMeshPosition);
		}
		Debug.DrawRay(propScript.targetFloorPosition, Vector3.up * 2f, Color.yellow, 5f);
		if (flag)
		{
			if (dropBabyCoroutine != null)
			{
				StopCoroutine(dropBabyCoroutine);
			}
			dropBabyCoroutine = StartCoroutine(DropBabyAnimation(navMeshPosition));
		}
		else
		{
			Debug.Log($"Drop baby F; got no floor target; drop pos: {navMeshPosition}");
			base.transform.position = navMeshPosition;
			inSpecialAnimation = false;
		}
	}

	public Transform ChooseClosestNodeToPositionNoPathCheck(Vector3 pos)
	{
		nodesTempArray = allAINodes.OrderBy((GameObject x) => Vector3.Distance(pos, x.transform.position)).ToArray();
		return nodesTempArray[0].transform;
	}

	private IEnumerator DropBabyAnimation(Vector3 dropOnPosition)
	{
		float time = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => propScript.reachedFloorTarget || Time.realtimeSinceStartup - time > 2f);
		Debug.Log("Drop baby E");
		if (currentBehaviourStateIndex == 0)
		{
			base.transform.position = dropOnPosition;
			inSpecialAnimation = false;
			if (base.IsServer && propScript.previousPlayerHeldBy != null && propScript.previousPlayerHeldBy.isPlayerDead)
			{
				ScareBaby(base.transform.position);
			}
		}
		dropBabyCoroutine = null;
	}

	public void PingAttention(int newFocusLevel, float timeToLook, Vector3 attentionPosition, bool sync = true)
	{
		if (!(pingAttentionTimer >= 0f) || newFocusLevel >= focusLevel)
		{
			focusLevel = newFocusLevel;
			pingAttentionTimer = timeToLook;
			pingAttentionPosition = attentionPosition;
			timeAtLastPingAttention = Time.realtimeSinceStartup;
			if (sync)
			{
				PingAttentionServerRpc(timeToLook, attentionPosition);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PingAttentionServerRpc(float timeToLook, Vector3 attentionPosition)
			{
				PingAttentionClientRpc(timeToLook, attentionPosition);
			}

	[ClientRpc]
	public void PingAttentionClientRpc(float timeToLook, Vector3 attentionPosition)
{if(!base.IsOwner)			{
				pingAttentionTimer = timeToLook;
				pingAttentionPosition = attentionPosition;
			}
}
	public PlayerControllerB[] GetAllPlayerBodiesInLineOfSight(float width = 45f, int range = 60, Transform eyeObject = null, float proximityCheck = -1f, int layerMask = -1)
	{
		if (layerMask == -1)
		{
			layerMask = StartOfRound.Instance.collidersAndRoomMaskAndDefault;
		}
		if (eyeObject == null)
		{
			eyeObject = eye;
		}
		if (isOutside && !enemyType.canSeeThroughFog && TimeOfDay.Instance.currentLevelWeather == LevelWeatherType.Foggy)
		{
			range = Mathf.Clamp(range, 0, 30);
		}
		List<PlayerControllerB> list = new List<PlayerControllerB>(4);
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (!StartOfRound.Instance.allPlayerScripts[i].isPlayerDead || StartOfRound.Instance.allPlayerScripts[i].deadBody == null)
			{
				continue;
			}
			Vector3 position = StartOfRound.Instance.allPlayerScripts[i].deadBody.bodyParts[5].transform.position;
			if (Vector3.Distance(eye.position, position) < (float)range && !Physics.Linecast(eyeObject.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				Vector3 to = position - eyeObject.position;
				if (Vector3.Angle(eyeObject.forward, to) < width || Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < proximityCheck)
				{
					list.Add(StartOfRound.Instance.allPlayerScripts[i]);
				}
			}
		}
		if (list.Count == 4)
		{
			return StartOfRound.Instance.allPlayerScripts;
		}
		if (list.Count > 0)
		{
			return list.ToArray();
		}
		return null;
	}

	private bool BabyObserveScrap()
	{
		if (Time.realtimeSinceStartup - timeSinceCheckingForScrap < 1f || lonelinessMeter > 0.85f)
		{
			return false;
		}
		GameObject gameObject = CheckLineOfSight(HoarderBugAI.grabbableObjectsInMap, 130f, 12, 3f, BabyEye, ignoreItemIds);
		if (gameObject != null && (observingPlayer == null || (lonelinessMeter < babyFollowPlayersThreshold && !observingPlayer.isPlayerDead)))
		{
			if (!hasPlayerFoundBaby && HoarderBugAI.HoarderBugItems != null && HoarderBugAI.HoarderBugItems.Count > 0)
			{
				for (int i = 0; i < HoarderBugAI.HoarderBugItems.Count; i++)
				{
					if (HoarderBugAI.HoarderBugItems[i].itemGrabbableObject == gameObject)
					{
						return false;
					}
				}
			}
			GrabbableObject component = gameObject.GetComponent<GrabbableObject>();
			if (component != null && component != propScript && component.NetworkObject.IsSpawned && (!seenScrap.Contains(gameObject) || UnityEngine.Random.Range(0, 100) < 4))
			{
				observingObject = gameObject;
				observingScrap = component;
				focusLevel = 2;
				babyState = BabyState.Observing;
				observingPlayer = null;
				SetBabyObservingObjectServerRpc(component.NetworkObject);
				return true;
			}
		}
		return false;
	}

	private void DoBabyAIInterval()
	{
		if (holdingBaby || rolledOver)
		{
			return;
		}
		bool num = !babyRunning;
		bool flag = false;
		if (num)
		{
			if (!babyCrying)
			{
				PlayerControllerB[] allPlayerBodiesInLineOfSight = GetAllPlayerBodiesInLineOfSight(50f, 20, BabyEye, 2f);
				if (allPlayerBodiesInLineOfSight != null)
				{
					for (int i = 0; i < allPlayerBodiesInLineOfSight.Length; i++)
					{
						BabyPlayerMemory babyMemoryOfPlayer = GetBabyMemoryOfPlayer(allPlayerBodiesInLineOfSight[i]);
						if (babyMemoryOfPlayer != null && babyMemoryOfPlayer.orderSeen != -1)
						{
							if (babyMemoryOfPlayer.likeMeter > 0.4f && allPlayerBodiesInLineOfSight[i].deadBody.grabBodyObject != null && allPlayerBodiesInLineOfSight[i].deadBody.grabBodyObject.NetworkObject != null && allPlayerBodiesInLineOfSight[i].deadBody.grabBodyObject.NetworkObject.IsSpawned)
							{
								babyState = BabyState.Observing;
								observingScrap = null;
								observingObject = allPlayerBodiesInLineOfSight[i].deadBody.bodyParts[6].transform.gameObject;
								observingPlayer = allPlayerBodiesInLineOfSight[i];
								flag = true;
								SetBabyObservingObjectServerRpc(allPlayerBodiesInLineOfSight[i].deadBody.grabBodyObject.NetworkObject);
							}
							babyMemoryOfPlayer.isPlayerDead = true;
						}
					}
				}
			}
			int num2 = -1;
			bool flag2 = false;
			if (observingPlayer != null)
			{
				num2 = (int)observingPlayer.playerClientId;
			}
			int num3 = -1;
			PlayerControllerB[] allPlayersInLineOfSight = GetAllPlayersInLineOfSight(120f, 30, BabyEye, 3f);
			if (allPlayersInLineOfSight != null)
			{
				for (int j = 0; j < allPlayersInLineOfSight.Length; j++)
				{
					Debug.DrawLine(BabyEye.transform.position, allPlayersInLineOfSight[j].gameplayCamera.transform.position, Color.green, 1f);
					BabyPlayerMemory babyMemoryOfPlayer2 = GetBabyMemoryOfPlayer(allPlayersInLineOfSight[j]);
					if (babyMemoryOfPlayer2 == null)
					{
						continue;
					}
					if (Time.realtimeSinceStartup - babyMemoryOfPlayer2.timeAtLastSighting > 4f)
					{
						babyMemoryOfPlayer2.timeAtLastNoticing = Time.realtimeSinceStartup;
					}
					babyMemoryOfPlayer2.timeAtLastSighting = Time.realtimeSinceStartup;
					if (babyMemoryOfPlayer2.orderSeen == -1)
					{
						if (playersSeen == 0)
						{
							babyMemoryOfPlayer2.likeMeter = UnityEngine.Random.Range(0.4f, 0.8f);
							if (num3 < 3 || pingAttentionTimer <= 0f)
							{
								flag2 = true;
							}
						}
						else
						{
							babyMemoryOfPlayer2.likeMeter = UnityEngine.Random.Range(-0.2f, 0.5f);
							if (focusLevel <= 2 || pingAttentionTimer <= 0f || (observingScrap != null && timeSpentObservingTimer > 4f))
							{
								flag2 = true;
							}
						}
						playersSeen++;
						babyMemoryOfPlayer2.orderSeen = playersSeen;
					}
					else if (Time.realtimeSinceStartup - babyMemoryOfPlayer2.timeAtLastSighting > 26f && babyMemoryOfPlayer2.likeMeter > 0.25f && lonelinessMeter > babyRoamFromPlayersThreshold)
					{
						if (focusLevel < 2 || pingAttentionTimer <= 0f || (observingScrap != null && timeSpentObservingTimer > 4f))
						{
							flag2 = true;
						}
					}
					else if (lonelinessMeter > 0.75f && !observingPlayer)
					{
						flag2 = true;
					}
					if (flag2)
					{
						if (num2 != -1)
						{
							BabyPlayerMemory babyMemoryOfPlayer3 = GetBabyMemoryOfPlayer(StartOfRound.Instance.allPlayerScripts[num2]);
							if (babyMemoryOfPlayer2.likeMeter > babyMemoryOfPlayer3.likeMeter)
							{
								num2 = (int)allPlayersInLineOfSight[j].playerClientId;
								num3 = 3;
							}
						}
						else
						{
							num2 = (int)allPlayersInLineOfSight[j].playerClientId;
							num3 = 3;
						}
					}
					flag2 = false;
				}
				if (num2 != -1 && !flag)
				{
					babyState = BabyState.Observing;
					focusLevel = num3;
					observingScrap = null;
					observingObject = StartOfRound.Instance.allPlayerScripts[num2].gameplayCamera.gameObject;
					observingPlayer = StartOfRound.Instance.allPlayerScripts[num2];
					flag = true;
					SetBabyObservingPlayerServerRpc(num2, 1);
				}
			}
			if (!babyCrying && !flag && BabyObserveScrap())
			{
				flag = true;
			}
		}
		switch (babyState)
		{
		case BabyState.Roaming:
			if (pathingTowardsNearestPlayer)
			{
				if (babySearchRoutine.inProgress)
				{
					StopSearch(babySearchRoutine);
				}
				if (TargetClosestPlayer())
				{
					SetMovingTowardsTargetPlayer(targetPlayer);
				}
				break;
			}
			if (sittingDown)
			{
				agent.speed = 0f;
				break;
			}
			if (!isInsidePlayerShip && !babySearchRoutine.inProgress)
			{
				Transform transform = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: false, UnityEngine.Random.Range(5, allAINodes.Length / 2));
				Vector3 position = transform.position;
				Debug.Log("Starting search at node " + transform.name, transform.gameObject);
				StartSearch(position, babySearchRoutine);
			}
			if (babyCrying)
			{
				agent.speed = 2.5f;
			}
			else
			{
				agent.speed = 4f;
			}
			break;
		case BabyState.Observing:
		{
			if (observingObject == null)
			{
				break;
			}
			if (babySearchRoutine.inProgress)
			{
				StopSearch(babySearchRoutine);
			}
			Vector3 position2 = observingObject.transform.position;
			position2.y = BabyEye.transform.position.y;
			if (!CheckLineOfSightForPosition(position2, 120f, 10, 3f, BabyEye))
			{
				cantFindObservedObjectTimer = Mathf.Max(cantFindObservedObjectTimer + AIIntervalTime, 0f);
				if (cantFindObservedObjectTimer > 2.3f)
				{
					babyState = BabyState.Roaming;
					break;
				}
				if (sittingDown)
				{
					sittingDown = false;
					SetBabySittingServerRpc(sit: false);
				}
				Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(observingObject.transform.position, default(NavMeshHit), 6f);
				if (RoundManager.Instance.GotNavMeshPositionResult)
				{
					SetDestinationToPosition(navMeshPosition);
					agent.speed = 4.2f;
				}
				break;
			}
			if ((bool)observingPlayer && (observingPlayer.isPlayerDead || (observingPlayer.criticallyInjured && Time.realtimeSinceStartup - timeSinceCryingFromScarySight > 12f)))
			{
				timeSinceCryingFromScarySight = Time.realtimeSinceStartup;
				ScareBaby(observingPlayer.transform.position);
				break;
			}
			if (!babyCrying && Time.realtimeSinceStartup - timeSinceCryingFromScarySight > 5f && (bool)observingScrap && observingScrap.itemProperties.itemId == 123984)
			{
				CaveDwellerPhysicsProp caveDwellerPhysicsProp = observingScrap as CaveDwellerPhysicsProp;
				if (caveDwellerPhysicsProp != null && caveDwellerPhysicsProp.caveDwellerScript.babyCrying)
				{
					timeSinceCryingFromScarySight = Time.realtimeSinceStartup;
					SetCryingLocalClient(setCrying: true);
					SetBabyCryingServerRpc(setCry: true);
					break;
				}
			}
			if (eatingScrap)
			{
				if (observingScrap != null && !observingScrap.isHeld && SetDestinationToPosition(observingObject.transform.position))
				{
					if (sittingDown)
					{
						sittingDown = false;
						SetBabySittingServerRpc(sit: false);
					}
					agent.speed = 3.6f;
					if (Vector3.Distance(base.transform.position, observingObject.transform.position) < 0.9f)
					{
						eatingScrap = false;
						babyState = BabyState.Roaming;
						observingScrap.NetworkObject.Despawn();
						StopObserving(eatScrap: true);
						break;
					}
					if (Time.realtimeSinceStartup - timeSinceEatingScrap > 7f)
					{
						eatingScrap = false;
					}
				}
				else
				{
					if (!observingScrap.isHeld && observingScrap.playerHeldBy != null)
					{
						BabyPlayerMemory babyMemoryOfPlayer4 = GetBabyMemoryOfPlayer(observingScrap.playerHeldBy);
						babyMemoryOfPlayer4.likeMeter = Mathf.Max(babyMemoryOfPlayer4.likeMeter - 0.2f, -0.5f);
					}
					eatingScrap = false;
				}
			}
			else if (observingPlayer == null && observingScrap != null && !observingScrap.isHeld && observingScrap.itemProperties.itemId != 123984 && Time.realtimeSinceStartup - eatScrapRandomCheckInterval > 3f)
			{
				if (!seenScrap.Contains(observingObject))
				{
					seenScrap.Add(observingObject);
					if (seenScrap.Count > 5)
					{
						seenScrap.RemoveAt(0);
					}
				}
				eatScrapRandomCheckInterval = Time.realtimeSinceStartup;
				if ((UnityEngine.Random.Range(0, 100) < 20 || (isInsidePlayerShip && UnityEngine.Random.Range(0, 100) < 70)) && (Time.realtimeSinceStartup - timeSinceEatingScrap > 25f || (Time.realtimeSinceStartup - timeSinceEatingScrap > 1f && UnityEngine.Random.Range(0, 100) < 40)))
				{
					eatingScrap = true;
					timeSinceEatingScrap = Time.realtimeSinceStartup;
				}
			}
			cantFindObservedObjectTimer = Mathf.Min(cantFindObservedObjectTimer - AIIntervalTime, 0f);
			BabyPlayerMemory babyPlayerMemory = null;
			if (observingPlayer != null)
			{
				babyPlayerMemory = GetBabyMemoryOfPlayer(observingPlayer);
			}
			if (observingPlayer != null && observingPlayer.isPlayerDead && observingPlayer.deadBody != null && babyPlayerMemory != null && babyPlayerMemory.orderSeen != -1 && babyPlayerMemory.likeMeter > 0.4f)
			{
				ScareBaby(observingPlayer.deadBody.bodyParts[5].transform.position);
			}
			break;
		}
		case BabyState.Running:
		{
			if (babySearchRoutine.inProgress)
			{
				StopSearch(babySearchRoutine);
			}
			if (farthestNodeFromRunningSpot == null)
			{
				gettingFarthestNodeFromSpotAsync = true;
				break;
			}
			SetDestinationToPosition(farthestNodeFromRunningSpot.transform.position);
			agent.speed = 6.25f;
			if (Vector3.Distance(base.transform.position, farthestNodeFromRunningSpot.transform.position) < 4f)
			{
				if (babyCrying)
				{
					farthestNodeFromRunningSpot = null;
					break;
				}
				babyState = BabyState.Roaming;
			}
			if (babyCrying)
			{
				break;
			}
			float num4 = Vector3.Distance(base.transform.position, runningFromPosition);
			if (num4 > 16f)
			{
				if (Time.realtimeSinceStartup - currentActivityTimer > 10f)
				{
					babyState = BabyState.Roaming;
				}
			}
			else if (num4 > 26f)
			{
				babyState = BabyState.Roaming;
			}
			break;
		}
		case BabyState.RolledOver:
			if (babySearchRoutine.inProgress)
			{
				StopSearch(babySearchRoutine);
			}
			agent.speed = 0f;
			break;
		}
	}

	private void ScareBaby(Vector3 runFromPosition)
	{
		if (!base.IsServer)
		{
			return;
		}
		if (sittingDown && !holdingBaby)
		{
			SetRolledOverLocalClient(setRolled: true, scared: true);
			SetBabyRolledOverServerRpc(rolledOver: true, scared: true);
			return;
		}
		runningFromPosition = runFromPosition;
		currentActivityTimer = Time.realtimeSinceStartup;
		farthestNodeFromRunningSpot = null;
		SetCryingLocalClient(setCrying: true);
		babyState = BabyState.Running;
		if (propScript.playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			propScript.playerHeldBy.DiscardHeldObject();
		}
		ScareBabyServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	public void ScareBabyServerRpc()
			{
				ScareBabyClientRpc();
			}

	[ClientRpc]
	public void ScareBabyClientRpc()
{if(!base.IsServer)		{
			SetCryingLocalClient(setCrying: true);
			babyRunning = true;
			if (propScript.playerHeldBy == GameNetworkManager.Instance.localPlayerController)
			{
				propScript.playerHeldBy.DiscardHeldObject();
			}
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetBabySittingServerRpc(bool sit)
			{
				SetBabySittingClientRpc(sit);
			}

	[ClientRpc]
	public void SetBabySittingClientRpc(bool sit)
{if(!base.IsServer)			{
				sittingDown = sit;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetBabyRolledOverServerRpc(bool rolledOver, bool scared)
			{
				SetBabyRolledOverClientRpc(rolledOver, scared);
			}

	[ClientRpc]
	public void SetBabyRolledOverClientRpc(bool setRolled, bool scared)
{if(!base.IsServer)			{
				SetRolledOverLocalClient(setRolled, scared);
			}
}
	private void PlayBabyScaredAudio()
	{
		RoundManager.PlayRandomClip(babyVoice, scaredBabyVoiceSFX);
	}

	private void SetRolledOverLocalClient(bool setRolled, bool scared)
	{
		Debug.Log($"Set rolled local client to {setRolled}; scared: {scared}");
		if (scared && setRolled)
		{
			PlayBabyScaredAudio();
			rollOverTimer = 0.7f;
		}
		else if (setRolled && !rolledOver)
		{
			babyState = BabyState.RolledOver;
			rollOverTimer = 0f;
		}
		rolledOver = setRolled;
		Debug.Log($"Set fall over anim: {setRolled}");
		babyCreatureAnimator.SetBool("FallOver", setRolled);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetBabyCryingServerRpc(bool setCry)
			{
				SetBabyCryingClientRpc(setCry);
			}

	[ClientRpc]
	public void SetBabyCryingClientRpc(bool setCry)
{if(!base.IsServer)			{
				SetCryingLocalClient(setCry);
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetBabyBServerRpc()
			{
				SetBabyBClientRpc();
			}

	[ClientRpc]
	public void SetBabyBClientRpc()
			{
			}

	[ServerRpc(RequireOwnership = false)]
	public void SetBabyRunningServerRpc(bool setRunning)
			{
				SetBabyRunningClientRpc(setRunning);
			}

	[ClientRpc]
	public void SetBabyRunningClientRpc(bool setRunning)
{if(!base.IsServer)			{
				babyRunning = setRunning;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetBabySquirmingServerRpc(bool setSquirm)
			{
				SetBabySquirmingClientRpc(setSquirm);
			}

	[ClientRpc]
	public void SetBabySquirmingClientRpc(bool setSquirm)
{if(!base.IsServer)			{
				babySquirming = setSquirm;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void ClearBabyObservingServerRpc(bool eatScrap)
			{
				ClearBabyObservingClientRpc(eatScrap);
			}

	[ClientRpc]
	public void ClearBabyObservingClientRpc(bool eatScrap)
{if(!base.IsServer)		{
			observingPlayer = null;
			observingObject = null;
			observingScrap = null;
			if (eatScrap)
			{
				scrapEaten++;
				babyCreatureAnimator.SetTrigger("Bite");
				babyVoice.PlayOneShot(biteSFX);
				timeSinceBiting = Time.realtimeSinceStartup;
				babyLookRig.weight = 0f;
				float num = Mathf.Lerp(1f, 1.6f, Mathf.Min(scrapEaten, 5f) / 5f);
				spine2.localScale = new Vector3(num, 1f, num);
			}
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetBabyObservingPlayerServerRpc(int playerId, int setFocusLevel)
			{
				SetBabyObservingPlayerClientRpc(playerId, setFocusLevel);
			}

	[ClientRpc]
	public void SetBabyObservingPlayerClientRpc(int playerId, int setFocusLevel)
{if(!base.IsServer)			{
				observingScrap = null;
				observingPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
				focusLevel = setFocusLevel;
				observingObject = StartOfRound.Instance.allPlayerScripts[playerId].gameplayCamera.transform.gameObject;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetBabyObservingObjectServerRpc(NetworkObjectReference netObject)
			{
				SetBabyObservingObjectClientRpc(netObject);
			}

	[ClientRpc]
	public void SetBabyObservingObjectClientRpc(NetworkObjectReference netObject)
{if(netObject.TryGet(out var networkObject))		{
			observingObject = networkObject.gameObject;
			GrabbableObject component = observingObject.GetComponent<GrabbableObject>();
			if (component != null)
			{
				observingScrap = component;
			}
		}
}
	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if (noiseID == 6 || noiseID == 7 || (!base.IsServer && noiseID != 75) || isEnemyDead || noiseLoudness <= 0.1f || Time.realtimeSinceStartup - timeAtLastHeardNoise < 3f || Vector3.Distance(noisePosition, base.transform.position + Vector3.up * 0.4f) < 0.8f)
		{
			return;
		}
		float num = Vector3.Distance(noisePosition, base.transform.position);
		if (Physics.Linecast(base.transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			noiseLoudness *= 0.6f;
			num += 3f;
		}
		if (pingAttentionTimer > 0f)
		{
			if (focusLevel >= 3)
			{
				if (num > 7f || noiseLoudness <= 0.7f)
				{
					return;
				}
			}
			else if (focusLevel == 2)
			{
				if (num > 14f || noiseLoudness <= 0.4f)
				{
					return;
				}
			}
			else if (focusLevel <= 1 && (num > 20f || noiseLoudness <= 0.25f))
			{
				return;
			}
		}
		timeAtLastHeardNoise = Time.realtimeSinceStartup;
		int newFocusLevel = 1;
		Debug.Log($"Heard noise; noise loudness: {noiseLoudness}; noise distance: {num}");
		if (!base.IsServer || noiseID == 75 || ((!(num < 10f) || !(noiseLoudness >= 0.3f)) && (!(num < 14f) || !(noiseLoudness >= 0.85f))))
		{
			newFocusLevel = ((!(noiseLoudness > 0.6f)) ? 2 : 3);
		}
		else if (Time.realtimeSinceStartup - scareBabyWhileHoldingCooldown > 0.4f)
		{
			if (babyCrying)
			{
				ScareBaby(noisePosition);
				return;
			}
			scareBabyWhileHoldingCooldown = Time.realtimeSinceStartup;
			SetCryingLocalClient(setCrying: true);
			SetBabyCryingServerRpc(setCry: true);
			return;
		}
		PingAttention(newFocusLevel, 0.5f, noisePosition + Vector3.up * 0.6f);
	}

	private void BabyObserveAnimation()
	{
		bool num = babyRunning || babyCrying || rolledOver || Time.realtimeSinceStartup - timeSinceBiting < 0.75f;
		bool flag = observingObject != null && Physics.Linecast(BabyEye.transform.position, observingObject.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore);
		if (num || flag)
		{
			babyLookRig.weight = Mathf.Lerp(babyLookRig.weight, 0f, Time.deltaTime * 3f);
			agent.angularSpeed = 320f;
			return;
		}
		Vector3 vector;
		if (pingAttentionTimer > 0f && (observingObject == null || Vector3.Distance(observingObject.transform.position, pingAttentionPosition) > 1.6f))
		{
			vector = pingAttentionPosition - Vector3.up * lookVerticalOffset;
			Debug.DrawRay(vector, Vector3.up * 0.25f, Color.cyan);
		}
		else
		{
			if (!(observingObject != null) || holdingBaby)
			{
				babyLookRig.weight = Mathf.Lerp(babyLookRig.weight, 0f, Time.deltaTime * 3f);
				agent.angularSpeed = 320f;
				return;
			}
			vector = observingObject.transform.position - Vector3.up * lookVerticalOffset;
			Debug.DrawRay(vector, Vector3.up * 0.25f, Color.cyan);
		}
		babyLookRig.weight = Mathf.Lerp(babyLookRig.weight, 1f, Time.deltaTime * 8f);
		babyLookTarget.position = Vector3.Lerp(babyLookTarget.position, vector, Time.deltaTime * 8f);
		float num2 = Vector3.Angle(base.transform.forward, Vector3.Scale(new Vector3(1f, 0f, 1f), vector - base.transform.position));
		if (num2 > 10f)
		{
			if (!movedSinceLastCheck && !sittingDown)
			{
				agent.angularSpeed = 0f;
				if (Vector3.Dot(new Vector3(vector.x, base.transform.position.y, vector.z) - base.transform.position, base.transform.right) > 0f)
				{
					base.transform.rotation *= Quaternion.Euler(0f, 605f * Mathf.Max(num2 / 180f, 0.2f) * Time.deltaTime, 0f);
				}
				else
				{
					base.transform.rotation *= Quaternion.Euler(0f, -605f * Mathf.Max(num2 / 180f, 0.2f) * Time.deltaTime, 0f);
				}
			}
			else
			{
				agent.angularSpeed = 220f;
			}
		}
		else
		{
			agent.angularSpeed = 25f;
		}
	}

	private void SitCycle()
	{
		if (sittingDown)
		{
			if (Time.realtimeSinceStartup - currentActivityTimer > 12f + activityTimerOffset && !isInsidePlayerShip)
			{
				currentActivityTimer = Time.realtimeSinceStartup;
				activityTimerOffset = 7f;
				sittingDown = false;
				SetBabySittingServerRpc(sit: false);
			}
			else if (Time.realtimeSinceStartup - fallOverWhileSittingChanceInterval > 3f)
			{
				fallOverWhileSittingChanceInterval = Time.realtimeSinceStartup;
				if (UnityEngine.Random.Range(0, 100) < 10)
				{
					SetRolledOverLocalClient(setRolled: true, scared: false);
					SetBabyRolledOverServerRpc(rolledOver: true, scared: false);
				}
			}
		}
		else if (isInsidePlayerShip || Time.realtimeSinceStartup - currentActivityTimer > 14f + activityTimerOffset)
		{
			currentActivityTimer = Time.realtimeSinceStartup;
			activityTimerOffset = 8f;
			sittingDown = true;
			fallOverWhileSittingChanceInterval = Time.realtimeSinceStartup;
			SetBabySittingServerRpc(sit: true);
		}
	}

	private void StopObserving(bool eatScrap)
	{
		observingObject = null;
		observingScrap = null;
		observingPlayer = null;
		if (eatScrap)
		{
			scrapEaten++;
			babyCreatureAnimator.SetTrigger("Bite");
			babyVoice.PlayOneShot(biteSFX);
			timeSinceBiting = Time.realtimeSinceStartup;
			babyLookRig.weight = 0f;
			float num = Mathf.Lerp(1f, 1.6f, Mathf.Min(scrapEaten, 3f) / 3f);
			spine2.localScale = new Vector3(num, 1f, num);
		}
		ClearBabyObservingServerRpc(eatScrap);
	}

	private void SetCryingLocalClient(bool setCrying)
	{
		Debug.Log($"Baby crying set to {setCrying}");
		if (!babyCrying && setCrying)
		{
			Debug.Log("Baby crying set true; play");
			babyCrying = true;
			babyCryingAudio.Play();
			babyTearsParticle.Play(withChildren: true);
		}
		else if (!setCrying && babyCrying)
		{
			Debug.Log("Baby crying  set false");
			babyCrying = false;
			babyTearsParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
	}

	private void BabyUpdate()
	{
		babyCreatureAnimator.SetBool("Sitting", sittingDown);
		babyCreatureAnimator.SetBool("BabyRunning", babyRunning);
		babyCreatureAnimator.SetBool("HoldingBaby", holdingBaby);
		babyCreatureAnimator.SetBool("BabyCrying", babyCrying);
		babyCreatureAnimator.SetBool("Squirming", babySquirming);
		pingAttentionTimer -= Time.deltaTime;
		BabyObserveAnimation();
		if (StartOfRound.Instance.shipIsLeaving)
		{
			propScript.grabbable = false;
			if (playerHolding != null)
			{
				playerHolding.DiscardHeldObject();
			}
		}
		else if (!inSpecialAnimation)
		{
			propScript.grabbable = true;
		}
		if (babySquirming)
		{
			if (!babyVoice.isPlaying || babyVoice.clip != squirmingSFX)
			{
				babyVoice.clip = squirmingSFX;
				babyVoice.Play();
			}
		}
		else if (babyVoice.clip == squirmingSFX && babyVoice.isPlaying)
		{
			babyVoice.Stop();
		}
		if (babyCrying)
		{
			babyCryingAudio.pitch = Mathf.Lerp(babyCryingAudio.pitch, 1f, 2f * Time.deltaTime);
			babyCryingAudio.volume = Mathf.Lerp(babyCryingAudio.volume, 1f, 2f * Time.deltaTime);
		}
		else
		{
			babyCryingAudio.pitch = Mathf.Lerp(babyCryingAudio.pitch, 0.85f, 3f * Time.deltaTime);
			babyCryingAudio.volume = Mathf.Lerp(babyCryingAudio.volume, 0f, 3.6f * Time.deltaTime);
			if (babyCryingAudio.volume <= 0.07f && babyCryingAudio.isPlaying)
			{
				babyCryingAudio.Stop();
				if (!babyPuked && nearTransforming)
				{
					babyPuked = true;
					babyVoice.PlayOneShot(pukeSFX);
					WalkieTalkie.TransmitOneShotAudio(babyVoice, pukeSFX);
					babyFoamAtMouthParticle.Play(withChildren: true);
				}
			}
		}
		if (!base.IsServer)
		{
			return;
		}
		if (holdingBaby && playerHolding != null)
		{
			hasPlayerFoundBaby = true;
			if (Time.realtimeSinceStartup - scareBabyWhileHoldingCooldown > 0.4f && Time.realtimeSinceStartup - playerHolding.timeSinceTakingDamage < 0.38f)
			{
				scareBabyWhileHoldingCooldown = Time.realtimeSinceStartup;
				if (babyCrying)
				{
					ScareBaby(base.transform.position);
				}
				else
				{
					SetCryingLocalClient(setCrying: true);
					SetBabyCryingServerRpc(setCry: true);
				}
			}
		}
		if (babyCrying)
		{
			if (babySquirming)
			{
				babySquirming = false;
				SetBabySquirmingServerRpc(setSquirm: false);
			}
			if (!stopCryingWhenReleased && !isOutside)
			{
				lonelinessMeter = 0.7f;
				if (rockingBaby != 0 && babyState == BabyState.Running)
				{
					babyState = BabyState.Roaming;
				}
				if (rockingBaby == 1)
				{
					rockBabyTimer += Time.deltaTime * 0.7f;
				}
				else if (rockingBaby == 2)
				{
					rockBabyTimer += Time.deltaTime * 0.4f;
				}
				if (rockBabyTimer > 1f)
				{
					SetCryingLocalClient(setCrying: false);
					SetBabyCryingServerRpc(setCry: false);
				}
			}
		}
		else
		{
			rockBabyTimer = 0f;
			if (rockingBaby == 2)
			{
				stressMeter += Time.deltaTime * 0.8f;
				if (stressMeter >= 1f)
				{
					stressMeter = 0f;
					stopCryingWhenReleased = true;
					babyState = BabyState.Running;
					runningFromPosition = base.transform.position;
					farthestNodeFromRunningSpot = null;
					SetCryingLocalClient(setCrying: true);
					SetBabyCryingServerRpc(setCry: true);
				}
			}
			else if (rockingBaby == 1 && propScript.playerHeldBy != null && lonelinessMeter > 0.6f)
			{
				BabyPlayerMemory babyMemoryOfPlayer = GetBabyMemoryOfPlayer(propScript.playerHeldBy);
				babyMemoryOfPlayer.likeMeter = Mathf.Min(babyMemoryOfPlayer.likeMeter + Time.deltaTime * 0.035f);
			}
		}
		if (isOutside && !isInsidePlayerShip && !babyCrying)
		{
			SetCryingLocalClient(setCrying: true);
			SetBabyCryingServerRpc(setCry: true);
		}
		IncreaseBabyGrowthMeter();
		if (currentBehaviourStateIndex != 0)
		{
			return;
		}
		if (rolledOver)
		{
			rollOverTimer += Time.deltaTime;
			if (!babyCrying && rollOverTimer > 1.3f)
			{
				SetCryingLocalClient(setCrying: true);
				SetBabyCryingServerRpc(setCry: true);
			}
		}
		if (!holdingBaby)
		{
			if (babySquirming)
			{
				babySquirming = false;
				SetBabySquirmingServerRpc(setSquirm: false);
			}
			if (stopCryingWhenReleased)
			{
				stopCryingWhenReleased = false;
				SetCryingLocalClient(setCrying: false);
				SetBabyCryingServerRpc(setCry: false);
			}
			if (observingPlayer != null && Vector3.Distance(base.transform.position, observingPlayer.transform.position) < 12f)
			{
				BabyPlayerMemory babyMemoryOfPlayer2 = GetBabyMemoryOfPlayer(observingPlayer);
				lonelinessMeter = Mathf.Max(0f, lonelinessMeter - Time.deltaTime * (decreaseLonelinessMultiplier / Mathf.Max(babyMemoryOfPlayer2.likeMeter * 3f, 0.45f)) * moodinessMultiplier);
			}
			else
			{
				if (!hasPlayerFoundBaby)
				{
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
					{
						if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.25f, 140f, 50, 5f))
						{
							hasPlayerFoundBaby = true;
							break;
						}
					}
				}
				if (playersSeen > 0 || hasPlayerFoundBaby)
				{
					lonelinessMeter = Mathf.Min(1f, lonelinessMeter + Time.deltaTime * increaseLonelinessMultiplier * moodinessMultiplier);
				}
				else
				{
					lonelinessMeter = Mathf.Min(1f, lonelinessMeter + Time.deltaTime * 0.01f * moodinessMultiplier);
				}
			}
			if (holdingBaby)
			{
				return;
			}
			switch (babyState)
			{
			case BabyState.Roaming:
				if (previousBabyState != (int)babyState)
				{
					currentActivityTimer = Time.realtimeSinceStartup;
					activityTimerOffset = UnityEngine.Random.Range(-10f, 10f);
					leapTimer = 0f;
					if ((bool)observingObject)
					{
						StopObserving(eatScrap: false);
					}
					eatingScrap = false;
					if (sittingDown)
					{
						sittingDown = false;
						SetBabySittingServerRpc(sit: false);
					}
					if (babyRunning)
					{
						babyRunning = false;
						SetBabyRunningServerRpc(setRunning: false);
					}
					previousBabyState = (int)babyState;
				}
				if (playersSeen == 0)
				{
					if (pathingTowardsNearestPlayer)
					{
						if (Time.realtimeSinceStartup - currentActivityTimer > 20f + activityTimerOffset)
						{
							currentActivityTimer = Time.realtimeSinceStartup;
							pathingTowardsNearestPlayer = false;
						}
					}
					else if (Time.realtimeSinceStartup - currentActivityTimer > 26f)
					{
						currentActivityTimer = Time.realtimeSinceStartup;
						activityTimerOffset = UnityEngine.Random.Range(-6f, 6f);
						pathingTowardsNearestPlayer = true;
					}
				}
				else
				{
					pathingTowardsNearestPlayer = false;
					if (lonelinessMeter >= 0.99f && !babyCrying)
					{
						SetCryingLocalClient(setCrying: true);
						SetBabyCryingServerRpc(setCry: true);
					}
					if (babyCrying)
					{
						if (sittingDown)
						{
							sittingDown = false;
							SetBabySittingServerRpc(sit: false);
						}
					}
					else
					{
						SitCycle();
					}
				}
				if (observingObject != null && !eatingScrap)
				{
					timeSpentObservingTimer += Time.deltaTime;
					if (timeSpentObservingTimer > 3f)
					{
						timeSpentObservingTimer = 0f;
						StopObserving(eatScrap: false);
					}
				}
				break;
			case BabyState.Observing:
			{
				if (previousBabyState != (int)babyState)
				{
					timeSpentObservingTimer = 0f;
					cantFindObservedObjectTimer = 0f;
					currentActivityTimer = Time.realtimeSinceStartup;
					eatingScrap = false;
					leapTimer = 0f;
					if (babyRunning)
					{
						babyRunning = false;
						SetBabyRunningServerRpc(setRunning: false);
					}
					previousBabyState = (int)babyState;
				}
				if (eatingScrap)
				{
					break;
				}
				BabyPlayerMemory babyPlayerMemory = null;
				if (observingPlayer != null)
				{
					babyPlayerMemory = GetBabyMemoryOfPlayer(observingPlayer);
				}
				float num = 4.7f;
				if ((bool)observingScrap)
				{
					num = 2f;
				}
				else if ((bool)observingPlayer && babyPlayerMemory != null && babyPlayerMemory.likeMeter < 0.25f && lonelinessMeter < 0.75f)
				{
					num = 6.7f;
				}
				float num2 = Vector3.Distance(base.transform.position, observingObject.transform.position);
				if (num - num2 > 1.5f)
				{
					Ray ray = new Ray(base.transform.position, base.transform.position + Vector3.up * 0.2f - observingObject.transform.position + Vector3.up * 0.2f);
					ray.direction = new Vector3(ray.direction.x, 0f, ray.direction.z);
					Debug.DrawRay(base.transform.position, ray.direction, Color.cyan);
					Vector3 pos = ((!Physics.Raycast(ray, out hit, 10f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(10f) : hit.point);
					if (babyPlayerMemory != null && babyPlayerMemory.likeMeter < 0.25f)
					{
						babyPlayerMemory.likeMeter = Mathf.Max(babyPlayerMemory.likeMeter - AIIntervalTime * 0.2f, -0.5f);
						stressMeter = Mathf.Min(stressMeter + AIIntervalTime, 1f);
						if (stressMeter > 1f)
						{
							stressMeter = 0f;
							babyPlayerMemory.likeMeter = -0.2f;
							ScareBaby(observingObject.transform.position);
						}
					}
					if (sittingDown)
					{
						sittingDown = false;
						SetBabySittingServerRpc(sit: false);
					}
					pos = RoundManager.Instance.GetNavMeshPosition(pos, default(NavMeshHit), 6f);
					agent.speed = 4.6f;
					SetDestinationToPosition(pos);
				}
				else if (num2 > num)
				{
					if (sittingDown)
					{
						sittingDown = false;
						SetBabySittingServerRpc(sit: false);
					}
					agent.speed = 4.2f;
					SetDestinationToPosition(observingObject.transform.position);
				}
				else
				{
					SitCycle();
					agent.speed = 0f;
				}
				if (observingPlayer != null)
				{
					timeSpentObservingTimer += Time.deltaTime;
					if (lonelinessMeter < babyRoamFromPlayersThreshold && timeSpentObservingTimer > 6f)
					{
						babyState = BabyState.Roaming;
					}
				}
				else
				{
					timeSpentObservingTimer += Time.deltaTime;
					if (timeSpentObservingTimer > 12f || lonelinessMeter > 0.88f)
					{
						babyState = BabyState.Roaming;
					}
				}
				break;
			}
			case BabyState.Running:
				if (previousBabyState != (int)babyState)
				{
					currentActivityTimer = Time.realtimeSinceStartup;
					farthestNodeFromRunningSpot = null;
					stressMeter = 0f;
					if ((bool)observingObject)
					{
						StopObserving(eatScrap: false);
					}
					eatingScrap = false;
					if (sittingDown)
					{
						sittingDown = false;
						SetBabySittingServerRpc(sit: false);
					}
					babyRunning = true;
					previousBabyState = (int)babyState;
				}
				if (gettingFarthestNodeFromSpotAsync && farthestNodeFromRunningSpot == null)
				{
					Transform transform = ChooseFarthestNodeFromPosition(runningFromPosition, avoidLineOfSight: false, 0, doAsync: true);
					if (gotFarthestNodeAsync)
					{
						farthestNodeFromRunningSpot = transform;
						gettingFarthestNodeFromSpotAsync = false;
					}
				}
				break;
			case BabyState.RolledOver:
				if (previousBabyState != (int)babyState)
				{
					currentActivityTimer = Time.realtimeSinceStartup;
					farthestNodeFromRunningSpot = null;
					stressMeter = 0f;
					if ((bool)observingObject)
					{
						StopObserving(eatScrap: false);
					}
					eatingScrap = false;
					previousBabyState = (int)babyState;
				}
				break;
			}
			return;
		}
		BabyPlayerMemory babyMemoryOfPlayer3 = GetBabyMemoryOfPlayer(playerHolding);
		if (babySearchRoutine.inProgress)
		{
			StopSearch(babySearchRoutine);
		}
		if (!babyCrying)
		{
			if (babyMemoryOfPlayer3.likeMeter < 0.1f)
			{
				lonelinessMeter = Mathf.Max(0f, lonelinessMeter - Time.deltaTime * 0.025f);
			}
			else
			{
				lonelinessMeter = 0.75f;
			}
			if (lonelinessMeter < babyCrySquirmingThreshold)
			{
				stopCryingWhenReleased = true;
				SetCryingLocalClient(setCrying: true);
				SetBabyCryingServerRpc(setCry: true);
			}
			else if (!babySquirming && lonelinessMeter < babyRoamFromPlayersThreshold)
			{
				babySquirming = true;
				SetBabySquirmingServerRpc(setSquirm: true);
			}
		}
	}

	private Vector3 GetCavePosition(System.Random randomSeed)
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("CaveNode");
		if (array == null || array.Length == 0)
		{
			return ChooseFarthestNodeFromPosition(RoundManager.FindMainEntrancePosition(), avoidLineOfSight: false, randomSeed.Next(0, Mathf.Min(allAINodes.Length, 40))).position;
		}
		List<GameObject> list = array.ToList();
		Transform transform = list[0].transform;
		for (int i = 0; i < 15; i++)
		{
			if (list.Count == 0)
			{
				break;
			}
			int index = randomSeed.Next(0, list.Count);
			transform = list[index].transform;
			if (!PathIsIntersectedByLineOfSight(transform.position, calculatePathDistance: false, avoidLineOfSight: false))
			{
				return transform.position;
			}
			list.Remove(list[index]);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				if (Vector3.Distance(list[num].transform.position, transform.position) < 30f)
				{
					list.Remove(list[num]);
				}
			}
		}
		return ChooseClosestNodeToPosition(base.transform.position).position;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncCaveHidingSpotServerRpc(Vector3 setHidingSpot)
			{
				SyncCaveHidingSpotClientRpc(setHidingSpot);
			}

	[ClientRpc]
	public void SyncCaveHidingSpotClientRpc(Vector3 setHidingSpot)
{if(!base.IsServer)			{
				caveHidingSpot = setHidingSpot;
			}
}
	public override void Start()
	{
		base.Start();
		currentSearchWidth = baseSearchWidth;
		if (base.IsServer)
		{
			System.Random randomSeed = new System.Random(StartOfRound.Instance.randomMapSeed + 249);
			caveHidingSpot = GetCavePosition(randomSeed);
			SyncCaveHidingSpotServerRpc(caveHidingSpot);
		}
		InitializeBabyValues();
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!(leapTimer < 0.02f) && currentBehaviourStateIndex == 3 && !isEnemyDead)
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation || startingKillAnimationLocalClient);
			if (playerControllerB != null)
			{
				KillPlayerAnimationServerRpc((int)playerControllerB.playerClientId);
				startingKillAnimationLocalClient = true;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerAnimationServerRpc(int playerObjectId)
{		{
			if (!inKillAnimation)
			{
				inKillAnimation = true;
				KillPlayerAnimationClientRpc(playerObjectId);
			}
			else
			{
				CancelKillAnimationClientRpc(playerObjectId);
			}
		}
}
	[ClientRpc]
	public void CancelKillAnimationClientRpc(int playerObjectId)
{if((int)GameNetworkManager.Instance.localPlayerController.playerClientId == playerObjectId)			{
				startingKillAnimationLocalClient = false;
			}
}
	[ClientRpc]
	public void KillPlayerAnimationClientRpc(int playerObjectId)
{		{
			PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObjectId];
			if (playerControllerB == null || playerControllerB.isPlayerDead || !playerControllerB.isInsideFactory)
			{
				FinishKillAnimation(completed: false);
			}
			if (currentBehaviourStateIndex == 3)
			{
				chasingAfterLeap = true;
				headRig.weight = 0f;
				leaping = false;
				screaming = false;
				screamTimer = screamTime;
				creatureAnimator.SetBool("Leaping", value: false);
				creatureAnimator.SetBool("Screaming", value: false);
				startedLeapingThisFrame = false;
			}
			if (playerControllerB == GameNetworkManager.Instance.localPlayerController)
			{
				playerControllerB.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Mauling);
				startingKillAnimationLocalClient = false;
			}
			if (base.IsOwner)
			{
				agent.speed = 0f;
			}
			inKillAnimation = true;
			creatureAnimator.SetBool("killing", value: true);
			if (killAnimationCoroutine != null)
			{
				StopCoroutine(killAnimationCoroutine);
			}
			killAnimationCoroutine = StartCoroutine(killAnimation(playerControllerB));
		}
}
	private IEnumerator killAnimation(PlayerControllerB killingPlayer)
	{
		float timeAtStart = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => Time.realtimeSinceStartup - timeAtStart > 2f || killingPlayer.deadBody != null);
		if (killingPlayer.deadBody != null)
		{
			GrabBody(killingPlayer.deadBody);
			yield return new WaitForSeconds(0.5f);
			killPlayerParticle1.Play();
			if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) < 10f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			}
			yield return new WaitForSeconds(1.5f);
			killPlayerParticle2.Play();
			if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) < 10f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
			yield return new WaitForSeconds(0.25f);
			FinishKillAnimation(completed: true);
		}
		else
		{
			FinishKillAnimation(completed: false);
		}
	}

	private void GrabBody(DeadBodyInfo body)
	{
		bodyBeingCarried = body;
		bodyBeingCarried.attachedTo = bodyRagdollPoint;
		bodyBeingCarried.attachedLimb = bodyBeingCarried.bodyParts[5];
		bodyBeingCarried.matchPositionExactly = false;
	}

	private void DropBody()
	{
		if (bodyBeingCarried != null)
		{
			bodyBeingCarried.attachedTo = null;
			bodyBeingCarried.attachedLimb = null;
			bodyBeingCarried.matchPositionExactly = false;
			bodyBeingCarried = null;
		}
	}

	public void FinishKillAnimation(bool completed)
	{
		inKillAnimation = false;
		inSpecialAnimation = false;
		creatureAnimator.SetBool("killing", value: false);
		headRig.weight = 1f;
		DropBody();
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		if (checkMovingInterval <= 0f)
		{
			checkMovingInterval = 0.1f;
			bool flag = Vector3.Distance(base.transform.position, previousPosition) > 0.06f;
			if (currentBehaviourStateIndex == 0)
			{
				babyCreatureAnimator.SetBool("moving", flag);
			}
			else
			{
				creatureAnimator.SetBool("moving", flag);
				if (flag && !leaping)
				{
					walkingAudio.volume = Mathf.Lerp(walkingAudio.volume, 1f, 5f * Time.deltaTime);
					if (!walkingAudio.isPlaying)
					{
						walkingAudio.Play();
					}
				}
				else
				{
					if (walkingAudio.isPlaying && walkingAudio.volume <= 0.05f)
					{
						walkingAudio.Pause();
					}
					walkingAudio.volume = Mathf.Lerp(walkingAudio.volume, 0f, 5f * Time.deltaTime);
				}
			}
			previousPosition = base.transform.position;
		}
		else
		{
			checkMovingInterval -= Time.deltaTime;
		}
	}

	public override void Update()
	{
		base.Update();
		if (inKillAnimation || isEnemyDead || StartOfRound.Instance.livingPlayers == 0)
		{
			return;
		}
		CalculateAnimationDirection(1.6f);
		SetClickingAudioVolume();
		if (!base.IsOwner && ((!holdingBaby && dropBabyCoroutine == null) || currentBehaviourStateIndex != 0))
		{
			base.transform.localEulerAngles = new Vector3(0f, base.transform.localEulerAngles.y, 0f);
		}
		if (wasOutsideLastFrame && base.transform.position.y < -80f)
		{
			wasOutsideLastFrame = false;
			SetEnemyOutside();
		}
		else if (!wasOutsideLastFrame && base.transform.position.y > -80f)
		{
			wasOutsideLastFrame = true;
			SetEnemyOutside(outside: true);
		}
		if (currentBehaviourStateIndex == 0)
		{
			if (base.IsServer && (holdingBaby || rolledOver))
			{
				if (babySearchRoutine.inProgress)
				{
					StopSearch(babySearchRoutine);
				}
				agent.speed = 0f;
			}
			BabyUpdate();
			return;
		}
		if (base.IsServer)
		{
			if (changeOwnershipInterval <= 0f)
			{
				changeOwnershipInterval = 0.3f;
				PlayerControllerB playerControllerB = targetPlayer;
				targetPlayer = null;
				float num = 4f;
				mostOptimalDistance = 2000f;
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
				{
					if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]))
					{
						tempDist = Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position);
						if (tempDist < mostOptimalDistance)
						{
							mostOptimalDistance = tempDist;
							targetPlayer = StartOfRound.Instance.allPlayerScripts[i];
						}
					}
				}
				if (targetPlayer != null && num > 0f && playerControllerB != null && Mathf.Abs(mostOptimalDistance - Vector3.Distance(base.transform.position, playerControllerB.transform.position)) < num)
				{
					targetPlayer = playerControllerB;
				}
				if (targetPlayer != null && !inKillAnimation && targetPlayer.actualClientId != base.NetworkObject.OwnerClientId && (chasingAfterLeap || currentBehaviourStateIndex != 3))
				{
					ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
					return;
				}
			}
			else
			{
				changeOwnershipInterval -= Time.deltaTime;
			}
		}
		DoNonBabyUpdateLogic();
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (inKillAnimation || isEnemyDead || inSpecialAnimation)
		{
			return;
		}
		if (StartOfRound.Instance.livingPlayers == 0)
		{
			base.DoAIInterval();
		}
		else if (currentBehaviourStateIndex == 0)
		{
			if (base.IsServer)
			{
				DoBabyAIInterval();
			}
		}
		else
		{
			DoAIIntervalChaseLogic();
		}
	}

	private void TransformIntoAdult()
	{
		if (base.IsServer)
		{
			SwitchToBehaviourStateOnLocalClient(1);
			TurnIntoAdultServerRpc();
			StartTransformationAnim();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TurnIntoAdultServerRpc()
			{
				TurnIntoAdultClientRpc();
			}

	[ClientRpc]
	public void TurnIntoAdultClientRpc()
{if(!base.IsServer)			{
				SwitchToBehaviourStateOnLocalClient(1);
				StartTransformationAnim();
			}
}
	private void StartTransformationAnim()
	{
		agent.acceleration = 425f;
		agent.angularSpeed = 700f;
		syncMovementSpeed = 0.15f;
		addPlayerVelocityToDestination = 1f;
		updatePositionThreshold = 0.8f;
		propScript.EnablePhysics(enable: false);
		propScript.grabbable = false;
		propScript.grabbableToEnemies = false;
		propScript.enabled = false;
		if (propScript.playerHeldBy != null && propScript.playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			propScript.playerHeldBy.DropAllHeldItemsAndSync();
		}
		if (dropBabyCoroutine != null)
		{
			StopCoroutine(dropBabyCoroutine);
		}
		if (babySearchRoutine.inProgress)
		{
			StopSearch(babySearchRoutine);
		}
		Vector3 position = base.transform.position;
		inSpecialAnimation = true;
		agent.enabled = false;
		Vector3 vector = (serverPosition = RoundManager.Instance.GetNavMeshPosition(position, default(NavMeshHit), 10f));
		base.transform.position = vector;
		base.transform.localEulerAngles = new Vector3(0f, base.transform.localEulerAngles.y, 0f);
		position = vector;
		Debug.DrawRay(vector, Vector3.up * 2f, Color.magenta, 7f);
		StartCoroutine(becomeAdultAnimation(position));
	}

	private IEnumerator becomeAdultAnimation(Vector3 setToPos)
	{
		creatureSFX.volume = 1f;
		creatureSFX.PlayOneShot(transformationSFX);
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, transformationSFX);
		babyCreatureAnimator.SetBool("Transform", value: true);
		yield return new WaitForSeconds(0.5f);
		babyContainer.SetActive(value: false);
		adultContainer.SetActive(value: true);
		yield return new WaitForSeconds(1.7f);
		inSpecialAnimation = false;
		if (isOutside)
		{
			leapSpeed += 8f;
		}
		if (base.IsOwner)
		{
			base.transform.position = setToPos;
			agent.enabled = true;
		}
		Debug.DrawRay(setToPos, Vector3.up * 2f, Color.magenta, 7f);
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetFakingBabyVoiceServerRpc(bool fakingBabyVoice)
			{
				SetFakingBabyVoiceClientRpc(fakingBabyVoice);
			}

	[ClientRpc]
	public void SetFakingBabyVoiceClientRpc(bool faking)
{if(!base.IsOwner)			{
				isFakingBabyVoice = faking;
			}
}
	private void DoNonBabyUpdateLogic()
	{
		if (!isEnemyDead && GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.5f, 100f, 30, 3f))
		{
			GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.7f);
		}
		if (base.IsOwner && Time.realtimeSinceStartup - checkIfTrappedInterval > 15f)
		{
			checkIfTrappedInterval = Time.realtimeSinceStartup;
			if (ChooseClosestNodeToPosition(base.transform.position) == nodesTempArray[0].transform && PathIsIntersectedByLineOfSight(nodesTempArray[0].transform.position, calculatePathDistance: false, avoidLineOfSight: false))
			{
				bool flag = false;
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
				{
					if (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 1.3f, 100f, 70, 12f))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					agent.enabled = false;
					base.transform.position = RoundManager.Instance.GetNavMeshPosition(ChooseClosestNodeToPositionNoPathCheck(base.transform.position).position);
					return;
				}
			}
		}
		switch (currentBehaviourStateIndex)
		{
		case 1:
			if (currentBehaviourStateIndex != previousBehaviourState)
			{
				screamTimer = screamTime;
				creatureAnimator.SetBool("Screaming", value: false);
				creatureAnimator.SetBool("Leaping", value: false);
				creatureAnimator.SetBool("FinishedLeaping", value: false);
				useSecondaryAudiosOnAnimatedObjects = false;
				openDoorSpeedMultiplier = 1f;
				beganCooldown = true;
				screaming = false;
				leaping = false;
				chasingAfterLeap = false;
				headRig.weight = 0f;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			agent.speed = 6f;
			break;
		case 2:
		{
			if (currentBehaviourStateIndex != previousBehaviourState)
			{
				screamTimer = screamTime;
				ignoredNodes.Clear();
				creatureAnimator.SetBool("Screaming", value: false);
				creatureAnimator.SetBool("Leaping", value: false);
				creatureAnimator.SetBool("FinishedLeaping", value: false);
				useSecondaryAudiosOnAnimatedObjects = false;
				openDoorSpeedMultiplier = 1f;
				beganCooldown = true;
				screaming = false;
				leaping = false;
				chasingAfterLeap = false;
				headRig.weight = 0f;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (!base.IsOwner || targetNode == null)
			{
				break;
			}
			bool flag2 = IsBodyVisibleToTarget();
			if (!flag2 && targetNode != null)
			{
				if (wasBodyVisible)
				{
					wasBodyVisible = false;
					positionAtLeavingLOS = base.transform.position;
				}
				if (Vector3.Distance(base.transform.position, targetNode.transform.position) < 1f)
				{
					agent.speed = 0f;
				}
				else
				{
					agent.speed = sneakSpeed;
				}
			}
			else if (flag2)
			{
				if (!wasBodyVisible)
				{
					wasBodyVisible = true;
				}
				agent.speed = sneakSpeed + 4f;
			}
			else
			{
				agent.speed = sneakSpeed;
			}
			if (!wasBodyVisible && Vector3.Distance(base.transform.position, positionAtLeavingLOS) > 7f)
			{
				Debug.DrawRay(positionAtLeavingLOS, Vector3.up, Color.yellow, 0.7f);
				ignoredNodes.Clear();
			}
			if (Vector3.Distance(base.transform.position, targetNode.transform.position) < 0.5f && flag2 && !ignoredNodes.Contains(targetNode))
			{
				if (ignoredNodes.Count < 16)
				{
					ignoredNodes.Add(targetNode);
					break;
				}
				ignoredNodes.RemoveAt(0);
				ignoredNodes.Add(targetNode);
			}
			break;
		}
		case 3:
			if (currentBehaviourStateIndex != previousBehaviourState)
			{
				if (TargetClosestPlayer())
				{
					SetMovingTowardsTargetPlayer(targetPlayer);
				}
				useSecondaryAudiosOnAnimatedObjects = true;
				openDoorSpeedMultiplier = 1.5f;
				currentSearchWidth = Mathf.Max(currentSearchWidth - 35f, baseSearchWidth);
				startedLeapingThisFrame = false;
				headRig.weight = 0f;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (leaping && leapTimer > 0.1f)
			{
				if (shakeCameraInterval <= 0f && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) < 12f)
				{
					shakeCameraInterval = 0.05f;
					HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
				}
				else
				{
					shakeCameraInterval -= Time.deltaTime;
				}
			}
			if (!base.IsOwner)
			{
				screaming = false;
				startedLeapingThisFrame = false;
				leapTimer = Mathf.Max(leapTimer - Time.deltaTime, 0f);
				if (leapTimer <= 0f && !beganCooldown)
				{
					beganCooldown = true;
					creatureVoice.PlayOneShot(cooldownSFX);
					creatureAnimator.SetBool("FinishedLeaping", value: true);
				}
				if (stunNormalizedTimer > 0f)
				{
					leapTimer = 0f;
				}
				if (leaping && leapTimer <= 0f)
				{
					leaping = false;
					creatureAnimator.SetBool("Leaping", value: false);
					creatureAnimator.SetBool("Screaming", value: false);
				}
				screamTimer -= Time.deltaTime;
			}
			else
			{
				if (targetPlayer == null)
				{
					break;
				}
				float num = 15f;
				if (isOutside)
				{
					num = 35f;
				}
				if (chasingAfterLeap)
				{
					if (Physics.Linecast(targetPlayer.gameplayCamera.transform.position, base.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && (Vector3.Distance(targetPlayer.transform.position, base.transform.position) > num || (!isOutside && Vector3.Distance(caveHidingSpot, base.transform.position) > currentSearch.searchWidth + 30f)))
					{
						SwitchToBehaviourState(2);
						break;
					}
					agent.speed = chaseSpeed;
					float num2 = attackDistance - 3f;
					if (isOutside)
					{
						num2 += 18f;
					}
					if (Vector3.Distance(targetPlayer.transform.position, base.transform.position) < num2)
					{
						chasingAfterLeap = false;
					}
					break;
				}
				if (!screaming)
				{
					headRig.weight = 0f;
					beganCooldown = false;
					creatureAnimator.SetBool("Screaming", value: true);
					creatureAnimator.SetBool("FinishedLeaping", value: false);
					screaming = true;
					agent.speed = 0f;
					isFakingBabyVoice = false;
					creatureVoice.PlayOneShot(growlSFX);
					WalkieTalkie.TransmitOneShotAudio(creatureVoice, growlSFX);
					DoScreamServerRpc();
				}
				if (leaping)
				{
					leapTimer -= Time.deltaTime;
					if (!startedLeapingThisFrame)
					{
						startedLeapingThisFrame = true;
						creatureAnimator.SetBool("Leaping", value: true);
						float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
						screamAudio.pitch = pitch;
						screamAudio.Play();
						screamAudioNonDiagetic.pitch = pitch;
						screamAudioNonDiagetic.Play();
						leapTimer = leapTime;
						DoLeapServerRpc();
					}
					if (stunNormalizedTimer > 0f)
					{
						leapTimer = 0f;
					}
					else
					{
						agent.speed = leapSpeed;
					}
					if (leapTimer <= 0f)
					{
						agent.speed = 0f;
						screamTimer += Time.deltaTime;
						if (screamTimer > cooldownTime)
						{
							chasingAfterLeap = true;
							headRig.weight = 1f;
							leaping = false;
							screaming = false;
							screamTimer = screamTime;
							creatureAnimator.SetBool("Leaping", value: false);
							creatureAnimator.SetBool("Screaming", value: false);
							creatureAnimator.SetBool("FinishedLeaping", value: false);
							startedLeapingThisFrame = false;
							FinishLeapServerRpc();
						}
						else if (!beganCooldown)
						{
							beganCooldown = true;
							creatureVoice.PlayOneShot(cooldownSFX);
							creatureAnimator.SetBool("FinishedLeaping", value: true);
						}
					}
					else
					{
						agent.speed = leapSpeed;
					}
				}
				else
				{
					screamTimer -= Time.deltaTime;
					if (screamTimer <= 0f)
					{
						leaping = true;
						break;
					}
					agent.speed = 0f;
					LookAtTargetPlayer();
				}
			}
			break;
		}
	}

	private void LookAtTargetPlayer()
	{
		if (!(targetPlayer == null))
		{
			Vector3 position = targetPlayer.transform.position;
			position.y = base.transform.position.y;
			RoundManager.Instance.tempTransform.position = base.transform.position;
			RoundManager.Instance.tempTransform.LookAt(position);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, RoundManager.Instance.tempTransform.rotation, 14f * Time.deltaTime);
		}
	}

	private void DoAIIntervalChaseLogic()
	{
		if (inSpecialAnimation || inKillAnimation)
		{
			return;
		}
		bool flag = TargetClosestPlayer(4f);
		if (currentBehaviourStateIndex == 3)
		{
			if (searchRoutine.inProgress)
			{
				StopSearch(searchRoutine);
			}
			if (TargetClosestPlayer())
			{
				noPlayersTimer = 0f;
				SetMovingTowardsTargetPlayer(targetPlayer);
			}
			else if (noPlayersTimer > 2.5f)
			{
				SwitchToBehaviourState(1);
			}
			else
			{
				noPlayersTimer += AIIntervalTime;
			}
			return;
		}
		fakeCryTimer -= AIIntervalTime;
		if (isFakingBabyVoice)
		{
			if (fakeCryTimer <= 0f)
			{
				fakeCryTimer = Mathf.Max(UnityEngine.Random.Range(-2f, 7f), 4.2f);
				clickingMandibles = false;
				int num = UnityEngine.Random.Range(0, fakeCrySFX.Length);
				DoFakeCryLocalClient(num);
				MakeFakeCryServerRpc(num);
			}
		}
		else if (fakeCryTimer <= 0f)
		{
			if (UnityEngine.Random.Range(0, 100) < 65)
			{
				fakeCryTimer = 9f;
				clickingMandibles = true;
				SetClickingMandiblesServerRpc();
			}
			else
			{
				isFakingBabyVoice = true;
				SetFakingBabyVoiceServerRpc(fakingBabyVoice: true);
			}
		}
		if (!flag)
		{
			if (noPlayersTimer > 2.5f)
			{
				RoamAroundCaveSpot(gotTarget: false);
			}
			else
			{
				noPlayersTimer += AIIntervalTime;
			}
			return;
		}
		noPlayersTimer = 0f;
		float num2 = ((!isOutside) ? Vector3.Distance(targetPlayer.transform.position, caveHidingSpot) : Vector3.Distance(targetPlayer.transform.position, base.transform.position));
		float num3 = attackDistance;
		if (isOutside)
		{
			num3 += 16f;
		}
		float num4 = Vector3.Distance(targetPlayer.transform.position, base.transform.position);
		if ((num4 < num3 && !Physics.Linecast(base.transform.position + Vector3.up * 0.25f, targetPlayer.transform.position + Vector3.up * 0.25f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) || num4 < 4.5f)
		{
			SwitchToBehaviourState(3);
		}
		else if (pursuingPlayerInSneakMode)
		{
			if (num2 > searchRoutine.searchWidth + 30f)
			{
				pursuingPlayerInSneakMode = false;
			}
			if (searchRoutine.inProgress)
			{
				StopSearch(searchRoutine);
			}
			if (currentBehaviourStateIndex == 1)
			{
				SwitchToBehaviourState(2);
			}
			if (isFakingBabyVoice && targetPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.75f, 90f, 20, 2f))
			{
				isFakingBabyVoice = false;
				fakeCryTimer = 18f;
				SetFakingBabyVoiceServerRpc(fakingBabyVoice: false);
				if (UnityEngine.Random.Range(0, 100) < 30)
				{
					clickingMandibles = true;
					SetClickingMandiblesServerRpc();
				}
			}
			ChooseClosestNodeToPlayer();
		}
		else
		{
			if (num2 < searchRoutine.searchWidth + 15f)
			{
				pursuingPlayerInSneakMode = true;
			}
			RoamAroundCaveSpot(gotTarget: true);
		}
	}

	private void DoFakeCryLocalClient(int fakeCryIndex)
	{
		clickingMandibles = false;
		creatureVoice.PlayOneShot(fakeCrySFX[fakeCryIndex], 1f);
		WalkieTalkie.TransmitOneShotAudio(creatureVoice, fakeCrySFX[fakeCryIndex]);
	}

	[ServerRpc(RequireOwnership = false)]
	public void MakeFakeCryServerRpc(int clipIndex)
			{
				MakeFakeCryClientRpc(clipIndex);
			}

	[ClientRpc]
	public void MakeFakeCryClientRpc(int clipIndex)
{if(!base.IsOwner)			{
				DoFakeCryLocalClient(clipIndex);
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void SetClickingMandiblesServerRpc()
			{
				SetClickingMandiblesClientRpc();
			}

	[ClientRpc]
	public void SetClickingMandiblesClientRpc()
{if(!base.IsOwner)			{
				clickingMandibles = true;
				fakeCryTimer = 9f;
			}
}
	private void RoamAroundCaveSpot(bool gotTarget, float distToTarget = -1f)
	{
		if (currentBehaviourStateIndex == 2)
		{
			SwitchToBehaviourState(1);
		}
		if (!gotTarget)
		{
			if (isOutside)
			{
				if (!searchRoutine.inProgress)
				{
					Transform transform = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: false, UnityEngine.Random.Range(0, allAINodes.Length / 2));
					searchRoutine.searchPrecision = 16f;
					if (transform != null)
					{
						StartSearch(transform.position, searchRoutine);
					}
					currentSearchWidth = 200f;
				}
			}
			else
			{
				if (searchRoutine.inProgress)
				{
					StopSearch(searchRoutine);
				}
				SetDestinationToPosition(caveHidingSpot);
				currentSearchWidth = Mathf.Max(currentSearchWidth - AIIntervalTime * 1.5f, baseSearchWidth);
			}
			return;
		}
		if (isOutside)
		{
			currentSearchWidth = 100f;
		}
		else if (currentSearchWidth - Vector3.Distance(caveHidingSpot, targetPlayer.transform.position) < 16f)
		{
			currentSearchWidth = Mathf.Min(currentSearchWidth + AIIntervalTime * 1.7f, baseSearchWidth + 80f);
		}
		searchRoutine.searchWidth = currentSearchWidth;
		if (searchRoutine.inProgress)
		{
			return;
		}
		if (isOutside)
		{
			Transform transform2 = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: false, UnityEngine.Random.Range(0, allAINodes.Length / 2));
			if (transform2 != null)
			{
				StartSearch(transform2.position, searchRoutine);
			}
		}
		else
		{
			StartSearch(caveHidingSpot, searchRoutine);
		}
	}

	public void ChooseClosestNodeToPlayer()
	{
		if (targetNode == null)
		{
			targetNode = allAINodes[0].transform;
		}
		Transform transform = ChooseClosestNodeToPositionNoLOS(targetPlayer.transform.position + Vector3.up, avoidLineOfSight: true);
		if (transform != null)
		{
			targetNode = transform;
		}
		SetDestinationToPosition(targetNode.position);
	}

	public Transform ChooseClosestNodeToPositionNoLOS(Vector3 pos, bool avoidLineOfSight = false, int offset = 0)
	{
		nodesTempArray = allAINodes.OrderBy((GameObject x) => Vector3.Distance(pos, x.transform.position)).ToArray();
		Transform result = nodesTempArray[0].transform;
		float num = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
		bool flag = false;
		for (int i = 0; i < nodesTempArray.Length; i++)
		{
			if (ignoredNodes.Contains(nodesTempArray[i].transform) || Vector3.Distance(caveHidingSpot, nodesTempArray[i].transform.position) > searchRoutine.searchWidth || Vector3.Distance(base.transform.position, nodesTempArray[i].transform.position) > 40f || !Physics.Linecast(nodesTempArray[i].transform.position + Vector3.up, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				continue;
			}
			bool flag2 = !wasBodyVisible;
			if (PathIsIntersectedByLineOfSight(nodesTempArray[i].transform.position, calculatePathDistance: true, flag2, flag2) || (pathDistance > num + 16f && !wasBodyVisible))
			{
				continue;
			}
			if (wasBodyVisible)
			{
				bool flag3 = false;
				for (int j = 1; j < path1.corners.Length; j++)
				{
					if (Vector3.Distance(Vector3.Lerp(path1.corners[j - 1], path1.corners[j], 0.5f), targetPlayer.transform.position) < 8f || Vector3.Distance(path1.corners[j], targetPlayer.transform.position) < 8f || num - Vector3.Distance(path1.corners[j], targetPlayer.transform.position) > 4f)
					{
						flag3 = true;
						break;
					}
				}
				if (flag3)
				{
					continue;
				}
			}
			mostOptimalDistance = Vector3.Distance(pos, nodesTempArray[i].transform.position);
			result = nodesTempArray[i].transform;
			if (offset == 0 || i >= nodesTempArray.Length - 1)
			{
				flag = true;
				break;
			}
			offset--;
		}
		if (!flag)
		{
			ignoredNodes.Clear();
			return nodesTempArray[Mathf.Min(UnityEngine.Random.Range(6, 15), nodesTempArray.Length)].transform;
		}
		return result;
	}

	[ServerRpc(RequireOwnership = false)]
	public void FinishLeapServerRpc()
			{
				FinishLeapClientRpc();
			}

	[ClientRpc]
	public void FinishLeapClientRpc()
{if(!base.IsOwner)			{
				syncMovementSpeed = 0.2f;
				chasingAfterLeap = true;
				screaming = false;
				leaping = false;
				headRig.weight = 1f;
				creatureAnimator.SetBool("Screaming", value: false);
				creatureAnimator.SetBool("Leaping", value: false);
				creatureAnimator.SetBool("FinishedLeaping", value: false);
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void DoLeapServerRpc()
			{
				DoLeapClientRpc();
			}

	[ClientRpc]
	public void DoLeapClientRpc()
{if(!base.IsOwner)		{
			leaping = true;
			leapTimer = leapTime;
			syncMovementSpeed = 0.09f;
			if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 10f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
			}
			beganCooldown = false;
			creatureAnimator.SetBool("Leaping", value: true);
			creatureAnimator.SetBool("FinishedLeaping", value: false);
			float pitch = UnityEngine.Random.Range(0.95f, 1.05f);
			screamAudio.pitch = pitch;
			creatureVoice.Stop();
			screamAudio.Play();
			screamAudioNonDiagetic.pitch = pitch;
			screamAudioNonDiagetic.Play();
			WalkieTalkie.TransmitOneShotAudio(screamAudio, screamAudio.clip);
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void DoScreamServerRpc()
			{
				DoScreamClientRpc();
			}

	[ClientRpc]
	public void DoScreamClientRpc()
{if(!base.IsOwner)			{
				syncMovementSpeed = 0.09f;
				beganCooldown = false;
				chasingAfterLeap = false;
				isFakingBabyVoice = false;
				fakeCryTimer = 18f;
				headRig.weight = 0f;
				creatureVoice.Stop();
				creatureVoice.PlayOneShot(growlSFX);
				WalkieTalkie.TransmitOneShotAudio(creatureVoice, growlSFX);
				creatureAnimator.SetBool("Screaming", value: true);
				creatureAnimator.SetBool("FinishedLeaping", value: false);
				screaming = true;
			}
}
	public override void OnDestroy()
	{
		base.OnDestroy();
	}

	private void LateUpdate()
	{
	}

	private void SetClickingAudioVolume()
	{
		if (StartOfRound.Instance.audioListener == null)
		{
			return;
		}
		bool flag = true;
		float diageticMasterVolume = 0f;
		if (currentBehaviourStateIndex == 0 || isFakingBabyVoice || (screaming && !leaping))
		{
			clickingAudio1.volume = Mathf.Lerp(clickingAudio1.volume, 0f, Time.deltaTime * 5f);
			clickingAudio2.volume = Mathf.Lerp(clickingAudio2.volume, 0f, Time.deltaTime * 5f);
			diageticMasterVolume = 0f;
			flag = false;
		}
		else if (currentBehaviourStateIndex != 3 && clickingMandibles)
		{
			clickingAudio1.volume = Mathf.Lerp(clickingAudio1.volume, 1f, Time.deltaTime * 5f);
			clickingAudio2.volume = Mathf.Lerp(clickingAudio1.volume, 0f, Time.deltaTime * 5f);
			diageticMasterVolume = 0f;
			flag = false;
		}
		else if (chasingAfterLeap)
		{
			clickingAudio1.volume = Mathf.Lerp(clickingAudio1.volume, 1f, Time.deltaTime * 1.5f);
			clickingAudio2.volume = Mathf.Lerp(clickingAudio2.volume, 1f, Time.deltaTime * 1.5f);
		}
		else
		{
			clickingAudio1.volume = Mathf.Lerp(clickingAudio1.volume, 0f, Time.deltaTime * 1.5f);
			clickingAudio2.volume = Mathf.Lerp(clickingAudio2.volume, 0f, Time.deltaTime * 1.5f);
		}
		if (clickingAudio1.volume < 0.02f)
		{
			clickingAudio1.Stop();
		}
		else if (!clickingAudio1.isPlaying)
		{
			clickingAudio1.Play();
		}
		if (clickingAudio2.volume < 0.02f)
		{
			clickingAudio2.Stop();
		}
		else if (!clickingAudio2.isPlaying)
		{
			clickingAudio2.Play();
		}
		if (flag)
		{
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				CaveDwellerDeafenAmount = 0f;
			}
			else
			{
				float num = Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position);
				if (!leaping || leapTimer <= 0f)
				{
					maxDeafenAmount = -9f;
				}
				else if (leapTimer > 0.5f)
				{
					maxDeafenAmount = -20f;
				}
				else
				{
					maxDeafenAmount = -13f;
				}
				diageticMasterVolume = Mathf.Clamp((num - deafeningMinDistance) / (deafeningMaxDistance - deafeningMinDistance), 0f, 1f);
				diageticMasterVolume = Mathf.Abs(diageticMasterVolume - 1f) * maxDeafenAmount;
				diageticMasterVolume = Mathf.Clamp(diageticMasterVolume, maxDeafenAmount, 0f);
			}
		}
		SoundManager.Instance.SetDiageticMasterVolume(diageticMasterVolume);
	}
}
