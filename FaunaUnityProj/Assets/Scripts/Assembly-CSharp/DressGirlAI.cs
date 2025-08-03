using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class DressGirlAI : EnemyAI
{
	public PlayerControllerB hauntingPlayer;

	public bool hauntingLocalPlayer;

	public float timer;

	public float hauntInterval;

	private bool couldNotStareLastAttempt;

	public float staringTimer;

	public bool staringInHaunt;

	private int timesSeenByPlayer;

	private int timesStared;

	private bool seenByPlayerThisTime;

	private bool playerApproachedThisTime;

	public bool disappearingFromStare;

	private bool disappearByVanishing;

	private bool choseDisappearingPosition;

	private int timesChased;

	private float chaseTimer;

	public GameObject[] outsideNodes;

	public NavMeshHit navHit;

	private Coroutine disappearOnDelayCoroutine;

	public Transform turnCompass;

	public AudioClip[] appearStaringSFX;

	public AudioClip skipWalkSFX;

	public AudioClip breathingSFX;

	public float SFXVolumeLerpTo = 1f;

	public AudioSource heartbeatMusic;

	private bool enemyMeshEnabled;

	private System.Random ghostGirlRandom;

	private bool initializedRandomSeed;

	private bool switchedHauntingPlayer;

	private Coroutine switchHauntedPlayerCoroutine;

	private int timesChoosingAPlayer;

	public override void Start()
	{
		base.Start();
		if (!RoundManager.Instance.hasInitializedLevelRandomSeed)
		{
			RoundManager.Instance.InitializeRandomNumberGenerators();
		}
		outsideNodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
		ChoosePlayerToHaunt();
		EnableEnemyMesh(enable: false, overrideDoNotSet: true);
		enemyMeshEnabled = false;
		Debug.Log("DISABLING ENEMY MESH!!!!!!!!!!!");
		navHit = default(NavMeshHit);
	}

	private void ChoosePlayerToHaunt()
	{
		timesChoosingAPlayer++;
		if (timesChoosingAPlayer > 1)
		{
			timer = hauntInterval - 1f;
		}
		SFXVolumeLerpTo = 0f;
		creatureVoice.Stop();
		heartbeatMusic.volume = 0f;
		if (!initializedRandomSeed)
		{
			ghostGirlRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 158);
		}
		float num = 0f;
		float num2 = 0f;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < 4; i++)
		{
			if (StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount > num3)
			{
				num3 = StartOfRound.Instance.gameStats.allPlayerStats[i].turnAmount;
				num4 = i;
			}
			if (StartOfRound.Instance.allPlayerScripts[i].insanityLevel > num)
			{
				num = StartOfRound.Instance.allPlayerScripts[i].insanityLevel;
				num2 = i;
			}
		}
		int[] array = new int[4];
		for (int j = 0; j < 4; j++)
		{
			if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled)
			{
				array[j] = 0;
				continue;
			}
			array[j] += 80;
			if (num2 == (float)j && num > 1f)
			{
				array[j] += 50;
			}
			if (num4 == j)
			{
				array[j] += 30;
			}
			if (!StartOfRound.Instance.allPlayerScripts[j].hasBeenCriticallyInjured)
			{
				array[j] += 10;
			}
			if (StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer != null && StartOfRound.Instance.allPlayerScripts[j].currentlyHeldObjectServer.scrapValue > 150)
			{
				array[j] += 30;
			}
		}
		hauntingPlayer = StartOfRound.Instance.allPlayerScripts[RoundManager.Instance.GetRandomWeightedIndex(array, ghostGirlRandom)];
		if (hauntingPlayer.isPlayerDead)
		{
			for (int k = 0; k < StartOfRound.Instance.allPlayerScripts.Length; k++)
			{
				if (!StartOfRound.Instance.allPlayerScripts[k].isPlayerDead)
				{
					hauntingPlayer = StartOfRound.Instance.allPlayerScripts[k];
					break;
				}
			}
		}
		Debug.Log($"Little girl: Haunting player with playerClientId: {hauntingPlayer.playerClientId}; actualClientId: {hauntingPlayer.actualClientId}");
		ChangeOwnershipOfEnemy(hauntingPlayer.actualClientId);
		hauntingLocalPlayer = GameNetworkManager.Instance.localPlayerController == hauntingPlayer;
		if (switchHauntedPlayerCoroutine != null)
		{
			StopCoroutine(switchHauntedPlayerCoroutine);
		}
		switchHauntedPlayerCoroutine = StartCoroutine(setSwitchingHauntingPlayer());
	}

	private IEnumerator setSwitchingHauntingPlayer()
	{
		yield return new WaitForSeconds(10f);
		switchedHauntingPlayer = false;
	}

	[ClientRpc]
	private void ChooseNewHauntingPlayerClientRpc()
			{
				ChoosePlayerToHaunt();
			}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (!isEnemyDead)
		{
			_ = StartOfRound.Instance.allPlayersDead;
		}
	}

	public override void Update()
	{
		base.Update();
		if (base.IsServer && !hauntingPlayer.isPlayerControlled)
		{
			if (!switchedHauntingPlayer)
			{
				switchedHauntingPlayer = true;
				ChooseNewHauntingPlayerClientRpc();
			}
		}
		else if (!base.IsOwner)
		{
			if (enemyMeshEnabled)
			{
				enemyMeshEnabled = false;
				EnableEnemyMesh(enable: false, overrideDoNotSet: true);
			}
		}
		else if (GameNetworkManager.Instance.localPlayerController != hauntingPlayer)
		{
			ChangeOwnershipOfEnemy(hauntingPlayer.actualClientId);
		}
		else
		{
			if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
			{
				return;
			}
			creatureSFX.volume = Mathf.Lerp(creatureSFX.volume, SFXVolumeLerpTo, 5f * Time.deltaTime);
			if (creatureSFX.volume <= 0.01f && SFXVolumeLerpTo == 0f && creatureSFX.isPlaying)
			{
				creatureSFX.Stop();
			}
			switch (currentBehaviourStateIndex)
			{
			case 0:
				if (!staringInHaunt)
				{
					SoundManager.Instance.SetDiageticMixerSnapshot();
					heartbeatMusic.volume = Mathf.Lerp(heartbeatMusic.volume, 0f, 4f * Time.deltaTime);
					float num = hauntInterval;
					if (couldNotStareLastAttempt)
					{
						num = 4f;
					}
					if (timer > num)
					{
						timer = 0f;
						TryFindingHauntPosition();
					}
					else
					{
						timer += Time.deltaTime;
					}
					break;
				}
				if (disappearingFromStare)
				{
					if (!choseDisappearingPosition)
					{
						choseDisappearingPosition = true;
						SetDestinationToPosition(FindPositionOutOfLOS());
						agent.speed = 5.25f;
						creatureAnimator.SetBool("Walk", value: true);
						creatureVoice.Stop();
					}
					else if (disappearOnDelayCoroutine == null)
					{
						if (disappearByVanishing)
						{
							RoundManager.Instance.FlickerLights(flickerFlashlights: true, disableFlashlights: true);
							MessWithLightsServerRpc();
							disappearOnDelayCoroutine = StartCoroutine(disappearOnDelay());
						}
						else if (Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, base.transform.position + Vector3.up * 0.4f, StartOfRound.Instance.collidersAndRoomMask))
						{
							DisappearDuringHaunt();
						}
						else if (Vector3.Distance(base.transform.position, destination) < 0.2f || Vector3.Distance(base.transform.position, hauntingPlayer.transform.position) < 4f)
						{
							disappearOnDelayCoroutine = StartCoroutine(disappearOnDelay());
						}
					}
					break;
				}
				turnCompass.LookAt(hauntingPlayer.transform);
				base.transform.eulerAngles = new Vector3(base.transform.eulerAngles.x, turnCompass.eulerAngles.y, base.transform.eulerAngles.z);
				creatureAnimator.SetBool("Walk", value: false);
				if (timer > staringTimer)
				{
					timer = 0f;
					disappearingFromStare = true;
				}
				else if (!Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, base.transform.position + Vector3.up * 0.4f, StartOfRound.Instance.collidersAndRoomMask))
				{
					if (hauntingPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.4f, 60f, 100, 5f))
					{
						SoundManager.Instance.SetDiageticMixerSnapshot(1);
						heartbeatMusic.volume = Mathf.Lerp(heartbeatMusic.volume, 1f, 3f * Time.deltaTime);
						timer += Time.deltaTime * 1.25f;
						if (!seenByPlayerThisTime)
						{
							seenByPlayerThisTime = true;
							timesSeenByPlayer++;
							float num2 = UnityEngine.Random.Range(3, 5);
							if (((float)timesSeenByPlayer >= num2 || timesStared - timesSeenByPlayer > 2) && UnityEngine.Random.Range(0, 100) < 85)
							{
								BeginChasing();
							}
						}
					}
					else
					{
						SoundManager.Instance.SetDiageticMixerSnapshot();
						heartbeatMusic.volume = Mathf.Lerp(heartbeatMusic.volume, 0f, 3f * Time.deltaTime);
						timer += Time.deltaTime;
					}
					float num3 = Vector3.Distance(hauntingPlayer.gameplayCamera.transform.position, base.transform.position);
					if (!(num3 < 7f))
					{
						break;
					}
					if (!playerApproachedThisTime && UnityEngine.Random.Range(0, 100) < 25 && timesSeenByPlayer <= 1)
					{
						disappearingFromStare = true;
					}
					else if (num3 < 5f)
					{
						if (UnityEngine.Random.Range(0, 100) > 35 && timesSeenByPlayer >= 2)
						{
							BeginChasing();
						}
						else
						{
							disappearingFromStare = true;
							disappearByVanishing = true;
						}
					}
					playerApproachedThisTime = true;
				}
				else
				{
					timer += Time.deltaTime * 3f;
				}
				break;
			case 1:
				if (chaseTimer <= 0f || Vector3.Distance(base.transform.position, hauntingPlayer.transform.position) > 50f)
				{
					StopChasing();
				}
				else
				{
					chaseTimer -= Time.deltaTime;
				}
				if (timer >= 5f)
				{
					TryTeleportingAroundPlayer();
					timer = 0f;
				}
				else
				{
					timer += Time.deltaTime;
				}
				break;
			}
			if (!isEnemyDead)
			{
				_ = StartOfRound.Instance.allPlayersDead;
			}
		}
	}

	[ServerRpc]
	private void MessWithLightsServerRpc()
{		{
			MessWithLightsClientRpc();
		}
}
	[ClientRpc]
	private void MessWithLightsClientRpc()
{if(!base.IsOwner)		{
			RoundManager.Instance.FlickerLights(flickerFlashlights: true, disableFlashlights: true);
			if (timesSeenByPlayer > 0)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.2f);
			}
		}
}
	[ServerRpc]
	private void FlipLightsBreakerServerRpc()
{		{
			MessWithLightsClientRpc();
		}
}
	[ClientRpc]
	private void FlipLightsBreakerClientRpc()
{		{
			BreakerBox breakerBox = UnityEngine.Object.FindObjectOfType<BreakerBox>();
			if (breakerBox != null)
			{
				breakerBox.SetSwitchesOff();
				RoundManager.Instance.TurnOnAllLights(on: false);
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.2f);
			}
		}
}
	private void BeginChasing()
	{
		if (currentBehaviourStateIndex != 1)
		{
			SwitchToBehaviourStateOnLocalClient(1);
			staringInHaunt = false;
			disappearingFromStare = false;
			disappearByVanishing = false;
			choseDisappearingPosition = false;
			agent.speed = 5.25f;
			creatureAnimator.SetBool("Walk", value: true);
			timesChased++;
			if (timesChased != 1 && UnityEngine.Random.Range(0, 100) < 65)
			{
				FlipLightsBreakerServerRpc();
			}
			else
			{
				MessWithLightsServerRpc();
			}
			chaseTimer = 20f;
			timer = 0f;
			SetMovingTowardsTargetPlayer(hauntingPlayer);
			moveTowardsDestination = true;
		}
	}

	private void StopChasing()
	{
		SwitchToBehaviourStateOnLocalClient(0);
		creatureVoice.Stop();
		EnableEnemyMesh(enable: false, overrideDoNotSet: true);
		SFXVolumeLerpTo = 0f;
		timer = 0f;
		creatureAnimator.SetBool("Walk", value: false);
		moveTowardsDestination = false;
	}

	private void TryTeleportingAroundPlayer()
	{
		if (!hauntingPlayer.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.4f, 70f, 100, 10f))
		{
			Vector3 vector = TryFindingHauntPosition(staringMode: false, mustBeInLOS: false);
			if (vector != Vector3.zero)
			{
				creatureSFX.volume = 0f;
				agent.Warp(RoundManager.Instance.GetNavMeshPosition(vector, navHit));
			}
		}
	}

	private IEnumerator disappearOnDelay()
	{
		yield return new WaitForSeconds(0.1f);
		DisappearDuringHaunt();
		disappearOnDelayCoroutine = null;
	}

	private void DisappearDuringHaunt()
	{
		EnableEnemyMesh(enable: false, overrideDoNotSet: true);
		disappearingFromStare = false;
		choseDisappearingPosition = false;
		disappearByVanishing = false;
		staringInHaunt = false;
		SFXVolumeLerpTo = 0f;
	}

	private Vector3 FindPositionOutOfLOS()
	{
		Vector3 vector = base.transform.right;
		float num = Vector3.Distance(base.transform.position, hauntingPlayer.transform.position);
		for (int i = 0; i < 8; i++)
		{
			Debug.DrawRay(base.transform.position + Vector3.up * 0.4f, vector * 8f, Color.red, 1f);
			Ray ray = new Ray(base.transform.position + Vector3.up * 0.4f, vector);
			if (Physics.Raycast(ray, out var hitInfo, 8f, StartOfRound.Instance.collidersAndRoomMaskAndDefault) && Vector3.Distance(hitInfo.point, hauntingPlayer.transform.position) - num > -1f && Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, ray.GetPoint(hitInfo.distance - 0.1f), StartOfRound.Instance.collidersAndRoomMaskAndDefault))
			{
				Debug.DrawRay(base.transform.position + Vector3.up * 0.4f, vector * 8f, Color.green, 1f);
				Debug.Log("Girl: Found hide position with raycast");
				return RoundManager.Instance.GetNavMeshPosition(hitInfo.point, navHit);
			}
			vector = Quaternion.Euler(0f, 45f, 0f) * vector;
		}
		for (int j = 0; j < allAINodes.Length; j++)
		{
			if (Vector3.Distance(allAINodes[j].transform.position, base.transform.position) < 7f && Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, allAINodes[j].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
			{
				Debug.Log("Girl: Found hide position with AI nodes");
				Debug.DrawRay(allAINodes[j].transform.position, Vector3.up * 7f, Color.green, 1f);
				return RoundManager.Instance.GetNavMeshPosition(allAINodes[j].transform.position, navHit);
			}
		}
		Debug.Log("Girl: Unable to find a location to hide away; vanishing instead");
		disappearByVanishing = true;
		return base.transform.position;
	}

	private Vector3 TryFindingHauntPosition(bool staringMode = true, bool mustBeInLOS = true)
	{
		if (hauntingPlayer.isInsideFactory)
		{
			for (int i = 0; i < allAINodes.Length; i++)
			{
				if ((!mustBeInLOS || !Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) && !hauntingPlayer.HasLineOfSightToPosition(allAINodes[i].transform.position, 80f, 100, 8f))
				{
					Debug.DrawLine(hauntingPlayer.gameplayCamera.transform.position, allAINodes[i].transform.position, Color.green, 2f);
					Debug.Log($"Player distance to haunt position: {Vector3.Distance(hauntingPlayer.transform.position, allAINodes[i].transform.position)}");
					if (staringMode)
					{
						SetHauntStarePosition(allAINodes[i].transform.position);
					}
					return allAINodes[i].transform.position;
				}
			}
		}
		else if (hauntingPlayer.isInElevator)
		{
			for (int j = 0; j < outsideNodes.Length; j++)
			{
				if ((!mustBeInLOS || !Physics.Linecast(hauntingPlayer.gameplayCamera.transform.position, outsideNodes[j].transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) && !hauntingPlayer.HasLineOfSightToPosition(outsideNodes[j].transform.position, 80f, 100, 8f))
				{
					if (staringMode)
					{
						SetHauntStarePosition(outsideNodes[j].transform.position, 25f);
					}
					return outsideNodes[j].transform.position;
				}
			}
		}
		couldNotStareLastAttempt = true;
		return Vector3.zero;
	}

	private void SetHauntStarePosition(Vector3 newPosition, float timeToStare = 15f)
	{
		couldNotStareLastAttempt = false;
		Vector3 randomNavMeshPositionInRadiusSpherical = RoundManager.Instance.GetRandomNavMeshPositionInRadiusSpherical(newPosition, 1f, navHit);
		agent.Warp(randomNavMeshPositionInRadiusSpherical);
		moveTowardsDestination = false;
		destination = base.transform.position;
		agent.SetDestination(destination);
		agent.speed = 0f;
		EnableEnemyMesh(enable: true, overrideDoNotSet: true);
		enemyMeshEnabled = true;
		Debug.Log("Girl: STARTING HAUNT STARE");
		staringInHaunt = true;
		staringTimer = timeToStare;
		seenByPlayerThisTime = false;
		playerApproachedThisTime = false;
		timesStared++;
		SFXVolumeLerpTo = 1f;
		creatureSFX.volume = 1f;
		if (UnityEngine.Random.Range(0, 100) < 85)
		{
			Debug.Log("girL: Playing sound");
			RoundManager.PlayRandomClip(creatureVoice, appearStaringSFX);
		}
		creatureVoice.clip = breathingSFX;
		creatureVoice.Play();
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!hauntingLocalPlayer)
		{
			return;
		}
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation: false, overrideIsInsideFactoryCheck: true);
		if (!(playerControllerB != null))
		{
			return;
		}
		Debug.Log("Girl: collided with player");
		if (playerControllerB == hauntingPlayer)
		{
			if (staringInHaunt && currentBehaviourStateIndex == 0)
			{
				disappearByVanishing = true;
			}
			else if (currentBehaviourStateIndex == 1)
			{
				hauntingPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Unknown, 1);
				EnableEnemyMesh(enable: false, overrideDoNotSet: true);
				creatureSFX.Stop();
			}
		}
		else
		{
			Debug.Log("Girl: collided with player who cannot see it");
			if (staringInHaunt && currentBehaviourStateIndex == 0)
			{
				disappearByVanishing = true;
			}
		}
	}
}
