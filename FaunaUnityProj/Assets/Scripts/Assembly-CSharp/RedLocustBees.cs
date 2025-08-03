using System;
using System.Collections;
using DigitalRuby.ThunderAndLightning;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;

public class RedLocustBees : EnemyAI
{
	public int defenseDistance;

	[Space(5f)]
	public GameObject hivePrefab;

	public GrabbableObject hive;

	public Vector3 lastKnownHivePosition;

	private int previousState = -1;

	public VisualEffect beeParticles;

	public Transform beeParticlesTarget;

	public AudioSource beesIdle;

	public AudioSource beesDefensive;

	public AudioSource beesAngry;

	public AISearchRoutine searchForHive;

	private int chasePriority;

	private Vector3 lastSeenPlayerPos;

	private float lostLOSTimer;

	private bool wasInChase;

	private bool hasFoundHiveAfterChasing;

	private bool hasSpawnedHive;

	private float beesZapCurrentTimer;

	private float beesZapTimer;

	public LightningBoltPathScript lightningComponent;

	public Transform[] lightningPoints;

	private int beesZappingMode;

	private int timesChangingZapModes;

	private System.Random beeZapRandom;

	public AudioSource beeZapAudio;

	private float timeSinceHittingPlayer;

	private float attackZapModeTimer;

	private bool overrideBeeParticleTarget;

	private int beeParticleState = -1;

	private PlayerControllerB killingPlayer;

	private Coroutine killingPlayerCoroutine;

	private bool syncedLastKnownHivePosition;

	public override void Start()
	{
		base.Start();
		if (base.IsServer)
		{
			SpawnHiveNearEnemy();
			syncedLastKnownHivePosition = true;
		}
	}

	private void SpawnHiveNearEnemy()
	{
		if (base.IsServer)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 1314 + enemyType.numberSpawned);
			Vector3 randomNavMeshPositionInBoxPredictable = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(base.transform.position, 10f, RoundManager.Instance.navHit, random, -5);
			GameObject gameObject = UnityEngine.Object.Instantiate(hivePrefab, randomNavMeshPositionInBoxPredictable + Vector3.up * 0.2f, Quaternion.Euler(Vector3.zero), RoundManager.Instance.spawnedScrapContainer);
			gameObject.SetActive(value: true);
			gameObject.GetComponent<NetworkObject>().Spawn();
			SpawnHiveClientRpc(hiveScrapValue: (!(Vector3.Distance(randomNavMeshPositionInBoxPredictable, StartOfRound.Instance.shipLandingPosition.transform.position) < 40f)) ? random.Next(50, 150) : random.Next(40, 100), hiveObject: gameObject.GetComponent<NetworkObject>(), hivePosition: randomNavMeshPositionInBoxPredictable + Vector3.up * 0.2f);
		}
	}

	[ClientRpc]
	public void SpawnHiveClientRpc(NetworkObjectReference hiveObject, int hiveScrapValue, Vector3 hivePosition)
{		if (hiveObject.TryGet(out var networkObject))
		{
			hive = networkObject.gameObject.GetComponent<GrabbableObject>();
			hive.scrapValue = hiveScrapValue;
			ScanNodeProperties componentInChildren = hive.GetComponentInChildren<ScanNodeProperties>();
			if (componentInChildren != null)
			{
				componentInChildren.scrapValue = hiveScrapValue;
				componentInChildren.headerText = "Bee hive";
				componentInChildren.subText = $"VALUE: ${hiveScrapValue}";
			}
			hive.targetFloorPosition = hivePosition;
			Debug.Log($"Set targetfloorposition of hive: {hivePosition}");
			if (Physics.Raycast(RoundManager.Instance.GetNavMeshPosition(hive.transform.position), hive.transform.position + Vector3.up - eye.position, out var hitInfo, 20f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				lastKnownHivePosition = hitInfo.point;
			}
			else
			{
				lastKnownHivePosition = hive.transform.position;
			}
			RoundManager.Instance.totalScrapValueInLevel += hive.scrapValue;
			hasSpawnedHive = true;
		}
		else
		{
			Debug.LogError("Bees: Error! Hive could not be accessed from network object reference");
		}
}
	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.allPlayersDead || !hasSpawnedHive || daytimeEnemyLeaving)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (wasInChase)
			{
				wasInChase = false;
			}
			if (Vector3.Distance(base.transform.position, lastKnownHivePosition) > 2f)
			{
				SetDestinationToPosition(lastKnownHivePosition);
			}
			if (IsHiveMissing())
			{
				SwitchToBehaviourState(2);
				break;
			}
			PlayerControllerB playerControllerB3 = CheckLineOfSightForPlayer(360f, 16, 1);
			if (playerControllerB3 != null && Vector3.Distance(playerControllerB3.transform.position, hive.transform.position) < (float)defenseDistance)
			{
				SetMovingTowardsTargetPlayer(playerControllerB3);
				SwitchToBehaviourState(1);
				SwitchOwnershipOfBeesToClient(playerControllerB3);
			}
			break;
		}
		case 1:
			if (targetPlayer == null || !PlayerIsTargetable(targetPlayer) || Vector3.Distance(targetPlayer.transform.position, hive.transform.position) > (float)defenseDistance + 5f)
			{
				targetPlayer = null;
				wasInChase = false;
				if (IsHiveMissing())
				{
					SwitchToBehaviourState(2);
				}
				else
				{
					SwitchToBehaviourState(0);
				}
			}
			else if (targetPlayer.currentlyHeldObjectServer == hive)
			{
				SwitchToBehaviourState(2);
			}
			break;
		case 2:
		{
			if (IsHivePlacedAndInLOS())
			{
				if (wasInChase)
				{
					wasInChase = false;
				}
				lastKnownHivePosition = hive.transform.position + Vector3.up * 0.5f;
				Collider[] array = Physics.OverlapSphere(hive.transform.position, defenseDistance, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Collide);
				PlayerControllerB playerControllerB = null;
				if (array != null && array.Length != 0)
				{
					for (int i = 0; i < array.Length; i++)
					{
						playerControllerB = array[0].gameObject.GetComponent<PlayerControllerB>();
						if (playerControllerB != null)
						{
							break;
						}
					}
				}
				if (playerControllerB != null && Vector3.Distance(playerControllerB.transform.position, hive.transform.position) < (float)defenseDistance)
				{
					SetMovingTowardsTargetPlayer(playerControllerB);
					SwitchToBehaviourState(1);
					SwitchOwnershipOfBeesToClient(playerControllerB);
				}
				else
				{
					SwitchToBehaviourState(0);
				}
				break;
			}
			bool flag = false;
			PlayerControllerB playerControllerB2 = ChaseWithPriorities();
			if (playerControllerB2 != null && targetPlayer != playerControllerB2)
			{
				flag = true;
				wasInChase = false;
				SetMovingTowardsTargetPlayer(playerControllerB2);
				StopSearch(searchForHive);
				if (SwitchOwnershipOfBeesToClient(playerControllerB2))
				{
					Debug.Log("Bee10 switching owner to " + playerControllerB2.playerUsername);
					break;
				}
			}
			if (targetPlayer != null)
			{
				agent.acceleration = 16f;
				if ((!flag && !CheckLineOfSightForPlayer(360f, 16, 2)) || !PlayerIsTargetable(targetPlayer))
				{
					lostLOSTimer += AIIntervalTime;
					if (lostLOSTimer >= 4.5f)
					{
						targetPlayer = null;
						lostLOSTimer = 0f;
					}
				}
				else
				{
					wasInChase = true;
					lastSeenPlayerPos = targetPlayer.transform.position;
					lostLOSTimer = 0f;
				}
				break;
			}
			agent.acceleration = 13f;
			if (!searchForHive.inProgress)
			{
				if (wasInChase)
				{
					StartSearch(lastSeenPlayerPos, searchForHive);
				}
				else
				{
					StartSearch(base.transform.position, searchForHive);
				}
			}
			break;
		}
		}
	}

	private bool SwitchOwnershipOfBeesToClient(PlayerControllerB player)
	{
		if (player != GameNetworkManager.Instance.localPlayerController)
		{
			syncedLastKnownHivePosition = false;
			lostLOSTimer = 0f;
			SyncLastKnownHivePositionServerRpc(lastKnownHivePosition);
			ChangeOwnershipOfEnemy(player.actualClientId);
			return true;
		}
		return false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void SyncLastKnownHivePositionServerRpc(Vector3 hivePosition)
			{
				SyncLastKnownHivePositionClientRpc(hivePosition);
			}

	[ClientRpc]
	public void SyncLastKnownHivePositionClientRpc(Vector3 hivePosition)
			{
				lastKnownHivePosition = hivePosition;
				syncedLastKnownHivePosition = true;
			}

	private PlayerControllerB ChaseWithPriorities()
	{
		PlayerControllerB[] allPlayersInLineOfSight = GetAllPlayersInLineOfSight(360f, 16);
		PlayerControllerB playerControllerB = null;
		if (allPlayersInLineOfSight != null)
		{
			float num = 3000f;
			int num2 = 0;
			int num3 = -1;
			for (int i = 0; i < allPlayersInLineOfSight.Length; i++)
			{
				if (allPlayersInLineOfSight[i].currentlyHeldObjectServer != null)
				{
					if (num3 == -1 && allPlayersInLineOfSight[i].currentlyHeldObjectServer.itemProperties.itemId == 1531)
					{
						num3 = i;
						continue;
					}
					if (allPlayersInLineOfSight[i].currentlyHeldObjectServer == hive)
					{
						return allPlayersInLineOfSight[i];
					}
				}
				if (targetPlayer == null)
				{
					float num4 = Vector3.Distance(base.transform.position, allPlayersInLineOfSight[i].transform.position);
					if (num4 < num)
					{
						num = num4;
						num2 = i;
					}
				}
			}
			if (num3 != -1 && Vector3.Distance(base.transform.position, allPlayersInLineOfSight[num3].transform.position) - num > 7f)
			{
				playerControllerB = allPlayersInLineOfSight[num2];
			}
			else if (playerControllerB == null)
			{
				return allPlayersInLineOfSight[num2];
			}
		}
		return playerControllerB;
	}

	private bool IsHiveMissing()
	{
		float num = Vector3.Distance(eye.position, lastKnownHivePosition);
		if (!syncedLastKnownHivePosition)
		{
			return false;
		}
		if (num < 4f || (num < 8f && !Physics.Linecast(eye.position, lastKnownHivePosition, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)))
		{
			if ((Vector3.Distance(hive.transform.position, lastKnownHivePosition) > 6f && !IsHivePlacedAndInLOS()) || hive.isHeld)
			{
				return true;
			}
			lastKnownHivePosition = hive.transform.position + Vector3.up * 0.5f;
			return false;
		}
		return false;
	}

	private bool IsHivePlacedAndInLOS()
	{
		if (hive.isHeld)
		{
			return false;
		}
		if (Vector3.Distance(eye.position, hive.transform.position) > 9f || Physics.Linecast(eye.position, hive.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			return false;
		}
		return true;
	}

	public override void Update()
	{
		base.Update();
		if (StartOfRound.Instance.allPlayersDead || daytimeEnemyLeaving)
		{
			return;
		}
		timeSinceHittingPlayer += Time.deltaTime;
		attackZapModeTimer += Time.deltaTime;
		float num = Time.deltaTime * 0.7f;
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousState != currentBehaviourStateIndex)
			{
				previousState = currentBehaviourStateIndex;
				SetBeeParticleMode(0);
				ResetBeeZapTimer();
			}
			if (attackZapModeTimer > 1f)
			{
				beesZappingMode = 0;
				ResetBeeZapTimer();
			}
			agent.speed = 4f;
			agent.acceleration = 13f;
			if (!overrideBeeParticleTarget)
			{
				float num2 = Vector3.Distance(base.transform.position, hive.transform.position);
				if (hive != null && (num2 < 2f || (num2 < 5f && !Physics.Linecast(eye.position, hive.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))))
				{
					beeParticlesTarget.position = hive.transform.position;
				}
				else
				{
					beeParticlesTarget.position = base.transform.position + Vector3.up * 1.5f;
				}
			}
			beesIdle.volume = Mathf.Min(beesIdle.volume + num, 1f);
			if (!beesIdle.isPlaying)
			{
				beesIdle.Play();
			}
			beesDefensive.volume = Mathf.Max(beesDefensive.volume - num, 0f);
			if (beesDefensive.isPlaying && beesDefensive.volume <= 0f)
			{
				beesDefensive.Stop();
			}
			beesAngry.volume = Mathf.Max(beesAngry.volume - num, 0f);
			if (beesAngry.isPlaying && beesAngry.volume <= 0f)
			{
				beesAngry.Stop();
			}
			break;
		case 1:
			if (previousState != currentBehaviourStateIndex)
			{
				previousState = currentBehaviourStateIndex;
				ResetBeeZapTimer();
				SetBeeParticleMode(1);
				if (!overrideBeeParticleTarget)
				{
					beeParticlesTarget.position = base.transform.position + Vector3.up * 1.5f;
				}
			}
			if (attackZapModeTimer > 3f)
			{
				beesZappingMode = 1;
				ResetBeeZapTimer();
			}
			agent.speed = 6f;
			agent.acceleration = 13f;
			beesIdle.volume = Mathf.Max(beesIdle.volume - num, 0f);
			if (beesIdle.isPlaying && beesIdle.volume <= 0f)
			{
				beesIdle.Stop();
			}
			beesDefensive.volume = Mathf.Min(beesDefensive.volume + num, 1f);
			if (!beesDefensive.isPlaying)
			{
				beesDefensive.Play();
			}
			beesAngry.volume = Mathf.Max(beesAngry.volume - num, 0f);
			if (beesAngry.isPlaying && beesAngry.volume <= 0f)
			{
				beesAngry.Stop();
			}
			break;
		case 2:
			if (previousState != currentBehaviourStateIndex)
			{
				previousState = currentBehaviourStateIndex;
				SetBeeParticleMode(2);
				ResetBeeZapTimer();
				if (!overrideBeeParticleTarget)
				{
					beeParticlesTarget.position = base.transform.position + Vector3.up * 1.5f;
				}
			}
			beesZappingMode = 2;
			agent.speed = 10.3f;
			beesIdle.volume = Mathf.Max(beesIdle.volume - num, 0f);
			if (beesIdle.isPlaying && beesIdle.volume <= 0f)
			{
				beesIdle.Stop();
			}
			beesDefensive.volume = Mathf.Max(beesDefensive.volume - num, 0f);
			if (beesDefensive.isPlaying && beesDefensive.volume <= 0f)
			{
				beesDefensive.Stop();
			}
			beesAngry.volume = Mathf.Min(beesAngry.volume + num, 1f);
			if (!beesAngry.isPlaying)
			{
				beesAngry.Play();
			}
			break;
		}
		BeesZapOnTimer();
		if (stunNormalizedTimer > 0f || overrideBeeParticleTarget)
		{
			SetBeeParticleMode(2);
			agent.speed = 0f;
		}
	}

	private void ResetBeeZapTimer()
	{
		timesChangingZapModes++;
		beeZapRandom = new System.Random(StartOfRound.Instance.randomMapSeed + timesChangingZapModes);
		beesZapCurrentTimer = 0f;
		attackZapModeTimer = 0f;
		beeZapAudio.Stop();
	}

	private void BeesZapOnTimer()
	{
		if (beesZappingMode == 0)
		{
			return;
		}
		if (beesZapCurrentTimer > beesZapTimer)
		{
			beesZapCurrentTimer = 0f;
			switch (beesZappingMode)
			{
			case 1:
				beesZapTimer = (float)beeZapRandom.Next(1, 8) * 0.1f;
				break;
			case 2:
				beesZapTimer = (float)beeZapRandom.Next(1, 7) * 0.06f;
				break;
			case 3:
				beesZapTimer = (float)beeZapRandom.Next(1, 5) * 0.04f;
				if (!beeZapAudio.isPlaying)
				{
					beeZapAudio.Play();
				}
				beeZapAudio.pitch = 1f;
				if (attackZapModeTimer > 3f)
				{
					attackZapModeTimer = 0f;
					GetClosestPlayer();
					if (mostOptimalDistance > 3f)
					{
						beesZappingMode = currentBehaviourStateIndex;
						Debug.Log($"Setting bee zap mode to {currentBehaviourState} at end of zapping mode 3");
						beeZapAudio.Stop();
					}
				}
				break;
			}
			BeesZap();
		}
		else
		{
			beesZapCurrentTimer += Time.deltaTime;
		}
	}

	private void SetBeeParticleMode(int newState)
	{
		if (beeParticleState != newState)
		{
			beeParticleState = newState;
			switch (newState)
			{
			case 0:
				beeParticles.SetFloat("NoiseIntensity", 3f);
				beeParticles.SetFloat("NoiseFrequency", 35f);
				beeParticles.SetFloat("MoveToTargetSpeed", 155f);
				beeParticles.SetFloat("MoveToTargetForce", 155f);
				beeParticles.SetFloat("TargetRadius", 0.3f);
				beeParticles.SetFloat("TargetStickiness", 7f);
				break;
			case 1:
				beeParticles.SetFloat("NoiseIntensity", 16f);
				beeParticles.SetFloat("NoiseFrequency", 20f);
				beeParticles.SetFloat("MoveToTargetSpeed", 13f);
				beeParticles.SetFloat("MoveToTargetForce", 13f);
				beeParticles.SetFloat("TargetRadius", 1f);
				beeParticles.SetFloat("TargetStickiness", 0f);
				break;
			case 2:
				beeParticles.SetFloat("NoiseIntensity", 35f);
				beeParticles.SetFloat("NoiseFrequency", 35f);
				beeParticles.SetFloat("MoveToTargetSpeed", 35f);
				beeParticles.SetFloat("MoveToTargetForce", 35f);
				beeParticles.SetFloat("TargetRadius", 1f);
				beeParticles.SetFloat("TargetStickiness", 0f);
				break;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void EnterAttackZapModeServerRpc(int clientWhoSent)
{if(beesZappingMode != 3)			{
				EnterAttackZapModeClientRpc(clientWhoSent);
			}
}
	[ClientRpc]
	public void EnterAttackZapModeClientRpc(int clientWhoSent)
{if((int)GameNetworkManager.Instance.localPlayerController.playerClientId != clientWhoSent)			{
				beesZappingMode = 3;
				Debug.Log("Entered zap mode 3");
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void BeeKillPlayerServerRpc(int playerId)
			{
				BeeKillPlayerClientRpc(playerId);
			}

	[ClientRpc]
	public void BeeKillPlayerClientRpc(int playerId)
			{
				BeeKillPlayerOnLocalClient(playerId);
			}

	private void BeeKillPlayerOnLocalClient(int playerId)
	{
		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
		playerControllerB.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Electrocution, 3);
		if (killingPlayerCoroutine != null)
		{
			StopCoroutine(killingPlayerCoroutine);
		}
		killingPlayerCoroutine = StartCoroutine(BeesKillPlayer(playerControllerB));
	}

	private IEnumerator BeesKillPlayer(PlayerControllerB killedPlayer)
	{
		float timeAtStart = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => killedPlayer.deadBody != null || Time.realtimeSinceStartup - timeAtStart > 3f);
		if (!(killedPlayer.deadBody == null))
		{
			killingPlayer = killedPlayer;
			overrideBeeParticleTarget = true;
			inSpecialAnimation = true;
			Debug.Log("Bees on body");
			beeParticlesTarget.position = killedPlayer.deadBody.bodyParts[0].transform.position;
			yield return new WaitForSeconds(4f);
			overrideBeeParticleTarget = false;
			beeParticlesTarget.position = base.transform.position + Vector3.up * 1.5f;
			inSpecialAnimation = false;
			killingPlayer = null;
		}
	}

	private void OnPlayerTeleported(PlayerControllerB playerTeleported)
	{
		if (playerTeleported == targetPlayer)
		{
			targetPlayer = null;
		}
		if (playerTeleported == killingPlayer && killingPlayerCoroutine != null)
		{
			StopCoroutine(killingPlayerCoroutine);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (timeSinceHittingPlayer < 0.4f)
		{
			return;
		}
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
		if (playerControllerB != null)
		{
			timeSinceHittingPlayer = 0f;
			if (playerControllerB.health <= 10 || playerControllerB.criticallyInjured)
			{
				BeeKillPlayerOnLocalClient((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
				BeeKillPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
			else
			{
				playerControllerB.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Electrocution, 3);
			}
			if (beesZappingMode != 3)
			{
				beesZappingMode = 3;
				EnterAttackZapModeServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	public void BeesZap()
	{
		if (beeParticles.GetBool("Alive"))
		{
			for (int i = 0; i < lightningPoints.Length; i++)
			{
				lightningPoints[i].position = RoundManager.Instance.GetRandomPositionInBoxPredictable(beeParticlesTarget.position, 4f, beeZapRandom);
			}
			lightningComponent.Trigger(0.1f);
		}
		if (beesZappingMode != 3)
		{
			beeZapAudio.pitch = UnityEngine.Random.Range(0.8f, 1.1f);
			beeZapAudio.PlayOneShot(enemyType.audioClips[UnityEngine.Random.Range(0, enemyType.audioClips.Length)], UnityEngine.Random.Range(0.6f, 1f));
		}
	}

	public void OnEnable()
	{
		lightningComponent.Camera = StartOfRound.Instance.activeCamera;
		StartOfRound.Instance.playerTeleportedEvent.AddListener(OnPlayerTeleported);
		StartOfRound.Instance.CameraSwitchEvent.AddListener(OnCameraSwitch);
	}

	public void OnDisable()
	{
		StartOfRound.Instance.playerTeleportedEvent.RemoveListener(OnPlayerTeleported);
		StartOfRound.Instance.CameraSwitchEvent.RemoveListener(OnCameraSwitch);
	}

	private void OnCameraSwitch()
	{
		lightningComponent.Camera = StartOfRound.Instance.activeCamera;
	}

	public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false)
	{
		base.EnableEnemyMesh(enable, overrideDoNotSet);
		beeParticles.SetBool("Alive", enable);
	}

	public override void DaytimeEnemyLeave()
	{
		base.DaytimeEnemyLeave();
		beeParticles.SetFloat("MoveToTargetForce", -15f);
		creatureSFX.PlayOneShot(enemyType.audioClips[0], 0.5f);
		agent.speed = 0f;
		StartCoroutine(bugsLeave());
	}

	private IEnumerator bugsLeave()
	{
		yield return new WaitForSeconds(6f);
		KillEnemyOnOwnerClient(overrideDestroy: true);
	}
}
