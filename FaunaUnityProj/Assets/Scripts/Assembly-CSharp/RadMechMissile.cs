using System;
using GameNetcodeStuff;
using UnityEngine;

public class RadMechMissile : MonoBehaviour
{
	private float currentMissileSpeed = 0.35f;

	public RadMechAI RadMechScript;

	private bool hitWall = true;

	private float despawnTimer;

	private System.Random missileFlyRandom;

	private float forwardDistance;

	private float lastRotationDistance;

	private void Start()
	{
		missileFlyRandom = new System.Random((int)(base.transform.position.x + base.transform.position.y) + RadMechScript.missilesFired);
		hitWall = false;
	}

	private void FixedUpdate()
	{
		if (hitWall)
		{
			return;
		}
		if (despawnTimer < 5f && RadMechScript != null)
		{
			despawnTimer += Time.deltaTime;
			CheckCollision();
			base.transform.position += base.transform.forward * RadMechScript.missileSpeed * currentMissileSpeed;
			forwardDistance += RadMechScript.missileSpeed * currentMissileSpeed;
			if (forwardDistance - lastRotationDistance > 2f)
			{
				lastRotationDistance = forwardDistance;
				base.transform.rotation *= Quaternion.Euler(new Vector3(15f * RadMechScript.missileWarbleLevel * (float)(missileFlyRandom.NextDouble() * 2.0 - 1.0), 7f * RadMechScript.missileWarbleLevel * (float)(missileFlyRandom.NextDouble() * 2.0 - 1.0), 15f * RadMechScript.missileWarbleLevel * (float)(missileFlyRandom.NextDouble() * 2.0 - 1.0)));
			}
			currentMissileSpeed += 0.05f;
		}
		else
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void CheckCollision()
	{
		if (!Physics.Raycast(base.transform.position, base.transform.forward, out var hitInfo, 0.6f * currentMissileSpeed, 526592, QueryTriggerInteraction.Ignore) && !Physics.Raycast(base.transform.position, base.transform.forward, out hitInfo, 0.6f * currentMissileSpeed, 8, QueryTriggerInteraction.Collide))
		{
			return;
		}
		if (hitInfo.collider.gameObject.layer == 19)
		{
			EnemyAICollisionDetect component = hitInfo.collider.GetComponent<EnemyAICollisionDetect>();
			if (component != null && component.mainScript == RadMechScript)
			{
				return;
			}
		}
		bool calledByClient = false;
		if (hitInfo.collider.gameObject.layer == 3)
		{
			PlayerControllerB component2 = hitInfo.collider.gameObject.GetComponent<PlayerControllerB>();
			if (component2 != null && component2 == GameNetworkManager.Instance.localPlayerController)
			{
				calledByClient = true;
			}
		}
		hitWall = true;
		RadMechScript.StartExplosion(base.transform.position - base.transform.forward * 0.5f, base.transform.forward, calledByClient);
		UnityEngine.Object.Destroy(base.gameObject);
	}
}
