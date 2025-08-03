using System.Collections;
using UnityEngine;

public class ElevatorAnimationEvents : MonoBehaviour
{
	public RoundManager roundManager;

	public AudioSource audioToPlay;

	public AudioSource audioToPlay2;

	private Coroutine fadeCoroutine;

	public void PlayAudio(AudioClip SFXclip)
	{
		if (roundManager.ElevatorLowering || roundManager.ElevatorRunning)
		{
			audioToPlay.clip = SFXclip;
			audioToPlay.Play();
		}
	}

	public void PlayAudio2(AudioClip SFXclip)
	{
		if (roundManager.ElevatorLowering || roundManager.ElevatorRunning)
		{
			audioToPlay2.clip = SFXclip;
			audioToPlay2.Play();
		}
	}

	public void PlayAudioOneshot(AudioClip SFXclip)
	{
		Debug.Log($"elevator running? : {roundManager.ElevatorRunning}");
		if (roundManager.ElevatorLowering || roundManager.ElevatorRunning)
		{
			audioToPlay.PlayOneShot(SFXclip);
		}
	}

	public void PlayAudio2Oneshot(AudioClip SFXclip)
	{
		if (roundManager.ElevatorLowering || roundManager.ElevatorRunning)
		{
			audioToPlay2.PlayOneShot(SFXclip);
		}
	}

	public void StopAudio(AudioSource audio)
	{
		audio.Stop();
	}

	public void FadeAudioOut(AudioSource audio)
	{
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}
		fadeCoroutine = StartCoroutine(fadeAudioIn(fadeIn: false));
	}

	public void FadeAudioIn(AudioSource audio)
	{
		if (fadeCoroutine != null)
		{
			StopCoroutine(fadeCoroutine);
		}
		fadeCoroutine = StartCoroutine(fadeAudioIn(fadeIn: true));
	}

	private IEnumerator fadeAudioIn(bool fadeIn)
	{
		if (fadeIn)
		{
			audioToPlay2.volume = 0f;
			for (int i = 0; i < 20; i++)
			{
				yield return null;
				audioToPlay2.volume += 0.05f;
			}
		}
		else
		{
			for (int j = 0; j < 20; j++)
			{
				audioToPlay2.volume -= 0.05f;
			}
			audioToPlay2.Stop();
		}
	}

	public void LoadNewFloor()
	{
	}

	public void ElevatorFullyRunning()
	{
		roundManager.isSpawningEnemies = false;
		roundManager.DetectElevatorIsRunning();
		if (GameNetworkManager.Instance.localPlayerController != null && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			if (!GameNetworkManager.Instance.localPlayerController.isInElevator)
			{
				Debug.Log($"Killing player obj #{GameNetworkManager.Instance.localPlayerController.playerClientId}, they were not in the ship when it left.");
				GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.zero, spawnBody: false, CauseOfDeath.Abandoned);
				HUDManager.Instance.AddTextToChatOnServer(GameNetworkManager.Instance.localPlayerController.playerUsername + " was left behind.");
			}
			else
			{
				roundManager.playersManager.ForcePlayerIntoShip();
			}
		}
		roundManager.playersManager.ShipHasLeft();
		SetBodiesKinematic();
	}

	private void SetBodiesKinematic()
	{
		DeadBodyInfo[] array = Object.FindObjectsOfType<DeadBodyInfo>();
		for (int i = 0; i < array.Length; i++)
		{
			if (StartOfRound.Instance.shipBounds.bounds.Contains(array[i].bodyParts[5].position))
			{
				array[i].isInShip = true;
			}
			if (array[i].isInShip && array[i].grabBodyObject != null && !array[i].grabBodyObject.isHeld)
			{
				array[i].grabBodyObject.grabbable = false;
				array[i].grabBodyObject.grabbableToEnemies = false;
				array[i].SetBodyPartsKinematic();
			}
		}
	}

	public void ElevatorNoLongerRunning()
	{
		roundManager.ElevatorRunning = false;
	}
}
