using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class FlowermanAI : EnemyAI
{
	private bool evadeModeStareDown;

	private bool stopTurningTowardsPlayers;

	public float evadeStealthTimer;

	private int stareDownChanceIncrease;

	public PlayerControllerB lookAtPlayer;

	private Transform localPlayerCamera;

	private RaycastHit rayHit;

	private Ray playerRay;

	public Transform turnCompass;

	private int roomAndEnemiesMask = 8915200;

	private Vector3 agentLocalVelocity;

	public Collider thisEnemyCollider;

	private Vector3 previousPosition;

	private float velX;

	private float velZ;

	[Header("Kill animation")]
	public bool inKillAnimation;

	private Coroutine killAnimationCoroutine;

	public bool carryingPlayerBody;

	public DeadBodyInfo bodyBeingCarried;

	public Transform rightHandGrip;

	public Transform animationContainer;

	private bool wasInEvadeMode;

	public List<Transform> ignoredNodes = new List<Transform>();

	private Vector3 mainEntrancePosition;

	[Header("Anger phase")]
	public float angerMeter;

	public float angerCheckInterval;

	public bool isInAngerMode;

	public AudioSource creatureAngerVoice;

	public AudioSource crackNeckAudio;

	public AudioClip crackNeckSFX;

	public int timesThreatened;

	private Vector3 waitAroundEntrancePosition;

	private int timesFoundSneaking;

	private bool stunnedByPlayerLastFrame;

	private bool startingKillAnimationLocalClient;

	private float getPathToFavoriteNodeInterval;

	private bool gettingFarthestNodeFromPlayerAsync;

	private Transform farthestNodeFromTargetPlayer;

	[Space(5f)]
	public int maxAsync = 50;

	public override void Start()
	{
		base.Start();
		movingTowardsTargetPlayer = true;
		localPlayerCamera = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform;
		mainEntrancePosition = RoundManager.FindMainEntrancePosition();
	}

	public override void DoAIInterval()
	{
		if (StartOfRound.Instance.livingPlayers == 0)
		{
			base.DoAIInterval();
			return;
		}
		if (TargetClosestPlayer())
		{
			if (currentBehaviourStateIndex == 2)
			{
				SetMovingTowardsTargetPlayer(targetPlayer);
				if (!inKillAnimation && targetPlayer != GameNetworkManager.Instance.localPlayerController)
				{
					ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
				}
				base.DoAIInterval();
				return;
			}
			if (currentBehaviourStateIndex == 1)
			{
				if (favoriteSpot != null && carryingPlayerBody)
				{
					if (mostOptimalDistance < 5f || PathIsIntersectedByLineOfSight(favoriteSpot.position))
					{
						AvoidClosestPlayer();
					}
					else
					{
						targetNode = favoriteSpot;
						if (Time.realtimeSinceStartup - getPathToFavoriteNodeInterval > 1f)
						{
							SetDestinationToPosition(favoriteSpot.position, checkForPath: true);
							getPathToFavoriteNodeInterval = Time.realtimeSinceStartup;
						}
					}
				}
				else
				{
					AvoidClosestPlayer();
				}
			}
			else
			{
				ChooseClosestNodeToPlayer();
			}
		}
		else
		{
			if (currentBehaviourStateIndex == 2)
			{
				SetDestinationToPosition(waitAroundEntrancePosition);
				base.DoAIInterval();
				return;
			}
			Transform transform = ChooseFarthestNodeFromPosition(mainEntrancePosition);
			if (favoriteSpot == null)
			{
				favoriteSpot = transform;
			}
			targetNode = transform;
			SetDestinationToPosition(transform.position, checkForPath: true);
		}
		base.DoAIInterval();
	}

	public void AvoidClosestPlayer()
	{
		if (farthestNodeFromTargetPlayer == null)
		{
			gettingFarthestNodeFromPlayerAsync = true;
			return;
		}
		Transform transform = farthestNodeFromTargetPlayer;
		farthestNodeFromTargetPlayer = null;
		if (transform != null && mostOptimalDistance > 5f && Physics.Linecast(transform.transform.position, targetPlayer.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			targetNode = transform;
			SetDestinationToPosition(targetNode.position);
			return;
		}
		if (carryingPlayerBody)
		{
			DropPlayerBody();
			DropPlayerBodyServerRpc();
		}
		AddToAngerMeter(AIIntervalTime);
		agent.speed = 0f;
	}

	public void AddToAngerMeter(float amountToAdd)
	{
		if (stunNormalizedTimer > 0f)
		{
			if (stunnedByPlayer != null)
			{
				stunnedByPlayerLastFrame = true;
				angerMeter = 12f;
			}
			else
			{
				angerMeter = 2f;
			}
			return;
		}
		angerMeter += amountToAdd;
		if (angerMeter <= 0.4f)
		{
			return;
		}
		angerCheckInterval += amountToAdd;
		if (!(angerCheckInterval > 1f))
		{
			return;
		}
		angerCheckInterval = 0f;
		float num = Mathf.Clamp(0.09f * angerMeter, 0f, 0.99f);
		if (Random.Range(0f, 1f) < num)
		{
			if (angerMeter < 2.5f)
			{
				timesThreatened++;
			}
			angerMeter += (float)timesThreatened / 1.75f;
			SwitchToBehaviourStateOnLocalClient(2);
			EnterAngerModeServerRpc(angerMeter);
		}
	}

	[ServerRpc]
	public void EnterAngerModeServerRpc(float angerTime)
{		{
			EnterAngerModeClientRpc(angerTime);
		}
}
	[ClientRpc]
	public void EnterAngerModeClientRpc(float angerTime)
			{
				angerMeter = angerTime;
				agent.speed = 9f;
				SwitchToBehaviourStateOnLocalClient(2);
				waitAroundEntrancePosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(mainEntrancePosition, 6f);
			}

	public void ChooseClosestNodeToPlayer()
	{
		if (targetNode == null)
		{
			targetNode = allAINodes[0].transform;
		}
		Transform transform = ChooseClosestNodeToPosition(targetPlayer.transform.position, avoidLineOfSight: true);
		if (transform != null)
		{
			targetNode = transform;
		}
		float num = Vector3.Distance(targetPlayer.transform.position, base.transform.position);
		if (num - mostOptimalDistance < 0.1f && (!PathIsIntersectedByLineOfSight(targetPlayer.transform.position, calculatePathDistance: true) || num < 3f))
		{
			if (pathDistance > 10f && !ignoredNodes.Contains(targetNode) && ignoredNodes.Count < 4)
			{
				ignoredNodes.Add(targetNode);
			}
			movingTowardsTargetPlayer = true;
		}
		else
		{
			SetDestinationToPosition(targetNode.position);
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead || inKillAnimation || GameNetworkManager.Instance == null)
		{
			return;
		}
		if (base.IsOwner && gettingFarthestNodeFromPlayerAsync && targetPlayer != null)
		{
			float num = Vector3.Distance(base.transform.position, targetPlayer.transform.position);
			if (num < 16f)
			{
				maxAsync = 100;
			}
			else if (num < 40f)
			{
				maxAsync = 25;
			}
			else
			{
				maxAsync = 4;
			}
			Transform transform = ChooseFarthestNodeFromPosition(targetPlayer.transform.position, avoidLineOfSight: true, 0, doAsync: true, maxAsync, capDistance: true);
			if (!gotFarthestNodeAsync)
			{
				return;
			}
			farthestNodeFromTargetPlayer = transform;
			gettingFarthestNodeFromPlayerAsync = false;
		}
		if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.5f, 30f))
		{
			if (currentBehaviourStateIndex == 0)
			{
				SwitchToBehaviourState(1);
				if (!thisNetworkObject.IsOwner)
				{
					ChangeOwnershipOfEnemy(GameNetworkManager.Instance.localPlayerController.actualClientId);
				}
				if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 5f)
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.6f);
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.3f);
				}
				agent.speed = 0f;
				evadeStealthTimer = 0f;
			}
			else if (evadeStealthTimer > 0.5f)
			{
				int playerObj = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
				LookAtFlowermanTrigger(playerObj);
				ResetFlowermanStealthTimerServerRpc(playerObj);
			}
		}
		switch (currentBehaviourStateIndex)
		{
		case 1:
			if (isInAngerMode)
			{
				isInAngerMode = false;
				creatureAnimator.SetBool("anger", value: false);
			}
			if (!wasInEvadeMode)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 7f, 0.8f);
				wasInEvadeMode = true;
				movingTowardsTargetPlayer = false;
				if (favoriteSpot != null && !carryingPlayerBody && Vector3.Distance(base.transform.position, favoriteSpot.position) < 7f)
				{
					favoriteSpot = null;
				}
			}
			if (stunNormalizedTimer > 0f)
			{
				creatureAnimator.SetLayerWeight(2, 1f);
			}
			else
			{
				creatureAnimator.SetLayerWeight(2, 0f);
			}
			evadeStealthTimer += Time.deltaTime;
			if (thisNetworkObject.IsOwner)
			{
				float num2 = ((timesFoundSneaking % 3 != 0) ? 11f : 24f);
				if (favoriteSpot != null && carryingPlayerBody)
				{
					num2 = ((!(Vector3.Distance(base.transform.position, favoriteSpot.position) > 8f)) ? 3f : 24f);
				}
				if (evadeStealthTimer > num2)
				{
					evadeStealthTimer = 0f;
					SwitchToBehaviourState(0);
				}
				if (!carryingPlayerBody && evadeModeStareDown && evadeStealthTimer < 1.25f)
				{
					AddToAngerMeter(Time.deltaTime * 1.5f);
					agent.speed = 0f;
				}
				else
				{
					evadeModeStareDown = false;
					if (stunNormalizedTimer > 0f)
					{
						DropPlayerBody();
						AddToAngerMeter(0f);
						agent.speed = 0f;
					}
					else
					{
						if (stunnedByPlayerLastFrame)
						{
							stunnedByPlayerLastFrame = false;
							AddToAngerMeter(0f);
						}
						if (carryingPlayerBody)
						{
							agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 7.25f, 4f, 9f);
						}
						else
						{
							agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 4.25f, 0f, 6f);
						}
					}
				}
				if (!carryingPlayerBody && ventAnimationFinished)
				{
					LookAtPlayerOfInterest();
				}
			}
			if (!carryingPlayerBody)
			{
				CalculateAnimationDirection();
				break;
			}
			creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f).sqrMagnitude / (Time.deltaTime * 2f));
			previousPosition = base.transform.position;
			break;
		case 0:
			if (isInAngerMode)
			{
				isInAngerMode = false;
				creatureAnimator.SetBool("anger", value: false);
			}
			if (wasInEvadeMode)
			{
				wasInEvadeMode = false;
				evadeStealthTimer = 0f;
				if (carryingPlayerBody)
				{
					DropPlayerBody();
					agent.enabled = true;
					favoriteSpot = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: true);
					if (!base.IsOwner)
					{
						agent.enabled = false;
					}
					Debug.Log("Flowerman: Dropped player body");
				}
			}
			creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f).sqrMagnitude / (Time.deltaTime / 4f));
			previousPosition = base.transform.position;
			agent.speed = 6f;
			break;
		case 2:
		{
			bool flag = false;
			if (!isInAngerMode)
			{
				isInAngerMode = true;
				DropPlayerBody();
				creatureAngerVoice.Play();
				creatureAngerVoice.pitch = Random.Range(0.9f, 1.3f);
				creatureAnimator.SetBool("anger", value: true);
				creatureAnimator.SetBool("sneak", value: false);
				if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 60f, 15, 2.5f))
				{
					flag = true;
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);
				}
			}
			if (!flag && GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 60f, 13, 4f))
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.8f);
			}
			CalculateAnimationDirection(3f);
			if (stunNormalizedTimer > 0f)
			{
				creatureAnimator.SetLayerWeight(2, 1f);
				agent.speed = 0f;
				angerMeter = 6f;
			}
			else
			{
				creatureAnimator.SetLayerWeight(2, 0f);
				agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 1.2f, 3f, 12f);
			}
			angerMeter -= Time.deltaTime;
			if (base.IsOwner && angerMeter <= 0f)
			{
				SwitchToBehaviourState(1);
			}
			break;
		}
		}
		if (isInAngerMode)
		{
			creatureAngerVoice.volume = Mathf.Lerp(creatureAngerVoice.volume, 1f, 10f * Time.deltaTime);
		}
		else
		{
			creatureAngerVoice.volume = Mathf.Lerp(creatureAngerVoice.volume, 0f, 2f * Time.deltaTime);
		}
		Vector3 localEulerAngles = animationContainer.localEulerAngles;
		if (carryingPlayerBody)
		{
			agent.angularSpeed = 50f;
			localEulerAngles.z = Mathf.Lerp(localEulerAngles.z, 179f, 10f * Time.deltaTime);
			creatureAnimator.SetLayerWeight(1, Mathf.Lerp(creatureAnimator.GetLayerWeight(1), 1f, 10f * Time.deltaTime));
		}
		else
		{
			agent.angularSpeed = 220f;
			localEulerAngles.z = Mathf.Lerp(localEulerAngles.z, 0f, 10f * Time.deltaTime);
			creatureAnimator.SetLayerWeight(1, Mathf.Lerp(creatureAnimator.GetLayerWeight(1), 0f, 10f * Time.deltaTime));
		}
		animationContainer.localEulerAngles = localEulerAngles;
	}

	[ServerRpc]
	public void DropPlayerBodyServerRpc()
{		{
			DropPlayerBodyClientRpc();
		}
}
	[ClientRpc]
	public void DropPlayerBodyClientRpc()
			{
				DropPlayerBody();
			}

	private void DropPlayerBody()
	{
		if (carryingPlayerBody)
		{
			carryingPlayerBody = false;
			bodyBeingCarried.matchPositionExactly = false;
			bodyBeingCarried.attachedTo = null;
			bodyBeingCarried = null;
			creatureAnimator.SetBool("carryingBody", value: false);
		}
	}

	private void LookAtPlayerOfInterest()
	{
		if (isInAngerMode)
		{
			lookAtPlayer = targetPlayer;
		}
		else
		{
			lookAtPlayer = GetClosestPlayer();
		}
		if (lookAtPlayer != null)
		{
			turnCompass.LookAt(lookAtPlayer.gameplayCamera.transform.position);
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(new Vector3(0f, turnCompass.eulerAngles.y, 0f)), 30f * Time.deltaTime);
		}
	}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f) / (Time.deltaTime * 2f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, 0f - agentLocalVelocity.y, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = base.transform.position;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation || startingKillAnimationLocalClient || carryingPlayerBody);
		if (playerControllerB != null)
		{
			KillPlayerAnimationServerRpc((int)playerControllerB.playerClientId);
			startingKillAnimationLocalClient = true;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerAnimationServerRpc(int playerObjectId)
{		{
			if (!inKillAnimation && !carryingPlayerBody)
			{
				inKillAnimation = true;
				inSpecialAnimation = true;
				isClientCalculatingAI = false;
				inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerObjectId];
				inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
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
			inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerObjectId];
			if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController)
			{
				startingKillAnimationLocalClient = false;
			}
			if (inSpecialAnimationWithPlayer == null || inSpecialAnimationWithPlayer.isPlayerDead || !inSpecialAnimationWithPlayer.isInsideFactory)
			{
				FinishKillAnimation(carryingBody: false);
			}
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
			inKillAnimation = true;
			inSpecialAnimation = true;
			creatureAnimator.SetBool("killing", value: true);
			agent.enabled = false;
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
			inSpecialAnimationWithPlayer.snapToServerPosition = true;
			Vector3 vector = ((!inSpecialAnimationWithPlayer.IsOwner) ? inSpecialAnimationWithPlayer.transform.parent.TransformPoint(inSpecialAnimationWithPlayer.serverPlayerPosition) : inSpecialAnimationWithPlayer.transform.position);
			Vector3 position = base.transform.position;
			position.y = inSpecialAnimationWithPlayer.transform.position.y;
			playerRay = new Ray(vector, position - inSpecialAnimationWithPlayer.transform.position);
			turnCompass.LookAt(vector);
			position = base.transform.eulerAngles;
			position.y = turnCompass.eulerAngles.y;
			base.transform.eulerAngles = position;
			if (killAnimationCoroutine != null)
			{
				StopCoroutine(killAnimationCoroutine);
			}
			killAnimationCoroutine = StartCoroutine(killAnimation());
		}
}
	private IEnumerator killAnimation()
	{
		WalkieTalkie.TransmitOneShotAudio(crackNeckAudio, crackNeckSFX);
		crackNeckAudio.PlayOneShot(crackNeckSFX);
		Vector3 endPosition = playerRay.GetPoint(1f);
		if (endPosition.y < -80f)
		{
			Vector3 startingPosition = base.transform.position;
			for (int i = 0; i < 5; i++)
			{
				base.transform.position = Vector3.Lerp(startingPosition, endPosition, (float)i / 5f);
				yield return null;
			}
			base.transform.position = endPosition;
		}
		creatureAnimator.SetBool("killing", value: false);
		creatureAnimator.SetBool("carryingBody", value: true);
		yield return new WaitForSeconds(0.65f);
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Strangulation);
			inSpecialAnimationWithPlayer.snapToServerPosition = false;
			float startTime = Time.timeSinceLevelLoad;
			yield return new WaitUntil(() => inSpecialAnimationWithPlayer.deadBody != null || Time.timeSinceLevelLoad - startTime > 2f);
		}
		if (inSpecialAnimationWithPlayer == null || inSpecialAnimationWithPlayer.deadBody == null)
		{
			Debug.Log("Flowerman: Player body was not spawned or found within 2 seconds.");
			FinishKillAnimation(carryingBody: false);
		}
		else
		{
			inSpecialAnimationWithPlayer.deadBody.bodyBleedingHeavily = true;
			FinishKillAnimation();
		}
	}

	public void FinishKillAnimation(bool carryingBody = true)
	{
		if (killAnimationCoroutine != null)
		{
			StopCoroutine(killAnimationCoroutine);
		}
		inSpecialAnimation = false;
		inKillAnimation = false;
		startingKillAnimationLocalClient = false;
		creatureAnimator.SetBool("killing", value: false);
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
			inSpecialAnimationWithPlayer.snapToServerPosition = false;
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = null;
			if (carryingBody)
			{
				bodyBeingCarried = inSpecialAnimationWithPlayer.deadBody;
				bodyBeingCarried.attachedTo = rightHandGrip;
				bodyBeingCarried.attachedLimb = inSpecialAnimationWithPlayer.deadBody.bodyParts[0];
				bodyBeingCarried.matchPositionExactly = true;
				carryingPlayerBody = true;
			}
		}
		evadeStealthTimer = 0f;
		movingTowardsTargetPlayer = false;
		ignoredNodes.Clear();
		if (!carryingBody)
		{
			creatureAnimator.SetBool("carryingBody", value: false);
		}
		if (base.IsOwner)
		{
			Vector3 position = base.transform.position;
			position = RoundManager.Instance.GetNavMeshPosition(position, default(NavMeshHit), 10f);
			if (!RoundManager.Instance.GotNavMeshPositionResult)
			{
				position = ((!Physics.Raycast(base.transform.position, -Vector3.up, out var hitInfo, 50f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? allAINodes[Random.Range(0, allAINodes.Length)].transform.position : RoundManager.Instance.GetNavMeshPosition(hitInfo.point, default(NavMeshHit), 10f));
			}
			base.transform.position = position;
			agent.enabled = true;
			isClientCalculatingAI = true;
		}
		SwitchToBehaviourStateOnLocalClient(1);
		if (base.IsServer)
		{
			SwitchToBehaviourState(1);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ResetFlowermanStealthTimerServerRpc(int playerObj)
			{
				ResetFlowermanStealthClientRpc(playerObj);
			}

	[ClientRpc]
	public void ResetFlowermanStealthClientRpc(int playerObj)
{if(playerObj != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)			{
				LookAtFlowermanTrigger(playerObj);
			}
}
	public void LookAtFlowermanTrigger(int playerObj)
	{
		if (!base.IsOwner)
		{
			return;
		}
		if (!evadeModeStareDown)
		{
			if (Random.Range(0, 70) < stareDownChanceIncrease)
			{
				stareDownChanceIncrease = -6;
				evadeModeStareDown = true;
			}
			else
			{
				stareDownChanceIncrease++;
			}
			evadeStealthTimer = 0f;
		}
		if (carryingPlayerBody && favoriteSpot != null && Vector3.Distance(base.transform.position, favoriteSpot.transform.position) < 5f)
		{
			DropPlayerBody();
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		if (creatureVoice != null)
		{
			creatureVoice.Stop();
		}
		creatureSFX.Stop();
		creatureAngerVoice.Stop();
		creatureAnimator.SetLayerWeight(2, 0f);
		base.KillEnemy();
		if (carryingPlayerBody)
		{
			carryingPlayerBody = false;
			if (bodyBeingCarried != null)
			{
				bodyBeingCarried.matchPositionExactly = false;
				bodyBeingCarried.attachedTo = null;
			}
		}
		if (inKillAnimation)
		{
			FinishKillAnimation(carryingBody: false);
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (isEnemyDead)
		{
			return;
		}
		enemyHP -= force;
		if (base.IsOwner)
		{
			if (enemyHP <= 0)
			{
				KillEnemyOnOwnerClient();
				return;
			}
			angerMeter = 11f;
			angerCheckInterval = 1f;
			AddToAngerMeter(0.1f);
		}
	}
}
