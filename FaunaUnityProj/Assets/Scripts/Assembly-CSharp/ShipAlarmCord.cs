using Unity.Netcode;
using UnityEngine;

public class ShipAlarmCord : NetworkBehaviour
{
	private bool hornBlaring;

	private float cordPulledDownTimer;

	public Animator cordAnimator;

	public AudioSource hornClose;

	public AudioSource hornFar;

	public AudioSource cordAudio;

	public AudioClip cordPullSFX;

	private bool otherClientHoldingCord;

	private float playAudibleNoiseInterval;

	private int timesPlayingAtOnce;

	public PlaceableShipObject shipObjectScript;

	private int unlockableID;

	private bool localClientHoldingCord;

	private void Start()
	{
		unlockableID = shipObjectScript.unlockableID;
	}

	public void HoldCordDown()
	{
		if (otherClientHoldingCord)
		{
			return;
		}
		Debug.Log("HOLD horn local client called");
		cordPulledDownTimer = 0.3f;
		if (!hornBlaring)
		{
			Debug.Log("Hornblaring setting to true!");
			localClientHoldingCord = true;
			cordAnimator.SetBool("pulled", value: true);
			cordAudio.PlayOneShot(cordPullSFX);
			WalkieTalkie.TransmitOneShotAudio(cordAudio, cordPullSFX);
			RoundManager.Instance.PlayAudibleNoise(cordAudio.transform.position, 4.5f, 0.5f, 0, StartOfRound.Instance.hangarDoorsClosed);
			hornBlaring = true;
			if (!hornClose.isPlaying)
			{
				hornClose.Play();
				hornFar.Play();
			}
			PullCordServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	public void StopHorn()
	{
		if (hornBlaring)
		{
			Debug.Log("Stop horn local client called");
			localClientHoldingCord = false;
			hornBlaring = false;
			cordAnimator.SetBool("pulled", value: false);
			StopPullingCordServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		}
	}

	private void Update()
	{
		if (hornBlaring)
		{
			hornFar.volume = Mathf.Min(hornFar.volume + Time.deltaTime * 0.45f, 1f);
			hornFar.pitch = Mathf.Lerp(hornFar.pitch, 0.97f, Time.deltaTime * 0.8f);
			hornClose.volume = Mathf.Min(hornClose.volume + Time.deltaTime * 0.45f, 1f);
			hornClose.pitch = Mathf.Lerp(hornClose.pitch, 0.97f, Time.deltaTime * 0.8f);
			if (hornClose.volume > 0.6f && playAudibleNoiseInterval <= 0f)
			{
				playAudibleNoiseInterval = 1f;
				RoundManager.Instance.PlayAudibleNoise(hornClose.transform.position, 30f, 0.8f, timesPlayingAtOnce, noiseIsInsideClosedShip: false, 14155);
				timesPlayingAtOnce++;
			}
			else
			{
				playAudibleNoiseInterval -= Time.deltaTime;
			}
		}
		else
		{
			hornFar.volume = Mathf.Max(hornFar.volume - Time.deltaTime * 0.3f, 0f);
			hornFar.pitch = Mathf.Lerp(hornFar.pitch, 0.88f, Time.deltaTime * 0.5f);
			hornClose.volume = Mathf.Max(hornClose.volume - Time.deltaTime * 0.3f, 0f);
			hornClose.pitch = Mathf.Lerp(hornClose.pitch, 0.88f, Time.deltaTime * 0.5f);
			if (hornClose.volume <= 0f)
			{
				hornClose.Stop();
				hornFar.Stop();
				timesPlayingAtOnce = 0;
			}
		}
		if (localClientHoldingCord)
		{
			if (cordPulledDownTimer >= 0f && !StartOfRound.Instance.unlockablesList.unlockables[unlockableID].inStorage)
			{
				cordPulledDownTimer -= Time.deltaTime;
			}
			else if (hornBlaring)
			{
				StopHorn();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PullCordServerRpc(int playerPullingCord)
			{
				PullCordClientRpc(playerPullingCord);
			}

	[ClientRpc]
	public void PullCordClientRpc(int playerPullingCord)
{		Debug.Log("Received pull cord client rpc");
		if (!(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerPullingCord)
		{
			otherClientHoldingCord = true;
			hornBlaring = true;
			cordAnimator.SetBool("pulled", value: true);
			cordAudio.PlayOneShot(cordPullSFX);
			WalkieTalkie.TransmitOneShotAudio(cordAudio, cordPullSFX);
			if (!hornClose.isPlaying)
			{
				hornClose.Play();
			}
			if (!hornFar.isPlaying)
			{
				hornFar.Play();
			}
		}
}
	[ServerRpc(RequireOwnership = false)]
	public void StopPullingCordServerRpc(int playerPullingCord)
			{
				StopPullingCordClientRpc(playerPullingCord);
			}

	[ClientRpc]
	public void StopPullingCordClientRpc(int playerPullingCord)
{		Debug.Log("Received STOP pull cord client rpc");
		if (!(GameNetworkManager.Instance.localPlayerController == null) && (int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerPullingCord)
		{
			otherClientHoldingCord = false;
			hornBlaring = false;
			cordAnimator.SetBool("pulled", value: false);
			if (StartOfRound.Instance.unlockablesList.unlockables[unlockableID].inStorage)
			{
				hornFar.volume = 0f;
				hornFar.pitch = 0.8f;
				hornClose.volume = 0f;
				hornClose.pitch = 0.8f;
			}
		}
}}
