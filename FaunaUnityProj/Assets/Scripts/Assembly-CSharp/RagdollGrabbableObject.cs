using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class RagdollGrabbableObject : GrabbableObject
{
	public NetworkVariable<int> bodyID = new NetworkVariable<int>(0);

	public DeadBodyInfo ragdoll;

	private bool foundRagdollObject;

	private bool bodySetToHold;

	public bool testBody;

	private bool setBodyInElevator;

	private PlayerControllerB previousPlayerHeldBy;

	private bool hasBeenPlaced;

	public bool heldByEnemy;

	private bool heldByEnemyThisFrame;

	public override void Start()
	{
		base.Start();
		if (HoarderBugAI.grabbableObjectsInMap != null && !HoarderBugAI.grabbableObjectsInMap.Contains(base.gameObject))
		{
			HoarderBugAI.grabbableObjectsInMap.Add(base.gameObject);
		}
		if (radarIcon != null)
		{
			UnityEngine.Object.Destroy(radarIcon.gameObject);
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		previousPlayerHeldBy = playerHeldBy;
		hasBeenPlaced = false;
	}

	public override void OnPlaceObject()
	{
		base.OnPlaceObject();
		hasBeenPlaced = true;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (foundRagdollObject && ragdoll != null)
		{
			UnityEngine.Object.Destroy(ragdoll.gameObject);
		}
	}

	public override void Update()
	{
		base.Update();
		if (NetworkManager.Singleton.ShutdownInProgress || bodyID.Value == -1)
		{
			return;
		}
		if (!foundRagdollObject)
		{
			if (testBody)
			{
				DeadBodyInfo[] array = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].playerObjectId == 0)
					{
						ragdoll = array[i];
						break;
					}
				}
				ragdoll.grabBodyObject = this;
				parentObject = ragdoll.bodyParts[5].transform;
				base.transform.SetParent(ragdoll.bodyParts[5].transform);
				foundRagdollObject = true;
			}
			else
			{
				if (!(StartOfRound.Instance.allPlayerScripts[bodyID.Value].deadBody != null))
				{
					return;
				}
				ragdoll = StartOfRound.Instance.allPlayerScripts[bodyID.Value].deadBody;
				ragdoll.grabBodyObject = this;
				parentObject = ragdoll.bodyParts[5].transform;
				base.transform.SetParent(ragdoll.bodyParts[5].transform);
				foundRagdollObject = true;
			}
		}
		if (ragdoll == null)
		{
			return;
		}
		bool flag = hasBeenPlaced && StartOfRound.Instance.currentLevel.levelID == 3 && UnityEngine.Object.FindObjectOfType<DepositItemsDesk>() != null && base.transform.parent == UnityEngine.Object.FindObjectOfType<DepositItemsDesk>().deskObjectsContainer.transform;
		if (isHeld || heldByEnemy || flag)
		{
			if (flag)
			{
				ragdoll.matchPositionExactly = false;
				ragdoll.attachedLimb.isKinematic = false;
				ragdoll.speedMultiplier = 45f;
				ragdoll.maxVelocity = 0.75f;
			}
			if (!bodySetToHold)
			{
				if (heldByEnemy)
				{
					heldByEnemyThisFrame = true;
				}
				else
				{
					ragdoll.bodyBleedingHeavily = false;
				}
				grabbableToEnemies = false;
				bodySetToHold = true;
				ragdoll.gameObject.SetActive(value: true);
				ragdoll.SetBodyPartsKinematic(setKinematic: false);
				ragdoll.attachedTo = base.transform;
				ragdoll.attachedLimb = ragdoll.bodyParts[5];
				ragdoll.matchPositionExactly = true;
				ragdoll.lerpBeforeMatchingPosition = true;
				SetRagdollParentToMatchHoldingPlayer();
			}
		}
		else if (bodySetToHold)
		{
			bodySetToHold = false;
			grabbableToEnemies = true;
			ragdoll.attachedTo = null;
			parentObject = ragdoll.bodyParts[5].transform;
			base.transform.SetParent(ragdoll.bodyParts[5].transform);
			ragdoll.attachedLimb = null;
			ragdoll.matchPositionExactly = false;
			ragdoll.lerpBeforeMatchingPosition = false;
			SetRagdollParentToMatchHoldingPlayer();
			heldByEnemyThisFrame = false;
		}
	}

	public override void GrabItemFromEnemy(EnemyAI enemy)
	{
		base.GrabItemFromEnemy(enemy);
		heldByEnemy = true;
	}

	public override void DiscardItemFromEnemy()
	{
		base.DiscardItemFromEnemy();
		heldByEnemy = false;
	}

	private void SetRagdollParentToMatchHoldingPlayer()
	{
		if (!heldByEnemyThisFrame && previousPlayerHeldBy != null)
		{
			if (previousPlayerHeldBy.isInElevator && !setBodyInElevator)
			{
				setBodyInElevator = true;
				ragdoll.transform.SetParent(StartOfRound.Instance.elevatorTransform);
			}
			else if (!previousPlayerHeldBy.isInElevator && setBodyInElevator)
			{
				setBodyInElevator = false;
				ragdoll.transform.SetParent(null);
			}
		}
	}
}
