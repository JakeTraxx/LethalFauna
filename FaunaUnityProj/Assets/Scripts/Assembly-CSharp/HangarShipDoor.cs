using TMPro;
using Unity.Netcode;
using UnityEngine;

public class HangarShipDoor : MonoBehaviour
{
	public InteractTrigger triggerScript;

	public TextMeshProUGUI doorPowerDisplay;

	private StartOfRound playersManager;

	public Animator shipDoorsAnimator;

	public bool buttonsEnabled = true;

	public float doorPower = 1f;

	public float doorPowerDuration = 20f;

	public bool overheated;

	public bool doorsOpenedInGameOverAnimation;

	public GameObject hydraulicsDisplay;

	private bool hydraulicsScreenDisplayed = true;

	public Transform outsideDoorPoint;

	public void Update()
	{
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		SetScreenDisplay();
		if (StartOfRound.Instance.hangarDoorsClosed && StartOfRound.Instance.shipHasLanded)
		{
			overheated = false;
			triggerScript.interactable = true;
			if (doorPower > 0f)
			{
				doorPower = Mathf.Clamp(doorPower - Time.deltaTime / doorPowerDuration, 0f, 1f);
			}
			else if (NetworkManager.Singleton.IsServer)
			{
				PlayDoorAnimation(closed: false);
				StartOfRound.Instance.SetShipDoorsOverheatServerRpc();
			}
		}
		else
		{
			doorPower = Mathf.Clamp(doorPower + Time.deltaTime / (doorPowerDuration * 0.22f), 0f, 1f);
			if (overheated && doorPower >= 1f)
			{
				overheated = false;
				triggerScript.interactable = true;
			}
		}
		doorPowerDisplay.text = $"{Mathf.RoundToInt(doorPower * 100f)}%";
	}

	private void SetScreenDisplay()
	{
		bool flag = true;
		if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			flag = GameNetworkManager.Instance.localPlayerController.isInElevator;
		}
		else if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
		{
			flag = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInElevator;
		}
		if (hydraulicsScreenDisplayed != flag)
		{
			hydraulicsScreenDisplayed = flag;
			hydraulicsDisplay.SetActive(flag);
		}
	}

	public void PlayDoorAnimation(bool closed)
	{
		if (buttonsEnabled)
		{
			shipDoorsAnimator.SetBool("Closed", closed);
		}
	}

	public void SetDoorClosed()
	{
		playersManager.SetShipDoorsClosed(closed: true);
	}

	public void SetDoorOpen()
	{
		playersManager.SetShipDoorsClosed(closed: false);
	}

	public void SetDoorButtonsEnabled(bool doorButtonsEnabled)
	{
		buttonsEnabled = doorButtonsEnabled;
	}

	private void Start()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
	}
}
