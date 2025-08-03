using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BeltBagItem : GrabbableObject
{
	public Animator beltBagAnimator;

	public AudioSource bagAudio;

	public Vector3 beltBagTorsoOffset;

	public Vector3 beltBagTorsoOffsetRotation;

	public bool subtractOffset;

	public Transform hangingBeltTarget;

	public PlayerControllerB currentPlayerChecking;

	private bool tryingCheckBag;

	public List<GrabbableObject> objectsInBag = new List<GrabbableObject>();

	private BeltBagInventoryUI beltBagUI;

	public bool tryingAddToBag;

	public AudioClip zipUpBagSFX;

	public AudioClip[] unzipBagSFX;

	public AudioClip[] grabItemInBagSFX;

	public AudioClip beltBagUnclipSFX;

	private bool wasPocketed;

	private float timeAtLastGrabbingItemInBag;

	private int placingItemsInBag;

	public BeltBagItem insideAnotherBeltBag;

	public InteractTrigger useBagTrigger;

	public void GrabItemInBag()
	{
		if (Time.realtimeSinceStartup - timeAtLastGrabbingItemInBag > 0.17f)
		{
			timeAtLastGrabbingItemInBag = Time.realtimeSinceStartup;
			RoundManager.PlayRandomClip(bagAudio, grabItemInBagSFX, randomize: true, 1f, -1);
			GrabItemInBagServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void GrabItemInBagServerRpc()
			{
				GrabItemInBagClientRpc();
			}

	[ClientRpc]
	public void GrabItemInBagClientRpc()
{if(!(currentPlayerChecking == GameNetworkManager.Instance.localPlayerController))			{
				RoundManager.PlayRandomClip(bagAudio, grabItemInBagSFX, randomize: true, 1f, -1);
			}
}
	public void TryAddObjectToBag(GrabbableObject gObject)
	{
		if (!tryingAddToBag && !objectsInBag.Contains(gObject))
		{
			NetworkObject component = gObject.GetComponent<NetworkObject>();
			if (!(component == null))
			{
				tryingAddToBag = true;
				TryAddObjectToBagServerRpc(component, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TryAddObjectToBagServerRpc(NetworkObjectReference netObjectRef, int playerWhoAdded)
{		if (netObjectRef.TryGet(out var networkObject))
		{
			GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
			if (component != null && !component.isHeld && !component.heldByPlayerOnServer && !component.isHeldByEnemy)
			{
				if (StartOfRound.Instance.allPlayerScripts[playerWhoAdded] == GameNetworkManager.Instance.localPlayerController)
				{
					tryingAddToBag = false;
				}
				component.heldByPlayerOnServer = true;
				PutObjectInBagLocalClient(component);
				TryAddObjectToBagClientRpc(netObjectRef, playerWhoAdded);
				return;
			}
		}
		CancelAddObjectToBagClientRpc(playerWhoAdded);
}
	[ClientRpc]
	public void TryAddObjectToBagClientRpc(NetworkObjectReference netObjectRef, int playerWhoAdded)
{if(base.IsServer)		{
			return;
		}
		if (StartOfRound.Instance.allPlayerScripts[playerWhoAdded] == GameNetworkManager.Instance.localPlayerController)
		{
			tryingAddToBag = false;
		}
		if (netObjectRef.TryGet(out var networkObject))
		{
			GrabbableObject component = networkObject.GetComponent<GrabbableObject>();
			if (component != null)
			{
				PutObjectInBagLocalClient(component);
			}
		}
}
	[ClientRpc]
	public void CancelAddObjectToBagClientRpc(int playerWhoAdded)
{if(!(StartOfRound.Instance.allPlayerScripts[playerWhoAdded] != GameNetworkManager.Instance.localPlayerController))			{
				tryingAddToBag = false;
			}
}
	public void PutObjectInBagLocalClient(GrabbableObject gObject)
	{
		if (!objectsInBag.Contains(gObject))
		{
			objectsInBag.Add(gObject);
		}
		if (currentPlayerChecking == GameNetworkManager.Instance.localPlayerController)
		{
			beltBagUI.FillSlots(this);
		}
		BeltBagItem beltBagItem = gObject as BeltBagItem;
		if (beltBagItem != null)
		{
			beltBagItem.insideAnotherBeltBag = this;
		}
		RoundManager.PlayRandomClip(bagAudio, grabItemInBagSFX, randomize: true, 1f, -1);
		StartCoroutine(putObjectInBagAnimation(gObject));
	}

	private IEnumerator putObjectInBagAnimation(GrabbableObject gObject)
	{
		float time = 0f;
		Vector3 startingPosition = gObject.transform.position;
		gObject.EnablePhysics(enable: false);
		gObject.transform.SetParent(null);
		gObject.startFallingPosition = gObject.transform.position;
		gObject.targetFloorPosition = gObject.transform.position;
		placingItemsInBag++;
		while (time < 1f)
		{
			time += Time.deltaTime * 14f;
			gObject.targetFloorPosition = Vector3.Lerp(startingPosition, base.transform.position, time / 1f);
			yield return null;
		}
		placingItemsInBag--;
		gObject.targetFloorPosition = new Vector3(3000f, -400f, 3000f);
		gObject.startFallingPosition = new Vector3(3000f, -400f, 3000f);
	}

	public void OnDisable()
	{
		if (!base.IsServer)
		{
			return;
		}
		for (int i = 0; i < objectsInBag.Count; i++)
		{
			if (!objectsInBag[i].isHeld && !objectsInBag[i].isHeldByEnemy)
			{
				Object.Destroy(objectsInBag[i].gameObject);
			}
		}
	}

	public void RemoveObjectFromBag(int objectId)
	{
		NetworkObject networkObject = objectsInBag[objectId].NetworkObject;
		bool flag = false;
		bool flag2 = false;
		Vector3 hitPoint;
		NetworkObject physicsRegionOfDroppedObject = GetPhysicsRegionOfDroppedObject(null, out hitPoint);
		if (physicsRegionOfDroppedObject == null)
		{
			hitPoint = GetItemFloorPosition();
			if (StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(hitPoint))
			{
				flag2 = true;
				flag = true;
				hitPoint = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(hitPoint);
			}
			else if (StartOfRound.Instance.shipBounds.bounds.Contains(hitPoint))
			{
				flag2 = true;
				hitPoint = StartOfRound.Instance.elevatorTransform.InverseTransformPoint(hitPoint);
			}
			else
			{
				hitPoint = StartOfRound.Instance.propsContainer.InverseTransformPoint(hitPoint);
			}
		}
		if ((bool)physicsRegionOfDroppedObject)
		{
			RemoveFromBagLocalClientNonElevatorParent(networkObject, physicsRegionOfDroppedObject, hitPoint, isInFactory);
			RemoveFromBagNonElevatorParentServerRpc(networkObject, physicsRegionOfDroppedObject, hitPoint, (int)GameNetworkManager.Instance.localPlayerController.playerClientId, isInFactory);
		}
		else
		{
			RemoveFromBagLocalClient(networkObject, flag2, flag, hitPoint, isInFactory);
			RemoveFromBagServerRpc(networkObject, flag2, flag, hitPoint, (int)GameNetworkManager.Instance.localPlayerController.playerClientId, isInFactory);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void RemoveFromBagServerRpc(NetworkObjectReference objectRef, bool setInElevator, bool setInShip, Vector3 targetPosition, int playerWhoRemoved, bool inFactory)
{		{
			if (StartOfRound.Instance.allPlayerScripts[playerWhoRemoved] != GameNetworkManager.Instance.localPlayerController)
			{
				RemoveFromBagLocalClient(objectRef, setInElevator, setInShip, targetPosition, inFactory);
			}
			RemoveFromBagClientRpc(objectRef, setInElevator, setInShip, targetPosition, playerWhoRemoved, inFactory);
		}
}
	[ClientRpc]
	public void RemoveFromBagClientRpc(NetworkObjectReference objectRef, bool setInElevator, bool setInShip, Vector3 targetPosition, int playerWhoRemoved, bool inFactory)
{if(!base.IsServer && StartOfRound.Instance.allPlayerScripts[playerWhoRemoved] != GameNetworkManager.Instance.localPlayerController)			{
				RemoveFromBagLocalClient(objectRef, setInElevator, setInShip, targetPosition, inFactory);
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void RemoveFromBagNonElevatorParentServerRpc(NetworkObjectReference objectRef, NetworkObjectReference nonElevatorParent, Vector3 targetPosition, int playerWhoRemoved, bool inFactory)
{		{
			if (StartOfRound.Instance.allPlayerScripts[playerWhoRemoved] != GameNetworkManager.Instance.localPlayerController)
			{
				RemoveFromBagLocalClientNonElevatorParent(objectRef, nonElevatorParent, targetPosition, inFactory);
			}
			RemoveFromBagNonElevatorParentClientRpc(objectRef, nonElevatorParent, targetPosition, playerWhoRemoved, inFactory);
		}
}
	[ClientRpc]
	public void RemoveFromBagNonElevatorParentClientRpc(NetworkObjectReference objectRef, NetworkObjectReference nonElevatorParent, Vector3 targetPosition, int playerWhoRemoved, bool inFactory)
{if(!base.IsServer && StartOfRound.Instance.allPlayerScripts[playerWhoRemoved] != GameNetworkManager.Instance.localPlayerController)			{
				RemoveFromBagLocalClientNonElevatorParent(objectRef, nonElevatorParent, targetPosition, inFactory);
			}
}
	private void RemoveFromBagLocalClientNonElevatorParent(NetworkObjectReference objectRef, NetworkObjectReference parentNetworkObj, Vector3 targetPosition, bool inFactory)
	{
		GrabbableObject grabbableObject = null;
		if (objectRef.TryGet(out var networkObject))
		{
			for (int i = 0; i < objectsInBag.Count; i++)
			{
				if (objectsInBag[i].NetworkObject == networkObject)
				{
					grabbableObject = objectsInBag[i];
					objectsInBag.Remove(grabbableObject);
				}
			}
		}
		if (!(grabbableObject == null))
		{
			if (base.IsServer)
			{
				grabbableObject.heldByPlayerOnServer = false;
			}
			grabbableObject.fallTime = 0f;
			grabbableObject.hasHitGround = false;
			grabbableObject.isInFactory = inFactory;
			if (parentNetworkObj.TryGet(out var networkObject2))
			{
				grabbableObject.transform.SetParent(networkObject2.transform);
			}
			GameNetworkManager.Instance.localPlayerController.SetItemInElevator(droppedInShipRoom: false, droppedInElevator: false, grabbableObject);
			if (grabbableObject.transform.parent != null)
			{
				grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(base.transform.position + Vector3.up * 0.07f);
			}
			else
			{
				grabbableObject.startFallingPosition = base.transform.position + Vector3.up * 0.07f;
			}
			grabbableObject.targetFloorPosition = targetPosition;
			grabbableObject.EnablePhysics(enable: true);
			BeltBagItem beltBagItem = grabbableObject as BeltBagItem;
			if (beltBagItem != null)
			{
				beltBagItem.insideAnotherBeltBag = null;
			}
		}
	}

	private void RemoveFromBagLocalClient(NetworkObjectReference objectRef, bool inElevator, bool inShip, Vector3 targetPosition, bool inFactory)
	{
		GrabbableObject grabbableObject = null;
		if (objectRef.TryGet(out var networkObject))
		{
			for (int i = 0; i < objectsInBag.Count; i++)
			{
				if (objectsInBag[i].NetworkObject == networkObject)
				{
					grabbableObject = objectsInBag[i];
					objectsInBag.Remove(grabbableObject);
				}
			}
		}
		if (!(grabbableObject == null))
		{
			if (base.IsServer)
			{
				grabbableObject.heldByPlayerOnServer = false;
			}
			grabbableObject.fallTime = 0f;
			grabbableObject.hasHitGround = false;
			grabbableObject.isInFactory = inFactory;
			if (inElevator)
			{
				grabbableObject.transform.SetParent(StartOfRound.Instance.elevatorTransform, worldPositionStays: true);
			}
			else
			{
				grabbableObject.transform.SetParent(StartOfRound.Instance.propsContainer, worldPositionStays: true);
			}
			GameNetworkManager.Instance.localPlayerController.SetItemInElevator(inElevator, inShip, grabbableObject);
			if (grabbableObject.transform.parent != null)
			{
				grabbableObject.startFallingPosition = grabbableObject.transform.parent.InverseTransformPoint(base.transform.position + Vector3.up * 0.07f);
			}
			grabbableObject.targetFloorPosition = targetPosition;
			grabbableObject.EnablePhysics(enable: true);
			BeltBagItem beltBagItem = grabbableObject as BeltBagItem;
			if (beltBagItem != null)
			{
				beltBagItem.insideAnotherBeltBag = null;
			}
		}
	}

	public override void Start()
	{
		base.Start();
		beltBagUI = Object.FindObjectOfType<BeltBagInventoryUI>(includeInactive: true);
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		hangingBeltTarget.position = base.transform.position - Vector3.up * 25f;
		if (!(currentPlayerChecking != null))
		{
			return;
		}
		if (currentPlayerChecking.isPlayerDead || !currentPlayerChecking.isPlayerControlled)
		{
			currentPlayerChecking = null;
			beltBagAnimator.SetBool("Opened", value: false);
		}
		else if (currentPlayerChecking == GameNetworkManager.Instance.localPlayerController)
		{
			bool flag = false;
			if (Vector3.Distance(currentPlayerChecking.transform.position, base.transform.position) > 4.25f || Vector3.Angle(currentPlayerChecking.transform.forward, base.transform.position - currentPlayerChecking.transform.position) > 120f)
			{
				flag = true;
			}
			else if (HUDManager.Instance.currentSpecialMenu != SpecialHUDMenu.BeltBagInventory)
			{
				flag = true;
			}
			if (flag)
			{
				StopCheckingBagLocalClient(isLocalClient: true);
				StopCheckingBagServerRpc();
			}
		}
	}

	public void TryCheckBagContents()
	{
		if (!tryingCheckBag && !(currentPlayerChecking != null) && placingItemsInBag <= 0 && !beltBagUI.displaying)
		{
			TryCheckingBagServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void TryCheckingBagServerRpc(int playerId)
{		{
			if (currentPlayerChecking == null && placingItemsInBag == 0)
			{
				currentPlayerChecking = StartOfRound.Instance.allPlayerScripts[playerId];
				CheckBagLocalClient();
				ConfirmCheckingBagClientRpc(playerId);
			}
			else
			{
				DenyCheckingBagClientRpc(playerId);
			}
		}
}
	[ClientRpc]
	public void ConfirmCheckingBagClientRpc(int playerId)
{if(!base.IsServer)			{
				currentPlayerChecking = StartOfRound.Instance.allPlayerScripts[playerId];
				CheckBagLocalClient();
			}
}
	public void CheckBagLocalClient()
	{
		beltBagAnimator.SetBool("Opened", value: true);
		RoundManager.PlayRandomClip(bagAudio, unzipBagSFX);
		if (GameNetworkManager.Instance.localPlayerController == currentPlayerChecking)
		{
			tryingCheckBag = false;
			beltBagUI.FillSlots(this);
			HUDManager.Instance.SetMouseCursorSprite(HUDManager.Instance.handOpenCursorTex, new Vector2(16f, 16f));
			GameNetworkManager.Instance.localPlayerController.SetInSpecialMenu(setInMenu: true);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopCheckingBagServerRpc()
{		{
			if (currentPlayerChecking != null)
			{
				StopCheckingBagLocalClient();
			}
			StopCheckingBagClientRpc();
		}
}
	[ClientRpc]
	public void StopCheckingBagClientRpc()
{if(currentPlayerChecking != null)			{
				StopCheckingBagLocalClient();
			}
}
	private void StopCheckingBagLocalClient(bool isLocalClient = false)
	{
		if (isLocalClient)
		{
			if (HUDManager.Instance.currentSpecialMenu == SpecialHUDMenu.BeltBagInventory)
			{
				currentPlayerChecking.SetInSpecialMenu(setInMenu: false);
			}
			beltBagUI.currentBeltBag = null;
			beltBagUI.grabbingItemSlot = -1;
		}
		bagAudio.PlayOneShot(zipUpBagSFX);
		WalkieTalkie.TransmitOneShotAudio(bagAudio, zipUpBagSFX);
		beltBagAnimator.SetBool("Opened", value: false);
		currentPlayerChecking = null;
	}

	[ClientRpc]
	public void DenyCheckingBagClientRpc(int playerId)
{if(GameNetworkManager.Instance.localPlayerController == StartOfRound.Instance.allPlayerScripts[playerId])			{
				tryingCheckBag = false;
			}
}
	public override void PocketItem()
	{
		if (base.IsOwner && playerHeldBy != null)
		{
			playerHeldBy.IsInspectingItem = false;
			playerHeldBy.equippedUsableItemQE = false;
		}
		isPocketed = true;
		wasPocketed = true;
		useBagTrigger.GetComponent<Collider>().enabled = true;
		base.gameObject.GetComponent<AudioSource>().PlayOneShot(itemProperties.pocketSFX, 1f);
		beltBagAnimator.SetBool("Buckled", value: true);
		parentObject = playerHeldBy.lowerTorsoCostumeContainerBeltBagOffset.transform;
		if (currentPlayerChecking == GameNetworkManager.Instance.localPlayerController)
		{
			StopCheckingBagLocalClient(isLocalClient: true);
			StopCheckingBagServerRpc();
		}
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		TryCheckBagContents();
	}

	public override void ItemInteractLeftRight(bool right)
	{
		base.ItemInteractLeftRight(right);
		if (playerHeldBy == null || tryingAddToBag || objectsInBag.Count >= 15 || right)
		{
			return;
		}
		Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward * 4f, Color.red, 2f);
		if (Physics.Raycast(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, out var hitInfo, 4f, 1073742144, QueryTriggerInteraction.Ignore))
		{
			GrabbableObject component = hitInfo.collider.gameObject.GetComponent<GrabbableObject>();
			if (!(component == null) && !(component == this) && !component.itemProperties.isScrap && !component.isHeld && !component.isHeldByEnemy && component.itemProperties.itemId != 123984 && component.itemProperties.itemId != 819501)
			{
				TryAddObjectToBag(component);
			}
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		if (wasPocketed)
		{
			wasPocketed = false;
			bagAudio.PlayOneShot(beltBagUnclipSFX);
			WalkieTalkie.TransmitOneShotAudio(bagAudio, beltBagUnclipSFX);
		}
		if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			parentObject = playerHeldBy.localItemHolder.transform;
			useBagTrigger.GetComponent<Collider>().enabled = false;
		}
		else
		{
			parentObject = playerHeldBy.serverItemHolder.transform;
		}
		beltBagAnimator.SetBool("Buckled", value: false);
		playerHeldBy.equippedUsableItemQE = true;
	}

	public override void DiscardItem()
	{
		if (playerHeldBy != null)
		{
			playerHeldBy.equippedUsableItemQE = false;
		}
		wasPocketed = false;
		beltBagAnimator.SetBool("Buckled", value: true);
		useBagTrigger.GetComponent<Collider>().enabled = true;
		if (currentPlayerChecking == GameNetworkManager.Instance.localPlayerController)
		{
			StopCheckingBagLocalClient(isLocalClient: true);
			StopCheckingBagServerRpc();
		}
		base.DiscardItem();
	}
}
