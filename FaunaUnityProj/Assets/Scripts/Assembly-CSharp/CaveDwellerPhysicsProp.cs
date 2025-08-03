using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class CaveDwellerPhysicsProp : GrabbableObject
{
	public CaveDwellerAI caveDwellerScript;

	public PlayerControllerB previousPlayerHeldBy;

	private float timeSinceRockingBaby;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (!base.IsOwner)
		{
			return;
		}
		if (buttonDown)
		{
			if (Time.realtimeSinceStartup - timeSinceRockingBaby < 0.25f)
			{
				SetRockingBabyServerRpc(rockHard: true);
				caveDwellerScript.rockingBaby = 2;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 2);
			}
			else
			{
				SetRockingBabyServerRpc(rockHard: false);
				caveDwellerScript.rockingBaby = 1;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 1);
			}
			timeSinceRockingBaby = Time.realtimeSinceStartup;
		}
		else
		{
			playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 0);
			caveDwellerScript.rockingBaby = 0;
			StopRockingBabyServerRpc();
		}
	}

	[ServerRpc]
	public void SetRockingBabyServerRpc(bool rockHard)
{		{
			SetRockingBabyClientRpc(rockHard);
		}
}
	[ClientRpc]
	public void SetRockingBabyClientRpc(bool rockHard)
{if(!base.IsOwner)		{
			if (rockHard)
			{
				caveDwellerScript.rockingBaby = 2;
			}
			else
			{
				caveDwellerScript.rockingBaby = 1;
			}
		}
}
	[ServerRpc]
	public void StopRockingBabyServerRpc()
{		{
			StopRockingBabyClientRpc();
		}
}
	[ClientRpc]
	public void StopRockingBabyClientRpc()
{if(!base.IsOwner)			{
				caveDwellerScript.rockingBaby = 0;
			}
}
	public override void EquipItem()
	{
		base.EquipItem();
		Debug.Log("Equip item function");
		caveDwellerScript.PickUpBabyLocalClient();
		previousPlayerHeldBy = playerHeldBy;
		Debug.Log($"Baby prop script reached floor target Equipped : {reachedFloorTarget} ");
	}

	[ServerRpc(RequireOwnership = false)]
	public void DropBabyServerRpc(int playerId)
			{
				DropBabyClientRpc(playerId);
			}

	[ClientRpc]
	public void DropBabyClientRpc(int playerId)
{if(playerId != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)			{
				caveDwellerScript.DropBabyLocalClient();
			}
}
	public override void FallWithCurve()
	{
		if (caveDwellerScript.inSpecialAnimation)
		{
			base.FallWithCurve();
		}
	}

	public override void Start()
	{
		propColliders = base.gameObject.GetComponentsInChildren<Collider>();
		for (int i = 0; i < propColliders.Length; i++)
		{
			if (!propColliders[i].CompareTag("DoNotSet") && !propColliders[i].CompareTag("Enemy"))
			{
				propColliders[i].excludeLayers = -2621449;
			}
		}
		originalScale = base.transform.localScale;
		if (itemProperties.itemSpawnsOnGround)
		{
			startFallingPosition = base.transform.position;
			if (base.transform.parent != null)
			{
				startFallingPosition = base.transform.parent.InverseTransformPoint(startFallingPosition);
			}
			FallToGround();
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
			radarIcon = Object.Instantiate(StartOfRound.Instance.itemRadarIconPrefab, RoundManager.Instance.mapPropsContainer.transform).transform;
		}
		if (!itemProperties.isScrap)
		{
			HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
		}
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			componentsInChildren[j].renderingLayerMask = 1u;
		}
		SkinnedMeshRenderer[] componentsInChildren2 = base.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
		for (int k = 0; k < componentsInChildren2.Length; k++)
		{
			componentsInChildren2[k].renderingLayerMask = 1u;
		}
	}

	public override void Update()
	{
		if (isHeld && playerHeldBy == GameNetworkManager.Instance.localPlayerController && caveDwellerScript.rockingBaby > 0)
		{
			if (caveDwellerScript.rockingBaby < 2 && StartOfRound.Instance.fearLevel > 0.75f)
			{
				caveDwellerScript.rockingBaby = 2;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 2);
				SetRockingBabyServerRpc(rockHard: true);
			}
			else if (StartOfRound.Instance.fearLevel < 0.6f && caveDwellerScript.rockingBaby > 2)
			{
				caveDwellerScript.rockingBaby = 1;
				playerHeldBy.playerBodyAnimator.SetInteger("RockBaby", 1);
				SetRockingBabyServerRpc(rockHard: false);
			}
		}
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
			if (caveDwellerScript.inSpecialAnimation)
			{
				base.transform.localPosition = targetFloorPosition;
			}
		}
		else if (isHeld || isHeldByEnemy)
		{
			reachedFloorTarget = false;
		}
	}

	public override void LateUpdate()
	{
		if (caveDwellerScript.inSpecialAnimation && parentObject != null)
		{
			base.transform.rotation = parentObject.rotation;
			base.transform.Rotate(itemProperties.rotationOffset);
			base.transform.position = parentObject.position;
			Vector3 positionOffset = itemProperties.positionOffset;
			positionOffset = parentObject.rotation * positionOffset;
			base.transform.position += positionOffset;
		}
		if (radarIcon != null)
		{
			radarIcon.position = base.transform.position;
		}
	}

	public override void EnableItemMeshes(bool enable)
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (!componentsInChildren[i].gameObject.CompareTag("DoNotSet") && !componentsInChildren[i].gameObject.CompareTag("InteractTrigger") && !componentsInChildren[i].gameObject.CompareTag("Enemy"))
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

	public override void DiscardItem()
	{
		GameNetworkManager.Instance.localPlayerController.playerBodyAnimator.SetInteger("RockBaby", 0);
		Debug.Log("Discard function called");
		caveDwellerScript.DropBabyLocalClient();
		DropBabyServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		previousPlayerHeldBy = playerHeldBy;
		base.DiscardItem();
	}
}
