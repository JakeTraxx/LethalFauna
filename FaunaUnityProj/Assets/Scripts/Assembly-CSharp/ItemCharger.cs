using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ItemCharger : NetworkBehaviour
{
	public InteractTrigger triggerScript;

	public Animator chargeStationAnimator;

	private Coroutine chargeItemCoroutine;

	public AudioSource zapAudio;

	private float updateInterval;

	public void ChargeItem()
	{
		GrabbableObject currentlyHeldObjectServer = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer;
		if (!(currentlyHeldObjectServer == null) && currentlyHeldObjectServer.itemProperties.requiresBattery)
		{
			PlayChargeItemEffectServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			if (chargeItemCoroutine != null)
			{
				StopCoroutine(chargeItemCoroutine);
			}
			chargeItemCoroutine = StartCoroutine(chargeItemDelayed(currentlyHeldObjectServer));
		}
	}

	private void Update()
	{
		if (NetworkManager.Singleton == null)
		{
			return;
		}
		if (updateInterval > 1f)
		{
			updateInterval = 0f;
			if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
			{
				triggerScript.interactable = GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer != null && GameNetworkManager.Instance.localPlayerController.currentlyHeldObjectServer.itemProperties.requiresBattery;
			}
		}
		else
		{
			updateInterval += Time.deltaTime;
		}
	}

	private IEnumerator chargeItemDelayed(GrabbableObject itemToCharge)
	{
		zapAudio.Play();
		yield return new WaitForSeconds(0.75f);
		chargeStationAnimator.SetTrigger("zap");
		if (itemToCharge != null)
		{
			itemToCharge.insertedBattery = new Battery(isEmpty: false, 1f);
			itemToCharge.SyncBatteryServerRpc(100);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayChargeItemEffectServerRpc(int playerChargingItem)
			{
				PlayChargeItemEffectClientRpc(playerChargingItem);
			}

	[ClientRpc]
	public void PlayChargeItemEffectClientRpc(int playerChargingItem)
{if(!(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerChargingItem)		{
			if (chargeItemCoroutine != null)
			{
				StopCoroutine(chargeItemCoroutine);
			}
			chargeItemCoroutine = StartCoroutine(chargeItemDelayed(null));
		}
}}
