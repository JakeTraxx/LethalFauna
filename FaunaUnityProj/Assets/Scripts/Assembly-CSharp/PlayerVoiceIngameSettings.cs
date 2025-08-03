using Dissonance;
using Dissonance.Audio.Playback;
using UnityEngine;

public class PlayerVoiceIngameSettings : MonoBehaviour
{
	public AudioReverbFilter filter;

	public AudioSource voiceAudio;

	public VoicePlayback _playbackComponent;

	public DissonanceComms _dissonanceComms;

	public VoicePlayerState _playerState;

	public bool set2D;

	private bool isEnabled;

	private void Awake()
	{
		InitializeComponents();
	}

	public void InitializeComponents()
	{
		_playbackComponent = GetComponent<VoicePlayback>();
		_dissonanceComms = Object.FindObjectOfType<DissonanceComms>();
		filter = base.gameObject.GetComponent<AudioReverbFilter>();
		voiceAudio = base.gameObject.GetComponent<AudioSource>();
	}

	private void LateUpdate()
	{
		if (isEnabled)
		{
			if (voiceAudio == null)
			{
				voiceAudio = base.gameObject.GetComponent<AudioSource>();
			}
			if (set2D)
			{
				voiceAudio.spatialBlend = 0f;
			}
			else
			{
				voiceAudio.spatialBlend = 1f;
			}
		}
	}

	private void OnEnable()
	{
		isEnabled = true;
		if (_playbackComponent == null)
		{
			InitializeComponents();
			if (_playbackComponent == null)
			{
				return;
			}
		}
		_playerState = _dissonanceComms.FindPlayer(_playbackComponent.PlayerName);
	}

	public void FindPlayerIfNull()
	{
		if (_playerState == null)
		{
			if (_playbackComponent == null)
			{
				InitializeComponents();
				if (_playbackComponent == null)
				{
					return;
				}
			}
			if (string.IsNullOrEmpty(_playbackComponent.PlayerName))
			{
				return;
			}
			_playerState = _dissonanceComms.FindPlayer(_playbackComponent.PlayerName);
		}
		InitializeComponents();
	}

	private void OnDisable()
	{
		isEnabled = false;
		voiceAudio = null;
		filter = null;
		_playerState = null;
	}
}
