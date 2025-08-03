using Unity.Netcode;
using UnityEngine;

public class HighAndLowAltitudeAudio : MonoBehaviour
{
	public AudioSource HighAudio;

	public AudioSource LowAudio;

	public float maxAltitude;

	public float minAltitude;

	public bool transitionFromDayToNight;

	public AudioSource stopAudioAtTime;

	public float normalizedDayTimeForEvent = 0.7f;

	private void Start()
	{
	}

	private void Update()
	{
		if (!(GameNetworkManager.Instance.localPlayerController == null) && !(NetworkManager.Singleton == null))
		{
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
			{
				HighAudio.volume = 0f;
				LowAudio.volume = 0f;
			}
			else if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				SetAudioVolumeBasedOnAltitude(GameNetworkManager.Instance.localPlayerController.transform.position.y);
			}
			else if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
			{
				SetAudioVolumeBasedOnAltitude(GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.transform.position.y);
			}
		}
	}

	private void SetAudioVolumeBasedOnAltitude(float playerHeight)
	{
		if (transitionFromDayToNight)
		{
			HighAudio.volume = Mathf.Lerp(1f, 0f, TimeOfDay.Instance.normalizedTimeOfDay);
			LowAudio.volume = Mathf.Abs(HighAudio.volume - 1f);
			if (stopAudioAtTime.isPlaying && TimeOfDay.Instance.currentDayTimeStarted && TimeOfDay.Instance.normalizedTimeOfDay > normalizedDayTimeForEvent)
			{
				stopAudioAtTime.Stop();
			}
		}
		else
		{
			HighAudio.volume = Mathf.Clamp((playerHeight - minAltitude) / maxAltitude, 0f, 1f);
			LowAudio.volume = Mathf.Abs(HighAudio.volume - 1f);
		}
	}
}
