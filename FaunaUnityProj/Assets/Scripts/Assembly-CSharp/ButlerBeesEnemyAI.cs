using GameNetcodeStuff;
using UnityEngine;

public class ButlerBeesEnemyAI : EnemyAI
{
	private float timeAtLastHurtingPlayer;

	public AISearchRoutine searchForPlayers;

	private float chasePlayerTimer;

	private float timeAtSpawning;

	public AudioSource buzzing;

	public override void Start()
	{
		base.Start();
		timeAtSpawning = Time.realtimeSinceStartup;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!(Time.realtimeSinceStartup - timeAtSpawning < 1.7f) && !(Time.realtimeSinceStartup - timeAtLastHurtingPlayer < 0.5f))
		{
			timeAtLastHurtingPlayer = Time.realtimeSinceStartup;
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null && playerControllerB == GameNetworkManager.Instance.localPlayerController)
			{
				playerControllerB.DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Stabbing);
			}
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		if (!TargetClosestPlayer(4f, requireLineOfSight: true, 180f))
		{
			if (movingTowardsTargetPlayer)
			{
				buzzing.pitch = Mathf.Lerp(buzzing.pitch, 1.15f, Time.deltaTime * 5f);
				agent.speed = Mathf.Max(agent.speed - AIIntervalTime, 4f);
				chasePlayerTimer += AIIntervalTime;
				if (chasePlayerTimer > 3f)
				{
					movingTowardsTargetPlayer = false;
				}
			}
			else
			{
				if (!searchForPlayers.inProgress)
				{
					StartSearch(base.transform.position, searchForPlayers);
				}
				buzzing.pitch = Mathf.Lerp(buzzing.pitch, 1f, Time.deltaTime * 5f);
				agent.speed = 3f;
			}
		}
		else
		{
			movingTowardsTargetPlayer = true;
			chasePlayerTimer = 0f;
			float b = 5.4f;
			if (StartOfRound.Instance.connectedPlayersAmount == 0)
			{
				b = 4.25f;
			}
			agent.speed = Mathf.Min(agent.speed + AIIntervalTime * 0.75f, b);
			buzzing.pitch = Mathf.Lerp(buzzing.pitch, 1.3f, Time.deltaTime * 5f);
			if (currentOwnershipOnThisClient != (int)targetPlayer.playerClientId)
			{
				ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
			}
		}
	}
}
