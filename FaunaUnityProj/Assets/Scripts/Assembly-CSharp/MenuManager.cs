using System;
using System.Collections;
using Dissonance;
using Steamworks;
using Steamworks.Data;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
	public GameObject menuButtons;

	public bool isInitScene;

	[Space(5f)]
	public GameObject menuNotification;

	public TextMeshProUGUI menuNotificationText;

	public TextMeshProUGUI menuNotificationButtonText;

	public TextMeshProUGUI versionNumberText;

	[Space(5f)]
	public TextMeshProUGUI loadingText;

	public GameObject loadingScreen;

	[Space(5f)]
	public GameObject lanButtonContainer;

	public GameObject lanWarningContainer;

	public GameObject joinCrewButtonContainer;

	public TextMeshProUGUI launchedInLanModeText;

	[Space(3f)]
	public GameObject serverListUIContainer;

	public AudioListener menuListener;

	public TextMeshProUGUI tipTextHostSettings;

	[Space(5f)]
	public TextMeshProUGUI logText;

	public GameObject inputFieldGameObject;

	[Space(5f)]
	public GameObject NewsPanel;

	[Space(5f)]
	public GameObject HostSettingsScreen;

	public TMP_InputField lobbyNameInputField;

	public TMP_InputField lobbyTagInputField;

	public bool hostSettings_LobbyPublic;

	public Animator setPublicButtonAnimator;

	public Animator setPrivateButtonAnimator;

	public TextMeshProUGUI privatePublicDescription;

	[SerializeField]
	private Button startHostButton;

	[SerializeField]
	private Button startClientButton;

	[SerializeField]
	private Button leaveButton;

	public GameObject HostSettingsOptionsLAN;

	public GameObject HostSettingsOptionsNormal;

	public Animator lanSetLocalButtonAnimator;

	public Animator lanSetAllowRemoteButtonAnimator;

	[SerializeField]
	private TMP_InputField joinCodeInput;

	private bool hasServerStarted;

	private bool startingAClient;

	private int currentMicrophoneDevice;

	public TextMeshProUGUI currentMicrophoneText;

	public DissonanceComms comms;

	public AudioSource MenuAudio;

	public AudioClip menuMusic;

	public AudioClip openMenuSound;

	public Animator menuAnimator;

	public TextMeshProUGUI changesNotAppliedText;

	public TextMeshProUGUI settingsBackButton;

	public GameObject PleaseConfirmChangesSettingsPanel;

	public Button PleaseConfirmChangesSettingsPanelBackButton;

	public GameObject KeybindsPanel;

	private bool selectingUIThisFrame;

	private GameObject lastSelectedGameObject;

	private bool playSelectAudioThisFrame;

	public bool[] filesCompatible;

	private Leaderboard? challengeLeaderboard;

	public GameObject leaderboardContainer;

	public GameObject leaderboardSlotPrefab;

	public Transform leaderboardSlotsContainer;

	public int leaderboardSlotOffset;

	public int leaderboardFilterType;

	public bool requestingLeaderboard;

	public GameObject hostSettingsPanel;

	public bool hasChallengeBeenCompleted;

	public int challengeScore;

	public Animator submittedRankAnimator;

	public AudioClip submitRankSFX;

	public TextMeshProUGUI submittedRankText;

	private Coroutine displayLeaderboardSlotsCoroutine;

	public TextMeshProUGUI leaderboardHeaderText;

	public TextMeshProUGUI leaderboardLoadingText;

	public GameObject removeScoreButton;

	public AudioClip coldOpen2Audio;

	public Texture2D defaultCursor;

	private void Update()
	{
		if (EventSystem.current == null)
		{
			return;
		}
		if (lastSelectedGameObject != EventSystem.current.currentSelectedGameObject)
		{
			lastSelectedGameObject = EventSystem.current.currentSelectedGameObject;
			if (!playSelectAudioThisFrame)
			{
				playSelectAudioThisFrame = true;
				return;
			}
			MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonSelectSFX);
		}
		if (!(lobbyTagInputField == null) && !(lobbyTagInputField.gameObject == null))
		{
			if (!lobbyTagInputField.gameObject.activeSelf && hostSettings_LobbyPublic)
			{
				lobbyTagInputField.gameObject.SetActive(value: true);
			}
			else if (lobbyTagInputField.gameObject.activeSelf && (!hostSettings_LobbyPublic || GameNetworkManager.Instance.disableSteam))
			{
				lobbyTagInputField.gameObject.SetActive(value: false);
			}
		}
	}

	public void PlayConfirmSFX()
	{
		playSelectAudioThisFrame = false;
		MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonPressSFX);
	}

	public void PlayCancelSFX()
	{
		playSelectAudioThisFrame = false;
		MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonCancelSFX);
	}

	private void Awake()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		Cursor.SetCursor(defaultCursor, new Vector2(0f, 0f), CursorMode.Auto);
		if (GameNetworkManager.Instance != null)
		{
			GameNetworkManager.Instance.isDisconnecting = false;
			GameNetworkManager.Instance.isHostingGame = false;
		}
		if (GameNetworkManager.Instance != null && versionNumberText != null)
		{
			versionNumberText.text = $"v{GameNetworkManager.Instance.gameVersionNum}";
		}
		filesCompatible = new bool[3];
		for (int i = 0; i < filesCompatible.Length; i++)
		{
			filesCompatible[i] = true;
		}
		if (coldOpen2Audio != null)
		{
			coldOpen2Audio.UnloadAudioData();
		}
	}

	private IEnumerator PlayMenuMusicDelayed()
	{
		if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.firstTimeInMenu)
		{
			GameNetworkManager.Instance.firstTimeInMenu = false;
			MenuAudio.PlayOneShot(openMenuSound, 1f);
			yield return new WaitForSeconds(0.3f);
		}
		else
		{
			menuAnimator.SetTrigger("skipOpening");
		}
		yield return new WaitForSeconds(0.1f);
		MenuAudio.clip = menuMusic;
		MenuAudio.Play();
	}

	private void Start()
	{
		if (isInitScene)
		{
			return;
		}
		bool flag = false;
		if (!string.IsNullOrEmpty(GameNetworkManager.Instance.disconnectionReasonMessage))
		{
			SetLoadingScreen(isLoading: false);
		}
		else if (GameNetworkManager.Instance.disconnectReason != 0)
		{
			if (!string.IsNullOrEmpty(NetworkManager.Singleton.DisconnectReason))
			{
				DisplayMenuNotification(NetworkManager.Singleton.DisconnectReason ?? "", "[ Back ]");
				flag = true;
			}
			else if (GameNetworkManager.Instance.disconnectReason == 1)
			{
				DisplayMenuNotification("The server host disconnected.", "[ Back ]");
				flag = true;
			}
			else if (GameNetworkManager.Instance.disconnectReason == 2)
			{
				DisplayMenuNotification("Your connection timed out.", "[ Back ]");
				flag = true;
			}
			GameNetworkManager.Instance.disconnectReason = 0;
		}
		if (GameNetworkManager.Instance.disableSteam)
		{
			launchedInLanModeText.enabled = true;
			lanButtonContainer.SetActive(value: true);
			lanWarningContainer.SetActive(value: true);
			joinCrewButtonContainer.SetActive(value: false);
		}
		else
		{
			lanButtonContainer.SetActive(value: false);
			joinCrewButtonContainer.SetActive(value: true);
		}
		string defaultValue;
		if (GameNetworkManager.Instance.disableSteam)
		{
			defaultValue = "Unnamed";
		}
		else if (!SteamClient.IsLoggedOn)
		{
			DisplayMenuNotification("Could not connect to Steam servers! (If you just want to play on your local network, choose LAN on launch.)", "Continue");
			defaultValue = "Unnamed";
		}
		else
		{
			defaultValue = SteamClient.Name.ToString() + "'s Crew";
		}
		hostSettings_LobbyPublic = ES3.Load("HostSettings_Public", "LCGeneralSaveData", defaultValue: false);
		lobbyNameInputField.text = ES3.Load("HostSettings_Name", "LCGeneralSaveData", defaultValue);
		int num = ES3.Load("LastVerPlayed", "LCGeneralSaveData", -1);
		if (!flag)
		{
			if (GameNetworkManager.Instance.firstTimeInMenu && (GameNetworkManager.Instance.AlwaysDisplayNews || num != GameNetworkManager.Instance.gameVersionNum))
			{
				NewsPanel.SetActive(value: true);
				EventSystem.current.SetSelectedGameObject(NewsPanel.gameObject.GetComponentInChildren<Button>().gameObject);
			}
			else
			{
				EventSystem.current.SetSelectedGameObject(startHostButton.gameObject);
			}
		}
		string filePath = "noSaveNameSet";
		bool flag2 = true;
		for (int i = 0; i < 3; i++)
		{
			switch (i)
			{
			case 0:
				filePath = "LCSaveFile1";
				break;
			case 1:
				filePath = "LCSaveFile2";
				break;
			case 2:
				filePath = "LCSaveFile3";
				break;
			}
			if (!ES3.FileExists(filePath))
			{
				continue;
			}
			try
			{
				if (ES3.Load("FileGameVers", filePath, 0) < GameNetworkManager.Instance.compatibleFileCutoffVersion)
				{
					Debug.Log(string.Format("file vers: {0} not compatible; {1}", ES3.Load("FileGameVers", filePath, 0), GameNetworkManager.Instance.compatibleFileCutoffVersion));
					flag2 = false;
					filesCompatible[i] = false;
				}
			}
			catch (Exception arg)
			{
				Debug.LogError($"Error loading file #{i}! Deleting file since it's likely corrupted. Error: {arg}");
				ES3.DeleteFile(filePath);
			}
		}
		if (!flag2)
		{
			DisplayMenuNotification($"Some of your save files may not be compatible with version {GameNetworkManager.Instance.compatibleFileCutoffVersion} and may be corrupted if you play them.", "[ Back ]");
		}
		ES3.Save("LastVerPlayed", GameNetworkManager.Instance.gameVersionNum, "LCGeneralSaveData");
		int num2 = ES3.Load("TimesLoadedGame", "LCGeneralSaveData", 0);
		ES3.Save("TimesLoadedGame", num2 + 1, "LCGeneralSaveData");
		if (MenuAudio != null)
		{
			StartCoroutine(PlayMenuMusicDelayed());
		}
		SetIfChallengeMoonHasBeenCompleted();
	}

	private void SetIfChallengeMoonHasBeenCompleted()
	{
		int weekNumber = GameNetworkManager.Instance.GetWeekNumber();
		Debug.Log($"week num: {weekNumber}");
		bool flag = ES3.Load("FinishedChallenge", "LCChallengeFile", defaultValue: false);
		if (flag && ES3.Load("ChallengeWeekNum", "LCChallengeFile", weekNumber - 1) == weekNumber)
		{
			Debug.Log("Set challenge moon completed A");
			challengeScore = ES3.Load("ProfitEarned", "LCChallengeFile", 0);
			hasChallengeBeenCompleted = true;
		}
		else if (flag)
		{
			Debug.Log("Set challenge moon completed B");
			ES3.Save("FinishedChallenge", value: false, "LCChallengeFile");
			ES3.Save("SetChallengeFileMoney", value: false, "LCChallengeFile");
			ES3.Save("ChallengeWeekNum", weekNumber, "LCChallengeFile");
			ES3.Save("FinishedChallenge", value: false, "LCChallengeFile");
			ES3.Save("ProfitEarned", 0, "LCChallengeFile");
			ES3.Save("SubmittedScore", value: false, "LCChallengeFile");
		}
	}

	public void EnableLeaderboardDisplay(bool enable)
	{
		leaderboardContainer.SetActive(enable);
		hostSettingsPanel.SetActive(!enable);
		removeScoreButton.SetActive(enable);
		if (enable)
		{
			bool flag = ES3.Load("SubmittedScore", "LCChallengeFile", defaultValue: false);
			if (!GameNetworkManager.Instance.disableSteam && !requestingLeaderboard)
			{
				GetLeaderboardForChallenge(!flag);
			}
			else
			{
				ClearLeaderboard();
			}
			if (flag && ES3.Load("ProfitEarned", "LCChallengeFile", 0) != 0)
			{
				removeScoreButton.SetActive(value: true);
			}
		}
	}

	private async void RemoveLeaderboardScore()
	{
		if (requestingLeaderboard)
		{
			return;
		}
		requestingLeaderboard = true;
		if (!challengeLeaderboard.HasValue || !challengeLeaderboard.HasValue)
		{
			int weekNumber = GameNetworkManager.Instance.GetWeekNumber();
			challengeLeaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync($"challenge{weekNumber}", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
		}
		int[] details = new int[1] { 2 };
		await challengeLeaderboard.Value.ReplaceScore(0, details);
		if (challengeLeaderboard.HasValue && challengeLeaderboard.HasValue)
		{
			LeaderboardEntry[] entries = null;
			switch (leaderboardFilterType)
			{
			case 0:
				entries = await challengeLeaderboard.Value.GetScoresFromFriendsAsync();
				break;
			case 1:
				entries = await challengeLeaderboard.Value.GetScoresAroundUserAsync();
				break;
			case 2:
				entries = await challengeLeaderboard.Value.GetScoresAsync(20);
				break;
			}
			DisplayLeaderboardSlots(entries);
		}
		removeScoreButton.SetActive(value: false);
		requestingLeaderboard = false;
	}

	public void SetLeaderboardFilter(int filterId)
	{
		leaderboardFilterType = filterId;
	}

	public void RefreshLeaderboardButton()
	{
		GetLeaderboardForChallenge();
	}

	public void RemoveScoreFromLeaderboardButton()
	{
		RemoveLeaderboardScore();
	}

	private async void GetLeaderboardForChallenge(bool submitScore = false)
	{
		if (requestingLeaderboard || GameNetworkManager.Instance.disableSteam)
		{
			return;
		}
		requestingLeaderboard = true;
		int weekNumber = GameNetworkManager.Instance.GetWeekNumber();
		leaderboardHeaderText.text = "Challenge Moon " + GameNetworkManager.Instance.GetNameForWeekNumber(weekNumber) + " Results";
		challengeLeaderboard = await SteamUserStats.FindOrCreateLeaderboardAsync($"challenge{weekNumber}", LeaderboardSort.Descending, LeaderboardDisplay.Numeric);
		if (submitScore && !hasChallengeBeenCompleted && !ES3.Load("SubmittedScore", "LCChallengeFile", defaultValue: false))
		{
			LeaderboardUpdate? leaderboardUpdate = await challengeLeaderboard.Value.ReplaceScore(challengeScore);
			if (leaderboardUpdate.HasValue && leaderboardUpdate.HasValue)
			{
				ES3.Save("SubmittedScore", value: true, "LCChallengeFile");
				if (ES3.Load("ProfitEarned", "LCChallengeFile", 0) != 0)
				{
					removeScoreButton.SetActive(value: true);
				}
			}
		}
		if (challengeLeaderboard.HasValue && challengeLeaderboard.HasValue)
		{
			LeaderboardEntry[] entries = null;
			switch (leaderboardFilterType)
			{
			case 0:
				entries = await challengeLeaderboard.Value.GetScoresFromFriendsAsync();
				break;
			case 1:
				entries = await challengeLeaderboard.Value.GetScoresAroundUserAsync();
				break;
			case 2:
				entries = await challengeLeaderboard.Value.GetScoresAsync(20);
				break;
			}
			DisplayLeaderboardSlots(entries);
		}
		else
		{
			ClearLeaderboard();
			leaderboardLoadingText.text = "No entries to display!";
		}
		requestingLeaderboard = false;
	}

	private void ClearLeaderboard()
	{
		ChallengeLeaderboardSlot[] array = UnityEngine.Object.FindObjectsByType<ChallengeLeaderboardSlot>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		if (array.Length != 0)
		{
			for (int i = 0; i < array.Length; i++)
			{
				UnityEngine.Object.Destroy(array[i].gameObject);
			}
		}
		leaderboardSlotOffset = 0;
	}

	private void DisplayLeaderboardSlots(LeaderboardEntry[] entries)
	{
		ClearLeaderboard();
		if (entries == null)
		{
			leaderboardLoadingText.text = "No entries to display!";
			return;
		}
		if (displayLeaderboardSlotsCoroutine != null)
		{
			StopCoroutine(displayLeaderboardSlotsCoroutine);
		}
		leaderboardLoadingText.text = "Loading ranking...";
		displayLeaderboardSlotsCoroutine = StartCoroutine(CreateLeaderboardSlots(entries));
	}

	private IEnumerator CreateLeaderboardSlots(LeaderboardEntry[] entries)
	{
		for (int i = 0; i < entries.Length && i <= 150; i++)
		{
			int entryDetails = ((entries[i].Details == null || entries[i].Details.Length == 0) ? (-1) : entries[i].Details[0]);
			GameObject obj = UnityEngine.Object.Instantiate(leaderboardSlotPrefab, leaderboardSlotsContainer);
			obj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f + (float)leaderboardSlotOffset);
			leaderboardSlotOffset -= 54;
			obj.GetComponent<ChallengeLeaderboardSlot>().SetSlotValues(entries[i].User.Name, entries[i].GlobalRank, entries[i].Score, entries[i].User.Id, entryDetails);
			yield return new WaitForSeconds(0.06f);
		}
	}

	public void SubmitLeaderboardScore()
	{
		if (hasChallengeBeenCompleted)
		{
			GetLeaderboardForChallenge(submitScore: true);
		}
	}

	private IEnumerator connectionTimeOut()
	{
		yield return new WaitForSeconds(10.5f);
		logText.text = "Connection failed.";
		SetLoadingScreen(isLoading: false);
		menuButtons.SetActive(value: true);
		if (GameNetworkManager.Instance.currentLobby.HasValue)
		{
			Lobby value = GameNetworkManager.Instance.currentLobby.Value;
			GameNetworkManager.Instance.SetCurrentLobbyNull();
			try
			{
				value.Leave();
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to leave lobby; {arg}");
			}
		}
	}

	public void SetLoadingScreen(bool isLoading, RoomEnter result = RoomEnter.Error, string overrideMessage = "")
	{
		Debug.Log("Displaying menu message");
		if (isLoading)
		{
			menuButtons.SetActive(value: false);
			loadingScreen.SetActive(value: true);
			serverListUIContainer.SetActive(value: false);
			MenuAudio.volume = 0.2f;
			return;
		}
		MenuAudio.volume = 0.5f;
		menuButtons.SetActive(value: true);
		loadingScreen.SetActive(value: false);
		serverListUIContainer.SetActive(value: false);
		if (!string.IsNullOrEmpty(overrideMessage))
		{
			Debug.Log("Displaying menu message 2");
			DisplayMenuNotification(overrideMessage, "[ Back ]");
			return;
		}
		if (!string.IsNullOrEmpty(GameNetworkManager.Instance.disconnectionReasonMessage))
		{
			Debug.Log("Displaying menu message 3");
			DisplayMenuNotification(GameNetworkManager.Instance.disconnectionReasonMessage ?? "", "[ Back ]");
			GameNetworkManager.Instance.disconnectionReasonMessage = "";
			return;
		}
		Debug.Log("Failed loading; displaying notification");
		Debug.Log("result: " + result);
		switch (result)
		{
		case RoomEnter.Full:
			DisplayMenuNotification("The server is full!", "[ Back ]");
			break;
		case RoomEnter.DoesntExist:
			DisplayMenuNotification("The server no longer exists!", "[ Back ]");
			break;
		case RoomEnter.RatelimitExceeded:
			DisplayMenuNotification("You are joining/leaving too fast!", "[ Back ]");
			break;
		case RoomEnter.MemberBlockedYou:
			DisplayMenuNotification("A member of the server has blocked you!", "[ Back ]");
			break;
		case RoomEnter.Error:
			DisplayMenuNotification("An error occured!", "[ Back ]");
			break;
		case RoomEnter.NotAllowed:
			DisplayMenuNotification("Connection was not approved!", "[ Back ]");
			break;
		case RoomEnter.YouBlockedMember:
			DisplayMenuNotification("You have blocked someone in this server!", "[ Back ]");
			break;
		case RoomEnter.Banned:
			DisplayMenuNotification("Unable to join because you have been banned!", "[ Back ]");
			break;
		default:
			DisplayMenuNotification("Something went wrong!", "[ Back ]");
			break;
		}
	}

	public void DisplayMenuNotification(string notificationText, string buttonText)
	{
		if (!isInitScene)
		{
			Debug.Log("Displaying menu notification: " + notificationText);
			menuNotificationText.text = notificationText;
			menuNotificationButtonText.text = buttonText;
			menuNotification.SetActive(value: true);
			EventSystem.current.SetSelectedGameObject(menuNotification.GetComponentInChildren<Button>().gameObject);
		}
	}

	public void StartConnectionTimeOutTimer()
	{
		StartCoroutine(connectionTimeOut());
	}

	public void StartAClient()
	{
		LAN_HostSetAllowRemoteConnections();
		startingAClient = true;
		logText.text = "Connecting to server...";
		try
		{
			GameNetworkManager.Instance.SetConnectionDataBeforeConnecting();
			GameNetworkManager.Instance.SubscribeToConnectionCallbacks();
			if (NetworkManager.Singleton.StartClient())
			{
				Debug.Log("Started a client");
				logText.text = "Connecting to host...";
				SetLoadingScreen(isLoading: true);
			}
			else
			{
				Debug.Log("Could not start client");
				SetLoadingScreen(isLoading: false);
				startingAClient = false;
				logText.text = "Connection failed. Try again?";
			}
		}
		catch (Exception arg)
		{
			logText.text = "Connection failed.";
			Debug.Log($"Connection failed: {arg}");
		}
	}

	public void StartHosting()
	{
		SetLoadingScreen(isLoading: true);
		try
		{
			if (NetworkManager.Singleton.StartHost())
			{
				Debug.Log("started host!");
				Debug.Log($"are we in a server?: {NetworkManager.Singleton.IsServer}");
				NetworkManager.Singleton.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Single);
				StartCoroutine(delayedStartScene());
			}
			else
			{
				SetLoadingScreen(isLoading: false);
				logText.text = "Failed to start server; 20";
			}
		}
		catch (Exception arg)
		{
			logText.text = "Failed to start server; 30";
			Debug.Log($"Server connection failed: {arg}");
		}
	}

	private IEnumerator delayedStartScene()
	{
		logText.text = "Started server, joining...";
		yield return new WaitForSeconds(1f);
		AudioListener.volume = 0f;
		yield return new WaitForSeconds(0.1f);
		NetworkManager.Singleton.SceneManager.LoadScene("SampleSceneRelay", LoadSceneMode.Single);
	}

	private void OnEnable()
	{
		startHostButton?.onClick.AddListener(ClickHostButton);
		leaveButton?.onClick.AddListener(ClickQuitButton);
	}

	public void ClickHostButton()
	{
		Debug.Log("host button pressed");
		EnableLeaderboardDisplay(enable: false);
		HostSettingsScreen.SetActive(value: true);
		if (GameNetworkManager.Instance.disableSteam)
		{
			HostSettingsOptionsLAN.SetActive(value: true);
			HostSettingsOptionsNormal.SetActive(value: false);
		}
		if ((bool)UnityEngine.Object.FindObjectOfType<SaveFileUISlot>())
		{
			UnityEngine.Object.FindObjectOfType<SaveFileUISlot>().SetButtonColorForAllFileSlots();
		}
		HostSetLobbyPublic(hostSettings_LobbyPublic);
	}

	public void ConfirmHostButton()
	{
		if (string.IsNullOrEmpty(lobbyNameInputField.text))
		{
			tipTextHostSettings.text = "Enter a lobby name!";
			return;
		}
		HostSettingsScreen.SetActive(value: false);
		if (lobbyNameInputField.text.Length > 40)
		{
			lobbyNameInputField.text = lobbyNameInputField.text.Substring(0, 40);
		}
		ES3.Save("HostSettings_Name", lobbyNameInputField.text, "LCGeneralSaveData");
		ES3.Save("HostSettings_Public", hostSettings_LobbyPublic, "LCGeneralSaveData");
		if (!hostSettings_LobbyPublic)
		{
			lobbyTagInputField.text = "";
		}
		GameNetworkManager.Instance.lobbyHostSettings = new HostSettings(lobbyNameInputField.text, hostSettings_LobbyPublic, lobbyTagInputField.text);
		GameNetworkManager.Instance.StartHost();
	}

	public void LAN_HostSetLocal()
	{
		Debug.Log("Clicked local connection only");
		NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = "127.0.0.1";
		lanSetLocalButtonAnimator.SetBool("isPressed", value: true);
		lanSetAllowRemoteButtonAnimator.SetBool("isPressed", value: false);
	}

	public void LAN_HostSetAllowRemoteConnections()
	{
		Debug.Log("Clicked allow remote connections");
		NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.ServerListenAddress = "0.0.0.0";
		lanSetLocalButtonAnimator.SetBool("isPressed", value: false);
		lanSetAllowRemoteButtonAnimator.SetBool("isPressed", value: true);
	}

	public void HostSetLobbyPublic(bool setPublic = false)
	{
		if (GameNetworkManager.Instance.disableSteam)
		{
			lanSetLocalButtonAnimator.SetBool("isPressed", value: true);
			lanSetAllowRemoteButtonAnimator.SetBool("isPressed", value: false);
			LAN_HostSetLocal();
			privatePublicDescription.text = "";
			return;
		}
		hostSettings_LobbyPublic = setPublic;
		setPrivateButtonAnimator.SetBool("isPressed", !setPublic);
		setPublicButtonAnimator.SetBool("isPressed", setPublic);
		if (setPublic)
		{
			privatePublicDescription.text = "PUBLIC means your game will be visible on the server list for all to see.";
		}
		else
		{
			privatePublicDescription.text = "PRIVATE means you must send invites through Steam for players to join.";
		}
	}

	public void FilledRoomNameField()
	{
		tipTextHostSettings.text = "";
	}

	public void EnableUIPanel(GameObject enablePanel)
	{
		enablePanel.SetActive(value: true);
	}

	public void DisableUIPanel(GameObject enablePanel)
	{
		enablePanel.SetActive(value: false);
	}

	private void ClickJoinButton()
	{
		Debug.Log("join button pressed");
		startClientButton.gameObject.SetActive(value: false);
		inputFieldGameObject.SetActive(value: true);
	}

	private void ClickQuitButton()
	{
		Application.Quit();
	}
}
