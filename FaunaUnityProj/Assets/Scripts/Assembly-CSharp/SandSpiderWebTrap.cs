using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class SandSpiderWebTrap : MonoBehaviour, IHittable
{
	public SandSpiderAI mainScript;

	private bool hinderingLocalPlayer;

	public PlayerControllerB currentTrappedPlayer;

	public Transform leftBone;

	public Transform rightBone;

	public Transform centerOfWeb;

	public int trapID;

	public float zScale = 1f;

	public AudioSource webAudio;

	private bool webHasBeenBroken;

	public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		if (!webHasBeenBroken)
		{
			webHasBeenBroken = true;
			mainScript.BreakWebServerRpc(trapID, (int)playerWhoHit.playerClientId);
		}
		return true;
	}

	private void OnEnable()
	{
		StartOfRound.Instance.playerTeleportedEvent.AddListener(PlayerLeaveWeb);
	}

	private void OnDisable()
	{
		StartOfRound.Instance.playerTeleportedEvent.RemoveListener(PlayerLeaveWeb);
		PlayerLeaveWeb(GameNetworkManager.Instance.localPlayerController);
	}

	private void OnDestroy()
	{
		PlayerLeaveWeb(GameNetworkManager.Instance.localPlayerController);
	}

	public void Update()
	{
		if (currentTrappedPlayer != null)
		{
			CallPlayerLeaveWebOnDeath();
			Vector3 worldPosition = currentTrappedPlayer.transform.position + Vector3.up * 0.6f;
			rightBone.LookAt(worldPosition);
			leftBone.LookAt(worldPosition);
		}
		else
		{
			rightBone.LookAt(centerOfWeb);
			leftBone.LookAt(centerOfWeb);
		}
		base.transform.localScale = Vector3.Lerp(base.transform.localScale, new Vector3(1f, 1f, zScale), 8f * Time.deltaTime);
	}

	private void Awake()
	{
		base.transform.localScale = new Vector3(0.7f, 0.7f, 0.02f);
	}

	private void CallPlayerLeaveWebOnDeath()
	{
		if (NetworkManager.Singleton != null)
		{
			if (NetworkManager.Singleton.IsHost && !currentTrappedPlayer.isPlayerControlled && !currentTrappedPlayer.isPlayerDead)
			{
				currentTrappedPlayer = null;
				mainScript.PlayerLeaveWebServerRpc(trapID, (int)currentTrappedPlayer.playerClientId);
			}
			else if (GameNetworkManager.Instance.localPlayerController == currentTrappedPlayer && GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				currentTrappedPlayer = null;
				currentTrappedPlayer.isMovementHindered--;
				currentTrappedPlayer.hinderedMultiplier = Mathf.Clamp(currentTrappedPlayer.hinderedMultiplier * 0.4f, 1f, 100f);
				hinderingLocalPlayer = false;
				mainScript.PlayerLeaveWebServerRpc(trapID, (int)currentTrappedPlayer.playerClientId);
			}
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (GameNetworkManager.Instance == null || hinderingLocalPlayer)
		{
			return;
		}
		PlayerControllerB component = other.GetComponent<PlayerControllerB>();
		if (component != null && component == GameNetworkManager.Instance.localPlayerController)
		{
			component.isMovementHindered++;
			component.hinderedMultiplier *= 2.5f;
			hinderingLocalPlayer = true;
			if (currentTrappedPlayer == null)
			{
				currentTrappedPlayer = GameNetworkManager.Instance.localPlayerController;
			}
			if (mainScript != null)
			{
				mainScript.PlayerTripWebServerRpc(trapID, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	private void PlayerLeaveWeb(PlayerControllerB playerScript)
	{
		Debug.Log("Player leave web called");
		if (hinderingLocalPlayer)
		{
			hinderingLocalPlayer = false;
			playerScript.isMovementHindered--;
			playerScript.hinderedMultiplier *= 0.4f;
			if (currentTrappedPlayer == playerScript)
			{
				currentTrappedPlayer = null;
			}
			webAudio.Stop();
			if (mainScript != null)
			{
				mainScript.PlayerLeaveWebServerRpc(trapID, (int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (hinderingLocalPlayer)
		{
			PlayerControllerB component = other.GetComponent<PlayerControllerB>();
			if (component != null && component == GameNetworkManager.Instance.localPlayerController)
			{
				PlayerLeaveWeb(component);
			}
		}
	}
}
