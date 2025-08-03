using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class LassoManAI : EnemyAI
{
	public AISearchRoutine searchForPlayers;

	private float checkLineOfSightInterval;

	public float maxSearchAndRoamRadius = 100f;

	[Space(5f)]
	public float noticePlayerTimer;

	private bool hasEnteredChaseMode;

	private bool lostPlayerInChase;

	private bool beginningChasingThisClient;

	private float timeSinceHittingPlayer;

	public DeadBodyInfo currentlyHeldBody;

	public override void Start()
	{
		base.Start();
		searchForPlayers.searchWidth = maxSearchAndRoamRadius;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (!searchForPlayers.inProgress)
			{
				StartSearch(base.transform.position, searchForPlayers);
				Debug.Log($"Crawler: Started new search; is searching?: {searchForPlayers.inProgress}");
			}
			break;
		case 1:
			if (lostPlayerInChase)
			{
				if (!searchForPlayers.inProgress)
				{
					searchForPlayers.searchWidth = 30f;
					StartSearch(targetPlayer.transform.position, searchForPlayers);
					Debug.Log("Crawler: Lost player in chase; beginning search where the player was last seen");
				}
			}
			else if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
				movingTowardsTargetPlayer = true;
				Debug.Log("Crawler: Found player during chase; stopping search coroutine and moving after target player");
			}
			break;
		}
	}

	public override void FinishedCurrentSearchRoutine()
	{
		base.FinishedCurrentSearchRoutine();
		searchForPlayers.searchWidth = Mathf.Clamp(searchForPlayers.searchWidth + 20f, 1f, maxSearchAndRoamRadius);
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			return;
		}
		if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 60f, 8, 5f))
		{
			if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) < 7f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);
			}
			else
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.5f, 0.6f);
			}
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			if (hasEnteredChaseMode)
			{
				hasEnteredChaseMode = false;
				beginningChasingThisClient = false;
				noticePlayerTimer = 0f;
				useSecondaryAudiosOnAnimatedObjects = false;
				openDoorSpeedMultiplier = 0.6f;
				agent.speed = 5f;
			}
			if (checkLineOfSightInterval <= 0.05f)
			{
				checkLineOfSightInterval += Time.deltaTime;
				break;
			}
			checkLineOfSightInterval = 0f;
			PlayerControllerB playerControllerB2;
			if (stunnedByPlayer != null)
			{
				playerControllerB2 = stunnedByPlayer;
				noticePlayerTimer = 1f;
			}
			else
			{
				playerControllerB2 = CheckLineOfSightForPlayer(55f);
			}
			if (playerControllerB2 == GameNetworkManager.Instance.localPlayerController)
			{
				Debug.Log($"Seeing player; {noticePlayerTimer}");
				noticePlayerTimer = Mathf.Clamp(noticePlayerTimer + 0.05f, 0f, 10f);
				if (noticePlayerTimer > 0.1f && !beginningChasingThisClient)
				{
					beginningChasingThisClient = true;
					BeginChasingPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
					Debug.Log("Begin chasing local client");
				}
			}
			else
			{
				noticePlayerTimer -= Time.deltaTime;
			}
			break;
		}
		case 1:
		{
			if (!hasEnteredChaseMode)
			{
				hasEnteredChaseMode = true;
				lostPlayerInChase = false;
				checkLineOfSightInterval = 0f;
				noticePlayerTimer = 0f;
				beginningChasingThisClient = false;
				useSecondaryAudiosOnAnimatedObjects = true;
				openDoorSpeedMultiplier = 1.5f;
				agent.speed = 6f;
			}
			if (!base.IsOwner || stunNormalizedTimer > 0f)
			{
				break;
			}
			if (checkLineOfSightInterval <= 0.075f)
			{
				checkLineOfSightInterval += Time.deltaTime;
				break;
			}
			checkLineOfSightInterval = 0f;
			if (lostPlayerInChase)
			{
				if ((bool)CheckLineOfSightForPlayer(55f))
				{
					noticePlayerTimer = 0f;
					lostPlayerInChase = false;
					MakeScreechNoiseServerRpc();
					break;
				}
				noticePlayerTimer -= 0.075f;
				if (noticePlayerTimer < -15f)
				{
					SwitchToBehaviourState(0);
				}
				break;
			}
			PlayerControllerB playerControllerB = CheckLineOfSightForPlayer(55f);
			if (playerControllerB != null)
			{
				Debug.Log("Seeing player!!!!");
				noticePlayerTimer = 0f;
				if (playerControllerB != targetPlayer)
				{
					targetPlayer = playerControllerB;
				}
			}
			else
			{
				noticePlayerTimer += 0.075f;
				if (noticePlayerTimer > 2.5f)
				{
					lostPlayerInChase = true;
				}
			}
			break;
		}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BeginChasingPlayerServerRpc(int playerObjectId)
			{
				BeginChasingPlayerClientRpc(playerObjectId);
			}

	[ClientRpc]
	public void BeginChasingPlayerClientRpc(int playerObjectId)
			{
				SwitchToBehaviourStateOnLocalClient(1);
				SetMovingTowardsTargetPlayer(StartOfRound.Instance.allPlayerScripts[playerObjectId]);
			}

	[ServerRpc]
	public void MakeScreechNoiseServerRpc()
{		{
			MakeScreechNoiseClientRpc();
		}
}
	[ClientRpc]
	public void MakeScreechNoiseClientRpc()
			{
			}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
		if (!isEnemyDead && component != null && component == GameNetworkManager.Instance.localPlayerController && component.inAnimationWithEnemy == null && !component.isPlayerDead && timeSinceHittingPlayer > 0.5f)
		{
			timeSinceHittingPlayer = 0f;
			component.DamagePlayer(40, hasDamageSFX: true, callRPC: true, CauseOfDeath.Strangulation);
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy();
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (!isEnemyDead)
		{
			creatureAnimator.SetTrigger("HurtEnemy");
			enemyHP--;
			if (enemyHP <= 0 && base.IsOwner)
			{
				KillEnemyOnOwnerClient();
			}
		}
	}
}
