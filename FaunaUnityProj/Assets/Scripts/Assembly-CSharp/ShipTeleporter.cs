using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class ShipTeleporter : NetworkBehaviour
{
	public bool isInverseTeleporter;

	public Transform teleportOutPosition;

	[Space(5f)]
	public Transform teleporterPosition;

	public Animator teleporterAnimator;

	public Animator buttonAnimator;

	public AudioSource buttonAudio;

	public AudioSource shipTeleporterAudio;

	public AudioClip buttonPressSFX;

	public AudioClip teleporterSpinSFX;

	public AudioClip teleporterBeamUpSFX;

	public AudioClip beamUpPlayerBodySFX;

	private Coroutine beamUpPlayerCoroutine;

	public int teleporterId = 1;

	private int[] playersBeingTeleported;

	private float cooldownTime;

	public float cooldownAmount;

	public InteractTrigger buttonTrigger;

	public static bool hasBeenSpawnedThisSession;

	public static bool hasBeenSpawnedThisSessionInverse;

	private System.Random shipTeleporterSeed;

	public ReverbPreset caveReverb;

	public void SetRandomSeed()
	{
		if (isInverseTeleporter)
		{
			shipTeleporterSeed = new System.Random(StartOfRound.Instance.randomMapSeed + 17 + (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	private void Awake()
	{
		playersBeingTeleported = new int[4] { -1, -1, -1, -1 };
		if ((isInverseTeleporter && hasBeenSpawnedThisSessionInverse) || (!isInverseTeleporter && hasBeenSpawnedThisSession))
		{
			buttonTrigger.interactable = false;
			cooldownTime = cooldownAmount;
		}
		else if (isInverseTeleporter && !StartOfRound.Instance.inShipPhase)
		{
			SetRandomSeed();
		}
		if (isInverseTeleporter)
		{
			hasBeenSpawnedThisSessionInverse = true;
		}
		else
		{
			hasBeenSpawnedThisSession = true;
		}
	}

	private void Update()
	{
		if (!buttonTrigger.interactable)
		{
			if (cooldownTime <= 0f)
			{
				buttonTrigger.interactable = true;
				return;
			}
			buttonTrigger.disabledHoverTip = $"[Cooldown: {(int)cooldownTime} sec.]";
			cooldownTime -= Time.deltaTime;
		}
	}

	private void OnDisable()
	{
		for (int i = 0; i < playersBeingTeleported.Length; i++)
		{
			if (playersBeingTeleported[i] == teleporterId)
			{
				StartOfRound.Instance.allPlayerScripts[playersBeingTeleported[i]].shipTeleporterId = -1;
			}
		}
		StartOfRound.Instance.StartNewRoundEvent.RemoveListener(SetRandomSeed);
	}

	private void OnEnable()
	{
		StartOfRound.Instance.StartNewRoundEvent.AddListener(SetRandomSeed);
	}

	public void PressTeleportButtonOnLocalClient()
	{
		if (!isInverseTeleporter || (!StartOfRound.Instance.inShipPhase && SceneManager.sceneCount > 1))
		{
			PressTeleportButtonServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PressTeleportButtonServerRpc()
			{
				PressTeleportButtonClientRpc();
			}

	[ClientRpc]
	public void PressTeleportButtonClientRpc()
{		PressButtonEffects();
		if (beamUpPlayerCoroutine != null)
		{
			StopCoroutine(beamUpPlayerCoroutine);
		}
		cooldownTime = cooldownAmount;
		buttonTrigger.interactable = false;
		if (isInverseTeleporter)
		{
			if (CanUseInverseTeleporter())
			{
				beamUpPlayerCoroutine = StartCoroutine(beamOutPlayer());
			}
		}
		else
		{
			beamUpPlayerCoroutine = StartCoroutine(beamUpPlayer());
		}
}
	private void PressButtonEffects()
	{
		buttonAnimator.SetTrigger("press");
		buttonAnimator.SetBool("GlassOpen", value: false);
		buttonAnimator.GetComponentInChildren<AnimatedObjectTrigger>().boolValue = false;
		if (isInverseTeleporter)
		{
			if (CanUseInverseTeleporter())
			{
				teleporterAnimator.SetTrigger("useInverseTeleporter");
			}
			else
			{
				Debug.Log($"Using inverse teleporter was not allowed; {StartOfRound.Instance.inShipPhase}; {StartOfRound.Instance.currentLevel.PlanetName}");
			}
		}
		else
		{
			teleporterAnimator.SetTrigger("useTeleporter");
		}
		buttonAudio.PlayOneShot(buttonPressSFX);
		WalkieTalkie.TransmitOneShotAudio(buttonAudio, buttonPressSFX);
	}

	private bool CanUseInverseTeleporter()
	{
		if (!StartOfRound.Instance.inShipPhase)
		{
			return StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap;
		}
		return false;
	}

	private IEnumerator beamOutPlayer()
	{
		if (GameNetworkManager.Instance.localPlayerController == null)
		{
			yield break;
		}
		if (StartOfRound.Instance.inShipPhase)
		{
			Debug.Log("Attempted using teleporter while in ship phase");
			yield break;
		}
		shipTeleporterAudio.PlayOneShot(teleporterSpinSFX);
		for (int b = 0; b < 5; b++)
		{
			for (int i = 0; i < StartOfRound.Instance.allPlayerObjects.Length; i++)
			{
				PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
				Vector3 position = playerControllerB.transform.position;
				if (playerControllerB.deadBody != null)
				{
					position = playerControllerB.deadBody.bodyParts[5].transform.position;
				}
				if (Vector3.Distance(position, teleportOutPosition.position) > 2f)
				{
					if (playerControllerB.shipTeleporterId != 1)
					{
						if (playerControllerB.deadBody != null)
						{
							playerControllerB.deadBody.beamOutParticle.Stop();
							playerControllerB.deadBody.bodyAudio.Stop();
						}
						else
						{
							playerControllerB.beamOutBuildupParticle.Stop();
							playerControllerB.movementAudio.Stop();
						}
					}
					continue;
				}
				if (playerControllerB.shipTeleporterId == 1)
				{
					Debug.Log($"Cancelled teleporting #{playerControllerB.playerClientId} with inverse teleporter; {playerControllerB.shipTeleporterId}");
					continue;
				}
				SetPlayerTeleporterId(playerControllerB, 2);
				if (playerControllerB.deadBody != null)
				{
					if (playerControllerB.deadBody.beamUpParticle == null)
					{
						yield break;
					}
					if (!playerControllerB.deadBody.beamOutParticle.isPlaying)
					{
						playerControllerB.deadBody.beamOutParticle.Play();
						playerControllerB.deadBody.bodyAudio.PlayOneShot(beamUpPlayerBodySFX);
					}
				}
				else if (!playerControllerB.beamOutBuildupParticle.isPlaying)
				{
					playerControllerB.beamOutBuildupParticle.Play();
					playerControllerB.movementAudio.PlayOneShot(beamUpPlayerBodySFX);
				}
			}
			yield return new WaitForSeconds(1f);
		}
		for (int j = 0; j < StartOfRound.Instance.allPlayerObjects.Length; j++)
		{
			PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[j];
			if (playerControllerB.shipTeleporterId == 1)
			{
				Debug.Log($"Player #{playerControllerB.playerClientId} is in teleport 1, skipping");
				continue;
			}
			SetPlayerTeleporterId(playerControllerB, -1);
			if (playerControllerB.deadBody != null)
			{
				playerControllerB.deadBody.beamOutParticle.Stop();
				playerControllerB.deadBody.bodyAudio.Stop();
			}
			else
			{
				playerControllerB.beamOutBuildupParticle.Stop();
				playerControllerB.movementAudio.Stop();
			}
			if (playerControllerB != GameNetworkManager.Instance.localPlayerController || StartOfRound.Instance.inShipPhase)
			{
				continue;
			}
			Vector3 position2 = playerControllerB.transform.position;
			if (playerControllerB.deadBody != null)
			{
				position2 = playerControllerB.deadBody.bodyParts[5].transform.position;
			}
			if (Vector3.Distance(position2, teleportOutPosition.position) < 2f)
			{
				if (RoundManager.Instance.insideAINodes.Length != 0)
				{
					Vector3 position3 = RoundManager.Instance.insideAINodes[shipTeleporterSeed.Next(0, RoundManager.Instance.insideAINodes.Length)].transform.position;
					Debug.DrawRay(position3, Vector3.up * 1f, Color.red);
					position3 = RoundManager.Instance.GetRandomNavMeshPositionInBoxPredictable(position3, 10f, default(NavMeshHit), shipTeleporterSeed);
					Debug.DrawRay(position3 + Vector3.right * 0.01f, Vector3.up * 3f, Color.green);
					SetPlayerTeleporterId(playerControllerB, 2);
					if (playerControllerB.deadBody != null)
					{
						TeleportPlayerBodyOutServerRpc((int)playerControllerB.playerClientId, position3);
						continue;
					}
					TeleportPlayerOutWithInverseTeleporter((int)playerControllerB.playerClientId, position3);
					TeleportPlayerOutServerRpc((int)playerControllerB.playerClientId, position3);
				}
			}
			else
			{
				Debug.Log($"Player #{playerControllerB.playerClientId} is not close enough to teleporter to beam out");
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TeleportPlayerOutServerRpc(int playerObj, Vector3 teleportPos)
			{
				TeleportPlayerOutClientRpc(playerObj, teleportPos);
			}

	[ClientRpc]
	public void TeleportPlayerOutClientRpc(int playerObj, Vector3 teleportPos)
{if(!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.allPlayerScripts[playerObj].IsOwner)			{
				TeleportPlayerOutWithInverseTeleporter(playerObj, teleportPos);
			}
}
	private void SpikeTrapsReactToInverseTeleport()
	{
		SpikeRoofTrap[] array = UnityEngine.Object.FindObjectsByType<SpikeRoofTrap>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].timeSinceMovingUp = Time.realtimeSinceStartup - 1f;
		}
	}

	private void SetCaveReverb(PlayerControllerB playerScript)
	{
		if (RoundManager.Instance.currentDungeonType != 4)
		{
			return;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("CaveNode");
		for (int i = 0; i < array.Length; i++)
		{
			if (Vector3.Distance(array[i].transform.position, playerScript.transform.position) < 12f)
			{
				playerScript.reverbPreset = caveReverb;
			}
		}
	}

	private void TeleportPlayerOutWithInverseTeleporter(int playerObj, Vector3 teleportPos)
	{
		if (StartOfRound.Instance.allPlayerScripts[playerObj].isPlayerDead)
		{
			StartCoroutine(teleportBodyOut(playerObj, teleportPos));
			return;
		}
		SpikeTrapsReactToInverseTeleport();
		SetCaveReverb(StartOfRound.Instance.allPlayerScripts[playerObj]);
		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerObj];
		SetPlayerTeleporterId(playerControllerB, -1);
		playerControllerB.DropAllHeldItems();
		if ((bool)UnityEngine.Object.FindObjectOfType<AudioReverbPresets>())
		{
			UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[2].ChangeAudioReverbForPlayer(playerControllerB);
		}
		playerControllerB.isInElevator = false;
		playerControllerB.isInHangarShipRoom = false;
		playerControllerB.isInsideFactory = true;
		playerControllerB.averageVelocity = 0f;
		playerControllerB.velocityLastFrame = Vector3.zero;
		StartOfRound.Instance.allPlayerScripts[playerObj].TeleportPlayer(teleportPos);
		StartOfRound.Instance.allPlayerScripts[playerObj].beamOutParticle.Play();
		shipTeleporterAudio.PlayOneShot(teleporterBeamUpSFX);
		StartOfRound.Instance.allPlayerScripts[playerObj].movementAudio.PlayOneShot(teleporterBeamUpSFX);
		if (playerControllerB == GameNetworkManager.Instance.localPlayerController)
		{
			Debug.Log("Teleporter shaking camera");
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TeleportPlayerBodyOutServerRpc(int playerObj, Vector3 teleportPos)
			{
				TeleportPlayerBodyOutClientRpc(playerObj, teleportPos);
			}

	[ClientRpc]
	public void TeleportPlayerBodyOutClientRpc(int playerObj, Vector3 teleportPos)
			{
				StartCoroutine(teleportBodyOut(playerObj, teleportPos));
			}

	private IEnumerator teleportBodyOut(int playerObj, Vector3 teleportPosition)
	{
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => StartOfRound.Instance.allPlayerScripts[playerObj].deadBody != null || Time.realtimeSinceStartup - startTime > 2f);
		if (StartOfRound.Instance.inShipPhase || SceneManager.sceneCount <= 1)
		{
			yield break;
		}
		DeadBodyInfo deadBody = StartOfRound.Instance.allPlayerScripts[playerObj].deadBody;
		SetPlayerTeleporterId(StartOfRound.Instance.allPlayerScripts[playerObj], -1);
		if (deadBody != null)
		{
			deadBody.attachedTo = null;
			deadBody.attachedLimb = null;
			deadBody.secondaryAttachedLimb = null;
			deadBody.secondaryAttachedTo = null;
			if (deadBody.grabBodyObject != null && deadBody.grabBodyObject.isHeld && deadBody.grabBodyObject.playerHeldBy != null)
			{
				deadBody.grabBodyObject.playerHeldBy.DropAllHeldItems();
			}
			deadBody.isInShip = false;
			deadBody.parentedToShip = false;
			deadBody.transform.SetParent(null, worldPositionStays: true);
			deadBody.SetRagdollPositionSafely(teleportPosition, disableSpecialEffects: true);
		}
	}

	private IEnumerator beamUpPlayer()
	{
		shipTeleporterAudio.PlayOneShot(teleporterSpinSFX);
		PlayerControllerB playerToBeamUp = StartOfRound.Instance.mapScreen.targetedPlayer;
		if (playerToBeamUp == null)
		{
			Debug.Log("Targeted player is null");
			yield break;
		}
		if (playerToBeamUp.redirectToEnemy != null)
		{
			Debug.Log($"Attemping to teleport enemy '{playerToBeamUp.redirectToEnemy.gameObject.name}' (tied to player #{playerToBeamUp.playerClientId}) to ship.");
			if (StartOfRound.Instance.shipIsLeaving)
			{
				Debug.Log($"Ship could not teleport enemy '{playerToBeamUp.redirectToEnemy.gameObject.name}' (tied to player #{playerToBeamUp.playerClientId}) because the ship is leaving the nav mesh.");
			}
			playerToBeamUp.redirectToEnemy.ShipTeleportEnemy();
			yield return new WaitForSeconds(3f);
			shipTeleporterAudio.PlayOneShot(teleporterBeamUpSFX);
			if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
		}
		SetPlayerTeleporterId(playerToBeamUp, 1);
		if (playerToBeamUp.deadBody != null)
		{
			if (playerToBeamUp.deadBody.beamUpParticle == null)
			{
				yield break;
			}
			playerToBeamUp.deadBody.beamUpParticle.Play();
			playerToBeamUp.deadBody.bodyAudio.PlayOneShot(beamUpPlayerBodySFX);
		}
		else
		{
			playerToBeamUp.beamUpParticle.Play();
			playerToBeamUp.movementAudio.PlayOneShot(beamUpPlayerBodySFX);
		}
		Debug.Log("Teleport A");
		yield return new WaitForSeconds(3f);
		bool flag = false;
		if (playerToBeamUp.deadBody != null)
		{
			if (playerToBeamUp.deadBody.grabBodyObject == null || !playerToBeamUp.deadBody.grabBodyObject.isHeldByEnemy)
			{
				flag = true;
				playerToBeamUp.deadBody.attachedTo = null;
				playerToBeamUp.deadBody.attachedLimb = null;
				playerToBeamUp.deadBody.secondaryAttachedLimb = null;
				playerToBeamUp.deadBody.secondaryAttachedTo = null;
				playerToBeamUp.deadBody.SetRagdollPositionSafely(teleporterPosition.position, disableSpecialEffects: true);
				playerToBeamUp.deadBody.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
				if (playerToBeamUp.deadBody.grabBodyObject != null && playerToBeamUp.deadBody.grabBodyObject.isHeld && playerToBeamUp.deadBody.grabBodyObject.playerHeldBy != null)
				{
					playerToBeamUp.deadBody.grabBodyObject.playerHeldBy.DropAllHeldItems();
				}
			}
		}
		else
		{
			flag = true;
			playerToBeamUp.DropAllHeldItems();
			if ((bool)UnityEngine.Object.FindObjectOfType<AudioReverbPresets>())
			{
				UnityEngine.Object.FindObjectOfType<AudioReverbPresets>().audioPresets[3].ChangeAudioReverbForPlayer(playerToBeamUp);
			}
			playerToBeamUp.isInElevator = true;
			playerToBeamUp.isInHangarShipRoom = true;
			playerToBeamUp.isInsideFactory = false;
			playerToBeamUp.averageVelocity = 0f;
			playerToBeamUp.velocityLastFrame = Vector3.zero;
			if (flag == (bool)GameNetworkManager.Instance.localPlayerController)
			{
				TimeOfDay.Instance.SetInsideLightingDimness(doNotLerp: false, setValueTo: true);
			}
			playerToBeamUp.TeleportPlayer(teleporterPosition.position, withRotation: true, 160f);
		}
		Debug.Log("Teleport B");
		SetPlayerTeleporterId(playerToBeamUp, -1);
		if (flag)
		{
			shipTeleporterAudio.PlayOneShot(teleporterBeamUpSFX);
			if (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
		}
		Debug.Log("Teleport C");
	}

	private void SetPlayerTeleporterId(PlayerControllerB playerScript, int teleporterId)
	{
		playerScript.shipTeleporterId = teleporterId;
		playersBeingTeleported[playerScript.playerClientId] = (int)playerScript.playerClientId;
	}
}
