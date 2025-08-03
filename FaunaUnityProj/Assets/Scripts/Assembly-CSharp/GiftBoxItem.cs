using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class GiftBoxItem : GrabbableObject
{
	private GameObject objectInPresent;

	public ParticleSystem PoofParticle;

	public AudioSource presentAudio;

	public AudioClip openGiftAudio;

	private PlayerControllerB previousPlayerHeldBy;

	private bool hasUsedGift;

	private int objectInPresentValue;

	private Item objectInPresentItem;

	private bool loadedItemFromSave;

	public override void LoadItemSaveData(int saveData)
	{
		base.LoadItemSaveData(saveData);
		objectInPresentItem = StartOfRound.Instance.allItemsList.itemsList[saveData];
		objectInPresent = objectInPresentItem.spawnPrefab;
		System.Random random = new System.Random((int)targetFloorPosition.x + (int)targetFloorPosition.y);
		objectInPresentValue = (int)((float)random.Next(objectInPresentItem.minValue + 25, objectInPresentItem.maxValue + 35) * RoundManager.Instance.scrapValueMultiplier);
		loadedItemFromSave = true;
	}

	public override int GetItemDataToSave()
	{
		base.GetItemDataToSave();
		for (int i = 0; i < StartOfRound.Instance.allItemsList.itemsList.Count; i++)
		{
			if (StartOfRound.Instance.allItemsList.itemsList[i] == objectInPresentItem)
			{
				return i;
			}
		}
		return 0;
	}

	public override void InitializeAfterPositioning()
	{
		base.InitializeAfterPositioning();
		if (loadedItemFromSave)
		{
			return;
		}
		System.Random randomSeed = new System.Random((int)targetFloorPosition.x + (int)targetFloorPosition.y);
		System.Random random = new System.Random((int)targetFloorPosition.x + (int)targetFloorPosition.y);
		if (!base.IsServer)
		{
			return;
		}
		List<int> list = new List<int>(RoundManager.Instance.currentLevel.spawnableScrap.Count);
		for (int i = 0; i < RoundManager.Instance.currentLevel.spawnableScrap.Count; i++)
		{
			if (RoundManager.Instance.currentLevel.spawnableScrap[i].spawnableItem.itemId == 152767)
			{
				list.Add(0);
			}
			else
			{
				list.Add(RoundManager.Instance.currentLevel.spawnableScrap[i].rarity);
			}
		}
		int randomWeightedIndexList = RoundManager.Instance.GetRandomWeightedIndexList(list, randomSeed);
		objectInPresentItem = RoundManager.Instance.currentLevel.spawnableScrap[randomWeightedIndexList].spawnableItem;
		objectInPresent = objectInPresentItem.spawnPrefab;
		objectInPresentValue = (int)((float)random.Next(objectInPresentItem.minValue + 25, objectInPresentItem.maxValue + 35) * RoundManager.Instance.scrapValueMultiplier);
	}

	public override void EquipItem()
	{
		base.EquipItem();
		previousPlayerHeldBy = playerHeldBy;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (!(playerHeldBy == null) && !hasUsedGift)
		{
			hasUsedGift = true;
			playerHeldBy.activatingItem = true;
			OpenGiftBoxServerRpc();
		}
	}

	public override void PocketItem()
	{
		base.PocketItem();
		playerHeldBy.activatingItem = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void OpenGiftBoxServerRpc()
{		GameObject gameObject = null;
		int presentValue = 0;
		Vector3 vector = Vector3.zero;
		if (objectInPresent == null)
		{
			Debug.LogError("Error: There is no object in gift box!");
		}
		else
		{
			Transform parentOfGiftObject = GetParentOfGiftObject();
			vector = base.transform.position + Vector3.up * 0.25f;
			gameObject = UnityEngine.Object.Instantiate(objectInPresent, vector, Quaternion.identity, parentOfGiftObject);
			GrabbableObject component = gameObject.GetComponent<GrabbableObject>();
			component.startFallingPosition = vector;
			StartCoroutine(SetObjectToHitGroundSFX(component));
			component.targetFloorPosition = component.GetItemFloorPosition(base.transform.position);
			if (previousPlayerHeldBy != null && previousPlayerHeldBy.isInHangarShipRoom)
			{
				previousPlayerHeldBy.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, component);
			}
			presentValue = objectInPresentValue;
			component.SetScrapValue(presentValue);
			component.NetworkObject.Spawn();
		}
		if (gameObject != null)
		{
			OpenGiftBoxClientRpc(gameObject.GetComponent<NetworkObject>(), presentValue, vector);
		}
		OpenGiftBoxNoPresentClientRpc();
}
	private IEnumerator SetObjectToHitGroundSFX(GrabbableObject gObject)
	{
		yield return new WaitForEndOfFrame();
		Debug.Log("Setting " + gObject.itemProperties.itemName + " hit ground to false");
		gObject.reachedFloorTarget = false;
		gObject.hasHitGround = false;
		gObject.fallTime = 0f;
	}

	[ClientRpc]
	public void OpenGiftBoxNoPresentClientRpc()
{		{
			PoofParticle.Play();
			presentAudio.PlayOneShot(openGiftAudio);
			WalkieTalkie.TransmitOneShotAudio(presentAudio, openGiftAudio);
			RoundManager.Instance.PlayAudibleNoise(presentAudio.transform.position, 8f, 0.5f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
			if (playerHeldBy != null)
			{
				playerHeldBy.activatingItem = false;
				DestroyObjectInHand(playerHeldBy);
			}
		}
}
	[ClientRpc]
	public void OpenGiftBoxClientRpc(NetworkObjectReference netObjectRef, int presentValue, Vector3 startFallingPos)
{		{
			PoofParticle.Play();
			presentAudio.PlayOneShot(openGiftAudio);
			WalkieTalkie.TransmitOneShotAudio(presentAudio, openGiftAudio);
			RoundManager.Instance.PlayAudibleNoise(presentAudio.transform.position, 8f, 0.5f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
			if (playerHeldBy != null)
			{
				playerHeldBy.activatingItem = false;
				DestroyObjectInHand(playerHeldBy);
			}
			if (!base.IsServer)
			{
				StartCoroutine(waitForGiftPresentToSpawnOnClient(netObjectRef, presentValue, startFallingPos));
			}
		}
}
	private IEnumerator waitForGiftPresentToSpawnOnClient(NetworkObjectReference netObjectRef, int presentValue, Vector3 startFallingPos)
	{
		NetworkObject netObject = null;
		float startTime = Time.realtimeSinceStartup;
		while (Time.realtimeSinceStartup - startTime < 8f && !netObjectRef.TryGet(out netObject))
		{
			yield return new WaitForSeconds(0.03f);
		}
		if (netObject == null)
		{
			Debug.Log("Gift box: No network object found");
			yield break;
		}
		yield return new WaitForEndOfFrame();
		GrabbableObject component = netObject.GetComponent<GrabbableObject>();
		RoundManager.Instance.totalScrapValueInLevel -= scrapValue;
		RoundManager.Instance.totalScrapValueInLevel += component.scrapValue;
		component.SetScrapValue(presentValue);
		Transform parentOfGiftObject = GetParentOfGiftObject();
		component.transform.SetParent(parentOfGiftObject, worldPositionStays: true);
		component.targetFloorPosition = component.transform.parent.InverseTransformPoint(component.GetItemFloorPosition(startFallingPos));
		if (component.transform.parent != null)
		{
			startFallingPos = component.transform.parent.InverseTransformPoint(startFallingPos);
		}
		component.startFallingPosition = startFallingPos;
		component.fallTime = 0f;
		component.hasHitGround = false;
		component.reachedFloorTarget = false;
		if (previousPlayerHeldBy != null && previousPlayerHeldBy.isInHangarShipRoom)
		{
			previousPlayerHeldBy.SetItemInElevator(droppedInShipRoom: true, droppedInElevator: true, component);
		}
	}

	private Transform GetParentOfGiftObject()
	{
		if ((previousPlayerHeldBy != null && previousPlayerHeldBy.isInElevator) || StartOfRound.Instance.inShipPhase)
		{
			return StartOfRound.Instance.elevatorTransform;
		}
		return null;
	}
}
