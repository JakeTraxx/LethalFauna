using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class PufferAI : EnemyAI
{
	private PlayerControllerB closestSeenPlayer;

	public AISearchRoutine roamMap;

	private float avoidPlayersTimer;

	private float fearTimer;

	private int previousBehaviourState = -1;

	public Transform lookAtPlayersCompass;

	private Coroutine shakeTailCoroutine;

	private bool inPuffingAnimation;

	private Vector3 agentLocalVelocity;

	private Vector3 previousPosition;

	public Transform animationContainer;

	private float velX;

	private float velZ;

	private float unclampedSpeed;

	private Vector3 lookAtNoise;

	private float timeSinceLookingAtNoise;

	private bool playerIsInLOS;

	private bool didStompAnimation;

	private bool inStompingAnimation;

	public AudioClip[] footstepsSFX;

	public AudioClip[] frightenSFX;

	public AudioClip stomp;

	public AudioClip angry;

	public AudioClip puff;

	public AudioClip nervousMumbling;

	public AudioClip rattleTail;

	public AudioClip bitePlayerSFX;

	[Space(5f)]
	public Transform tailPosition;

	public GameObject smokePrefab;

	private bool startedMovingAfterAlert;

	private float timeSinceAlert;

	private bool didPuffAnimation;

	private float timeSinceHittingPlayer;

	private bool gettingFarthestNodeFromPlayerAsync;

	private Transform farthestNodeFromTargetPlayer;

	public override void Start()
	{
		lookAtNoise = Vector3.zero;
		base.Start();
	}

	public override void DoAIInterval()
	{
		if (StartOfRound.Instance.livingPlayers == 0)
		{
			base.DoAIInterval();
			return;
		}
		base.DoAIInterval();
		if (stunNormalizedTimer > 0f)
		{
			return;
		}
		PlayerControllerB playerControllerB = null;
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (base.IsServer)
			{
				agent.angularSpeed = 300f;
				if (!roamMap.inProgress)
				{
					StartSearch(base.transform.position, roamMap);
				}
				playerControllerB = CheckLineOfSightForPlayer(45f, 20);
				playerIsInLOS = playerControllerB;
				if (playerIsInLOS)
				{
					ChangeOwnershipOfEnemy(playerControllerB.actualClientId);
					SwitchToBehaviourState(1);
				}
			}
			break;
		case 1:
			if (roamMap.inProgress)
			{
				StopSearch(roamMap);
			}
			playerControllerB = CheckLineOfSightForClosestPlayer(45f, 20, 2);
			playerIsInLOS = playerControllerB;
			if (!playerIsInLOS)
			{
				avoidPlayersTimer += AIIntervalTime;
				agent.angularSpeed = 300f;
			}
			else
			{
				avoidPlayersTimer = 0f;
				float num = Vector3.Distance(eye.position, playerControllerB.transform.position);
				if (!inPuffingAnimation)
				{
					if (num < 5f)
					{
						if (didPuffAnimation)
						{
							SwitchToBehaviourState(2);
							break;
						}
						if (timeSinceAlert > 1.5f)
						{
							didPuffAnimation = true;
							inPuffingAnimation = true;
							ShakeTailServerRpc();
						}
					}
					else if (num < 7f && !didStompAnimation)
					{
						fearTimer += AIIntervalTime;
						if (fearTimer > 1f)
						{
							didStompAnimation = true;
							StompServerRpc();
						}
					}
				}
				if (closestSeenPlayer == null || (playerControllerB != closestSeenPlayer && num < Vector3.Distance(eye.position, closestSeenPlayer.transform.position)))
				{
					closestSeenPlayer = playerControllerB;
					avoidPlayersTimer = 0f;
					ChangeOwnershipOfEnemy(closestSeenPlayer.actualClientId);
				}
			}
			if (!inPuffingAnimation && closestSeenPlayer != null)
			{
				AvoidClosestPlayer();
			}
			if (avoidPlayersTimer > 5f)
			{
				SwitchToBehaviourState(0);
				ChangeOwnershipOfEnemy(StartOfRound.Instance.allPlayerScripts[0].actualClientId);
			}
			break;
		case 2:
			if (closestSeenPlayer == null)
			{
				closestSeenPlayer = CheckLineOfSightForClosestPlayer(45f, 20, 2);
				break;
			}
			playerIsInLOS = CheckLineOfSightForPlayer(70f, 20, 2);
			SetMovingTowardsTargetPlayer(closestSeenPlayer);
			break;
		}
	}

	private void LookAtPosition(Vector3 look, bool lookInstantly = false)
	{
		agent.angularSpeed = 0f;
		lookAtPlayersCompass.LookAt(look);
		lookAtPlayersCompass.eulerAngles = new Vector3(0f, lookAtPlayersCompass.eulerAngles.y, 0f);
		if (lookInstantly)
		{
			base.transform.rotation = lookAtPlayersCompass.rotation;
		}
		else
		{
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, lookAtPlayersCompass.rotation, 10f * Time.deltaTime);
		}
	}

	private void CalculateAnimationDirection(float maxSpeed = 1.7f)
	{
		agentLocalVelocity = animationContainer.InverseTransformDirection(Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f) / (Time.deltaTime * 5f));
		velX = Mathf.Lerp(velX, 0f - agentLocalVelocity.x, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("moveX", Mathf.Clamp(velX, 0f - maxSpeed, maxSpeed));
		velZ = Mathf.Lerp(velZ, 0f - agentLocalVelocity.z, 10f * Time.deltaTime);
		creatureAnimator.SetFloat("moveZ", Mathf.Clamp(velZ, 0f - maxSpeed, maxSpeed));
		previousPosition = base.transform.position;
		creatureAnimator.SetFloat("movementSpeed", Mathf.Clamp(agentLocalVelocity.magnitude, 0f, maxSpeed));
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
		if (transform != null)
		{
			targetNode = transform;
			SetDestinationToPosition(targetNode.position);
			return;
		}
		agent.speed = 0f;
		fearTimer += AIIntervalTime;
		if (timeSinceAlert < 0.75f)
		{
			return;
		}
		if (fearTimer > 1f && !didStompAnimation)
		{
			didStompAnimation = true;
			inStompingAnimation = true;
			StompServerRpc();
		}
		else if (fearTimer > 3f)
		{
			if (didPuffAnimation)
			{
				SwitchToBehaviourState(2);
				return;
			}
			didPuffAnimation = true;
			inPuffingAnimation = true;
			ShakeTailServerRpc();
		}
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		float num = Vector3.Distance(noisePosition, base.transform.position);
		if (!(num > 15f))
		{
			if (Physics.Linecast(eye.position, noisePosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				noiseLoudness /= 2f;
			}
			if (!((double)(noiseLoudness / num) <= 0.045) && timeSinceLookingAtNoise > 5f)
			{
				timeSinceLookingAtNoise = 0f;
				lookAtNoise = noisePosition;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead || inPuffingAnimation || inStompingAnimation)
		{
			return;
		}
		if (base.IsOwner && gettingFarthestNodeFromPlayerAsync && closestSeenPlayer != null)
		{
			Transform transform = ChooseFarthestNodeFromPosition(maxAsyncIterations: (!(timeSinceAlert < 0.5f)) ? 10 : 30, pos: closestSeenPlayer.transform.position, avoidLineOfSight: true, offset: 0, doAsync: true, capDistance: true);
			if (!gotFarthestNodeAsync)
			{
				return;
			}
			farthestNodeFromTargetPlayer = transform;
			gettingFarthestNodeFromPlayerAsync = false;
		}
		timeSinceLookingAtNoise += Time.deltaTime;
		timeSinceHittingPlayer += Time.deltaTime;
		CalculateAnimationDirection(2f);
		if (stunNormalizedTimer > 0f)
		{
			creatureAnimator.SetLayerWeight(1, 1f);
		}
		else
		{
			creatureAnimator.SetLayerWeight(1, 0f);
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousBehaviourState != 0)
			{
				previousBehaviourState = 0;
				creatureAnimator.SetBool("alerted", value: false);
				agent.speed = 4f;
				playerIsInLOS = false;
				startedMovingAfterAlert = false;
				timeSinceAlert = 0f;
				creatureVoice.Stop();
				fearTimer = 0f;
				avoidPlayersTimer = 0f;
				didPuffAnimation = false;
				didStompAnimation = false;
				movingTowardsTargetPlayer = false;
			}
			if (!base.IsOwner)
			{
				break;
			}
			if (stunNormalizedTimer > 0f)
			{
				if (stunnedByPlayer != null)
				{
					ChangeOwnershipOfEnemy(stunnedByPlayer.actualClientId);
					SwitchToBehaviourState(1);
				}
				agent.speed = 0f;
			}
			else
			{
				agent.speed = 4f;
			}
			fearTimer = Mathf.Clamp(fearTimer - Time.deltaTime, 0f, 100f);
			if (!playerIsInLOS && timeSinceLookingAtNoise < 2f)
			{
				LookAtPosition(lookAtNoise);
			}
			break;
		case 1:
			if (previousBehaviourState != 1)
			{
				if (previousBehaviourState != 2)
				{
					creatureAnimator.SetTrigger("alert");
					RoundManager.PlayRandomClip(creatureVoice, frightenSFX);
					creatureSFX.PlayOneShot(rattleTail);
					WalkieTalkie.TransmitOneShotAudio(creatureSFX, rattleTail);
					unclampedSpeed = -6f;
				}
				previousBehaviourState = 1;
				creatureAnimator.SetBool("alerted", value: true);
				playerIsInLOS = false;
				agent.speed = 0f;
				startedMovingAfterAlert = false;
				timeSinceAlert = 0f;
				fearTimer = 0f;
				didPuffAnimation = false;
				didStompAnimation = false;
				creatureAnimator.SetBool("attacking", value: false);
				movingTowardsTargetPlayer = false;
			}
			if (!base.IsOwner)
			{
				break;
			}
			timeSinceAlert += Time.deltaTime;
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
				unclampedSpeed = 5f;
			}
			else
			{
				unclampedSpeed += Time.deltaTime * 4f;
				agent.speed = Mathf.Clamp(unclampedSpeed, 0f, 12f);
			}
			if (!startedMovingAfterAlert && agent.speed > 0.75f)
			{
				startedMovingAfterAlert = true;
				creatureVoice.clip = nervousMumbling;
				creatureVoice.Play();
			}
			if (!playerIsInLOS)
			{
				if (timeSinceLookingAtNoise < 1f)
				{
					LookAtPosition(lookAtNoise);
				}
				else if (avoidPlayersTimer < 1f && closestSeenPlayer != null)
				{
					LookAtPosition(closestSeenPlayer.transform.position);
				}
			}
			else
			{
				LookAtPosition(closestSeenPlayer.transform.position);
			}
			break;
		case 2:
			if (previousBehaviourState != 2)
			{
				previousBehaviourState = 2;
				creatureAnimator.SetBool("attacking", value: true);
				playerIsInLOS = false;
				unclampedSpeed = 9f;
				startedMovingAfterAlert = false;
				timeSinceAlert = 0f;
				didPuffAnimation = false;
				didStompAnimation = false;
			}
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
				SwitchToBehaviourState(1);
			}
			else
			{
				unclampedSpeed = Mathf.Clamp(unclampedSpeed - Time.deltaTime * 5f, -1f, 100f);
				agent.speed = Mathf.Clamp(unclampedSpeed, 0f, 12f);
			}
			if (unclampedSpeed <= -0.75f)
			{
				SwitchToBehaviourState(1);
			}
			break;
		}
	}

	[ServerRpc]
	public void StompServerRpc()
{		{
			StompClientRpc();
		}
}
	[ClientRpc]
	public void StompClientRpc()
{		{
			if (shakeTailCoroutine != null)
			{
				StopCoroutine(shakeTailCoroutine);
			}
			shakeTailCoroutine = StartCoroutine(stompAnimation());
		}
}
	[ServerRpc]
	public void ShakeTailServerRpc()
{		{
			ShakeTailClientRpc();
		}
}
	[ClientRpc]
	public void ShakeTailClientRpc()
{		{
			if (shakeTailCoroutine != null)
			{
				StopCoroutine(shakeTailCoroutine);
			}
			shakeTailCoroutine = StartCoroutine(shakeTailAnimation());
		}
}
	private IEnumerator stompAnimation()
	{
		didStompAnimation = true;
		inPuffingAnimation = true;
		creatureAnimator.SetTrigger("stomp");
		agent.speed = 0f;
		yield return new WaitForSeconds(0.15f);
		creatureSFX.PlayOneShot(stomp);
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, stomp);
		RoundManager.Instance.PlayAudibleNoise(creatureSFX.transform.position, 8f, 0.8f);
		yield return new WaitForSeconds(0.7f);
		timeSinceAlert = 0f;
		inStompingAnimation = false;
		inPuffingAnimation = false;
		unclampedSpeed = 0f;
	}

	private IEnumerator shakeTailAnimation()
	{
		didPuffAnimation = true;
		inPuffingAnimation = true;
		inStompingAnimation = false;
		creatureAnimator.SetTrigger("puff");
		creatureVoice.Stop();
		creatureVoice.PlayOneShot(angry);
		agent.speed = 0f;
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, angry);
		yield return new WaitForSeconds(0.5f);
		creatureSFX.PlayOneShot(puff);
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, puff);
		Object.Instantiate(smokePrefab, tailPosition.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
		yield return new WaitForSeconds(0.2f);
		timeSinceAlert = -2f;
		creatureVoice.clip = nervousMumbling;
		creatureVoice.Play();
		inPuffingAnimation = false;
		fearTimer = 0f;
		unclampedSpeed = 3f;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
		if (playerControllerB != null && timeSinceHittingPlayer > 1f)
		{
			timeSinceHittingPlayer = 0f;
			playerControllerB.DamagePlayer(20, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
			BitePlayerServerRpc((int)playerControllerB.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BitePlayerServerRpc(int playerBit)
			{
				BitePlayerClientRpc(playerBit);
			}

	[ClientRpc]
	public void BitePlayerClientRpc(int playerBit)
{		{
			if (unclampedSpeed > 0.25f)
			{
				unclampedSpeed = 0.25f;
			}
			timeSinceHittingPlayer = 0f;
			creatureVoice.PlayOneShot(bitePlayerSFX);
			WalkieTalkie.TransmitOneShotAudio(creatureVoice, bitePlayerSFX);
			creatureAnimator.SetTrigger("Bite");
			LookAtPosition(StartOfRound.Instance.allPlayerScripts[playerBit].transform.position, lookInstantly: true);
			if (base.IsOwner && currentBehaviourStateIndex == 0)
			{
				SwitchToBehaviourState(1);
			}
		}
}
	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
	}
}
