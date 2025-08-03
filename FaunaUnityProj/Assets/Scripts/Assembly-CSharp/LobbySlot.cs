using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

public class LobbySlot : MonoBehaviour
{
	public MenuManager menuScript;

	public TextMeshProUGUI LobbyName;

	public TextMeshProUGUI playerCount;

	public SteamId lobbyId;

	public Lobby thisLobby;

	private static Coroutine timeOutLobbyRefreshCoroutine;

	private void Awake()
	{
		menuScript = Object.FindObjectOfType<MenuManager>();
	}

	public void JoinButton()
	{
		if (!GameNetworkManager.Instance.waitingForLobbyDataRefresh)
		{
			JoinLobbyAfterVerifying(thisLobby, lobbyId);
		}
	}

	public static void JoinLobbyAfterVerifying(Lobby lobby, SteamId lobbyId)
	{
		if (GameNetworkManager.Instance.waitingForLobbyDataRefresh)
		{
			return;
		}
		MenuManager menuManager = Object.FindObjectOfType<MenuManager>();
		if (!(menuManager == null))
		{
			menuManager.serverListUIContainer.SetActive(value: false);
			menuManager.menuButtons.SetActive(value: true);
			Debug.Log($"Lobby id joining: {lobbyId}");
			SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataRefresh;
			GameNetworkManager.Instance.waitingForLobbyDataRefresh = true;
			Debug.Log("refreshing lobby...");
			if (lobby.Refresh())
			{
				timeOutLobbyRefreshCoroutine = GameNetworkManager.Instance.StartCoroutine(GameNetworkManager.Instance.TimeOutLobbyRefresh());
				Debug.Log("Waiting for lobby data refresh");
				Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: true);
			}
			else
			{
				Debug.Log("Could not refresh lobby");
				SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataRefresh;
				Object.FindObjectOfType<MenuManager>().SetLoadingScreen(isLoading: false, RoomEnter.Error, "Error! Could not get the lobby data. Are you offline?");
			}
		}
	}

	public static void OnLobbyDataRefresh(Lobby lobby)
	{
		if (timeOutLobbyRefreshCoroutine != null)
		{
			GameNetworkManager.Instance.StopCoroutine(timeOutLobbyRefreshCoroutine);
			timeOutLobbyRefreshCoroutine = null;
		}
		if (!GameNetworkManager.Instance.waitingForLobbyDataRefresh)
		{
			Debug.Log("Not waiting for lobby data refresh; returned");
			return;
		}
		GameNetworkManager.Instance.waitingForLobbyDataRefresh = false;
		SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataRefresh;
		Debug.Log($"Got lobby data refresh!; {lobby.Id}");
		Debug.Log($"Members in lobby: {lobby.MemberCount}");
		if (GameNetworkManager.Instance.LobbyDataIsJoinable(lobby))
		{
			GameNetworkManager.Instance.JoinLobby(lobby, lobby.Id);
		}
	}

	private void Update()
	{
	}
}
