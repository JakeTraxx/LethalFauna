using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Samples;
using UnityEngine;

public abstract class GrabbableObject : NetworkBehaviour
{
	public bool grabbable;

	public bool isHeld;

	public bool isHeldByEnemy;

	public bool deactivated;

	[Space(3f)]
	public Transform parentObject;

	public Vector3 targetFloorPosition;

	public Vector3 startFallingPosition;

	public int floorYRot;

	public float fallTime;

	public bool hasHitGround;

	[Space(5f)]
	public int scrapValue;

	public bool itemUsedUp;

	public PlayerControllerB playerHeldBy;

	public bool isPocketed;

	public bool isBeingUsed;

	public bool isInElevator;

	public bool isInShipRoom;

	public bool isInFactory = true;

	[Space(10f)]
	public float useCooldown;

	public float currentUseCooldown;

	[Space(10f)]
	public Item itemProperties;

	public Battery insertedBattery;

	public string customGrabTooltip;

	[HideInInspector]
	public Rigidbody propBody;

	[HideInInspector]
	public Collider[] propColliders;

	[HideInInspector]
	public Vector3 originalScale;

	public bool wasOwnerLastFrame;

	public MeshRenderer mainObjectRenderer;

	public int isSendingItemRPC;

	public bool scrapPersistedThroughRounds;

	public bool heldByPlayerOnServer;

	[HideInInspector]
	public Transform radarIcon;

	public bool reachedFloorTarget;

	[Space(3f)]
	public bool grabbableToEnemies = true;

	public bool hasBeenHeld;

	public bool rotateObject;

	public virtual int GetItemDataToSave()
	{
		if (!itemProperties.saveItemVariable)
		{
			Debug.LogError("GetItemDataToSave is being called on " + itemProperties.itemName + ", which does not have saveItemVariable set true.");
		}
		return 0;
	}

	public virtual void LoadItemSaveData(int saveData)
	{
		if (!itemProperties.saveItemVariable)
		{
			Debug.LogError("LoadItemSaveData is being called on " + itemProperties.itemName + ", which does not have saveItemVariable set true.");
		}
	}

	public virtual void InitializeAfterPositioning()
	{
	}

	public virtual void Start()
	{
		propColliders = base.gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!propColliders[i].CompareTag("InteractTrigger"))
			{
				propColliders[i].excludeLayers = -2621449;
			}
		}
		originalScale = base.transform.localScale;
		if (itemProperties.itemSpawnsOnGround)
		{
			RandomScrapSpawn[] array = UnityEngine.Object.FindObjectsOfType<RandomScrapSpawn>();
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].spawnWithParent != null && array[j].spawnWithParent.transform.position == base.transform.position)
				{
					base.transform.SetParent(array[j].spawnWithParent, worldPositionStays: true);
					break;
				}
			}
			Debug.DrawRay(base.transform.position, Vector3.up * 10f, Color.cyan, 10f);
			startFallingPosition = base.transform.position;
			if (base.transform.parent != null)
			{
				startFallingPosition = base.transform.parent.InverseTransformPoint(startFallingPosition);
			}
			Debug.DrawRay(base.transform.position, Vector3.up * 10f, Color.cyan, 10f);
			if (base.transform.parent != null)
			{
				Debug.Log($"Item spawning: {itemProperties.itemName} ; item parent : {base.transform.parent}");
			}
			else
			{
				Debug.Log("Item spawning: " + itemProperties.itemName + " ; item parent : null ");
			}
			FallToGround(randomizePosition: false, justSpawned: true, startFallingPosition);
		}
		else
		{
			fallTime = 1f;
			hasHitGround = true;
			reachedFloorTarget = true;
			targetFloorPosition = base.transform.localPosition;
		}
		if (itemProperties.isScrap)
		{
			fallTime = 1f;
			hasHitGround = true;
		}
		if (itemProperties.isScrap && RoundManager.Instance.mapPropsContainer != null)
		{
			radarIcon = UnityEngine.Object.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		else if (itemProperties.itemId == 14 && RoundManager.Instance.mapPropsContainer != null)
		{
			radarIcon = UnityEngine.Object.Instantiate(StartOfRound.Instance.keyRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		if (!itemProperties.isScrap)
		{
			HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
		}
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int k = 0; k < componentsInChildren.Length; k++)
		{
			componentsInChildren[k].renderingLayerMask = 1u;
		}
		SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int l = 0; l < componentsInChildren2.Length; l++)
		{
			componentsInChildren2[l].renderingLayerMask = 1u;
		}
	}

	private IEnumerator fallToGroundOnFrameDelay(Vector3 startPosition)
	{
		yield return null;
		if (!(startFallingPosition != startPosition))
		{
			FallToGround(randomizePosition: false, justSpawned: false, startPosition);
		}
	}

	public void FallToGround(bool randomizePosition = false, bool justSpawned = false, Vector3 overrideStartPos = default(Vector3))
	{
		Vector3 startPosition = base.transform.position;
		if (overrideStartPos != Vector3.zero)
		{
			startPosition = overrideStartPos;
		}
		if (justSpawned)
		{
			Debug.Log($"Start falling position: {startFallingPosition}");
			Debug.DrawRay(startFallingPosition, Vector3.up * 0.5f, Color.yellow, 20f);
			StartCoroutine(fallToGroundOnFrameDelay(startPosition));
			return;
		}
		fallTime = 0f;
		base.transform.localPosition = startFallingPosition;
		startPosition = base.transform.position;
		Debug.Log($"global startposition falltoground for object {base.gameObject.name}: {startPosition}");
		Debug.DrawRay(startPosition, Vector3.up * 1.5f, Color.blue, 25f);
		if (Physics.Raycast(startPosition, Vector3.down, out var hitInfo, 80f, 268437760, QueryTriggerInteraction.Ignore))
		{
			Debug.DrawRay(startPosition, Vector3.down * hitInfo.distance, Color.red, 20f, depthTest: true);
			Debug.Log("Item " + itemProperties.itemName + " landed on : " + hitInfo.collider.name + " / " + hitInfo.transform.gameObject.name);
			targetFloorPosition = hitInfo.point + itemProperties.verticalOffset * Vector3.up;
			if (base.transform.parent != null)
			{
				targetFloorPosition = base.transform.parent.InverseTransformPoint(targetFloorPosition);
			}
		}
		else
		{
			Debug.Log("dropping item did not get raycast : " + base.gameObject.name);
			targetFloorPosition = base.transform.localPosition;
		}
		if (randomizePosition)
		{
			targetFloorPosition += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0f, UnityEngine.Random.Range(-0.5f, 0.5f));
		}
		InitializeAfterPositioning();
	}

	public void EnablePhysics(bool enable)
	{
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!(propColliders[i] == null) && !propColliders[i].gameObject.CompareTag("InteractTrigger") && !propColliders[i].gameObject.CompareTag("DoNotSet") && !propColliders[i].gameObject.CompareTag("Enemy"))
			{
				propColliders[i].enabled = enable;
			}
		}
	}

	public virtual void InspectItem()
	{
		if (base.IsOwner && playerHeldBy != null && itemProperties.canBeInspected)
		{
			playerHeldBy.IsInspectingItem = !playerHeldBy.IsInspectingItem;
			HUDManager.Instance.SetNearDepthOfFieldEnabled(!playerHeldBy.IsInspectingItem);
		}
	}

	public virtual void InteractItem()
	{
	}

	public void GrabItemOnClient()
	{
		if (!base.IsOwner)
		{
			Debug.LogError("GrabItemOnClient was called but player was not the owner.");
			return;
		}
		SetControlTipsForItem();
		GrabItem();
		if (itemProperties.syncGrabFunction)
		{
			isSendingItemRPC++;
			GrabServerRpc();
		}
	}

	public virtual void SetControlTipsForItem()
	{
		HUDManager.Instance.ChangeControlTipMultiple(itemProperties.toolTips, holdingItem: true, itemProperties);
	}

	public virtual void GrabItem()
	{
	}

	public void UseItemOnClient(bool buttonDown = true)
	{
		if (!base.IsOwner)
		{
			Debug.Log("Can't use item; not owner");
		}
		else if (!RequireCooldown() && UseItemBatteries(!itemProperties.holdButtonUse, buttonDown))
		{
			if (itemProperties.syncUseFunction)
			{
				isSendingItemRPC++;
				ActivateItemServerRpc(isBeingUsed, buttonDown);
			}
			ItemActivate(isBeingUsed, buttonDown);
		}
	}

	public bool UseItemBatteries(bool isToggle, bool buttonDown = true)
	{
		if (itemProperties.requiresBattery && (insertedBattery == null || insertedBattery.empty))
		{
			return false;
		}
		if (itemProperties.itemIsTrigger)
		{
			insertedBattery.charge = Mathf.Clamp(insertedBattery.charge - itemProperties.batteryUsage, 0f, 1f);
			if (insertedBattery.charge <= 0f)
			{
				insertedBattery.empty = true;
			}
			isBeingUsed = false;
		}
		else if (itemProperties.automaticallySetUsingPower)
		{
			if (isToggle)
			{
				isBeingUsed = !isBeingUsed;
			}
			else
			{
				isBeingUsed = buttonDown;
			}
		}
		return true;
	}

	public virtual void ItemActivate(bool used, bool buttonDown = true)
	{
	}

	public void ItemInteractLeftRightOnClient(bool right)
	{
		if (!base.IsOwner)
		{
			Debug.Log("InteractLeftRight was called but player was not the owner.");
		}
		else if (!RequireCooldown() && UseItemBatteries(isToggle: true))
		{
			ItemInteractLeftRight(right);
			if (itemProperties.syncInteractLRFunction)
			{
				isSendingItemRPC++;
				InteractLeftRightServerRpc(right);
			}
		}
	}

	public virtual void ItemInteractLeftRight(bool right)
	{
	}

	public virtual void ActivatePhysicsTrigger(Collider other)
	{
	}

	public virtual void UseUpBatteries()
	{
		Debug.Log("Use up batteries on local client");
		isBeingUsed = false;
	}

	public virtual void GrabItemFromEnemy(EnemyAI enemy)
	{
	}

	public virtual void DiscardItemFromEnemy()
	{
	}

	public virtual void ChargeBatteries()
	{
	}

	public virtual void DestroyObjectInHand(PlayerControllerB playerHolding)
	{
		grabbable = false;
		grabbableToEnemies = false;
		deactivated = true;
		if (playerHolding != null)
		{
			playerHolding.activatingItem = false;
		}
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.Destroy(componentsInChildren[i]);
		}
		Collider[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<Collider>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			UnityEngine.Object.Destroy(componentsInChildren2[j]);
		}
		if (base.IsOwner && isHeld && !isPocketed && playerHolding != null && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			playerHeldBy.DiscardHeldObject();
		}
	}

	public virtual void EquipItem()
	{
		if (base.IsOwner)
		{
			HUDManager.Instance.ClearControlTips();
			SetControlTipsForItem();
		}
		rotateObject = false;
		EnableItemMeshes(enable: true);
		isPocketed = false;
		if (!hasBeenHeld)
		{
			hasBeenHeld = true;
			if (!isInShipRoom && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
			{
				RoundManager.Instance.valueOfFoundScrapItems += scrapValue;
			}
		}
	}

	public virtual void PocketItem()
	{
		if (base.IsOwner && playerHeldBy != null)
		{
			playerHeldBy.IsInspectingItem = false;
		}
		isPocketed = true;
		EnableItemMeshes(enable: false);
		base.gameObject.GetComponent<AudioSource>().PlayOneShot(itemProperties.pocketSFX, 1f);
	}

	public void DiscardItemOnClient()
	{
		if (base.IsOwner)
		{
			DiscardItem();
			HUDManager.Instance.ClearControlTips();
			SyncBatteryServerRpc((int)(insertedBattery.charge * 100f));
			if (itemProperties.syncDiscardFunction)
			{
				isSendingItemRPC++;
				DiscardItemServerRpc();
			}
		}
	}

	[ServerRpc]
	public void SyncBatteryServerRpc(int charge)
{		{
			SyncBatteryClientRpc(charge);
		}
}
	[ClientRpc]
	public void SyncBatteryClientRpc(int charge)
			{
				float num = (float)charge / 100f;
				insertedBattery = new Battery(num <= 0f, num);
				ChargeBatteries();
			}

	public virtual void DiscardItem()
	{
		if (base.IsOwner)
		{
			HUDManager.Instance.ClearControlTips();
			if (playerHeldBy != null)
			{
				playerHeldBy.IsInspectingItem = false;
				playerHeldBy.activatingItem = false;
			}
		}
		playerHeldBy = null;
	}

	public virtual void LateUpdate()
	{
		if (parentObject != null)
		{
			base.transform.rotation = parentObject.rotation;
			base.transform.Rotate(itemProperties.rotationOffset);
			base.transform.position = parentObject.position;
			Vector3 positionOffset = itemProperties.positionOffset;
			positionOffset = parentObject.rotation * positionOffset;
			base.transform.position += positionOffset;
		}
		if (rotateObject)
		{
			base.transform.Rotate(new Vector3(0f, Time.deltaTime * 60f, 0f), Space.World);
		}
		if (radarIcon != null)
		{
			radarIcon.position = base.transform.position;
		}
	}

	public virtual void FallWithCurve()
	{
		float num = startFallingPosition.y - targetFloorPosition.y;
		if (floorYRot == -1)
		{
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
		}
		else
		{
			base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z), Mathf.Clamp(14f * Time.deltaTime / num, 0f, 1f));
		}
		if (num > 5f)
		{
			base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurveNoBounce.Evaluate(fallTime));
		}
		else
		{
			base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, StartOfRound.Instance.objectFallToGroundCurve.Evaluate(fallTime));
		}
		fallTime += Mathf.Abs(Time.deltaTime * 6f / num);
	}

	public virtual void OnPlaceObject()
	{
	}

	public virtual void OnBroughtToShip()
	{
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
	}

	public virtual void Update()
	{
		if (currentUseCooldown >= 0f)
		{
			currentUseCooldown -= Time.deltaTime;
		}
		if (base.IsOwner)
		{
			if (isBeingUsed && itemProperties.requiresBattery)
			{
				if (insertedBattery.charge > 0f)
				{
					if (!itemProperties.itemIsTrigger)
					{
						insertedBattery.charge -= Time.deltaTime / itemProperties.batteryUsage;
					}
				}
				else if (!insertedBattery.empty)
				{
					insertedBattery.empty = true;
					if (isBeingUsed)
					{
						Debug.Log("Use up batteries local");
						isBeingUsed = false;
						UseUpBatteries();
						isSendingItemRPC++;
						UseUpItemBatteriesServerRpc();
					}
				}
			}
			if (!wasOwnerLastFrame)
			{
				wasOwnerLastFrame = true;
			}
		}
		else if (wasOwnerLastFrame)
		{
			wasOwnerLastFrame = false;
		}
		if (!isHeld && parentObject == null)
		{
			if (fallTime < 1f)
			{
				reachedFloorTarget = false;
				FallWithCurve();
				if (base.transform.localPosition.y - targetFloorPosition.y < 0.05f && !hasHitGround)
				{
					PlayDropSFX();
					OnHitGround();
				}
				return;
			}
			if (!reachedFloorTarget)
			{
				if (!hasHitGround)
				{
					PlayDropSFX();
					OnHitGround();
				}
				reachedFloorTarget = true;
				if (floorYRot == -1)
				{
					base.transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z);
				}
				else
				{
					base.transform.rotation = Quaternion.Euler(itemProperties.restingRotation.x, (float)(floorYRot + itemProperties.floorYOffset) + 90f, itemProperties.restingRotation.z);
				}
			}
			base.transform.localPosition = targetFloorPosition;
		}
		else if (isHeld || isHeldByEnemy)
		{
			reachedFloorTarget = false;
		}
	}

	public virtual void OnHitGround()
	{
	}

	public virtual void PlayDropSFX()
	{
		if (itemProperties.dropSFX != null)
		{
			AudioSource component = base.gameObject.GetComponent<AudioSource>();
			component.PlayOneShot(itemProperties.dropSFX);
			WalkieTalkie.TransmitOneShotAudio(component, itemProperties.dropSFX);
			if (base.IsOwner)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.5f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed, 941);
			}
		}
		hasHitGround = true;
	}

	public void SetScrapValue(int setValueTo)
	{
		scrapValue = setValueTo;
		ScanNodeProperties componentInChildren = base.gameObject.GetComponentInChildren<ScanNodeProperties>();
		if (componentInChildren == null)
		{
			Debug.LogError("Scan node is missing for item!: " + base.gameObject.name);
			return;
		}
		componentInChildren.subText = $"Value: ${setValueTo}";
		componentInChildren.scrapValue = setValueTo;
	}

	public bool RequireCooldown()
	{
		if (useCooldown > 0f)
		{
			if (itemProperties.holdButtonUse && isBeingUsed)
			{
				return false;
			}
			if (currentUseCooldown <= 0f)
			{
				currentUseCooldown = useCooldown;
				return false;
			}
			return true;
		}
		return false;
	}

	[ServerRpc(RequireOwnership = false)]
	private void InteractLeftRightServerRpc(bool right)
			{
				InteractLeftRightClientRpc(right);
			}

	[ClientRpc]
	private void InteractLeftRightClientRpc(bool right)
{if(!base.IsOwner)			{
				ItemInteractLeftRight(right);
			}
}
	[ServerRpc(RequireOwnership = false)]
	private void GrabServerRpc()
			{
				GrabClientRpc();
			}

	[ClientRpc]
	private void GrabClientRpc()
{if(!base.IsOwner)			{
				GrabItem();
			}
}
	[ServerRpc(RequireOwnership = false)]
	private void ActivateItemServerRpc(bool onOff, bool buttonDown)
			{
				ActivateItemClientRpc(onOff, buttonDown);
			}

	[ClientRpc]
	private void ActivateItemClientRpc(bool onOff, bool buttonDown)
{if(!base.IsOwner)			{
				Debug.Log($"Is being used set to {onOff} by RPC");
				isBeingUsed = onOff;
				ItemActivate(onOff, buttonDown);
			}
}
	[ServerRpc(RequireOwnership = false)]
	private void DiscardItemServerRpc()
			{
				DiscardItemClientRpc();
			}

	[ClientRpc]
	private void DiscardItemClientRpc()
{if(!base.IsOwner)			{
				DiscardItem();
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void UseUpItemBatteriesServerRpc()
			{
				UseUpItemBatteriesClientRpc();
			}

	[ClientRpc]
	private void UseUpItemBatteriesClientRpc()
{if(!base.IsOwner)			{
				UseUpBatteries();
			}
}
	[ServerRpc(RequireOwnership = false)]
	private void EquipItemServerRpc()
			{
				EquipItemClientRpc();
			}

	[ClientRpc]
	private void EquipItemClientRpc()
{if(!base.IsOwner)			{
				EquipItem();
			}
}
	[ServerRpc(RequireOwnership = false)]
	private void PocketItemServerRpc()
			{
				PocketItemClientRpc();
			}

	[ClientRpc]
	private void PocketItemClientRpc()
{if(!base.IsOwner)			{
				PocketItem();
			}
}
	public void ChangeOwnershipOfProp(ulong clientId)
	{
		ChangeOwnershipOfPropServerRpc(clientId);
	}

	[ServerRpc(RequireOwnership = false)]
	private void ChangeOwnershipOfPropServerRpc(ulong NewOwner)
{		try
		{
			base.gameObject.GetComponent<NetworkRigidbodyModifiable>().kinematicOnOwner = true;
			base.transform.SetParent(playerHeldBy.localItemHolder, worldPositionStays: true);
			base.gameObject.GetComponent<ClientNetworkTransform>().InLocalSpace = true;
			base.transform.localPosition = Vector3.zero;
			base.transform.localEulerAngles = Vector3.zero;
			playerHeldBy.grabSetParentServer = false;
			base.gameObject.GetComponent<NetworkObject>().ChangeOwnership(NewOwner);
		}
		catch (Exception arg)
		{
			Debug.Log($"Failed to transfer ownership of prop to client: {arg}");
		}
}
	public virtual void EnableItemMeshes(bool enable)
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger"))
			{
				componentsInChildren[i].enabled = enable;
			}
		}
		SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].enabled = enable;
			Debug.Log("DISABLING/ENABLING SKINNEDMESH: " + componentsInChildren2[j].gameObject.name);
		}
	}

	public Vector3 GetItemFloorPosition(Vector3 startPosition = default(Vector3))
	{
		if (startPosition == Vector3.zero)
		{
			startPosition = base.transform.position + Vector3.up * 0.15f;
		}
		if (Physics.Raycast(startPosition, -Vector3.up, out var hitInfo, 80f, 268437761, QueryTriggerInteraction.Ignore))
		{
			return hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up;
		}
		return startPosition;
	}

	public NetworkObject GetPhysicsRegionOfDroppedObject(PlayerControllerB playerDropping, out Vector3 hitPoint)
	{
		Transform transform = null;
		RaycastHit hitInfo;
		if (playerDropping != null && itemProperties.allowDroppingAheadOfPlayer)
		{
			Debug.DrawRay(playerDropping.transform.position + Vector3.up * 0.4f, playerDropping.gameplayCamera.transform.forward * 1.7f, Color.yellow, 1f);
			Ray ray = new Ray(playerDropping.transform.position + Vector3.up * 0.4f, playerDropping.gameplayCamera.transform.forward);
			Vector3 vector = ((!Physics.Raycast(ray, out hitInfo, 1.7f, 1342179585, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(1.7f) : ray.GetPoint(Mathf.Clamp(hitInfo.distance - 0.3f, 0.01f, 2f)));
			if (Physics.Raycast(vector, -Vector3.up, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(vector, -Vector3.up * 80f, Color.yellow, 2f);
				transform = hitInfo.collider.gameObject.transform;
			}
		}
		else
		{
			Ray ray = new Ray(base.transform.position, -Vector3.up);
			if (Physics.Raycast(ray, out hitInfo, 80f, 1342179585, QueryTriggerInteraction.Ignore))
			{
				Debug.DrawRay(base.transform.position, -Vector3.up * 80f, Color.blue, 2f);
				transform = hitInfo.collider.gameObject.transform;
			}
		}
		if (transform != null)
		{
			PlayerPhysicsRegion componentInChildren = transform.GetComponentInChildren<PlayerPhysicsRegion>();
			if (componentInChildren != null && componentInChildren.allowDroppingItems && componentInChildren.itemDropCollider.ClosestPoint(hitInfo.point) == hitInfo.point)
			{
				NetworkObject parentNetworkObject = componentInChildren.parentNetworkObject;
				if (parentNetworkObject != null)
				{
					Vector3 addPositionOffsetToItems = componentInChildren.addPositionOffsetToItems;
					hitPoint = componentInChildren.physicsTransform.InverseTransformPoint(hitInfo.point + Vector3.up * 0.04f + itemProperties.verticalOffset * Vector3.up + addPositionOffsetToItems);
					return parentNetworkObject;
				}
				Debug.LogError("Error: physics region transform does not have network object?: " + transform.gameObject.name);
			}
		}
		hitPoint = Vector3.zero;
		return null;
	}

	public virtual void ReactToSellingItemOnCounter()
	{
	}
}
