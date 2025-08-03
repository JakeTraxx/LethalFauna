using UnityEngine;

public class ConditionalOnTwoObjects : MonoBehaviour
{
	public GameObject conditionA;

	public GameObject conditionB;

	private void Start()
	{
		if (conditionA == null && conditionB == null)
		{
			Object.Destroy(base.gameObject);
		}
		else
		{
			Object.Destroy(this);
		}
	}
}
