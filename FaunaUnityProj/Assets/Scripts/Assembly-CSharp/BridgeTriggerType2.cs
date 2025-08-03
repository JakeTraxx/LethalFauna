using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BridgeTriggerType2 : NetworkBehaviour
{
	private int timesTriggered;

	public AnimatedObjectTrigger animatedObjectTrigger;

	private bool bridgeFell;

	private void OnTriggerEnter(Collider other)
	{
		if (!bridgeFell)
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && GameNetworkManager.Instance.localPlayerController == component)
			{
				AddToBridgeInstabilityServerRpc();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void AddToBridgeInstabilityServerRpc()
{		{
			timesTriggered++;
			if (timesTriggered == 2)
			{
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
			if (timesTriggered >= 4)
			{
				bridgeFell = true;
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
		}
}}
