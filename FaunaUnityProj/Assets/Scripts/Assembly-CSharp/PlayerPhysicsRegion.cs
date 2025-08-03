using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class PlayerPhysicsRegion : MonoBehaviour
{
	public Transform physicsTransform;

	public NetworkObject parentNetworkObject;

	public bool allowDroppingItems;

	private float checkInterval;

	private bool hasLocalPlayer;

	public int priority;

	public bool disablePhysicsRegion;

	public Collider physicsCollider;

	public Collider itemDropCollider;

	public Vector3 addPositionOffsetToItems;

	public float maxTippingAngle = 180f;

	private bool removePlayerNextFrame;

	private void OnDestroy()
	{
		disablePhysicsRegion = true;
		if (StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(this))
		{
			StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(this);
		}
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].transform.parent == physicsTransform)
			{
				StartOfRound.Instance.allPlayerScripts[i].transform.SetParent(null);
				Debug.Log($"Player {i} setting parent null since physics region was destroyed");
			}
		}
		GrabbableObject[] componentsInChildren = physicsTransform.GetComponentsInChildren<GrabbableObject>();
		for (int j = 0; j < componentsInChildren.Length; j++)
		{
			if (RoundManager.Instance.mapPropsContainer != null)
			{
				componentsInChildren[j].transform.SetParent(RoundManager.Instance.mapPropsContainer.transform, worldPositionStays: true);
			}
			else
			{
				componentsInChildren[j].transform.SetParent(null, worldPositionStays: true);
			}
			if (!componentsInChildren[j].isHeld)
			{
				componentsInChildren[j].FallToGround();
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (GameNetworkManager.Instance.localPlayerController == null)
		{
			hasLocalPlayer = false;
			return;
		}
		string text = other.gameObject.tag;
		if (text.StartsWith("PlayerRagdoll") && other.gameObject.name == "spine.002")
		{
			PlayerControllerB playerControllerB = null;
			switch (text)
			{
			case "PlayerRagdoll":
				playerControllerB = StartOfRound.Instance.allPlayerScripts[0];
				break;
			case "PlayerRagdoll1":
				playerControllerB = StartOfRound.Instance.allPlayerScripts[1];
				break;
			case "PlayerRagdoll2":
				playerControllerB = StartOfRound.Instance.allPlayerScripts[2];
				break;
			case "PlayerRagdoll3":
				playerControllerB = StartOfRound.Instance.allPlayerScripts[3];
				break;
			}
			if (playerControllerB != null && playerControllerB.deadBody != null && !playerControllerB.deadBody.isParentedToPhysicsRegion)
			{
				playerControllerB.deadBody.SetPhysicsParent(physicsTransform, physicsCollider);
			}
		}
		else
		{
			if (!other.gameObject.CompareTag("Player"))
			{
				return;
			}
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if ((bool)component && !(component != GameNetworkManager.Instance.localPlayerController))
			{
				checkInterval = 0f;
				removePlayerNextFrame = false;
				if (StartOfRound.Instance.CurrentPlayerPhysicsRegions != null && !StartOfRound.Instance.CurrentPlayerPhysicsRegions.Contains(this))
				{
					StartOfRound.Instance.CurrentPlayerPhysicsRegions.Add(this);
					hasLocalPlayer = true;
				}
			}
		}
	}

	private bool IsPhysicsRegionActive()
	{
		if (Vector3.Angle(base.transform.up, Vector3.up) > maxTippingAngle)
		{
			return false;
		}
		if (disablePhysicsRegion)
		{
			return false;
		}
		return true;
	}

	private void Update()
	{
		physicsCollider.enabled = IsPhysicsRegionActive();
		if (!hasLocalPlayer)
		{
			return;
		}
		if (checkInterval > 0.15f)
		{
			if (!removePlayerNextFrame)
			{
				removePlayerNextFrame = true;
				return;
			}
			removePlayerNextFrame = false;
			checkInterval = 0f;
			hasLocalPlayer = false;
			StartOfRound.Instance.CurrentPlayerPhysicsRegions.Remove(this);
		}
		else
		{
			checkInterval += Time.deltaTime;
		}
	}
}
