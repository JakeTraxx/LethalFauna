using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShipBuildModeManager : NetworkBehaviour
{
	public AudioClip beginPlacementSFX;

	public AudioClip denyPlacementSFX;

	public AudioClip cancelPlacementSFX;

	public AudioClip storeItemSFX;

	[Space(5f)]
	public bool InBuildMode;

	private bool CanConfirmPosition;

	private PlaceableShipObject placingObject;

	public Transform ghostObject;

	public MeshFilter ghostObjectMesh;

	public MeshRenderer ghostObjectRenderer;

	public MeshFilter selectionOutlineMesh;

	public MeshRenderer selectionOutlineRenderer;

	public Material ghostObjectGreen;

	public Material ghostObjectRed;

	private PlayerControllerB player;

	private int placeableShipObjectsMask = 67108864;

	private int placementMask = 2305;

	private int placementMaskAndBlockers = 134220033;

	private float timeSincePlacingObject;

	public PlayerActions playerActions;

	private RaycastHit rayHit;

	private Ray playerCameraRay;

	private BoxCollider currentCollider;

	private Collider[] collidersInPlacingObject;

	public static ShipBuildModeManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			playerActions = new PlayerActions();
		}
		else
		{
			Object.Destroy(Instance.gameObject);
		}
	}

	private void OnEnable()
	{
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("BuildMode").performed += EnterBuildMode;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("Delete").performed += StoreObject_performed;
		playerActions.Movement.Enable();
	}

	private void OnDisable()
	{
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("BuildMode").performed -= EnterBuildMode;
		IngamePlayerSettings.Instance.playerInput.actions.FindAction("Delete").performed -= StoreObject_performed;
		playerActions.Movement.Disable();
	}

	private Vector3 OffsetObjectFromWallBasedOnDimensions(Vector3 targetPosition, RaycastHit wall)
	{
		if (placingObject.overrideWallOffset)
		{
			return wall.point + wall.normal * placingObject.wallOffset;
		}
		float num = (currentCollider.size.z / 2f + currentCollider.size.x / 2f) / 2f;
		return wall.point + wall.normal * (num + 0.01f);
	}

	private void Update()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		player = GameNetworkManager.Instance.localPlayerController;
		if (!PlayerMeetsConditionsToBuild(log: false))
		{
			CancelBuildMode();
		}
		if (placingObject == null)
		{
			CancelBuildMode();
		}
		if (InBuildMode)
		{
			if (currentCollider == null)
			{
				currentCollider = placingObject.placeObjectCollider as BoxCollider;
			}
			if (IngamePlayerSettings.Instance.playerInput.actions.FindAction("ReloadBatteries").IsPressed() || (StartOfRound.Instance.localPlayerUsingController && playerActions.Movement.InspectItem.IsPressed()))
			{
				ghostObject.eulerAngles = new Vector3(ghostObject.eulerAngles.x, ghostObject.eulerAngles.y + Time.deltaTime * 155f, ghostObject.eulerAngles.z);
			}
			playerCameraRay = new Ray(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward);
			if (Physics.Raycast(playerCameraRay, out rayHit, 4f, placementMask, QueryTriggerInteraction.Ignore))
			{
				if (Vector3.Angle(rayHit.normal, Vector3.up) < 45f)
				{
					ghostObject.position = rayHit.point + Vector3.up * placingObject.yOffset;
				}
				else if (placingObject.AllowPlacementOnWalls)
				{
					ghostObject.position = OffsetObjectFromWallBasedOnDimensions(rayHit.point, rayHit);
					if (Physics.Raycast(ghostObject.position, Vector3.down, out rayHit, placingObject.yOffset, placementMask, QueryTriggerInteraction.Ignore))
					{
						ghostObject.position += Vector3.up * rayHit.distance;
					}
				}
				else if (Physics.Raycast(OffsetObjectFromWallBasedOnDimensions(rayHit.point, rayHit), Vector3.down, out rayHit, 20f, placementMask, QueryTriggerInteraction.Ignore))
				{
					ghostObject.position = rayHit.point + Vector3.up * placingObject.yOffset;
				}
			}
			else if (Physics.Raycast(playerCameraRay.GetPoint(4f), Vector3.down, out rayHit, 20f, placementMask, QueryTriggerInteraction.Ignore))
			{
				ghostObject.position = rayHit.point + Vector3.up * placingObject.yOffset;
			}
			bool flag = Physics.CheckBox(ghostObject.position, currentCollider.size * 0.5f * 0.57f, Quaternion.Euler(ghostObject.eulerAngles), placementMaskAndBlockers, QueryTriggerInteraction.Ignore);
			if (!flag && placingObject.doCollisionPointCheck)
			{
				Vector3 vector = ghostObject.position + ghostObject.forward * placingObject.collisionPointCheck.z + ghostObject.right * placingObject.collisionPointCheck.x + ghostObject.up * placingObject.collisionPointCheck.y;
				Debug.DrawRay(vector, Vector3.up * 2f, Color.blue);
				if (Physics.CheckSphere(vector, 1f, placementMaskAndBlockers, QueryTriggerInteraction.Ignore))
				{
					flag = true;
				}
			}
			CanConfirmPosition = !flag && StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(ghostObject.position);
			if (flag)
			{
				ghostObjectRenderer.sharedMaterial = ghostObjectRed;
			}
			else
			{
				ghostObjectRenderer.sharedMaterial = ghostObjectGreen;
			}
		}
		else
		{
			timeSincePlacingObject += Time.deltaTime;
		}
	}

	private bool PlayerMeetsConditionsToBuild(bool log = true)
	{
		if (InBuildMode && (placingObject == null || placingObject.inUse || StartOfRound.Instance.unlockablesList.unlockables[placingObject.unlockableID].inStorage))
		{
			if (log)
			{
				Debug.Log("Could not build 1");
			}
			return false;
		}
		if (GameNetworkManager.Instance.localPlayerController.isTypingChat)
		{
			if (log)
			{
				Debug.Log("Could not build 2");
			}
			return false;
		}
		if (player.isPlayerDead || player.inSpecialInteractAnimation || player.activatingItem)
		{
			if (log)
			{
				Debug.Log("Could not build 3");
			}
			return false;
		}
		if (player.disablingJetpackControls || player.jetpackControls)
		{
			if (log)
			{
				Debug.Log("Could not build 4");
			}
			return false;
		}
		if (!player.isInHangarShipRoom)
		{
			if (log)
			{
				Debug.Log("Could not build 5");
			}
			return false;
		}
		if (StartOfRound.Instance.fearLevel > 0.4f)
		{
			if (log)
			{
				Debug.Log("Could not build 6");
			}
			return false;
		}
		if (StartOfRound.Instance.shipAnimator.GetCurrentAnimatorStateInfo(0).tagHash != Animator.StringToHash("ShipIdle"))
		{
			if (log)
			{
				Debug.Log("Could not build 7");
			}
			return false;
		}
		if (!StartOfRound.Instance.inShipPhase && !StartOfRound.Instance.shipHasLanded)
		{
			if (log)
			{
				Debug.Log("Could not build 7b");
			}
			return false;
		}
		return true;
	}

	private void EnterBuildMode(InputAction.CallbackContext context)
	{
		if (!context.performed || GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.isTypingChat)
		{
			return;
		}
		if (InBuildMode)
		{
			if (!(timeSincePlacingObject <= 1f) && PlayerMeetsConditionsToBuild())
			{
				if (!CanConfirmPosition)
				{
					HUDManager.Instance.UIAudio.PlayOneShot(denyPlacementSFX);
					return;
				}
				timeSincePlacingObject = 0f;
				Debug.DrawRay(ghostObject.position, Vector3.up * 1.5f, Color.green, 2f);
				PlaceShipObject(ghostObject.position, ghostObject.eulerAngles, placingObject);
				CancelBuildMode(cancelBeforePlacement: false);
				PlaceShipObjectServerRpc(ghostObject.position, ghostObject.eulerAngles, placingObject.parentObject.GetComponent<NetworkObject>(), (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
			return;
		}
		player = GameNetworkManager.Instance.localPlayerController;
		if (!PlayerMeetsConditionsToBuild() || (!Physics.Raycast(player.gameplayCamera.transform.position, player.gameplayCamera.transform.forward, out rayHit, 4f, placeableShipObjectsMask, QueryTriggerInteraction.Ignore) && !Physics.Raycast(player.gameplayCamera.transform.position + Vector3.up * 5f, Vector3.down, out rayHit, 5f, placeableShipObjectsMask, QueryTriggerInteraction.Ignore)) || !rayHit.collider.gameObject.CompareTag("PlaceableObject"))
		{
			return;
		}
		PlaceableShipObject component = rayHit.collider.gameObject.GetComponent<PlaceableShipObject>();
		if (component == null)
		{
			return;
		}
		if (timeSincePlacingObject <= 1f)
		{
			HUDManager.Instance.UIAudio.PlayOneShot(denyPlacementSFX);
			return;
		}
		placingObject = component;
		collidersInPlacingObject = placingObject.parentObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < collidersInPlacingObject.Length; i++)
		{
			if (!collidersInPlacingObject[i].CompareTag("DoNotSet") && !collidersInPlacingObject[i].CompareTag("InteractTrigger"))
			{
				collidersInPlacingObject[i].enabled = false;
			}
		}
		InBuildMode = true;
		CreateGhostObjectAndHighlight();
	}

	private void CreateGhostObjectAndHighlight()
	{
		if (!(placingObject == null))
		{
			HUDManager.Instance.buildModeControlTip.enabled = true;
			if (StartOfRound.Instance.localPlayerUsingController)
			{
				HUDManager.Instance.buildModeControlTip.text = "Confirm: [Y]   |   Rotate: [L-shoulder]   |   Store: [B]";
			}
			else
			{
				HUDManager.Instance.buildModeControlTip.text = "Confirm: [B]   |   Rotate: [R]   |   Store: [X]";
			}
			HUDManager.Instance.UIAudio.PlayOneShot(beginPlacementSFX);
			ghostObject.transform.eulerAngles = placingObject.mainMesh.transform.eulerAngles;
			ghostObjectMesh.mesh = placingObject.mainMesh.mesh;
			ghostObjectMesh.transform.localScale = Vector3.Scale(placingObject.mainMesh.transform.localScale, placingObject.parentObject.transform.localScale);
			ghostObjectMesh.transform.position = ghostObject.position + (placingObject.mainMesh.transform.position - placingObject.placeObjectCollider.transform.position);
			ghostObjectMesh.transform.localEulerAngles = Vector3.zero;
			ghostObjectRenderer.enabled = true;
			selectionOutlineMesh.mesh = placingObject.mainMesh.mesh;
			selectionOutlineMesh.transform.localScale = Vector3.Scale(placingObject.mainMesh.transform.localScale, placingObject.parentObject.transform.localScale);
			selectionOutlineMesh.transform.localScale = selectionOutlineMesh.transform.localScale * 1.04f;
			selectionOutlineMesh.transform.position = placingObject.mainMesh.transform.position;
			selectionOutlineMesh.transform.eulerAngles = placingObject.mainMesh.transform.eulerAngles;
			selectionOutlineRenderer.enabled = true;
		}
	}

	public void CancelBuildMode(bool cancelBeforePlacement = true)
	{
		if (!InBuildMode)
		{
			return;
		}
		InBuildMode = false;
		if (cancelBeforePlacement)
		{
			HUDManager.Instance.UIAudio.PlayOneShot(cancelPlacementSFX);
		}
		if (placingObject != null && collidersInPlacingObject != null)
		{
			for (int i = 0; i < collidersInPlacingObject.Length; i++)
			{
				if (!(collidersInPlacingObject[i] == null) && !collidersInPlacingObject[i].CompareTag("DoNotSet") && !collidersInPlacingObject[i].CompareTag("InteractTrigger"))
				{
					collidersInPlacingObject[i].enabled = true;
				}
			}
		}
		if (currentCollider != null)
		{
			currentCollider.enabled = true;
		}
		currentCollider = null;
		HUDManager.Instance.buildModeControlTip.enabled = false;
		ghostObjectRenderer.enabled = false;
		selectionOutlineRenderer.enabled = false;
	}

	private void ConfirmBuildMode_performed(InputAction.CallbackContext context)
	{
		if (context.performed && !(timeSincePlacingObject <= 1f) && PlayerMeetsConditionsToBuild() && InBuildMode)
		{
			if (!CanConfirmPosition)
			{
				HUDManager.Instance.UIAudio.PlayOneShot(denyPlacementSFX);
				return;
			}
			timeSincePlacingObject = 0f;
			PlaceShipObject(ghostObject.position, ghostObject.eulerAngles, placingObject);
			CancelBuildMode(cancelBeforePlacement: false);
			PlaceShipObjectServerRpc(ghostObject.position, ghostObject.eulerAngles, placingObject.parentObject.GetComponent<NetworkObject>(), (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlaceShipObjectServerRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved)
{if(objectRef.TryGet(out var networkObject))		{
			PlaceableShipObject componentInChildren = networkObject.gameObject.GetComponentInChildren<PlaceableShipObject>();
			if (componentInChildren != null && !StartOfRound.Instance.unlockablesList.unlockables[componentInChildren.unlockableID].inStorage)
			{
				PlaceShipObjectClientRpc(newPosition, newRotation, objectRef, playerWhoMoved);
			}
			else
			{
				Debug.Log($"Error! Object was in storage on server. object id: {networkObject.NetworkObjectId}; name: {networkObject.gameObject.name}");
			}
		}
}
	[ClientRpc]
	public void PlaceShipObjectClientRpc(Vector3 newPosition, Vector3 newRotation, NetworkObjectReference objectRef, int playerWhoMoved)
{if(NetworkManager.Singleton == null || base.NetworkManager.ShutdownInProgress || GameNetworkManager.Instance == null || StartOfRound.Instance == null || (GameNetworkManager.Instance.localPlayerController != null && playerWhoMoved == (int)GameNetworkManager.Instance.localPlayerController.playerClientId))		{
			return;
		}
		if (objectRef.TryGet(out var networkObject))
		{
			if (networkObject == null)
			{
				Debug.Log($"Error! Could not get network object with id: {objectRef.NetworkObjectId} in placeshipobjectClientRpc");
				return;
			}
			PlaceableShipObject componentInChildren = networkObject.GetComponentInChildren<PlaceableShipObject>();
			if (componentInChildren != null && !StartOfRound.Instance.unlockablesList.unlockables[componentInChildren.unlockableID].inStorage)
			{
				PlaceShipObject(newPosition, newRotation, componentInChildren);
			}
			else
			{
				Debug.Log($"Error! Object was in storage on client. object id: {networkObject.NetworkObjectId}; name: {networkObject.gameObject.name}");
			}
		}
		else
		{
			Debug.Log($"Error! Could not get network object with id: {objectRef.NetworkObjectId} in placeshipobjectClientRpc");
		}
}
	private void StoreObject_performed(InputAction.CallbackContext context)
	{
		if (context.performed)
		{
			StoreObjectLocalClient();
		}
	}

	public void StoreObjectLocalClient()
	{
		if (!(timeSincePlacingObject <= 0.25f) && InBuildMode && !(placingObject == null) && StartOfRound.Instance.unlockablesList.unlockables[placingObject.unlockableID].canBeStored)
		{
			bool num = placingObject.parentObject.gameObject.GetComponentInChildren<GrabbableObject>();
			HUDManager.Instance.UIAudio.PlayOneShot(storeItemSFX);
			CancelBuildMode(cancelBeforePlacement: false);
			if (num)
			{
				HUDManager.Instance.DisplayTip("Cannot store!", "This furniture contains scrap.");
				return;
			}
			if (StartOfRound.Instance.unlockablesList.unlockables[placingObject.unlockableID].inStorage)
			{
				Debug.Log("Cannot store " + StartOfRound.Instance.unlockablesList.unlockables[placingObject.unlockableID].unlockableName + "; already in storage.");
				return;
			}
			timeSincePlacingObject = 0f;
			StoreObjectServerRpc(placingObject.parentObject.GetComponent<NetworkObject>(), (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StoreObjectServerRpc(NetworkObjectReference objectRef, int playerWhoStored)
{if(!objectRef.TryGet(out var networkObject))		{
			return;
		}
		PlaceableShipObject componentInChildren = networkObject.gameObject.GetComponentInChildren<PlaceableShipObject>();
		bool flag = networkObject.gameObject.GetComponentInChildren<GrabbableObject>();
		if (componentInChildren != null && !StartOfRound.Instance.unlockablesList.unlockables[componentInChildren.unlockableID].inStorage && !flag && Time.realtimeSinceStartup - componentInChildren.lastTimeScrapWasPlaced > 6f && StartOfRound.Instance.unlockablesList.unlockables[componentInChildren.unlockableID].canBeStored)
		{
			if (TimeOfDay.Instance.furniturePlacedAtQuotaStart.Contains(componentInChildren.unlockableID))
			{
				TimeOfDay.Instance.furniturePlacedAtQuotaStart.Remove(componentInChildren.unlockableID);
			}
			StartOfRound.Instance.unlockablesList.unlockables[componentInChildren.unlockableID].inStorage = true;
			StoreShipObjectClientRpc(objectRef, playerWhoStored, componentInChildren.unlockableID);
			if (!StartOfRound.Instance.unlockablesList.unlockables[componentInChildren.unlockableID].spawnPrefab)
			{
				componentInChildren.parentObject.disableObject = true;
				Debug.Log("DISABLE OBJECT D");
			}
			else if (networkObject.IsSpawned)
			{
				networkObject.Despawn();
				Debug.Log($"despawning object. IsSpawned? : {networkObject.IsSpawned}");
			}
			if (StartOfRound.Instance.SpawnedShipUnlockables.ContainsKey(componentInChildren.unlockableID))
			{
				StartOfRound.Instance.SpawnedShipUnlockables.Remove(componentInChildren.unlockableID);
			}
		}
}
	[ClientRpc]
	public void StoreShipObjectClientRpc(NetworkObjectReference objectRef, int playerWhoStored, int unlockableID)
{if(NetworkManager.Singleton == null || base.NetworkManager.ShutdownInProgress || base.IsServer)		{
			return;
		}
		if (playerWhoStored == (int)GameNetworkManager.Instance.localPlayerController.playerClientId)
		{
			HUDManager.Instance.DisplayTip("Item stored!", "You can see stored items in the terminal by using command 'STORAGE'", isWarning: false, useSave: true, "LC_StorageTip");
		}
		StartOfRound.Instance.unlockablesList.unlockables[unlockableID].inStorage = true;
		if (objectRef.TryGet(out var networkObject))
		{
			PlaceableShipObject componentInChildren = networkObject.GetComponentInChildren<PlaceableShipObject>();
			if (componentInChildren != null && !StartOfRound.Instance.unlockablesList.unlockables[unlockableID].spawnPrefab)
			{
				componentInChildren.parentObject.disableObject = true;
				Debug.Log("DISABLE OBJECT E");
			}
		}
}
	public void PlaceShipObject(Vector3 placementPosition, Vector3 placementRotation, PlaceableShipObject placeableObject, bool placementSFX = true)
	{
		_ = placeableObject.parentObject.rotationOffset;
		StartOfRound.Instance.suckingFurnitureOutOfShip = false;
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedPosition = placementPosition;
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedRotation = placementRotation;
		Debug.Log($"Saving placed position as: {placementPosition}");
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].hasBeenMoved = true;
		if (placeableObject.parentObjectSecondary != null)
		{
			_ = placeableObject.parentObjectSecondary.transform.position;
			_ = placeableObject.parentObjectSecondary.transform.rotation;
			Quaternion quaternion = Quaternion.Euler(placementRotation) * Quaternion.Inverse(placeableObject.mainMesh.transform.rotation);
			placeableObject.parentObjectSecondary.transform.rotation = quaternion * placeableObject.parentObjectSecondary.transform.rotation;
			placeableObject.parentObjectSecondary.position = placementPosition + (placeableObject.parentObjectSecondary.transform.position - placeableObject.mainMesh.transform.position) + (placeableObject.mainMesh.transform.position - placeableObject.placeObjectCollider.transform.position);
		}
		else if (placeableObject.parentObject != null)
		{
			_ = placeableObject.parentObject.positionOffset;
			_ = placeableObject.parentObject.transform.rotation;
			Quaternion quaternion2 = Quaternion.Euler(placementRotation) * Quaternion.Inverse(placeableObject.mainMesh.transform.rotation);
			placeableObject.parentObject.rotationOffset = (quaternion2 * placeableObject.parentObject.transform.rotation).eulerAngles;
			placeableObject.parentObject.transform.rotation = quaternion2 * placeableObject.parentObject.transform.rotation;
			placeableObject.parentObject.positionOffset = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(placementPosition + (placeableObject.parentObject.transform.position - placeableObject.mainMesh.transform.position) + (placeableObject.mainMesh.transform.position - placeableObject.placeObjectCollider.transform.position));
		}
		if (placementSFX)
		{
			placeableObject.GetComponent<AudioSource>().PlayOneShot(placeableObject.placeObjectSFX);
		}
	}

	public void ResetShipObjectToDefaultPosition(PlaceableShipObject placeableObject)
	{
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedPosition = Vector3.zero;
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].placedRotation = Vector3.zero;
		StartOfRound.Instance.unlockablesList.unlockables[placeableObject.unlockableID].hasBeenMoved = false;
		if (placeableObject.parentObjectSecondary != null)
		{
			placeableObject.parentObjectSecondary.transform.eulerAngles = placeableObject.parentObject.startingRotation;
			placeableObject.parentObjectSecondary.position = placeableObject.parentObject.startingPosition;
		}
		else if (placeableObject.parentObject != null)
		{
			placeableObject.parentObject.rotationOffset = placeableObject.parentObject.startingRotation;
			placeableObject.parentObject.positionOffset = placeableObject.parentObject.startingPosition;
		}
	}
}
