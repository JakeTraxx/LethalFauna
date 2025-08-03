using Unity.Netcode;
using UnityEngine;

public class SteamValveFixInteraction : NetworkBehaviour
{
	public SteamValveHazard steamValveMain;

	public void FixValve()
	{
		steamValveMain.FixValveLocalClient();
		if (base.IsServer)
		{
			FixValveClientRpc();
		}
		else
		{
			FixValveServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void FixValveServerRpc()
			{
				FixValveClientRpc();
			}

	[ClientRpc]
	public void FixValveClientRpc()
			{
				steamValveMain.FixValveLocalClient();
			}
}
