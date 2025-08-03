using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;

public class BushWolfEnemy : EnemyAI, IVisibleThreat
{
	[Header("Bush Wolf Variables")]
	public float changeNestRangeSpeed = 0.75f;

	public float dragForce = 7f;

	public float nestRange = 15f;

	private float baseNestRange;

	public float attackDistance = 5f;

	private float baseAttackDistance;

	public float speedMultiplier;

	[Space(5f)]
	public Transform[] proceduralBodyTargets;

	public Transform[] IKTargetContainers;

	private float resetIKOffsetsInterval;

	private RaycastHit hit;

	public bool hideBodyOnTerrain;

	private int previousState = -1;

	private Collider[] nearbyColliders;

	private Vector3 aggressivePosition;

	private Vector3 mostHiddenPosition;

	private bool foundSpawningPoint;

	private bool inKillAnimation;

	private float velX;

	private float velZ;

	private Vector3 previousPosition;

	private Vector3 agentLocalVelocity;

	public Transform animationContainer;

	private float timeSpentHiding;

	private float timeSinceAdjustingPosition;

	private Vector3 currentHidingSpot;

	public bool isHiding;

	public PlayerControllerB staringAtPlayer;

	public PlayerControllerB lastPlayerStaredAt;

	private bool backedUpFromWatchingPlayer;

	private float staringAtPlayerTimer;

	public float spottedMeter;

	private int checkForPlayerDistanceInterval;

	private int checkPlayer;

	public Vector3 rotAxis;

	public Transform turnCompass;

	private float maxAnimSpeed = 1.25f;

	private bool looking;

	private bool dragging;

	private bool startedShootingTongue;

	private float shootTongueTimer;

	public Transform tongue;

	public Transform tongueStartPoint;

	private float tongueLengthNormalized;

	private bool failedTongueShoot;

	private float randomForceInterval;

	private PlayerControllerB lastHitByPlayer;

	private float timeSinceTakingDamage;

	private float timeSinceKillingPlayer;

	private Coroutine killPlayerCoroutine;

	private DeadBodyInfo body;

	public Transform playerBodyHeadPoint;

	private float timeSinceChangingState;

	private Transform tongueTarget;

	private float tongueScale;

	public AudioClip snarlSFX;

	public AudioClip[] growlSFX;

	public AudioSource growlAudio;

	public AudioClip shootTongueSFX;

	private float timeAtLastGrowl;

	private bool playedTongueAudio;

	public AudioSource tongueAudio;

	public AudioClip tongueShootSFX;

	private bool changedHidingSpot;

	public ParticleSystem spitParticle;

	public Transform playerAnimationHeadPoint;

	public PlayerControllerB draggingPlayer;

	private float timeSinceCheckHomeBase;

	private int timesFailingTongueShoot;

	public Rig bendHeadBack;

	private float timeSinceLOSBlocked;

	public AudioClip killSFX;

	private float timeSinceCall;

	public AudioSource callClose;

	public AudioSource callFar;

	private float matingCallTimer;

	public AudioClip[] callsClose;

	public AudioClip[] callsFar;

	public AudioClip hitBushWolfSFX;

	private float timeSinceHitting;

	private float noTargetTimer;

	private float waitOutsideShipTimer;

	private Vector3 hiddenPos;

	ThreatType IVisibleThreat.type => ThreatType.BushWolf;

	bool IVisibleThreat.IsThreatDead()
	{
		return isEnemyDead;
	}

	GrabbableObject IVisibleThreat.GetHeldObject()
	{
		return null;
	}

	int IVisibleThreat.SendSpecialBehaviour(int id)
	{
		return 0;
	}

	int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
	{
		int num = 0;
		if (dragging && !startedShootingTongue)
		{
			return 1;
		}
		num = ((enemyHP >= 2) ? 3 : 2);
		if (Time.realtimeSinceStartup - timeSinceCall < 5f)
		{
			num += 3;
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
		return agentLocalVelocity;
	}

	float IVisibleThreat.GetVisibility()
	{
		if (isEnemyDead)
		{
			return 0f;
		}
		if ((dragging && !startedShootingTongue) || Time.realtimeSinceStartup - timeSinceCall < 5f)
		{
			return 0.8f;
		}
		if (Vector3.Distance(base.transform.position, currentHidingSpot) < baseNestRange - 3f)
		{
			return 0.18f;
		}
		return 0.5f;
	}

	public override void AnimationEventA()
	{
		base.AnimationEventA();
		spitParticle.Play(withChildren: true);
	}

	public override void Start()
	{
		base.Start();
		resetIKOffsetsInterval = 15f;
		nearbyColliders = new Collider[10];
		EnableEnemyMesh(enable: false);
		inSpecialAnimation = true;
		agent.enabled = false;
		baseAttackDistance = attackDistance;
		if (base.IsServer)
		{
			if (!GetBiggestWeedPatch())
			{
				KillEnemyOnOwnerClient(overrideDestroy: true);
				return;
			}
			CalculateNestRange(useCurrentHidingSpot: false);
			SyncWeedPositionsServerRpc(mostHiddenPosition, aggressivePosition, nestRange);
		}
	}

	private void CalculateNestRange(bool useCurrentHidingSpot)
	{
		Vector3 a = mostHiddenPosition;
		if (useCurrentHidingSpot)
		{
			a = currentHidingSpot;
		}
		Collider[] array = Physics.OverlapSphere(mostHiddenPosition, 50f, 65536, QueryTriggerInteraction.Collide);
		if (array == null)
		{
			Debug.Log("Got no colliders for nest range");
			return;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			num2 = Vector3.Distance(a, array[i].transform.position);
			if (num2 > num3)
			{
				num3 = num2;
			}
			num += num2;
		}
		num = num / (float)array.Length + 16f;
		nestRange = num;
		baseNestRange = nestRange;
	}

	public override void OnDrawGizmos()
	{
		base.OnDrawGizmos();
		if (debugEnemyAI)
		{
			Gizmos.DrawWireSphere(mostHiddenPosition, nestRange);
			Gizmos.DrawSphere(hiddenPos, 5f);
		}
	}

	[ServerRpc]
	public void SyncWeedPositionsServerRpc(Vector3 hiddenPosition, Vector3 aggressivePosition, float nest)
{		{
			SyncWeedPositionsClientRpc(hiddenPosition, aggressivePosition, nest);
		}
}
	[ClientRpc]
	public void SyncWeedPositionsClientRpc(Vector3 hiddenPosition, Vector3 agg, float nest)
			{
				mostHiddenPosition = hiddenPosition;
				aggressivePosition = agg;
				currentHidingSpot = mostHiddenPosition;
				nestRange = nest;
				baseNestRange = nest;
			}

	private bool GetBiggestWeedPatch()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("MoldSpore");
		if (array == null || array.Length == 0)
		{
			Debug.Log("Bush wolf: No game objects found with spore tag; cancelling");
			return false;
		}
		GameObject gameObject = GameObject.FindGameObjectWithTag("MoldAttractionPoint");
		int num = 0;
		int num2 = 0;
		List<GameObject> list = new List<GameObject>();
		GameObject gameObject2 = array[0];
		for (int i = 0; i < array.Length; i++)
		{
			num2 = Physics.OverlapSphereNonAlloc(array[i].transform.position, 5f, nearbyColliders, 65536, QueryTriggerInteraction.Collide);
			Debug.Log($"{i}: Mold spore {array[i].gameObject.name} at {array[i].transform.position} surrounded by {num2}");
			if ((num >= 3 && num - num2 >= 1) || num2 >= 2)
			{
				list.Add(array[i]);
			}
			if (num2 > num)
			{
				num = num2;
				gameObject2 = array[i];
			}
		}
		Debug.Log($"Bush wolf: Most surrounding spores is {num}");
		Debug.DrawRay(gameObject2.transform.position, Vector3.up * 5f, Color.green, 10f);
		if (num == 0)
		{
			Debug.Log("Bush wolf: All spores found were lone spores; cancelling");
			return false;
		}
		mostHiddenPosition = RoundManager.Instance.GetNavMeshPosition(gameObject2.transform.position, default(NavMeshHit), 8f);
		if (gameObject != null)
		{
			if (Vector3.Distance(gameObject2.transform.position, gameObject.transform.position) > 35f)
			{
				float num3 = 45f;
				float num4 = 0f;
				GameObject gameObject3 = null;
				for (int j = 0; j < list.Count; j++)
				{
					num4 = Vector3.Distance(list[j].transform.position, gameObject.transform.position);
					if (num4 < num3)
					{
						num3 = num4;
						gameObject3 = list[j];
					}
				}
				if (gameObject3 != null)
				{
					aggressivePosition = RoundManager.Instance.GetNavMeshPosition(gameObject3.transform.position, default(NavMeshHit), 8f);
					if (!RoundManager.Instance.GotNavMeshPositionResult)
					{
						aggressivePosition = mostHiddenPosition;
					}
				}
			}
			else
			{
				aggressivePosition = mostHiddenPosition;
			}
		}
		return true;
	}

	public PlayerControllerB GetClosestPlayerToNest()
	{
		PlayerControllerB result = null;
		mostOptimalDistance = 2000f;
		for (int i = 0; i < 4; i++)
		{
			if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]))
			{
				tempDist = Vector3.Distance(currentHidingSpot, StartOfRound.Instance.allPlayerScripts[i].transform.position);
				if (tempDist < mostOptimalDistance)
				{
					mostOptimalDistance = tempDist;
					result = StartOfRound.Instance.allPlayerScripts[i];
				}
			}
		}
		return result;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || !foundSpawningPoint || isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (checkForPlayerDistanceInterval < 1)
			{
				checkForPlayerDistanceInterval++;
				break;
			}
			checkForPlayerDistanceInterval = 0;
			PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[checkPlayer];
			if (PlayerIsTargetable(playerControllerB) && !PathIsIntersectedByLineOfSight(playerControllerB.transform.position, calculatePathDistance: true, avoidLineOfSight: false))
			{
				float num = Vector3.Distance(base.transform.position, playerControllerB.transform.position);
				bool flag = (playerControllerB.timeSincePlayerMoving > 0.7f && num < attackDistance - 4.75f) || num < attackDistance - 7f;
				if (timeSinceChangingState > 0.35f && flag && !Physics.Linecast(base.transform.position + Vector3.up * 0.6f, playerControllerB.gameplayCamera.transform.position - Vector3.up * 0.3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					SwitchToBehaviourStateOnLocalClient(2);
					SyncTargetPlayerAndAttackServerRpc((int)playerControllerB.playerClientId);
				}
				else if (pathDistance < nestRange && Vector3.Distance(playerControllerB.transform.position, currentHidingSpot) < nestRange)
				{
					Debug.Log($"Beginning attack on '{playerControllerB.playerUsername}'; distance: {pathDistance}");
					targetPlayer = playerControllerB;
					SwitchToBehaviourState(1);
				}
			}
			checkPlayer = (checkPlayer + 1) % StartOfRound.Instance.allPlayerScripts.Length;
			break;
		}
		case 1:
		{
			bool flag2 = false;
			PlayerControllerB closestPlayer = GetClosestPlayer();
			if (closestPlayer != null)
			{
				flag2 = true;
				float num2 = mostOptimalDistance;
				PlayerControllerB closestPlayerToNest = GetClosestPlayerToNest();
				if (closestPlayerToNest != null && closestPlayerToNest != targetPlayer && (num2 > 9f || spottedMeter > 0.45f) && Vector3.Distance(base.transform.position, closestPlayerToNest.transform.position) - num2 < 15f)
				{
					SetMovingTowardsTargetPlayer(closestPlayerToNest);
				}
				else
				{
					SetMovingTowardsTargetPlayer(closestPlayer);
				}
			}
			if (flag2)
			{
				noTargetTimer = 0f;
				float num3 = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
				bool flag3 = (targetPlayer.timeSincePlayerMoving > 0.7f && num3 < attackDistance - 5f) || num3 < attackDistance - 7f;
				if (Vector3.Distance(currentHidingSpot, targetPlayer.transform.position) > nestRange + 9f)
				{
					timeSinceAdjustingPosition = 0f;
					SwitchToBehaviourState(0);
				}
				else if (timeSinceChangingState > 0.35f && flag3 && !Physics.Linecast(base.transform.position + Vector3.up * 0.6f, targetPlayer.gameplayCamera.transform.position - Vector3.up * 0.3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					SwitchToBehaviourStateOnLocalClient(2);
					SyncTargetPlayerAndAttackServerRpc((int)targetPlayer.playerClientId);
				}
				else
				{
					ChooseClosestNodeToPlayer();
				}
			}
			else if (timeSinceChangingState > 0.25f)
			{
				if (noTargetTimer > 0.5f)
				{
					noTargetTimer = 0f;
					timeSinceAdjustingPosition = 0f;
					SwitchToBehaviourState(0);
				}
				else
				{
					noTargetTimer += AIIntervalTime;
				}
			}
			break;
		}
		case 2:
			if (dragging && !startedShootingTongue && (PathIsIntersectedByLineOfSight(currentHidingSpot, calculatePathDistance: false, avoidLineOfSight: false) || shootTongueTimer > 35f || (shootTongueTimer > 12f && Vector3.Distance(base.transform.position, currentHidingSpot) > 20f)))
			{
				timesFailingTongueShoot++;
				SwitchToBehaviourState(1);
			}
			break;
		}
	}

	[ServerRpc]
	public void SyncTargetPlayerAndAttackServerRpc(int playerId)
{		{
			SyncTargetPlayerAndAttackClientRpc(playerId);
		}
}
	[ClientRpc]
	public void SyncTargetPlayerAndAttackClientRpc(int playerId)
{if(!base.IsOwner)			{
				targetPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
				SwitchToBehaviourStateOnLocalClient(2);
			}
}
	[ServerRpc]
	public void SyncNewHidingSpotServerRpc(Vector3 newHidingSpot, float nest)
{		{
			SyncNewHidingSpotClientRpc(newHidingSpot, nest);
		}
}
	[ClientRpc]
	public void SyncNewHidingSpotClientRpc(Vector3 newHidingSpot, float nest)
{if(!base.IsOwner)			{
				currentHidingSpot = newHidingSpot;
				nestRange = nest;
				baseNestRange = nest;
			}
}
	public Transform ChooseClosestHiddenNode(Vector3 pos)
	{
		nodesTempArray = allAINodes.OrderBy((GameObject x) => Vector3.Distance(pos, x.transform.position)).ToArray();
		Transform transform = null;
		if (targetNode != null)
		{
			Physics.Linecast(targetPlayer.gameplayCamera.transform.position, targetNode.transform.position, 1024, QueryTriggerInteraction.Ignore);
		}
		else
			_ = 0;
		for (int i = 0; i < nodesTempArray.Length; i++)
		{
			if (!(Vector3.Distance(nodesTempArray[i].transform.position, currentHidingSpot) > nestRange) && !PathIsIntersectedByLineOfSight(nodesTempArray[i].transform.position))
			{
				mostOptimalDistance = Vector3.Distance(pos, nodesTempArray[i].transform.position);
				if (transform != null && mostOptimalDistance - Vector3.Distance(transform.position, pos) > 10f)
				{
					break;
				}
				if (Physics.Linecast(targetPlayer.gameplayCamera.transform.position, nodesTempArray[i].transform.position, 1024, QueryTriggerInteraction.Ignore))
				{
					transform = nodesTempArray[i].transform;
					break;
				}
				if (!targetPlayer.HasLineOfSightToPosition(nodesTempArray[i].transform.position + Vector3.up * 1.5f, 110f, 40))
				{
					transform = nodesTempArray[i].transform;
					break;
				}
				if (spottedMeter > 0.9f && Vector3.Distance(base.transform.position, targetPlayer.transform.position) < 12f)
				{
					transform = nodesTempArray[i].transform;
				}
			}
		}
		if (transform != null)
		{
			mostOptimalDistance = Vector3.Distance(pos, transform.transform.position);
		}
		return transform;
	}

	public void ChooseClosestNodeToPlayer()
	{
		if (targetNode == null)
		{
			targetNode = allAINodes[0].transform;
		}
		Transform transform = ChooseClosestHiddenNode(targetPlayer.transform.position);
		if (transform != null)
		{
			targetNode = transform;
		}
		Vector3 vector = Vector3.Normalize((base.transform.position - targetPlayer.transform.position) * 100f) * 4f;
		vector.y = 0f;
		if (targetNode != null)
		{
			mostOptimalDistance = Vector3.Distance(base.transform.position, targetNode.position + vector);
		}
		float num = targetPlayer.LineOfSightToPositionAngle(base.transform.position + Vector3.up * 0.75f, 70);
		float num2 = Vector3.Distance(targetPlayer.transform.position, base.transform.position);
		bool flag = num != -361f && num2 < 10f && ((num < 10f && spottedMeter > 0.25f) || (num > 90f && Vector3.Distance(targetPlayer.transform.position, currentHidingSpot) < nestRange - 7f));
		if (targetPlayer.isInElevator)
		{
			if (waitOutsideShipTimer > 18f && Vector3.Distance(base.transform.position, StartOfRound.Instance.elevatorTransform.position) < 27f)
			{
				movingTowardsTargetPlayer = true;
			}
			else
			{
				SetDestinationToPosition(targetNode.position + vector, checkForPath: true);
			}
		}
		else if (flag || (num2 - mostOptimalDistance < 0.5f && (!PathIsIntersectedByLineOfSight(targetPlayer.transform.position) || num2 < 8f)))
		{
			movingTowardsTargetPlayer = true;
		}
		else
		{
			SetDestinationToPosition(targetNode.position + vector, checkForPath: true);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetAnimationSpeedServerRpc(float animSpeed)
			{
				SetAnimationSpeedClientRpc(animSpeed);
			}

	[ClientRpc]
	public void SetAnimationSpeedClientRpc(float animSpeed)
{if(!base.IsOwner)			{
				maxAnimSpeed = animSpeed;
			}
}
	public override void SetEnemyStunned(bool setToStunned, float setToStunTime = 1f, PlayerControllerB setStunnedByPlayer = null)
	{
		base.SetEnemyStunned(setToStunned, setToStunTime, setStunnedByPlayer);
		SwitchToBehaviourState(0);
		CancelKillAnimation();
		if (dragging && !startedShootingTongue && targetPlayer != null)
		{
			dragging = false;
			targetPlayer.inShockingMinigame = false;
			targetPlayer.inSpecialInteractAnimation = false;
		}
		if (setStunnedByPlayer != null)
		{
			lastHitByPlayer = setStunnedByPlayer;
		}
		timeSinceTakingDamage = 0f;
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		enemyHP -= force;
		if (enemyHP <= 0 && !isEnemyDead && base.IsOwner)
		{
			KillEnemyOnOwnerClient();
		}
		creatureAnimator.SetTrigger("HitEnemy");
		timeSinceTakingDamage = 0f;
		if (playerWhoHit != null)
		{
			lastHitByPlayer = playerWhoHit;
		}
		if (base.IsOwner)
		{
			CancelKillAnimation();
			if (currentBehaviourStateIndex == 2)
			{
				SwitchToBehaviourState(0);
			}
		}
		CancelReelingPlayerIn();
		creatureVoice.PlayOneShot(hitBushWolfSFX);
	}

	private void CancelReelingPlayerIn()
	{
		if (previousState == 2 || previousState == 1 || currentBehaviourStateIndex == 2)
		{
			if (dragging)
			{
				creatureVoice.Stop();
			}
			growlAudio.Stop();
			tongueAudio.Stop();
			playedTongueAudio = false;
			tongueTarget = null;
			tongue.localScale = Vector3.one;
			creatureAnimator.SetBool("ReelingPlayerIn", value: false);
			creatureAnimator.SetBool("ShootTongue", value: false);
			shootTongueTimer = 0f;
			timeSinceAdjustingPosition = 0f;
			if (draggingPlayer != null)
			{
				draggingPlayer.inShockingMinigame = false;
				draggingPlayer.inSpecialInteractAnimation = false;
				draggingPlayer.shockingTarget = null;
				draggingPlayer.disableInteract = false;
			}
			draggingPlayer = null;
			dragging = false;
			startedShootingTongue = false;
		}
	}

	private void DoGrowlLocalClient()
	{
		timeAtLastGrowl = Time.realtimeSinceStartup;
		growlAudio.PlayOneShot(growlSFX[Random.Range(0, growlSFX.Length)]);
		if (base.IsOwner)
		{
			DoGrowlServerRpc();
		}
	}

	[ServerRpc]
	public void DoGrowlServerRpc()
{		{
			DoGrowlClientRpc();
		}
}
	[ClientRpc]
	public void DoGrowlClientRpc()
{if(!base.IsOwner)			{
				DoGrowlLocalClient();
			}
}
	[ServerRpc]
	public void SeeBushWolfServerRpc(int playerId)
{		{
			SeeBushWolfClientRpc(playerId);
		}
}
	[ClientRpc]
	public void SeeBushWolfClientRpc(int playerId)
{if(playerId == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)			{
				SetFearLevelFromBushWolf();
			}
}
	private void SetFearLevelFromBushWolf()
	{
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		if (Vector3.Distance(localPlayerController.transform.position, base.transform.position) < 10f)
		{
			localPlayerController.JumpToFearLevel(0.6f);
		}
		else
		{
			localPlayerController.JumpToFearLevel(0.3f);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		CancelReelingPlayerIn();
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		if (!destroy)
		{
			growlAudio.Stop();
			callClose.Stop();
			callFar.Stop();
			creatureSFX.Stop();
			creatureVoice.Stop();
			CancelKillAnimation();
			creatureVoice.PlayOneShot(dieSFX);
			WalkieTalkie.TransmitOneShotAudio(creatureVoice, dieSFX);
			RoundManager.Instance.PlayAudibleNoise(creatureVoice.transform.position, 20f, 0.7f, 0, isInsidePlayerShip && StartOfRound.Instance.hangarDoorsClosed);
			CancelReelingPlayerIn();
		}
	}

	public void HitTongue(PlayerControllerB playerWhoHit, int hitID)
	{
		if (dragging && !startedShootingTongue)
		{
			int playerWhoHit2 = -1;
			if (playerWhoHit != null)
			{
				playerWhoHit2 = (int)playerWhoHit.playerClientId;
				HitTongueLocalClient();
			}
			HitTongueServerRpc(playerWhoHit2);
		}
	}

	private void HitTongueLocalClient()
	{
		timeSinceTakingDamage = 0f;
		creatureVoice.PlayOneShot(hitBushWolfSFX);
		creatureAnimator.ResetTrigger("HitEnemy");
		creatureAnimator.SetTrigger("HitEnemy");
		CancelReelingPlayerIn();
		SwitchToBehaviourStateOnLocalClient(0);
	}

	[ServerRpc(RequireOwnership = false)]
	public void HitTongueServerRpc(int playerWhoHit)
			{
				HitTongueClientRpc(playerWhoHit);
			}

	[ClientRpc]
	public void HitTongueClientRpc(int playerWhoHit)
{if((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerWhoHit)			{
				HitTongueLocalClient();
			}
}
	private void CheckHomeBase(bool overrideInterval = false)
	{
		if (!base.IsOwner || ((!overrideInterval || !(Time.realtimeSinceStartup - timeSinceCheckHomeBase > 5f)) && !(Time.realtimeSinceStartup - timeSinceCheckHomeBase > 30f)))
		{
			return;
		}
		timeSinceCheckHomeBase = Time.realtimeSinceStartup;
		Vector3 vector = mostHiddenPosition;
		if (GetBiggestWeedPatch())
		{
			if (vector != mostHiddenPosition || changedHidingSpot)
			{
				CalculateNestRange(useCurrentHidingSpot: false);
			}
			changedHidingSpot = false;
			SyncWeedPositionsClientRpc(mostHiddenPosition, aggressivePosition, nestRange);
		}
	}

	private void DoMatingCall()
	{
		if (base.IsOwner)
		{
			MatingCallServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void MatingCallServerRpc()
			{
				MatingCallClientRpc();
			}

	[ClientRpc]
	public void MatingCallClientRpc()
			{
				matingCallTimer = 2.2f;
				creatureAnimator.SetTrigger("MatingCall");
				int num = Random.Range(0, callsClose.Length);
				callClose.PlayOneShot(callsClose[num]);
				WalkieTalkie.TransmitOneShotAudio(callClose, callsClose[num]);
				callFar.PlayOneShot(callsFar[num]);
				WalkieTalkie.TransmitOneShotAudio(callFar, callsFar[num]);
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 20f, 0.6f, 0, noiseIsInsideClosedShip: false, 245403);
			}

	public override void Update()
	{
		base.Update();
		if (looking)
		{
			looking = false;
		}
		else
		{
			agent.updateRotation = true;
		}
		if (!foundSpawningPoint)
		{
			if (!(mostHiddenPosition != Vector3.zero))
			{
				return;
			}
			Debug.DrawRay(mostHiddenPosition, Vector3.up * 2f, Color.red, 15f);
			foundSpawningPoint = true;
			Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(mostHiddenPosition, default(NavMeshHit), 12f);
			if (!RoundManager.Instance.GotNavMeshPositionResult)
			{
				navMeshPosition = RoundManager.Instance.GetNavMeshPosition(aggressivePosition, default(NavMeshHit), 12f);
				if (!RoundManager.Instance.GotNavMeshPositionResult && base.IsOwner)
				{
					KillEnemyOnOwnerClient(overrideDestroy: true);
				}
			}
			base.transform.position = navMeshPosition;
			currentHidingSpot = navMeshPosition;
			inSpecialAnimation = false;
			EnableEnemyMesh(enable: true);
			isHiding = true;
			if (base.IsOwner)
			{
				SetDestinationToPosition(mostHiddenPosition);
			}
			return;
		}
		if (!ventAnimationFinished)
		{
			serverPosition = mostHiddenPosition;
			base.transform.position = mostHiddenPosition;
		}
		if (inKillAnimation || isEnemyDead)
		{
			tongueTarget = null;
		}
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead || !foundSpawningPoint)
		{
			return;
		}
		creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
		if (stunNormalizedTimer > 0f || matingCallTimer >= 0f)
		{
			matingCallTimer -= Time.deltaTime;
			agent.speed = 0f;
			return;
		}
		timeSinceTakingDamage += Time.deltaTime;
		timeSinceKillingPlayer += Time.deltaTime;
		timeSinceHitting += Time.deltaTime;
		CalculateAnimationDirection(maxAnimSpeed);
		timeSinceChangingState += Time.deltaTime;
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (previousState != currentBehaviourStateIndex)
			{
				SetAnimationSpeedServerRpc(1.3f);
				backedUpFromWatchingPlayer = false;
				moveTowardsDestination = true;
				movingTowardsTargetPlayer = false;
				timeSpentHiding = 0f;
				timeSinceChangingState = 0f;
				CancelReelingPlayerIn();
				nestRange = Mathf.Clamp(nestRange / 2f, baseNestRange * 0.75f, 140f);
				previousState = currentBehaviourStateIndex;
			}
			if (base.IsOwner && Vector3.Distance(base.transform.position, currentHidingSpot) < 16f && timeSinceChangingState > 0.5f && timeSinceTakingDamage < 2.5f && lastHitByPlayer != null)
			{
				SetMovingTowardsTargetPlayer(lastHitByPlayer);
				agent.speed = 6f * speedMultiplier;
				break;
			}
			if (timeSinceKillingPlayer < 2f || timeSinceTakingDamage < 0.35f)
			{
				agent.speed = 0f;
				break;
			}
			inKillAnimation = false;
			isHiding = true;
			if (!base.IsOwner)
			{
				break;
			}
			timeSpentHiding += Time.deltaTime;
			if ((bool)staringAtPlayer)
			{
				LookAtPosition(staringAtPlayer.transform.position);
				staringAtPlayerTimer += Time.deltaTime;
				if (staringAtPlayerTimer > 4f)
				{
					spottedMeter = 0f;
					backedUpFromWatchingPlayer = false;
					staringAtPlayer = null;
				}
				else if (staringAtPlayerTimer > 0.55f)
				{
					if (backedUpFromWatchingPlayer)
					{
						break;
					}
					backedUpFromWatchingPlayer = true;
					if (Vector3.Distance(base.transform.position, currentHidingSpot) < 6f)
					{
						Vector3 direction = base.transform.position - staringAtPlayer.transform.position;
						direction.y = 0f;
						Ray ray2 = new Ray(base.transform.position + Vector3.up * 0.5f, direction);
						direction = ((!Physics.Raycast(ray2, out hit, 4.6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? ray2.GetPoint(4.6f) : ray2.GetPoint(Mathf.Clamp(hit.distance - 1f, 0f, hit.distance)));
						direction = RoundManager.Instance.GetNavMeshPosition(direction);
						Debug.DrawRay(base.transform.position + Vector3.up * 0.5f, ray2.direction, Color.red, 5f);
						Debug.DrawRay(direction, Vector3.up, Color.green, 5f);
						if (RoundManager.Instance.GotNavMeshPositionResult)
						{
							SetDestinationToPosition(direction);
							moveTowardsDestination = true;
							agent.speed = 5f * speedMultiplier;
							if (Vector3.Distance(base.transform.position, staringAtPlayer.transform.position) < 18f && Random.Range(0, 100) < 50 && Time.realtimeSinceStartup - timeAtLastGrowl > 5f)
							{
								DoGrowlLocalClient();
							}
							if (Physics.Linecast(staringAtPlayer.gameplayCamera.transform.position, base.transform.position + Vector3.up * 0.5f, out hit, 1024, QueryTriggerInteraction.Ignore) && Vector3.Distance(hit.point, base.transform.position) < 6f)
							{
								SeeBushWolfServerRpc((int)staringAtPlayer.playerClientId);
							}
						}
					}
					else
					{
						agent.speed = 0f;
					}
				}
				else
				{
					agent.speed = 0f;
				}
				break;
			}
			int num3 = 0;
			int num4 = -1;
			float num5 = 2555f;
			Debug.Log("Fox A");
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				if (StartOfRound.Instance.allPlayerScripts[i].isPlayerDead || !StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled || (bool)StartOfRound.Instance.allPlayerScripts[i].inAnimationWithEnemy || Physics.Linecast(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, base.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					continue;
				}
				Debug.Log("Fox B");
				float num6 = Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].transform.position, base.transform.position);
				float num7 = StartOfRound.Instance.allPlayerScripts[i].LineOfSightToPositionAngle(base.transform.position, 40);
				if (num7 == -361f || num6 > 70f)
				{
					continue;
				}
				Debug.Log($"Fox C; {num6}; {num7}");
				Debug.DrawLine(StartOfRound.Instance.allPlayerScripts[i].transform.position, base.transform.position, Color.red);
				if (num7 < 10f || (num7 < 20f && num6 < 20f))
				{
					Debug.Log("Fox D");
					num3++;
					if (num3 >= Mathf.Max(StartOfRound.Instance.livingPlayers - 1, 0))
					{
						Debug.Log("Fox E");
						spottedMeter = Mathf.Min(spottedMeter + 0.5f * Time.deltaTime, 1f);
					}
					if (num6 < num5)
					{
						Debug.Log("Fox F");
						num5 = num6;
						num4 = i;
					}
				}
				else if (num7 < 20f)
				{
					Debug.Log("Fox G");
					num3++;
					if (num3 >= Mathf.Max(StartOfRound.Instance.livingPlayers - 1, 0))
					{
						Debug.Log("Fox H");
						spottedMeter = Mathf.Min(spottedMeter + 0.28f * Time.deltaTime, 1f);
					}
					if (num6 < num5)
					{
						Debug.Log("Fox I");
						num5 = num6;
						num4 = i;
					}
				}
			}
			if (num3 <= 0)
			{
				Debug.Log("Fox J");
				spottedMeter = Mathf.Max(spottedMeter - Time.deltaTime * 0.75f, 0f);
			}
			if (spottedMeter >= 1f && timeSinceChangingState > 1.2f)
			{
				staringAtPlayerTimer = 0f;
				backedUpFromWatchingPlayer = false;
				staringAtPlayer = StartOfRound.Instance.allPlayerScripts[num4];
				lastPlayerStaredAt = staringAtPlayer;
			}
			float num8 = 15f;
			if (timeSpentHiding > 70f && spottedMeter <= 0f && !changedHidingSpot)
			{
				currentHidingSpot = aggressivePosition;
				CalculateNestRange(useCurrentHidingSpot: true);
				SyncNewHidingSpotServerRpc(aggressivePosition, nestRange);
			}
			if (timeSpentHiding > 36f)
			{
				num8 = 4f;
			}
			else if (timeSpentHiding > 25f)
			{
				num8 = 7f;
			}
			else if (timeSpentHiding > 15f)
			{
				num8 = 10f;
			}
			Debug.Log($"Fox spotted meter: {spottedMeter}");
			if (spottedMeter > 0.2f)
			{
				nestRange = Mathf.Clamp(nestRange - changeNestRangeSpeed * Mathf.Clamp(spottedMeter * 4f, 0.2f, 4f) * Time.deltaTime, baseNestRange * 0.75f, 140f);
			}
			else
			{
				nestRange += changeNestRangeSpeed * Time.deltaTime;
			}
			if (matingCallTimer <= 0f && timeSpentHiding > 10f && spottedMeter < 0.6f && Time.realtimeSinceStartup - timeSinceCall > 7f)
			{
				timeSinceCall = Time.realtimeSinceStartup;
				if (Random.Range(0, 100) < 8)
				{
					DoMatingCall();
				}
			}
			if (Time.realtimeSinceStartup - timeSinceAdjustingPosition > num8)
			{
				timeSinceAdjustingPosition = Time.realtimeSinceStartup;
				SetDestinationToPosition(RoundManager.Instance.GetNavMeshPosition(currentHidingSpot + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f))));
				if (timeSinceTakingDamage < 2f)
				{
					agent.speed = 14f * speedMultiplier;
				}
				else
				{
					agent.speed = 10f * speedMultiplier;
				}
			}
			CheckHomeBase();
			break;
		}
		case 1:
		{
			if (previousState != currentBehaviourStateIndex)
			{
				spottedMeter = 0f;
				staringAtPlayer = null;
				timeSpentHiding = 0f;
				noTargetTimer = 0f;
				waitOutsideShipTimer = 0f;
				CancelReelingPlayerIn();
				previousState = currentBehaviourStateIndex;
			}
			if (timeSinceKillingPlayer < 2f || timeSinceTakingDamage < 0.35f)
			{
				agent.speed = 0f;
				break;
			}
			inKillAnimation = false;
			if (!base.IsOwner || targetPlayer == null)
			{
				break;
			}
			int num9 = 0;
			float num10 = 4f;
			bool flag = false;
			bool flag2 = targetNode != null && Vector3.Distance(base.transform.position, targetNode.position + Vector3.Normalize((base.transform.position - targetPlayer.transform.position) * 100f) * 4f) < 0.8f;
			bool flag3 = false;
			float num11;
			for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
			{
				flag3 = false;
				if (StartOfRound.Instance.allPlayerScripts[j].isPlayerDead || !StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled || (bool)StartOfRound.Instance.allPlayerScripts[j].inAnimationWithEnemy || (StartOfRound.Instance.allPlayerScripts[j].isInHangarShipRoom && isInsidePlayerShip))
				{
					continue;
				}
				num11 = Vector3.Distance(StartOfRound.Instance.allPlayerScripts[j].transform.position, base.transform.position);
				float num12 = StartOfRound.Instance.allPlayerScripts[j].LineOfSightToPositionAngle(base.transform.position, 40);
				if (num12 == -361f || num11 > 80f)
				{
					continue;
				}
				if (num12 < 10f || (num12 < 20f && num11 < 20f))
				{
					num9++;
					spottedMeter += Time.deltaTime * 0.6f;
					flag3 = true;
				}
				else if (num12 < 30f && num11 < 35f)
				{
					num9++;
					spottedMeter += Time.deltaTime * 0.33f;
					flag3 = true;
				}
				if (!flag3 || flag || !(targetNode != null))
				{
					continue;
				}
				if (Physics.Linecast(StartOfRound.Instance.allPlayerScripts[j].gameplayCamera.transform.position, targetNode.transform.position, 1024, QueryTriggerInteraction.Ignore))
				{
					if (flag2)
					{
						num10 = 0f;
					}
					else if (num10 < 8f)
					{
						num10 = 10f;
					}
					continue;
				}
				num10 = 8f;
				flag = true;
				if (num11 < 14f && spottedMeter < 0.5f && !backedUpFromWatchingPlayer)
				{
					backedUpFromWatchingPlayer = true;
					SetFearLevelFromBushWolf();
				}
			}
			num11 = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
			if (num9 <= 0)
			{
				spottedMeter = Mathf.Max(spottedMeter - Time.deltaTime, 0f);
				agent.speed = 6f * speedMultiplier;
			}
			else if (spottedMeter > 0.8f)
			{
				agent.speed = 10f * speedMultiplier;
				waitOutsideShipTimer = Mathf.Max(waitOutsideShipTimer - Time.deltaTime * 1.3f, 0f);
			}
			else
			{
				agent.speed = num10;
			}
			if (((StartOfRound.Instance.livingPlayers > 1 && num9 == StartOfRound.Instance.livingPlayers) || (spottedMeter > 0.95f && !flag2) || (spottedMeter <= 0f && Vector3.Distance(base.transform.position, targetPlayer.transform.position) < 11f)) && Time.realtimeSinceStartup - timeAtLastGrowl > 8f)
			{
				DoGrowlLocalClient();
			}
			if (matingCallTimer <= 0f && num11 > 12f && Time.realtimeSinceStartup - timeSinceCall > 7f)
			{
				timeSinceCall = Time.realtimeSinceStartup;
				if (Random.Range(0, 100) < 15)
				{
					DoMatingCall();
				}
			}
			if (spottedMeter > 0.05f && targetPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.5f, 60f, 30))
			{
				nestRange = Mathf.Clamp(nestRange - changeNestRangeSpeed * Mathf.Clamp(spottedMeter * 4f, 0.4f, 4f) * Time.deltaTime, baseNestRange * 0.75f, Vector3.Distance(targetPlayer.transform.position, currentHidingSpot) + 15f);
			}
			else
			{
				nestRange += changeNestRangeSpeed * Time.deltaTime;
			}
			if (targetPlayer.isInHangarShipRoom && spottedMeter < 0.5f && Vector3.Distance(base.transform.position, StartOfRound.Instance.elevatorTransform.position) < 25f)
			{
				waitOutsideShipTimer = Mathf.Min(waitOutsideShipTimer + Time.deltaTime, 20f);
			}
			if (targetPlayer != null && Vector3.Distance(base.transform.position, targetPlayer.transform.position) < attackDistance + 6.5f)
			{
				LookAtPosition(targetPlayer.transform.position);
			}
			break;
		}
		case 2:
		{
			if (previousState != currentBehaviourStateIndex)
			{
				agent.speed = 0f;
				movingTowardsTargetPlayer = false;
				shootTongueTimer = 0f;
				spottedMeter = 0f;
				isHiding = false;
				failedTongueShoot = false;
				timeSinceChangingState = 0f;
				previousState = currentBehaviourStateIndex;
			}
			if (targetPlayer == null)
			{
				break;
			}
			if (timeSinceKillingPlayer < 2f || timeSinceTakingDamage < 0.35f)
			{
				agent.speed = 0f;
				break;
			}
			inKillAnimation = false;
			if (base.IsOwner)
			{
				LookAtPosition(targetPlayer.transform.position);
			}
			if (failedTongueShoot)
			{
				agent.speed = 0f;
				CancelReelingPlayerIn();
				if (base.IsOwner && Time.realtimeSinceStartup - timeAtLastGrowl > 4f && Random.Range(0, 100) < 40)
				{
					DoGrowlLocalClient();
				}
				if (tongueLengthNormalized < -0.25f && base.IsOwner)
				{
					if (timesFailingTongueShoot >= 1)
					{
						SwitchToBehaviourState(0);
						break;
					}
					timesFailingTongueShoot++;
					SwitchToBehaviourState(1);
				}
				break;
			}
			VehicleController vehicleController = Object.FindObjectOfType<VehicleController>();
			if (targetPlayer.isPlayerDead || !targetPlayer.isPlayerControlled || (bool)targetPlayer.inAnimationWithEnemy || stunNormalizedTimer > 0f || (targetPlayer.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed && !isInsidePlayerShip) || (vehicleController != null && targetPlayer.physicsParent != null && targetPlayer.physicsParent == vehicleController.transform && !vehicleController.backDoorOpen))
			{
				agent.speed = 0f;
				CancelReelingPlayerIn();
				if (base.IsOwner && tongueLengthNormalized < -0.25f)
				{
					SwitchToBehaviourState(0);
				}
			}
			else if (dragging)
			{
				if (startedShootingTongue)
				{
					startedShootingTongue = false;
					shootTongueTimer = 0f;
					creatureVoice.clip = snarlSFX;
					creatureVoice.Play();
					draggingPlayer = targetPlayer;
					targetPlayer.disableInteract = true;
					timesFailingTongueShoot = 0;
					if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
					{
						targetPlayer.CancelSpecialTriggerAnimations();
						targetPlayer.DropAllHeldItemsAndSync();
					}
					CheckHomeBase();
				}
				if (GameNetworkManager.Instance.localPlayerController == draggingPlayer)
				{
					draggingPlayer.shockingTarget = base.transform;
				}
				agent.speed = 8f;
				Vector3 position = targetPlayer.transform.position;
				position.y = base.transform.position.y;
				float num = Vector3.Distance(base.transform.position, position);
				if (base.IsOwner)
				{
					if ((num > 3f && shootTongueTimer < 1.3f) || num > attackDistance - 3f || (num > 4.2f && Physics.Linecast(tongueStartPoint.position, tongueTarget.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
					{
						SetMovingTowardsTargetPlayer(draggingPlayer);
					}
					else
					{
						movingTowardsTargetPlayer = false;
						SetDestinationToPosition(currentHidingSpot);
					}
				}
				if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
					creatureAnimator.SetBool("mouthOpen", Vector3.Distance(base.transform.position, currentHidingSpot) < 3f);
					if (num > 0.7f)
					{
						Vector3 vector = base.transform.position + base.transform.forward * 2f + Vector3.up * 1.15f;
						float num2 = 1f;
						if (targetPlayer.activatingItem)
						{
							num2 = 1.35f;
						}
						if (targetPlayer.isInElevator && Vector3.Distance(targetPlayer.transform.position, StartOfRound.Instance.elevatorTransform.position) < 25f)
						{
							num2 = 1.7f;
						}
						Vector3 zero = Vector3.zero;
						timeSinceLOSBlocked = Mathf.Max(timeSinceLOSBlocked - Time.deltaTime, 0f);
						if (timeSinceLOSBlocked > 0f || targetPlayer.isInHangarShipRoom)
						{
							if (timeSinceLOSBlocked <= 0f)
							{
								timeSinceLOSBlocked = 0.5f;
							}
							if (targetPlayer.isInHangarShipRoom && targetPlayer.transform.position.x < -14.3f && targetPlayer.transform.position.x > -13.6f)
							{
								vector = targetPlayer.transform.position;
								vector.z = StartOfRound.Instance.middleOfShipNode.position.z;
							}
							else
							{
								vector = StartOfRound.Instance.middleOfSpaceNode.position;
							}
						}
						else if (randomForceInterval <= 0f)
						{
							Ray ray = new Ray(targetPlayer.transform.position + Vector3.up * 0.4f, vector - targetPlayer.transform.position + Vector3.up * 0.4f);
							if (Physics.Raycast(ray, 0.8f, 268435456, QueryTriggerInteraction.Ignore))
							{
								randomForceInterval = 0.8f;
								targetPlayer.externalForceAutoFade += Vector3.up * 22f;
							}
							else if (Physics.Raycast(ray, 0.8f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
							{
								randomForceInterval = 0.32f;
								targetPlayer.externalForceAutoFade += Random.onUnitSphere * 6f;
							}
							else
							{
								randomForceInterval = 0.45f;
							}
						}
						else
						{
							randomForceInterval -= Time.deltaTime;
						}
						zero += Vector3.Normalize((vector - targetPlayer.transform.position) * 1000f) * dragForce * num2 * Mathf.Clamp(shootTongueTimer / 2.5f, 1.15f, 2.55f);
						if (targetPlayer.isInElevator && !targetPlayer.isInHangarShipRoom && targetPlayer.transform.position.x < -6f && targetPlayer.transform.position.x > -9.1f && targetPlayer.transform.position.z < -10.8f && targetPlayer.transform.position.z > -17.3f)
						{
							zero.x = Mathf.Min(zero.x, 0f);
							if (base.transform.position.z > targetPlayer.transform.position.z)
							{
								zero += Vector3.forward * 10f;
							}
							else
							{
								zero += Vector3.forward * -10f;
							}
						}
						zero.y = Mathf.Max(zero.y, 0f);
						if (num > attackDistance + 6f && shootTongueTimer > 1.6f)
						{
							SwitchToBehaviourState(1);
							break;
						}
						targetPlayer.externalForces += zero;
						Debug.DrawRay(targetPlayer.transform.position, targetPlayer.externalForces, Color.red);
						Debug.DrawRay(targetPlayer.transform.position, targetPlayer.externalForceAutoFade, Color.blue);
					}
				}
				creatureAnimator.SetBool("ReelingPlayerIn", value: true);
				shootTongueTimer += Time.deltaTime;
				if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
				{
					tongueTarget = targetPlayer.upperSpineLocalPoint;
				}
				else
				{
					tongueTarget = targetPlayer.upperSpine;
				}
			}
			else if (!startedShootingTongue)
			{
				startedShootingTongue = true;
				creatureAnimator.SetBool("ShootTongue", value: true);
				creatureVoice.PlayOneShot(shootTongueSFX);
				tongueLengthNormalized = 0f;
			}
			else
			{
				if (timeSinceChangingState < 0.28f)
				{
					break;
				}
				if (!playedTongueAudio)
				{
					playedTongueAudio = true;
					tongueAudio.PlayOneShot(tongueShootSFX);
					tongueAudio.Play();
				}
				shootTongueTimer += Time.deltaTime;
				if (GameNetworkManager.Instance.localPlayerController == targetPlayer)
				{
					tongueTarget = targetPlayer.upperSpineLocalPoint;
				}
				else
				{
					tongueTarget = targetPlayer.upperSpine;
				}
				if (tongueLengthNormalized < 1f)
				{
					tongueLengthNormalized = Mathf.Min(tongueLengthNormalized + Time.deltaTime * 3f, 1f);
				}
				else if (targetPlayer == GameNetworkManager.Instance.localPlayerController)
				{
					if (!Physics.Linecast(tongueStartPoint.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position - Vector3.up * 0.3f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < attackDistance)
					{
						HitByEnemyServerRpc();
					}
					else
					{
						DodgedEnemyHitServerRpc();
					}
				}
				else if (shootTongueTimer > 3f)
				{
					TongueShootWasUnsuccessful();
				}
			}
			break;
		}
		}
	}

	private void TongueShootWasUnsuccessful()
	{
		shootTongueTimer = 0f;
		failedTongueShoot = true;
	}

	[ServerRpc(RequireOwnership = false)]
	public void HitByEnemyServerRpc()
{if(!failedTongueShoot)			{
				HitByEnemyClientRpc();
			}
}
	[ClientRpc]
	public void HitByEnemyClientRpc()
			{
				dragging = true;
			}

	[ServerRpc(RequireOwnership = false)]
	public void DodgedEnemyHitServerRpc()
			{
				DodgedEnemyHitClientRpc();
			}

	[ClientRpc]
	public void DodgedEnemyHitClientRpc()
			{
				TongueShootWasUnsuccessful();
			}

	private void LookAtPosition(Vector3 pos)
	{
		looking = true;
		agent.updateRotation = false;
		pos.y = base.transform.position.y;
		base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.LookRotation(pos - base.transform.position, Vector3.up), 4f * Time.deltaTime);
	}

	public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null)
	{
		base.OnCollideWithEnemy(other, collidedEnemy);
		if (!(collidedEnemy.enemyType == enemyType) && !(timeSinceHitting < 0.75f) && !dragging && !startedShootingTongue && !(stunNormalizedTimer > 0f) && !isEnemyDead)
		{
			timeSinceHitting = 0f;
			creatureAnimator.ResetTrigger("Hit");
			creatureAnimator.SetTrigger("Hit");
			creatureSFX.PlayOneShot(enemyType.audioClips[5]);
			WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[0]);
			RoundManager.Instance.PlayAudibleNoise(creatureSFX.transform.position, 8f, 0.6f);
			collidedEnemy.HitEnemy(1, null, playHitSFX: true);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (foundSpawningPoint && !inKillAnimation && !isEnemyDead && MeetsStandardPlayerCollisionConditions(other, inKillAnimation) != null)
		{
			float num = Vector3.Distance(base.transform.position, currentHidingSpot);
			bool flag = false;
			if (timeSinceTakingDamage < 2.5f && lastHitByPlayer != null && num < 16f)
			{
				flag = true;
			}
			else if (num < 7f && dragging && !startedShootingTongue && targetPlayer == GameNetworkManager.Instance.localPlayerController)
			{
				flag = true;
			}
			if (flag)
			{
				GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.up * 15f, spawnBody: true, CauseOfDeath.Mauling, 8);
				DoKillPlayerAnimationServerRpc((int)targetPlayer.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void DoKillPlayerAnimationServerRpc(int playerId)
			{
				DoKillPlayerAnimationClientRpc(playerId);
			}

	[ClientRpc]
	public void DoKillPlayerAnimationClientRpc(int playerId)
{		{
			creatureAnimator.SetTrigger("killPlayer");
			if (StartOfRound.Instance.allPlayerScripts[playerId] == GameNetworkManager.Instance.localPlayerController)
			{
				GameNetworkManager.Instance.localPlayerController.overrideGameOverSpectatePivot = playerAnimationHeadPoint;
			}
			timeSinceKillingPlayer = 0f;
			if (killPlayerCoroutine != null)
			{
				CancelKillAnimation();
			}
			inKillAnimation = true;
			growlAudio.PlayOneShot(killSFX);
			WalkieTalkie.TransmitOneShotAudio(growlAudio, killSFX);
			killPlayerCoroutine = StartCoroutine(KillAnimationOnPlayer(StartOfRound.Instance.allPlayerScripts[playerId]));
		}
}
	private void CancelKillAnimation()
	{
		if (inKillAnimation)
		{
			creatureAnimator.SetTrigger("cancelKillAnim");
			inKillAnimation = false;
		}
		DropBody();
		if (killPlayerCoroutine != null)
		{
			StopCoroutine(killPlayerCoroutine);
		}
	}

	private void DropBody()
	{
		if (body != null)
		{
			body.matchPositionExactly = false;
			body.speedMultiplier = 12f;
			body.attachedTo = null;
			body.attachedLimb = null;
			body = null;
		}
	}

	private IEnumerator KillAnimationOnPlayer(PlayerControllerB player)
	{
		float time = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => Time.realtimeSinceStartup - time > 1f || player.deadBody != null);
		if (player.deadBody != null)
		{
			body = player.deadBody;
			body.matchPositionExactly = true;
			body.attachedTo = playerBodyHeadPoint;
			body.attachedLimb = player.deadBody.bodyParts[0];
		}
		yield return new WaitForSeconds(0.25f);
		DropBody();
	}

	private void LateUpdate()
	{
		AddProceduralOffsetToLimbsOverTerrain();
		if (tongueTarget != null)
		{
			if (!tongue.gameObject.activeSelf)
			{
				tongue.gameObject.SetActive(value: true);
			}
			tongueLengthNormalized = Mathf.Min(tongueLengthNormalized + Time.deltaTime * 3f, 1f);
			if (tongueLengthNormalized < 1f || dragging)
			{
				Vector3 direction = tongueTarget.transform.position - tongueStartPoint.position;
				if (!dragging)
				{
					Ray ray = new Ray(tongueStartPoint.position, direction);
					direction = ((!Physics.Raycast(ray, out hit, attackDistance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(attackDistance) : hit.point);
				}
				else
				{
					direction = tongueTarget.transform.position;
				}
				tongue.position = tongueStartPoint.position;
				tongue.LookAt(direction, Vector3.up);
				tongue.localScale = Vector3.Lerp(tongue.localScale, new Vector3(1f, 1f, Vector3.Distance(tongueStartPoint.position, tongueTarget.position)), tongueLengthNormalized);
			}
		}
		else
		{
			tongueLengthNormalized = Mathf.Max(tongueLengthNormalized - Time.deltaTime * 3f, -1f);
			if (tongueLengthNormalized > 0f)
			{
				tongue.position = tongueStartPoint.position;
				tongue.localScale = Vector3.Lerp(tongue.localScale, new Vector3(1f, 1f, 0.5f), tongueLengthNormalized);
			}
			else if (tongue.gameObject.activeSelf)
			{
				tongue.gameObject.SetActive(value: false);
			}
		}
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		hideBodyOnTerrain = agentLocalVelocity.magnitude == 0f || isHiding;
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("WalkX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("WalkZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		creatureAnimator.SetBool("idling", agentLocalVelocity.x == 0f && agentLocalVelocity.z == 0f);
		previousPosition = base.transform.position;
	}

	private void AddProceduralOffsetToLimbsOverTerrain()
	{
		bool flag = stunNormalizedTimer < 0f && !dragging && !inKillAnimation && !isEnemyDead && timeSinceTakingDamage > 0.4f && matingCallTimer <= 0f;
		if (flag && Physics.Raycast(base.transform.position + Vector3.up * 1.25f, base.transform.forward, out hit, 3.15f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			bendHeadBack.weight = Mathf.Lerp(1f, 0f, hit.distance / 3.15f);
		}
		else
		{
			bendHeadBack.weight = Mathf.Lerp(bendHeadBack.weight, 0f, Time.deltaTime * 5f);
		}
		if (resetIKOffsetsInterval < 0f || !hideBodyOnTerrain || !flag)
		{
			resetIKOffsetsInterval = 8f;
			for (int i = 0; i < proceduralBodyTargets.Length; i++)
			{
				proceduralBodyTargets[i].localPosition = Vector3.zero;
			}
			animationContainer.rotation = Quaternion.Lerp(animationContainer.rotation, base.transform.rotation, 10f * Time.deltaTime);
			return;
		}
		resetIKOffsetsInterval -= Time.deltaTime;
		if (Physics.Raycast(base.transform.position + Vector3.up, Vector3.down, out hit, 2f, 1073744129, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) < 75f)
		{
			Quaternion b = Quaternion.FromToRotation(animationContainer.up, hit.normal) * base.transform.rotation;
			animationContainer.rotation = Quaternion.Lerp(animationContainer.rotation, b, Time.deltaTime * 10f);
		}
		float num = 0f;
		for (int j = 0; j < proceduralBodyTargets.Length; j++)
		{
			if (j == 4 && currentBehaviourStateIndex == 2)
			{
				proceduralBodyTargets[j].localPosition = Vector3.zero;
				break;
			}
			if (Physics.Raycast(proceduralBodyTargets[j].position + base.transform.up * 1.5f, -base.transform.up, out hit, 5f, 1073744129, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(proceduralBodyTargets[j].position + base.transform.up * 1.5f, -base.transform.up * 5f, Color.white);
				num = Mathf.Clamp(hit.point.y, base.transform.position.y - 1.25f, base.transform.position.y + 1.25f);
			}
			else
			{
				num = base.transform.position.y;
			}
			if (j == 4)
			{
				proceduralBodyTargets[j].position = new Vector3(IKTargetContainers[j].position.x, Mathf.Lerp(proceduralBodyTargets[j].position.y, num + 0.4f, 40f * Time.deltaTime), IKTargetContainers[j].position.z);
			}
			else
			{
				proceduralBodyTargets[j].position = new Vector3(IKTargetContainers[j].position.x, Mathf.Lerp(proceduralBodyTargets[j].position.y, num, 40f * Time.deltaTime), IKTargetContainers[j].position.z);
			}
		}
	}
}
