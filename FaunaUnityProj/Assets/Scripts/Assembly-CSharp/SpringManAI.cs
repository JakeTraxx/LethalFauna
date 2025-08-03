using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SpringManAI : EnemyAI
{
	public AISearchRoutine searchForPlayers;

	private float checkLineOfSightInterval;

	private bool hasEnteredChaseMode;

	private bool stoppingMovement;

	private bool hasStopped;

	public AnimationStopPoints animStopPoints;

	private float currentChaseSpeed = 14.5f;

	private float currentAnimSpeed = 1f;

	private PlayerControllerB previousTarget;

	private bool wasOwnerLastFrame;

	private float stopAndGoMinimumInterval;

	private float hitPlayerTimer;

	public AudioClip[] springNoises;

	public AudioClip enterCooldownSFX;

	public Collider mainCollider;

	private float loseAggroTimer;

	private float timeSinceHittingPlayer;

	private bool movingOnOffMeshLink;

	private Coroutine offMeshLinkCoroutine;

	private float stopMovementTimer;

	public float timeSpentMoving;

	public float onCooldownPhase;

	private bool setOnCooldown;

	public float timeAtLastCooldown;

	private bool inCooldownAnimation;

	private Vector3 previousPosition;

	private float checkPositionInterval;

	private bool isMakingDistance;

	[ServerRpc(RequireOwnership = false)]
	public void SetCoilheadOnCooldownServerRpc(bool setTrue)
			{
				SetCoilheadOnCooldownClientRpc(setTrue);
			}

	[ClientRpc]
	public void SetCoilheadOnCooldownClientRpc(bool setTrue)
{		{
			timeSpentMoving = 0f;
			if (setTrue)
			{
				onCooldownPhase = 20f;
				setOnCooldown = true;
				inCooldownAnimation = true;
				SwitchToBehaviourStateOnLocalClient(0);
				creatureVoice.PlayOneShot(enterCooldownSFX);
			}
			else
			{
				onCooldownPhase = 0f;
				setOnCooldown = false;
				timeAtLastCooldown = Time.realtimeSinceStartup;
			}
		}
}
	public bool PlayerHasHorizontalLOS(PlayerControllerB player)
	{
		Vector3 to = base.transform.position - player.transform.position;
		to.y = 0f;
		return Vector3.Angle(player.transform.forward, to) < 68f;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.allPlayersDead || isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (!base.IsServer)
			{
				ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				break;
			}
			if (onCooldownPhase > 0f)
			{
				agent.speed = 0f;
				SetDestinationToPosition(base.transform.position);
				onCooldownPhase -= AIIntervalTime;
				break;
			}
			if (setOnCooldown)
			{
				setOnCooldown = false;
				SetCoilheadOnCooldownClientRpc(setTrue: false);
			}
			loseAggroTimer = 0f;
			for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
			{
				if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[j]))
				{
					if ((StartOfRound.Instance.allPlayerScripts[j].HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.25f, 68f) || StartOfRound.Instance.allPlayerScripts[j].HasLineOfSightToPosition(base.transform.position + Vector3.up * 1.6f, 68f)) && PlayerHasHorizontalLOS(StartOfRound.Instance.allPlayerScripts[j]))
					{
						targetPlayer = StartOfRound.Instance.allPlayerScripts[j];
						SwitchToBehaviourState(1);
					}
					if (!PathIsIntersectedByLineOfSight(StartOfRound.Instance.allPlayerScripts[j].transform.position, calculatePathDistance: false, avoidLineOfSight: false) && !Physics.Linecast(base.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.allPlayerScripts[j].gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[j].transform.position) < 30f)
					{
						SwitchToBehaviourState(1);
						return;
					}
				}
			}
			agent.speed = 6f;
			if (!searchForPlayers.inProgress)
			{
				movingTowardsTargetPlayer = false;
				SetDestinationToPosition(base.transform.position);
				StartSearch(base.transform.position, searchForPlayers);
			}
			break;
		}
		case 1:
		{
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
			}
			if (TargetClosestPlayer())
			{
				if (previousTarget != targetPlayer)
				{
					previousTarget = targetPlayer;
					ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
				}
				if (!(Time.realtimeSinceStartup - timeSinceHittingPlayer > 7f) || stoppingMovement)
				{
					break;
				}
				if (Vector3.Distance(targetPlayer.transform.position, base.transform.position) > 40f && !CheckLineOfSightForPosition(targetPlayer.gameplayCamera.transform.position, 180f, 140))
				{
					loseAggroTimer += AIIntervalTime;
					if (loseAggroTimer > 4.5f)
					{
						SwitchToBehaviourState(0);
						ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
					}
				}
				else
				{
					loseAggroTimer = 0f;
				}
				break;
			}
			bool flag = false;
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				if ((StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.6f, 68f) || StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 3f, 68f)) && PlayerHasHorizontalLOS(StartOfRound.Instance.allPlayerScripts[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				loseAggroTimer += AIIntervalTime;
				if (loseAggroTimer > 1f)
				{
					SwitchToBehaviourState(0);
					ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
				}
			}
			break;
		}
		}
	}

	private IEnumerator Parabola(NavMeshAgent agent, float height, float duration)
	{
		OffMeshLinkData data = agent.currentOffMeshLinkData;
		Vector3 startPos = agent.transform.position;
		Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
		float normalizedTime = 0f;
		Debug.Log($"Beginning off mesh link movement. {data.valid}; {data.activated}; {base.IsOwner}");
		while (normalizedTime < 1f && data.valid && data.activated && base.IsOwner)
		{
			float num = height * 4f * (normalizedTime - normalizedTime * normalizedTime);
			Debug.Log($"Moving on off mesh link; time: {normalizedTime}; y: {num}");
			agent.transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + num * Vector3.up;
			normalizedTime += Time.deltaTime / duration;
			yield return null;
		}
		agent.CompleteOffMeshLink();
		Debug.Log($"Completed off mesh link without interruption, position: {base.transform.position}");
		offMeshLinkCoroutine = null;
	}

	private void StopOffMeshLinkMovement()
	{
		if (offMeshLinkCoroutine == null)
		{
			return;
		}
		StopCoroutine(offMeshLinkCoroutine);
		offMeshLinkCoroutine = null;
		OffMeshLinkData currentOffMeshLinkData = agent.currentOffMeshLinkData;
		agent.CompleteOffMeshLink();
		if (currentOffMeshLinkData.valid)
		{
			Debug.Log($"Completed off mesh EARLY link due to an interruption; position: {base.transform.position}");
			if (Vector3.Distance(base.transform.position, currentOffMeshLinkData.startPos) < Vector3.Distance(base.transform.position, currentOffMeshLinkData.endPos))
			{
				Debug.Log($"Warping agent to start position at {currentOffMeshLinkData.startPos}");
				agent.Warp(currentOffMeshLinkData.startPos);
			}
			else
			{
				Debug.Log($"Warping agent to end position at {currentOffMeshLinkData.endPos}");
				agent.Warp(currentOffMeshLinkData.endPos);
			}
		}
		else
		{
			Debug.Log("Off mesh link data invalid; agent completing off mesh link anyway");
		}
	}

	private void DoSpringAnimation(bool springPopUp = false)
	{
		if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.6f, 70f, 25))
		{
			float num = Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position);
			if (num < 4f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
			}
			else if (num < 9f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f);
			}
		}
		if (currentAnimSpeed > 2f || springPopUp)
		{
			RoundManager.PlayRandomClip(creatureVoice, springNoises, randomize: false);
			if (animStopPoints.animationPosition == 1)
			{
				creatureAnimator.SetTrigger("springBoing");
			}
			else
			{
				creatureAnimator.SetTrigger("springBoingPosition2");
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		if (hitPlayerTimer >= 0f)
		{
			hitPlayerTimer -= Time.deltaTime;
		}
		if (!base.IsOwner)
		{
			stopMovementTimer = 5f;
			loseAggroTimer = 0f;
			if (offMeshLinkCoroutine != null)
			{
				StopCoroutine(offMeshLinkCoroutine);
			}
		}
		if (base.IsOwner)
		{
			if (agent.isOnOffMeshLink)
			{
				if (!stoppingMovement && agent.currentOffMeshLinkData.activated)
				{
					if (offMeshLinkCoroutine == null)
					{
						offMeshLinkCoroutine = StartCoroutine(Parabola(agent, 0.6f, 0.5f));
					}
				}
				else
				{
					StopOffMeshLinkMovement();
				}
			}
			else if (offMeshLinkCoroutine != null)
			{
				StopOffMeshLinkMovement();
			}
		}
		creatureAnimator.SetBool("OnCooldown", setOnCooldown);
		if (setOnCooldown)
		{
			mainCollider.isTrigger = true;
			stoppingMovement = true;
			hasStopped = true;
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			agent.autoTraverseOffMeshLink = false;
			creatureAnimator.SetFloat("walkSpeed", 4.7f);
			stoppingMovement = false;
			hasStopped = false;
			break;
		case 1:
		{
			if (base.IsOwner)
			{
				if (onCooldownPhase > 0f)
				{
					SwitchToBehaviourState(0);
					break;
				}
				if (stopAndGoMinimumInterval > 0f)
				{
					stopAndGoMinimumInterval -= Time.deltaTime;
				}
				if (!wasOwnerLastFrame)
				{
					wasOwnerLastFrame = true;
					if (!stoppingMovement && hitPlayerTimer < 0.12f)
					{
						agent.speed = currentChaseSpeed;
					}
					else
					{
						agent.speed = 0f;
					}
				}
				bool flag = false;
				for (int i = 0; i < 4; i++)
				{
					if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && (StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.6f, 68f) || StartOfRound.Instance.allPlayerScripts[i].HasLineOfSightToPosition(base.transform.position + Vector3.up * 3f, 68f)) && PlayerHasHorizontalLOS(StartOfRound.Instance.allPlayerScripts[i]) && Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, eye.position) > 0.3f)
					{
						flag = true;
					}
				}
				if (stunNormalizedTimer > 0f)
				{
					flag = true;
				}
				if (flag != stoppingMovement && stopAndGoMinimumInterval <= 0f)
				{
					stopAndGoMinimumInterval = 0.15f;
					if (flag)
					{
						SetAnimationStopServerRpc();
					}
					else
					{
						SetAnimationGoServerRpc();
					}
					stoppingMovement = flag;
				}
			}
			float num = 0f;
			if (stoppingMovement)
			{
				if (animStopPoints.canAnimationStop || stopMovementTimer > 0.27f)
				{
					if (!hasStopped)
					{
						hasStopped = true;
						DoSpringAnimation();
					}
					else if (inCooldownAnimation)
					{
						inCooldownAnimation = false;
						DoSpringAnimation(springPopUp: true);
					}
					if (RoundManager.Instance.currentMineshaftElevator != null && Vector3.Distance(base.transform.position, RoundManager.Instance.currentMineshaftElevator.elevatorInsidePoint.position) < 1f)
					{
						num = 0.5f;
					}
					if (mainCollider.isTrigger && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) > 0.3f)
					{
						mainCollider.isTrigger = false;
					}
					creatureAnimator.SetFloat("walkSpeed", 0f);
					currentAnimSpeed = 0f;
					if (base.IsOwner)
					{
						agent.speed = 0f;
						movingTowardsTargetPlayer = false;
						SetDestinationToPosition(base.transform.position);
					}
				}
				else
				{
					stopMovementTimer += Time.deltaTime;
				}
			}
			else
			{
				stopMovementTimer = 0f;
				if (hasStopped)
				{
					hasStopped = false;
					mainCollider.isTrigger = true;
					isMakingDistance = true;
				}
				currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, 6f, 5f * Time.deltaTime);
				creatureAnimator.SetFloat("walkSpeed", currentAnimSpeed);
				inCooldownAnimation = false;
				if (base.IsServer)
				{
					if (checkPositionInterval <= 0f)
					{
						checkPositionInterval = 0.65f;
						isMakingDistance = Vector3.Distance(base.transform.position, previousPosition) > 0.5f;
						previousPosition = base.transform.position;
					}
					else
					{
						checkPositionInterval -= Time.deltaTime;
					}
					num = ((!isMakingDistance) ? 0.2f : 1f);
				}
				if (base.IsOwner)
				{
					agent.speed = Mathf.Lerp(agent.speed, currentChaseSpeed, 4.5f * Time.deltaTime);
					movingTowardsTargetPlayer = true;
				}
			}
			if (base.IsServer)
			{
				if (num > 0f)
				{
					timeSpentMoving += Time.deltaTime * num;
				}
				if (timeSpentMoving > 9f)
				{
					onCooldownPhase = 11f;
					setOnCooldown = true;
					inCooldownAnimation = true;
					SetCoilheadOnCooldownClientRpc(setTrue: true);
					SwitchToBehaviourStateOnLocalClient(0);
				}
			}
			break;
		}
		}
	}

	[ServerRpc]
	public void SetAnimationStopServerRpc()
{		{
			SetAnimationStopClientRpc();
		}
}
	[ClientRpc]
	public void SetAnimationStopClientRpc()
			{
				stoppingMovement = true;
			}

	[ServerRpc]
	public void SetAnimationGoServerRpc()
{		{
			SetAnimationGoClientRpc();
		}
}
	[ClientRpc]
	public void SetAnimationGoClientRpc()
			{
				stoppingMovement = false;
			}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!stoppingMovement && currentBehaviourStateIndex == 1 && !(hitPlayerTimer >= 0f) && !setOnCooldown && !((double)(Time.realtimeSinceStartup - timeAtLastCooldown) < 0.45))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null)
			{
				hitPlayerTimer = 0.2f;
				playerControllerB.DamagePlayer(90, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 2);
				playerControllerB.JumpToFearLevel(1f);
				timeSinceHittingPlayer = Time.realtimeSinceStartup;
			}
		}
	}
}
