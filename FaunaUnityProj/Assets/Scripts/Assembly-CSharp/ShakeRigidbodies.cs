using UnityEngine;

public class ShakeRigidbodies : MonoBehaviour
{
	public Rigidbody[] rigidBodies;

	public float shakeTimer = 8f;

	public float shakeIntensity;

	private bool shaking = true;

	private void Update()
	{
		if (shakeTimer > 0f)
		{
			shakeTimer -= Time.deltaTime;
		}
		else
		{
			shaking = false;
		}
	}

	private void FixedUpdate()
	{
		if (shaking)
		{
			for (int i = 0; i < rigidBodies.Length; i++)
			{
				rigidBodies[i].AddForce(new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeIntensity, ForceMode.Force);
			}
		}
	}
}
