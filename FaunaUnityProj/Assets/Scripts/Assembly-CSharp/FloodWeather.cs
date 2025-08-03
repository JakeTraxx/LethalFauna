using UnityEngine;

public class FloodWeather : MonoBehaviour
{
	public AudioSource waterAudio;

	private float floodLevelOffset;

	private float previousGlobalTime;

	private void OnEnable()
	{
		if (!(TimeOfDay.Instance == null))
		{
			base.transform.position = new Vector3(0f, TimeOfDay.Instance.currentWeatherVariable, 0f);
			TimeOfDay.Instance.onTimeSync.AddListener(OnGlobalTimeSync);
		}
	}

	private void OnDisable()
	{
		waterAudio.volume = 0f;
		floodLevelOffset = 0f;
		TimeOfDay.Instance.onTimeSync.RemoveListener(OnGlobalTimeSync);
		base.transform.position = new Vector3(0f, -50f, 0f);
	}

	private void OnGlobalTimeSync()
	{
		floodLevelOffset = Mathf.Clamp(TimeOfDay.Instance.globalTime / 1080f, 0f, 100f) * TimeOfDay.Instance.currentWeatherVariable2;
	}

	private void Update()
	{
		if (TimeOfDay.Instance == null)
		{
			return;
		}
		base.transform.position = Vector3.MoveTowards(base.transform.position, new Vector3(0f, TimeOfDay.Instance.currentWeatherVariable, 0f) + Vector3.up * floodLevelOffset, 0.5f * Time.deltaTime);
		if (GameNetworkManager.Instance.localPlayerController.isInsideFactory)
		{
			waterAudio.volume = 0f;
			return;
		}
		waterAudio.transform.position = new Vector3(GameNetworkManager.Instance.localPlayerController.transform.position.x, base.transform.position.y + 3f, GameNetworkManager.Instance.localPlayerController.transform.position.z);
		if (Physics.Linecast(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, waterAudio.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			waterAudio.volume = Mathf.Lerp(waterAudio.volume, 0f, 0.5f * Time.deltaTime);
		}
		else
		{
			waterAudio.volume = Mathf.Lerp(waterAudio.volume, 1f, 0.5f * Time.deltaTime);
		}
	}
}
