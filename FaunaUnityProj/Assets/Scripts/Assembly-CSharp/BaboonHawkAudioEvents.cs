using UnityEngine;

public class BaboonHawkAudioEvents : MonoBehaviour
{
	public AudioSource audioToPlay;

	public AudioClip[] randomClips;

	public Animator thisAnimator;

	private float timeLastAudioPlayed;

	public ParticleSystem particle;

	public void PlayParticleWithChildren()
	{
		particle.Play(withChildren: true);
	}

	public void PlayAudio1RandomClipWithMinSpeedCondition()
	{
		if (!(Time.realtimeSinceStartup - timeLastAudioPlayed < 0.2f) && (!(Mathf.Abs(thisAnimator.GetFloat("VelocityX")) < 0.5f) || !(Mathf.Abs(thisAnimator.GetFloat("VelocityZ")) < 0.5f)))
		{
			timeLastAudioPlayed = Time.realtimeSinceStartup;
			int num = Random.Range(0, randomClips.Length);
			audioToPlay.spatialize = false;
			audioToPlay.PlayOneShot(randomClips[num]);
			WalkieTalkie.TransmitOneShotAudio(audioToPlay, randomClips[num]);
			RoundManager.Instance.PlayAudibleNoise(base.transform.position, 7f, 0.4f, 0, noiseIsInsideClosedShip: false, 24751);
		}
	}
}
