using UnityEngine;

public class PowerSwitchable : MonoBehaviour
{
	public OnSwitchPowerEvent powerSwitchEvent;

	public void OnPowerSwitch(bool switchedOn)
	{
		powerSwitchEvent.Invoke(switchedOn);
	}

	private void OnEnable()
	{
		RoundManager.Instance.onPowerSwitch.AddListener(OnPowerSwitch);
	}

	private void OnDisable()
	{
		RoundManager.Instance.onPowerSwitch.RemoveListener(OnPowerSwitch);
	}
}
