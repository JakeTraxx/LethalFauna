using Unity.Netcode;
using UnityEngine;

public class RandomPeriodicAudioPlayer : NetworkBehaviour
{
	public GrabbableObject attachedGrabbableObject;

	public AudioClip[] randomClips;

	public AudioSource thisAudio;

	public float audioMinInterval;

	public float audioMaxInterval;

	public float audioChancePercent;

	private float currentInterval;

	private float lastIntervalTime;

	private void Update()
	{
		if (base.IsServer && !(GameNetworkManager.Instance.localPlayerController == null) && (!(attachedGrabbableObject != null) || !attachedGrabbableObject.deactivated) && Time.realtimeSinceStartup - lastIntervalTime > currentInterval)
		{
			lastIntervalTime = Time.realtimeSinceStartup;
			currentInterval = Random.Range(audioMinInterval, audioMaxInterval);
			if (Random.Range(0f, 100f) < audioChancePercent)
			{
				PlayRandomAudioClientRpc(Random.Range(0, randomClips.Length));
			}
		}
	}

	[ClientRpc]
	public void PlayRandomAudioClientRpc(int clipIndex)
			{
				PlayAudio(clipIndex);
			}

	private void PlayAudio(int clipIndex)
	{
		AudioClip clip = randomClips[clipIndex];
		thisAudio.PlayOneShot(clip, 1f);
		WalkieTalkie.TransmitOneShotAudio(thisAudio, clip);
		RoundManager.Instance.PlayAudibleNoise(thisAudio.transform.position, 7f, 0.6f);
	}
}
