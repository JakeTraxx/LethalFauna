using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SandWormAI : EnemyAI
{
	public AudioSource groundAudio;

	public ParticleSystem emergeFromGroundParticle1;

	public ParticleSystem emergeFromGroundParticle2;

	public ParticleSystem hitGroundParticle;

	public AudioClip[] groundRumbleSFX;

	public AudioClip[] ambientRumbleSFX;

	public AudioClip hitGroundSFX;

	public AudioClip emergeFromGroundSFX;

	public AudioClip[] roarSFX;

	public bool inEmergingState;

	public bool emerged;

	private int timesEmerging;

	public bool hitGroundInAnimation;

	public Transform endingPosition;

	public Transform[] airPathNodes;

	public Vector3 endOfFlightPathPosition;

	private Coroutine emergingFromGroundCoroutine;

	public AISearchRoutine roamMap;

	public float chaseTimer;

	private int stateLastFrame;

	private NavMeshHit navHit;

	private System.Random sandWormRandom;

	public override void Start()
	{
		base.Start();
		sandWormRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 15 + thisEnemyIndex);
		roamMap.randomized = true;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
		{
			return;
		}
		PlayerControllerB playerControllerB = null;
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (!emerged && !inEmergingState)
			{
				if (!roamMap.inProgress)
				{
					StartSearch(base.transform.position, roamMap);
				}
				agent.speed = 4f;
				playerControllerB = GetClosestPlayer(requireLineOfSight: false, cannotBeInShip: true, cannotBeNearShip: true);
				if (playerControllerB != null && mostOptimalDistance < 15f)
				{
					SetMovingTowardsTargetPlayer(playerControllerB);
					SwitchToBehaviourState(1);
					chaseTimer = 0f;
				}
			}
			break;
		case 1:
			if (roamMap.inProgress)
			{
				StopSearch(roamMap);
			}
			targetPlayer = GetClosestPlayer(requireLineOfSight: false, cannotBeInShip: true, cannotBeNearShip: true);
			if (mostOptimalDistance > 19f)
			{
				targetPlayer = null;
			}
			if (targetPlayer == null)
			{
				SwitchToBehaviourState(0);
				break;
			}
			SetMovingTowardsTargetPlayer(targetPlayer);
			if (chaseTimer < 1.5f && Vector3.Distance(base.transform.position, targetPlayer.transform.position) < 4f && !(Vector3.Distance(StartOfRound.Instance.shipInnerRoomBounds.ClosestPoint(base.transform.position), base.transform.position) < 9f) && UnityEngine.Random.Range(0, 100) < 17)
			{
				StartEmergeAnimation();
			}
			break;
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		if (stateLastFrame != currentBehaviourStateIndex)
		{
			stateLastFrame = currentBehaviourStateIndex;
			chaseTimer = 0f;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (creatureSFX.isPlaying)
			{
				creatureSFX.Stop();
			}
			break;
		case 1:
			if (!creatureSFX.isPlaying && !inEmergingState && !emerged)
			{
				int num = UnityEngine.Random.Range(0, ambientRumbleSFX.Length);
				creatureSFX.clip = ambientRumbleSFX[num];
				creatureSFX.Play();
			}
			if (!base.IsOwner)
			{
				break;
			}
			if (targetPlayer == null)
			{
				SwitchToBehaviourState(0);
				break;
			}
			if (!PlayerIsTargetable(targetPlayer, cannotBeInShip: true) || Vector3.Distance(targetPlayer.transform.position, base.transform.position) > 22f)
			{
				chaseTimer += Time.deltaTime;
			}
			else
			{
				chaseTimer = 0f;
			}
			if (chaseTimer > 6f)
			{
				SwitchToBehaviourState(0);
			}
			break;
		}
	}

	public void StartEmergeAnimation()
	{
		if (!base.IsServer)
		{
			return;
		}
		inEmergingState = true;
		float num = RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(base.transform.position + Vector3.up * 1.5f, 30f);
		num += UnityEngine.Random.Range(-45f, 45f);
		agent.enabled = false;
		inSpecialAnimation = true;
		base.transform.eulerAngles = new Vector3(0f, num, 0f);
		bool flag = false;
		for (int i = 0; i < 6; i++)
		{
			RaycastHit hitInfo;
			for (int j = 0; j < airPathNodes.Length - 1; j++)
			{
				Vector3 direction = airPathNodes[j + 1].position - airPathNodes[j].position;
				if (!Physics.SphereCast(airPathNodes[j].position, 5f, direction, out hitInfo, direction.magnitude, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					continue;
				}
				flag = false;
				for (int k = 0; k < StartOfRound.Instance.naturalSurfaceTags.Length; k++)
				{
					if (hitInfo.collider.CompareTag(StartOfRound.Instance.naturalSurfaceTags[k]) || (StartOfRound.Instance.currentLevel.levelID == 12 && hitInfo.collider.CompareTag("Rock")))
					{
						flag = true;
					}
				}
				if (!flag)
				{
					break;
				}
			}
			if (!flag)
			{
				num += 60f;
				base.transform.eulerAngles = new Vector3(0f, num, 0f);
			}
			else if (Physics.Raycast(endingPosition.position + Vector3.up * 50f, Vector3.down, out hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				endOfFlightPathPosition = RoundManager.Instance.GetNavMeshPosition(hitInfo.point, navHit, 8f, agent.areaMask);
				if (!RoundManager.Instance.GotNavMeshPositionResult)
				{
					endOfFlightPathPosition = RoundManager.Instance.GetClosestNode(hitInfo.point).position;
				}
				break;
			}
		}
		if (!flag)
		{
			inSpecialAnimation = false;
			agent.enabled = true;
			inEmergingState = false;
		}
		else
		{
			EmergeServerRpc((int)num);
		}
	}

	[ServerRpc]
	public void EmergeServerRpc(int yRot)
{		{
			EmergeClientRpc(yRot);
		}
}
	[ClientRpc]
	public void EmergeClientRpc(int yRot)
{		{
			inSpecialAnimation = true;
			inEmergingState = true;
			hitGroundInAnimation = false;
			agent.enabled = false;
			base.transform.position = serverPosition;
			base.transform.eulerAngles = new Vector3(0f, yRot, 0f);
			timesEmerging++;
			creatureSFX.Stop();
			if (emergingFromGroundCoroutine != null)
			{
				StopCoroutine(emergingFromGroundCoroutine);
			}
			emergingFromGroundCoroutine = StartCoroutine(EmergeFromGround(yRot));
		}
}
	private IEnumerator EmergeFromGround(int rot)
	{
		RoundManager.PlayRandomClip(creatureSFX, groundRumbleSFX);
		emergeFromGroundParticle1.Play(withChildren: true);
		yield return new WaitForSeconds((float)sandWormRandom.Next(3, 7) / 3f);
		creatureAnimator.SetBool("emerge", value: true);
		inEmergingState = false;
		emerged = true;
		yield return new WaitForSeconds(0.1f);
		creatureSFX.PlayOneShot(emergeFromGroundSFX);
		emergeFromGroundParticle2.Play();
		ShakePlayerCameraInProximity(base.transform.position);
		yield return new WaitForSeconds((float)sandWormRandom.Next(2, 5) / 3f);
		creatureVoice.PlayOneShot(roarSFX[sandWormRandom.Next(0, roarSFX.Length)]);
		Debug.Log("Playing sandworm roar!");
		yield return new WaitUntil(() => hitGroundInAnimation);
		hitGroundParticle.Play(withChildren: true);
		groundAudio.PlayOneShot(hitGroundSFX);
		ShakePlayerCameraInProximity(groundAudio.transform.position);
		yield return new WaitForSeconds(10f);
		SetInGround();
	}

	private void ShakePlayerCameraInProximity(Vector3 pos)
	{
		if (!GameNetworkManager.Instance.localPlayerController.isInsideFactory)
		{
			float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, pos);
			if (num < 27f)
			{
				Debug.Log("Shaking camera strong");
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
			else if (num < 50f)
			{
				Debug.Log("Shaking camera strong");
				HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
			}
			else if (num < 90f)
			{
				Debug.Log("Shaking camera long");
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			}
			else if (num < 120f)
			{
				Debug.Log("Shaking camera small");
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			}
		}
	}

	public void HitGroundInAnimation()
	{
		hitGroundInAnimation = true;
	}

	public void SetInGround()
	{
		base.transform.position = endOfFlightPathPosition;
		inSpecialAnimation = false;
		emerged = false;
		inEmergingState = false;
		creatureAnimator.SetBool("emerge", value: false);
		if (base.IsOwner)
		{
			agent.enabled = true;
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!isEnemyDead && emerged)
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && component.inAnimationWithEnemy == null && component == GameNetworkManager.Instance.localPlayerController)
			{
				EatPlayer(component);
			}
		}
	}

	public void EatPlayer(PlayerControllerB playerScript)
	{
		if (playerScript.inSpecialInteractAnimation && playerScript.currentTriggerInAnimationWith != null)
		{
			playerScript.currentTriggerInAnimationWith.CancelAnimationExternally();
		}
		playerScript.inAnimationWithEnemy = null;
		playerScript.inSpecialInteractAnimation = false;
		Debug.Log("KILL player called");
		playerScript.KillPlayer(Vector3.zero, spawnBody: false);
	}

	public override void OnCollideWithEnemy(Collider other, EnemyAI enemyScript = null)
	{
		base.OnCollideWithEnemy(other);
		if (base.IsServer && emerged)
		{
			enemyScript.KillEnemyOnOwnerClient(overrideDestroy: true);
		}
	}
}
