using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class WalkieTalkie : GrabbableObject
{
	public PlayerControllerB playerListeningTo;

	public AudioSource thisAudio;

	private PlayerControllerB previousPlayerHeldBy;

	public bool isHoldingButton;

	public bool speakingIntoWalkieTalkie;

	public bool clientIsHoldingAndSpeakingIntoThis;

	public bool otherClientIsTransmittingAudios;

	private Coroutine speakIntoWalkieTalkieCoroutine;

	public AudioClip[] stopTransmissionSFX;

	public AudioClip[] startTransmissionSFX;

	public AudioClip switchWalkieTalkiePowerOff;

	public AudioClip switchWalkieTalkiePowerOn;

	public AudioClip talkingOnWalkieTalkieNotHeldSFX;

	public AudioClip playerDieOnWalkieTalkieSFX;

	public static List<WalkieTalkie> allWalkieTalkies = new List<WalkieTalkie>();

	public bool playingGarbledVoice;

	public Material onMaterial;

	public Material offMaterial;

	public Light walkieTalkieLight;

	public AudioSource target;

	[SerializeField]
	private float recordingRange = 6f;

	[SerializeField]
	private float maxVolume = 0.6f;

	private List<AudioSource> audioSourcesToReplay = new List<AudioSource>();

	private Dictionary<AudioSource, AudioSource> audioSourcesReceiving = new Dictionary<AudioSource, AudioSource>();

	public Collider listenCollider;

	private int audioSourcesToReplayLastFrameCount;

	public Collider[] collidersInRange = new Collider[30];

	public List<WalkieTalkie> talkiesSendingToThis = new List<WalkieTalkie>();

	private float cleanUpInterval;

	private float updateInterval;

	public AudioSource wallAudio;

	public AudioClip[] wallAudios;

	private float wallAudioCheckInterval;

	private void OnDisable()
	{
		base.OnDestroy();
		if (allWalkieTalkies.Contains(this))
		{
			allWalkieTalkies.Remove(this);
			if (allWalkieTalkies.Count <= 0)
			{
				allWalkieTalkies.TrimExcess();
			}
		}
	}

	private void OnEnable()
	{
		if (!allWalkieTalkies.Contains(this))
		{
			allWalkieTalkies.Add(this);
		}
	}

	public void SetLocalClientSpeaking(bool speaking)
	{
		if (previousPlayerHeldBy.speakingToWalkieTalkie != speaking)
		{
			previousPlayerHeldBy.speakingToWalkieTalkie = speaking;
			Debug.Log($"Set local client speaking on walkie talkie: {speaking}");
			if (speaking)
			{
				SetPlayerSpeakingOnWalkieTalkieServerRpc((int)previousPlayerHeldBy.playerClientId);
			}
			else
			{
				UnsetPlayerSpeakingOnWalkieTalkieServerRpc((int)previousPlayerHeldBy.playerClientId);
			}
		}
	}

	[ServerRpc]
	public void SetPlayerSpeakingOnWalkieTalkieServerRpc(int playerId)
{		{
			SetPlayerSpeakingOnWalkieTalkieClientRpc(playerId);
		}
}
	[ClientRpc]
	public void SetPlayerSpeakingOnWalkieTalkieClientRpc(int playerId)
			{
				StartOfRound.Instance.allPlayerScripts[playerId].speakingToWalkieTalkie = true;
				clientIsHoldingAndSpeakingIntoThis = true;
				SendWalkieTalkieStartTransmissionSFX(playerId);
				StartOfRound.Instance.UpdatePlayerVoiceEffects();
			}

	[ServerRpc]
	public void UnsetPlayerSpeakingOnWalkieTalkieServerRpc(int playerId)
{		{
			UnsetPlayerSpeakingOnWalkieTalkieClientRpc(playerId);
		}
}
	[ClientRpc]
	public void UnsetPlayerSpeakingOnWalkieTalkieClientRpc(int playerId)
			{
				StartOfRound.Instance.allPlayerScripts[playerId].speakingToWalkieTalkie = false;
				clientIsHoldingAndSpeakingIntoThis = false;
				SendWalkieTalkieEndTransmissionSFX(playerId);
				updateInterval = 0.2f;
				StartOfRound.Instance.UpdatePlayerVoiceEffects();
			}

	private void SendWalkieTalkieEndTransmissionSFX(int playerId)
	{
		for (int i = 0; i < allWalkieTalkies.Count; i++)
		{
			if (!(StartOfRound.Instance.allPlayerScripts[playerId] == allWalkieTalkies[i].playerHeldBy) && !PlayerIsHoldingAnotherWalkieTalkie(allWalkieTalkies[i]) && allWalkieTalkies[i].isBeingUsed)
			{
				RoundManager.PlayRandomClip(allWalkieTalkies[i].thisAudio, allWalkieTalkies[i].stopTransmissionSFX);
			}
		}
	}

	private void SendWalkieTalkieStartTransmissionSFX(int playerId)
	{
		Debug.Log("Walkie talkie A");
		UnityEngine.Random.Range(0f, talkingOnWalkieTalkieNotHeldSFX.length - 0.1f);
		for (int i = 0; i < allWalkieTalkies.Count; i++)
		{
			Debug.Log($"Walkie talkie #{i} {allWalkieTalkies[i].gameObject.name} B");
			Debug.Log($"is walkie being used: {allWalkieTalkies[i].isBeingUsed}");
			if (!PlayerIsHoldingAnotherWalkieTalkie(allWalkieTalkies[i]) && allWalkieTalkies[i].isBeingUsed)
			{
				RoundManager.PlayRandomClip(allWalkieTalkies[i].thisAudio, allWalkieTalkies[i].startTransmissionSFX);
				Debug.Log($"Walkie talkie #{i}  {allWalkieTalkies[i].gameObject.name} C");
			}
		}
	}

	private void BroadcastSFXFromWalkieTalkie(AudioClip sfx, int fromPlayerId)
	{
		for (int i = 0; i < allWalkieTalkies.Count; i++)
		{
			if (!(StartOfRound.Instance.allPlayerScripts[fromPlayerId] == allWalkieTalkies[i].playerHeldBy))
			{
				if (PlayerIsHoldingAnotherWalkieTalkie(allWalkieTalkies[i]))
				{
					break;
				}
				allWalkieTalkies[i].thisAudio.PlayOneShot(sfx);
			}
		}
	}

	private bool PlayerIsHoldingAnotherWalkieTalkie(WalkieTalkie walkieTalkie)
	{
		if (walkieTalkie.playerHeldBy == null)
		{
			Debug.Log("False A");
			return false;
		}
		if (walkieTalkie.playerHeldBy.currentlyHeldObjectServer == null)
		{
			Debug.Log("False B");
			return false;
		}
		if (walkieTalkie.playerHeldBy.currentlyHeldObjectServer.GetComponent<WalkieTalkie>() == null)
		{
			Debug.Log("False C");
			return false;
		}
		Debug.Log($"{walkieTalkie.isPocketed}");
		if (walkieTalkie.playerHeldBy != null && walkieTalkie.playerHeldBy.currentlyHeldObjectServer != null && walkieTalkie.playerHeldBy.currentlyHeldObjectServer.GetComponent<WalkieTalkie>() != null)
		{
			return walkieTalkie.isPocketed;
		}
		return false;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		isHoldingButton = buttonDown;
		if (isBeingUsed && !speakingIntoWalkieTalkie && buttonDown)
		{
			previousPlayerHeldBy = playerHeldBy;
			if (speakIntoWalkieTalkieCoroutine != null)
			{
				StopCoroutine(speakIntoWalkieTalkieCoroutine);
			}
			speakIntoWalkieTalkieCoroutine = StartCoroutine(speakingIntoWalkieTalkieMode());
		}
	}

	private IEnumerator speakingIntoWalkieTalkieMode()
	{
		PlayerHoldingWalkieTalkieButton(speaking: true);
		SetLocalClientSpeaking(speaking: true);
		yield return new WaitForSeconds(0.2f);
		yield return new WaitUntil(() => !isHoldingButton || !isHeld || !isBeingUsed);
		SetLocalClientSpeaking(speaking: false);
		PlayerHoldingWalkieTalkieButton(speaking: false);
	}

	private void PlayerHoldingWalkieTalkieButton(bool speaking)
	{
		speakingIntoWalkieTalkie = speaking;
		previousPlayerHeldBy.activatingItem = speaking;
		previousPlayerHeldBy.playerBodyAnimator.SetBool("walkieTalkie", speaking);
	}

	public void EnableWalkieTalkieListening(bool enable)
	{
		if (playerHeldBy != null)
		{
			playerHeldBy.holdingWalkieTalkie = enable;
		}
		if (IsPlayerSpectatedOrLocal())
		{
			thisAudio.Stop();
			StartOfRound.Instance.UpdatePlayerVoiceEffects();
		}
	}

	public override void UseUpBatteries()
	{
		base.UseUpBatteries();
		SwitchWalkieTalkieOn(on: false);
	}

	public override void PocketItem()
	{
		base.PocketItem();
		playerHeldBy.equippedUsableItemQE = false;
		walkieTalkieLight.enabled = false;
	}

	public override void ItemInteractLeftRight(bool right)
	{
		base.ItemInteractLeftRight(right);
		if (!right)
		{
			SwitchWalkieTalkieOn(!isBeingUsed);
		}
	}

	public void SwitchWalkieTalkieOn(bool on)
	{
		isBeingUsed = on;
		EnableWalkieTalkieListening(on);
		if (on)
		{
			mainObjectRenderer.sharedMaterial = onMaterial;
			walkieTalkieLight.enabled = true;
			thisAudio.PlayOneShot(switchWalkieTalkiePowerOn);
		}
		else
		{
			mainObjectRenderer.sharedMaterial = offMaterial;
			walkieTalkieLight.enabled = false;
			thisAudio.PlayOneShot(switchWalkieTalkiePowerOff);
			wallAudio.Stop();
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		if (isBeingUsed)
		{
			walkieTalkieLight.enabled = true;
		}
		playerHeldBy.equippedUsableItemQE = true;
		if (isBeingUsed)
		{
			EnableWalkieTalkieListening(enable: true);
		}
	}

	public override void DiscardItem()
	{
		if (playerHeldBy.isPlayerDead && clientIsHoldingAndSpeakingIntoThis)
		{
			BroadcastSFXFromWalkieTalkie(playerDieOnWalkieTalkieSFX, (int)playerHeldBy.playerClientId);
		}
		EnableWalkieTalkieListening(enable: false);
		playerHeldBy.equippedUsableItemQE = false;
		base.DiscardItem();
	}

	private bool IsPlayerSpectatedOrLocal()
	{
		if (!base.IsOwner)
		{
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				return playerHeldBy == GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
			}
			return false;
		}
		return true;
	}

	public override void Start()
	{
		base.Start();
		GetAllAudioSourcesToReplay();
		SetupAudiosourceClip();
	}

	public override void Update()
	{
		base.Update();
		if (isBeingUsed && base.IsOwner)
		{
			SetWallAudioVolume();
		}
		if (cleanUpInterval >= 0f)
		{
			cleanUpInterval -= Time.deltaTime;
		}
		else
		{
			cleanUpInterval = 15f;
			if (audioSourcesReceiving.Count > 10)
			{
				foreach (KeyValuePair<AudioSource, AudioSource> item in audioSourcesReceiving)
				{
					if (item.Key == null)
					{
						audioSourcesReceiving.Remove(item.Key);
					}
				}
			}
		}
		if (updateInterval >= 0f)
		{
			updateInterval -= Time.deltaTime;
			return;
		}
		updateInterval = 0.3f;
		GetAllAudioSourcesToReplay();
		TimeAllAudioSources();
	}

	private void SetWallAudioVolume()
	{
		if (StartOfRound.Instance.currentLevel.levelID == 3)
		{
			if (Time.realtimeSinceStartup - wallAudioCheckInterval > 40f)
			{
				wallAudioCheckInterval = Time.realtimeSinceStartup;
				if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, new Vector3(-28.8f, -7.72f, -10.56f)) < 18f && UnityEngine.Random.Range(0, 100) < 3)
				{
					int num = UnityEngine.Random.Range(0, wallAudios.Length);
					wallAudio.PlayOneShot(wallAudios[num]);
					PlayWallAudioServerRpc(num);
				}
			}
		}
		else
		{
			wallAudio.volume = 0f;
		}
	}

	[ServerRpc]
	public void PlayWallAudioServerRpc(int clipIndex)
{		{
			PlayWallAudioClientRpc(clipIndex);
		}
}
	[ClientRpc]
	public void PlayWallAudioClientRpc(int clipIndex)
{if(isBeingUsed && !base.IsOwner)			{
				wallAudio.PlayOneShot(wallAudios[clipIndex]);
			}
}
	private void TimeAllAudioSources()
	{
		for (int i = 0; i < allWalkieTalkies.Count; i++)
		{
			if (allWalkieTalkies[i] == this)
			{
				continue;
			}
			AudioSource value;
			if (allWalkieTalkies[i].playerHeldBy != null && allWalkieTalkies[i].clientIsHoldingAndSpeakingIntoThis && allWalkieTalkies[i].isBeingUsed && isBeingUsed)
			{
				if (!talkiesSendingToThis.Contains(allWalkieTalkies[i]))
				{
					talkiesSendingToThis.Add(allWalkieTalkies[i]);
				}
				for (int num = allWalkieTalkies[i].audioSourcesToReplay.Count - 1; num >= 0; num--)
				{
					AudioSource audioSource = allWalkieTalkies[i].audioSourcesToReplay[num];
					if (!(audioSource == null))
					{
						if (audioSourcesReceiving.TryAdd(audioSource, null))
						{
							audioSourcesReceiving[audioSource] = target.gameObject.AddComponent<AudioSource>();
							audioSourcesReceiving[audioSource].clip = audioSource.clip;
							try
							{
								if (audioSource.time >= audioSource.clip.length)
								{
									if (audioSource.time - 0.05f < audioSource.clip.length)
									{
										audioSourcesReceiving[audioSource].time = Mathf.Clamp(audioSource.time - 0.05f, 0f, 1000f);
									}
									else
									{
										audioSourcesReceiving[audioSource].time = audioSource.time / 5f;
									}
								}
								else
								{
									audioSourcesReceiving[audioSource].time = audioSource.time;
								}
								audioSourcesReceiving[audioSource].spatialBlend = 1f;
								audioSourcesReceiving[audioSource].Play();
							}
							catch (Exception ex)
							{
								Debug.LogError($"Error while playing audio clip in walkie talkie. Clip name: {audioSource.clip.name} object: {audioSource.gameObject.name}; time: {audioSource.time}; {ex}");
							}
						}
						else
						{
							float num2 = Vector3.Distance(audioSource.transform.position, allWalkieTalkies[i].transform.position);
							if (num2 > recordingRange + 7f)
							{
								audioSourcesReceiving.Remove(audioSource, out value);
								UnityEngine.Object.Destroy(value);
								allWalkieTalkies[i].audioSourcesToReplay.RemoveAt(num);
							}
							else
							{
								audioSourcesReceiving[audioSource].volume = Mathf.Lerp(maxVolume, 0f, num2 / (recordingRange + 3f));
								if ((audioSource.isPlaying && !audioSourcesReceiving[audioSource].isPlaying) || audioSource.clip != audioSourcesReceiving[audioSource].clip)
								{
									audioSourcesReceiving[audioSource].clip = audioSource.clip;
									audioSourcesReceiving[audioSource].Play();
								}
								else if (!audioSource.isPlaying || !audioSource.gameObject.activeInHierarchy)
								{
									audioSourcesReceiving[audioSource].Stop();
								}
								audioSourcesReceiving[audioSource].time = audioSource.time;
							}
						}
					}
				}
			}
			else
			{
				if (!talkiesSendingToThis.Contains(allWalkieTalkies[i]))
				{
					continue;
				}
				talkiesSendingToThis.Remove(allWalkieTalkies[i]);
				foreach (AudioSource item in allWalkieTalkies[i].audioSourcesToReplay)
				{
					for (int j = 0; j < allWalkieTalkies.Count; j++)
					{
						if (allWalkieTalkies[j].audioSourcesReceiving.ContainsKey(item))
						{
							allWalkieTalkies[j].audioSourcesReceiving.Remove(item, out value);
							UnityEngine.Object.Destroy(value);
						}
					}
				}
				allWalkieTalkies[i].audioSourcesToReplay.Clear();
			}
		}
	}

	public static void TransmitOneShotAudio(AudioSource audioSource, AudioClip clip, float vol = 1f)
	{
		if (clip == null || audioSource == null)
		{
			return;
		}
		for (int i = 0; i < allWalkieTalkies.Count; i++)
		{
			if (allWalkieTalkies[i].playerHeldBy == null || !allWalkieTalkies[i].clientIsHoldingAndSpeakingIntoThis || !allWalkieTalkies[i].isBeingUsed)
			{
				continue;
			}
			float num = Vector3.Distance(allWalkieTalkies[i].transform.position, audioSource.transform.position);
			if (!(num < allWalkieTalkies[i].recordingRange))
			{
				continue;
			}
			for (int j = 0; j < allWalkieTalkies.Count; j++)
			{
				if (j != i && allWalkieTalkies[j].isBeingUsed)
				{
					float num2 = Mathf.Lerp(allWalkieTalkies[i].maxVolume, 0f, num / (allWalkieTalkies[i].recordingRange + 3f));
					allWalkieTalkies[j].target.PlayOneShot(clip, num2 * vol);
				}
			}
		}
	}

	private void SetupAudiosourceClip()
	{
		target.Stop();
	}

	private void GetAllAudioSourcesToReplay()
	{
		if (playerHeldBy == null || !playerHeldBy.speakingToWalkieTalkie || !isBeingUsed)
		{
			return;
		}
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, recordingRange, collidersInRange, 11010632, QueryTriggerInteraction.Collide);
		for (int i = 0; i < num; i++)
		{
			if (!collidersInRange[i].gameObject.GetComponent<WalkieTalkie>())
			{
				AudioSource component = collidersInRange[i].GetComponent<AudioSource>();
				if (component != null && component.isPlaying && component.clip != null && component.time > 0f && !audioSourcesToReplay.Contains(component) && component.gameObject.activeInHierarchy)
				{
					audioSourcesToReplay.Add(component);
				}
			}
		}
	}
}
