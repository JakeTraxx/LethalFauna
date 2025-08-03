using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class NutcrackerEnemyAI : EnemyAI
{
	private int previousBehaviourState = -1;

	private int previousBehaviourStateAIInterval = -1;

	public static float timeAtNextInspection;

	private bool inspectingLocalPlayer;

	private float localPlayerTurnDistance;

	private bool isInspecting;

	private bool hasGun;

	private int randomSeedNumber;

	public GameObject gunPrefab;

	public ShotgunItem gun;

	public Transform gunPoint;

	private NetworkObjectReference gunObjectRef;

	public AISearchRoutine patrol;

	public AISearchRoutine attackSearch;

	public Transform torsoContainer;

	public float currentTorsoRotation;

	public int targetTorsoDegrees;

	public float torsoTurnSpeed = 2f;

	public AudioSource torsoTurnAudio;

	public AudioSource longRangeAudio;

	public AudioClip[] torsoFinishTurningClips;

	public AudioClip aimSFX;

	public AudioClip kickSFX;

	public GameObject shotgunShellPrefab;

	private bool torsoTurning;

	private System.Random NutcrackerRandom;

	private int timesDoingInspection;

	private Coroutine inspectionCoroutine;

	public int lastPlayerSeenMoving = -1;

	private float timeSinceSeeingTarget;

	private float timeSinceInspecting;

	private float timeSinceFiringGun;

	private bool aimingGun;

	private bool reloadingGun;

	private Vector3 lastSeenPlayerPos;

	private RaycastHit rayHit;

	private Coroutine gunCoroutine;

	private bool isLeaderScript;

	private Vector3 positionLastCheck;

	private Vector3 strafePosition;

	private bool reachedStrafePosition;

	private bool lostPlayerInChase;

	private float timeSinceHittingPlayer;

	private Coroutine waitToFireGunCoroutine;

	private float walkCheckInterval;

	private int timesSeeingSamePlayer;

	private int previousPlayerSeenWhenAiming = -1;

	private float speedWhileAiming;

	public override void Start()
	{
		base.Start();
		if (base.IsServer)
		{
			InitializeNutcrackerValuesServerRpc();
			if (enemyType.numberSpawned <= 1)
			{
				isLeaderScript = true;
			}
		}
		rayHit = default(RaycastHit);
	}

	[ServerRpc]
	public void InitializeNutcrackerValuesServerRpc()
{		{
			GameObject gameObject = UnityEngine.Object.Instantiate(gunPrefab, base.transform.position + Vector3.up * 0.5f, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
			gameObject.GetComponent<NetworkObject>().Spawn();
			GrabGun(gameObject);
			randomSeedNumber = UnityEngine.Random.Range(0, 10000);
			InitializeNutcrackerValuesClientRpc(randomSeedNumber, gameObject.GetComponent<NetworkObject>());
		}
}
	[ClientRpc]
	public void InitializeNutcrackerValuesClientRpc(int randomSeed, NetworkObjectReference gunObject)
			{
				randomSeedNumber = randomSeed;
				gunObjectRef = gunObject;
			}

	private void GrabGun(GameObject gunObject)
	{
		gun = gunObject.GetComponent<ShotgunItem>();
		if (gun == null)
		{
			LogEnemyError("Gun in GrabGun function did not contain ShotgunItem component.");
			return;
		}
		Debug.Log("Setting gun scrap value");
		gun.SetScrapValue(60);
		RoundManager.Instance.totalScrapValueInLevel += gun.scrapValue;
		gun.parentObject = gunPoint;
		gun.isHeldByEnemy = true;
		gun.grabbableToEnemies = false;
		gun.grabbable = false;
		gun.shellsLoaded = 2;
		gun.GrabItemFromEnemy(this);
	}

	private void DropGun(Vector3 dropPosition)
	{
		if (gun == null)
		{
			LogEnemyError("Could not drop gun since no gun was held!");
			return;
		}
		gun.DiscardItemFromEnemy();
		gun.isHeldByEnemy = false;
		gun.grabbableToEnemies = true;
		gun.grabbable = true;
	}

	private void SpawnShotgunShells()
	{
		if (base.IsOwner)
		{
			for (int i = 0; i < 2; i++)
			{
				Vector3 position = base.transform.position + Vector3.up * 0.6f;
				position += new Vector3(UnityEngine.Random.Range(-0.8f, 0.8f), 0f, UnityEngine.Random.Range(-0.8f, 0.8f));
				GameObject obj = UnityEngine.Object.Instantiate(shotgunShellPrefab, position, Quaternion.identity, RoundManager.Instance.spawnedScrapContainer);
				obj.GetComponent<GrabbableObject>().fallTime = 0f;
				obj.GetComponent<NetworkObject>().Spawn();
			}
		}
	}

	[ServerRpc]
	public void DropGunServerRpc(Vector3 dropPosition)
{		{
			DropGunClientRpc(dropPosition);
		}
}
	[ClientRpc]
	public void DropGunClientRpc(Vector3 dropPosition)
{if(!(gun == null))			{
				DropGun(dropPosition);
			}
}
	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead || stunNormalizedTimer > 0f || gun == null)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousBehaviourStateAIInterval != currentBehaviourStateIndex)
			{
				previousBehaviourStateAIInterval = currentBehaviourStateIndex;
				agent.stoppingDistance = 0.02f;
			}
			if (!patrol.inProgress)
			{
				StartSearch(base.transform.position, patrol);
			}
			break;
		case 1:
			if (previousBehaviourStateAIInterval != currentBehaviourStateIndex)
			{
				previousBehaviourStateAIInterval = currentBehaviourStateIndex;
				if (patrol.inProgress)
				{
					StopSearch(patrol);
				}
			}
			break;
		case 2:
			if (previousBehaviourStateAIInterval != currentBehaviourStateIndex)
			{
				previousBehaviourStateAIInterval = currentBehaviourStateIndex;
				if (patrol.inProgress)
				{
					StopSearch(patrol);
				}
			}
			if (!base.IsOwner)
			{
				break;
			}
			if (timeSinceSeeingTarget < 0.5f)
			{
				if (attackSearch.inProgress)
				{
					StopSearch(attackSearch);
				}
				reachedStrafePosition = false;
				SetDestinationToPosition(lastSeenPlayerPos);
				agent.stoppingDistance = 1f;
				if (lostPlayerInChase)
				{
					lostPlayerInChase = false;
					SetLostPlayerInChaseServerRpc(lostPlayer: false);
				}
				break;
			}
			agent.stoppingDistance = 0.02f;
			if (timeSinceSeeingTarget > 12f)
			{
				if (!reloadingGun && timeSinceFiringGun > 0.5f)
				{
					SwitchToBehaviourState(1);
				}
			}
			else if (!reachedStrafePosition)
			{
				if (!agent.CalculatePath(lastSeenPlayerPos, path1))
				{
					break;
				}
				if (DebugEnemy)
				{
					for (int i = 1; i < path1.corners.Length; i++)
					{
						Debug.DrawLine(path1.corners[i - 1], path1.corners[i], Color.red, AIIntervalTime);
					}
				}
				if (path1.corners.Length > 1)
				{
					Ray ray = new Ray(path1.corners[path1.corners.Length - 1], path1.corners[path1.corners.Length - 1] - path1.corners[path1.corners.Length - 2]);
					if (Physics.Raycast(ray, out rayHit, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
					{
						strafePosition = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(Mathf.Max(0f, rayHit.distance - 2f)));
					}
					else
					{
						strafePosition = RoundManager.Instance.GetNavMeshPosition(ray.GetPoint(6f));
					}
				}
				else
				{
					strafePosition = lastSeenPlayerPos;
				}
				SetDestinationToPosition(strafePosition);
				if (Vector3.Distance(base.transform.position, strafePosition) < 2f)
				{
					reachedStrafePosition = true;
				}
			}
			else
			{
				if (!lostPlayerInChase)
				{
					lostPlayerInChase = true;
					SetLostPlayerInChaseServerRpc(lostPlayer: true);
				}
				if (!attackSearch.inProgress)
				{
					StartSearch(strafePosition, attackSearch);
				}
			}
			break;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetLostPlayerInChaseServerRpc(bool lostPlayer)
			{
				SetLostPlayerInChaseClientRpc(lostPlayer);
			}

	[ClientRpc]
	public void SetLostPlayerInChaseClientRpc(bool lostPlayer)
{if(!base.IsOwner)		{
			lostPlayerInChase = lostPlayer;
			if (!lostPlayer)
			{
				timeSinceSeeingTarget = 0f;
			}
		}
}
	private bool GrabGunIfNotHolding()
	{
		if (gun != null)
		{
			return true;
		}
		if (gunObjectRef.TryGet(out var networkObject))
		{
			gun = networkObject.gameObject.GetComponent<ShotgunItem>();
			GrabGun(gun.gameObject);
		}
		return gun != null;
	}

	public void TurnTorsoToTargetDegrees()
	{
		currentTorsoRotation = Mathf.MoveTowardsAngle(currentTorsoRotation, targetTorsoDegrees, Time.deltaTime * torsoTurnSpeed);
		torsoContainer.localEulerAngles = new Vector3(currentTorsoRotation + 90f, 90f, 90f);
		if (Mathf.Abs(currentTorsoRotation - (float)targetTorsoDegrees) > 5f)
		{
			if (!torsoTurning)
			{
				torsoTurning = true;
				torsoTurnAudio.Play();
			}
		}
		else if (torsoTurning)
		{
			torsoTurning = false;
			torsoTurnAudio.Stop();
			RoundManager.PlayRandomClip(torsoTurnAudio, torsoFinishTurningClips);
		}
		torsoTurnAudio.volume = Mathf.Lerp(torsoTurnAudio.volume, 1f, Time.deltaTime * 2f);
	}

	private void SetTargetDegreesToPosition(Vector3 pos)
	{
		pos.y = base.transform.position.y;
		Vector3 vector = pos - base.transform.position;
		targetTorsoDegrees = (int)Vector3.Angle(vector, base.transform.forward);
		if (Vector3.Cross(base.transform.forward, vector).y > 0f)
		{
			targetTorsoDegrees = 360 - targetTorsoDegrees;
		}
		torsoTurnSpeed = 455f;
	}

	private void StartInspectionTurn()
	{
		if (!isInspecting && !isEnemyDead)
		{
			timesDoingInspection++;
			if (inspectionCoroutine != null)
			{
				StopCoroutine(inspectionCoroutine);
			}
			inspectionCoroutine = StartCoroutine(InspectionTurn());
		}
	}

	private IEnumerator InspectionTurn()
	{
		yield return new WaitForSeconds(0.75f);
		isInspecting = true;
		NutcrackerRandom = new System.Random(randomSeedNumber + timesDoingInspection);
		int degrees = 0;
		int turnTime = 1;
		for (int i = 0; i < 8; i++)
		{
			degrees = Mathf.Min(degrees + NutcrackerRandom.Next(45, 95), 360);
			if (Physics.Raycast(eye.position, eye.forward, 5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				turnTime = 1;
			}
			else
			{
				int a = ((!((float)turnTime > 2f)) ? 4 : (turnTime / 3));
				turnTime = NutcrackerRandom.Next(1, Mathf.Max(a, 3));
			}
			targetTorsoDegrees = degrees;
			torsoTurnSpeed = NutcrackerRandom.Next(275, 855) / turnTime;
			yield return new WaitForSeconds(turnTime);
			if (degrees >= 360)
			{
				break;
			}
		}
		if (base.IsOwner)
		{
			SwitchToBehaviourState(0);
		}
	}

	public void StopInspection()
	{
		if (isInspecting)
		{
			isInspecting = false;
		}
		if (inspectionCoroutine != null)
		{
			StopCoroutine(inspectionCoroutine);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SeeMovingThreatServerRpc(int playerId, bool enterAttackFromPatrolMode = false)
			{
				SeeMovingThreatClientRpc(playerId, enterAttackFromPatrolMode);
			}

	[ClientRpc]
	public void SeeMovingThreatClientRpc(int playerId, bool enterAttackFromPatrolMode = false)
{if((currentBehaviourStateIndex == 1 || (enterAttackFromPatrolMode && currentBehaviourStateIndex == 0)))			{
				SwitchTargetToPlayer(playerId);
				SwitchToBehaviourStateOnLocalClient(2);
			}
}
	private void GlobalNutcrackerClock()
	{
		if (isLeaderScript && Time.realtimeSinceStartup - timeAtNextInspection > 2f)
		{
			timeAtNextInspection = Time.realtimeSinceStartup + UnityEngine.Random.Range(6f, 15f);
		}
	}

	public override void Update()
	{
		base.Update();
		TurnTorsoToTargetDegrees();
		if (isEnemyDead)
		{
			StopInspection();
			return;
		}
		GlobalNutcrackerClock();
		if (!isEnemyDead && !GrabGunIfNotHolding())
		{
			return;
		}
		if (walkCheckInterval <= 0f)
		{
			walkCheckInterval = 0.1f;
			creatureAnimator.SetBool("IsWalking", (base.transform.position - positionLastCheck).sqrMagnitude > 0.001f);
			positionLastCheck = base.transform.position;
		}
		else
		{
			walkCheckInterval -= Time.deltaTime;
		}
		if (stunNormalizedTimer >= 0f)
		{
			agent.speed = 0f;
			return;
		}
		timeSinceSeeingTarget += Time.deltaTime;
		timeSinceInspecting += Time.deltaTime;
		timeSinceFiringGun += Time.deltaTime;
		timeSinceHittingPlayer += Time.deltaTime;
		creatureAnimator.SetInteger("State", currentBehaviourStateIndex);
		creatureAnimator.SetBool("Aiming", aimingGun);
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				creatureAnimator.SetBool("AimDown", value: false);
				isInspecting = false;
				lostPlayerInChase = false;
				creatureVoice.Stop();
				previousBehaviourState = currentBehaviourStateIndex;
			}
			agent.speed = 5.5f;
			targetTorsoDegrees = 0;
			torsoTurnSpeed = 525f;
			if (base.IsOwner && Time.realtimeSinceStartup > timeAtNextInspection && timeSinceInspecting > 4f)
			{
				if (UnityEngine.Random.Range(0, 100) < 40 || (GetClosestPlayer() != null && mostOptimalDistance < 27f))
				{
					SwitchToBehaviourState(1);
				}
				else
				{
					timeSinceInspecting = 2f;
				}
			}
			break;
		case 1:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				creatureAnimator.SetBool("AimDown", value: false);
				localPlayerTurnDistance = 0f;
				StartInspectionTurn();
				creatureVoice.Stop();
				if (previousBehaviourState != 2)
				{
					longRangeAudio.PlayOneShot(enemyType.audioClips[3]);
				}
				lostPlayerInChase = false;
				previousBehaviourState = currentBehaviourStateIndex;
			}
			timeSinceInspecting = 0f;
			agent.speed = 0f;
			if (isInspecting && CheckLineOfSightForLocalPlayer(70f, 60, 1) && IsLocalPlayerMoving())
			{
				isInspecting = false;
				SeeMovingThreatServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
			break;
		case 2:
			if (previousBehaviourState != currentBehaviourStateIndex)
			{
				if (previousBehaviourState != 1)
				{
					longRangeAudio.PlayOneShot(enemyType.audioClips[3]);
				}
				StopInspection();
				previousBehaviourState = currentBehaviourStateIndex;
			}
			if (base.IsOwner)
			{
				if (reloadingGun || aimingGun || (timeSinceFiringGun < 1.2f && timeSinceSeeingTarget < 0.5f) || timeSinceHittingPlayer < 1f)
				{
					if (aimingGun && !reloadingGun)
					{
						agent.speed = speedWhileAiming;
					}
					else
					{
						agent.speed = 0f;
					}
				}
				else
				{
					agent.speed = 7f;
				}
			}
			if (base.IsOwner && timeSinceFiringGun > 0.75f && gun.shellsLoaded <= 0 && !reloadingGun && !aimingGun)
			{
				reloadingGun = true;
				ReloadGunServerRpc();
			}
			if (lastPlayerSeenMoving == -1)
			{
				break;
			}
			if (lostPlayerInChase)
			{
				targetTorsoDegrees = 0;
			}
			else
			{
				SetTargetDegreesToPosition(lastSeenPlayerPos);
			}
			if (CheckLineOfSightForPosition(StartOfRound.Instance.allPlayerScripts[lastPlayerSeenMoving].gameplayCamera.transform.position, 70f, 60, 1f))
			{
				timeSinceSeeingTarget = 0f;
				lastSeenPlayerPos = StartOfRound.Instance.allPlayerScripts[lastPlayerSeenMoving].transform.position;
				creatureAnimator.SetBool("AimDown", Vector3.Distance(lastSeenPlayerPos, base.transform.position) < 2f && lastSeenPlayerPos.y - base.transform.position.y < 1f);
			}
			if (!CheckLineOfSightForLocalPlayer(70f, 25, 1))
			{
				break;
			}
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId == lastPlayerSeenMoving && timeSinceSeeingTarget < 8f)
			{
				if (timeSinceFiringGun > 0.75f && !reloadingGun && !aimingGun && timeSinceHittingPlayer > 1f && Vector3.Angle(gun.shotgunRayPoint.forward, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position - gun.shotgunRayPoint.position) < 30f)
				{
					timeSinceFiringGun = 0f;
					agent.speed = 0f;
					AimGunServerRpc(base.transform.position);
				}
				if (lostPlayerInChase)
				{
					lostPlayerInChase = false;
					SetLostPlayerInChaseServerRpc(lostPlayer: false);
				}
				timeSinceSeeingTarget = 0f;
				lastSeenPlayerPos = GameNetworkManager.Instance.localPlayerController.transform.position;
			}
			else if (IsLocalPlayerMoving())
			{
				bool flag = (int)GameNetworkManager.Instance.localPlayerController.playerClientId == lastPlayerSeenMoving;
				if (flag)
				{
					timeSinceSeeingTarget = 0f;
				}
				if (Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[lastPlayerSeenMoving].transform.position) - Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) > 3f || (timeSinceSeeingTarget > 3f && !flag))
				{
					lastPlayerSeenMoving = (int)GameNetworkManager.Instance.localPlayerController.playerClientId;
					SwitchTargetServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
				}
			}
			break;
		}
	}

	[ServerRpc]
	public void ReloadGunServerRpc()
{		{
			if (aimingGun)
			{
				reloadingGun = false;
			}
			else
			{
				ReloadGunClientRpc();
			}
		}
}
	[ClientRpc]
	public void ReloadGunClientRpc()
			{
				StopAimingGun();
				gun.shellsLoaded = 2;
				gunCoroutine = StartCoroutine(ReloadGun());
			}

	private IEnumerator ReloadGun()
	{
		reloadingGun = true;
		creatureSFX.PlayOneShot(enemyType.audioClips[2]);
		creatureAnimator.SetBool("Reloading", value: true);
		yield return new WaitForSeconds(0.32f);
		gun.gunAnimator.SetBool("Reloading", value: true);
		yield return new WaitForSeconds(0.92f);
		gun.gunAnimator.SetBool("Reloading", value: false);
		creatureAnimator.SetBool("Reloading", value: false);
		yield return new WaitForSeconds(0.5f);
		reloadingGun = false;
	}

	private void StopReloading()
	{
		reloadingGun = false;
		gun.gunAnimator.SetBool("Reloading", value: false);
		creatureAnimator.SetBool("Reloading", value: false);
		if (gunCoroutine != null)
		{
			StopCoroutine(gunCoroutine);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void AimGunServerRpc(Vector3 enemyPos)
{if(!reloadingGun)		{
			if (gun.shellsLoaded <= 0)
			{
				aimingGun = false;
				ReloadGunClientRpc();
			}
			else if (!reloadingGun)
			{
				aimingGun = true;
				AimGunClientRpc(enemyPos);
			}
		}
}
	[ClientRpc]
	public void AimGunClientRpc(Vector3 enemyPos)
			{
				StopReloading();
				gunCoroutine = StartCoroutine(AimGun(enemyPos));
			}

	private IEnumerator AimGun(Vector3 enemyPos)
	{
		aimingGun = true;
		if (lastPlayerSeenMoving == previousPlayerSeenWhenAiming)
		{
			timesSeeingSamePlayer++;
		}
		else
		{
			previousPlayerSeenWhenAiming = lastPlayerSeenMoving;
			timesSeeingSamePlayer = 1;
		}
		longRangeAudio.PlayOneShot(aimSFX);
		if (timesSeeingSamePlayer >= 3)
		{
			speedWhileAiming = 2.25f;
		}
		else
		{
			speedWhileAiming = 0f;
		}
		updatePositionThreshold = 0.45f;
		serverPosition = enemyPos;
		if (enemyHP <= 1)
		{
			yield return new WaitForSeconds(0.5f);
		}
		else if (gun.shellsLoaded == 1)
		{
			yield return new WaitForSeconds(1.3f);
		}
		else
		{
			yield return new WaitForSeconds(1.75f);
		}
		yield return new WaitForEndOfFrame();
		if (base.IsOwner)
		{
			FireGunServerRpc();
		}
		timeSinceFiringGun = 0f;
		yield return new WaitForSeconds(0.35f);
		aimingGun = false;
		inSpecialAnimation = false;
		updatePositionThreshold = 1f;
		creatureVoice.Play();
		creatureVoice.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
	}

	[ServerRpc]
	public void FireGunServerRpc()
{		{
			if (stunNormalizedTimer <= 0f)
			{
				FireGunClientRpc();
			}
			else
			{
				StartCoroutine(waitToFireGun());
			}
		}
}
	[ClientRpc]
	public void FireGunClientRpc()
			{
				FireGun(gun.shotgunRayPoint.position, gun.shotgunRayPoint.forward);
			}

	private IEnumerator waitToFireGun()
	{
		yield return new WaitUntil(() => stunNormalizedTimer <= 0f);
		yield return new WaitForSeconds(0.5f);
		FireGunClientRpc();
	}

	private void StopAimingGun()
	{
		inSpecialAnimation = false;
		updatePositionThreshold = 1f;
		aimingGun = false;
		if (gunCoroutine != null)
		{
			StopCoroutine(gunCoroutine);
		}
	}

	private void FireGun(Vector3 gunPosition, Vector3 gunForward)
	{
		creatureAnimator.ResetTrigger("ShootGun");
		creatureAnimator.SetTrigger("ShootGun");
		if (gun == null)
		{
			LogEnemyError("No gun held on local client, unable to shoot");
		}
		else
		{
			gun.ShootGun(gunPosition, gunForward);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SwitchTargetServerRpc(int playerId)
			{
				SwitchTargetClientRpc(playerId);
			}

	[ClientRpc]
	public void SwitchTargetClientRpc(int playerId)
			{
				SwitchTargetToPlayer(playerId);
			}

	private void SwitchTargetToPlayer(int playerId)
	{
		lastPlayerSeenMoving = playerId;
		timeSinceSeeingTarget = 0f;
		lastSeenPlayerPos = StartOfRound.Instance.allPlayerScripts[playerId].transform.position;
	}

	public bool CheckLineOfSightForLocalPlayer(float width = 45f, int range = 60, int proximityAwareness = -1)
	{
		Vector3 position = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position;
		if (Vector3.Distance(position, eye.position) < (float)range && !Physics.Linecast(eye.position, position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
		{
			Vector3 to = position - eye.position;
			if (Vector3.Angle(eye.forward, to) < width || (proximityAwareness != -1 && Vector3.Distance(eye.position, position) < (float)proximityAwareness))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsLocalPlayerMoving()
	{
		localPlayerTurnDistance += StartOfRound.Instance.playerLookMagnitudeThisFrame;
		if (localPlayerTurnDistance > 0.1f && Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) < 10f)
		{
			return true;
		}
		if (GameNetworkManager.Instance.localPlayerController.performingEmote)
		{
			return true;
		}
		if (Time.realtimeSinceStartup - StartOfRound.Instance.timeAtMakingLastPersonalMovement < 0.25f)
		{
			return true;
		}
		if (GameNetworkManager.Instance.localPlayerController.timeSincePlayerMoving < 0.05f)
		{
			return true;
		}
		return false;
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!isEnemyDead && !(timeSinceHittingPlayer < 1f) && !(stunNormalizedTimer >= 0f))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, reloadingGun || aimingGun);
			if (playerControllerB != null)
			{
				timeSinceHittingPlayer = 0f;
				LegKickPlayerServerRpc((int)playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void LegKickPlayerServerRpc(int playerId)
			{
				LegKickPlayerClientRpc(playerId);
			}

	[ClientRpc]
	public void LegKickPlayerClientRpc(int playerId)
			{
				LegKickPlayer(playerId);
			}

	private void LegKickPlayer(int playerId)
	{
		timeSinceHittingPlayer = 0f;
		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerId];
		RoundManager.Instance.tempTransform.position = base.transform.position;
		RoundManager.Instance.tempTransform.LookAt(playerControllerB.transform.position);
		base.transform.eulerAngles = new Vector3(0f, RoundManager.Instance.tempTransform.eulerAngles.y, 0f);
		serverRotation = new Vector3(0f, RoundManager.Instance.tempTransform.eulerAngles.y, 0f);
		Vector3 bodyVelocity = Vector3.Normalize((playerControllerB.transform.position + Vector3.up * 0.75f - base.transform.position) * 100f) * 25f;
		playerControllerB.KillPlayer(bodyVelocity, spawnBody: true, CauseOfDeath.Kicking);
		creatureAnimator.SetTrigger("Kick");
		creatureSFX.Stop();
		torsoTurnAudio.volume = 0f;
		creatureSFX.PlayOneShot(kickSFX);
		if (currentBehaviourStateIndex != 2)
		{
			SwitchTargetToPlayer(playerId);
			SwitchToBehaviourStateOnLocalClient(2);
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (!isEnemyDead)
		{
			if (isInspecting || currentBehaviourStateIndex == 2)
			{
				creatureSFX.PlayOneShot(enemyType.audioClips[0]);
				enemyHP -= force;
			}
			else
			{
				creatureSFX.PlayOneShot(enemyType.audioClips[1]);
			}
			if (playerWhoHit != null)
			{
				SeeMovingThreatServerRpc((int)playerWhoHit.playerClientId, enterAttackFromPatrolMode: true);
			}
			if (enemyHP <= 0 && base.IsOwner)
			{
				KillEnemyOnOwnerClient();
			}
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		targetTorsoDegrees = 0;
		StopInspection();
		StopReloading();
		if (base.IsOwner)
		{
			DropGunServerRpc(gunPoint.position);
			StartCoroutine(spawnShotgunShellsOnDelay());
		}
		creatureVoice.Stop();
		torsoTurnAudio.Stop();
		creatureSFX.Stop();
	}

	private IEnumerator spawnShotgunShellsOnDelay()
	{
		yield return new WaitForSeconds(1.2f);
		SpawnShotgunShells();
	}
}
