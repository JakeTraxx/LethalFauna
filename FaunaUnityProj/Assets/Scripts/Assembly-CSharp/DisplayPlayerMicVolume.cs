using Dissonance;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DisplayPlayerMicVolume : MonoBehaviour
{
	public bool useDissonanceForMicDetection;

	[Space(3f)]
	private DissonanceComms comms;

	public Image volumeMeterImage;

	public float detectedVolumeAmplitude;

	private VoicePlayerState playerState;

	public float MicLoudness;

	private string _device;

	private AudioClip _clipRecord;

	private int _sampleWindow = 128;

	private bool _isInitialized;

	private void InitMic()
	{
		IngamePlayerSettings.Instance.RefreshAndDisplayCurrentMicrophone(saveResult: false);
		if (IngamePlayerSettings.Instance.settings.micDevice == "none")
		{
			Debug.Log("No devices connected");
			return;
		}
		if (_device != null && Microphone.IsRecording(_device))
		{
			StopMicrophone();
		}
		_device = IngamePlayerSettings.Instance.unsavedSettings.micDevice;
		Microphone.GetDeviceCaps(_device, out var minFreq, out var maxFreq);
		_clipRecord = Microphone.Start(_device, loop: true, 1, Mathf.Clamp(5000, minFreq, maxFreq));
	}

	private void StopMicrophone()
	{
		Microphone.End(_device);
	}

	public void SwitchMicrophone()
	{
		if (_isInitialized)
		{
			Microphone.End(_device);
		}
		InitMic();
	}

	private float LevelMax()
	{
		float num = 0f;
		float[] array = new float[_sampleWindow];
		int num2 = Microphone.GetPosition(IngamePlayerSettings.Instance.unsavedSettings.micDevice) - (_sampleWindow + 1);
		if (num2 < 0)
		{
			return 0f;
		}
		_clipRecord.GetData(array, num2);
		for (int i = 0; i < _sampleWindow; i++)
		{
			float num3 = array[i] * array[i];
			if (num < num3)
			{
				num = num3;
			}
		}
		return num;
	}

	private void Update()
	{
		volumeMeterImage.fillAmount = Mathf.Lerp(volumeMeterImage.fillAmount, detectedVolumeAmplitude, 25f * Time.deltaTime);
		detectedVolumeAmplitude = 0f;
		if (!IngamePlayerSettings.Instance.unsavedSettings.micEnabled)
		{
			return;
		}
		if (useDissonanceForMicDetection && NetworkManager.Singleton != null)
		{
			if (comms == null)
			{
				comms = Object.FindObjectOfType<DissonanceComms>();
			}
			detectedVolumeAmplitude = Mathf.Clamp(comms.FindPlayer(comms.LocalPlayerName).Amplitude * 35f, 0f, 1f);
		}
		else
		{
			detectedVolumeAmplitude = Mathf.Clamp(LevelMax() * 300f, 0f, 1f);
		}
		if (detectedVolumeAmplitude < 0.25f)
		{
			detectedVolumeAmplitude = 0f;
		}
	}

	private void OnEnable()
	{
		if (!useDissonanceForMicDetection && Application.isFocused)
		{
			InitMic();
			_isInitialized = true;
		}
	}

	private void Awake()
	{
		if (!useDissonanceForMicDetection)
		{
			_clipRecord = AudioClip.Create("newClip", 44100, 1, 2000, stream: true);
		}
	}

	private void OnDisable()
	{
		if (!useDissonanceForMicDetection)
		{
			StopMicrophone();
			_isInitialized = false;
		}
	}

	private void OnDestroy()
	{
		if (!useDissonanceForMicDetection)
		{
			StopMicrophone();
		}
	}

	private void OnApplicationFocus(bool focus)
	{
		if (!useDissonanceForMicDetection)
		{
			if (focus && !_isInitialized)
			{
				InitMic();
				_isInitialized = true;
			}
			if (!focus)
			{
				StopMicrophone();
				_isInitialized = false;
			}
		}
	}
}
