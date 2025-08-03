using Steamworks;
using UnityEngine;

public class SteamManager : MonoBehaviour
{
	public static SteamManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnDisable()
	{
		SteamClient.Shutdown();
	}

	private void Update()
	{
		SteamClient.RunCallbacks();
	}
}
