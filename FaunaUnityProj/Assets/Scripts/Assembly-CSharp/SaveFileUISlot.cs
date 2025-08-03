using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveFileUISlot : MonoBehaviour
{
	public Button fileButton;

	public Animator buttonAnimator;

	public TextMeshProUGUI fileStatsText;

	public int fileNum;

	private string fileString;

	public TextMeshProUGUI fileNotCompatibleAlert;

	public TextMeshProUGUI specialTipText;

	public TextMeshProUGUI fileNameText;

	private void Awake()
	{
		switch (fileNum)
		{
		case -1:
			fileString = "LCChallengeFile";
			break;
		case 0:
			fileString = "LCSaveFile1";
			break;
		case 1:
			fileString = "LCSaveFile2";
			break;
		case 2:
			fileString = "LCSaveFile3";
			break;
		default:
			fileString = "LCSaveFile1";
			break;
		}
	}

	private void SetChallengeFileSettings()
	{
		if (Object.FindObjectOfType<MenuManager>().hasChallengeBeenCompleted)
		{
			int num = ES3.Load("ProfitEarned", fileString, 0);
			Debug.Log(ES3.Load("ProfitEarned", fileString, 0));
			Debug.Log($"scrapEarnedInFile: {num}");
			fileStatsText.enabled = true;
			fileStatsText.text = $"${num} Collected";
			if (GameNetworkManager.Instance.currentSaveFileName == "LCChallengeFile")
			{
				GameNetworkManager.Instance.currentSaveFileName = "LCSaveFile1";
				GameNetworkManager.Instance.saveFileNum = 0;
				SetButtonColorForAllFileSlots();
			}
		}
		else
		{
			fileStatsText.enabled = false;
		}
	}

	private void OnEnable()
	{
		if (fileNum == -1)
		{
			fileNameText.text = GameNetworkManager.Instance.GetNameForWeekNumber();
		}
		if (ES3.FileExists(fileString))
		{
			if (fileNum == -1)
			{
				SetChallengeFileSettings();
			}
			else
			{
				int num = ES3.Load("GroupCredits", fileString, 0);
				int num2 = ES3.Load("Stats_DaysSpent", fileString, 0);
				fileStatsText.text = $"${num}\nDays: {num2}";
			}
		}
		else
		{
			fileStatsText.text = "";
		}
		if (fileNum != -1 && !Object.FindObjectOfType<MenuManager>().filesCompatible[fileNum])
		{
			fileNotCompatibleAlert.enabled = true;
		}
	}

	public void SetButtonColor()
	{
		buttonAnimator.SetBool("isPressed", GameNetworkManager.Instance.currentSaveFileName == fileString);
		if (specialTipText != null && GameNetworkManager.Instance.currentSaveFileName != fileString)
		{
			specialTipText.enabled = false;
		}
	}

	public void SetFileToThis()
	{
		if (Object.FindObjectOfType<MenuManager>().requestingLeaderboard)
		{
			return;
		}
		if (fileNum == -1 && Object.FindObjectOfType<MenuManager>().hasChallengeBeenCompleted)
		{
			Object.FindObjectOfType<MenuManager>().EnableLeaderboardDisplay(enable: true);
		}
		else
		{
			Object.FindObjectOfType<MenuManager>().EnableLeaderboardDisplay(enable: false);
			if (fileNum == -1)
			{
				specialTipText.text = "This is the weekly challenge moon. You have one day to make as much profit as possible.";
				specialTipText.enabled = true;
			}
		}
		GameNetworkManager.Instance.currentSaveFileName = fileString;
		GameNetworkManager.Instance.saveFileNum = fileNum;
		SetButtonColorForAllFileSlots();
	}

	public void SetButtonColorForAllFileSlots()
	{
		SaveFileUISlot[] array = Object.FindObjectsOfType<SaveFileUISlot>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetButtonColor();
		}
	}
}
