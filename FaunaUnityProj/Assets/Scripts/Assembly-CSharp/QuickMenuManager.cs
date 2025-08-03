using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dissonance;
using Steamworks;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class QuickMenuManager : MonoBehaviour
{
	[Header("HUD")]
	public TextMeshProUGUI interactTipText;

	public TextMeshProUGUI leaveGameClarificationText;

	public Image cursorIcon;

	[Header("In-game Menu")]
	public GameObject menuContainer;

	public GameObject mainButtonsPanel;

	public GameObject leaveGameConfirmPanel;

	public GameObject settingsPanel;

	[Space(3f)]
	public GameObject ConfirmKickUserPanel;

	public TextMeshProUGUI ConfirmKickPlayerText;

	public GameObject KeybindsPanel;

	public bool isMenuOpen;

	private int currentMicrophoneDevice;

	public TextMeshProUGUI currentMicrophoneText;

	public DissonanceComms voiceChatModule;

	public TextMeshProUGUI changesNotAppliedText;

	public TextMeshProUGUI settingsBackButton;

	public GameObject PleaseConfirmChangesSettingsPanel;

	public Button PleaseConfirmChangesSettingsPanelBackButton;

	public CanvasGroup inviteFriendsTextAlpha;

	[Header("Player list")]
	public PlayerListSlot[] playerListSlots;

	public GameObject playerListPanel;

	private int playerObjToKick;

	[Header("Debug menu")]
	public GameObject[] doorGameObjects;

	public Collider outOfBoundsCollider;

	public GameObject debugMenuUI;

	public SelectableLevel testAllEnemiesLevel;

	[Space(3f)]
	private int enemyToSpawnId;

	[Space(3f)]
	private int enemyTypeId;

	[Space(3f)]
	private int itemToSpawnId;

	[Space(3f)]
	private int numberEnemyToSpawn = 1;

	public Transform[] debugEnemySpawnPositions;

	public TMP_Dropdown debugEnemyDropdown;

	public TMP_Dropdown allItemsDropdown;

	public GameObject truckPrefab;

	private void Start()
	{
		currentMicrophoneDevice = PlayerPrefs.GetInt("LethalCompany_currentMic", 0);
		if (!(NetworkManager.Singleton == null) && NetworkManager.Singleton.IsServer)
		{
			Debug_SetEnemyDropdownOptions();
			Debug_SetAllItemsDropdownOptions();
		}
	}

	public void Debug_SetAllItemsDropdownOptions()
	{
		allItemsDropdown.ClearOptions();
		List<string> list = new List<string>();
		for (int i = 0; i < StartOfRound.Instance.allItemsList.itemsList.Count; i++)
		{
			list.Add(StartOfRound.Instance.allItemsList.itemsList[i].itemName);
		}
		allItemsDropdown.AddOptions(list);
	}

	public void Debug_SpawnItem()
	{
		if (Application.isEditor && NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.IsServer)
		{
			GameObject obj = UnityEngine.Object.Instantiate(StartOfRound.Instance.allItemsList.itemsList[itemToSpawnId].spawnPrefab, debugEnemySpawnPositions[3].position, Quaternion.identity, StartOfRound.Instance.propsContainer);
			obj.GetComponent<GrabbableObject>().fallTime = 0f;
			obj.GetComponent<NetworkObject>().Spawn();
		}
	}

	public void Debug_SetItemToSpawn(int itemId)
	{
		itemToSpawnId = itemId;
	}

	public void Debug_KillLocalPlayer()
	{
		if (Application.isEditor && NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.IsServer)
		{
			GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.zero);
		}
	}

	public void Debug_SpawnTruck()
	{
		if (Application.isEditor && NetworkManager.Singleton.IsConnectedClient && NetworkManager.Singleton.IsServer)
		{
			UnityEngine.Object.Instantiate(truckPrefab, StartOfRound.Instance.groundOutsideShipSpawnPosition.position, Quaternion.identity, RoundManager.Instance.VehiclesContainer).gameObject.GetComponent<NetworkObject>().Spawn();
		}
	}

	public void Debug_ToggleTestRoom()
	{
		if (Application.isEditor)
		{
			StartOfRound.Instance.Debug_EnableTestRoomServerRpc(StartOfRound.Instance.testRoom == null);
		}
	}

	public void Debug_ToggleAllowDeath()
	{
		if (Application.isEditor)
		{
			StartOfRound.Instance.Debug_ToggleAllowDeathServerRpc();
		}
	}

	public void Debug_RevivePlayers()
	{
		if (CanEnableDebugMenu())
		{
			StartOfRound.Instance.Debug_EnableTestRoomServerRpc(enable: false);
			RoundManager.Instance.DespawnPropsAtEndOfRound();
			StartOfRound.Instance.ReviveDeadPlayers();
			HUDManager.Instance.HideHUD(hide: false);
			StartOfRound.Instance.Debug_ReviveAllPlayersServerRpc();
		}
	}

	public void Debug_SetEnemyType(int enemyType)
	{
		enemyTypeId = enemyType;
		Debug_SetEnemyDropdownOptions();
	}

	private void Debug_SetEnemyDropdownOptions()
	{
		debugEnemyDropdown.ClearOptions();
		List<string> list = new List<string>();
		switch (enemyTypeId)
		{
		case 0:
		{
			for (int j = 0; j < testAllEnemiesLevel.Enemies.Count; j++)
			{
				list.Add(testAllEnemiesLevel.Enemies[j].enemyType.enemyName);
			}
			break;
		}
		case 1:
		{
			for (int k = 0; k < testAllEnemiesLevel.OutsideEnemies.Count; k++)
			{
				list.Add(testAllEnemiesLevel.OutsideEnemies[k].enemyType.enemyName);
			}
			break;
		}
		case 2:
		{
			for (int i = 0; i < testAllEnemiesLevel.DaytimeEnemies.Count; i++)
			{
				list.Add(testAllEnemiesLevel.DaytimeEnemies[i].enemyType.enemyName);
			}
			break;
		}
		}
		debugEnemyDropdown.AddOptions(list);
		Debug_SetEnemyToSpawn(0);
	}

	public void Debug_SetEnemyToSpawn(int enemyId)
	{
		enemyToSpawnId = enemyId;
	}

	public void Debug_SetNumberToSpawn(string numString)
	{
		numString = Regex.Replace(numString, "[^.0-9]", "");
		int num = Convert.ToInt32(numString);
		if (num > 0)
		{
			numberEnemyToSpawn = num;
		}
	}

	public void Debug_SpawnEnemy()
	{
		if (!NetworkManager.Singleton.IsConnectedClient || !NetworkManager.Singleton.IsServer || !Application.isEditor)
		{
			return;
		}
		EnemyType enemyType = null;
		Vector3 spawnPosition = Vector3.zero;
		switch (enemyTypeId)
		{
		case 0:
			enemyType = testAllEnemiesLevel.Enemies[enemyToSpawnId].enemyType;
			spawnPosition = ((!(StartOfRound.Instance.testRoom != null)) ? RoundManager.Instance.insideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.insideAINodes.Length)].transform.position : debugEnemySpawnPositions[enemyTypeId].position);
			break;
		case 1:
			enemyType = testAllEnemiesLevel.OutsideEnemies[enemyToSpawnId].enemyType;
			spawnPosition = ((!(StartOfRound.Instance.testRoom != null)) ? RoundManager.Instance.outsideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.outsideAINodes.Length)].transform.position : debugEnemySpawnPositions[enemyTypeId].position);
			break;
		case 2:
			enemyType = testAllEnemiesLevel.DaytimeEnemies[enemyToSpawnId].enemyType;
			spawnPosition = ((!(StartOfRound.Instance.testRoom != null)) ? RoundManager.Instance.outsideAINodes[UnityEngine.Random.Range(0, RoundManager.Instance.outsideAINodes.Length)].transform.position : debugEnemySpawnPositions[enemyTypeId].position);
			break;
		}
		if (!(enemyType == null))
		{
			for (int i = 0; i < numberEnemyToSpawn && i <= 50; i++)
			{
				RoundManager.Instance.SpawnEnemyGameObject(spawnPosition, 0f, -1, enemyType);
			}
		}
	}

	private bool CanEnableDebugMenu()
	{
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
		{
			return Application.isEditor;
		}
		return false;
	}

	public void OpenQuickMenu()
	{
		menuContainer.SetActive(value: true);
		Cursor.lockState = CursorLockMode.None;
		Cursor.SetCursor(HUDManager.Instance.defaultCursorTex, new Vector2(0f, 0f), CursorMode.Auto);
		if (!StartOfRound.Instance.localPlayerUsingController)
		{
			Cursor.visible = true;
		}
		isMenuOpen = true;
		playerListPanel.SetActive(NonHostPlayerSlotsEnabled());
		debugMenuUI.SetActive(CanEnableDebugMenu());
	}

	public void CloseQuickMenu()
	{
		if (settingsPanel.activeSelf)
		{
			IngamePlayerSettings.Instance.DiscardChangedSettings();
		}
		CloseQuickMenuPanels();
		menuContainer.SetActive(value: false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		isMenuOpen = false;
	}

	public void CloseQuickMenuPanels()
	{
		leaveGameConfirmPanel.SetActive(value: false);
		settingsPanel.SetActive(value: false);
		mainButtonsPanel.SetActive(value: true);
		playerListPanel.SetActive(NonHostPlayerSlotsEnabled());
	}

	public void DisableInviteFriendsButton()
	{
		inviteFriendsTextAlpha.alpha = 0.2f;
	}

	public void InviteFriendsButton()
	{
		if (!GameNetworkManager.Instance.gameHasStarted)
		{
			GameNetworkManager.Instance.InviteFriendsUI();
		}
	}

	public void LeaveGame()
	{
		playerListPanel.SetActive(value: false);
		leaveGameConfirmPanel.SetActive(value: true);
		mainButtonsPanel.SetActive(value: false);
		leaveGameClarificationText.enabled = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && !StartOfRound.Instance.inShipPhase;
	}

	public void LeaveGameConfirm()
	{
		if (GameNetworkManager.Instance != null && !HUDManager.Instance.retrievingSteamLeaderboard)
		{
			GameNetworkManager.Instance.Disconnect();
		}
	}

	public void EnableUIPanel(GameObject enablePanel)
	{
		enablePanel.SetActive(value: true);
		playerListPanel.SetActive(value: false);
		debugMenuUI.SetActive(value: false);
	}

	public void DisableUIPanel(GameObject enablePanel)
	{
		enablePanel.SetActive(value: false);
		if (enablePanel != mainButtonsPanel)
		{
			playerListPanel.SetActive(NonHostPlayerSlotsEnabled());
			debugMenuUI.SetActive(CanEnableDebugMenu());
		}
	}

	private void Update()
	{
		for (int i = 0; i < playerListSlots.Length; i++)
		{
			if (playerListSlots[i].isConnected)
			{
				float num = playerListSlots[i].volumeSlider.value / playerListSlots[i].volumeSlider.maxValue;
				if (num == -1f)
				{
					SoundManager.Instance.playerVoiceVolumes[i] = -70f;
				}
				else
				{
					SoundManager.Instance.playerVoiceVolumes[i] = num;
				}
			}
		}
	}

	private bool NonHostPlayerSlotsEnabled()
	{
		for (int i = 1; i < playerListSlots.Length; i++)
		{
			if (playerListSlots[i].isConnected)
			{
				return true;
			}
		}
		return false;
	}

	public void AddUserToPlayerList(ulong steamId, string playerName, int playerObjectId)
	{
		if (playerObjectId >= 0 && playerObjectId <= 4)
		{
			playerListSlots[playerObjectId].KickUserButton.SetActive(StartOfRound.Instance.IsServer);
			playerListSlots[playerObjectId].slotContainer.SetActive(value: true);
			playerListSlots[playerObjectId].isConnected = true;
			playerListSlots[playerObjectId].playerSteamId = steamId;
			playerListSlots[playerObjectId].usernameHeader.text = playerName;
			if (GameNetworkManager.Instance.localPlayerController != null)
			{
				playerListSlots[playerObjectId].volumeSliderContainer.SetActive(playerObjectId != (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	public void KickUserFromServer(int playerObjId)
	{
		ConfirmKickPlayerText.text = "Kick out " + StartOfRound.Instance.allPlayerScripts[playerObjId].playerUsername.Substring(0, Mathf.Min(6, StartOfRound.Instance.allPlayerScripts[playerObjId].playerUsername.Length - 1)) + "?";
		playerObjToKick = playerObjId;
		ConfirmKickUserPanel.SetActive(value: true);
	}

	public void CancelKickUserFromServer()
	{
		ConfirmKickUserPanel.SetActive(value: false);
	}

	public void ConfirmKickUserFromServer()
	{
		if (playerObjToKick > 0 && playerObjToKick <= 3)
		{
			StartOfRound.Instance.KickPlayer(playerObjToKick);
			ConfirmKickUserPanel.SetActive(value: false);
		}
	}

	public void RemoveUserFromPlayerList(int playerObjectId)
	{
		playerListSlots[playerObjectId].slotContainer.SetActive(value: false);
		playerListSlots[playerObjectId].isConnected = false;
	}

	public void OpenUserSteamProfile(int slotId)
	{
		if (!GameNetworkManager.Instance.disableSteam && playerListSlots[slotId].isConnected && playerListSlots[slotId].playerSteamId != 0L)
		{
			SteamFriends.OpenUserOverlay(playerListSlots[slotId].playerSteamId, "steamid");
		}
	}
}
