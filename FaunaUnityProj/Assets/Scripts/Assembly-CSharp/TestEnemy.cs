using GameNetcodeStuff;
using UnityEngine;

public class TestEnemy : EnemyAI
{
	public float detectionRadius = 12f;

	private Collider[] allPlayerColliders = new Collider[4];

	private float closestPlayerDist;

	private Collider tempTargetCollider;

	public bool detectingPlayers;

	private bool tempDebug;

	public override void Start()
	{
		base.Start();
		movingTowardsTargetPlayer = true;
	}

	public override void DoAIInterval()
	{
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, detectionRadius, allPlayerColliders, StartOfRound.Instance.playersMask);
		if (num > 0)
		{
			detectingPlayers = true;
			closestPlayerDist = 255555f;
			for (int i = 0; i < num; i++)
			{
				float num2 = Vector3.Distance(base.transform.position, allPlayerColliders[i].transform.position);
				if (num2 < closestPlayerDist)
				{
					closestPlayerDist = num2;
					tempTargetCollider = allPlayerColliders[i];
				}
			}
			SetMovingTowardsTargetPlayer(tempTargetCollider.gameObject.GetComponent<PlayerControllerB>());
		}
		else
		{
			agent.speed = 5f;
			detectingPlayers = false;
		}
		base.DoAIInterval();
	}

	public override void Update()
	{
		if (base.IsOwner && detectingPlayers)
		{
			agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime / 3f, 0f, 12f);
		}
		base.Update();
	}
}
