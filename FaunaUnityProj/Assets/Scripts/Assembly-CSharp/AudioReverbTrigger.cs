using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class AudioReverbTrigger : MonoBehaviour
{
	public PlayerControllerB playerScript;

	public ReverbPreset reverbPreset;

	public int usePreset = -1;

	[Header("CHANGE AUDIO AMBIANCE")]
	public switchToAudio[] audioChanges;

	[Header("MISC")]
	public bool elevatorTriggerForProps;

	public bool setInElevatorTrigger;

	public bool isShipRoom;

	public bool toggleLocalFog;

	public float fogEnabledAmount = 10f;

	public LocalVolumetricFog localFog;

	public Terrain terrainObj;

	[Header("Weather and effects")]
	public bool setInsideAtmosphere;

	public bool insideLighting;

	public int weatherEffect = -1;

	public bool effectEnabled;

	public bool disableAllWeather;

	public bool enableCurrentLevelWeather;

	private bool spectatedClientTriggered;

	private IEnumerator changeVolume(AudioSource aud, float changeVolumeTo)
	{
		if (localFog != null)
		{
			float fogTarget = fogEnabledAmount;
			if (!toggleLocalFog)
			{
				fogTarget = 200f;
			}
			for (int i = 0; i < 40; i++)
			{
				aud.volume = Mathf.Lerp(aud.volume, changeVolumeTo, (float)i / 40f);
				localFog.parameters.meanFreePath = Mathf.Lerp(localFog.parameters.meanFreePath, fogTarget, (float)i / 40f);
				yield return new WaitForSeconds(0.004f);
			}
		}
		else
		{
			for (int i = 0; i < 40; i++)
			{
				aud.volume = Mathf.Lerp(aud.volume, changeVolumeTo, (float)i / 40f);
				yield return new WaitForSeconds(0.004f);
			}
		}
		playerScript.audioCoroutines.Remove(aud);
		playerScript.audioCoroutines2.Remove(aud);
	}

	public void ChangeAudioReverbForPlayer(PlayerControllerB pScript)
	{
		playerScript = pScript;
		if (GameNetworkManager.Instance.localPlayerController == null || playerScript.currentAudioTrigger == this || !playerScript.isPlayerControlled)
		{
			return;
		}
		if (NetworkManager.Singleton == null)
		{
			Debug.Log("Network manager is null");
		}
		if (usePreset != -1)
		{
			AudioReverbPresets audioReverbPresets = Object.FindObjectOfType<AudioReverbPresets>();
			if (audioReverbPresets != null)
			{
				if (audioReverbPresets.audioPresets.Length <= usePreset)
				{
					Debug.LogError("The audio preset set by " + base.gameObject.name + " is not one allowed by the audioreverbpresets in the scene.");
				}
				else if (audioReverbPresets.audioPresets[usePreset].usePreset != -1)
				{
					Debug.LogError("Audio preset AudioReverbTrigger is set to call another audio preset which would crash!");
				}
				else
				{
					audioReverbPresets.audioPresets[usePreset].ChangeAudioReverbForPlayer(pScript);
				}
				return;
			}
		}
		if (reverbPreset != null)
		{
			playerScript.reverbPreset = reverbPreset;
		}
		if (elevatorTriggerForProps && (!setInElevatorTrigger || StartOfRound.Instance.shipBounds.bounds.Contains(playerScript.transform.position)))
		{
			if (playerScript.currentlyHeldObjectServer != null && playerScript.isHoldingObject)
			{
				playerScript.SetItemInElevator(isShipRoom, setInElevatorTrigger, playerScript.currentlyHeldObjectServer);
			}
			if (playerScript.playersManager.shipDoorsEnabled || setInElevatorTrigger)
			{
				playerScript.isInElevator = setInElevatorTrigger;
				playerScript.isInHangarShipRoom = isShipRoom;
			}
			playerScript.playersManager.SetPlayerSafeInShip();
		}
		if (playerScript != GameNetworkManager.Instance.localPlayerController)
		{
			if (GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != playerScript)
			{
				playerScript.currentAudioTrigger = this;
				return;
			}
			spectatedClientTriggered = true;
		}
		else
		{
			spectatedClientTriggered = false;
		}
		if (disableAllWeather)
		{
			TimeOfDay.Instance.DisableAllWeather();
		}
		else
		{
			if (weatherEffect != -1)
			{
				TimeOfDay.Instance.effects[weatherEffect].effectEnabled = effectEnabled;
			}
			if (enableCurrentLevelWeather && TimeOfDay.Instance.currentLevelWeather != LevelWeatherType.None)
			{
				TimeOfDay.Instance.effects[(int)TimeOfDay.Instance.currentLevelWeather].effectEnabled = true;
			}
		}
		if (setInsideAtmosphere)
		{
			TimeOfDay.Instance.insideLighting = insideLighting;
		}
		PlayerControllerB playerControllerB = playerScript;
		playerScript = GameNetworkManager.Instance.localPlayerController;
		for (int i = 0; i < audioChanges.Length; i++)
		{
			AudioSource audio = audioChanges[i].audio;
			if (audioChanges[i].stopAudio)
			{
				audio.Stop();
				continue;
			}
			if (audioChanges[i].changeToClip != null && audio.clip != audioChanges[i].changeToClip)
			{
				bool flag = false;
				if (audio.isPlaying)
				{
					flag = true;
				}
				audio.clip = audioChanges[i].changeToClip;
				if (flag)
				{
					audio.Play();
				}
			}
			else if (audioChanges[i].changeToClip == null && !audio.isPlaying && !audioChanges[i].changeAudioVolume)
			{
				audio.Play();
			}
			if (audioChanges[i].changeAudioVolume && playerScript.currentAudioTrigger != this)
			{
				if (playerScript.audioCoroutines.TryGetValue(audio, out var value))
				{
					value.StopAudioCoroutine(audio);
					IEnumerator routine = changeVolume(audio, audioChanges[i].audioVolume);
					StartCoroutine(routine);
				}
				else
				{
					IEnumerator enumerator = changeVolume(audio, audioChanges[i].audioVolume);
					StartCoroutine(enumerator);
					playerScript.audioCoroutines.Add(audio, this);
					playerScript.audioCoroutines2.Add(audio, enumerator);
				}
			}
		}
		if (spectatedClientTriggered)
		{
			playerControllerB.currentAudioTrigger = this;
		}
		playerScript.currentAudioTrigger = this;
	}

	private void OnTriggerStay(Collider other)
	{
		if (elevatorTriggerForProps)
		{
			if (setInElevatorTrigger && other.gameObject.CompareTag("Enemy") && base.gameObject.GetComponent<Collider>().bounds.Contains(other.transform.position))
			{
				EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
				if (component != null)
				{
					bool flag = false;
					if (component.mainScript.isInsidePlayerShip != isShipRoom)
					{
						flag = true;
					}
					component.mainScript.isInsidePlayerShip = isShipRoom;
					if (flag)
					{
						StartOfRound.Instance.SetPlayerSafeInShip();
					}
				}
				return;
			}
			if (other.gameObject.tag.StartsWith("PlayerRagdoll"))
			{
				DeadBodyInfo component2 = other.gameObject.GetComponent<DeadBodyInfo>();
				if (component2 != null)
				{
					if (component2.attachedTo != null && component2.attachedLimb != null)
					{
						return;
					}
					component2.parentedToShip = setInElevatorTrigger;
					if (component2.attachedLimb == null || component2.attachedTo == null)
					{
						if (setInElevatorTrigger)
						{
							component2.transform.SetParent(StartOfRound.Instance.elevatorTransform);
						}
						else
						{
							component2.transform.SetParent(null);
						}
					}
				}
			}
		}
		if (other.gameObject.CompareTag("Player") && !(GameNetworkManager.Instance.localPlayerController == null))
		{
			playerScript = other.gameObject.GetComponent<PlayerControllerB>();
			if (!(playerScript == null) && playerScript.isPlayerControlled)
			{
				ChangeAudioReverbForPlayer(playerScript);
			}
		}
	}

	public void StopAudioCoroutine(AudioSource audioKey)
	{
		if (playerScript.audioCoroutines2.TryGetValue(audioKey, out var value))
		{
			StopCoroutine(value);
		}
	}
}
