using System.Collections;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering.HighDefinition;

public class ForestGiantAI : EnemyAI, IVisibleThreat
{
	private Coroutine eatPlayerCoroutine;

	private bool inEatingPlayerAnimation;

	public Transform holdPlayerPoint;

	public AISearchRoutine roamPlanet;

	public AISearchRoutine searchForPlayers;

	private float velX;

	private float velZ;

	private Vector3 previousPosition;

	private Vector3 agentLocalVelocity;

	public Transform animationContainer;

	public TwoBoneIKConstraint reachForPlayerRig;

	public Transform reachForPlayerTarget;

	private float stopAndLookInterval;

	private float stopAndLookTimer;

	private float targetYRot;

	public float scrutiny = 1f;

	public float[] playerStealthMeters = new float[4];

	public float timeSpentStaring;

	public bool investigating;

	private bool hasBegunInvestigating;

	public Vector3 investigatePosition;

	public PlayerControllerB chasingPlayer;

	private bool lostPlayerInChase;

	private float noticePlayerTimer;

	private bool lookingAtTarget;

	public Transform turnCompass;

	public Transform lookTarget;

	private bool chasingPlayerInLOS;

	private float timeSinceChangingTarget;

	private bool hasLostPlayerInChaseDebounce;

	private bool triggerChaseByTouchingDebounce;

	public AudioSource farWideSFX;

	public DecalProjector bloodOnFaceDecal;

	private Vector3 lastSeenPlayerPositionInChase;

	private float timeSinceDetectingVoice;

	public Transform centerPosition;

	public Transform handBone;

	public Transform deathFallPosition;

	public AudioClip giantFall;

	public AudioClip giantCry;

	public AudioSource giantBurningAudio;

	public GameObject burningParticlesContainer;

	private float timeAtStartOfBurning;

	ThreatType IVisibleThreat.type => ThreatType.ForestGiant;

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
		return 18;
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
		if (agentLocalVelocity.sqrMagnitude > 0f)
		{
			return 1f;
		}
		return 0.75f;
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		enemyHP -= force;
		if ((float)enemyHP <= 0f && !isEnemyDead && base.IsOwner)
		{
			KillEnemyOnOwnerClient();
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		agent.speed = 0f;
		if (eatPlayerCoroutine != null)
		{
			StopCoroutine(eatPlayerCoroutine);
		}
		DropPlayerBody();
		creatureVoice.PlayOneShot(giantCry);
		burningParticlesContainer.SetActive(value: false);
	}

	public override void AnimationEventA()
	{
		base.AnimationEventA();
		RaycastHit[] array = Physics.SphereCastAll(deathFallPosition.position, 2.7f, deathFallPosition.forward, 3.9f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < array.Length; i++)
		{
			PlayerControllerB component = array[i].transform.GetComponent<PlayerControllerB>();
			if (component != null && component == GameNetworkManager.Instance.localPlayerController)
			{
				GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Gravity);
				break;
			}
		}
	}

	public override void HitFromExplosion(float distance)
	{
		base.HitFromExplosion(distance);
		if (!isEnemyDead && currentBehaviourStateIndex != 2)
		{
			timeAtStartOfBurning = Time.realtimeSinceStartup;
			if (base.IsOwner)
			{
				SwitchToBehaviourState(2);
			}
		}
	}

	public override void Start()
	{
		base.Start();
		for (int i = 0; i < playerStealthMeters.Length; i++)
		{
			playerStealthMeters[i] = 0f;
		}
		lookTarget.SetParent(null);
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
			}
			if (investigating)
			{
				if (!hasBegunInvestigating)
				{
					hasBegunInvestigating = true;
					StopSearch(roamPlanet, clear: false);
					SetDestinationToPosition(investigatePosition);
				}
				if (Vector3.Distance(base.transform.position, investigatePosition) < 5f)
				{
					investigating = false;
					hasBegunInvestigating = false;
				}
			}
			else if (!roamPlanet.inProgress)
			{
				Vector3 position = base.transform.position;
				if (previousBehaviourStateIndex == 1 && Vector3.Distance(base.transform.position, StartOfRound.Instance.elevatorTransform.position) < 30f)
				{
					position = ChooseFarthestNodeFromPosition(StartOfRound.Instance.elevatorTransform.position).position;
				}
				StartSearch(position, roamPlanet);
			}
			break;
		case 1:
			investigating = false;
			hasBegunInvestigating = false;
			if (roamPlanet.inProgress)
			{
				StopSearch(roamPlanet, clear: false);
			}
			if (lostPlayerInChase)
			{
				if (!searchForPlayers.inProgress)
				{
					Debug.Log("Forest giant starting search for players routine");
					searchForPlayers.searchWidth = 25f;
					StartSearch(lastSeenPlayerPositionInChase, searchForPlayers);
					Debug.Log("Lost player in chase; beginning search where the player was last seen");
				}
			}
			else
			{
				if (searchForPlayers.inProgress)
				{
					StopSearch(searchForPlayers);
					Debug.Log("Found player during chase; stopping search coroutine and moving after target player");
				}
				SetMovingTowardsTargetPlayer(chasingPlayer);
			}
			break;
		case 2:
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
			}
			if (!roamPlanet.inProgress)
			{
				roamPlanet.searchPrecision = 18f;
				StartSearch(ChooseFarthestNodeFromPosition(base.transform.position).position, roamPlanet);
			}
			break;
		}
	}

	public override void FinishedCurrentSearchRoutine()
	{
		if (base.IsOwner && currentBehaviourStateIndex == 1 && lostPlayerInChase && !chasingPlayerInLOS)
		{
			Debug.Log("Forest giant: Finished search; player not in line of sight, lost player, returning to roaming mode");
			SwitchToBehaviourState(0);
		}
	}

	public override void ReachedNodeInSearch()
	{
		base.ReachedNodeInSearch();
		if (base.IsOwner && currentBehaviourStateIndex == 0 && stopAndLookInterval > 12f)
		{
			stopAndLookInterval = 0f;
			stopAndLookTimer = Random.Range(3f, 12f);
			targetYRot = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(eye.position, 10f, 5);
		}
	}

	private void LateUpdate()
	{
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.transform.position = holdPlayerPoint.position;
			inSpecialAnimationWithPlayer.transform.rotation = holdPlayerPoint.rotation;
		}
		if (lookingAtTarget)
		{
			LookAtTarget();
		}
		creatureAnimator.SetBool("staring", lookingAtTarget);
		if (!(GameNetworkManager.Instance == null) && !(GameNetworkManager.Instance.localPlayerController == null))
		{
			farWideSFX.volume = Mathf.Clamp(Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) / (farWideSFX.maxDistance - 10f), 0f, 1f);
		}
	}

	private void GiantSeePlayerEffect()
	{
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead || GameNetworkManager.Instance.localPlayerController.isInsideFactory)
		{
			return;
		}
		if (currentBehaviourStateIndex == 1 && chasingPlayer == GameNetworkManager.Instance.localPlayerController && !lostPlayerInChase)
		{
			GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(1.4f);
			return;
		}
		bool flag = false;
		if (!GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && CheckLineOfSightForPosition(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, 45f, 70))
		{
			if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 15f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.7f);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.4f);
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		if (isEnemyDead)
		{
			giantBurningAudio.volume -= Time.deltaTime * 0.5f;
		}
		if ((stunNormalizedTimer > 0f && inEatingPlayerAnimation) || isEnemyDead || currentBehaviourStateIndex == 2)
		{
			StopKillAnimation();
		}
		else
		{
			GiantSeePlayerEffect();
		}
		if (isEnemyDead)
		{
			return;
		}
		creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
		CalculateAnimationDirection();
		stopAndLookInterval += Time.deltaTime;
		timeSinceChangingTarget += Time.deltaTime;
		timeSinceDetectingVoice += Time.deltaTime;
		switch (currentBehaviourStateIndex)
		{
		case 0:
			reachForPlayerRig.weight = Mathf.Lerp(reachForPlayerRig.weight, 0f, Time.deltaTime * 15f);
			lostPlayerInChase = false;
			triggerChaseByTouchingDebounce = false;
			hasLostPlayerInChaseDebounce = false;
			lookingAtTarget = false;
			if (!base.IsOwner)
			{
				break;
			}
			if (stopAndLookTimer > 0f)
			{
				stopAndLookTimer -= Time.deltaTime;
				turnCompass.eulerAngles = new Vector3(base.transform.eulerAngles.x, targetYRot, base.transform.eulerAngles.z);
				base.transform.rotation = Quaternion.Lerp(base.transform.rotation, turnCompass.rotation, 5f * Time.deltaTime);
				agent.speed = 0f;
			}
			else
			{
				if (stunNormalizedTimer > 0f && stunnedByPlayer != null && stunnedByPlayer != chasingPlayer)
				{
					FindAndTargetNewPlayerOnLocalClient(stunnedByPlayer);
					BeginChasingNewPlayerClientRpc((int)stunnedByPlayer.playerClientId);
				}
				agent.speed = 5f;
			}
			LookForPlayers();
			break;
		case 1:
			ReachForPlayerIfClose();
			if (!base.IsOwner)
			{
				break;
			}
			if (inEatingPlayerAnimation)
			{
				agent.speed = 0f;
				break;
			}
			LookForPlayers();
			if (lostPlayerInChase)
			{
				if (!hasLostPlayerInChaseDebounce)
				{
					lookingAtTarget = false;
					hasLostPlayerInChaseDebounce = true;
					HasLostPlayerInChaseClientRpc();
				}
				reachForPlayerRig.weight = Mathf.Lerp(reachForPlayerRig.weight, 0f, Time.deltaTime * 15f);
				if (stopAndLookTimer > 0f)
				{
					stopAndLookTimer -= Time.deltaTime;
					turnCompass.eulerAngles = new Vector3(base.transform.eulerAngles.x, targetYRot, base.transform.eulerAngles.z);
					base.transform.rotation = Quaternion.Lerp(base.transform.rotation, turnCompass.rotation, 5f * Time.deltaTime);
					agent.speed = 0f;
				}
				else if (stunNormalizedTimer > 0f)
				{
					agent.speed = 0f;
				}
				else
				{
					agent.speed = Mathf.Min(Mathf.Max(agent.speed, 0.1f) * 1.3f, 7f);
					Debug.Log($"agent speed: {agent.speed}");
				}
				if (chasingPlayerInLOS)
				{
					noticePlayerTimer = 0f;
					lostPlayerInChase = false;
					break;
				}
				noticePlayerTimer += Time.deltaTime;
				if (noticePlayerTimer > 9f)
				{
					SwitchToBehaviourState(0);
				}
				break;
			}
			lookTarget.position = chasingPlayer.transform.position;
			lookingAtTarget = true;
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
			}
			else
			{
				agent.speed = Mathf.Min(Mathf.Max(agent.speed, 0.1f) * 1.3f, 7f);
			}
			if (hasLostPlayerInChaseDebounce)
			{
				hasLostPlayerInChaseDebounce = false;
				HasFoundPlayerInChaseClientRpc();
			}
			if (chasingPlayerInLOS)
			{
				noticePlayerTimer = 0f;
				lastSeenPlayerPositionInChase = chasingPlayer.transform.position;
				break;
			}
			noticePlayerTimer += Time.deltaTime;
			if (noticePlayerTimer > 3f)
			{
				lostPlayerInChase = true;
			}
			break;
		case 2:
			lookingAtTarget = false;
			if (isEnemyDead)
			{
				break;
			}
			if (!burningParticlesContainer.activeSelf)
			{
				burningParticlesContainer.SetActive(value: true);
			}
			if (!giantBurningAudio.isPlaying)
			{
				giantBurningAudio.Play();
			}
			giantBurningAudio.volume = Mathf.Min(giantBurningAudio.volume + Time.deltaTime * 0.5f, 1f);
			if (base.IsOwner)
			{
				agent.speed = Mathf.Min(Mathf.Max(agent.speed, 0.1f) * 1.3f, 8f);
				if (Time.realtimeSinceStartup - timeAtStartOfBurning > 10f)
				{
					KillEnemyOnOwnerClient();
				}
			}
			break;
		}
	}

	private void ReachForPlayerIfClose()
	{
		if (stunNormalizedTimer <= 0f && !lostPlayerInChase && inSpecialAnimationWithPlayer == null && !Physics.Linecast(eye.position, chasingPlayer.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(base.transform.position, chasingPlayer.transform.position) < 8f)
		{
			reachForPlayerRig.weight = Mathf.Lerp(reachForPlayerRig.weight, 0.9f, Time.deltaTime * 6f);
			Vector3 vector = chasingPlayer.transform.position + Vector3.up * 0.5f;
			reachForPlayerTarget.position = new Vector3(vector.x + Random.Range(-0.2f, 0.2f), vector.y + Random.Range(-0.2f, 0.2f), vector.z + Random.Range(-0.2f, 0.2f));
		}
		else
		{
			reachForPlayerRig.weight = Mathf.Lerp(reachForPlayerRig.weight, 0f, Time.deltaTime * 15f);
		}
	}

	private void LookAtTarget()
	{
		turnCompass.LookAt(lookTarget);
		base.transform.rotation = Quaternion.Lerp(base.transform.rotation, turnCompass.rotation, 15f * Time.deltaTime);
		base.transform.localEulerAngles = new Vector3(0f, base.transform.localEulerAngles.y, 0f);
	}

	private void LookForPlayers()
	{
		PlayerControllerB[] allPlayersInLineOfSight = GetAllPlayersInLineOfSight(50f, 70, eye, 3f, StartOfRound.Instance.collidersRoomDefaultAndFoliage);
		if (allPlayersInLineOfSight != null)
		{
			PlayerControllerB playerControllerB = allPlayersInLineOfSight[0];
			int num = 0;
			float num2 = 1000f;
			PlayerControllerB playerControllerB2 = allPlayersInLineOfSight[0];
			float num3 = 0f;
			float num4 = 1f;
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				if (allPlayersInLineOfSight.Contains(StartOfRound.Instance.allPlayerScripts[i]))
				{
					float num5 = Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].transform.position, eye.position);
					if (!StartOfRound.Instance.allPlayerScripts[i].isCrouching)
					{
						num4 += 1f;
					}
					if (StartOfRound.Instance.allPlayerScripts[i].timeSincePlayerMoving < 0.1f)
					{
						num4 += 1f;
					}
					playerStealthMeters[i] += Mathf.Clamp(Time.deltaTime / (num5 * 0.21f) * scrutiny * num4, 0f, 1f);
					if (playerStealthMeters[i] > num3)
					{
						num3 = playerStealthMeters[i];
						playerControllerB2 = StartOfRound.Instance.allPlayerScripts[i];
					}
					if (num5 < num2)
					{
						playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
						num2 = num5;
						num = i;
					}
				}
				else
				{
					playerStealthMeters[i] -= Time.deltaTime * 0.33f;
				}
			}
			if (currentBehaviourStateIndex == 1)
			{
				if (lostPlayerInChase)
				{
					chasingPlayerInLOS = num3 > 0.15f;
					return;
				}
				chasingPlayerInLOS = allPlayersInLineOfSight.Contains(chasingPlayer);
				if (stunnedByPlayer != null)
				{
					playerControllerB = stunnedByPlayer;
				}
				if (playerControllerB != chasingPlayer && playerStealthMeters[num] > 0.3f && timeSinceChangingTarget > 2f)
				{
					FindAndTargetNewPlayerOnLocalClient(playerControllerB);
					if (base.IsServer)
					{
						BeginChasingNewPlayerServerRpc((int)playerControllerB.playerClientId);
					}
				}
				return;
			}
			if (stunnedByPlayer != null)
			{
				playerControllerB2 = stunnedByPlayer;
			}
			if (num3 > 1f || (bool)stunnedByPlayer)
			{
				BeginChasingNewPlayerClientRpc((int)playerControllerB2.playerClientId);
				chasingPlayerInLOS = true;
			}
			else if (num3 > 0.35f)
			{
				if (stopAndLookTimer < 2f)
				{
					stopAndLookTimer = 2f;
				}
				turnCompass.LookAt(playerControllerB2.transform);
				targetYRot = turnCompass.eulerAngles.y;
				timeSpentStaring += Time.deltaTime;
			}
			if (currentBehaviourStateIndex != 1 && timeSpentStaring > 3f && !investigating)
			{
				investigating = true;
				hasBegunInvestigating = false;
				investigatePosition = RoundManager.Instance.GetNavMeshPosition(playerControllerB2.transform.position);
			}
		}
		else
		{
			if (currentBehaviourStateIndex == 1)
			{
				chasingPlayerInLOS = false;
			}
			timeSpentStaring = 0f;
		}
	}

	public void FindAndTargetNewPlayerOnLocalClient(PlayerControllerB newPlayer)
	{
		chasingPlayer = newPlayer;
		timeSinceChangingTarget = 0f;
		stopAndLookTimer = 0f;
	}

	[ServerRpc]
	private void BeginChasingNewPlayerServerRpc(int playerId)
{		{
			BeginChasingNewPlayerClientRpc(playerId);
		}
}
	[ClientRpc]
	private void BeginChasingNewPlayerClientRpc(int playerId)
{		{
			noticePlayerTimer = 0f;
			timeSinceChangingTarget = 0f;
			chasingPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
			hasLostPlayerInChaseDebounce = false;
			lostPlayerInChase = false;
			if (timeSinceChangingTarget > 1f)
			{
				agent.speed = 0f;
			}
			SwitchToBehaviourStateOnLocalClient(1);
		}
}
	[ClientRpc]
	private void HasLostPlayerInChaseClientRpc()
			{
				lostPlayerInChase = true;
				lookingAtTarget = false;
			}

	[ClientRpc]
	private void HasFoundPlayerInChaseClientRpc()
			{
				lostPlayerInChase = false;
				lookingAtTarget = true;
			}

	private void CalculateAnimationDirection(float maxSpeed = 1f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f) / (Time.deltaTime * 4f));
		velX = Mathf.Lerp(velX, agentLocalVelocity.x, 5f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, agentLocalVelocity.z, 5f * Time.deltaTime);
		creatureAnimator.SetFloat("VelocityY", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = base.transform.position;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (inSpecialAnimationWithPlayer != null || inEatingPlayerAnimation || stunNormalizedTimer >= 0f || currentBehaviourStateIndex == 2)
		{
			return;
		}
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inEatingPlayerAnimation);
		if (!(playerControllerB != null) || !(playerControllerB == GameNetworkManager.Instance.localPlayerController))
		{
			return;
		}
		VehicleController vehicleController = Object.FindObjectOfType<VehicleController>();
		if (vehicleController != null && playerControllerB.physicsParent != null && playerControllerB.physicsParent == vehicleController.transform && !vehicleController.backDoorOpen)
		{
			return;
		}
		Vector3 vector = Vector3.Normalize((centerPosition.position - (GameNetworkManager.Instance.localPlayerController.transform.position + Vector3.up * 1.5f)) * 1000f);
		if (!Physics.Linecast(centerPosition.position + vector * 1.7f, GameNetworkManager.Instance.localPlayerController.transform.position + Vector3.up * 1.5f, out var _, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && ((!StartOfRound.Instance.shipIsLeaving && StartOfRound.Instance.shipHasLanded) || !GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom) && !(playerControllerB.inAnimationWithEnemy != null))
		{
			if (playerControllerB.inSpecialInteractAnimation && playerControllerB.currentTriggerInAnimationWith != null)
			{
				playerControllerB.currentTriggerInAnimationWith.CancelAnimationExternally();
			}
			if (currentBehaviourStateIndex == 0 && !triggerChaseByTouchingDebounce)
			{
				triggerChaseByTouchingDebounce = true;
				BeginChasingNewPlayerServerRpc((int)playerControllerB.playerClientId);
			}
			else
			{
				GrabPlayerServerRpc((int)playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void GrabPlayerServerRpc(int playerId)
{if(!(inSpecialAnimationWithPlayer != null))		{
			Vector3 position = base.transform.position;
			int enemyYRot = (int)base.transform.eulerAngles.y;
			if (Physics.Raycast(centerPosition.position, centerPosition.forward, out var _, 6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				enemyYRot = (int)RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(position, 20f, 5);
			}
			GrabPlayerClientRpc(playerId, position, enemyYRot);
		}
}
	[ClientRpc]
	public void GrabPlayerClientRpc(int playerId, Vector3 enemyPosition, int enemyYRot)
{if(!(inSpecialAnimationWithPlayer != null))			{
				BeginEatPlayer(StartOfRound.Instance.allPlayerScripts[playerId], enemyPosition, enemyYRot);
			}
}
	private void BeginEatPlayer(PlayerControllerB playerBeingEaten, Vector3 enemyPosition, int enemyYRot)
	{
		inSpecialAnimationWithPlayer = playerBeingEaten;
		inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
		inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
		if (eatPlayerCoroutine != null)
		{
			StopCoroutine(eatPlayerCoroutine);
		}
		eatPlayerCoroutine = StartCoroutine(EatPlayerAnimation(playerBeingEaten, enemyPosition, enemyYRot));
	}

	private IEnumerator EatPlayerAnimation(PlayerControllerB playerBeingEaten, Vector3 enemyPosition, int enemyYRot)
	{
		lookingAtTarget = false;
		creatureAnimator.SetTrigger("EatPlayer");
		inEatingPlayerAnimation = true;
		inSpecialAnimation = true;
		playerBeingEaten.isInElevator = false;
		playerBeingEaten.isInHangarShipRoom = false;
		Vector3 startPosition = base.transform.position;
		Quaternion startRotation = base.transform.rotation;
		for (int i = 0; i < 10; i++)
		{
			base.transform.position = Vector3.Lerp(startPosition, enemyPosition, (float)i / 10f);
			base.transform.rotation = Quaternion.Lerp(startRotation, Quaternion.Euler(base.transform.eulerAngles.x, enemyYRot, base.transform.eulerAngles.z), (float)i / 10f);
			yield return new WaitForSeconds(0.01f);
		}
		base.transform.position = enemyPosition;
		base.transform.rotation = Quaternion.Euler(base.transform.eulerAngles.x, enemyYRot, base.transform.eulerAngles.z);
		serverRotation = base.transform.eulerAngles;
		yield return new WaitForSeconds(0.2f);
		inSpecialAnimation = false;
		yield return new WaitForSeconds(4.4f);
		if (playerBeingEaten.inAnimationWithEnemy == this && !playerBeingEaten.isPlayerDead)
		{
			inSpecialAnimationWithPlayer = null;
			playerBeingEaten.KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Crushing);
			playerBeingEaten.inSpecialInteractAnimation = false;
			playerBeingEaten.inAnimationWithEnemy = null;
			bloodOnFaceDecal.enabled = true;
			yield return new WaitForSeconds(3f);
		}
		else
		{
			creatureVoice.Stop();
		}
		inEatingPlayerAnimation = false;
		inSpecialAnimationWithPlayer = null;
		if (base.IsOwner)
		{
			if (CheckLineOfSightForPlayer(50f, 15) != null)
			{
				_ = chasingPlayer;
			}
			else
			{
				SwitchToBehaviourState(0);
			}
		}
	}

	private void DropPlayerBody()
	{
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = null;
			inSpecialAnimationWithPlayer = null;
		}
	}

	private void StopKillAnimation()
	{
		if (eatPlayerCoroutine != null)
		{
			StopCoroutine(eatPlayerCoroutine);
		}
		inEatingPlayerAnimation = false;
		inSpecialAnimation = false;
		DropPlayerBody();
		creatureVoice.Stop();
	}

	private void ReactToNoise(float distanceToNoise, Vector3 noisePosition)
	{
		if (currentBehaviourStateIndex == 1)
		{
			if (chasingPlayerInLOS && distanceToNoise - Vector3.Distance(base.transform.position, chasingPlayer.transform.position) < -3f)
			{
				stopAndLookTimer = 1f;
				turnCompass.LookAt(noisePosition);
				targetYRot = turnCompass.eulerAngles.y;
			}
			else if (distanceToNoise < 15f && noticePlayerTimer > 3f)
			{
				stopAndLookTimer = 2f;
				turnCompass.LookAt(noisePosition);
				targetYRot = turnCompass.eulerAngles.y;
			}
		}
		else
		{
			stopAndLookTimer = 1.5f;
			turnCompass.LookAt(noisePosition);
			targetYRot = turnCompass.eulerAngles.y;
			timeSpentStaring += 0.3f;
			if (timeSpentStaring > 3f)
			{
				investigating = true;
				hasBegunInvestigating = false;
				investigatePosition = RoundManager.Instance.GetNavMeshPosition(noisePosition);
			}
		}
	}

	[ServerRpc]
	public void DetectPlayerVoiceServerRpc(Vector3 noisePosition)
{		{
			float distanceToNoise = Vector3.Distance(noisePosition, base.transform.position);
			ReactToNoise(distanceToNoise, noisePosition);
		}
}}
