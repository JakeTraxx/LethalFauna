using UnityEngine;
using UnityEngine.Events;

public class PlayAudioAnimationEvent : MonoBehaviour
{
	public AudioSource audioToPlay;

	public AudioSource audioToPlayB;

	public AudioClip audioClip;

	public AudioClip audioClip2;

	public AudioClip audioClip3;

	public AudioClip[] randomClips;

	public AudioClip[] randomClips2;

	public bool randomizePitch;

	public ParticleSystem particle;

	public UnityEvent onAnimationEventCalled;

	public GameObject destroyObject;

	private float timeAtStart;

	public bool playAudibleNoise;

	public bool enableAudio = true;

	private void Start()
	{
		timeAtStart = Time.timeSinceLevelLoad;
	}

	public void ScreenShake()
	{
		if (!(StartOfRound.Instance.audioListener == null))
		{
			float num = Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, audioToPlay.transform.position);
			if (num < 6f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
			else if (num < 12f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			}
		}
	}

	public void PlayAudio1()
	{
		if (!(Time.timeSinceLevelLoad - timeAtStart < 2f))
		{
			audioToPlay.clip = audioClip;
			audioToPlay.Play();
			WalkieTalkie.TransmitOneShotAudio(audioToPlay, audioClip);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void PlayAudio1RandomClip()
	{
		if (!enableAudio)
		{
			return;
		}
		int num = Random.Range(0, randomClips.Length);
		if (!(randomClips[num] == null))
		{
			audioToPlay.spatialize = false;
			audioToPlay.PlayOneShot(randomClips[num]);
			WalkieTalkie.TransmitOneShotAudio(audioToPlay, randomClips[num]);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void PlayAudio2RandomClip()
	{
		int num = Random.Range(0, randomClips2.Length);
		if (!(randomClips2[num] == null))
		{
			audioToPlayB.PlayOneShot(randomClips2[num]);
			WalkieTalkie.TransmitOneShotAudio(audioToPlayB, randomClips2[num]);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void PlayAudioB1()
	{
		if (!(Time.timeSinceLevelLoad - timeAtStart < 2f))
		{
			audioToPlayB.clip = audioClip;
			audioToPlayB.Play();
			WalkieTalkie.TransmitOneShotAudio(audioToPlayB, audioClip);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void PlayParticle()
	{
		particle.Play();
	}

	public void PlayParticleWithChildren()
	{
		particle.Play(withChildren: true);
	}

	public void StopParticle()
	{
		particle.Stop(withChildren: false, ParticleSystemStopBehavior.StopEmitting);
	}

	public void PlayAudio1Oneshot()
	{
		if (!TooEarlySinceInitializing())
		{
			if (randomizePitch)
			{
				audioToPlay.pitch = Random.Range(0.8f, 1.4f);
			}
			audioToPlay.PlayOneShot(audioClip);
			WalkieTalkie.TransmitOneShotAudio(audioToPlay, audioClip);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void PlayAudio2()
	{
		audioToPlay.clip = audioClip2;
		audioToPlay.Play();
	}

	public void PlayAudioB2()
	{
		if (!(Time.timeSinceLevelLoad - timeAtStart < 2f))
		{
			audioToPlayB.clip = audioClip2;
			audioToPlayB.Play();
		}
	}

	public void PlayAudio2Oneshot()
	{
		if (!TooEarlySinceInitializing())
		{
			if (randomizePitch)
			{
				audioToPlay.pitch = Random.Range(0.6f, 1.4f);
			}
			audioToPlay.PlayOneShot(audioClip2);
			WalkieTalkie.TransmitOneShotAudio(audioToPlay, audioClip2);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void PlayAudio3Oneshot()
	{
		if (!TooEarlySinceInitializing())
		{
			if (randomizePitch)
			{
				audioToPlay.pitch = Random.Range(0.6f, 1.2f);
			}
			audioToPlay.PlayOneShot(audioClip3);
			WalkieTalkie.TransmitOneShotAudio(audioToPlay, audioClip3);
			if (playAudibleNoise)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, noiseIsInsideClosedShip: false, 546);
			}
		}
	}

	public void StopAudio()
	{
		audioToPlay.Stop();
	}

	public void PauseAudio()
	{
		audioToPlay.Pause();
	}

	public void PlayAudio1DefaultClip()
	{
		audioToPlay.Play();
	}

	public void PlayAudio1DefaultClipIfNotPlaying()
	{
		if (!audioToPlay.isPlaying)
		{
			audioToPlay.Play();
		}
	}

	public void OnAnimationEvent()
	{
		onAnimationEventCalled.Invoke();
	}

	private bool TooEarlySinceInitializing()
	{
		return Time.timeSinceLevelLoad - timeAtStart < 2f;
	}

	public void DestroyObject()
	{
		Object.Destroy(destroyObject);
	}
}
