using GameNetcodeStuff;
using UnityEngine;

public class CleanPlayerBodyTrigger : MonoBehaviour
{
	private bool enableCleaning = true;

	public void EnableCleaningTrigger(bool enable)
	{
		enableCleaning = enable;
	}

	private void OnTriggerStay(Collider other)
	{
		if (enableCleaning && other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null)
			{
				component.RemoveBloodFromBody();
			}
		}
	}
}
