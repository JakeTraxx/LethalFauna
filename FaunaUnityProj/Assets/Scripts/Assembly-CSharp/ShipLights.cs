using Unity.Netcode;
using UnityEngine;

public class ShipLights : NetworkBehaviour
{
	public bool areLightsOn = true;

	public Animator shipLightsAnimator;

	[ServerRpc(RequireOwnership = false)]
	public void SetShipLightsServerRpc(bool setLightsOn)
			{
				SetShipLightsClientRpc(setLightsOn);
			}

	[ClientRpc]
	public void SetShipLightsClientRpc(bool setLightsOn)
			{
				areLightsOn = setLightsOn;
				shipLightsAnimator.SetBool("lightsOn", areLightsOn);
				Debug.Log($"Received set ship lights RPC. Lights on?: {areLightsOn}");
			}

	public void ToggleShipLights()
	{
		areLightsOn = !areLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		SetShipLightsServerRpc(areLightsOn);
		Debug.Log($"Toggling ship lights RPC. lights now: {areLightsOn}");
	}

	public void SetShipLightsBoolean(bool setLights)
	{
		areLightsOn = setLights;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		SetShipLightsServerRpc(areLightsOn);
		Debug.Log($"Calling ship lights boolean RPC: {areLightsOn}");
	}

	public void ToggleShipLightsOnLocalClientOnly()
	{
		areLightsOn = !areLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		Debug.Log($"Set ship lights on client only: {areLightsOn}");
	}

	public void SetShipLightsOnLocalClientOnly(bool setLightsOn)
	{
		areLightsOn = setLightsOn;
		shipLightsAnimator.SetBool("lightsOn", areLightsOn);
		Debug.Log($"Set ship lights on client only: {areLightsOn}");
	}
}
