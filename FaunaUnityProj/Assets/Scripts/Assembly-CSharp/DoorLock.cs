using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class DoorLock : NetworkBehaviour
{
	public InteractTrigger doorTrigger;

	public InteractTrigger doorTriggerB;

	public float maxTimeLeft = 60f;

	public float lockPickTimeLeft = 60f;

	public bool isLocked;

	public bool isPickingLock;

	public bool canBeLocked = true;

	[Space(5f)]
	public DoorLock twinDoor;

	public Transform lockPickerPosition;

	public Transform lockPickerPosition2;

	private float enemyDoorMeter;

	private bool isDoorOpened;

	private NavMeshObstacle navMeshObstacle;

	public AudioClip pickingLockSFX;

	public AudioClip unlockSFX;

	public AudioSource doorLockSFX;

	private bool displayedLockTip;

	private bool localPlayerPickingLock;

	private int playersPickingDoor;

	private float playerPickingLockProgress;

	[Space(3f)]
	public float defaultTimeToHold = 0.3f;

	private bool hauntedDoor;

	private float doorHauntInterval;

	public void Awake()
	{
		if (doorTrigger == null)
		{
			doorTrigger = base.gameObject.GetComponent<InteractTrigger>();
		}
		lockPickTimeLeft = maxTimeLeft;
		navMeshObstacle = GetComponent<NavMeshObstacle>();
		if (RoundManager.Instance.currentDungeonType == 1 && Random.Range(0, 100) < 7)
		{
			hauntedDoor = true;
		}
	}

	public void OnHoldInteract()
	{
		if (isLocked && !displayedLockTip && HUDManager.Instance.holdFillAmount / doorTrigger.timeToHold > 0.3f)
		{
			displayedLockTip = true;
			HUDManager.Instance.DisplayTip("TIP:", "To get through locked doors efficiently, order a <u>lock-picker</u> from the ship terminal.", isWarning: false, useSave: true, "LCTip_Autopicker");
		}
	}

	public void LockDoor(float timeToLockPick = 30f)
	{
		doorTrigger.interactable = false;
		if (doorTriggerB != null)
		{
			doorTriggerB.interactable = false;
		}
		doorTrigger.timeToHold = timeToLockPick;
		doorTrigger.hoverTip = "Locked (pickable)";
		doorTrigger.holdTip = "Picking lock";
		isLocked = true;
		navMeshObstacle.carving = true;
		navMeshObstacle.carveOnlyStationary = true;
		if (twinDoor != null)
		{
			twinDoor.doorTrigger.interactable = false;
			twinDoor.doorTrigger.timeToHold = 35f;
			twinDoor.doorTrigger.hoverTip = "Locked (pickable)";
			twinDoor.doorTrigger.holdTip = "Picking lock";
			twinDoor.isLocked = true;
		}
	}

	public void UnlockDoor()
	{
		doorLockSFX.Stop();
		doorLockSFX.PlayOneShot(unlockSFX);
		navMeshObstacle.carving = false;
		if (isLocked)
		{
			if (doorTriggerB != null)
			{
				doorTriggerB.interactable = true;
				doorTriggerB.hoverTip = "Use door : [LMB]";
				doorTriggerB.holdTip = "";
				doorTriggerB.timeToHoldSpeedMultiplier = 1f;
				doorTriggerB.timeToHold = defaultTimeToHold;
			}
			doorTrigger.interactable = true;
			doorTrigger.hoverTip = "Use door : [LMB]";
			doorTrigger.holdTip = "";
			isPickingLock = false;
			isLocked = false;
			doorTrigger.timeToHoldSpeedMultiplier = 1f;
			navMeshObstacle.carving = false;
			Debug.Log("Unlocking door");
			doorTrigger.timeToHold = defaultTimeToHold;
		}
	}

	public void UnlockDoorSyncWithServer()
	{
		if (isLocked)
		{
			UnlockDoor();
			UnlockDoorServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void UnlockDoorServerRpc()
			{
				UnlockDoorClientRpc();
			}

	[ClientRpc]
	public void UnlockDoorClientRpc()
			{
				UnlockDoor();
			}

	private void TryDoorHaunt()
	{
		if (!(Time.realtimeSinceStartup - doorHauntInterval > 0.5f))
		{
			return;
		}
		doorHauntInterval = Time.realtimeSinceStartup + Mathf.Max(Random.Range(-18f, 30f), 0f);
		if (Random.Range(0, 100) > 50)
		{
			return;
		}
		if (enemyDoorMeter >= 0.1f || StartOfRound.Instance.fearLevel >= 0.4f || GameNetworkManager.Instance.localPlayerController.insanityLevel < GameNetworkManager.Instance.localPlayerController.maxInsanityLevel / 1.2f)
		{
			doorHauntInterval = Mathf.Max(Time.realtimeSinceStartup + 15f, doorHauntInterval);
		}
		else if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) > 18f || Physics.Linecast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, base.transform.position, 256, QueryTriggerInteraction.Ignore))
		{
			doorHauntInterval = Mathf.Max(Time.realtimeSinceStartup + 15f, doorHauntInterval);
		}
		else
		{
			if (Physics.CheckSphere(base.transform.position, 4f, 8912896, QueryTriggerInteraction.Ignore))
			{
				return;
			}
			AnimatedObjectTrigger component = base.gameObject.GetComponent<AnimatedObjectTrigger>();
			if (Random.Range(0, 100) < 16)
			{
				component.TriggerAnimationNonPlayer(playSecondaryAudios: true, overrideBool: true);
				OpenDoorAsEnemyServerRpc();
				return;
			}
			component.TriggerAnimationNonPlayer();
			if (component.boolValue)
			{
				OpenDoorAsEnemyServerRpc();
			}
			else
			{
				CloseDoorNonPlayerServerRpc();
			}
		}
	}

	private void Update()
	{
		if (isLocked)
		{
			if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
			{
				return;
			}
			if (GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.itemId == 14)
			{
				if (StartOfRound.Instance.localPlayerUsingController)
				{
					if (doorTriggerB != null)
					{
						doorTriggerB.disabledHoverTip = "Use key: [R-trigger]";
					}
					doorTrigger.disabledHoverTip = "Use key: [R-trigger]";
				}
				else
				{
					doorTrigger.disabledHoverTip = "Use key: [ LMB ]";
					if (doorTriggerB != null)
					{
						doorTriggerB.disabledHoverTip = "Use key: [ LMB ]";
					}
				}
			}
			else
			{
				doorTrigger.disabledHoverTip = "Locked";
				if (doorTriggerB != null)
				{
					doorTriggerB.disabledHoverTip = "Locked";
				}
			}
			if (playersPickingDoor > 0)
			{
				playerPickingLockProgress = Mathf.Clamp(playerPickingLockProgress + (float)playersPickingDoor * 0.85f * Time.deltaTime, 1f, 3.5f);
			}
			doorTrigger.timeToHoldSpeedMultiplier = Mathf.Clamp((float)playersPickingDoor * 0.85f, 1f, 3.5f);
			if (doorTriggerB != null)
			{
				doorTriggerB.timeToHoldSpeedMultiplier = Mathf.Clamp((float)playersPickingDoor * 0.85f, 1f, 3.5f);
			}
		}
		else
		{
			navMeshObstacle.carving = false;
			if (hauntedDoor)
			{
				TryDoorHaunt();
			}
		}
		if (isLocked && isPickingLock)
		{
			lockPickTimeLeft -= Time.deltaTime;
			doorTrigger.disabledHoverTip = $"Picking lock: {(int)lockPickTimeLeft} sec.";
			if (doorTriggerB != null)
			{
				doorTriggerB.disabledHoverTip = $"Picking lock: {(int)lockPickTimeLeft} sec.";
			}
			if (base.IsServer && lockPickTimeLeft < 0f)
			{
				UnlockDoor();
				UnlockDoorServerRpc();
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (NetworkManager.Singleton == null || !base.IsServer || isLocked || isDoorOpened || !other.CompareTag("Enemy"))
		{
			return;
		}
		EnemyAICollisionDetect component = other.GetComponent<EnemyAICollisionDetect>();
		if (!(component == null) && !component.mainScript.isEnemyDead)
		{
			enemyDoorMeter += Time.deltaTime * component.mainScript.openDoorSpeedMultiplier;
			if (enemyDoorMeter > 1f)
			{
				enemyDoorMeter = 0f;
				base.gameObject.GetComponent<AnimatedObjectTrigger>().TriggerAnimationNonPlayer(component.mainScript.useSecondaryAudiosOnAnimatedObjects, overrideBool: true);
				OpenDoorAsEnemyServerRpc();
			}
		}
	}

	public void OpenOrCloseDoor(PlayerControllerB playerWhoTriggered)
	{
		AnimatedObjectTrigger component = base.gameObject.GetComponent<AnimatedObjectTrigger>();
		component.TriggerAnimation(playerWhoTriggered);
		isDoorOpened = component.boolValue;
		navMeshObstacle.enabled = !component.boolValue;
	}

	public void SetDoorAsOpen(bool isOpen)
	{
		isDoorOpened = isOpen;
		navMeshObstacle.enabled = !isOpen;
		doorTrigger.SetInteractionToHoldOpposite(isOpen);
		if (doorTriggerB != null)
		{
			doorTriggerB.SetInteractionToHoldOpposite(isOpen);
		}
	}

	public void OpenDoorAsEnemy(bool setOpen = true)
	{
		if (hauntedDoor)
		{
			doorHauntInterval = Time.realtimeSinceStartup;
		}
		if (!setOpen)
		{
			isDoorOpened = false;
			navMeshObstacle.enabled = true;
		}
		else
		{
			isDoorOpened = true;
			navMeshObstacle.enabled = false;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenDoorAsEnemyServerRpc()
			{
				OpenDoorAsEnemyClientRpc();
			}

	[ClientRpc]
	public void OpenDoorAsEnemyClientRpc()
			{
				OpenDoorAsEnemy();
			}

	[ServerRpc(RequireOwnership = false)]
	public void CloseDoorNonPlayerServerRpc()
			{
				CloseDoorNonPlayerClientRpc();
			}

	[ClientRpc]
	public void CloseDoorNonPlayerClientRpc()
			{
				OpenDoorAsEnemy(setOpen: false);
			}

	public void TryPickingLock()
	{
		if (isLocked)
		{
			HUDManager.Instance.holdFillAmount = playerPickingLockProgress;
			if (!localPlayerPickingLock)
			{
				localPlayerPickingLock = true;
				PlayerPickLockServerRpc();
			}
		}
	}

	public void StopPickingLock()
	{
		if (localPlayerPickingLock)
		{
			localPlayerPickingLock = false;
			if (playersPickingDoor == 1)
			{
				playerPickingLockProgress = Mathf.Clamp(playerPickingLockProgress - 1f, 0f, 45f);
			}
			PlayerStopPickingLockServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerStopPickingLockServerRpc()
			{
				PlayerStopPickingLockClientRpc();
			}

	[ClientRpc]
	public void PlayerStopPickingLockClientRpc()
			{
				doorLockSFX.Stop();
				playersPickingDoor = Mathf.Clamp(playersPickingDoor - 1, 0, 4);
			}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerPickLockServerRpc()
			{
				PlayerPickLockClientRpc();
			}

	[ClientRpc]
	public void PlayerPickLockClientRpc()
			{
				doorLockSFX.clip = pickingLockSFX;
				doorLockSFX.Play();
				playersPickingDoor = Mathf.Clamp(playersPickingDoor + 1, 0, 4);
			}
}
