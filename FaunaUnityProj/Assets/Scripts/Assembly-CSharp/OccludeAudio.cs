using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class OccludeAudio : MonoBehaviour
{
	private AudioLowPassFilter lowPassFilter;

	private AudioReverbFilter reverbFilter;

	public bool useReverb;

	private bool occluded;

	private AudioSource thisAudio;

	private float checkInterval;

	public bool overridingLowPass;

	public float lowPassOverride = 20000f;

	public bool debugLog;

	private void Start()
	{
		lowPassFilter = base.gameObject.GetComponent<AudioLowPassFilter>();
		if (lowPassFilter == null)
		{
			lowPassFilter = base.gameObject.AddComponent<AudioLowPassFilter>();
			lowPassFilter.cutoffFrequency = 20000f;
		}
		if (useReverb)
		{
			reverbFilter = base.gameObject.GetComponent<AudioReverbFilter>();
			if (reverbFilter == null)
			{
				reverbFilter = base.gameObject.AddComponent<AudioReverbFilter>();
			}
			reverbFilter.reverbPreset = AudioReverbPreset.Hallway;
			reverbFilter.reverbPreset = AudioReverbPreset.User;
			reverbFilter.dryLevel = -1f;
			reverbFilter.decayTime = 0.8f;
			reverbFilter.room = -2300f;
		}
		thisAudio = base.gameObject.GetComponent<AudioSource>();
		if (StartOfRound.Instance != null && Physics.Linecast(base.transform.position, StartOfRound.Instance.audioListener.transform.position, 256, QueryTriggerInteraction.Ignore))
		{
			occluded = true;
		}
		else
		{
			occluded = false;
		}
		checkInterval = Random.Range(0f, 0.4f);
	}

	private void Update()
	{
		if (thisAudio.isVirtual)
		{
			return;
		}
		if (useReverb && GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
		{
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory || (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript.isInsideFactory))
			{
				if (SoundManager.Instance.echoEnabled)
				{
					reverbFilter.dryLevel = Mathf.Lerp(reverbFilter.dryLevel, Mathf.Clamp(0f - 4.5f * (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) / (thisAudio.maxDistance / 12f)), -1270f, -1f), Time.deltaTime * 8f);
				}
				else
				{
					reverbFilter.dryLevel = Mathf.Lerp(reverbFilter.dryLevel, Mathf.Clamp(0f - 3.4f * (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) / (thisAudio.maxDistance / 5f)), -300f, -1f), Time.deltaTime * 8f);
				}
				reverbFilter.enabled = true;
			}
			else
			{
				reverbFilter.enabled = false;
			}
		}
		if (!overridingLowPass)
		{
			if (occluded)
			{
				lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, Mathf.Clamp(2500f / (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position) / (thisAudio.maxDistance / 2f)), 900f, 4000f), Time.deltaTime * 8f);
			}
			else
			{
				lowPassFilter.cutoffFrequency = Mathf.Lerp(lowPassFilter.cutoffFrequency, 10000f, Time.deltaTime * 8f);
			}
		}
		else
		{
			lowPassFilter.cutoffFrequency = lowPassOverride;
		}
		if (checkInterval >= 0.5f)
		{
			checkInterval = Random.Range(0f, 0.04f);
			if (Vector3.Distance(base.transform.position, StartOfRound.Instance.audioListener.transform.position) < Mathf.Min(thisAudio.maxDistance, 110f))
			{
				if (Physics.Linecast(base.transform.position, StartOfRound.Instance.audioListener.transform.position, out var _, 256, QueryTriggerInteraction.Ignore))
				{
					_ = debugLog;
					occluded = true;
				}
				else
				{
					occluded = false;
				}
			}
		}
		else
		{
			checkInterval += Time.deltaTime;
		}
	}
}
