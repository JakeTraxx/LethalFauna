using UnityEngine;

public class EclipseWeather : MonoBehaviour
{
	private void OnEnable()
	{
		RoundManager.Instance.minOutsideEnemiesToSpawn = (int)TimeOfDay.Instance.currentWeatherVariable;
		RoundManager.Instance.minEnemiesToSpawn = (int)TimeOfDay.Instance.currentWeatherVariable;
	}
}
