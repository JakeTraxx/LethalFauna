using GameNetcodeStuff;
using UnityEngine;

public class VehicleCollisionTrigger : MonoBehaviour
{
	public VehicleController mainScript;

	private float timeSinceHittingPlayer;

	private float timeSinceHittingEnemy;

	public BoxCollider insideTruckNavMeshBounds;

	public EnemyAI[] enemiesLastHit;

	private int enemyIndex;

	private void Start()
	{
		enemiesLastHit = new EnemyAI[3];
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!mainScript.hasBeenSpawned || (mainScript.magnetedToShip && mainScript.magnetTime > 0.8f))
		{
			return;
		}
		if (other.gameObject.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component == null || mainScript.localPlayerInPassengerSeat || Time.realtimeSinceStartup - timeSinceHittingPlayer < 0.25f)
			{
				return;
			}
			float num = mainScript.averageVelocity.magnitude;
			if (num < 2f)
			{
				return;
			}
			Vector3 vector = component.transform.position - mainScript.mainRigidbody.position;
			float num2 = Vector3.Angle(Vector3.Normalize(mainScript.averageVelocity * 1000f), Vector3.Normalize(vector * 1000f));
			if (num2 > 70f)
			{
				return;
			}
			if (num2 < 30f && mainScript.EngineRPM > 400f)
			{
				num += 6f;
			}
			if ((component.gameplayCamera.transform.position - mainScript.mainRigidbody.position).y < -0.1f)
			{
				num *= 2f;
			}
			timeSinceHittingPlayer = Time.realtimeSinceStartup;
			Vector3 vector2 = Vector3.ClampMagnitude(mainScript.averageVelocity, 40f);
			if (component == GameNetworkManager.Instance.localPlayerController)
			{
				if (mainScript.physicsRegion.physicsTransform == GameNetworkManager.Instance.localPlayerController.physicsParent)
				{
					return;
				}
				if (num > 20f)
				{
					GameNetworkManager.Instance.localPlayerController.KillPlayer(vector2, spawnBody: true, CauseOfDeath.Crushing);
				}
				else
				{
					int num3 = 0;
					if (num > 15f)
					{
						num3 = 80;
					}
					else if (num > 12f)
					{
						num3 = 60;
					}
					else if (num > 8f)
					{
						num3 = 40;
					}
					if (num3 > 0)
					{
						GameNetworkManager.Instance.localPlayerController.DamagePlayer(num3, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing, 0, fallDamage: false, vector2);
					}
				}
				if (!GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.externalForceAutoFade.sqrMagnitude < mainScript.averageVelocity.sqrMagnitude)
				{
					GameNetworkManager.Instance.localPlayerController.externalForceAutoFade = mainScript.averageVelocity;
				}
			}
			else if (mainScript.IsOwner && mainScript.averageVelocity.magnitude > 1.8f)
			{
				mainScript.CarReactToObstacle(mainScript.averageVelocity, component.transform.position, mainScript.averageVelocity, CarObstacleType.Player);
			}
		}
		else
		{
			if (!other.gameObject.CompareTag("Enemy"))
			{
				return;
			}
			Debug.Log("Truck got collision from enemy; " + other.gameObject.name);
			if (Time.realtimeSinceStartup - timeSinceHittingEnemy < 0.25f)
			{
				return;
			}
			EnemyAICollisionDetect component2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
			Debug.Log($"Truck collision: is enemyCollisoinScript null? : {component2 == null}");
			if (component2 == null || component2.mainScript == null || component2.mainScript.isEnemyDead)
			{
				Debug.Log($"Truck collision: {component2 == null} || {component2.mainScript == null} || {component2.mainScript.isEnemyDead}");
				return;
			}
			if (Vector3.Angle(mainScript.averageVelocity, component2.mainScript.transform.position - base.transform.position) > 130f)
			{
				Debug.Log($"Angle/vel check did not pass; {Vector3.Angle(mainScript.averageVelocity, component2.mainScript.transform.position - base.transform.position)}");
				return;
			}
			if (mainScript.backDoorOpen && mainScript.averageVelocity.magnitude < 2f && (insideTruckNavMeshBounds.ClosestPoint(component2.mainScript.transform.position) == component2.mainScript.transform.position || insideTruckNavMeshBounds.ClosestPoint(component2.mainScript.agent.destination) == component2.mainScript.agent.destination))
			{
				Debug.Log("Truck collision: Enemy colliding with truck is inside the back of the truck; return");
				return;
			}
			bool dealDamage = false;
			for (int i = 0; i < enemiesLastHit.Length; i++)
			{
				if (enemiesLastHit[i] == component2.mainScript)
				{
					if (Time.realtimeSinceStartup - timeSinceHittingEnemy < 0.6f)
					{
						dealDamage = true;
					}
					if (mainScript.averageVelocity.magnitude < 4f)
					{
						dealDamage = true;
					}
				}
			}
			timeSinceHittingEnemy = Time.realtimeSinceStartup;
			bool flag = false;
			Vector3 position = component2.transform.position;
			switch (component2.mainScript.enemyType.EnemySize)
			{
			case EnemySize.Tiny:
				flag = mainScript.CarReactToObstacle(mainScript.averageVelocity, position, mainScript.averageVelocity, CarObstacleType.Enemy, 1f, component2.mainScript, dealDamage);
				break;
			case EnemySize.Giant:
				flag = mainScript.CarReactToObstacle(mainScript.averageVelocity, position, mainScript.averageVelocity, CarObstacleType.Enemy, 3f, component2.mainScript, dealDamage);
				break;
			case EnemySize.Medium:
				flag = mainScript.CarReactToObstacle(mainScript.averageVelocity, position, mainScript.averageVelocity, CarObstacleType.Enemy, 2f, component2.mainScript, dealDamage);
				break;
			}
			if (flag)
			{
				enemyIndex = (enemyIndex + 1) % 3;
				enemiesLastHit[enemyIndex] = component2.mainScript;
				return;
			}
			for (int j = 0; j < enemiesLastHit.Length; j++)
			{
				if (enemiesLastHit[j] == component2.mainScript)
				{
					enemiesLastHit[j] = null;
				}
			}
		}
	}
}
