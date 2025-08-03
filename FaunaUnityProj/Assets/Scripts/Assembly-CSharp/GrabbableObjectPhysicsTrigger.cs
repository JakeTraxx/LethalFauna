using UnityEngine;

public class GrabbableObjectPhysicsTrigger : MonoBehaviour
{
	public GrabbableObject itemScript;

	private void OnTriggerEnter(Collider other)
	{
		if (!itemScript.isHeld && (other.gameObject.CompareTag("Player") || other.gameObject.CompareTag("Enemy")))
		{
			itemScript.ActivatePhysicsTrigger(other);
		}
	}
}
