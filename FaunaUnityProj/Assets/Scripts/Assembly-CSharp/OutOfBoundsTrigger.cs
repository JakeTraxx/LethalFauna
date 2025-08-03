using GameNetcodeStuff;
using UnityEngine;

public class OutOfBoundsTrigger : MonoBehaviour
{
	private StartOfRound playersManager;

	public bool disableWhenRoundStarts;

	private void Start()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (disableWhenRoundStarts && !playersManager.inShipPhase)
		{
			return;
		}
		if (other.tag.StartsWith("PlayerRagdoll"))
		{
			DeadBodyInfo componentInParent = other.GetComponentInParent<DeadBodyInfo>();
			if (componentInParent != null)
			{
				componentInParent.timesOutOfBounds++;
				if (componentInParent.timesOutOfBounds > 2)
				{
					componentInParent.SetBodyPartsKinematic();
				}
				else
				{
					componentInParent.ResetRagdollPosition();
				}
			}
		}
		else
		{
			if (!(other.tag == "Player"))
			{
				return;
			}
			PlayerControllerB component = other.GetComponent<PlayerControllerB>();
			if (GameNetworkManager.Instance.localPlayerController != component)
			{
				return;
			}
			component.ResetFallGravity();
			if (!(component != null))
			{
				return;
			}
			if (!playersManager.shipDoorsEnabled)
			{
				playersManager.ForcePlayerIntoShip();
			}
			else if (component.isInsideFactory)
			{
				if (!StartOfRound.Instance.isChallengeFile)
				{
					component.KillPlayer(Vector3.zero, spawnBody: false);
				}
				else
				{
					component.TeleportPlayer(RoundManager.FindMainEntrancePosition(getTeleportPosition: true));
				}
			}
			else if (component.isInHangarShipRoom)
			{
				component.TeleportPlayer(playersManager.playerSpawnPositions[0].position);
			}
			else
			{
				component.TeleportPlayer(playersManager.outsideShipSpawnPosition.position);
			}
		}
	}
}
