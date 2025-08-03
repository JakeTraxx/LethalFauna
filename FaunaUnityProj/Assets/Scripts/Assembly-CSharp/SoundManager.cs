using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : NetworkBehaviour
{
	private System.Random SoundsRandom;

	public float soundFrequencyServer = 10f;

	public float soundRarityServer = 0.25f;

	public float soundTimerServer;

	private int serverSoundsPlayedInARow;

	public float soundFrequency = 8f;

	public float soundRarity = 0.6f;

	private float soundTimer;

	private int localSoundsPlayedInARow;

	public AudioSource ambienceAudio;

	public AudioSource ambienceAudioNonDiagetic;

	public LevelAmbienceLibrary currentLevelAmbience;

	[Header("Outside Music")]
	public AudioSource musicSource;

	public AudioClip[] DaytimeMusic;

	public AudioClip[] EveningMusic;

	private float timeSincePlayingLastMusic;

	public bool playingOutsideMusic;

	[Space(5f)]
	private bool isAudioPlaying;

	private PlayerControllerB localPlayer;

	private bool isInsanityMusicPlaying;

	private List<int> audioClipProbabilities = new List<int>();

	private int lastSoundTypePlayed = -1;

	private int lastServerSoundTypePlayed = -1;

	private bool playingInsanitySoundClip;

	private bool playingInsanitySoundClipOnServer;

	private float localPlayerAmbientMusicTimer;

	[Header("Audio Mixer")]
	public AudioMixerSnapshot[] mixerSnapshots;

	public AudioMixer diageticMixer;

	public AudioMixerGroup[] playerVoiceMixers;

	[Space(3f)]
	public float[] playerVoicePitchTargets;

	public float[] playerVoicePitches;

	public float[] playerVoicePitchLerpSpeed;

	[Space(3f)]
	public float[] playerVoiceVolumes;

	public int currentMixerSnapshotID;

	private bool overridingCurrentAudioMixer;

	[Space(3f)]
	public bool echoEnabled;

	[Header("Background music")]
	public AudioSource highAction1;

	private bool highAction1audible;

	public AudioSource highAction2;

	private bool highAction2audible;

	public AudioSource lowAction;

	private bool lowActionAudible;

	public AudioSource heartbeatSFX;

	public float currentHeartbeatInterval;

	public float heartbeatTimer;

	public AudioClip[] heartbeatClips;

	private int currentHeartbeatClip;

	private bool playingHeartbeat;

	public float earsRingingTimer;

	public float timeSinceEarsStartedRinging;

	private bool earsRinging;

	public AudioSource ringingEarsAudio;

	public AudioSource misc2DAudio;

	public AudioSource tempAudio1;

	public AudioSource tempAudio2;

	public AudioClip[] syncedAudioClips;

	private System.Random audioRandom;

	private float currentDiageticVolume;

	public float DeafenAmount;

	public static SoundManager Instance { get; private set; }

	public void ResetRandomSeed()
	{
		audioRandom = new System.Random(StartOfRound.Instance.randomMapSeed + 113);
	}

	public void OnDisable()
	{
		diageticMixer.SetFloat("EchoWetness", 0f);
		echoEnabled = false;
	}

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(Instance.gameObject);
		}
	}

	private void Start()
	{
		InitializeRandom();
		SetDiageticMixerSnapshot();
		playerVoicePitchLerpSpeed = new float[4] { 3f, 3f, 3f, 3f };
		playerVoicePitchTargets = new float[4] { 1f, 1f, 1f, 1f };
		playerVoicePitches = new float[4] { 1f, 1f, 1f, 1f };
		playerVoiceVolumes = new float[4] { 0.5f, 0.5f, 0.5f, 0.5f };
		AudioListener.volume = 0f;
		StartCoroutine(fadeVolumeBackToNormalDelayed());
		if (audioRandom == null)
		{
			ResetRandomSeed();
		}
	}

	private IEnumerator fadeVolumeBackToNormalDelayed()
	{
		yield return new WaitForSeconds(0.5f);
		float targetVolume = IngamePlayerSettings.Instance.settings.masterVolume;
		for (int i = 0; i < 40; i++)
		{
			AudioListener.volume += 0.025f;
			if (AudioListener.volume >= targetVolume)
			{
				break;
			}
			yield return new WaitForSeconds(0.016f);
		}
		AudioListener.volume = targetVolume;
	}

	public void InitializeRandom()
	{
		SoundsRandom = new System.Random(StartOfRound.Instance.randomMapSeed - 33);
		ResetValues();
	}

	public void ResetValues()
	{
		SetDiageticMixerSnapshot();
		lastSoundTypePlayed = -1;
		lastServerSoundTypePlayed = -1;
		localSoundsPlayedInARow = 0;
		soundFrequency = 0.8f;
		soundRarity = 0.6f;
		soundTimer = 0f;
		currentDiageticVolume = 0f;
		diageticMixer.SetFloat("DiageticVolume", Mathf.Min(currentDiageticVolume, 0f));
		isInsanityMusicPlaying = false;
	}

	public void SetPlayerPitch(float pitch, int playerObjNum)
	{
		diageticMixer.SetFloat($"PlayerPitch{playerObjNum}", pitch);
	}

	public void SetPlayerVoiceFilters()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
		}
		for (int j = 0; j < StartOfRound.Instance.allPlayerScripts.Length; j++)
		{
			if (!StartOfRound.Instance.allPlayerScripts[j].isPlayerControlled && !StartOfRound.Instance.allPlayerScripts[j].isPlayerDead)
			{
				playerVoicePitches[j] = 1f;
				playerVoiceVolumes[j] = 1f;
				continue;
			}
			diageticMixer.SetFloat($"PlayerVolume{j}", 16f * playerVoiceVolumes[j]);
			if (Mathf.Abs(playerVoicePitches[j] - playerVoicePitchTargets[j]) > 0.025f)
			{
				playerVoicePitches[j] = Mathf.Lerp(playerVoicePitches[j], playerVoicePitchTargets[j], 3f * Time.deltaTime);
				diageticMixer.SetFloat($"PlayerPitch{j}", playerVoicePitches[j]);
			}
			else if (playerVoicePitches[j] != playerVoicePitchTargets[j])
			{
				playerVoicePitches[j] = playerVoicePitchTargets[j];
				diageticMixer.SetFloat($"PlayerPitch{j}", playerVoicePitches[j]);
			}
		}
	}

	private void Update()
	{
		localPlayer = GameNetworkManager.Instance.localPlayerController;
		if (localPlayer == null || NetworkManager.Singleton == null)
		{
			Debug.Log($"soumdmanager: {localPlayer == null}; {NetworkManager.Singleton == null}");
			return;
		}
		timeSincePlayingLastMusic += 1f;
		SetPlayerVoiceFilters();
		SetAudioFilters();
		SetOutsideMusicValues();
		PlayNonDiageticSound();
		SetFearAudio();
		SetEarsRinging();
		if (!StartOfRound.Instance.inShipPhase && !ambienceAudio.isPlaying)
		{
			ServerSoundTimer();
			LocalPlayerSoundTimer();
		}
	}

	public void LateUpdate()
	{
		if (!(GameNetworkManager.Instance.localPlayerController == null) && !GameNetworkManager.Instance.isDisconnecting)
		{
			if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
			{
				DeafenAmount = 0f;
				currentDiageticVolume = 0f;
			}
			currentDiageticVolume = Mathf.Lerp(currentDiageticVolume, DeafenAmount, 5f * Time.deltaTime);
			diageticMixer.SetFloat("DiageticVolume", Mathf.Min(currentDiageticVolume, 0f));
			DeafenAmount = 0f;
		}
	}

	public void SetDiageticMasterVolume(float targetDecibels)
	{
		if (DeafenAmount > targetDecibels)
		{
			DeafenAmount = targetDecibels;
		}
	}

	public void SetEchoFilter(bool setEcho)
	{
		if (echoEnabled != setEcho)
		{
			echoEnabled = setEcho;
			if (echoEnabled)
			{
				diageticMixer.SetFloat("EchoWetness", 0.08f);
			}
			else
			{
				diageticMixer.SetFloat("EchoWetness", 0f);
			}
		}
	}

	private void SetAudioFilters()
	{
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && currentMixerSnapshotID != 0)
		{
			SetDiageticMasterVolume(0f);
			SetDiageticMixerSnapshot(0, 0.2f);
		}
		else if (StartOfRound.Instance.drunknessSideEffect.Evaluate(localPlayer.drunkness) > 0.6f && !overridingCurrentAudioMixer)
		{
			overridingCurrentAudioMixer = true;
			mixerSnapshots[4].TransitionTo(6f);
		}
		else if (StartOfRound.Instance.drunknessSideEffect.Evaluate(localPlayer.drunkness) < 0.4f && overridingCurrentAudioMixer)
		{
			overridingCurrentAudioMixer = false;
			ResumeCurrentMixerSnapshot(8f);
		}
	}

	public void PlayRandomOutsideMusic(bool eveningMusic = false)
	{
		if (timeSincePlayingLastMusic < 200f)
		{
			return;
		}
		int num = UnityEngine.Random.Range(0, DaytimeMusic.Length);
		if (eveningMusic)
		{
			if (EveningMusic.Length == 0)
			{
				return;
			}
			musicSource.clip = EveningMusic[num];
		}
		else
		{
			musicSource.clip = DaytimeMusic[num];
		}
		musicSource.Play();
		playingOutsideMusic = true;
		timeSincePlayingLastMusic = 0f;
	}

	private void SetOutsideMusicValues()
	{
		if (playingOutsideMusic)
		{
			musicSource.volume = Mathf.Lerp(musicSource.volume, 0.85f, 2f * Time.deltaTime);
			if (GameNetworkManager.Instance.localPlayerController.isInsideFactory || StartOfRound.Instance.fearLevel > 0.075f || (GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom && StartOfRound.Instance.mapScreen.targetedPlayer != null && StartOfRound.Instance.mapScreen.targetedPlayer.isInsideFactory))
			{
				playingOutsideMusic = false;
			}
		}
		else
		{
			musicSource.volume = Mathf.Lerp(musicSource.volume, 0f, 2f * Time.deltaTime);
			if (musicSource.volume <= 0.005f)
			{
				musicSource.Stop();
			}
		}
	}

	private void SetEarsRinging()
	{
		if (earsRingingTimer > 0f && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			timeSinceEarsStartedRinging = 0f;
			if (!earsRinging)
			{
				earsRinging = true;
				SetDiageticMixerSnapshot(2);
				ringingEarsAudio.Play();
			}
			ringingEarsAudio.volume = Mathf.Lerp(ringingEarsAudio.volume, earsRingingTimer, Time.deltaTime * 2f);
			earsRingingTimer -= Time.deltaTime * 0.1f;
		}
		else
		{
			timeSinceEarsStartedRinging += Time.deltaTime;
			if (earsRinging)
			{
				earsRinging = false;
				SetDiageticMixerSnapshot();
				ringingEarsAudio.Stop();
			}
		}
	}

	private void SetFearAudio()
	{
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			highAction1.volume = 0f;
			highAction1.Stop();
			highAction2.volume = 0f;
			highAction2.Stop();
			heartbeatSFX.volume = 0f;
			heartbeatSFX.Stop();
			lowAction.volume = 0f;
			lowAction.Stop();
			return;
		}
		if (!highAction2.isPlaying)
		{
			highAction1.Play();
			highAction2.Play();
			heartbeatSFX.Play();
			lowAction.Play();
		}
		float fearLevel = StartOfRound.Instance.fearLevel;
		if (fearLevel > 0.4f)
		{
			highAction1.volume = Mathf.Lerp(highAction1.volume, fearLevel - 0.2f, 0.75f * Time.deltaTime);
			highAction1audible = true;
		}
		else
		{
			highAction1.volume = Mathf.Lerp(highAction1.volume, 0f, Time.deltaTime);
			if (highAction1.volume < 0.01f && highAction1audible)
			{
				highAction1audible = false;
				highAction1.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
			}
		}
		if (fearLevel > 0.7f)
		{
			highAction2.volume = Mathf.Lerp(highAction2.volume, fearLevel, 2f * Time.deltaTime);
			highAction2audible = true;
		}
		else
		{
			highAction2.volume = Mathf.Lerp(highAction2.volume, 0f, 0.75f * Time.deltaTime);
			if (highAction2.volume < 0.01f && highAction2audible)
			{
				highAction2audible = false;
				highAction2.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
			}
		}
		if (fearLevel > 0.1f && fearLevel < 0.67f)
		{
			lowAction.volume = Mathf.Lerp(lowAction.volume, fearLevel + 0.2f, 2f * Time.deltaTime);
			lowActionAudible = true;
		}
		else
		{
			lowAction.volume = Mathf.Lerp(lowAction.volume, 0f, 2f * Time.deltaTime);
			if (lowAction.volume < 0.01f && lowActionAudible)
			{
				lowActionAudible = false;
				lowAction.pitch = UnityEngine.Random.Range(0.87f, 1.1f);
			}
		}
		float num = ((!(GameNetworkManager.Instance.localPlayerController.drunkness > 0.3f)) ? Mathf.Abs(fearLevel - 1.4f) : Mathf.Abs(StartOfRound.Instance.drunknessSideEffect.Evaluate(GameNetworkManager.Instance.localPlayerController.drunkness) - 1.6f));
		currentHeartbeatInterval = Mathf.MoveTowards(currentHeartbeatInterval, num, 0.3f * Time.deltaTime);
		if ((double)currentHeartbeatInterval > 1.3)
		{
			playingHeartbeat = false;
		}
		if (!(fearLevel > 0.5f) && !(GameNetworkManager.Instance.localPlayerController.drunkness > 0.3f) && !playingHeartbeat)
		{
			return;
		}
		playingHeartbeat = true;
		heartbeatSFX.volume = Mathf.Clamp(Mathf.Abs(num - 1f) + 0.55f, 0f, 1f);
		heartbeatTimer += Time.deltaTime;
		if (heartbeatTimer >= currentHeartbeatInterval)
		{
			heartbeatTimer = 0f;
			int num2 = UnityEngine.Random.Range(0, heartbeatClips.Length);
			if (num2 == currentHeartbeatClip)
			{
				num2 = (num2 + 1) % heartbeatClips.Length;
			}
			currentHeartbeatClip = num2;
			heartbeatSFX.clip = heartbeatClips[num2];
			heartbeatSFX.Play();
		}
	}

	private void PlayNonDiageticSound()
	{
		if (currentLevelAmbience == null)
		{
			return;
		}
		if (localPlayer.isPlayerDead || !localPlayer.isInsideFactory || localPlayer.insanityLevel < localPlayer.maxInsanityLevel * 0.2f)
		{
			ambienceAudioNonDiagetic.volume = Mathf.Lerp(ambienceAudioNonDiagetic.volume, 0f, Time.deltaTime);
			isInsanityMusicPlaying = false;
			return;
		}
		ambienceAudioNonDiagetic.volume = Mathf.Lerp(ambienceAudioNonDiagetic.volume, localPlayer.insanityLevel / localPlayer.maxInsanityLevel, Time.deltaTime);
		if (!isInsanityMusicPlaying)
		{
			if (localPlayerAmbientMusicTimer < 13f)
			{
				localPlayerAmbientMusicTimer += Time.deltaTime;
			}
			else
			{
				localPlayerAmbientMusicTimer = 0f;
				if ((float)UnityEngine.Random.Range(0, 45) < localPlayer.insanityLevel)
				{
					isInsanityMusicPlaying = true;
					ambienceAudioNonDiagetic.clip = currentLevelAmbience.insanityMusicAudios[UnityEngine.Random.Range(0, currentLevelAmbience.insanityMusicAudios.Length)];
					ambienceAudioNonDiagetic.Play();
				}
			}
		}
		if (!ambienceAudioNonDiagetic.isPlaying)
		{
			isInsanityMusicPlaying = false;
		}
	}

	private void ServerSoundTimer()
	{
		if (!base.IsServer)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && StartOfRound.Instance.allPlayerScripts[i].isPlayerAlone)
			{
				num++;
			}
		}
		if (num == GameNetworkManager.Instance.connectedPlayers)
		{
			return;
		}
		soundTimerServer += Time.deltaTime;
		if (soundTimerServer > soundFrequencyServer)
		{
			soundTimerServer = 0f;
			if (UnityEngine.Random.Range(0f, 1f) < soundRarityServer)
			{
				localSoundsPlayedInARow++;
				PlayAmbientSound(syncedForAllPlayers: true, playingInsanitySoundClipOnServer);
			}
			else
			{
				serverSoundsPlayedInARow = 0;
			}
			SetServerSoundRandomizerVariables();
		}
	}

	private void LocalPlayerSoundTimer()
	{
		if (localPlayer.isPlayerDead || !localPlayer.isPlayerAlone)
		{
			return;
		}
		soundTimer += Time.deltaTime;
		if (soundTimer > soundFrequency)
		{
			soundTimer = 0f;
			if (UnityEngine.Random.Range(0f, 1f) < soundRarity)
			{
				localSoundsPlayedInARow++;
				PlayAmbientSound(syncedForAllPlayers: false, playingInsanitySoundClip);
			}
			else
			{
				localSoundsPlayedInARow = 0;
			}
			SetLocalSoundRandomizerVariables();
		}
	}

	public void SetServerSoundRandomizerVariables()
	{
		if (TimeOfDay.Instance.normalizedTimeOfDay > 0.85f)
		{
			playingInsanitySoundClipOnServer = UnityEngine.Random.Range(0, 400) < 20;
		}
		else if (TimeOfDay.Instance.normalizedTimeOfDay > 0.6f)
		{
			playingInsanitySoundClipOnServer = UnityEngine.Random.Range(0, 400) < 12;
		}
		else
		{
			playingInsanitySoundClipOnServer = UnityEngine.Random.Range(0, 400) < 4;
		}
		if (UnityEngine.Random.Range(0, 100) < 30)
		{
			soundFrequencyServer = UnityEngine.Random.Range(0.5f, 15f);
		}
		else
		{
			soundFrequencyServer = UnityEngine.Random.Range(10f + (float)serverSoundsPlayedInARow * 3f, 15f);
		}
		if (serverSoundsPlayedInARow > 0)
		{
			soundRarityServer /= 3f;
		}
		else
		{
			soundRarityServer *= 1.2f;
		}
	}

	public void SetLocalSoundRandomizerVariables()
	{
		playingInsanitySoundClip = false;
		bool flag = localPlayer.insanityLevel > localPlayer.maxInsanityLevel * 0.75f;
		if (flag && (float)UnityEngine.Random.Range(0, 100) > 50f && localSoundsPlayedInARow < 2)
		{
			playingInsanitySoundClip = true;
		}
		soundFrequency = Mathf.Clamp(10f / (localPlayer.insanityLevel * 0.04f), 2f, 13f);
		if (!flag)
		{
			soundFrequency += (float)localSoundsPlayedInARow * 2f;
		}
		soundFrequency += UnityEngine.Random.Range(-3f, 3f);
		if (localSoundsPlayedInARow > 0)
		{
			if (flag && StartOfRound.Instance.connectedPlayersAmount + 1 > 1)
			{
				soundRarity /= 3f;
			}
			else
			{
				soundRarity /= 5f;
			}
		}
		else
		{
			soundRarity *= 1.2f;
		}
		soundRarity = Mathf.Clamp(soundRarity, 0.02f, 0.98f);
	}

	public void PlayAmbientSound(bool syncedForAllPlayers = false, bool playInsanitySounds = false)
	{
		float num = 1f;
		if (currentLevelAmbience == null)
		{
			return;
		}
		RandomAudioClip[] array = null;
		int num2;
		int num3;
		if (localPlayer.isInsideFactory)
		{
			num2 = 0;
			if (playInsanitySounds)
			{
				if (currentLevelAmbience.insideAmbienceInsanity.Length == 0)
				{
					return;
				}
				array = currentLevelAmbience.insideAmbienceInsanity;
				num3 = UnityEngine.Random.Range(0, currentLevelAmbience.insideAmbienceInsanity.Length);
			}
			else
			{
				if (currentLevelAmbience.insideAmbience.Length == 0)
				{
					return;
				}
				num3 = UnityEngine.Random.Range(0, currentLevelAmbience.insideAmbience.Length);
			}
		}
		else if (!localPlayer.isInHangarShipRoom)
		{
			num2 = 1;
			if (playInsanitySounds)
			{
				if (currentLevelAmbience.outsideAmbienceInsanity.Length == 0)
				{
					return;
				}
				array = currentLevelAmbience.outsideAmbienceInsanity;
				num3 = UnityEngine.Random.Range(0, currentLevelAmbience.outsideAmbienceInsanity.Length);
			}
			else
			{
				if (currentLevelAmbience.outsideAmbience.Length == 0)
				{
					return;
				}
				num3 = UnityEngine.Random.Range(0, currentLevelAmbience.outsideAmbience.Length);
			}
		}
		else
		{
			num2 = 2;
			if (playInsanitySounds)
			{
				if (currentLevelAmbience.shipAmbienceInsanity.Length == 0)
				{
					return;
				}
				array = currentLevelAmbience.shipAmbienceInsanity;
				num3 = UnityEngine.Random.Range(0, currentLevelAmbience.shipAmbienceInsanity.Length);
			}
			else
			{
				if (currentLevelAmbience.shipAmbience.Length == 0)
				{
					return;
				}
				num3 = UnityEngine.Random.Range(0, currentLevelAmbience.shipAmbience.Length);
			}
		}
		if (array != null)
		{
			Debug.Log($"soundtype: {num2}; lastSound: {lastSoundTypePlayed}");
			if (num2 != lastSoundTypePlayed || audioClipProbabilities.Count <= 0)
			{
				Debug.Log($"adding to sound probabilities list; array length: {array.Length}");
				audioClipProbabilities.Clear();
				for (int i = 0; i < array.Length; i++)
				{
					audioClipProbabilities.Add(array[i].chance);
				}
			}
			Debug.Log(audioClipProbabilities.Count);
			num3 = RoundManager.Instance.GetRandomWeightedIndexList(audioClipProbabilities, audioRandom);
			Debug.Log(num3);
		}
		if (syncedForAllPlayers)
		{
			lastServerSoundTypePlayed = num2;
		}
		else
		{
			lastSoundTypePlayed = num2;
		}
		num = ((!(UnityEngine.Random.Range(0f, 1f) < 0.4f)) ? UnityEngine.Random.Range(0.7f, 0.9f) : UnityEngine.Random.Range(0.3f, 0.8f));
		if (syncedForAllPlayers)
		{
			PlayAmbienceClipServerRpc(num2, num3, num, playInsanitySounds);
		}
		else
		{
			PlayAmbienceClipLocal(num2, num3, num, playInsanitySounds);
		}
	}

	public void ResetSoundType()
	{
		lastSoundTypePlayed = -1;
		lastServerSoundTypePlayed = -1;
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayAmbienceClipServerRpc(int soundType, int clipIndex, float soundVolume, bool playInsanitySounds)
			{
				PlayAmbienceClipClientRpc(soundType, clipIndex, soundVolume, playInsanitySounds);
			}

	[ClientRpc]
	public void PlayAmbienceClipClientRpc(int soundType, int clipIndex, float soundVolume, bool playInsanitySounds)
{		try
		{
			Debug.Log($"clip index: {clipIndex}; current planet: {StartOfRound.Instance.currentLevel.PlanetName}");
			switch (soundType)
			{
			case 0:
				Debug.Log($"Current inside ambience clips length: {currentLevelAmbience.insideAmbience.Length}");
				if (playInsanitySounds)
				{
					PlaySoundAroundPlayersAsGroup(currentLevelAmbience.insideAmbienceInsanity[clipIndex].audioClip, soundVolume);
				}
				else
				{
					PlaySoundAroundPlayersAsGroup(currentLevelAmbience.insideAmbience[clipIndex], soundVolume);
				}
				break;
			case 1:
				Debug.Log($"Current outside ambience clips length: {currentLevelAmbience.outsideAmbience.Length}");
				if (playInsanitySounds)
				{
					PlaySoundAroundPlayersAsGroup(currentLevelAmbience.outsideAmbienceInsanity[clipIndex].audioClip, soundVolume);
				}
				else
				{
					PlaySoundAroundPlayersAsGroup(currentLevelAmbience.outsideAmbience[clipIndex], soundVolume);
				}
				break;
			case 2:
				Debug.Log($"Current ship ambience clips length: {currentLevelAmbience.shipAmbience.Length}");
				if (playInsanitySounds)
				{
					PlaySoundAroundPlayersAsGroup(currentLevelAmbience.shipAmbienceInsanity[clipIndex].audioClip, soundVolume);
				}
				else
				{
					PlaySoundAroundPlayersAsGroup(currentLevelAmbience.shipAmbience[clipIndex], soundVolume);
				}
				break;
			}
		}
		catch (Exception message)
		{
			Debug.Log(message);
		}
}
	public void PlayAmbienceClipLocal(int soundType, int clipIndex, float soundVolume, bool playInsanitySounds)
	{
		switch (soundType)
		{
		case 0:
			if (playInsanitySounds)
			{
				PlaySoundAroundLocalPlayer(currentLevelAmbience.insideAmbienceInsanity[clipIndex].audioClip, soundVolume);
			}
			else
			{
				PlaySoundAroundLocalPlayer(currentLevelAmbience.insideAmbience[clipIndex], soundVolume);
			}
			break;
		case 1:
			if (playInsanitySounds)
			{
				PlaySoundAroundLocalPlayer(currentLevelAmbience.outsideAmbienceInsanity[clipIndex].audioClip, soundVolume);
			}
			else
			{
				PlaySoundAroundLocalPlayer(currentLevelAmbience.outsideAmbience[clipIndex], soundVolume);
			}
			break;
		case 2:
			if (playInsanitySounds)
			{
				PlaySoundAroundLocalPlayer(currentLevelAmbience.shipAmbienceInsanity[clipIndex].audioClip, soundVolume);
			}
			else
			{
				PlaySoundAroundLocalPlayer(currentLevelAmbience.shipAmbience[clipIndex], soundVolume);
			}
			break;
		}
	}

	public void PlaySoundAroundPlayersAsGroup(AudioClip clipToPlay, float vol)
	{
		Vector3 randomPositionInRadius = RoundManager.Instance.GetRandomPositionInRadius(RoundManager.AverageOfLivingGroupedPlayerPositions(), 10f, 15f, SoundsRandom);
		ambienceAudio.transform.position = randomPositionInRadius;
		ambienceAudio.volume = vol;
		ambienceAudio.clip = clipToPlay;
		ambienceAudio.Play();
	}

	public void PlaySoundAroundLocalPlayer(AudioClip clipToPlay, float vol)
	{
		Vector3 randomPositionInRadius = RoundManager.Instance.GetRandomPositionInRadius(GameNetworkManager.Instance.localPlayerController.transform.position, 6f, 11f);
		ambienceAudio.transform.position = randomPositionInRadius;
		ambienceAudio.volume = vol;
		ambienceAudio.clip = clipToPlay;
		ambienceAudio.Play();
	}

	public void SetDiageticMixerSnapshot(int snapshotID = 0, float transitionTime = 1f)
	{
		if (currentMixerSnapshotID != snapshotID)
		{
			currentMixerSnapshotID = snapshotID;
			if (!overridingCurrentAudioMixer)
			{
				mixerSnapshots[snapshotID].TransitionTo(transitionTime);
			}
		}
	}

	public void ResumeCurrentMixerSnapshot(float time)
	{
		mixerSnapshots[currentMixerSnapshotID].TransitionTo(time);
	}

	public void PlayAudio1AtPositionForAllClients(Vector3 audioPosition, int clipIndex)
	{
		PlayAudio1AtPositionServerRpc(audioPosition, clipIndex);
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayAudio1AtPositionServerRpc(Vector3 audioPos, int clipIndex)
			{
				PlayAudio1AtPositionClientRpc(audioPos, clipIndex);
			}

	[ClientRpc]
	public void PlayAudio1AtPositionClientRpc(Vector3 audioPos, int clipIndex)
			{
				tempAudio1.transform.position = audioPos;
				tempAudio1.PlayOneShot(syncedAudioClips[clipIndex], 1f);
			}
}
