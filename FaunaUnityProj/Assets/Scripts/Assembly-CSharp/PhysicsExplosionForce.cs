using UnityEngine;

public class PhysicsExplosionForce : MonoBehaviour
{
	private Rigidbody[] bodyParts;

	private void Start()
	{
		bodyParts = GetComponentsInChildren<Rigidbody>();
		for (int i = 0; i < bodyParts.Length; i++)
		{
			bodyParts[i].GetComponent<Rigidbody>().AddExplosionForce(35000f, base.transform.parent.position, 100f);
		}
	}

	private void Update()
	{
		for (int i = 0; i < bodyParts.Length; i++)
		{
			if (!(bodyParts[i] == null) && bodyParts[i].transform.position.y < -200f)
			{
				Object.Destroy(base.gameObject);
				break;
			}
		}
	}
}
