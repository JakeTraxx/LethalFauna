using System;
using System.Collections;
using Dissonance;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class IngamePlayerSettings : MonoBehaviour
{
	[Serializable]
	public class Settings
	{
		public bool playerHasFinishedSetup;

		public bool startInOnlineMode = true;

		public float gammaSetting;

		public int lookSensitivity = 10;

		public bool invertYAxis;

		public float masterVolume = 1f;

		public int framerateCapIndex;

		public FullScreenMode fullScreenType;

		[Header("MIC SETTINGS")]
		public bool micEnabled = true;

		public bool pushToTalk;

		public int micDeviceIndex;

		public string micDevice = string.Empty;

		[Header("BINDINGS")]
		public string keyBindings = string.Empty;

		[Header("ACCESSIBILITY")]
		public bool spiderSafeMode;

		public Settings(bool finishedSetup = true, bool onlineMode = true)
		{
			playerHasFinishedSetup = finishedSetup;
			startInOnlineMode = onlineMode;
		}

		public void CopySettings(Settings copyFrom)
		{
			playerHasFinishedSetup = copyFrom.playerHasFinishedSetup;
			startInOnlineMode = copyFrom.startInOnlineMode;
			gammaSetting = copyFrom.gammaSetting;
			lookSensitivity = copyFrom.lookSensitivity;
			micEnabled = copyFrom.micEnabled;
			pushToTalk = copyFrom.pushToTalk;
			micDeviceIndex = copyFrom.micDeviceIndex;
			micDevice = copyFrom.micDevice;
			keyBindings = copyFrom.keyBindings;
			masterVolume = copyFrom.masterVolume;
			framerateCapIndex = copyFrom.framerateCapIndex;
			fullScreenType = copyFrom.fullScreenType;
			invertYAxis = copyFrom.invertYAxis;
			spiderSafeMode = copyFrom.spiderSafeMode;
		}
	}

	public Settings settings;

	public Settings unsavedSettings;

	public AudioSource SettingsAudio;

	public Volume universalVolume;

	private DissonanceComms comms;

	public bool redoLaunchSettings;

	public bool changesNotApplied;

	public InputActionRebindingExtensions.RebindingOperation rebindingOperation;

	private SettingsOption currentRebindingKeyUI;

	public PlayerInput playerInput;

	public bool encounteredErrorDuringSave;

	public static IngamePlayerSettings Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
			StartCoroutine(waitToLoadSettings());
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private IEnumerator waitToLoadSettings()
	{
		ES3.Init();
		yield return new WaitForSeconds(0.5f);
		try
		{
			LoadSettingsFromPrefs();
			UpdateGameToMatchSettings();
		}
		catch (Exception e)
		{
			DisplaySaveFileError(e);
			yield break;
		}
		PreInitSceneScript preInitSceneScript = UnityEngine.Object.FindObjectOfType<PreInitSceneScript>();
		preInitSceneScript.SetLaunchPanelsEnabled();
		if (settings.playerHasFinishedSetup && preInitSceneScript != null)
		{
			preInitSceneScript.SkipToFinalSetting();
		}
	}

	private void DisplaySaveFileError(Exception e)
	{
		Debug.LogError($"Error while loading general save data file!: {e}, enabling error panel for player");
		encounteredErrorDuringSave = true;
		PreInitSceneScript preInitSceneScript = UnityEngine.Object.FindObjectOfType<PreInitSceneScript>();
		if (preInitSceneScript != null)
		{
			preInitSceneScript.EnableFileCorruptedScreen();
		}
	}

	public void LoadSettingsFromPrefs()
	{
		string filePath = "LCGeneralSaveData";
		settings.playerHasFinishedSetup = ES3.Load("PlayerFinishedSetup", filePath, defaultValue: false);
		settings.startInOnlineMode = ES3.Load("StartInOnlineMode", filePath, defaultValue: false);
		settings.gammaSetting = ES3.Load("Gamma", filePath, 0f);
		settings.masterVolume = ES3.Load("MasterVolume", filePath, 1f);
		settings.lookSensitivity = ES3.Load("LookSens", filePath, 10);
		settings.micEnabled = ES3.Load("MicEnabled", filePath, defaultValue: true);
		settings.pushToTalk = ES3.Load("PushToTalk", filePath, defaultValue: false);
		settings.micDevice = ES3.Load("CurrentMic", filePath, "LCNoMic");
		settings.keyBindings = ES3.Load("Bindings", filePath, string.Empty);
		settings.framerateCapIndex = ES3.Load("FPSCap", filePath, 0);
		settings.fullScreenType = (FullScreenMode)ES3.Load("ScreenMode", filePath, 1);
		settings.invertYAxis = ES3.Load("InvertYAxis", filePath, defaultValue: false);
		settings.spiderSafeMode = ES3.Load("SpiderSafeMode", filePath, defaultValue: false);
		if (!string.IsNullOrEmpty(settings.keyBindings))
		{
			playerInput.actions.LoadBindingOverridesFromJson(settings.keyBindings);
		}
		unsavedSettings.CopySettings(settings);
	}

	public void SaveSettingsToPrefs()
	{
		string filePath = "LCGeneralSaveData";
		try
		{
			ES3.Save("PlayerFinishedSetup", settings.playerHasFinishedSetup, filePath);
			ES3.Save("StartInOnlineMode", settings.startInOnlineMode, filePath);
			ES3.Save("Gamma", settings.gammaSetting, filePath);
			ES3.Save("MasterVolume", settings.masterVolume, filePath);
			ES3.Save("LookSens", settings.lookSensitivity, filePath);
			ES3.Save("MicEnabled", settings.micEnabled, filePath);
			ES3.Save("PushToTalk", settings.pushToTalk, filePath);
			ES3.Save("CurrentMic", settings.micDevice, filePath);
			ES3.Save("Bindings", settings.keyBindings, filePath);
			ES3.Save("FPSCap", settings.framerateCapIndex, filePath);
			ES3.Save("ScreenMode", (int)settings.fullScreenType, filePath);
			ES3.Save("InvertYAxis", settings.invertYAxis, filePath);
			ES3.Save("SpiderSafeMode", settings.spiderSafeMode, filePath);
		}
		catch (Exception e)
		{
			DisplaySaveFileError(e);
		}
	}

	public void UpdateAllKeybindOptions()
	{
		SettingsOption[] array = UnityEngine.Object.FindObjectsOfType<SettingsOption>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetBindingToCurrentSetting();
		}
		KepRemapPanel kepRemapPanel = UnityEngine.Object.FindObjectOfType<KepRemapPanel>();
		if (kepRemapPanel != null)
		{
			Debug.Log("Reseting keybind UI");
			kepRemapPanel.ResetKeybindsUI();
		}
	}

	public void UpdateGameToMatchSettings()
	{
		ChangeGamma(0, settings.gammaSetting);
		SetFramerateCap(settings.framerateCapIndex);
		SetFullscreenMode((int)settings.fullScreenType);
		AudioListener.volume = settings.masterVolume;
		UpdateMicPushToTalkButton();
		RefreshAndDisplayCurrentMicrophone();
		SettingsOption[] array = UnityEngine.Object.FindObjectsOfType<SettingsOption>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetValueToMatchSettings();
		}
		if (comms != null && StartOfRound.Instance != null)
		{
			comms.IsMuted = !settings.micEnabled;
		}
	}

	public void SetOption(SettingsOptionType optionType, int value)
	{
		if (GameNetworkManager.Instance != null)
		{
			SettingsAudio.PlayOneShot(GameNetworkManager.Instance.buttonTuneSFX);
		}
		Debug.Log($"Set settings not applied!; {optionType}");
		SetChangesNotAppliedTextVisible();
		switch (optionType)
		{
		case SettingsOptionType.Gamma:
			ChangeGamma(value);
			break;
		case SettingsOptionType.MasterVolume:
			ChangeMasterVolume(value);
			break;
		case SettingsOptionType.LookSens:
			ChangeLookSens(value);
			break;
		case SettingsOptionType.MicDevice:
			SwitchMicrophoneSetting();
			break;
		case SettingsOptionType.MicEnabled:
			SetMicrophoneEnabled();
			break;
		case SettingsOptionType.MicPushToTalk:
			SetMicPushToTalk();
			break;
		case SettingsOptionType.FramerateCap:
			SetFramerateCap(value);
			break;
		case SettingsOptionType.FullscreenType:
			SetFullscreenMode(value);
			break;
		case SettingsOptionType.InvertYAxis:
			SetInvertYAxis();
			break;
		case SettingsOptionType.SpiderSafeMode:
			SetSpiderSafeMode();
			break;
		case SettingsOptionType.OnlineMode:
		case SettingsOptionType.ChangeBinding:
		case SettingsOptionType.CancelOrConfirm:
			break;
		}
	}

	private void SetSpiderSafeMode()
	{
		unsavedSettings.spiderSafeMode = !unsavedSettings.spiderSafeMode;
	}

	private void SetInvertYAxis()
	{
		unsavedSettings.invertYAxis = !unsavedSettings.invertYAxis;
	}

	private void SetFullscreenMode(int value)
	{
		Screen.fullScreenMode = (FullScreenMode)value;
		unsavedSettings.fullScreenType = (FullScreenMode)value;
	}

	private void SetFramerateCap(int value)
	{
		switch (value)
		{
		case 0:
			QualitySettings.vSyncCount = 1;
			Application.targetFrameRate = -1;
			break;
		case 1:
			QualitySettings.vSyncCount = 0;
			Application.targetFrameRate = 250;
			break;
		default:
			QualitySettings.vSyncCount = 0;
			switch (value)
			{
			case 2:
				Application.targetFrameRate = 144;
				break;
			case 3:
				Application.targetFrameRate = 120;
				break;
			case 4:
				Application.targetFrameRate = 60;
				break;
			case 5:
				Application.targetFrameRate = 30;
				break;
			}
			break;
		}
		unsavedSettings.framerateCapIndex = value;
	}

	public void ChangeGamma(int setTo, float overrideWithFloat = -500f)
	{
		float num = Mathf.Clamp((float)setTo * 0.05f, -0.85f, 2f);
		if (overrideWithFloat != -500f)
		{
			num = overrideWithFloat;
		}
		if (universalVolume.sharedProfile.TryGet<LiftGammaGain>(out var component))
		{
			component.gamma.SetValue(new Vector4Parameter(new Vector4(0f, 0f, 0f, num), overrideState: true));
		}
		unsavedSettings.gammaSetting = num;
	}

	public void ChangeMasterVolume(int setTo)
	{
		unsavedSettings.masterVolume = (float)setTo / 100f;
		AudioListener.volume = (float)setTo / 100f;
	}

	public void ChangeLookSens(int setTo)
	{
		unsavedSettings.lookSensitivity = setTo;
		Debug.Log($"Set mouse sensitivity to new value: {setTo}");
	}

	public void RefreshAndDisplayCurrentMicrophone(bool saveResult = true)
	{
		Settings settings = ((!saveResult) ? unsavedSettings : this.settings);
		settings.micDeviceIndex = 0;
		bool flag = false;
		string text = ((!saveResult) ? unsavedSettings.micDevice : this.settings.micDevice);
		for (int i = 0; i < Microphone.devices.Length; i++)
		{
			if (Microphone.devices[i] == text)
			{
				settings.micDeviceIndex = i;
				settings.micDevice = Microphone.devices[i];
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			if (Microphone.devices.Length == 0)
			{
				SetSettingsOptionsText(SettingsOptionType.MicDevice, "No device found \n (click to refresh)");
				settings.micDevice = "LCNoMic";
				Debug.Log("No recording devices found");
				return;
			}
			settings.micDevice = Microphone.devices[0];
		}
		SetSettingsOptionsText(SettingsOptionType.MicDevice, "Current input device: \n " + settings.micDevice);
		if (saveResult && comms != null)
		{
			comms.MicrophoneName = settings.micDevice;
		}
	}

	public void SetSettingsOptionsText(SettingsOptionType optionType, string setToText)
	{
		SettingsOption[] array = UnityEngine.Object.FindObjectsOfType<SettingsOption>(includeInactive: true);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].optionType == optionType && array[i].textElement != null)
			{
				array[i].textElement.text = setToText;
			}
		}
	}

	public void SwitchMicrophoneSetting()
	{
		if (Microphone.devices.Length == 0)
		{
			Debug.Log("No mics found when trying to switch");
			return;
		}
		Debug.Log("Switching microphone");
		unsavedSettings.micDeviceIndex = ++unsavedSettings.micDeviceIndex % Microphone.devices.Length;
		unsavedSettings.micDevice = Microphone.devices[unsavedSettings.micDeviceIndex];
		SetSettingsOptionsText(SettingsOptionType.MicDevice, "Current input device: \n " + unsavedSettings.micDevice);
		DisplayPlayerMicVolume displayPlayerMicVolume = UnityEngine.Object.FindObjectOfType<DisplayPlayerMicVolume>();
		if (displayPlayerMicVolume != null)
		{
			displayPlayerMicVolume.SwitchMicrophone();
		}
		if (comms != null)
		{
			comms.MicrophoneName = unsavedSettings.micDevice;
		}
	}

	public void SetMicrophoneEnabled()
	{
		unsavedSettings.micEnabled = !unsavedSettings.micEnabled;
		if (comms != null && StartOfRound.Instance != null)
		{
			comms.IsMuted = !settings.micEnabled;
		}
	}

	public void SetMicPushToTalk()
	{
		unsavedSettings.pushToTalk = !unsavedSettings.pushToTalk;
		if (unsavedSettings.pushToTalk)
		{
			SetSettingsOptionsText(SettingsOptionType.MicPushToTalk, "MODE: Push to talk");
		}
		else
		{
			SetSettingsOptionsText(SettingsOptionType.MicPushToTalk, "MODE: Voice activation");
		}
	}

	public void UpdateMicPushToTalkButton()
	{
		if (settings.pushToTalk)
		{
			SetSettingsOptionsText(SettingsOptionType.MicPushToTalk, "MODE: Push to talk");
		}
		else
		{
			SetSettingsOptionsText(SettingsOptionType.MicPushToTalk, "MODE: Voice activation");
		}
	}

	public void SetPlayerFinishedLaunchOptions()
	{
		settings.playerHasFinishedSetup = true;
		unsavedSettings.playerHasFinishedSetup = true;
		ES3.Save("PlayerFinishedSetup", value: true, "LCGeneralSaveData");
	}

	public void SetLaunchInOnlineMode(bool enable)
	{
		settings.startInOnlineMode = enable;
		unsavedSettings.startInOnlineMode = enable;
		ES3.Save("StartInOnlineMode", enable, "LCGeneralSaveData");
	}

	public void RebindKey(InputActionReference rebindableAction, SettingsOption optionUI, int rebindIndex, bool gamepadRebinding = false)
	{
		if (rebindingOperation != null)
		{
			rebindingOperation.Dispose();
			if (currentRebindingKeyUI != null)
			{
				currentRebindingKeyUI.currentlyUsedKeyText.enabled = true;
				currentRebindingKeyUI.waitingForInput.SetActive(value: false);
			}
		}
		optionUI.currentlyUsedKeyText.enabled = false;
		optionUI.waitingForInput.SetActive(value: true);
		playerInput.DeactivateInput();
		currentRebindingKeyUI = optionUI;
		bool getBindingIndexManually = rebindIndex != -1;
		if (rebindIndex == -1)
		{
			rebindIndex = 0;
		}
		Debug.Log($"Rebinding starting.. rebindIndex: {rebindIndex}");
		if (gamepadRebinding)
		{
			rebindingOperation = rebindableAction.action.PerformInteractiveRebinding(rebindIndex).OnMatchWaitForAnother(0.1f).WithControlsHavingToMatchPath("<Gamepad>")
				.WithCancelingThrough(playerInput.actions.FindAction("OpenMenu").controls[2])
				.OnComplete(delegate
				{
					CompleteRebind(optionUI, getBindingIndexManually, rebindIndex);
				})
				.Start();
		}
		else
		{
			rebindingOperation = rebindableAction.action.PerformInteractiveRebinding(rebindIndex).OnMatchWaitForAnother(0.1f).WithControlsHavingToMatchPath("<Keyboard>")
				.WithControlsHavingToMatchPath("<Mouse>")
				.WithControlsExcluding("<Mouse>/scroll/y")
				.WithCancelingThrough(playerInput.actions.FindAction("OpenMenu").controls[0])
				.OnComplete(delegate
				{
					CompleteRebind(optionUI, getBindingIndexManually, rebindIndex);
				})
				.Start();
		}
		Debug.Log("Rebinding starting.. B");
	}

	public void CompleteRebind(SettingsOption optionUI, bool getBindingIndexManually, int setBindingIndex = 0)
	{
		InputAction action = rebindingOperation.action;
		if (rebindingOperation != null)
		{
			rebindingOperation.Dispose();
		}
		playerInput.ActivateInput();
		int num;
		if (!getBindingIndexManually)
		{
			num = action.GetBindingIndexForControl(action.controls[0]);
			Debug.Log($"Setting binding index to default which is {num}");
		}
		else
		{
			Debug.Log($"Setting binding index to manual which is {setBindingIndex}");
			num = setBindingIndex;
		}
		optionUI.currentlyUsedKeyText.text = InputControlPath.ToHumanReadableString(action.bindings[num].effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
		optionUI.currentlyUsedKeyText.enabled = true;
		optionUI.waitingForInput.SetActive(value: false);
		Debug.Log("Rebinding finishing.. A");
		unsavedSettings.keyBindings = playerInput.actions.SaveBindingOverridesAsJson();
		SetChangesNotAppliedTextVisible();
		Debug.Log("Rebinding finishing.. B");
	}

	public void CancelRebind(SettingsOption optionUI = null)
	{
		if (rebindingOperation != null)
		{
			rebindingOperation.Dispose();
		}
		try
		{
			playerInput.ActivateInput();
		}
		catch (Exception arg)
		{
			Debug.Log($"Unable to activate input!: {arg}");
		}
		if (!(optionUI == null))
		{
			optionUI.currentlyUsedKeyText.enabled = true;
			optionUI.waitingForInput.SetActive(value: false);
		}
	}

	public void ResetSettingsToDefault()
	{
		SetChangesNotAppliedTextVisible(visible: false);
		Settings copyFrom = new Settings(settings.playerHasFinishedSetup, settings.startInOnlineMode);
		settings.CopySettings(copyFrom);
		unsavedSettings.CopySettings(copyFrom);
		SaveSettingsToPrefs();
		UpdateGameToMatchSettings();
	}

	public void ResetAllKeybinds()
	{
		CancelRebind();
		playerInput.actions.RemoveAllBindingOverrides();
		unsavedSettings.keyBindings = string.Empty;
		SetChangesNotAppliedTextVisible();
		UpdateAllKeybindOptions();
	}

	public void SaveChangedSettings()
	{
		SetChangesNotAppliedTextVisible(visible: false);
		Debug.Log("Saving changed settings");
		settings.CopySettings(unsavedSettings);
		SaveSettingsToPrefs();
		UpdateGameToMatchSettings();
	}

	public void DisplayConfirmChangesScreen(bool visible)
	{
		MenuManager menuManager = UnityEngine.Object.FindObjectOfType<MenuManager>();
		if (menuManager != null)
		{
			menuManager.PleaseConfirmChangesSettingsPanel.SetActive(visible);
			menuManager.KeybindsPanel.SetActive(!visible);
			menuManager.PleaseConfirmChangesSettingsPanelBackButton.Select();
			return;
		}
		QuickMenuManager quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
		if (quickMenuManager != null)
		{
			quickMenuManager.PleaseConfirmChangesSettingsPanel.SetActive(visible);
			quickMenuManager.KeybindsPanel.SetActive(!visible);
			quickMenuManager.PleaseConfirmChangesSettingsPanelBackButton.Select();
		}
	}

	public void DiscardChangedSettings()
	{
		SetChangesNotAppliedTextVisible(visible: false);
		Debug.Log("Discarding changed settings");
		unsavedSettings.CopySettings(settings);
		if (!string.IsNullOrEmpty(settings.keyBindings))
		{
			playerInput.actions.LoadBindingOverridesFromJson(settings.keyBindings);
		}
		else
		{
			playerInput.actions.RemoveAllBindingOverrides();
		}
		UpdateGameToMatchSettings();
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode loadType)
	{
		if (loadType == LoadSceneMode.Single)
		{
			UpdateGameToMatchSettings();
			comms = UnityEngine.Object.FindObjectOfType<DissonanceComms>();
		}
	}

	private void SetChangesNotAppliedTextVisible(bool visible = true)
	{
		changesNotApplied = visible;
		MenuManager menuManager = UnityEngine.Object.FindObjectOfType<MenuManager>();
		if (menuManager != null)
		{
			menuManager.changesNotAppliedText.enabled = visible;
			if (visible)
			{
				menuManager.settingsBackButton.text = "DISCARD";
			}
			else
			{
				menuManager.settingsBackButton.text = "BACK";
			}
			return;
		}
		QuickMenuManager quickMenuManager = UnityEngine.Object.FindObjectOfType<QuickMenuManager>();
		if (quickMenuManager != null)
		{
			quickMenuManager.changesNotAppliedText.enabled = visible;
			if (visible)
			{
				quickMenuManager.settingsBackButton.text = "Discard changes";
			}
			else
			{
				quickMenuManager.settingsBackButton.text = "Back";
			}
		}
	}
}
