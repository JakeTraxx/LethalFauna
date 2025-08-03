using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeLeaderboardSlot : MonoBehaviour
{
	public RawImage profileIcon;

	public TextMeshProUGUI userNameText;

	public TextMeshProUGUI rankNumText;

	public TextMeshProUGUI scrapCollectedText;

	public SteamId steamId;

	public void SetSlotValues(string userName, int rankNum, int scrapCollected, SteamId playerSteamId, int entryDetails)
	{
		userNameText.text = userName.Substring(0, Mathf.Min(userName.Length, 15));
		rankNumText.text = $"#{rankNum}";
		switch (entryDetails)
		{
		case 2:
			scrapCollectedText.text = "(Removed score)";
			break;
		case 3:
			scrapCollectedText.text = "Deceased";
			break;
		default:
			scrapCollectedText.text = $"${scrapCollected} Collected";
			break;
		}
		steamId = playerSteamId;
		profileIcon.color = Color.white;
		HUDManager.FillImageWithSteamProfile(profileIcon, (ulong)playerSteamId, large: false);
	}

	public void ClickProfileIcon()
	{
		if (!GameNetworkManager.Instance.disableSteam && (ulong)steamId != 0L)
		{
			SteamFriends.OpenUserOverlay(steamId, "steamid");
		}
	}
}
