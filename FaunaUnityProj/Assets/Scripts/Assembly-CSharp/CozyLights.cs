using UnityEngine;

public class CozyLights : MonoBehaviour
{
	private bool cozyLightsOn;

	public Animator cozyLightsAnimator;

	public AudioSource turnOnAudio;

	private float soundInterval;

	private void Update()
	{
		if (!(StartOfRound.Instance == null))
		{
			if (StartOfRound.Instance.shipRoomLights.areLightsOn == cozyLightsOn)
			{
				cozyLightsOn = !cozyLightsOn;
				cozyLightsAnimator.SetBool("on", cozyLightsOn);
			}
			if (turnOnAudio != null)
			{
				SetAudio();
			}
		}
	}

	public void SetAudio()
	{
		if (cozyLightsOn)
		{
			turnOnAudio.pitch = 1f;
			turnOnAudio.volume = 0.3f;
			if (!turnOnAudio.isPlaying)
			{
				turnOnAudio.Play();
			}
			if (soundInterval <= 0f)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 20f, 0.6f, 0, StartOfRound.Instance.hangarDoorsClosed, 105152);
				soundInterval = 2f;
			}
			else
			{
				soundInterval -= Time.deltaTime;
			}
		}
		else if (turnOnAudio.isPlaying)
		{
			turnOnAudio.pitch = Mathf.Max(0.3f, turnOnAudio.pitch - Time.deltaTime);
			turnOnAudio.volume = Mathf.Max(0f, turnOnAudio.pitch - Time.deltaTime * 3f);
			if (turnOnAudio.pitch < 0.35f)
			{
				turnOnAudio.Stop();
			}
		}
	}
}
