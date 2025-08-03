using System;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class UnlockableSuit : NetworkBehaviour
{
	public NetworkVariable<int> syncedSuitID = new NetworkVariable<int>(-1);

	public int suitID = -1;

	public Material suitMaterial;

	public SkinnedMeshRenderer suitRenderer;

	private void Update()
	{
		if (!(GameNetworkManager.Instance == null) && !(NetworkManager.Singleton == null) && !NetworkManager.Singleton.ShutdownInProgress && suitID != syncedSuitID.Value)
		{
			suitID = syncedSuitID.Value;
			suitMaterial = StartOfRound.Instance.unlockablesList.unlockables[suitID].suitMaterial;
			suitRenderer.material = suitMaterial;
			base.gameObject.GetComponent<InteractTrigger>().hoverTip = "Change: " + StartOfRound.Instance.unlockablesList.unlockables[suitID].unlockableName;
		}
	}

	public void SwitchSuitToThis(PlayerControllerB playerWhoTriggered = null)
	{
		if (playerWhoTriggered == null)
		{
			playerWhoTriggered = GameNetworkManager.Instance.localPlayerController;
		}
		if (playerWhoTriggered.currentSuitID != suitID)
		{
			SwitchSuitForPlayer(playerWhoTriggered, suitID);
			SwitchSuitServerRpc((int)playerWhoTriggered.playerClientId);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchSuitServerRpc(int playerID)
			{
				SwitchSuitClientRpc(playerID);
			}

	[ClientRpc]
	public void SwitchSuitClientRpc(int playerID)
{if((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerID)			{
				SwitchSuitForPlayer(StartOfRound.Instance.allPlayerScripts[playerID], suitID);
			}
}
	public static void SwitchSuitForAllPlayers(int suitID, bool playAudio = false)
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			Material material = StartOfRound.Instance.unlockablesList.unlockables[suitID].suitMaterial;
			PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[i];
			if (playAudio && playerControllerB.currentSuitID != suitID)
			{
				playerControllerB.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
			}
			playerControllerB.thisPlayerModel.material = material;
			playerControllerB.thisPlayerModelLOD1.material = material;
			playerControllerB.thisPlayerModelLOD2.material = material;
			playerControllerB.thisPlayerModelArms.material = material;
			playerControllerB.currentSuitID = suitID;
		}
	}

	public static void SwitchSuitForPlayer(PlayerControllerB player, int suitID, bool playAudio = true)
	{
		if (playAudio && player.currentSuitID != suitID)
		{
			player.movementAudio.PlayOneShot(StartOfRound.Instance.changeSuitSFX);
		}
		Material material = StartOfRound.Instance.unlockablesList.unlockables[suitID].suitMaterial;
		player.thisPlayerModel.material = material;
		player.thisPlayerModelLOD1.material = material;
		player.thisPlayerModelLOD2.material = material;
		player.thisPlayerModelArms.material = material;
		if (GameNetworkManager.Instance.localPlayerController != player)
		{
			ChangePlayerCostumeElement(player.headCostumeContainer, StartOfRound.Instance.unlockablesList.unlockables[suitID].headCostumeObject);
			ChangePlayerCostumeElement(player.lowerTorsoCostumeContainer, StartOfRound.Instance.unlockablesList.unlockables[suitID].lowerTorsoCostumeObject);
		}
		else
		{
			ChangePlayerCostumeElement(player.headCostumeContainerLocal, StartOfRound.Instance.unlockablesList.unlockables[suitID].headCostumeObject);
		}
		player.currentSuitID = suitID;
	}

	public static void ChangePlayerCostumeElement(Transform costumeContainer, GameObject newCostume)
	{
		foreach (Transform item in costumeContainer)
		{
			if (!item.CompareTag("DoNotSet"))
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		if (!(newCostume == null))
		{
			UnityEngine.Object.Instantiate(newCostume, costumeContainer.transform.position, costumeContainer.transform.rotation, costumeContainer.transform);
		}
	}
}
