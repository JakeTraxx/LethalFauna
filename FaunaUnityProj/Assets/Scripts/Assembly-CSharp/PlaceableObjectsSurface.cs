using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class PlaceableObjectsSurface : NetworkBehaviour
{
	public NetworkObject parentTo;

	public Collider placeableBounds;

	public InteractTrigger triggerScript;

	private float checkHoverTipInterval;

	private void Update()
	{
		if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
		{
			triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.isHoldingObject;
		}
	}

	public void PlaceObject(PlayerControllerB playerWhoTriggered)
	{
		if (playerWhoTriggered.isHoldingObject && !playerWhoTriggered.isGrabbingObjectAnimation && playerWhoTriggered.currentlyHeldObjectServer != null)
		{
			Debug.Log("Placing object in storage; asking server for verification");
			if (!(itemPlacementPosition(playerWhoTriggered.gameplayCamera.transform, playerWhoTriggered.currentlyHeldObjectServer) == Vector3.zero))
			{
				CheckIfFurnitureIsAvailableForPlacingServerRpc((int)playerWhoTriggered.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void CheckIfFurnitureIsAvailableForPlacingServerRpc(int playerWhoTriggered)
{if(parentTo.IsSpawned)		{
			PlaceableShipObject componentInChildren = parentTo.gameObject.GetComponentInChildren<PlaceableShipObject>();
			if (componentInChildren != null)
			{
				componentInChildren.lastTimeScrapWasPlaced = Time.realtimeSinceStartup;
			}
			ConfirmPlaceOnFurnitureClientRpc(playerWhoTriggered);
		}
}
	[ClientRpc]
	public void ConfirmPlaceOnFurnitureClientRpc(int playerId)
{if((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerId)		{
			return;
		}
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		if (!localPlayerController.isHoldingObject || localPlayerController.isGrabbingObjectAnimation || !(localPlayerController.currentlyHeldObjectServer != null))
		{
			return;
		}
		Debug.Log("Placing object in storage");
		Vector3 vector = itemPlacementPosition(localPlayerController.gameplayCamera.transform, localPlayerController.currentlyHeldObjectServer);
		if (!(vector == Vector3.zero))
		{
			if (parentTo != null)
			{
				vector = parentTo.transform.InverseTransformPoint(vector);
			}
			localPlayerController.DiscardHeldObject(placeObject: true, parentTo, vector, matchRotationOfParent: false);
			Debug.Log("discard held object called from placeobject");
		}
}
	private Vector3 itemPlacementPosition(Transform gameplayCamera, GrabbableObject heldObject)
	{
		if (Physics.Raycast(gameplayCamera.position, gameplayCamera.forward, out var hitInfo, 7f, 1073744640, QueryTriggerInteraction.Ignore))
		{
			if (placeableBounds.ClosestPoint(hitInfo.point) == hitInfo.point)
			{
				Debug.DrawLine(hitInfo.point, hitInfo.point + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset, Color.red, 10f);
				return hitInfo.point + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset;
			}
			Debug.DrawLine(placeableBounds.ClosestPoint(hitInfo.point), placeableBounds.ClosestPoint(hitInfo.point) + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset, Color.green, 10f);
			return placeableBounds.ClosestPoint(hitInfo.point) + placeableBounds.transform.up * heldObject.itemProperties.verticalOffset;
		}
		return Vector3.zero;
	}
}
