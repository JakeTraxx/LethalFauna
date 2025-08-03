using System.Collections;
using Dissonance;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreInitSceneScript : MonoBehaviour
{
	public AudioSource mainAudio;

	public AudioClip hoverSFX;

	public AudioClip selectSFX;

	private bool choseLaunchOption;

	[Header("Other initial launch settings")]
	public Slider gammaSlider;

	public GameObject continueButton;

	public Animator blackTransition;

	public GameObject OnlineModeButton;

	public GameObject[] LaunchSettingsPanels;

	public int currentLaunchSettingPanel;

	public TextMeshProUGUI headerText;

	public GameObject FileCorruptedPanel;

	public GameObject FileCorruptedDialoguePanel;

	public GameObject FileCorruptedRestartButton;

	public GameObject restartingGameText;

	public GameObject launchSettingsPanelsContainer;

	private void Awake()
	{
		DissonanceComms.TestDependencies();
	}

	private void Start()
	{
		gammaSlider.value = IngamePlayerSettings.Instance.settings.gammaSetting / 0.05f;
	}

	public void PressContinueButton()
	{
		if (currentLaunchSettingPanel < LaunchSettingsPanels.Length)
		{
			LaunchSettingsPanels[currentLaunchSettingPanel].SetActive(value: false);
			currentLaunchSettingPanel++;
			LaunchSettingsPanels[currentLaunchSettingPanel].SetActive(value: true);
			blackTransition.SetTrigger("Transition");
			if (currentLaunchSettingPanel >= LaunchSettingsPanels.Length - 1)
			{
				continueButton.SetActive(value: false);
				headerText.text = "LAUNCH MODE";
			}
		}
	}

	public void HoverButton()
	{
		mainAudio.PlayOneShot(hoverSFX);
	}

	public void ChooseLaunchOption(bool online)
	{
		if (!choseLaunchOption)
		{
			choseLaunchOption = true;
			mainAudio.PlayOneShot(selectSFX);
			IngamePlayerSettings.Instance.SetPlayerFinishedLaunchOptions();
			IngamePlayerSettings.Instance.SaveChangedSettings();
			if (!IngamePlayerSettings.Instance.encounteredErrorDuringSave)
			{
				StartCoroutine(loadSceneDelayed(online));
			}
		}
	}

	private IEnumerator loadSceneDelayed(bool online)
	{
		yield return new WaitForSeconds(0.2f);
		if (online)
		{
			SceneManager.LoadScene("InitScene");
		}
		else
		{
			SceneManager.LoadScene("InitSceneLANMode");
		}
	}

	public void SetLaunchPanelsEnabled()
	{
		launchSettingsPanelsContainer.SetActive(value: true);
	}

	public void SkipToFinalSetting()
	{
		LaunchSettingsPanels[currentLaunchSettingPanel].SetActive(value: false);
		currentLaunchSettingPanel = LaunchSettingsPanels.Length - 1;
		LaunchSettingsPanels[currentLaunchSettingPanel].SetActive(value: true);
		continueButton.SetActive(value: false);
		headerText.text = "LAUNCH MODE";
		EventSystem.current.SetSelectedGameObject(OnlineModeButton);
	}

	public void EnableFileCorruptedScreen()
	{
		LaunchSettingsPanels[currentLaunchSettingPanel].SetActive(value: false);
		FileCorruptedPanel.SetActive(value: true);
		EventSystem.current.SetSelectedGameObject(FileCorruptedRestartButton);
	}

	public void EraseFileAndRestart()
	{
		StartCoroutine(restartGameDueToCorruptedFile());
	}

	private IEnumerator restartGameDueToCorruptedFile()
	{
		if (ES3.FileExists("LCGeneralSaveData"))
		{
			ES3.DeleteFile("LCGeneralSaveData");
		}
		if (ES3.FileExists("LCSaveFile1"))
		{
			ES3.DeleteFile("LCSaveFile1");
		}
		if (ES3.FileExists("LCSaveFile2"))
		{
			ES3.DeleteFile("LCSaveFile2");
		}
		if (ES3.FileExists("LCSaveFile3"))
		{
			ES3.DeleteFile("LCSaveFile3");
		}
		FileCorruptedDialoguePanel.SetActive(value: false);
		restartingGameText.SetActive(value: true);
		yield return new WaitForSeconds(2f);
		Application.Quit();
	}
}
