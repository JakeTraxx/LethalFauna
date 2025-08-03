using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class MouthDogAI : EnemyAI, INoiseListener, IVisibleThreat
{
	public float noiseApproximation = 14f;

	public int suspicionLevel;

	private Vector3 previousPosition;

	public DampedTransform neckDampedTransform;

	private RoundManager roundManager;

	private float AITimer;

	private List<GameObject> allAINodesWithinRange = new List<GameObject>();

	private bool hasEnteredChaseModeFully;

	private bool startedChaseModeCoroutine;

	public AudioClip screamSFX;

	public AudioClip breathingSFX;

	public AudioClip killPlayerSFX;

	private float hearNoiseCooldown;

	private bool inLunge;

	private float lungeCooldown;

	private bool inKillAnimation;

	public Transform mouthGrip;

	public bool endingLunge;

	private Ray ray;

	private RaycastHit rayHit;

	private Vector3 lastHeardNoisePosition;

	private Vector3 noisePositionGuess;

	private float lastHeardNoiseDistanceWhenHeard;

	private bool heardOtherHowl;

	private DeadBodyInfo carryingBody;

	private System.Random enemyRandom;

	private Coroutine killPlayerCoroutine;

	private const int suspicionThreshold = 5;

	private const int alertThreshold = 9;

	private const int maxSuspicionLevel = 11;

	public AISearchRoutine roamPlanet;

	private Collider debugCollider;

	private float timeSinceHittingOtherEnemy;

	private float coweringMeter;

	private bool coweringOnFloor;

	private bool coweringOnFloorDebounce;

	ThreatType IVisibleThreat.type => ThreatType.EyelessDog;

	bool IVisibleThreat.IsThreatDead()
	{
		return isEnemyDead;
	}

	GrabbableObject IVisibleThreat.GetHeldObject()
	{
		return null;
	}

	int IVisibleThreat.SendSpecialBehaviour(int id)
	{
		return 0;
	}

	int IVisibleThreat.GetThreatLevel(Vector3 seenByPosition)
	{
		int num = 0;
		num = ((enemyHP >= 2) ? 5 : 3);
		if (creatureAnimator.GetBool("StartedChase"))
		{
			num += 3;
		}
		return num;
	}

	int IVisibleThreat.GetInterestLevel()
	{
		return 0;
	}

	Transform IVisibleThreat.GetThreatLookTransform()
	{
		return eye;
	}

	Transform IVisibleThreat.GetThreatTransform()
	{
		return base.transform;
	}

	Vector3 IVisibleThreat.GetThreatVelocity()
	{
		if (base.IsOwner)
		{
			return agent.velocity;
		}
		return Vector3.zero;
	}

	float IVisibleThreat.GetVisibility()
	{
		if (isEnemyDead)
		{
			return 0f;
		}
		if (creatureAnimator.GetBool("StartedChase"))
		{
			return 1f;
		}
		return 0.75f;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		_ = StartOfRound.Instance.livingPlayers;
	}

	public override void Start()
	{
		base.Start();
		roundManager = UnityEngine.Object.FindObjectOfType<RoundManager>();
		useSecondaryAudiosOnAnimatedObjects = true;
		if (UnityEngine.Random.Range(0, 10) < 2)
		{
			creatureVoice.pitch = UnityEngine.Random.Range(0.6f, 1.3f);
		}
		else
		{
			creatureVoice.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
		}
		enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead)
		{
			creatureAnimator.SetLayerWeight(1, 0f);
		}
		else
		{
			if (!ventAnimationFinished)
			{
				return;
			}
			if (stunNormalizedTimer > 0f && !isEnemyDead)
			{
				if (stunnedByPlayer != null && currentBehaviourStateIndex != 2 && base.IsOwner)
				{
					EnrageDogOnLocalClient(stunnedByPlayer.transform.position, Vector3.Distance(base.transform.position, stunnedByPlayer.transform.position));
				}
				creatureAnimator.SetLayerWeight(1, 1f);
			}
			else
			{
				creatureAnimator.SetLayerWeight(1, 0f);
			}
			hitsPhysicsObjects = currentBehaviourStateIndex >= 2;
			if (!coweringOnFloor)
			{
				if (!coweringOnFloorDebounce)
				{
					coweringOnFloorDebounce = true;
					creatureAnimator.SetBool("Cower", value: false);
				}
				if (coweringMeter >= 0f)
				{
					coweringMeter -= Time.deltaTime;
				}
			}
			else
			{
				if (coweringOnFloorDebounce)
				{
					coweringOnFloorDebounce = false;
					creatureAnimator.SetBool("Cower", value: true);
					creatureAnimator.SetTrigger("StartCowering");
				}
				if (coweringMeter < 0.7f)
				{
					coweringMeter += Time.deltaTime;
				}
			}
			hearNoiseCooldown -= Time.deltaTime;
			timeSinceHittingOtherEnemy += Time.deltaTime;
			creatureAnimator.SetFloat("speedMultiplier", Vector3.ClampMagnitude(base.transform.position - previousPosition, 1f).sqrMagnitude / (Time.deltaTime / 4f));
			previousPosition = base.transform.position;
			if (currentBehaviourStateIndex == 2 || currentBehaviourStateIndex == 3)
			{
				if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 50f, 25, 10f))
				{
					GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.4f, 0.5f);
				}
			}
			else if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position, 50f, 30, 5f))
			{
				GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.25f, 0.3f);
			}
			switch (currentBehaviourStateIndex)
			{
			case 0:
				neckDampedTransform.weight = 1f;
				creatureAnimator.SetInteger("BehaviourState", 0);
				if (base.IsOwner)
				{
					agent.speed = 3.5f;
					if (stunNormalizedTimer > 0f || coweringOnFloor)
					{
						agent.speed = 0f;
					}
					if (base.IsOwner && !roamPlanet.inProgress)
					{
						StartSearch(base.transform.position, roamPlanet);
					}
				}
				break;
			case 1:
				if (hasEnteredChaseModeFully)
				{
					hasEnteredChaseModeFully = false;
					creatureVoice.Stop();
					startedChaseModeCoroutine = false;
					creatureAnimator.SetBool("StartedChase", value: false);
				}
				neckDampedTransform.weight = Mathf.Lerp(neckDampedTransform.weight, 1f, 8f * Time.deltaTime);
				creatureAnimator.SetInteger("BehaviourState", 1);
				if (!base.IsOwner)
				{
					break;
				}
				if (base.IsOwner && roamPlanet.inProgress)
				{
					StopSearch(roamPlanet);
				}
				agent.speed = 4.5f;
				if (stunNormalizedTimer > 0f || coweringOnFloor)
				{
					agent.speed = 0f;
				}
				AITimer -= Time.deltaTime;
				if (AITimer <= 0f)
				{
					AITimer = 4f;
					suspicionLevel--;
					if (suspicionLevel <= 1)
					{
						SwitchToBehaviourState(0);
					}
				}
				break;
			case 2:
				if (!hasEnteredChaseModeFully)
				{
					if (!startedChaseModeCoroutine)
					{
						startedChaseModeCoroutine = true;
						StartCoroutine(enterChaseMode());
					}
					break;
				}
				neckDampedTransform.weight = Mathf.Lerp(neckDampedTransform.weight, 0.2f, 8f * Time.deltaTime);
				creatureAnimator.SetInteger("BehaviourState", 2);
				if (!base.IsOwner)
				{
					break;
				}
				if (base.IsOwner && roamPlanet.inProgress)
				{
					StopSearch(roamPlanet);
				}
				if (!inLunge)
				{
					lungeCooldown -= Time.deltaTime;
					if (Vector3.Distance(base.transform.position, noisePositionGuess) < 4f && lungeCooldown <= 0f)
					{
						inLunge = true;
						EnterLunge();
						break;
					}
				}
				agent.speed = Mathf.Clamp(agent.speed + Time.deltaTime, 13f, 18f);
				if (stunNormalizedTimer > 0f || coweringOnFloor)
				{
					agent.speed = 0f;
				}
				AITimer -= Time.deltaTime;
				if (AITimer <= 0f)
				{
					AITimer = 3f;
					suspicionLevel--;
					if (Vector3.Distance(base.transform.position, agent.destination) < 3f)
					{
						SearchForPreviouslyHeardSound();
					}
					if (suspicionLevel <= 8)
					{
						SwitchToBehaviourState(1);
					}
				}
				break;
			case 3:
				if (base.IsOwner)
				{
					agent.speed -= Time.deltaTime * 5f;
					if (!endingLunge && agent.speed < 1.5f && !inKillAnimation)
					{
						endingLunge = true;
						lungeCooldown = 0.25f;
						EndLungeServerRpc();
					}
				}
				break;
			}
		}
	}

	private void SearchForPreviouslyHeardSound()
	{
		int num = 0;
		Vector3 vector = base.transform.position;
		while (num < 5 && Vector3.Distance(vector, base.transform.position) < 4f)
		{
			num++;
			vector = roundManager.GetRandomNavMeshPositionInRadius(lastHeardNoisePosition, lastHeardNoiseDistanceWhenHeard / noiseApproximation);
		}
		SetDestinationToPosition(vector);
		noisePositionGuess = vector;
	}

	private IEnumerator enterChaseMode()
	{
		if (base.IsOwner)
		{
			agent.speed = 0.05f;
		}
		DropCarriedBody();
		creatureVoice.PlayOneShot(screamSFX);
		if (!isEnemyDead)
		{
			creatureAnimator.SetTrigger("ChaseHowl");
		}
		if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.transform.position) < 16f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.5f);
		}
		yield return new WaitForSeconds(0.5f);
		if (!heardOtherHowl)
		{
			CallAllDogsWithHowl();
		}
		heardOtherHowl = false;
		yield return new WaitForSeconds(0.2f);
		if (!isEnemyDead)
		{
			creatureVoice.clip = breathingSFX;
			creatureVoice.Play();
			creatureAnimator.SetBool("StartedChase", value: true);
			hasEnteredChaseModeFully = true;
			creatureVoice.PlayOneShot(breathingSFX);
		}
	}

	private void CallAllDogsWithHowl()
	{
		MouthDogAI[] array = UnityEngine.Object.FindObjectsOfType<MouthDogAI>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == this))
			{
				array[i].ReactToOtherDogHowl(base.transform.position);
			}
		}
	}

	public void ReactToOtherDogHowl(Vector3 howlPosition)
	{
		heardOtherHowl = true;
		lastHeardNoiseDistanceWhenHeard = Vector3.Distance(base.transform.position, howlPosition);
		noisePositionGuess = roundManager.GetRandomNavMeshPositionInRadius(howlPosition, lastHeardNoiseDistanceWhenHeard / noiseApproximation);
		SetDestinationToPosition(noisePositionGuess);
		if (currentBehaviourStateIndex < 2)
		{
			SwitchToBehaviourStateOnLocalClient(2);
		}
		suspicionLevel = 8;
		lastHeardNoisePosition = howlPosition;
		Debug.Log($"Setting lastHeardNoisePosition to {howlPosition}");
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
		if (stunNormalizedTimer > 0f || noiseID == 675189 || noiseID == 7 || noiseID == 546 || inKillAnimation || hearNoiseCooldown >= 0f || timesNoisePlayedInOneSpot > 15)
		{
			return;
		}
		hearNoiseCooldown = 0.03f;
		float num = Vector3.Distance(base.transform.position, noisePosition);
		Debug.Log($"dog '{base.gameObject.name}': Heard noise! Distance: {num} meters");
		float num2 = 18f * noiseLoudness;
		if (Physics.Linecast(base.transform.position, noisePosition, 256))
		{
			noiseLoudness /= 2f;
			num2 /= 2f;
		}
		if (noiseLoudness < 0.25f)
		{
			return;
		}
		if (currentBehaviourStateIndex < 2 && num < num2)
		{
			suspicionLevel = 9;
		}
		else
		{
			suspicionLevel++;
		}
		bool fullyEnrage = false;
		if (suspicionLevel >= 9)
		{
			if (currentBehaviourStateIndex < 2)
			{
				fullyEnrage = true;
			}
		}
		else if (suspicionLevel >= 5 && currentBehaviourStateIndex == 0)
		{
			fullyEnrage = false;
		}
		AITimer = 3f;
		EnrageDogOnLocalClient(noisePosition, num, approximatePosition: true, fullyEnrage);
	}

	private void EnrageDogOnLocalClient(Vector3 targetPosition, float distanceToNoise, bool approximatePosition = true, bool fullyEnrage = false)
	{
		Debug.Log($"Mouth dog targetPos 1: {targetPosition}; distanceToNoise: {distanceToNoise}");
		if (approximatePosition)
		{
			targetPosition = roundManager.GetRandomNavMeshPositionInRadius(targetPosition, distanceToNoise / noiseApproximation);
		}
		noisePositionGuess = targetPosition;
		Debug.Log($"Mouth dog targetPos 2: {targetPosition}");
		if (fullyEnrage)
		{
			if (currentBehaviourStateIndex < 2)
			{
				SwitchToBehaviourState(2);
				hearNoiseCooldown = 1f;
				suspicionLevel = 12;
			}
			suspicionLevel = Mathf.Clamp(suspicionLevel, 0, 11);
		}
		else if (currentBehaviourStateIndex == 0)
		{
			SwitchToBehaviourState(1);
		}
		if (!base.IsOwner)
		{
			ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
		}
		if (!inLunge)
		{
			SetDestinationToPosition(noisePositionGuess);
		}
		lastHeardNoiseDistanceWhenHeard = distanceToNoise;
		lastHeardNoisePosition = targetPosition;
		Debug.Log($"Dog lastheardnoisePosition: {lastHeardNoisePosition}");
	}

	private void EnterLunge()
	{
		if (!base.IsOwner)
		{
			ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
		}
		SwitchToBehaviourState(3);
		endingLunge = false;
		ray = new Ray(base.transform.position + Vector3.up, base.transform.forward);
		Vector3 pos = ((!Physics.Raycast(ray, out rayHit, 17f, StartOfRound.Instance.collidersAndRoomMask)) ? ray.GetPoint(17f) : rayHit.point);
		pos = roundManager.GetNavMeshPosition(pos);
		SetDestinationToPosition(pos);
		agent.speed = 13f;
	}

	[ServerRpc(RequireOwnership = false)]
	public void EndLungeServerRpc()
			{
				EndLungeClientRpc();
			}

	[ClientRpc]
	public void EndLungeClientRpc()
{		{
			SwitchToBehaviourStateOnLocalClient(2);
			if (!isEnemyDead)
			{
				creatureAnimator.SetTrigger("EndLungeNoKill");
			}
			inLunge = false;
			Debug.Log("Ending lunge");
		}
}
	private void ChaseLocalPlayer()
	{
		SwitchToBehaviourState(2);
		ChangeOwnershipOfEnemy(NetworkManager.Singleton.LocalClientId);
		SetDestinationToPosition(GameNetworkManager.Instance.localPlayerController.transform.position);
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		enemyHP -= force;
		if (base.IsOwner)
		{
			if (enemyHP <= 0)
			{
				KillEnemyOnOwnerClient();
				return;
			}
			if (inKillAnimation)
			{
				StopKillAnimationServerRpc();
			}
		}
		if (playerWhoHit != null && currentBehaviourStateIndex != 2 && base.IsOwner)
		{
			EnrageDogOnLocalClient(playerWhoHit.transform.position, Vector3.Distance(base.transform.position, playerWhoHit.transform.position));
		}
	}

	public override void OnCollideWithEnemy(Collider other, EnemyAI collidedEnemy = null)
	{
		base.OnCollideWithEnemy(other, collidedEnemy);
		if (!(collidedEnemy.enemyType == enemyType) && !(timeSinceHittingOtherEnemy < 1f))
		{
			if (currentBehaviourStateIndex == 2 && !inLunge)
			{
				base.transform.LookAt(other.transform.position);
				base.transform.localEulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
				inLunge = true;
				EnterLunge();
			}
			timeSinceHittingOtherEnemy = 0f;
			collidedEnemy.HitEnemy(2, null, playHitSFX: true);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, inKillAnimation);
		if (!(playerControllerB != null))
		{
			return;
		}
		VehicleController vehicleController = UnityEngine.Object.FindObjectOfType<VehicleController>();
		bool num = vehicleController != null && playerControllerB.physicsParent != null && playerControllerB.physicsParent == vehicleController.transform && !vehicleController.backDoorOpen;
		bool flag = StartOfRound.Instance.hangarDoorsClosed && isInsidePlayerShip != playerControllerB.isInHangarShipRoom;
		if (num || flag)
		{
			return;
		}
		Vector3 vector = Vector3.Normalize((base.transform.position + Vector3.up - playerControllerB.gameplayCamera.transform.position) * 100f);
		if (Physics.Linecast(base.transform.position + Vector3.up + vector * 0.5f, playerControllerB.gameplayCamera.transform.position, out var hitInfo, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
		{
			if (!(hitInfo.collider == debugCollider))
			{
				debugCollider = hitInfo.collider;
			}
		}
		else if (!playerControllerB.inVehicleAnimation || !(Vector3.Distance(base.transform.position, playerControllerB.transform.position) > 3f))
		{
			if (currentBehaviourStateIndex == 3)
			{
				playerControllerB.inAnimationWithEnemy = this;
				KillPlayerServerRpc((int)playerControllerB.playerClientId);
			}
			else if (currentBehaviourStateIndex == 0 || currentBehaviourStateIndex == 1)
			{
				ChaseLocalPlayer();
			}
			else if (currentBehaviourStateIndex == 2 && !inLunge)
			{
				base.transform.LookAt(other.transform.position);
				base.transform.localEulerAngles = new Vector3(0f, base.transform.eulerAngles.y, 0f);
				inLunge = true;
				EnterLunge();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerServerRpc(int playerId)
{		{
			if (!inKillAnimation)
			{
				inKillAnimation = true;
				KillPlayerClientRpc(playerId);
			}
			else
			{
				CancelKillAnimationWithPlayerClientRpc(playerId);
			}
		}
}
	[ClientRpc]
	public void CancelKillAnimationWithPlayerClientRpc(int playerObjectId)
			{
				StartOfRound.Instance.allPlayerScripts[playerObjectId].inAnimationWithEnemy = null;
			}

	[ClientRpc]
	public void KillPlayerClientRpc(int playerId)
{		{
			Debug.Log("Kill player rpc");
			if (killPlayerCoroutine != null)
			{
				StopCoroutine(killPlayerCoroutine);
			}
			killPlayerCoroutine = StartCoroutine(KillPlayer(playerId));
		}
}
	private IEnumerator KillPlayer(int playerId)
	{
		if (base.IsOwner)
		{
			agent.speed = Mathf.Clamp(agent.speed, 2f, 0f);
		}
		Debug.Log("killing player A");
		creatureVoice.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
		creatureVoice.PlayOneShot(killPlayerSFX, 1f);
		PlayerControllerB killPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
		inKillAnimation = true;
		if (!isEnemyDead)
		{
			creatureAnimator.SetTrigger("EndLungeKill");
		}
		Debug.Log("killing player B");
		if (GameNetworkManager.Instance.localPlayerController == killPlayer)
		{
			killPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Mauling);
		}
		float startTime = Time.timeSinceLevelLoad;
		yield return new WaitUntil(() => killPlayer.deadBody != null || Time.timeSinceLevelLoad - startTime > 2f);
		if (killPlayer.deadBody == null)
		{
			Debug.Log("Giant dog: Player body was not spawned or found within 2 seconds.");
			killPlayer.inAnimationWithEnemy = null;
			inKillAnimation = false;
			yield break;
		}
		TakeBodyInMouth(killPlayer.deadBody);
		startTime = Time.timeSinceLevelLoad;
		Quaternion rotateTo = Quaternion.Euler(new Vector3(0f, RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(base.transform.position + Vector3.up * 0.6f), 0f));
		Quaternion rotateFrom = base.transform.rotation;
		while (Time.timeSinceLevelLoad - startTime < 2f)
		{
			yield return null;
			if (base.IsOwner)
			{
				base.transform.rotation = Quaternion.RotateTowards(rotateFrom, rotateTo, 60f * Time.deltaTime);
			}
		}
		yield return new WaitForSeconds(3.01f);
		DropCarriedBody();
		suspicionLevel = 2;
		SwitchToBehaviourStateOnLocalClient(2);
		endingLunge = true;
		inKillAnimation = false;
	}

	private void StopKillAnimation()
	{
		if (killPlayerCoroutine != null)
		{
			StopCoroutine(killPlayerCoroutine);
		}
		creatureVoice.Stop();
		DropCarriedBody();
		suspicionLevel = 2;
		SwitchToBehaviourStateOnLocalClient(2);
		endingLunge = true;
		inKillAnimation = false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void StopKillAnimationServerRpc()
			{
				StopKillAnimationClientRpc();
			}

	[ClientRpc]
	public void StopKillAnimationClientRpc()
			{
				StopKillAnimation();
			}

	private void TakeBodyInMouth(DeadBodyInfo body)
	{
		carryingBody = body;
		carryingBody.attachedTo = mouthGrip;
		carryingBody.attachedLimb = body.bodyParts[5];
		carryingBody.matchPositionExactly = true;
	}

	private void DropCarriedBody()
	{
		if (!(carryingBody == null))
		{
			carryingBody.speedMultiplier = 12f;
			carryingBody.attachedTo = null;
			carryingBody.attachedLimb = null;
			carryingBody.matchPositionExactly = false;
			carryingBody = null;
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		StopKillAnimation();
		creatureVoice.Stop();
		creatureSFX.Stop();
		base.KillEnemy(destroy);
	}

	public override void ReceiveLoudNoiseBlast(Vector3 position, float angle)
	{
		base.ReceiveLoudNoiseBlast(position, angle);
		if (angle < 30f)
		{
			coweringOnFloor = true;
		}
	}

	public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false)
	{
		base.EnableEnemyMesh(enable);
		ParticleSystem[] componentsInChildren = base.gameObject.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			ParticleSystem.MainModule main = componentsInChildren[i].main;
			main.playOnAwake = base.enabled;
		}
	}

	public override void OnDrawGizmos()
	{
		base.OnDrawGizmos();
		if (debugEnemyAI)
		{
			Gizmos.DrawCube(noisePositionGuess, Vector3.one);
			Gizmos.DrawLine(noisePositionGuess, base.transform.position + Vector3.up);
		}
	}
}
