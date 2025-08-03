using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GameNetcodeStuff;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameNetworkManager : MonoBehaviour
{
	public int gameVersionNum = 1;

	public int compatibleFileCutoffVersion;

	public bool AlwaysDisplayNews = true;

	public bool isDemo;

	[Space(5f)]
	public bool SendExceptionsToServer;

	[Space(5f)]
	public bool disableSteam;

	private FacepunchTransport transport;

	public List<SteamId> steamIdsInLobby = new List<SteamId>();

	public HostSettings lobbyHostSettings;

	public int connectedPlayers;

	public int maxAllowedPlayers = 4;

	private bool hasSubscribedToConnectionCallbacks;

	public bool gameHasStarted;

	public PlayerControllerB localPlayerController;

	public int disconnectReason;

	public string username;

	public bool isDisconnecting;

	public bool firstTimeInMenu = true;

	public bool isHostingGame;

	public bool waitingForLobbyDataRefresh;

	public int playersInRefreshedLobby;

	public string steamLobbyName;

	public const string LCchallengeFileName = "LCChallengeFile";

	public const string LCsaveFile1Name = "LCSaveFile1";

	public const string LCsaveFile2Name = "LCSaveFile2";

	public const string LCsaveFile3Name = "LCSaveFile3";

	public const string generalSaveDataName = "LCGeneralSaveData";

	public string currentSaveFileName = "LCSaveFile1";

	public int saveFileNum;

	public AudioClip buttonCancelSFX;

	public AudioClip buttonSelectSFX;

	public AudioClip buttonPressSFX;

	public AudioClip buttonTuneSFX;

	public bool disallowConnection;

	public string disconnectionReasonMessage;

	public bool localClientWaitingForApproval;

	public bool disapprovedClientThisFrame;

	private string previousLogErrorString;

	public static GameNetworkManager Instance { get; private set; }

	public Lobby? currentLobby { get; private set; }

	private void LogCallback(string condition, string stackTrace, LogType type)
	{
		if ((type != LogType.Exception && type != 0) || HUDManager.Instance == null || localPlayerController == null)
		{
			return;
		}
		string text = condition + stackTrace.Substring(0, Mathf.Clamp(200, 0, stackTrace.Length));
		if (string.IsNullOrEmpty(previousLogErrorString) || !(text == previousLogErrorString))
		{
			previousLogErrorString = text;
			if (!SendExceptionsToServer)
			{
				HUDManager.Instance.AddToErrorLog(text, (int)localPlayerController.playerClientId);
				return;
			}
			HUDManager.Instance.SendErrorMessageServerRpc(text, (int)localPlayerController.playerClientId);
			HUDManager.Instance.AddToErrorLog(text, (int)localPlayerController.playerClientId);
		}
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			StartCoroutine(waitFrameBeforeFindingUsername());
			if (compatibleFileCutoffVersion > gameVersionNum)
			{
				Debug.LogError("The compatible file cutoff version was higher than the game version number. This should not happen!!");
				compatibleFileCutoffVersion = gameVersionNum;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private IEnumerator waitFrameBeforeFindingUsername()
	{
		yield return null;
		yield return null;
		if (!disableSteam)
		{
			string text = SteamClient.Name.ToString();
			if (text.Length > 18)
			{
				text.Remove(15, text.Length - 15);
				text += "...";
			}
			username = text;
		}
		else
		{
			username = "PlayerName";
		}
	}

	public int GetWeekNumber()
	{
		DateTime dateTime = new DateTime(2023, 12, 11);
		DateTime dateTime2;
		try
		{
			dateTime2 = DateTime.UtcNow;
		}
		catch (Exception arg)
		{
			Debug.LogError($"Unable to get UTC time; defaulting to system date time; {arg}");
			dateTime2 = DateTime.Today;
		}
		Debug.Log($"Returning week num: {(int)((dateTime2 - dateTime).TotalDays / 7.0)}");
		return (int)((dateTime2 - dateTime).TotalDays / 7.0);
	}

	public string GetNameForWeekNumber(int overrideWeekNum = -1)
	{
		StringBuilder stringBuilder = new StringBuilder();
		System.Random random = ((overrideWeekNum == -1) ? new System.Random(GetWeekNumber()) : new System.Random(overrideWeekNum));
		List<char> list = "BCDFGKLMNPSTVZHRW".ToCharArray().ToList();
		List<char> list2 = "AEIOU".ToCharArray().ToList();
		int num = 0;
		int num2 = 1;
		char c = 'a';
		int num3 = ((random.Next(0, 100) >= 40) ? random.Next(3, 8) : random.Next(3, 5));
		for (int i = 0; i < num3; i++)
		{
			int num4 = 5;
			if (list.Contains(c))
			{
				num4 -= 2;
			}
			if (c == 'O' || c == 'E')
			{
				num4--;
			}
			if (random.Next(0, num4) > num2)
			{
				if (random.Next(0, 100) < 40)
				{
					if (!list.Contains('Y'))
					{
						list.Add('Y');
						list.Add('J');
						list.Add('X');
						list.Add('Q');
					}
				}
				else if (list.Contains('Y'))
				{
					list.Remove('Y');
					list.Remove('J');
					list.Remove('X');
					list.Remove('Q');
				}
				bool flag = false;
				char c2 = char.ToUpper(c);
				int num5 = list.Count;
				while (num5 > 0)
				{
					char c3 = list[random.Next(0, list.Count)];
					if (list.Count == 1 || num > 1 || random.Next(0, 100) < 33 || c2 == 'Q' || c2 == 'K' || (c2 == 'Q' && c3 == 'B') || (c2 == 'H' && c3 == 'P') || (c2 == 'I' && c3 == 'W'))
					{
						list.Remove(c3);
						num5--;
						num = 0;
						flag = true;
						continue;
					}
					c = c3;
					break;
				}
				if (flag)
				{
					list = "BCDFGJKLMNPSTVZHRW".ToCharArray().ToList();
				}
				if (c == c2)
				{
					num++;
				}
				num2++;
			}
			else
			{
				bool flag2 = false;
				char c2 = char.ToUpper(c);
				int num6 = list2.Count;
				while (num6 > 0)
				{
					char c3 = list2[random.Next(0, list2.Count)];
					if ((list2.Count == 1 && c2 == 'U' && c3 == 'U') || (c2 == 'I' && c3 == 'I') || (c2 == 'I' && c3 == 'U' && i == 1))
					{
						list2.Remove(c3);
						num6--;
						flag2 = true;
						continue;
					}
					c = c3;
					break;
				}
				num2 = 1;
				if (flag2)
				{
					list2 = "AEIOU".ToCharArray().ToList();
				}
			}
			if (i != 0)
			{
				c = char.ToLower(c);
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString() + "-" + random.Next(1, 99);
	}

	private void Start()
	{
		GetComponent<NetworkManager>().NetworkConfig.ProtocolVersion = (ushort)gameVersionNum;
		if ((bool)GetComponent<FacepunchTransport>())
		{
			transport = GetComponent<FacepunchTransport>();
		}
		else
		{
			Debug.Log("Facepunch transport is disabled.");
		}
		saveFileNum = ES3.Load("SelectedFile", "LCGeneralSaveData", 0);
		switch (saveFileNum)
		{
		case -1:
			currentSaveFileName = "LCChallengeFile";
			break;
		case 0:
			currentSaveFileName = "LCSaveFile1";
			break;
		case 1:
			currentSaveFileName = "LCSaveFile2";
			break;
		case 2:
			currentSaveFileName = "LCSaveFile3";
			break;
		default:
			currentSaveFileName = "LCSaveFile1";
			break;
		}
	}

	private void OnEnable()
	{
		Application.logMessageReceived += LogCallback;
		if (!disableSteam)
		{
			Debug.Log("subcribing to steam callbacks");
			SteamMatchmaking.OnLobbyCreated += SteamMatchmaking_OnLobbyCreated;
			SteamMatchmaking.OnLobbyMemberJoined += SteamMatchmaking_OnLobbyMemberJoined;
			SteamMatchmaking.OnLobbyMemberLeave += SteamMatchmaking_OnLobbyMemberLeave;
			SteamMatchmaking.OnLobbyInvite += SteamMatchmaking_OnLobbyInvite;
			SteamMatchmaking.OnLobbyGameCreated += SteamMatchmaking_OnLobbyGameCreated;
			SteamFriends.OnGameLobbyJoinRequested += SteamFriends_OnGameLobbyJoinRequested;
		}
	}

	private void OnDisable()
	{
		Application.logMessageReceived -= LogCallback;
		if (!disableSteam)
		{
			Debug.Log("unsubscribing from steam callbacks");
			SteamMatchmaking.OnLobbyCreated -= SteamMatchmaking_OnLobbyCreated;
			SteamMatchmaking.OnLobbyMemberJoined -= SteamMatchmaking_OnLobbyMemberJoined;
			SteamMatchmaking.OnLobbyMemberLeave -= SteamMatchmaking_OnLobbyMemberLeave;
			SteamMatchmaking.OnLobbyInvite -= SteamMatchmaking_OnLobbyInvite;
			SteamMatchmaking.OnLobbyGameCreated -= SteamMatchmaking_OnLobbyGameCreated;
			SteamFriends.OnGameLobbyJoinRequested -= SteamFriends_OnGameLobbyJoinRequested;
		}
	}

	public void SetSteamFriendGrouping(string groupName, int groupSize, string steamDisplay)
	{
		if (!disableSteam)
		{
			SteamFriends.SetRichPresence("steam_player_group", groupName);
			SteamFriends.SetRichPresence("steam_player_group_size", groupSize.ToString());
			SteamFriends.SetRichPresence("steam_display", steamDisplay);
		}
	}

	private void ConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
	{
		Debug.Log("Connection approval callback! Game version of client request: " + Encoding.ASCII.GetString(request.Payload).ToString());
		Debug.Log($"Joining client id: {request.ClientNetworkId}; Local/host client id: {NetworkManager.Singleton.LocalClientId}");
		if (request.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
		{
			Debug.Log("Stopped connection approval callback, as the client in question was the host!");
			return;
		}
		bool flag = !disallowConnection;
		if (flag)
		{
			string @string = Encoding.ASCII.GetString(request.Payload);
			string[] array = @string.Split(",");
			if (string.IsNullOrEmpty(@string))
			{
				response.Reason = "Unknown; please verify your game files.";
				flag = false;
			}
			else if (Instance.connectedPlayers >= 4)
			{
				response.Reason = "Lobby is full!";
				flag = false;
			}
			else if (Instance.gameHasStarted)
			{
				response.Reason = "Game has already started!";
				flag = false;
			}
			else if (Instance.gameVersionNum.ToString() != array[0])
			{
				response.Reason = $"Game version mismatch! Their version: {gameVersionNum}. Your version: {array[0]}";
				flag = false;
			}
			else if (!disableSteam && (StartOfRound.Instance == null || array.Length < 2 || StartOfRound.Instance.KickedClientIds.Contains((ulong)Convert.ToInt64(array[1]))))
			{
				response.Reason = "You cannot rejoin after being kicked.";
				flag = false;
			}
		}
		else
		{
			response.Reason = "The host was not accepting connections.";
		}
		Debug.Log($"Approved connection?: {flag}. Connected players #: {Instance.connectedPlayers}");
		Debug.Log("Disapproval reason: " + response.Reason);
		response.CreatePlayerObject = false;
		response.Approved = flag;
		response.Pending = false;
	}

	private void Singleton_OnClientDisconnectCallback(ulong clientId)
	{
		Debug.Log("Disconnect callback called");
		Debug.Log($"Is server: {NetworkManager.Singleton.IsServer}; ishost: {NetworkManager.Singleton.IsHost}; isConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
		if (NetworkManager.Singleton == null)
		{
			Debug.Log("Network singleton is null!");
			return;
		}
		if (clientId == NetworkManager.Singleton.LocalClientId && localClientWaitingForApproval)
		{
			OnLocalClientConnectionDisapproved(clientId);
			return;
		}
		if (NetworkManager.Singleton.IsServer)
		{
			Debug.Log($"Disconnect callback called in gamenetworkmanager; disconnecting clientId: {clientId}");
			if (StartOfRound.Instance != null && !StartOfRound.Instance.ClientPlayerList.ContainsKey(clientId))
			{
				Debug.Log("A Player disconnected but they were not in clientplayerlist");
				return;
			}
			if (clientId == NetworkManager.Singleton.LocalClientId)
			{
				Debug.Log("Disconnect callback called for local client; ignoring.");
				return;
			}
			if (NetworkManager.Singleton.IsServer)
			{
				connectedPlayers--;
			}
		}
		if (StartOfRound.Instance != null)
		{
			StartOfRound.Instance.OnClientDisconnect(clientId);
		}
		Debug.Log("Disconnect callback from networkmanager in gamenetworkmanager");
	}

	private void OnLocalClientConnectionDisapproved(ulong clientId)
	{
		localClientWaitingForApproval = false;
		Debug.Log($"Local client connection denied; clientId: {clientId}; reason: {disconnectionReasonMessage.ToString()}");
		if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
		{
			disconnectionReasonMessage = NetworkManager.Singleton.DisconnectReason;
		}
		UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false);
		LeaveCurrentSteamLobby();
		SetInstanceValuesBackToDefault();
		if (NetworkManager.Singleton.IsConnectedClient)
		{
			Debug.Log("Calling shutdown(true) on server in OnLocalClientDisapproved");
			NetworkManager.Singleton.Shutdown(discardMessageQueue: true);
		}
	}

	private void Singleton_OnClientConnectedCallback(ulong clientId)
	{
		if (!(NetworkManager.Singleton == null))
		{
			Debug.Log("Client connected callback in gamenetworkmanager");
			if (NetworkManager.Singleton.IsServer)
			{
				connectedPlayers++;
			}
			if (StartOfRound.Instance != null)
			{
				StartOfRound.Instance.OnClientConnect(clientId);
			}
		}
	}

	public void SubscribeToConnectionCallbacks()
	{
		if (!hasSubscribedToConnectionCallbacks)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += Instance.Singleton_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback += Instance.Singleton_OnClientDisconnectCallback;
			hasSubscribedToConnectionCallbacks = true;
		}
	}

	public void SteamFriends_OnGameLobbyJoinRequested(Lobby lobby, SteamId id)
	{
		if (UnityEngine.Object.FindObjectOfType<MenuManager>() == null)
		{
			return;
		}
		if (!Instance.currentLobby.HasValue)
		{
			Debug.Log("JOIN REQUESTED through steam invite");
			Debug.Log($"lobby id: {lobby.Id}");
			LobbySlot.JoinLobbyAfterVerifying(lobby, lobby.Id);
			return;
		}
		Debug.Log("Attempted to join by Steam invite request, but already in a lobby.");
		MenuManager menuManager = UnityEngine.Object.FindObjectOfType<MenuManager>();
		if (menuManager != null)
		{
			menuManager.DisplayMenuNotification("You are already in a lobby!", "Back");
		}
		Instance.currentLobby.Value.Leave();
		Instance.currentLobby = null;
	}

	public bool LobbyDataIsJoinable(Lobby lobby)
	{
		string data = lobby.GetData("vers");
		if (data != Instance.gameVersionNum.ToString())
		{
			Debug.Log($"Lobby join denied! Attempted to join vers.{data} lobby id: {lobby.Id}");
			UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, $"The server host is playing on version {data} while you are on version {Instance.gameVersionNum}.");
			return false;
		}
		Friend[] array = SteamFriends.GetBlocked().ToArray();
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				Debug.Log($"blocked users {i}: {array[i].Name}; id: {array[i].Id}");
				if (lobby.IsOwnedBy(array[i].Id))
				{
					UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "An error occured!");
					return false;
				}
			}
		}
		else
		{
			Debug.Log("Blocked users list is null");
		}
		if (lobby.GetData("joinable") == "false")
		{
			Debug.Log("Lobby join denied! Host lobby is not joinable");
			UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.DoesntExist, "The server host has already landed their ship, or they are still loading in.");
			return false;
		}
		if (lobby.MemberCount >= 4 || lobby.MemberCount < 1)
		{
			Debug.Log($"Lobby join denied! Too many members in lobby! {lobby.Id}");
			UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Full, "The server is full!");
			return false;
		}
		Debug.Log($"Lobby join accepted! Lobby id {lobby.Id} is OK");
		return true;
	}

	public IEnumerator TimeOutLobbyRefresh()
	{
		yield return new WaitForSeconds(7f);
		waitingForLobbyDataRefresh = false;
		UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Error, "Error! Could not get the lobby data. Are you offline?");
		SteamMatchmaking.OnLobbyDataChanged -= LobbySlot.OnLobbyDataRefresh;
	}

	private void SteamMatchmaking_OnLobbyMemberJoined(Lobby lobby, Friend friend)
	{
		if (Instance.currentLobby.HasValue)
		{
			Friend[] array = Instance.currentLobby.Value.Members.ToArray();
			if (array != null)
			{
				for (int i = 0; i < array.Length; i++)
				{
					if (!steamIdsInLobby.Contains(array[i].Id))
					{
						steamIdsInLobby.Add(array[i].Id);
					}
				}
			}
		}
		Debug.Log($"Player joined w steamId: {friend.Id}");
		if (StartOfRound.Instance != null)
		{
			QuickMenuManager quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
			if (quickMenuManager != null)
			{
				string input = NoPunctuation(friend.Name);
				input = Regex.Replace(input, "[^\\w\\._]", "");
				quickMenuManager.AddUserToPlayerList(friend.Id, input, StartOfRound.Instance.connectedPlayersAmount);
			}
		}
	}

	private string NoPunctuation(string input)
	{
		return new string(input.Where((char c) => char.IsLetter(c)).ToArray());
	}

	private void SteamMatchmaking_OnLobbyMemberLeave(Lobby lobby, Friend friend)
	{
		if (!steamIdsInLobby.Contains(friend.Id))
		{
			steamIdsInLobby.Remove(friend.Id);
		}
	}

	private void SteamMatchmaking_OnLobbyGameCreated(Lobby lobby, uint arg2, ushort arg3, SteamId arg4)
	{
	}

	private void SteamMatchmaking_OnLobbyInvite(Friend friend, Lobby lobby)
	{
		Debug.Log($"You got invited by {friend.Name} to join {lobby.Id}");
	}

	private void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
	{
		if (result != Result.OK)
		{
			Debug.LogError($"Lobby could not be created! {result}", this);
		}
		lobby.SetData("name", lobbyHostSettings.lobbyName.ToString());
		lobby.SetData("vers", Instance.gameVersionNum.ToString());
		if (!string.IsNullOrEmpty(lobbyHostSettings.serverTag))
		{
			lobby.SetData("tag", lobbyHostSettings.serverTag.ToLower());
		}
		else
		{
			lobby.SetData("tag", "none");
		}
		if (lobbyHostSettings.isLobbyPublic)
		{
			lobby.SetPublic();
		}
		else
		{
			lobby.SetPrivate();
			lobby.SetFriendsOnly();
		}
		lobby.SetJoinable(b: false);
		Instance.currentLobby = lobby;
		steamLobbyName = lobby.GetData("name");
		Debug.Log("Lobby has been created");
	}

	public void LeaveLobbyAtGameStart()
	{
		if (!Instance.currentLobby.HasValue)
		{
			Debug.Log("Current lobby is null. (Attempted to close lobby at game start)");
		}
		else
		{
			LeaveCurrentSteamLobby();
		}
	}

	public void SetLobbyJoinable(bool joinable)
	{
		if (!Instance.currentLobby.HasValue)
		{
			Debug.Log($"Current lobby is null. (Attempted to set lobby joinable {joinable}.)");
			return;
		}
		Instance.currentLobby.Value.SetJoinable(joinable);
		if (StartOfRound.Instance != null && currentSaveFileName == "LCChallengeFile")
		{
			Instance.currentLobby.Value.SetData("chal", "t");
		}
		else
		{
			Instance.currentLobby.Value.SetData("chal", "f");
		}
	}

	public void SetCurrentLobbyNull()
	{
		currentLobby = null;
	}

	private void OnApplicationQuit()
	{
		try
		{
			ES3.Save("SelectedFile", saveFileNum, "LCGeneralSaveData");
			Disconnect();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while disconnecting: {arg}");
		}
		if (DiscordController.Instance != null)
		{
			DiscordController.Instance.UpdateStatus(clear: true);
		}
	}

	public void Disconnect()
	{
		if (!isDisconnecting && !(StartOfRound.Instance == null))
		{
			isDisconnecting = true;
			if (isHostingGame)
			{
				disallowConnection = true;
			}
			StartDisconnect();
			SaveGame();
			if (NetworkManager.Singleton == null)
			{
				Debug.Log("Server is not active; quitting to main menu");
				ResetGameValuesToDefault();
				SceneManager.LoadScene("MainMenu");
			}
			else
			{
				StartCoroutine(DisconnectProcess());
			}
		}
	}

	private IEnumerator DisconnectProcess()
	{
		Debug.Log($"Shutting down and disconnecting from server. Is host?: {NetworkManager.Singleton.IsServer}");
		NetworkManager.Singleton.Shutdown();
		yield return new WaitUntil(() => !NetworkManager.Singleton.ShutdownInProgress);
		ResetGameValuesToDefault();
		SceneManager.LoadScene("MainMenu");
	}

	private void StartDisconnect()
	{
		if (!disableSteam)
		{
			Debug.Log("Leaving current lobby");
			LeaveCurrentSteamLobby();
			steamLobbyName = SteamClient.Name;
		}
		if (DiscordController.Instance != null)
		{
			DiscordController.Instance.UpdateStatus(clear: true);
		}
		Debug.Log("Disconnecting and setting networkobjects to destroy with owner");
		NetworkObject[] array = UnityEngine.Object.FindObjectsOfType<NetworkObject>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].DontDestroyWithOwner = false;
		}
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		if (terminal != null && terminal.displayingSteamKeyboard)
		{
			SteamUtils.OnGamepadTextInputDismissed -= terminal.OnGamepadTextInputDismissed_t;
		}
	}

	public void SaveGame()
	{
		SaveLocalPlayerValues();
		SaveGameValues();
	}

	private void ResetGameValuesToDefault()
	{
		ResetUnlockablesListValues();
		ResetStaticVariables();
		if (StartOfRound.Instance != null)
		{
			StartOfRound.Instance.OnLocalDisconnect();
		}
		SetInstanceValuesBackToDefault();
	}

	public void ResetStaticVariables()
	{
		SprayPaintItem.sprayPaintDecals.Clear();
		SprayPaintItem.sprayPaintDecalsIndex = 0;
		SprayPaintItem.previousSprayDecal = null;
		ShipTeleporter.hasBeenSpawnedThisSession = false;
		ShipTeleporter.hasBeenSpawnedThisSessionInverse = false;
		RadMechAI.PooledBlastMarks.Clear();
	}

	public void ResetUnlockablesListValues(bool onlyResetPrefabItems = false)
	{
		if (!(StartOfRound.Instance != null))
		{
			return;
		}
		Debug.Log("Resetting unlockables list!");
		List<UnlockableItem> unlockables = StartOfRound.Instance.unlockablesList.unlockables;
		for (int i = 0; i < unlockables.Count; i++)
		{
			if (!onlyResetPrefabItems || unlockables[i].spawnPrefab)
			{
				unlockables[i].hasBeenUnlockedByPlayer = false;
				if (unlockables[i].unlockableType == 1)
				{
					unlockables[i].placedPosition = Vector3.zero;
					unlockables[i].placedRotation = Vector3.zero;
					unlockables[i].hasBeenMoved = false;
					unlockables[i].inStorage = false;
				}
			}
		}
	}

	private void SaveLocalPlayerValues()
	{
		try
		{
			if (HUDManager.Instance != null)
			{
				if (HUDManager.Instance.setTutorialArrow)
				{
					ES3.Save("FinishedShockMinigame", PatcherTool.finishedShockMinigame, "LCGeneralSaveData");
				}
				if (HUDManager.Instance.hasSetSavedValues)
				{
					ES3.Save("PlayerLevel", HUDManager.Instance.localPlayerLevel, "LCGeneralSaveData");
					ES3.Save("PlayerXPNum", HUDManager.Instance.localPlayerXP, "LCGeneralSaveData");
				}
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"ERROR occured while saving local player values!: {arg}");
		}
	}

	public void ResetSavedGameValues()
	{
		if (!isHostingGame)
		{
			return;
		}
		TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
		if (timeOfDay != null)
		{
			ES3.Save("GlobalTime", 100, currentSaveFileName);
			ES3.Save("QuotaFulfilled", 0, currentSaveFileName);
			ES3.Save("QuotasPassed", 0, currentSaveFileName);
			ES3.Save("ProfitQuota", timeOfDay.quotaVariables.startingQuota, currentSaveFileName);
			ES3.Save("DeadlineTime", (int)(timeOfDay.totalTime * (float)timeOfDay.quotaVariables.deadlineDaysAmount), currentSaveFileName);
			ES3.Save("GroupCredits", timeOfDay.quotaVariables.startingCredits, currentSaveFileName);
		}
		ES3.Save("CurrentPlanetID", 0, currentSaveFileName);
		StartOfRound startOfRound = UnityEngine.Object.FindObjectOfType<StartOfRound>();
		if (!(startOfRound != null))
		{
			return;
		}
		ES3.DeleteKey("UnlockedShipObjects", Instance.currentSaveFileName);
		for (int i = 0; i < startOfRound.levels.Length; i++)
		{
			ES3.Save($"Level{startOfRound.levels[i].levelID}Mold", 0, currentSaveFileName);
			ES3.Save($"Level{startOfRound.levels[i].levelID}MoldOrigin", -1, currentSaveFileName);
			ES3.DeleteKey($"Level{startOfRound.levels[i].levelID}DestroyedMold", currentSaveFileName);
		}
		for (int j = 0; j < startOfRound.unlockablesList.unlockables.Count; j++)
		{
			if (startOfRound.unlockablesList.unlockables[j].unlockableType == 1)
			{
				ES3.DeleteKey("ShipUnlockMoved_" + startOfRound.unlockablesList.unlockables[j].unlockableName, currentSaveFileName);
				ES3.DeleteKey("ShipUnlockStored_" + startOfRound.unlockablesList.unlockables[j].unlockableName, currentSaveFileName);
				ES3.DeleteKey("ShipUnlockPos_" + startOfRound.unlockablesList.unlockables[j].unlockableName, currentSaveFileName);
				ES3.DeleteKey("ShipUnlockRot_" + startOfRound.unlockablesList.unlockables[j].unlockableName, currentSaveFileName);
			}
		}
		ResetUnlockablesListValues();
		ES3.Save("RandomSeed", startOfRound.randomMapSeed + 1, currentSaveFileName);
		ES3.Save("Stats_DaysSpent", 0, currentSaveFileName);
		ES3.Save("Stats_Deaths", 0, currentSaveFileName);
		ES3.Save("Stats_ValueCollected", 0, currentSaveFileName);
		ES3.Save("Stats_StepsTaken", 0, currentSaveFileName);
	}

	private void SaveGameValues()
	{
		if (StartOfRound.Instance.isChallengeFile && Instance.gameHasStarted)
		{
			ES3.Save("FinishedChallenge", value: true, "LCChallengeFile");
			if (!StartOfRound.Instance.displayedLevelResults)
			{
				ES3.Save("ProfitEarned", 0, "LCChallengeFile");
			}
			else
			{
				Debug.Log($"Saved challenge score as {StartOfRound.Instance.scrapCollectedLastRound}; total scrap in level: {RoundManager.Instance.totalScrapValueInLevel}");
				ES3.Save("ProfitEarned", StartOfRound.Instance.scrapCollectedLastRound, "LCChallengeFile");
			}
		}
		if (!isHostingGame)
		{
			return;
		}
		if (!ES3.KeyExists("FileGameVers", currentSaveFileName))
		{
			ES3.Save("FileGameVers", Instance.gameVersionNum, currentSaveFileName);
		}
		if (!StartOfRound.Instance.inShipPhase || StartOfRound.Instance.isChallengeFile)
		{
			return;
		}
		try
		{
			TimeOfDay timeOfDay = UnityEngine.Object.FindObjectOfType<TimeOfDay>();
			if (timeOfDay != null)
			{
				ES3.Save("QuotaFulfilled", timeOfDay.quotaFulfilled, currentSaveFileName);
				ES3.Save("QuotasPassed", timeOfDay.timesFulfilledQuota, currentSaveFileName);
				ES3.Save("ProfitQuota", timeOfDay.profitQuota, currentSaveFileName);
				ES3.Save("ShownAdThisQuota", timeOfDay.hasShownAdThisQuota, currentSaveFileName);
			}
			ES3.Save("CurrentPlanetID", StartOfRound.Instance.currentLevelID, currentSaveFileName);
			if (timeOfDay.furniturePlacedAtQuotaStart == null || timeOfDay.furniturePlacedAtQuotaStart.Count == 0)
			{
				if (ES3.KeyExists("LuckyFurniture", currentSaveFileName))
				{
					ES3.DeleteKey("LuckyFurniture", currentSaveFileName);
				}
			}
			else
			{
				ES3.Save("LuckyFurniture", timeOfDay.furniturePlacedAtQuotaStart.ToArray(), currentSaveFileName);
			}
			Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
			if (terminal != null)
			{
				ES3.Save("GroupCredits", terminal.groupCredits, currentSaveFileName);
				if (terminal.unlockedStoryLogs.Count > 0)
				{
					ES3.Save("StoryLogs", terminal.unlockedStoryLogs.ToArray(), currentSaveFileName);
				}
				if (terminal.scannedEnemyIDs.Count > 0)
				{
					ES3.Save("EnemyScans", terminal.scannedEnemyIDs.ToArray(), currentSaveFileName);
				}
				ES3.Save("VehicleWarrantyTicket", terminal.hasWarrantyTicket, currentSaveFileName);
			}
			StartOfRound startOfRound = UnityEngine.Object.FindObjectOfType<StartOfRound>();
			MoldSpreadManager moldSpreadManager = UnityEngine.Object.FindObjectOfType<MoldSpreadManager>();
			if (startOfRound != null)
			{
				for (int i = 0; i < startOfRound.levels.Length; i++)
				{
					ES3.Save($"Level{startOfRound.levels[i].levelID}Mold", startOfRound.levels[i].moldSpreadIterations, currentSaveFileName);
					ES3.Save($"Level{startOfRound.levels[i].levelID}MoldOrigin", startOfRound.levels[i].moldStartPosition, currentSaveFileName);
					if (moldSpreadManager != null)
					{
						if (moldSpreadManager.planetMoldStates[i].destroyedMold.Count > 0)
						{
							ES3.Save($"Level{startOfRound.levels[i].levelID}DestroyedMold", moldSpreadManager.planetMoldStates[i].destroyedMold.ToArray(), currentSaveFileName);
						}
						else
						{
							ES3.DeleteKey($"Level{startOfRound.levels[i].levelID}DestroyedMold", currentSaveFileName);
						}
					}
				}
				List<int> list = new List<int>();
				for (int j = 0; j < startOfRound.unlockablesList.unlockables.Count; j++)
				{
					if (startOfRound.unlockablesList.unlockables[j].hasBeenUnlockedByPlayer || startOfRound.unlockablesList.unlockables[j].hasBeenMoved || startOfRound.unlockablesList.unlockables[j].inStorage)
					{
						list.Add(j);
					}
					if (startOfRound.unlockablesList.unlockables[j].IsPlaceable)
					{
						if (startOfRound.unlockablesList.unlockables[j].canBeStored)
						{
							ES3.Save("ShipUnlockStored_" + startOfRound.unlockablesList.unlockables[j].unlockableName, startOfRound.unlockablesList.unlockables[j].inStorage, currentSaveFileName);
						}
						if (startOfRound.unlockablesList.unlockables[j].hasBeenMoved)
						{
							ES3.Save("ShipUnlockMoved_" + startOfRound.unlockablesList.unlockables[j].unlockableName, startOfRound.unlockablesList.unlockables[j].hasBeenMoved, currentSaveFileName);
							ES3.Save("ShipUnlockPos_" + startOfRound.unlockablesList.unlockables[j].unlockableName, startOfRound.unlockablesList.unlockables[j].placedPosition, currentSaveFileName);
							ES3.Save("ShipUnlockRot_" + startOfRound.unlockablesList.unlockables[j].unlockableName, startOfRound.unlockablesList.unlockables[j].placedRotation, currentSaveFileName);
						}
					}
				}
				if (list.Count > 0)
				{
					ES3.Save("UnlockedShipObjects", list.ToArray(), currentSaveFileName);
				}
				int num = -1;
				if (StartOfRound.Instance.attachedVehicle != null)
				{
					num = StartOfRound.Instance.attachedVehicle.vehicleID;
				}
				ES3.Save("AttachedVehicleID", num, currentSaveFileName);
				Debug.Log($"saved vehicle: {num}");
				ES3.Save("DeadlineTime", (int)Mathf.Clamp(timeOfDay.timeUntilDeadline, 0f, 99999f), currentSaveFileName);
				ES3.Save("RandomSeed", startOfRound.randomMapSeed, currentSaveFileName);
				ES3.Save("Stats_DaysSpent", startOfRound.gameStats.daysSpent, currentSaveFileName);
				ES3.Save("Stats_Deaths", startOfRound.gameStats.deaths, currentSaveFileName);
				ES3.Save("Stats_ValueCollected", startOfRound.gameStats.scrapValueCollected, currentSaveFileName);
				ES3.Save("Stats_StepsTaken", startOfRound.gameStats.allStepsTaken, currentSaveFileName);
			}
			SaveItemsInShip();
		}
		catch (Exception arg)
		{
			Debug.LogError($"Error while trying to save game values when disconnecting as host: {arg}");
		}
	}

	private void SaveItemsInShip()
	{
		GrabbableObject[] array = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		if (array == null || array.Length == 0)
		{
			ES3.DeleteKey("shipGrabbableItemIDs", currentSaveFileName);
			ES3.DeleteKey("shipGrabbableItemPos", currentSaveFileName);
			ES3.DeleteKey("shipScrapValues", currentSaveFileName);
			ES3.DeleteKey("shipItemSaveData", currentSaveFileName);
		}
		else
		{
			if (StartOfRound.Instance.isChallengeFile)
			{
				return;
			}
			List<int> list = new List<int>();
			List<Vector3> list2 = new List<Vector3>();
			List<int> list3 = new List<int>();
			List<int> list4 = new List<int>();
			int num = 0;
			for (int i = 0; i < array.Length && i <= StartOfRound.Instance.maxShipItemCapacity; i++)
			{
				if (!StartOfRound.Instance.allItemsList.itemsList.Contains(array[i].itemProperties) || array[i].deactivated)
				{
					continue;
				}
				if (array[i].itemProperties.spawnPrefab == null)
				{
					Debug.LogError("Item '" + array[i].itemProperties.itemName + "' has no spawn prefab set!");
				}
				else
				{
					if (array[i].itemUsedUp)
					{
						continue;
					}
					for (int j = 0; j < StartOfRound.Instance.allItemsList.itemsList.Count; j++)
					{
						if (StartOfRound.Instance.allItemsList.itemsList[j] == array[i].itemProperties)
						{
							list.Add(j);
							list2.Add(array[i].transform.position);
							break;
						}
					}
					if (array[i].itemProperties.isScrap)
					{
						list3.Add(array[i].scrapValue);
					}
					if (array[i].itemProperties.saveItemVariable)
					{
						try
						{
							num = array[i].GetItemDataToSave();
						}
						catch
						{
							Debug.LogError($"An error occured while getting item data to save for item type: {array[i].itemProperties}; gameobject '{array[i].gameObject.name}'");
						}
						list4.Add(num);
						Debug.Log($"Saved data for item type: {array[i].itemProperties.itemName} - {num}");
					}
				}
			}
			if (list.Count <= 0)
			{
				Debug.Log("Got no ship grabbable items to save.");
				return;
			}
			ES3.Save("shipGrabbableItemPos", list2.ToArray(), currentSaveFileName);
			ES3.Save("shipGrabbableItemIDs", list.ToArray(), currentSaveFileName);
			if (list3.Count > 0)
			{
				ES3.Save("shipScrapValues", list3.ToArray(), currentSaveFileName);
			}
			else
			{
				ES3.DeleteKey("shipScrapValues", currentSaveFileName);
			}
			if (list4.Count > 0)
			{
				ES3.Save("shipItemSaveData", list4.ToArray(), currentSaveFileName);
			}
			else
			{
				ES3.DeleteKey("shipItemSaveData", currentSaveFileName);
			}
		}
	}

	private void ConvertUnsellableItemsToCredits()
	{
		if (!StartOfRound.Instance.inShipPhase)
		{
			Debug.Log("Players disconnected, but they were not in ship phase so they can't be reimbursed for their items.");
			ES3.Save("Reimburse", 0, currentSaveFileName);
			return;
		}
		int num = 0;
		GrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].itemProperties.isScrap && !array[i].itemUsedUp)
			{
				num += array[i].itemProperties.creditsWorth;
			}
		}
		Terminal terminal = UnityEngine.Object.FindObjectOfType<Terminal>();
		for (int j = 0; j < terminal.orderedItemsFromTerminal.Count; j++)
		{
			num += terminal.buyableItemsList[terminal.orderedItemsFromTerminal[j]].creditsWorth;
		}
		if (terminal.orderedVehicleFromTerminal != -1)
		{
			num += terminal.buyableVehicles[terminal.orderedVehicleFromTerminal].creditsWorth;
		}
		ES3.Save("Reimburse", num, currentSaveFileName);
	}

	private void SetInstanceValuesBackToDefault()
	{
		isDisconnecting = false;
		disallowConnection = false;
		connectedPlayers = 0;
		localPlayerController = null;
		gameHasStarted = false;
		if (SoundManager.Instance != null)
		{
			SoundManager.Instance.ResetValues();
		}
		if (hasSubscribedToConnectionCallbacks && NetworkManager.Singleton != null)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
			NetworkManager.Singleton.OnClientDisconnectCallback -= Singleton_OnClientDisconnectCallback;
			hasSubscribedToConnectionCallbacks = false;
		}
	}

	public void InviteFriendsUI()
	{
		SteamFriends.OpenGameInviteOverlay(Instance.currentLobby.Value.Id);
	}

	public async void StartHost()
	{
		if (!UnityEngine.Object.FindObjectOfType<MenuManager>())
		{
			Debug.Log("Menu manager script is not present in scene; unable to start host");
			return;
		}
		if (Instance.currentLobby.HasValue)
		{
			Debug.Log("Tried starting host but currentLobby is not null! This should not happen. Leaving currentLobby and setting null.");
			LeaveCurrentSteamLobby();
		}
		if (!disableSteam)
		{
			GameNetworkManager instance = Instance;
			instance.currentLobby = await SteamMatchmaking.CreateLobbyAsync(4);
		}
		NetworkManager.Singleton.ConnectionApprovalCallback = ConnectionApproval;
		UnityEngine.Object.FindObjectOfType<MenuManager>().StartHosting();
		SubscribeToConnectionCallbacks();
		if (!disableSteam)
		{
			steamIdsInLobby.Add(SteamClient.SteamId);
		}
		isHostingGame = true;
		connectedPlayers = 1;
	}

	public async void JoinLobby(Lobby lobby, SteamId id)
	{
		Debug.Log($"lobby.id: {lobby.Id}");
		Debug.Log($"id: {id}");
		if (UnityEngine.Object.FindObjectOfType<MenuManager>() == null)
		{
			return;
		}
		if (!Instance.currentLobby.HasValue)
		{
			Instance.currentLobby = lobby;
			steamLobbyName = lobby.GetData("name");
			if (await lobby.Join() == RoomEnter.Success)
			{
				Debug.Log("Successfully joined steam lobby.");
				Debug.Log($"AA {Instance.currentLobby.Value.Id}");
				Debug.Log($"BB {id}");
				Instance.StartClient(lobby.Owner.Id);
			}
			else
			{
				Debug.Log("Failed to join steam lobby.");
				LeaveCurrentSteamLobby();
				steamLobbyName = SteamClient.Name;
				UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Error, "The host has not loaded or has already landed their ship.");
			}
		}
		else
		{
			Debug.Log("Lobby error!: Attempted to join, but we are already in a Steam lobby. We should not be in a lobby while in the menu!");
			LeaveCurrentSteamLobby();
		}
	}

	public void LeaveCurrentSteamLobby()
	{
		try
		{
			if (Instance.currentLobby.HasValue)
			{
				Instance.currentLobby.Value.Leave();
				Instance.currentLobby = null;
				steamIdsInLobby.Clear();
			}
		}
		catch (Exception arg)
		{
			Debug.Log($"Error caught while attempting to leave current lobby!: {arg}");
		}
	}

	public void SetConnectionDataBeforeConnecting()
	{
		localClientWaitingForApproval = true;
		Debug.Log("Game version: " + Instance.gameVersionNum);
		if (disableSteam)
		{
			NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(Instance.gameVersionNum.ToString());
		}
		else
		{
			NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(Instance.gameVersionNum + "," + (ulong)SteamClient.SteamId);
		}
	}

	public void StartClient(SteamId id)
	{
		Debug.Log($"CC {id}");
		transport.targetSteamId = id;
		SetConnectionDataBeforeConnecting();
		if (NetworkManager.Singleton.StartClient())
		{
			Debug.Log("started client!");
			SubscribeToConnectionCallbacks();
			UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: true);
			return;
		}
		Debug.Log("Joined steam lobby successfully, but connection failed");
		UnityEngine.Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false);
		if (Instance.currentLobby.HasValue)
		{
			Debug.Log("Leaving steam lobby");
			Instance.currentLobby.Value.Leave();
			Instance.currentLobby = null;
			steamLobbyName = SteamClient.Name;
		}
		SetInstanceValuesBackToDefault();
	}

	private IEnumerator delayStartClient()
	{
		yield return new WaitForSeconds(1f);
		if (NetworkManager.Singleton.StartClient())
		{
			Debug.Log("started client!");
			Debug.Log($"Are we connected client: {NetworkManager.Singleton.IsConnectedClient}");
			if (NetworkManager.Singleton != null)
			{
				Debug.Log("NetworkManager is not null");
			}
			Debug.Log($"Are we connected client: {NetworkManager.Singleton.IsConnectedClient}");
			Debug.Log($"Are we host: {NetworkManager.Singleton.IsHost}");
			yield return null;
			if (NetworkManager.Singleton != null)
			{
				Debug.Log("NetworkManager is not null");
			}
			Debug.Log($"is networkmanager listening: {NetworkManager.Singleton.IsListening}");
			Debug.Log("connected host name: " + NetworkManager.Singleton.ConnectedHostname);
		}
	}
}
