using UnityEngine;

public class MatchLocalPlayerPosition : MonoBehaviour
{
	private void LateUpdate()
	{
		if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null)
		{
			base.transform.position = GameNetworkManager.Instance.localPlayerController.transform.position;
		}
	}
}
