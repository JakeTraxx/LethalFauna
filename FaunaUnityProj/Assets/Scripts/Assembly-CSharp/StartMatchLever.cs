using Unity.Netcode;
using UnityEngine;

public class StartMatchLever : NetworkBehaviour
{
	public bool singlePlayerEnabled;

	public bool leverHasBeenPulled;

	public InteractTrigger triggerScript;

	public StartOfRound playersManager;

	public Animator leverAnimatorObject;

	private float updateInterval;

	private bool clientSentRPC;

	public bool hasDisplayedTimeWarning;

	public void LeverAnimation()
	{
		if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead && !playersManager.travellingToNewLevel && (!playersManager.inShipPhase || playersManager.connectedPlayersAmount + 1 > 1 || singlePlayerEnabled))
		{
			if (playersManager.shipHasLanded)
			{
				PullLeverAnim(leverPulled: false);
				clientSentRPC = true;
				PlayLeverPullEffectsServerRpc(leverPulled: false);
			}
			else if (playersManager.inShipPhase)
			{
				PullLeverAnim(leverPulled: true);
				clientSentRPC = true;
				PlayLeverPullEffectsServerRpc(leverPulled: true);
			}
		}
	}

	private void PullLeverAnim(bool leverPulled)
	{
		Debug.Log($"Lever animation: setting bool to {leverPulled}");
		leverAnimatorObject.SetBool("pullLever", leverPulled);
		leverHasBeenPulled = leverPulled;
		triggerScript.interactable = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayLeverPullEffectsServerRpc(bool leverPulled)
			{
				PlayLeverPullEffectsClientRpc(leverPulled);
			}

	[ClientRpc]
	private void PlayLeverPullEffectsClientRpc(bool leverPulled)
{		{
			if (clientSentRPC)
			{
				clientSentRPC = false;
				Debug.Log("Sent lever animation RPC on this client");
			}
			else
			{
				PullLeverAnim(leverPulled);
			}
		}
}
	public void PullLever()
	{
		if (leverHasBeenPulled)
		{
			StartGame();
		}
		else
		{
			EndGame();
		}
	}

	public void StartGame()
	{
		if (playersManager.travellingToNewLevel || !playersManager.inShipPhase || (playersManager.connectedPlayersAmount + 1 <= 1 && !singlePlayerEnabled))
		{
			return;
		}
		if (playersManager.fullyLoadedPlayers.Count >= playersManager.connectedPlayersAmount + 1)
		{
			if (!base.IsServer)
			{
				playersManager.StartGameServerRpc();
			}
			else
			{
				playersManager.StartGame();
			}
		}
		else
		{
			triggerScript.hoverTip = "[ Players are loading. ]";
			Debug.Log("Attempted to start the game while routing to a new planet");
			Debug.Log($"Number of loaded players: {playersManager.fullyLoadedPlayers}");
			updateInterval = 4f;
			CancelStartGame();
		}
	}

	[ClientRpc]
	public void CancelStartGameClientRpc()
			{
				CancelStartGame();
			}

	private void CancelStartGame()
	{
		leverHasBeenPulled = false;
		leverAnimatorObject.SetBool("pullLever", value: false);
	}

	public void EndGame()
	{
		if ((GameNetworkManager.Instance.localPlayerController.isPlayerDead || playersManager.shipHasLanded) && !playersManager.shipIsLeaving && !playersManager.shipLeftAutomatically)
		{
			triggerScript.interactable = false;
			playersManager.shipIsLeaving = true;
			playersManager.EndGameServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	public void BeginHoldingInteractOnLever()
	{
		if (playersManager.inShipPhase && !hasDisplayedTimeWarning && StartOfRound.Instance.currentLevel.planetHasTime)
		{
			hasDisplayedTimeWarning = true;
			if (TimeOfDay.Instance.daysUntilDeadline <= 0)
			{
				triggerScript.timeToHold = 4f;
				HUDManager.Instance.DisplayTip("HALT!", "You have 0 days left to meet the quota. Use the terminal to route to the company and sell.", isWarning: true);
			}
		}
	}

	private void Start()
	{
		if (!base.IsServer)
		{
			triggerScript.hoverTip = "[ Must be server host. ]";
			triggerScript.interactable = false;
		}
	}

	private void Update()
	{
		if (updateInterval <= 0f)
		{
			updateInterval = 2f;
			if (!leverHasBeenPulled)
			{
				if (!base.IsServer && !GameNetworkManager.Instance.gameHasStarted)
				{
					return;
				}
				if (playersManager.connectedPlayersAmount + 1 > 1 || singlePlayerEnabled)
				{
					if (GameNetworkManager.Instance.gameHasStarted)
					{
						triggerScript.hoverTip = "Land ship : [LMB]";
					}
					else
					{
						triggerScript.hoverTip = "Start game : [LMB]";
					}
				}
				else
				{
					triggerScript.hoverTip = "[ At least two players needed to start! ]";
				}
			}
			else
			{
				triggerScript.hoverTip = "Start ship : [LMB]";
			}
		}
		else
		{
			updateInterval -= Time.deltaTime;
		}
	}
}
