using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class CentipedeAI : EnemyAI
{
	public PlayerControllerB clingingToPlayer;

	public AudioClip fallShriek;

	public AudioClip hitGroundSFX;

	public AudioClip hitCentipede;

	public AudioClip[] shriekClips;

	private int offsetNodeAmount = 6;

	private Vector3 mainEntrancePosition;

	public AnimationCurve fallToGroundCurve;

	public Vector3 ceilingHidingPoint;

	private RaycastHit rayHit;

	public Transform tempTransform;

	private Ray ray;

	private bool clingingToCeiling;

	private Coroutine ceilingAnimationCoroutine;

	private bool startedCeilingAnimationCoroutine;

	private Coroutine killAnimationCoroutine;

	private Vector3 propelVelocity = Vector3.zero;

	private float damagePlayerInterval;

	private bool clingingToLocalClient;

	private bool clingingToDeadBody;

	private bool inDroppingOffPlayerAnim;

	private Vector3 firstKilledPlayerPosition = Vector3.zero;

	private bool pathToFirstKilledBodyIsClear = true;

	private bool syncedPositionInPrepForCeilingAnimation;

	public Transform modelContainer;

	private float updateOffsetPositionInterval;

	private Vector3 offsetTargetPos;

	private bool triggeredFall;

	public AudioSource clingingToPlayer2DAudio;

	public AudioClip clingToPlayer3D;

	private float chaseTimer;

	private float stuckTimer;

	private Coroutine beginClingingToCeilingCoroutine;

	private Coroutine dropFromCeilingCoroutine;

	private bool singlePlayerSecondChanceGiven;

	private bool choseHidingSpotNoPlayersNearby;

	public override void Start()
	{
		mainEntrancePosition = RoundManager.FindMainEntrancePosition();
		offsetTargetPos = base.transform.position;
		base.Start();
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
		{
			return;
		}
		if (currentBehaviourStateIndex == 0 && firstKilledPlayerPosition != Vector3.zero && pathToFirstKilledBodyIsClear && Vector3.Distance(base.transform.position, firstKilledPlayerPosition) < 13f)
		{
			choseHidingSpotNoPlayersNearby = false;
			ChooseHidingSpotNearPlayer(firstKilledPlayerPosition, targetingPositionOfFirstKilledPlayer: true);
		}
		else if (!TargetClosestPlayer())
		{
			if (!choseHidingSpotNoPlayersNearby)
			{
				choseHidingSpotNoPlayersNearby = true;
				SetDestinationToNode(ChooseFarthestNodeFromPosition(mainEntrancePosition, avoidLineOfSight: false, (allAINodes.Length / 2 + thisEnemyIndex) % allAINodes.Length));
			}
			else if (PathIsIntersectedByLineOfSight(destination, calculatePathDistance: false, avoidLineOfSight: false))
			{
				choseHidingSpotNoPlayersNearby = false;
			}
			if (currentBehaviourStateIndex == 2)
			{
				SwitchToBehaviourState(0);
			}
		}
		else
		{
			choseHidingSpotNoPlayersNearby = false;
			if (currentBehaviourStateIndex == 0)
			{
				ChooseHidingSpotNearPlayer(targetPlayer.transform.position);
			}
			else if (currentBehaviourStateIndex == 2)
			{
				movingTowardsTargetPlayer = true;
			}
		}
	}

	public void ChooseHidingSpotNearPlayer(Vector3 targetPos, bool targetingPositionOfFirstKilledPlayer = false)
	{
		movingTowardsTargetPlayer = false;
		if (targetNode != null)
		{
			if (!PathIsIntersectedByLineOfSight(targetNode.position))
			{
				SetDestinationToNode(targetNode);
				return;
			}
			if (targetingPositionOfFirstKilledPlayer)
			{
				pathToFirstKilledBodyIsClear = false;
			}
		}
		_ = (offsetNodeAmount + thisEnemyIndex) % allAINodes.Length;
		if (targetingPositionOfFirstKilledPlayer)
		{
			Random.Range(0, 3);
		}
		Transform transform = ChooseClosestNodeToPosition(targetPos, avoidLineOfSight: true, offsetNodeAmount);
		if (transform != null)
		{
			SetDestinationToNode(transform);
			return;
		}
		if (targetingPositionOfFirstKilledPlayer)
		{
			pathToFirstKilledBodyIsClear = false;
			return;
		}
		transform = ChooseClosestNodeToPosition(base.transform.position);
		SetDestinationToNode(transform);
	}

	private void SetDestinationToNode(Transform moveTowardsNode)
	{
		targetNode = moveTowardsNode;
		SetDestinationToPosition(targetNode.position);
	}

	private void LateUpdate()
	{
		if (isEnemyDead)
		{
			base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
		}
		else if (clingingToPlayer == null)
		{
			if (!clingingToCeiling)
			{
				if (updateOffsetPositionInterval <= 0f)
				{
					offsetTargetPos = RoundManager.Instance.RandomlyOffsetPosition(base.transform.position, 1.5f);
					updateOffsetPositionInterval = 0.04f;
				}
				else
				{
					modelContainer.position = Vector3.Lerp(modelContainer.position, offsetTargetPos, 3f * Time.deltaTime);
					updateOffsetPositionInterval -= Time.deltaTime;
				}
			}
			else
			{
				modelContainer.localPosition = Vector3.zero;
			}
		}
		else
		{
			modelContainer.localPosition = Vector3.zero;
			if (clingingToDeadBody && clingingToPlayer.deadBody != null)
			{
				base.transform.position = clingingToPlayer.deadBody.bodyParts[0].transform.position;
				base.transform.eulerAngles = clingingToPlayer.deadBody.bodyParts[0].transform.eulerAngles;
			}
			else
			{
				UpdatePositionToClingingPlayerHead();
			}
		}
	}

	private void UpdatePositionToClingingPlayerHead()
	{
		if (clingingToLocalClient)
		{
			base.transform.position = clingingToPlayer.gameplayCamera.transform.position;
			base.transform.eulerAngles = clingingToPlayer.gameplayCamera.transform.eulerAngles;
		}
		else
		{
			base.transform.position = clingingToPlayer.playerGlobalHead.position + clingingToPlayer.playerGlobalHead.up * 0.38f;
			base.transform.eulerAngles = clingingToPlayer.playerGlobalHead.eulerAngles;
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (base.IsOwner)
			{
				IncreaseSpeedSlowly(10f);
				movingTowardsTargetPlayer = false;
				if (targetNode != null)
				{
					tempTransform.position = new Vector3(targetNode.position.x, base.transform.position.y, targetNode.position.z);
					float num = Vector3.Distance(base.transform.position, tempTransform.position);
					if (num < 0.3f && !Physics.Linecast(base.transform.position, targetNode.position, 256))
					{
						RaycastToCeiling();
					}
					else if (num < 2.5f && !syncedPositionInPrepForCeilingAnimation)
					{
						syncedPositionInPrepForCeilingAnimation = true;
						SyncPositionToClients();
					}
				}
				if (agent.velocity.sqrMagnitude < 0.002f)
				{
					stuckTimer += Time.deltaTime;
					if (stuckTimer > 4f)
					{
						stuckTimer = 0f;
						offsetNodeAmount++;
						targetNode = null;
					}
				}
			}
			chaseTimer = 0f;
			break;
		case 1:
			if (!clingingToCeiling)
			{
				if (!startedCeilingAnimationCoroutine && ceilingAnimationCoroutine == null)
				{
					startedCeilingAnimationCoroutine = true;
					ceilingAnimationCoroutine = StartCoroutine(clingToCeiling());
				}
				break;
			}
			base.transform.position = Vector3.SmoothDamp(base.transform.position, ceilingHidingPoint, ref propelVelocity, 0.1f);
			ray = new Ray(base.transform.position, Vector3.down);
			if (Physics.SphereCast(ray, 2.15f, out rayHit, 20f, StartOfRound.Instance.playersMask) && rayHit.transform == GameNetworkManager.Instance.localPlayerController.transform && !clingingToPlayer && !Physics.Linecast(rayHit.transform.position, base.transform.position, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore) && !triggeredFall)
			{
				triggeredFall = true;
				TriggerCentipedeFallServerRpc(NetworkManager.Singleton.LocalClientId);
			}
			break;
		case 2:
			triggeredFall = false;
			if (clingingToCeiling)
			{
				if (!startedCeilingAnimationCoroutine && ceilingAnimationCoroutine == null)
				{
					startedCeilingAnimationCoroutine = true;
					ceilingAnimationCoroutine = StartCoroutine(fallFromCeiling());
				}
			}
			else if (base.IsOwner)
			{
				IncreaseSpeedSlowly();
				chaseTimer += Time.deltaTime;
				if (chaseTimer > 10f)
				{
					chaseTimer = 0f;
					SwitchToBehaviourState(0);
				}
			}
			break;
		case 3:
			if (!(clingingToPlayer != null))
			{
				break;
			}
			if (base.IsOwner && !clingingToPlayer.isInsideFactory && !clingingToPlayer.isPlayerDead)
			{
				KillEnemyOnOwnerClient();
				break;
			}
			if (clingingToLocalClient)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
				DamagePlayerOnIntervals();
			}
			else if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 60f, 12))
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.6f, 0.9f);
			}
			if (clingingToPlayer != null && clingingToPlayer.isPlayerDead && !inDroppingOffPlayerAnim)
			{
				inDroppingOffPlayerAnim = true;
				StopClingingToPlayer(playerDead: true);
			}
			break;
		}
	}

	private void DamagePlayerOnIntervals()
	{
		if (damagePlayerInterval <= 0f && !inDroppingOffPlayerAnim)
		{
			if (stunNormalizedTimer > 0f || (StartOfRound.Instance.connectedPlayersAmount <= 0 && clingingToPlayer.health <= 15 && !singlePlayerSecondChanceGiven))
			{
				singlePlayerSecondChanceGiven = true;
				inDroppingOffPlayerAnim = true;
				StopClingingServerRpc(playerDead: false);
			}
			else
			{
				clingingToPlayer.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Suffocation);
				damagePlayerInterval = 2f;
			}
		}
		else
		{
			damagePlayerInterval -= Time.deltaTime;
		}
	}

	private void IncreaseSpeedSlowly(float increaseSpeed = 1.5f)
	{
		if (stunNormalizedTimer > 0f)
		{
			creatureAnimator.SetBool("stunned", value: true);
			agent.speed = 0f;
		}
		else
		{
			creatureAnimator.SetBool("stunned", value: false);
			agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime * 1.5f, 0f, 5.5f);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopClingingServerRpc(bool playerDead)
			{
				StopClingingClientRpc(playerDead);
			}

	[ClientRpc]
	public void StopClingingClientRpc(bool playerDead)
			{
				inDroppingOffPlayerAnim = true;
				StopClingingToPlayer(playerDead);
			}

	private void OnEnable()
	{
		StartOfRound.Instance.playerTeleportedEvent.AddListener(OnPlayerTeleport);
	}

	private void OnDisable()
	{
		StartOfRound.Instance.playerTeleportedEvent.RemoveListener(OnPlayerTeleport);
	}

	private void OnPlayerTeleport(PlayerControllerB playerTeleported)
	{
		if (clingingToPlayer == playerTeleported && base.IsOwner)
		{
			KillEnemyOnOwnerClient();
		}
	}

	private void StopClingingToPlayer(bool playerDead)
	{
		if (clingingToPlayer.currentVoiceChatAudioSource == null)
		{
			StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
		}
		if (clingingToPlayer.currentVoiceChatAudioSource != null)
		{
			clingingToPlayer.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = 1f;
			OccludeAudio component = clingingToPlayer.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
			component.overridingLowPass = false;
			component.lowPassOverride = 20000f;
			clingingToPlayer.voiceMuffledByEnemy = false;
		}
		if (clingingToLocalClient)
		{
			clingingToPlayer2DAudio.Stop();
		}
		else
		{
			creatureSFX.Stop();
		}
		clingingToLocalClient = false;
		if (killAnimationCoroutine != null)
		{
			StopCoroutine(killAnimationCoroutine);
		}
		killAnimationCoroutine = StartCoroutine(UnclingFromPlayer(clingingToPlayer, playerDead));
	}

	private IEnumerator UnclingFromPlayer(PlayerControllerB playerBeingKilled, bool playerDead = true)
	{
		if (playerDead)
		{
			clingingToDeadBody = true;
			yield return new WaitForSeconds(1.5f);
			clingingToDeadBody = false;
		}
		clingingToPlayer = null;
		creatureAnimator.SetBool("clingingToPlayer", value: false);
		ray = new Ray(base.transform.position, Vector3.down);
		_ = base.transform.position;
		Vector3 startPosition = base.transform.position;
		Vector3 groundPosition = ((!Physics.Raycast(ray, out rayHit, 40f, 256)) ? RoundManager.Instance.GetNavMeshPosition(base.transform.position) : rayHit.point);
		float fallTime = 0.2f;
		while (fallTime < 1f)
		{
			yield return null;
			fallTime += Time.deltaTime * 4f;
			base.transform.position = Vector3.Lerp(startPosition, groundPosition, fallToGroundCurve.Evaluate(fallTime));
		}
		if (base.IsOwner)
		{
			agent.speed = 0f;
		}
		else
		{
			base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
		}
		serverPosition = base.transform.position;
		inSpecialAnimation = false;
		inDroppingOffPlayerAnim = false;
		SwitchToBehaviourStateOnLocalClient(0);
		if (playerDead)
		{
			firstKilledPlayerPosition = base.transform.position;
			pathToFirstKilledBodyIsClear = true;
		}
		movingTowardsTargetPlayer = false;
		targetNode = null;
	}

	public override void CancelSpecialAnimationWithPlayer()
	{
		base.CancelSpecialAnimationWithPlayer();
		_ = base.IsOwner;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!(stunNormalizedTimer >= 0f) && currentBehaviourStateIndex == 2 && !(clingingToPlayer != null))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null)
			{
				clingingToPlayer = playerControllerB;
				ClingToPlayerServerRpc(playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ClingToPlayerServerRpc(ulong playerObjectId)
			{
				ClingToPlayerClientRpc(playerObjectId);
			}

	[ClientRpc]
	public void ClingToPlayerClientRpc(ulong playerObjectId)
			{
				ClingToPlayer(StartOfRound.Instance.allPlayerScripts[playerObjectId]);
			}

	private void ClingToPlayer(PlayerControllerB playerScript)
	{
		if (ceilingAnimationCoroutine != null)
		{
			StopCoroutine(ceilingAnimationCoroutine);
			ceilingAnimationCoroutine = null;
		}
		startedCeilingAnimationCoroutine = false;
		clingingToCeiling = false;
		clingingToLocalClient = playerScript == GameNetworkManager.Instance.localPlayerController;
		clingingToPlayer = playerScript;
		inSpecialAnimation = true;
		agent.enabled = false;
		playerScript.DropAllHeldItems();
		creatureAnimator.SetBool("clingingToPlayer", value: true);
		if (clingingToPlayer.currentVoiceChatAudioSource == null)
		{
			StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
		}
		if (clingingToPlayer.currentVoiceChatAudioSource != null)
		{
			clingingToPlayer.currentVoiceChatAudioSource.GetComponent<AudioLowPassFilter>().lowpassResonanceQ = 5f;
			OccludeAudio component = clingingToPlayer.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
			component.overridingLowPass = true;
			component.lowPassOverride = 500f;
			clingingToPlayer.voiceMuffledByEnemy = true;
		}
		if (clingingToLocalClient)
		{
			clingingToPlayer2DAudio.Play();
			GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
		}
		else
		{
			creatureSFX.clip = clingToPlayer3D;
			creatureSFX.Play();
		}
		inDroppingOffPlayerAnim = false;
		SwitchToBehaviourStateOnLocalClient(3);
	}

	private IEnumerator fallFromCeiling()
	{
		targetNode = null;
		Vector3 startPosition = base.transform.position;
		Vector3 groundPosition = base.transform.position;
		ray = new Ray(base.transform.position, Vector3.down);
		if (Physics.Raycast(ray, out rayHit, 20f, 268435712))
		{
			groundPosition = rayHit.point;
		}
		else
		{
			Debug.LogError("Centipede: I could not get a raycast to the ground after falling from the ceiling! Choosing the closest nav mesh position to self.");
			startPosition = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(4f), default(NavMeshHit), 7f);
			if (base.IsOwner && !RoundManager.Instance.GotNavMeshPositionResult)
			{
				KillEnemyOnOwnerClient(overrideDestroy: true);
			}
		}
		float fallTime = 0f;
		while (fallTime < 1f)
		{
			yield return null;
			fallTime += Time.deltaTime * 2.5f;
			base.transform.position = Vector3.Lerp(startPosition, groundPosition, fallToGroundCurve.Evaluate(fallTime));
		}
		creatureSFX.PlayOneShot(hitGroundSFX);
		float distToPlayer = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position);
		if (distToPlayer < 13f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
		serverPosition = base.transform.position;
		if (base.IsOwner)
		{
			agent.speed = 0f;
		}
		else
		{
			base.transform.eulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
		}
		clingingToCeiling = false;
		inSpecialAnimation = false;
		yield return new WaitForSeconds(0.5f);
		RoundManager.PlayRandomClip(creatureSFX, shriekClips);
		if (distToPlayer < 7f)
		{
			GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);
		}
		ceilingAnimationCoroutine = null;
		startedCeilingAnimationCoroutine = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void TriggerCentipedeFallServerRpc(ulong clientId)
			{
				thisNetworkObject.ChangeOwnership(clientId);
				SwitchToBehaviourClientRpc(2);
			}

	private IEnumerator clingToCeiling()
	{
		yield return new WaitForSeconds(0.52f);
		if (currentBehaviourStateIndex != 1)
		{
			clingingToCeiling = false;
			startedCeilingAnimationCoroutine = false;
		}
		else
		{
			clingingToCeiling = true;
			ceilingAnimationCoroutine = null;
			startedCeilingAnimationCoroutine = false;
		}
	}

	private void RaycastToCeiling()
	{
		ray = new Ray(base.transform.position, Vector3.up);
		if (Physics.Raycast(ray, out rayHit, 20f, 256))
		{
			ceilingHidingPoint = ray.GetPoint(rayHit.distance - 0.8f);
			ceilingHidingPoint = RoundManager.Instance.RandomlyOffsetPosition(ceilingHidingPoint, 2.25f);
			SwitchToBehaviourStateOnLocalClient(1);
			syncedPositionInPrepForCeilingAnimation = false;
			inSpecialAnimation = true;
			agent.enabled = false;
			SwitchToHidingOnCeilingServerRpc(ceilingHidingPoint);
		}
		else
		{
			offsetNodeAmount++;
			targetNode = null;
			Debug.LogError("Centipede: Raycast to ceiling failed. Setting different node offset and resuming search for a hiding spot.");
		}
	}

	[ServerRpc]
	public void SwitchToHidingOnCeilingServerRpc(Vector3 ceilingPoint)
{		{
			SwitchToHidingOnCeilingClientRpc(ceilingPoint);
		}
}
	[ClientRpc]
	public void SwitchToHidingOnCeilingClientRpc(Vector3 ceilingPoint)
			{
				SwitchToBehaviourStateOnLocalClient(1);
				syncedPositionInPrepForCeilingAnimation = false;
				inSpecialAnimation = true;
				agent.enabled = false;
				ceilingHidingPoint = ceilingPoint;
			}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		creatureSFX.PlayOneShot(hitCentipede);
		StartCoroutine(delayedShriek());
		if (base.IsServer)
		{
			ReactBehaviourToBeingHurt();
		}
		enemyHP -= force;
		if (enemyHP <= 0 && base.IsOwner)
		{
			KillEnemyOnOwnerClient();
		}
	}

	public override void SetEnemyStunned(bool setToStunned, float setToStunTime = 1f, PlayerControllerB setStunnedByPlayer = null)
	{
		base.SetEnemyStunned(setToStunned, setToStunTime);
		if (base.IsServer)
		{
			ReactBehaviourToBeingHurt();
		}
	}

	public void ReactBehaviourToBeingHurt()
	{
		switch (currentBehaviourStateIndex)
		{
		case 2:
			GetHitAndRunAwayServerRpc();
			targetNode = null;
			break;
		case 3:
			if (!inDroppingOffPlayerAnim)
			{
				inDroppingOffPlayerAnim = true;
				StopClingingServerRpc(playerDead: false);
			}
			break;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void GetHitAndRunAwayServerRpc()
			{
				GetHitAndRunAwayClientRpc();
			}

	[ClientRpc]
	public void GetHitAndRunAwayClientRpc()
			{
				SwitchToBehaviourStateOnLocalClient(0);
				targetNode = null;
			}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy();
		agent.enabled = false;
		if (clingingToPlayer != null)
		{
			UpdatePositionToClingingPlayerHead();
			StopClingingToPlayer(playerDead: false);
		}
		if (clingingToCeiling && ceilingAnimationCoroutine == null)
		{
			ceilingAnimationCoroutine = StartCoroutine(fallFromCeiling());
		}
		modelContainer.localPosition = Vector3.zero;
	}

	private IEnumerator delayedShriek()
	{
		yield return new WaitForSeconds(0.2f);
		creatureVoice.pitch = 1.7f;
		RoundManager.PlayRandomClip(creatureVoice, shriekClips, randomize: false);
	}
}
