using TMPro;
using UnityEngine;

public class DisplayCompanyBuyingRate : MonoBehaviour
{
	public TextMeshProUGUI displayText;

	private void Update()
	{
		displayText.text = $"{Mathf.RoundToInt(StartOfRound.Instance.companyBuyingRate * 100f)}%";
	}
}
