using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class BaboonBirdAI : EnemyAI, IVisibleThreat
{
	public Dictionary<Transform, Threat> threats = new Dictionary<Transform, Threat>();

	public Transform focusedThreatTransform;

	public Threat focusedThreat;

	public bool focusingOnThreat;

	public bool focusedThreatIsInView;

	private int focusLevel;

	private float fearLevel;

	private float fearLevelNoDistComparison;

	private Vector3 agentLocalVelocity;

	private float velX;

	private float velZ;

	private Vector3 previousPosition;

	public Transform animationContainer;

	public MultiAimConstraint headLookRig;

	public Transform headLookTarget;

	private Ray lookRay;

	public float fov;

	public float visionDistance;

	private int visibleThreatsMask = 524296;

	private int scrapMask = 64;

	private int leadershipLevel;

	private int previousBehaviourState = -1;

	public BaboonHawkGroup scoutingGroup;

	private float miscAnimationTimer;

	private int currentMiscAnimation;

	private Vector3 lookTarget;

	private Vector3 peekTarget;

	private float peekTimer;

	public AISearchRoutine scoutingSearchRoutine;

	public static Vector3 baboonCampPosition;

	public float scoutTimer;

	public float timeToScout;

	private float timeSinceRestWhileScouting;

	private float restingDuringScouting;

	private bool eyesClosed;

	private bool restingAtCamp;

	private float restAtCampTimer;

	private float chosenDistanceToCamp = 1f;

	private float timeSincePingingBirdInterest;

	private float timeSinceLastMiscAnimation;

	private int aggressiveMode;

	private int previousAggressiveMode;

	private float fightTimer;

	public AudioSource aggressionAudio;

	private Vector3 debugSphere;

	public Collider ownCollider;

	private float timeSinceAggressiveDisplay;

	private float timeSpentFocusingOnThreat;

	private float timeSinceFighting;

	private bool doingKillAnimation;

	private Coroutine killAnimCoroutine;

	private float timeSinceHitting;

	public Transform deadBodyPoint;

	public AudioClip[] cawScreamSFX;

	public AudioClip[] cawLaughSFX;

	private float noiseTimer;

	private float noiseInterval;

	public GrabbableObject focusedScrap;

	public GrabbableObject heldScrap;

	public bool movingToScrap;

	public Transform grabTarget;

	public TwoBoneIKConstraint leftArmRig;

	public TwoBoneIKConstraint rightArmRig;

	private bool oddAIInterval;

	private DeadBodyInfo killAnimationBody;

	private float timeSinceBeingAttackedByPlayer;

	private float timeSinceJoiningOrLeavingScoutingGroup;

	private BaboonBirdAI biggestBaboon;

	ThreatType IVisibleThreat.type => ThreatType.BaboonHawk;

	bool IVisibleThreat.IsThreatDead()
	{
		return isEnemyDead;
	}

	GrabbableObject IVisibleThreat.GetHeldObject()
	{
		return heldScrap;
	}

	int IVisibleThreat.SendSpecialBehaviour(int id)
	{
		return 0;
	}

	int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
	{
		int num = 0;
		num = 1;
		if (aggressiveMode == 2)
		{
			num++;
		}
		if (scoutingGroup != null && !scoutingGroup.isEmpty)
		{
			num++;
		}
		return num;
	}

	int IVisibleThreat.GetInterestLevel()
	{
		return 0;
	}

	Transform IVisibleThreat.GetThreatLookTransform()
	{
		return eye;
	}

	Transform IVisibleThreat.GetThreatTransform()
	{
		return base.transform;
	}

	Vector3 IVisibleThreat.GetThreatVelocity()
	{
		if (base.IsOwner)
		{
			return agent.velocity;
		}
		return Vector3.zero;
	}

	float IVisibleThreat.GetVisibility()
	{
		if (isEnemyDead)
		{
			return 0f;
		}
		if (restingAtCamp)
		{
			return 0.6f;
		}
		return 1f;
	}

	public override void Start()
	{
		base.Start();
		if (!base.IsOwner)
		{
			return;
		}
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
		leadershipLevel = random.Next(0, 500);
		if (baboonCampPosition == Vector3.zero)
		{
			EnemyAINestSpawnObject[] array = UnityEngine.Object.FindObjectsByType<EnemyAINestSpawnObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			bool flag = false;
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i].enemyType != enemyType))
				{
					baboonCampPosition = RoundManager.Instance.GetNavMeshPosition(array[i].transform.position, default(NavMeshHit), 8f);
					if (RoundManager.Instance.GotNavMeshPositionResult)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				List<GameObject> list = new List<GameObject>();
				for (int j = 0; j < RoundManager.Instance.outsideAINodes.Length - 2; j += 2)
				{
					if (Vector3.Distance(RoundManager.Instance.outsideAINodes[j].transform.position, StartOfRound.Instance.elevatorTransform.position) > 30f && !PathIsIntersectedByLineOfSight(RoundManager.Instance.outsideAINodes[j].transform.position, calculatePathDistance: false, avoidLineOfSight: false))
					{
						list.Add(RoundManager.Instance.outsideAINodes[j]);
					}
				}
				if (list.Count == 0)
				{
					baboonCampPosition = base.transform.position;
				}
				else
				{
					baboonCampPosition = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(list[random.Next(0, list.Count)].transform.position, 15f, RoundManager.Instance.navHit, random);
				}
			}
		}
		SyncInitialValuesServerRpc(leadershipLevel, baboonCampPosition);
	}

	[ServerRpc]
	public void SyncInitialValuesServerRpc(int syncLeadershipLevel, Vector3 campPosition)
{		{
			SyncInitialValuesClientRpc(syncLeadershipLevel, campPosition);
		}
}
	[ClientRpc]
	public void SyncInitialValuesClientRpc(int syncLeadershipLevel, Vector3 campPosition)
			{
				leadershipLevel = syncLeadershipLevel;
				baboonCampPosition = campPosition;
				base.transform.localScale = base.transform.localScale * Mathf.Max((float)leadershipLevel / 200f * 0.6f, 0.9f);
			}

	public void LateUpdate()
	{
		if ((!inSpecialAnimation && (focusedThreatTransform == null || currentBehaviourStateIndex != 2) && peekTimer < 0f) || isEnemyDead)
		{
			agent.angularSpeed = 300f;
			headLookRig.weight = Mathf.Lerp(headLookRig.weight, 0f, Time.deltaTime * 10f);
			return;
		}
		agent.angularSpeed = 0f;
		headLookRig.weight = Mathf.Lerp(headLookRig.weight, 1f, Time.deltaTime * 10f);
		if (peekTimer >= 0f)
		{
			peekTimer -= Time.deltaTime;
			AnimateLooking(peekTarget);
		}
		else
		{
			AnimateLooking(lookTarget);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (timeSinceHitting < 0.5f)
		{
			return;
		}
		Vector3 vector = Vector3.Normalize(base.transform.position + Vector3.up * 0.7f - (other.transform.position + Vector3.up * 0.4f)) * 0.5f;
		if (Physics.Linecast(base.transform.position + Vector3.up * 0.7f + vector, other.transform.position + Vector3.up * 0.4f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			return;
		}
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inSpecialAnimation || doingKillAnimation);
		if (playerControllerB != null)
		{
			timeSinceHitting = 0f;
			playerControllerB.DamagePlayer(20);
			if (playerControllerB.isPlayerDead)
			{
				StabPlayerDeathAnimServerRpc((int)playerControllerB.playerClientId);
				return;
			}
			creatureAnimator.ResetTrigger("Hit");
			creatureAnimator.SetTrigger("Hit");
			creatureSFX.PlayOneShot(enemyType.audioClips[5]);
			WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[5]);
			RoundManager.Instance.PlayAudibleNoise(creatureSFX.transform.position, 8f, 0.7f);
		}
	}

	public override void OnCollideWithEnemy(Collider other, EnemyAI enemyScript = null)
	{
		base.OnCollideWithEnemy(other);
		if (!(enemyScript.enemyType == enemyType) && !(timeSinceHitting < 0.75f) && !(stunNormalizedTimer > 0f) && !isEnemyDead && enemyScript.enemyType.canDie)
		{
			creatureAnimator.ResetTrigger("Hit");
			creatureAnimator.SetTrigger("Hit");
			creatureSFX.PlayOneShot(enemyType.audioClips[5]);
			WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[5]);
			RoundManager.Instance.PlayAudibleNoise(creatureSFX.transform.position, 8f, 0.7f);
			timeSinceHitting = 0f;
			if (base.IsOwner)
			{
				enemyScript.HitEnemy(1, null, playHitSFX: true);
			}
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (isEnemyDead)
		{
			return;
		}
		creatureAnimator.SetTrigger("TakeDamage");
		if (playerWhoHit != null)
		{
			timeSinceBeingAttackedByPlayer = 0f;
			if (threats.TryGetValue(playerWhoHit.transform, out var value))
			{
				value.hasAttacked = true;
				fightTimer = 7f;
			}
		}
		enemyHP -= force;
		if (base.IsOwner && enemyHP <= 0 && !isEnemyDead)
		{
			KillEnemyOnOwnerClient();
		}
		StopKillAnimation();
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		creatureAnimator.SetBool("IsDead", value: true);
		if (heldScrap != null && base.IsOwner)
		{
			DropHeldItemAndSync();
		}
		StopKillAnimation();
	}

	public void StopKillAnimation()
	{
		if (killAnimCoroutine != null)
		{
			StopCoroutine(killAnimCoroutine);
		}
		agent.acceleration = 17f;
		inSpecialAnimation = false;
		doingKillAnimation = false;
		if (killAnimationBody != null)
		{
			killAnimationBody.attachedLimb = null;
			killAnimationBody.attachedTo = null;
			killAnimationBody = null;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StabPlayerDeathAnimServerRpc(int playerObject)
{if(!doingKillAnimation)		{
			if (base.IsOwner && heldScrap != null)
			{
				DropHeldItemAndSync();
			}
			doingKillAnimation = true;
			StabPlayerDeathAnimClientRpc(playerObject);
		}
}
	[ClientRpc]
	public void StabPlayerDeathAnimClientRpc(int playerObject)
{		{
			doingKillAnimation = true;
			inSpecialAnimation = true;
			agent.acceleration = 70f;
			agent.speed = 0f;
			if (killAnimCoroutine != null)
			{
				StopCoroutine(killAnimCoroutine);
			}
			killAnimCoroutine = StartCoroutine(killPlayerAnimation(playerObject));
		}
}
	private IEnumerator killPlayerAnimation(int playerObject)
	{
		PlayerControllerB killedPlayer = StartOfRound.Instance.allPlayerScripts[playerObject];
		creatureAnimator.ResetTrigger("KillAnimation");
		creatureAnimator.SetTrigger("KillAnimation");
		creatureVoice.PlayOneShot(enemyType.audioClips[4]);
		WalkieTalkie.TransmitOneShotAudio(creatureVoice, enemyType.audioClips[4]);
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 1f || killedPlayer.deadBody != null);
		if (killedPlayer.deadBody != null)
		{
			killAnimationBody = killedPlayer.deadBody;
			killAnimationBody.attachedLimb = killedPlayer.deadBody.bodyParts[5];
			killAnimationBody.attachedTo = deadBodyPoint;
			killAnimationBody.matchPositionExactly = true;
			killAnimationBody.canBeGrabbedBackByPlayers = false;
			yield return null;
			yield return new WaitForSeconds(1.7f);
			killAnimationBody.attachedLimb = null;
			killAnimationBody.attachedTo = null;
		}
		agent.acceleration = 17f;
		inSpecialAnimation = false;
		doingKillAnimation = false;
	}

	private void InteractWithScrap()
	{
		if (heldScrap != null)
		{
			focusedScrap = null;
			if (Vector3.Distance(base.transform.position, baboonCampPosition) < UnityEngine.Random.Range(1f, 7f) || heldScrap.isHeld)
			{
				DropHeldItemAndSync();
			}
		}
		else if (focusedScrap != null)
		{
			if (debugEnemyAI)
			{
				Debug.DrawRay(focusedScrap.transform.position, Vector3.up * 3f, Color.yellow);
			}
			if (!CanGrabScrap(focusedScrap))
			{
				focusedScrap = null;
			}
			else if (Vector3.Distance(base.transform.position, focusedScrap.transform.position) < 0.4f && !Physics.Linecast(base.transform.position, focusedScrap.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				GrabItemAndSync(focusedScrap.NetworkObject);
			}
		}
	}

	private bool CanGrabScrap(GrabbableObject scrap)
	{
		if (scrap.itemProperties.itemId == 1531)
		{
			return false;
		}
		if (scrap.isInShipRoom && !isInsidePlayerShip)
		{
			return false;
		}
		if (isEnemyDead)
		{
			return false;
		}
		if (fearLevel >= 9f)
		{
			return false;
		}
		if (!scrap.heldByPlayerOnServer && !scrap.isHeld && (scrap == heldScrap || !scrap.isHeldByEnemy))
		{
			return Vector3.Distance(scrap.transform.position, baboonCampPosition) > 8f;
		}
		return false;
	}

	private void DropHeldItemAndSync()
	{
		if (heldScrap == null)
		{
			Debug.LogError($"Baboon #{thisEnemyIndex} Error: DropItemAndSync called when baboon has no scrap!");
		}
		NetworkObject networkObject = heldScrap.NetworkObject;
		if (networkObject == null)
		{
			Debug.LogError($"Baboon #{thisEnemyIndex} Error: No network object in held scrap {heldScrap.gameObject.name}");
		}
		Vector3 itemFloorPosition = heldScrap.GetItemFloorPosition();
		DropScrap(networkObject, itemFloorPosition);
		DropScrapServerRpc(networkObject, itemFloorPosition, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
	}

	[ServerRpc]
	public void DropScrapServerRpc(NetworkObjectReference item, Vector3 targetFloorPosition, int clientWhoSentRPC)
{		{
			DropScrapClientRpc(item, targetFloorPosition, clientWhoSentRPC);
		}
}
	[ClientRpc]
	public void DropScrapClientRpc(NetworkObjectReference item, Vector3 targetFloorPosition, int clientWhoSentRPC)
{if(clientWhoSentRPC != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)		{
			if (item.TryGet(out var networkObject))
			{
				DropScrap(networkObject, targetFloorPosition);
			}
			else
			{
				Debug.LogError($"Baboon #{thisEnemyIndex}; Error, was not able to get network object from dropped item client rpc");
			}
		}
}
	private void DropScrap(NetworkObject item, Vector3 targetFloorPosition)
	{
		if (heldScrap == null)
		{
			Debug.LogError("Baboon: my held item is null when attempting to drop it!!");
			return;
		}
		if (heldScrap.isHeld)
		{
			heldScrap.DiscardItemFromEnemy();
			heldScrap.isHeldByEnemy = false;
			heldScrap = null;
			Debug.Log($"Baboon #{thisEnemyIndex}: Dropped item which was held by a player");
			return;
		}
		heldScrap.parentObject = null;
		heldScrap.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
		heldScrap.EnablePhysics(enable: true);
		heldScrap.fallTime = 0f;
		heldScrap.startFallingPosition = heldScrap.transform.parent.InverseTransformPoint(heldScrap.transform.position);
		heldScrap.targetFloorPosition = heldScrap.transform.parent.InverseTransformPoint(targetFloorPosition);
		heldScrap.floorYRot = -1;
		heldScrap.DiscardItemFromEnemy();
		heldScrap.isHeldByEnemy = false;
		heldScrap = null;
		Debug.Log($"Baboon #{thisEnemyIndex}: Dropped item");
	}

	private void GrabItemAndSync(NetworkObject item)
	{
		if (heldScrap != null)
		{
			Debug.LogError($"Baboon #{thisEnemyIndex} Error: GrabItemAndSync called when baboon is already carrying scrap!");
		}
		GrabScrap(item);
		GrabScrapServerRpc(item, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
	}

	[ServerRpc]
	public void GrabScrapServerRpc(NetworkObjectReference item, int clientWhoSentRPC)
{		{
			if (!item.TryGet(out var networkObject))
			{
				Debug.LogError($"Baboon #{thisEnemyIndex} error: Could not get grabbed network object from reference on server");
			}
			else if ((bool)networkObject.GetComponent<GrabbableObject>() && !networkObject.GetComponent<GrabbableObject>().heldByPlayerOnServer)
			{
				GrabScrapClientRpc(item, clientWhoSentRPC);
			}
		}
}
	[ClientRpc]
	public void GrabScrapClientRpc(NetworkObjectReference item, int clientWhoSentRPC)
{if(clientWhoSentRPC != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)		{
			if (item.TryGet(out var networkObject))
			{
				GrabScrap(networkObject);
			}
			else
			{
				Debug.LogError($"Baboon #{thisEnemyIndex}; Error, was not able to get id from grabbed item client rpc");
			}
		}
}
	private void GrabScrap(NetworkObject item)
	{
		if (heldScrap != null)
		{
			Debug.Log($"Baboon #{thisEnemyIndex}: Trying to grab another item ({item.gameObject.name}) while hands are already full with item ({heldScrap.gameObject.name}). Dropping the currently held one.");
			DropScrap(heldScrap.GetComponent<NetworkObject>(), heldScrap.GetItemFloorPosition());
		}
		GrabbableObject grabbableObject = (heldScrap = item.gameObject.GetComponent<GrabbableObject>());
		grabbableObject.parentObject = grabTarget;
		grabbableObject.hasHitGround = false;
		grabbableObject.GrabItemFromEnemy(this);
		grabbableObject.isHeldByEnemy = true;
		grabbableObject.EnablePhysics(enable: false);
		Debug.Log($"Baboon #{thisEnemyIndex}: Grabbing item!!! {heldScrap.gameObject.name}");
	}

	public override void ReachedNodeInSearch()
	{
		base.ReachedNodeInSearch();
		if (currentSearch.nodesEliminatedInCurrentSearch > 14 && timeSinceRestWhileScouting > 17f && timeSinceAggressiveDisplay > 6f)
		{
			timeSinceRestWhileScouting = 0f;
			restingDuringScouting = 12f;
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead)
		{
			agent.speed = 0f;
			if (scoutingSearchRoutine.inProgress)
			{
				StopSearch(scoutingSearchRoutine, clear: false);
			}
			return;
		}
		if (stunNormalizedTimer > 0f || miscAnimationTimer > 0f)
		{
			agent.speed = 0f;
			if (doingKillAnimation && stunNormalizedTimer >= 0f)
			{
				StopKillAnimation();
			}
			if (heldScrap != null && base.IsOwner)
			{
				DropHeldItemAndSync();
			}
			if (stunnedByPlayer != null)
			{
				PingBaboonInterest(stunnedByPlayer.gameplayCamera.transform.position, 4);
			}
		}
		if (inSpecialAnimation)
		{
			agent.speed = 0f;
			return;
		}
		if (!eyesClosed)
		{
			DoLOSCheck();
		}
		InteractWithScrap();
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				timeToScout = UnityEngine.Random.Range(25, 70);
				scoutTimer = 0f;
				restingAtCamp = false;
				restAtCampTimer = 0f;
				SetAggressiveMode(0);
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (!base.IsOwner)
			{
				break;
			}
			if (focusedScrap != null)
			{
				SetDestinationToPosition(focusedScrap.transform.position);
			}
			if (scoutingGroup == null || scoutingGroup.leader == this || !scoutingGroup.members.Contains(this))
			{
				_ = scoutingGroup;
				if (restingDuringScouting >= 0f)
				{
					if (scoutingSearchRoutine.inProgress)
					{
						StopSearch(scoutingSearchRoutine, clear: false);
					}
					if (!creatureAnimator.GetBool("sit"))
					{
						EnemyEnterRestModeServerRpc(sleep: false, atCamp: false);
					}
					creatureAnimator.SetBool("sit", value: true);
					restingDuringScouting -= AIIntervalTime;
					agent.speed = 0f;
				}
				else
				{
					if (!scoutingSearchRoutine.inProgress && focusedScrap == null)
					{
						StartSearch(baboonCampPosition, scoutingSearchRoutine);
					}
					if (creatureAnimator.GetBool("sit"))
					{
						EnemyGetUpServerRpc();
						creatureAnimator.SetBool("sit", value: false);
					}
					agent.speed = 10f;
				}
			}
			else
			{
				if (scoutingSearchRoutine.inProgress)
				{
					StopSearch(scoutingSearchRoutine);
				}
				if (creatureAnimator.GetBool("sit"))
				{
					EnemyGetUpServerRpc();
					creatureAnimator.SetBool("sit", value: false);
				}
				agent.speed = 12f;
				if (Vector3.Distance(base.transform.position, scoutingGroup.leader.transform.position) > 60f || PathIsIntersectedByLineOfSight(scoutingGroup.leader.transform.position, calculatePathDistance: false, avoidLineOfSight: false))
				{
					LeaveCurrentScoutingGroup(sync: true);
				}
				else if (Vector3.Distance(destination, scoutingGroup.leader.transform.position) > 8f && focusedScrap == null)
				{
					SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(scoutingGroup.leader.transform.position, 6f, RoundManager.Instance.navHit));
				}
			}
			if (scoutTimer < timeToScout && heldScrap == null)
			{
				scoutTimer += AIIntervalTime;
			}
			else
			{
				SwitchToBehaviourState(1);
			}
			break;
		case 1:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				restingDuringScouting = 0f;
				scoutTimer = 0f;
				chosenDistanceToCamp = UnityEngine.Random.Range(1f, 7f);
				LeaveCurrentScoutingGroup(sync: true);
				SetAggressiveMode(0);
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (scoutingSearchRoutine.inProgress)
			{
				StopSearch(scoutingSearchRoutine);
			}
			if (focusedScrap != null)
			{
				SetDestinationToPosition(focusedScrap.transform.position);
			}
			else
			{
				SetDestinationToPosition(baboonCampPosition);
			}
			if (Vector3.Distance(base.transform.position, baboonCampPosition) < chosenDistanceToCamp && peekTimer < 0f)
			{
				if (!restingAtCamp)
				{
					restingAtCamp = true;
					restAtCampTimer = UnityEngine.Random.Range(15f, 30f);
					if (heldScrap != null)
					{
						DropHeldItemAndSync();
					}
					bool sleep = false;
					if (UnityEngine.Random.Range(0, 100) < 35)
					{
						sleep = true;
					}
					EnemyEnterRestModeServerRpc(sleep, atCamp: true);
				}
				else if (restAtCampTimer <= 0f)
				{
					SwitchToBehaviourState(0);
				}
				else
				{
					restAtCampTimer -= AIIntervalTime;
				}
				agent.speed = 0f;
			}
			else
			{
				if (restingAtCamp)
				{
					restingAtCamp = false;
					EnemyGetUpServerRpc();
				}
				creatureAnimator.SetBool("sit", value: false);
				creatureAnimator.SetBool("sleep", value: false);
				agent.speed = 9f;
			}
			break;
		case 2:
		{
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				timeSpentFocusingOnThreat = 0f;
				creatureAnimator.SetBool("sleep", value: false);
				creatureAnimator.SetBool("sit", value: false);
				EnemyGetUpServerRpc();
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (focusedThreat == null || !focusingOnThreat)
			{
				StopFocusingThreat();
			}
			if (scoutingSearchRoutine.inProgress)
			{
				StopSearch(scoutingSearchRoutine, clear: false);
			}
			agent.speed = 9f;
			float num = fearLevelNoDistComparison * 2f;
			if (focusedThreat.interestLevel <= 0 || enemyHP <= 3)
			{
				num = Mathf.Max(num, 1f);
			}
			float num2 = GetComfortableDistanceToThreat(focusedThreat) + num;
			float num3 = Vector3.Distance(base.transform.position, focusedThreat.lastSeenPosition);
			bool flag = false;
			float num4 = Time.realtimeSinceStartup - focusedThreat.timeLastSeen;
			if (num4 > 5f)
			{
				SetThreatInView(inView: false);
				focusLevel = 0;
				StopFocusingThreat();
				break;
			}
			if (num4 > 3f)
			{
				SetThreatInView(inView: false);
				focusLevel = 1;
				if (num2 - num3 > 2f)
				{
					StopFocusingThreat();
					break;
				}
			}
			else if (num4 > 1f)
			{
				flag = true;
				focusedThreatIsInView = false;
				SetThreatInView(inView: false);
				focusLevel = 2;
				SetAggressiveMode(0);
			}
			else if (num4 < 0.55f)
			{
				flag = true;
				SetThreatInView(inView: true);
			}
			bool flag2 = (fearLevel > 0f && fearLevel < 4f) || focusedThreat.interestLevel > 0 || fearLevel < -6f || focusedThreat.hasAttacked;
			if (aggressiveMode == 2)
			{
				focusLevel = 3;
				if (heldScrap != null)
				{
					DropHeldItemAndSync();
					focusedScrap = heldScrap;
				}
				Vector3 vector = focusedThreat.threatScript.GetThreatTransform().position + focusedThreat.threatScript.GetThreatVelocity() * 10f;
				Debug.DrawRay(vector, Vector3.up * 5f, Color.red, AIIntervalTime);
				SetDestinationToPosition(vector, checkForPath: true);
				if (fightTimer > 4f || timeSinceBeingAttackedByPlayer < 4f || (fightTimer > 2f && (fearLevel >= 1f || !flag2)) || (enemyHP <= 3 && !flag2))
				{
					scoutTimer = timeToScout - 20f;
					fightTimer = -7f;
					SetAggressiveMode(1);
				}
				else if (num3 > 4f)
				{
					fightTimer += AIIntervalTime * 2f;
				}
				else if (num3 > 1f)
				{
					fightTimer += AIIntervalTime;
				}
				else
				{
					fightTimer += AIIntervalTime / 2f;
				}
				break;
			}
			bool flag3 = false;
			if (focusedScrap != null && (!flag || fearLevel <= 2f) && (focusedThreat.type != ThreatType.GiantKiwi || Vector3.Distance(base.transform.position, focusedThreat.lastSeenPosition) > 10f))
			{
				Debug.Log($"Baboon hawk fear level: {fearLevel} ; {flag} ; going for scrap!");
				SetDestinationToPosition(focusedScrap.transform.position);
				flag3 = true;
			}
			Vector3 vector2 = focusedThreat.lastSeenPosition + focusedThreat.threatScript.GetThreatVelocity() * -17f;
			Debug.DrawRay(vector2, Vector3.up * 3f, Color.red, AIIntervalTime);
			Ray ray = new Ray(base.transform.position + Vector3.up * 0.5f, Vector3.Normalize((base.transform.position + Vector3.up * 0.5f - vector2) * 100f));
			RaycastHit hitInfo;
			Vector3 vector3 = ((!Physics.Raycast(ray, out hitInfo, num2 - num3, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(num2 - num3), RoundManager.Instance.navHit, 8f) : RoundManager.Instance.GetNavMeshPosition(hitInfo.point, RoundManager.Instance.navHit, 8f));
			Debug.DrawRay(vector3, Vector3.up, Color.blue, AIIntervalTime);
			if (!flag3)
			{
				if (SetDestinationToPosition(vector3, checkForPath: true))
				{
					debugSphere = vector3;
				}
				else
				{
					debugSphere = vector3;
				}
			}
			if (fightTimer > 7f && timeSinceFighting > 4f)
			{
				fightTimer = -6f;
				SetAggressiveMode(2);
				break;
			}
			bool flag4 = false;
			if (scoutingGroup != null)
			{
				for (int i = 0; i < scoutingGroup.members.Count; i++)
				{
					if (scoutingGroup.members[i].aggressiveMode == 2)
					{
						flag4 = true;
					}
				}
			}
			float num5 = GetComfortableDistanceToThreat(focusedThreat) - num3;
			if (fearLevel <= -5f)
			{
				if (noiseTimer >= noiseInterval)
				{
					noiseInterval = UnityEngine.Random.Range(0.2f, 0.7f);
					noiseTimer = 0f;
					RoundManager.PlayRandomClip(creatureVoice, cawLaughSFX, randomize: true, 1f, 1105);
				}
				else
				{
					noiseTimer += Time.deltaTime;
				}
			}
			if ((flag && ((num5 > 8f && flag2) || num3 < 5f)) || timeSinceBeingAttackedByPlayer < 4f)
			{
				if (timeSinceFighting > 5f)
				{
					fightTimer += AIIntervalTime * 10.6f / (focusedThreat.distanceToThreat * 0.3f);
				}
				SetAggressiveMode(1);
			}
			else if (num5 > 4f && fearLevel < 3f && flag2)
			{
				fightTimer += AIIntervalTime * 7.4f / (focusedThreat.distanceToThreat * 0.3f);
				SetAggressiveMode(1);
			}
			else
			{
				if (!(num5 < 2f))
				{
					break;
				}
				if (timeSinceAggressiveDisplay > 2.5f)
				{
					SetAggressiveMode(0);
				}
				fightTimer -= Mathf.Max(-6f, AIIntervalTime * 0.2f);
				if (timeSpentFocusingOnThreat > 4f + (float)focusedThreat.interestLevel * 8f && !flag4)
				{
					if (fightTimer > 4f)
					{
						fightTimer -= Mathf.Max(-6f, AIIntervalTime * 0.5f * (focusedThreat.distanceToThreat * 0.1f));
					}
					else
					{
						StopFocusingThreat();
					}
				}
			}
			break;
		}
		}
	}

	private void StopFocusingThreat()
	{
		if (currentBehaviourStateIndex == 2)
		{
			aggressiveMode = 0;
			focusingOnThreat = false;
			focusedThreatIsInView = false;
			focusedThreatTransform = null;
			focusedThreat = null;
			if (heldScrap == null)
			{
				SwitchToBehaviourStateOnLocalClient(0);
			}
			else
			{
				SwitchToBehaviourStateOnLocalClient(1);
			}
			StopFocusingThreatServerRpc(heldScrap == null);
		}
	}

	[ServerRpc]
	public void StopFocusingThreatServerRpc(bool enterScoutingMode)
{		{
			StopFocusingThreatClientRpc(enterScoutingMode);
		}
}
	[ClientRpc]
	public void StopFocusingThreatClientRpc(bool enterScoutingMode)
{if(!base.IsOwner)		{
			aggressiveMode = 0;
			focusedThreatTransform = null;
			focusedThreat = null;
			if (enterScoutingMode)
			{
				SwitchToBehaviourStateOnLocalClient(0);
			}
			else
			{
				SwitchToBehaviourStateOnLocalClient(1);
			}
		}
}
	private void SetAggressiveMode(int mode)
	{
		if (aggressiveMode != mode)
		{
			aggressiveMode = mode;
			SetAggressiveModeServerRpc(mode);
		}
	}

	[ServerRpc]
	public void SetAggressiveModeServerRpc(int mode)
{		{
			SetAggressiveModeClientRpc(mode);
		}
}
	[ClientRpc]
	public void SetAggressiveModeClientRpc(int mode)
{if(!base.IsOwner)			{
				aggressiveMode = mode;
			}
}
	private void SetThreatInView(bool inView)
	{
		if (focusedThreatIsInView != inView)
		{
			focusedThreatIsInView = inView;
			SetThreatInViewServerRpc(inView);
		}
	}

	[ServerRpc]
	public void SetThreatInViewServerRpc(bool inView)
{		{
			SetThreatInViewClientRpc(inView);
		}
}
	[ClientRpc]
	public void SetThreatInViewClientRpc(bool inView)
{if(!base.IsOwner)			{
				focusedThreatIsInView = inView;
			}
}
	[ServerRpc]
	public void EnemyEnterRestModeServerRpc(bool sleep, bool atCamp)
{		{
			EnemyEnterRestModeClientRpc(sleep, atCamp);
		}
}
	[ClientRpc]
	public void EnemyEnterRestModeClientRpc(bool sleep, bool atCamp)
{		{
			restingAtCamp = atCamp;
			if (sleep)
			{
				eyesClosed = true;
				creatureAnimator.SetBool("sleep", value: true);
				creatureAnimator.SetBool("sit", value: false);
			}
			else
			{
				eyesClosed = false;
				creatureAnimator.SetBool("sleep", value: false);
				creatureAnimator.SetBool("sit", value: true);
			}
		}
}
	[ServerRpc]
	public void EnemyGetUpServerRpc()
{		{
			EnemyGetUpClientRpc();
		}
}
	[ClientRpc]
	public void EnemyGetUpClientRpc()
{if(!base.IsOwner)			{
				creatureAnimator.SetBool("sit", value: false);
			}
}
	public override void OnDrawGizmos()
	{
		if (!debugEnemyAI)
		{
			return;
		}
		if (currentBehaviourStateIndex == 1)
		{
			Gizmos.DrawCube(base.transform.position + Vector3.up * 2f, new Vector3(0.2f, 0.2f, 0.2f));
		}
		else if (scoutingGroup != null)
		{
			if (scoutingGroup.leader == this)
			{
				Gizmos.DrawSphere(base.transform.position + Vector3.up * 2f, 0.6f);
				return;
			}
			Gizmos.DrawLine(scoutingGroup.leader.transform.position + Vector3.up * 2f, base.transform.position + Vector3.up * 2f);
			Gizmos.DrawSphere(base.transform.position + Vector3.up * 2f, 0.1f);
		}
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		if (!base.IsOwner || isEnemyDead)
		{
			return;
		}
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if (Vector3.Distance(noisePosition, base.transform.position + Vector3.up * 0.4f) < 0.75f || noiseID == 1105 || noiseID == 24751)
		{
			return;
		}
		float num = Vector3.Distance(noisePosition, base.transform.position);
		float num2 = noiseLoudness / num;
		if (eyesClosed)
		{
			num2 *= 0.75f;
		}
		if (num2 < 0.12f && peekTimer >= 0f && focusLevel > 0)
		{
			return;
		}
		if (focusLevel >= 3)
		{
			if (num > 3f || num2 <= 0.06f)
			{
				return;
			}
		}
		else if (focusLevel == 2)
		{
			if (num > 25f || num2 <= 0.05f)
			{
				return;
			}
		}
		else if (focusLevel == 1 && (num > 40f || num2 <= 0.05f))
		{
			return;
		}
		PingBaboonInterest(noisePosition, focusLevel);
	}

	private void AnimateLooking(Vector3 lookAtPosition)
	{
		headLookTarget.position = Vector3.Lerp(headLookTarget.position, lookAtPosition, 15f * Time.deltaTime);
		Vector3 position = headLookTarget.position;
		position.y = base.transform.position.y;
		if (Vector3.Angle(base.transform.forward, position - base.transform.position) > 30f)
		{
			RoundManager.Instance.tempTransform.position = base.transform.position;
			RoundManager.Instance.tempTransform.LookAt(position);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, RoundManager.Instance.tempTransform.rotation, 4f * Time.deltaTime);
			base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		timeSinceHitting += Time.deltaTime;
		if (stunNormalizedTimer > 0f || miscAnimationTimer > 0f)
		{
			agent.speed = 0f;
		}
		creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
		if (miscAnimationTimer <= 0f)
		{
			currentMiscAnimation = -1;
		}
		else
		{
			miscAnimationTimer -= Time.deltaTime;
		}
		CalculateAnimationDirection(2f);
		timeSinceLastMiscAnimation += Time.deltaTime;
		timeSincePingingBirdInterest += Time.deltaTime;
		timeSinceBeingAttackedByPlayer += Time.deltaTime;
		timeSinceJoiningOrLeavingScoutingGroup += Time.deltaTime;
		if (debugEnemyAI)
		{
			if (focusedThreat != null && focusingOnThreat)
			{
				HUDManager.Instance.SetDebugText(string.Format("{0}; {1}; \n Focused threat level: {2}", fearLevel.ToString("0.0"), fearLevelNoDistComparison.ToString("0.0"), focusedThreat.threatLevel));
			}
			else
			{
				HUDManager.Instance.SetDebugText(fearLevel.ToString("0.0") + "; " + fearLevelNoDistComparison.ToString("0.0"));
			}
		}
		if (heldScrap != null && !isEnemyDead)
		{
			creatureAnimator.SetLayerWeight(1, Mathf.Lerp(creatureAnimator.GetLayerWeight(1), 1f, 12f * Time.deltaTime));
			rightArmRig.weight = Mathf.Lerp(rightArmRig.weight, 0f, 12f * Time.deltaTime);
			leftArmRig.weight = Mathf.Lerp(leftArmRig.weight, 0f, 12f * Time.deltaTime);
		}
		else
		{
			creatureAnimator.SetLayerWeight(1, Mathf.Lerp(creatureAnimator.GetLayerWeight(1), 0f, 12f * Time.deltaTime));
			rightArmRig.weight = Mathf.Lerp(rightArmRig.weight, 1f, 12f * Time.deltaTime);
			leftArmRig.weight = Mathf.Lerp(leftArmRig.weight, 1f, 12f * Time.deltaTime);
		}
		switch (aggressiveMode)
		{
		case 0:
			if (previousAggressiveMode != aggressiveMode)
			{
				creatureAnimator.SetBool("aggressiveDisplay", value: false);
				creatureAnimator.SetBool("fighting", value: false);
				previousAggressiveMode = aggressiveMode;
			}
			if (aggressionAudio.volume <= 0f)
			{
				aggressionAudio.Stop();
			}
			else
			{
				aggressionAudio.volume = Mathf.Max(aggressionAudio.volume - Time.deltaTime * 5f, 0f);
			}
			timeSinceAggressiveDisplay = 0f;
			break;
		case 1:
			if (previousAggressiveMode != aggressiveMode)
			{
				creatureAnimator.SetBool("aggressiveDisplay", value: true);
				creatureAnimator.SetBool("fighting", value: false);
				RoundManager.PlayRandomClip(creatureVoice, cawScreamSFX, randomize: true, 1f, 1105);
				WalkieTalkie.TransmitOneShotAudio(creatureVoice, enemyType.audioClips[1]);
				aggressionAudio.clip = enemyType.audioClips[2];
				aggressionAudio.Play();
				previousAggressiveMode = aggressiveMode;
			}
			timeSinceAggressiveDisplay += Time.deltaTime;
			aggressionAudio.volume = Mathf.Min(aggressionAudio.volume + Time.deltaTime * 4f, 1f);
			break;
		case 2:
			if (previousAggressiveMode != aggressiveMode)
			{
				creatureAnimator.SetBool("fighting", value: true);
				aggressionAudio.clip = enemyType.audioClips[3];
				aggressionAudio.Play();
				previousAggressiveMode = aggressiveMode;
			}
			timeSinceAggressiveDisplay += Time.deltaTime;
			aggressionAudio.volume = Mathf.Min(aggressionAudio.volume + Time.deltaTime * 5f, 1f);
			break;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			creatureAnimator.SetBool("sleep", value: false);
			restingAtCamp = false;
			eyesClosed = false;
			focusedThreatTransform = null;
			break;
		case 1:
			focusedThreatTransform = null;
			break;
		case 2:
			if (focusedThreatTransform != null && focusedThreatIsInView)
			{
				lookTarget = focusedThreatTransform.position;
			}
			timeSpentFocusingOnThreat += Time.deltaTime;
			timeSinceFighting += Time.deltaTime;
			break;
		}
	}

	private float GetComfortableDistanceToThreat(Threat focusedThreat)
	{
		return Mathf.Min((float)focusedThreat.threatLevel * 6f, 25f);
	}

	private void ReactToThreat(Threat closestThreat)
	{
		if (Vector3.Distance(closestThreat.lastSeenPosition, baboonCampPosition) < 18f)
		{
			closestThreat.interestLevel++;
		}
		if (closestThreat != focusedThreat && (focusedThreat == null || focusedThreat.threatLevel <= closestThreat.threatLevel) && closestThreat.distanceToThreat < GetComfortableDistanceToThreat(closestThreat))
		{
			NetworkObject component = closestThreat.threatScript.GetThreatTransform().gameObject.GetComponent<NetworkObject>();
			if (component == null)
			{
				Debug.LogError("Baboon: Error, threat did not contain network object. All objects implementing IVisibleThreat must have a NetworkObject");
				return;
			}
			fightTimer = 0f;
			focusingOnThreat = true;
			StartFocusOnThreatServerRpc(component);
			focusedThreat = closestThreat;
			focusedThreatTransform = closestThreat.threatScript.GetThreatLookTransform();
		}
	}

	[ServerRpc]
	public void StartFocusOnThreatServerRpc(NetworkObjectReference netObject)
{		{
			StartFocusOnThreatClientRpc(netObject);
		}
}
	[ClientRpc]
	public void StartFocusOnThreatClientRpc(NetworkObjectReference netObject)
{		{
			SwitchToBehaviourStateOnLocalClient(2);
			if (!netObject.TryGet(out var networkObject))
			{
				Debug.LogError($"Baboon: Error, could not get network object from id for StartFocusOnThreatClientRpc; id: {networkObject.NetworkObjectId}");
				return;
			}
			if (!networkObject.transform.TryGetComponent<IVisibleThreat>(out var component))
			{
				Debug.LogError($"Baboon: Error, threat transform did not contain IVisibleThreat in StartFocusOnThreatClientRpc; id: {networkObject.NetworkObjectId}");
				return;
			}
			focusingOnThreat = true;
			focusedThreatTransform = component.GetThreatLookTransform();
		}
}
	private float ReactToOtherBaboonSighted(BaboonBirdAI otherBaboon)
	{
		float num = 0f;
		if (otherBaboon.isEnemyDead)
		{
			num += 4f;
		}
		else if (otherBaboon.currentBehaviourStateIndex != 1 && currentBehaviourStateIndex != 1)
		{
			if (otherBaboon.currentBehaviourStateIndex == 2 && otherBaboon.focusedThreatIsInView && otherBaboon.focusedThreatTransform != null)
			{
				int pingImportance = 3;
				if (otherBaboon.fearLevel > 2f || otherBaboon.focusLevel >= 3)
				{
					pingImportance = 4;
				}
				PingBaboonInterest(otherBaboon.focusedThreatTransform.position, pingImportance);
			}
			if (timeSinceJoiningOrLeavingScoutingGroup < 4f || otherBaboon.currentBehaviourStateIndex == 1)
			{
				return num;
			}
			if (scoutingGroup != null && Time.realtimeSinceStartup - scoutingGroup.timeAtLastCallToGroup < 1f)
			{
				return num;
			}
			if (scoutingGroup == null || (!scoutingGroup.members.Contains(otherBaboon) && scoutingGroup.leader != otherBaboon))
			{
				if (otherBaboon.scoutingGroup != null)
				{
					if (otherBaboon.scoutingGroup.leader.leadershipLevel > biggestBaboon.leadershipLevel)
					{
						biggestBaboon = otherBaboon;
					}
					return num;
				}
				if (otherBaboon.leadershipLevel > biggestBaboon.leadershipLevel)
				{
					biggestBaboon = otherBaboon;
					return num;
				}
			}
		}
		return num;
	}

	private void DoLOSCheck()
	{
		Threat threat = null;
		Threat threat2 = null;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		int num5 = Physics.OverlapSphereNonAlloc(eye.position + eye.forward * 38f + eye.up * 8f, 40f, RoundManager.Instance.tempColliderResults, visibleThreatsMask, QueryTriggerInteraction.Collide);
		biggestBaboon = this;
		if (scoutingGroup != null && scoutingGroup.leader != null)
		{
			biggestBaboon = scoutingGroup.leader;
		}
		for (int i = 0; i < num5; i++)
		{
			if (RoundManager.Instance.tempColliderResults[i] == ownCollider)
			{
				continue;
			}
			float num6 = Vector3.Distance(eye.position, RoundManager.Instance.tempColliderResults[i].transform.position);
			float num7 = Vector3.Angle(RoundManager.Instance.tempColliderResults[i].transform.position - eye.position, eye.forward);
			if (num6 > 2f && num7 > fov)
			{
				continue;
			}
			if (Physics.Linecast(base.transform.position + Vector3.up * 0.7f, RoundManager.Instance.tempColliderResults[i].transform.position + Vector3.up * 0.5f, out var hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				if (debugEnemyAI)
				{
					Debug.DrawRay(hitInfo.point, Vector3.up * 0.5f, Color.magenta, AIIntervalTime);
				}
				continue;
			}
			EnemyAI component = RoundManager.Instance.tempColliderResults[i].transform.GetComponent<EnemyAI>();
			if (component != null && component.GetType() == typeof(BaboonBirdAI))
			{
				float num8 = ReactToOtherBaboonSighted(component as BaboonBirdAI);
				num3 += num8;
				num4 += num8;
			}
			else
			{
				if (!RoundManager.Instance.tempColliderResults[i].transform.TryGetComponent<IVisibleThreat>(out var component2))
				{
					continue;
				}
				float visibility = component2.GetVisibility();
				if (visibility < 1f && (visibility == 0f || (visibility < 0.2f && num6 > 10f) || (visibility < 0.6f && num6 > 20f && num7 > 30f) || (visibility < 0.8f && num6 > 16f && num7 > 80f)))
				{
					continue;
				}
				if (debugEnemyAI)
				{
					Debug.Log($"Baboon hawk: Seeing visible threat: {RoundManager.Instance.tempColliderResults[i].transform.name}; type: {component2.type}");
				}
				if (!threats.TryGetValue(RoundManager.Instance.tempColliderResults[i].transform, out var value))
				{
					value = new Threat();
				}
				else
				{
					if (Time.realtimeSinceStartup - value.timeLastSeen < 0.5f)
					{
						continue;
					}
					value.distanceMovedTowardsBaboon = value.distanceToThreat - num6;
				}
				value.type = component2.type;
				value.timeLastSeen = Time.realtimeSinceStartup;
				value.lastSeenPosition = RoundManager.Instance.tempColliderResults[i].transform.position + Vector3.up * 0.5f;
				value.distanceToThreat = num6;
				value.threatLevel = component2.GetThreatLevel(eye.position);
				value.threatScript = component2;
				if (value.distanceMovedTowardsBaboon > 1f)
				{
					value.threatLevel++;
				}
				else if (Mathf.Abs(value.distanceMovedTowardsBaboon) < 1f || value.distanceMovedTowardsBaboon < -1f)
				{
					value.threatLevel--;
				}
				value.interestLevel = component2.GetInterestLevel();
				float num9 = (float)value.threatLevel / (value.distanceToThreat * 0.2f);
				Debug.Log($"Threat type : {value.type.ToString()}; threat level: {value.threatLevel} ; with distance: {num9}");
				if (Vector3.Distance(value.lastSeenPosition, baboonCampPosition) < 9f)
				{
					value.interestLevel += 2;
					num9 *= 0.5f;
				}
				if (value.hasAttacked)
				{
					value.interestLevel++;
					num9 = ((scoutingGroup == null || scoutingGroup.members.Count <= 3) ? (num9 + 2f) : (num9 - (float)scoutingGroup.members.Count / 1.5f));
				}
				num3 += num9;
				num4 += (float)value.threatLevel;
				if ((float)value.threatLevel < num2)
				{
					threat2 = value;
					num2 = value.threatLevel;
				}
				else if (num9 > num)
				{
					num = num9;
					threat = value;
				}
				threats.TryAdd(RoundManager.Instance.tempColliderResults[i].transform, value);
			}
		}
		oddAIInterval = !oddAIInterval;
		if (oddAIInterval && aggressiveMode != 2 && !eyesClosed && !restingAtCamp)
		{
			GrabbableObject grabbableObject = null;
			int num10 = 0;
			num5 = Physics.OverlapSphereNonAlloc(eye.position + eye.forward * 28f + eye.up * 6f, 30f, RoundManager.Instance.tempColliderResults, scrapMask, QueryTriggerInteraction.Collide);
			for (int j = 0; j < num5; j++)
			{
				float num11 = Vector3.Angle(RoundManager.Instance.tempColliderResults[j].transform.position - eye.position, eye.forward);
				float num6 = Vector3.Distance(eye.position, RoundManager.Instance.tempColliderResults[j].transform.position);
				RaycastHit hitInfo2;
				if (num6 > 2f && num11 > fov)
				{
					Debug.Log($"Baboon #{thisEnemyIndex}; could not see threat, b");
				}
				else if (!Physics.Linecast(base.transform.position + Vector3.up * 0.7f, RoundManager.Instance.tempColliderResults[j].transform.position + Vector3.up * 0.5f, out hitInfo2, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && num6 < 20f && (bool)RoundManager.Instance.tempColliderResults[j].gameObject.GetComponent<GrabbableObject>())
				{
					GrabbableObject component3 = RoundManager.Instance.tempColliderResults[j].gameObject.GetComponent<GrabbableObject>();
					if (component3.scrapValue > 3 && component3.scrapValue > num10 && CanGrabScrap(component3))
					{
						num10 = component3.scrapValue;
						grabbableObject = component3;
					}
				}
			}
			if (grabbableObject != null)
			{
				focusedScrap = grabbableObject;
			}
		}
		if (biggestBaboon != this)
		{
			JoinScoutingGroup(biggestBaboon);
		}
		if (scoutingGroup != null)
		{
			num3 -= (float)scoutingGroup.members.Count;
			num4 -= (float)scoutingGroup.members.Count;
		}
		fearLevel = num3 + 1f;
		fearLevelNoDistComparison = num4;
		float num12 = 0f;
		if (focusingOnThreat)
		{
			num12 = 2f;
		}
		if (fearLevel > num12 && threat != null)
		{
			ReactToThreat(threat);
		}
		else if (fearLevel <= 0f - num12 && threat2 != null)
		{
			ReactToThreat(threat2);
		}
	}

	public void PingBaboonInterest(Vector3 interestPosition, int pingImportance)
	{
		if (focusedThreat != null && pingImportance < focusLevel)
		{
			Debug.Log($"Baboon bird #{thisEnemyIndex}: Did NOT listen to ping of importance {pingImportance} as focus level is {focusLevel}");
		}
		else if ((pingImportance >= focusLevel || !(timeSincePingingBirdInterest < Mathf.Max(0.6f, (float)focusLevel / 2f))) && (!focusingOnThreat || !(Vector3.Distance(focusedThreat.lastSeenPosition, interestPosition) < 4f)))
		{
			timeSincePingingBirdInterest = 0f;
			peekTimer = 0.7f / (float)Mathf.Max(focusLevel / Mathf.Max(pingImportance, 1), 1);
			peekTarget = interestPosition;
			if (currentBehaviourStateIndex == 1)
			{
				eyesClosed = false;
				peekTimer = Mathf.Max(peekTimer, 1.5f);
			}
			PingBirdInterestServerRpc(peekTarget, peekTimer);
		}
	}

	[ServerRpc]
	public void PingBirdInterestServerRpc(Vector3 lookPosition, float timeToPeek)
{		{
			PingBirdInterestClientRpc(lookPosition, timeToPeek);
		}
}
	[ClientRpc]
	public void PingBirdInterestClientRpc(Vector3 lookPosition, float timeToPeek)
{if(!base.IsOwner)			{
				peekTimer = timeToPeek;
				peekTarget = lookPosition;
			}
}
	private void JoinScoutingGroup(BaboonBirdAI otherBaboon)
	{
		if ((otherBaboon.scoutingGroup == null || otherBaboon.scoutingGroup != scoutingGroup || !otherBaboon.scoutingGroup.members.Contains(this)) && !PathIsIntersectedByLineOfSight(otherBaboon.transform.position, calculatePathDistance: true, avoidLineOfSight: false) && !(Vector3.Distance(base.transform.position, otherBaboon.transform.position) > 56f))
		{
			timeSinceJoiningOrLeavingScoutingGroup = 0f;
			if (otherBaboon.scoutingGroup != scoutingGroup)
			{
				LeaveCurrentScoutingGroup(sync: false);
			}
			if (otherBaboon.scoutingGroup == null)
			{
				otherBaboon.StartScoutingGroup(this, syncWithClients: true);
			}
			else if (scoutingGroup == null)
			{
				scoutingGroup = otherBaboon.scoutingGroup;
				JoinScoutingGroupServerRpc(otherBaboon.NetworkObject);
				StartMiscAnimationServerRpc(0);
			}
		}
	}

	public void StartScoutingGroup(BaboonBirdAI firstMember, bool syncWithClients)
	{
		if (scoutingGroup != null)
		{
			return;
		}
		timeSinceJoiningOrLeavingScoutingGroup = 0f;
		scoutingGroup = new BaboonHawkGroup();
		scoutingGroup.leader = this;
		scoutingGroup.members.Add(firstMember);
		firstMember.scoutingGroup = scoutingGroup;
		scoutingGroup.isEmpty = false;
		if (syncWithClients)
		{
			if (miscAnimationTimer <= 0f)
			{
				StartMiscAnimationServerRpc(0);
			}
			StartScoutingGroupServerRpc(firstMember.NetworkObject);
		}
	}

	private void LeaveCurrentScoutingGroup(bool sync)
	{
		if (scoutingGroup == null)
		{
			return;
		}
		timeSinceJoiningOrLeavingScoutingGroup = 0f;
		if (scoutingGroup.members.Contains(this))
		{
			scoutingGroup.members.Remove(this);
			if (scoutingGroup.members.Count <= 0)
			{
				scoutingGroup.isEmpty = true;
			}
		}
		else if (scoutingGroup.leader == this)
		{
			if (scoutingGroup.members != null && scoutingGroup.members.Count > 0)
			{
				int num = -1;
				int index = -1;
				for (int i = 0; i < scoutingGroup.members.Count; i++)
				{
					if (scoutingGroup.members[i].leadershipLevel > num)
					{
						index = i;
						num = scoutingGroup.members[i].leadershipLevel;
					}
				}
				scoutingGroup.leader = scoutingGroup.members[index];
				scoutingGroup.members.RemoveAt(index);
			}
			else
			{
				scoutingGroup.isEmpty = true;
			}
		}
		else
		{
			Debug.LogError($"Baboon #{thisEnemyIndex}: Scouting group was not null but did not contain me as a member!");
		}
		scoutingGroup = null;
	}

	[ServerRpc]
	public void LeaveScoutingGroupServerRpc()
{		{
			LeaveScoutingGroupClientRpc();
		}
}
	[ClientRpc]
	public void LeaveScoutingGroupClientRpc()
			{
				LeaveCurrentScoutingGroup(sync: false);
			}

	[ServerRpc]
	public void StartScoutingGroupServerRpc(NetworkObjectReference leaderNetworkObject)
{		{
			StartScoutingGroupClientRpc(leaderNetworkObject);
		}
}
	[ClientRpc]
	public void StartScoutingGroupClientRpc(NetworkObjectReference leaderNetworkObject)
{		{
			if (!leaderNetworkObject.TryGet(out var networkObject))
			{
				Debug.LogError($"Baboon enemy #{thisEnemyIndex}: Could not get network object from reference in JoinScoutingGroupClientRpc; {leaderNetworkObject.NetworkObjectId}");
				return;
			}
			BaboonBirdAI component = networkObject.gameObject.GetComponent<BaboonBirdAI>();
			StartScoutingGroup(component, syncWithClients: false);
		}
}
	[ServerRpc]
	public void JoinScoutingGroupServerRpc(NetworkObjectReference otherBaboonNetworkObject)
{		{
			JoinScoutingGroupClientRpc(otherBaboonNetworkObject);
		}
}
	[ClientRpc]
	public void JoinScoutingGroupClientRpc(NetworkObjectReference otherBaboonNetworkObject)
{		if (!otherBaboonNetworkObject.TryGet(out var networkObject))
		{
			Debug.LogError($"Baboon enemy #{thisEnemyIndex}: Could not get network object from reference in JoinScoutingGroupClientRpc; {otherBaboonNetworkObject.NetworkObjectId}");
			return;
		}
		BaboonBirdAI component = networkObject.gameObject.GetComponent<BaboonBirdAI>();
		if ((component.scoutingGroup != scoutingGroup || !component.scoutingGroup.members.Contains(this)) && component.scoutingGroup != null)
		{
			if (component.scoutingGroup != scoutingGroup)
			{
				LeaveCurrentScoutingGroup(sync: false);
			}
			scoutingGroup = component.scoutingGroup;
			if (!scoutingGroup.members.Contains(this))
			{
				scoutingGroup.members.Add(this);
			}
		}
}
	public void CallToOtherBaboon(BaboonBirdAI otherBaboon)
	{
		if (!(timeSinceJoiningOrLeavingScoutingGroup <= 1f))
		{
			if (scoutingGroup != null)
			{
				scoutingGroup.timeAtLastCallToGroup = Time.realtimeSinceStartup;
			}
			StartMiscAnimation(0);
			otherBaboon.PingBaboonInterest(base.transform.position, 2);
		}
	}

	private void StartMiscAnimation(int anim)
	{
		if (!isEnemyDead && !(timeSinceLastMiscAnimation <= 0.4f))
		{
			timeSinceLastMiscAnimation = 0f;
			StartMiscAnimationServerRpc(anim);
		}
	}

	[ServerRpc]
	public void StartMiscAnimationServerRpc(int miscAnimationId)
{if(!isEnemyDead && enemyType.miscAnimations.Length > miscAnimationId && !(creatureVoice == null) && (currentMiscAnimation == -1 || enemyType.miscAnimations[currentMiscAnimation].priority <= enemyType.miscAnimations[miscAnimationId].priority))		{
			StartMiscAnimationClientRpc(miscAnimationId);
		}
}
	[ClientRpc]
	public void StartMiscAnimationClientRpc(int miscAnimationId)
{if(!isEnemyDead && enemyType.miscAnimations.Length > miscAnimationId && !(creatureVoice == null) && (currentMiscAnimation == -1 || enemyType.miscAnimations[currentMiscAnimation].priority <= enemyType.miscAnimations[miscAnimationId].priority))		{
			currentMiscAnimation = miscAnimationId;
			miscAnimationTimer = enemyType.miscAnimations[miscAnimationId].AnimLength;
			if (!inSpecialAnimation || doingKillAnimation)
			{
				creatureVoice.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
				creatureVoice.PlayOneShot(enemyType.miscAnimations[miscAnimationId].AnimVoiceclip, UnityEngine.Random.Range(0.6f, 1f));
				WalkieTalkie.TransmitOneShotAudio(creatureVoice, enemyType.miscAnimations[miscAnimationId].AnimVoiceclip, 0.7f);
				creatureAnimator.ResetTrigger(enemyType.miscAnimations[miscAnimationId].AnimString);
				creatureAnimator.SetTrigger(enemyType.miscAnimations[miscAnimationId].AnimString);
			}
		}
}
	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = base.transform.position;
	}
}
