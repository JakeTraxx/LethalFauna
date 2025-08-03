using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class SandSpiderAI : EnemyAI
{
	private float[] legDistances = new float[12]
	{
		2.2f, 2.2f, 1.8f, 1.8f, 1.3f, 1.3f, 1.5f, 1.5f, 1f, 1f,
		0.6f, 0.6f
	};

	public Vector3[] legPositions;

	public Transform[] legDefaultPositions;

	public Transform[] legTargets;

	public Transform abdomen;

	public Transform mouthTarget;

	public bool burrowing;

	public Transform turnCompass;

	public Vector3 wallPosition;

	public Vector3 wallNormal;

	public Vector3 floorPosition;

	private bool onWall;

	private RaycastHit rayHit;

	private Ray ray;

	public bool lookingForWallPosition;

	private bool gotWallPositionInLOS;

	private float tryWallPositionInterval;

	private bool reachedWallPosition;

	public Transform meshContainer;

	public Vector3 meshContainerPosition;

	public Vector3 meshContainerTarget;

	private Quaternion meshContainerTargetRotation;

	public float spiderSpeed;

	public float calculatePathToAgentInterval;

	public bool navigateMeshTowardsPosition;

	public Vector3 navigateToPositionTarget;

	public NavMeshHit navHit;

	public List<SandSpiderWebTrap> webTraps = new List<SandSpiderWebTrap>();

	public GameObject webTrapPrefab;

	public int maxWebTrapsToPlace;

	private float timeSincePlacingWebTrap;

	public Vector3 meshContainerServerPosition;

	public Vector3 meshContainerServerRotation;

	private Vector3 refVel;

	public Transform homeNode;

	public AISearchRoutine patrolHomeBase;

	private bool setDestinationToHomeBase;

	private float chaseTimer;

	private bool overrideSpiderLookRotation;

	private bool watchFromDistance;

	public float overrideAnimation;

	private float overrideAnimationWeight;

	private float timeSinceHittingPlayer;

	private DeadBodyInfo currentlyHeldBody;

	public Mesh playerBodyWebMesh;

	public Material playerBodyWebMat;

	private bool spooledPlayerBody;

	private bool spoolingPlayerBody;

	private Coroutine turnBodyIntoWebCoroutine;

	private bool decidedChanceToHangBodyEarly;

	public GameObject hangBodyPhysicsPrefab;

	private Coroutine grabBodyCoroutine;

	private float waitOnWallTimer;

	public AudioClip[] footstepSFX;

	public AudioSource footstepAudio;

	public AudioClip hitWebSFX;

	public AudioClip attackSFX;

	public AudioClip spoolPlayerSFX;

	public AudioClip hangPlayerSFX;

	public AudioClip breakWebSFX;

	public AudioClip hitSpiderSFX;

	private float lookAtPlayerInterval;

	public Rigidbody meshContainerRigidbody;

	private RaycastHit rayHitB;

	public MeshRenderer spiderSafeModeMesh;

	public SkinnedMeshRenderer spiderNormalMesh;

	private bool spiderSafeEnabled;

	public override void Start()
	{
		base.Start();
		meshContainerPosition = base.transform.position;
		meshContainerTarget = base.transform.position;
		navHit = default(NavMeshHit);
		rayHitB = default(RaycastHit);
		patrolHomeBase.searchWidth = 17f;
		patrolHomeBase.searchPrecision = 3f;
		maxWebTrapsToPlace = Random.Range(6, 9);
		homeNode = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: false, 2);
		meshContainerTargetRotation = Quaternion.identity;
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead)
		{
			return;
		}
		if (lookingForWallPosition && !gotWallPositionInLOS)
		{
			gotWallPositionInLOS = GetWallPositionForSpiderMesh();
		}
		if (navigateMeshTowardsPosition)
		{
			CalculateSpiderPathToPosition();
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			setDestinationToHomeBase = false;
			lookingForWallPosition = false;
			reachedWallPosition = false;
			if (!patrolHomeBase.inProgress)
			{
				StartSearch(homeNode.position, patrolHomeBase);
			}
			break;
		case 1:
			movingTowardsTargetPlayer = false;
			if (!lookingForWallPosition)
			{
				if (Vector3.Distance(base.transform.position, homeNode.position) > 7f)
				{
					patrolHomeBase.searchWidth = 6f;
					if (!patrolHomeBase.inProgress)
					{
						if (PathIsIntersectedByLineOfSight(homeNode.position, calculatePathDistance: false, avoidLineOfSight: false))
						{
							homeNode = ChooseClosestNodeToPosition(base.transform.position, avoidLineOfSight: false, 2);
						}
						StartSearch(homeNode.position, patrolHomeBase);
					}
					break;
				}
				if (currentlyHeldBody != null && !spooledPlayerBody)
				{
					if (turnBodyIntoWebCoroutine == null)
					{
						turnBodyIntoWebCoroutine = StartCoroutine(turnBodyIntoWeb());
						SpiderTurnBodyIntoWebServerRpc();
					}
					break;
				}
				if (currentlyHeldBody != null && !decidedChanceToHangBodyEarly)
				{
					if (Random.Range(0, 100) < 150)
					{
						HangBodyFromCeiling();
						SpiderHangBodyServerRpc();
					}
					decidedChanceToHangBodyEarly = true;
				}
				if (patrolHomeBase.inProgress)
				{
					StopSearch(patrolHomeBase);
				}
				lookingForWallPosition = true;
				reachedWallPosition = false;
				break;
			}
			if (reachedWallPosition)
			{
				if (currentlyHeldBody != null)
				{
					HangBodyFromCeiling();
					SpiderHangBodyServerRpc();
				}
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
				{
					if (PlayerIsTargetable(StartOfRound.Instance.allPlayerScripts[i]) && !Physics.Linecast(meshContainer.position, StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMask))
					{
						float num = Vector3.Distance(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, meshContainer.position);
						if (num < 5f)
						{
							TriggerChaseWithPlayer(StartOfRound.Instance.allPlayerScripts[i]);
							break;
						}
						if (num < 10f)
						{
							Vector3 position = StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position;
							float num2 = Vector3.Dot(position - meshContainer.position, wallNormal);
							Vector3 forward = position - num2 * wallNormal;
							meshContainerTargetRotation = Quaternion.LookRotation(forward, wallNormal);
							overrideSpiderLookRotation = true;
							break;
						}
					}
				}
			}
			overrideSpiderLookRotation = false;
			break;
		case 2:
			if (patrolHomeBase.inProgress)
			{
				StopSearch(patrolHomeBase);
			}
			if (watchFromDistance && !TargetClosestPlayer(2f, requireLineOfSight: true, 80f))
			{
				StopChasing();
			}
			if (!(targetPlayer == null))
			{
				if (targetPlayer.isPlayerDead && targetPlayer.deadBody != null && (!SetDestinationToPosition(targetPlayer.deadBody.bodyParts[6].transform.position, checkForPath: true) || targetPlayer.deadBody.attachedTo != null))
				{
					targetPlayer = null;
					StopChasing(moveTowardsDeadPlayerBody: true);
				}
				if (watchFromDistance)
				{
					SetDestinationToPosition(ChooseClosestNodeToPosition(targetPlayer.transform.position, avoidLineOfSight: false, 4).transform.position);
				}
			}
			break;
		}
	}

	private IEnumerator turnBodyIntoWeb()
	{
		if (currentlyHeldBody == null)
		{
			Debug.LogError("Sand Spider: Tried to wrap body but it could not be found.");
			yield break;
		}
		spoolingPlayerBody = true;
		overrideAnimation = 4.05f;
		creatureAnimator.SetTrigger("spool");
		creatureSFX.PlayOneShot(spoolPlayerSFX);
		yield return new WaitForSeconds(0.9f);
		if (currentlyHeldBody.attachedTo != mouthTarget)
		{
			CancelSpoolingBody();
		}
		currentlyHeldBody.ChangeMesh(playerBodyWebMesh, playerBodyWebMat);
		yield return new WaitForSeconds(3.105f);
		spooledPlayerBody = true;
		spoolingPlayerBody = false;
		turnBodyIntoWebCoroutine = null;
		if (currentlyHeldBody.attachedTo != mouthTarget)
		{
			CancelSpoolingBody();
		}
	}

	private void CancelSpoolingBody()
	{
		if (turnBodyIntoWebCoroutine != null)
		{
			StopCoroutine(turnBodyIntoWebCoroutine);
		}
		if (currentlyHeldBody != null)
		{
			currentlyHeldBody.attachedLimb = null;
			currentlyHeldBody.attachedTo = null;
			currentlyHeldBody = null;
		}
		spooledPlayerBody = false;
		spoolingPlayerBody = false;
	}

	[ServerRpc]
	public void SpiderTurnBodyIntoWebServerRpc()
{		{
			SpiderTurnBodyIntoWebClientRpc();
		}
}
	[ClientRpc]
	public void SpiderTurnBodyIntoWebClientRpc()
{if(!base.IsOwner && !isEnemyDead && turnBodyIntoWebCoroutine == null)			{
				turnBodyIntoWebCoroutine = StartCoroutine(turnBodyIntoWeb());
			}
}
	[ServerRpc]
	public void SpiderHangBodyServerRpc()
{		{
			SpiderHangBodyClientRpc();
		}
}
	[ClientRpc]
	public void SpiderHangBodyClientRpc()
{if(!base.IsOwner)			{
				HangBodyFromCeiling();
			}
}
	private void HangBodyFromCeiling()
	{
		if (currentlyHeldBody == null)
		{
			Debug.LogError("Sand spider: Held body was null, couldn't hang up");
			return;
		}
		Vector3 position = abdomen.position + Vector3.up * 6f;
		if (Physics.Raycast(abdomen.position, Vector3.up, out rayHit, 25f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
		{
			position = rayHit.point;
		}
		SetLineRendererPoints component = Object.Instantiate(hangBodyPhysicsPrefab, position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform).GetComponent<SetLineRendererPoints>();
		component.target.position = currentlyHeldBody.bodyParts[6].transform.position;
		currentlyHeldBody.attachedTo = component.target;
		decidedChanceToHangBodyEarly = false;
		currentlyHeldBody.bodyAudio.volume = 0.8f;
		currentlyHeldBody.bodyAudio.PlayOneShot(hangPlayerSFX);
		currentlyHeldBody = null;
	}

	[ServerRpc]
	public void GrabBodyServerRpc(int playerId)
{		{
			GrabBodyClientRpc(playerId);
		}
}
	[ClientRpc]
	public void GrabBodyClientRpc(int playerId)
{if(!base.IsOwner)		{
			if (grabBodyCoroutine != null)
			{
				StopCoroutine(grabBodyCoroutine);
			}
			grabBodyCoroutine = StartCoroutine(WaitForBodyToGrab(playerId));
		}
}
	private void GrabBody(DeadBodyInfo body)
	{
		currentlyHeldBody = body;
		currentlyHeldBody.attachedLimb = currentlyHeldBody.bodyParts[6];
		currentlyHeldBody.attachedTo = mouthTarget;
		currentlyHeldBody.matchPositionExactly = true;
	}

	private IEnumerator WaitForBodyToGrab(int playerId)
	{
		float timeAtStartOfWait = Time.timeSinceLevelLoad;
		yield return new WaitUntil(() => StartOfRound.Instance.allPlayerScripts[playerId].deadBody != null || Time.timeSinceLevelLoad - timeAtStartOfWait > 10f);
		if (StartOfRound.Instance.allPlayerScripts[playerId].deadBody == null)
		{
			Debug.LogError("SandSpider: Grab body RPC was called, but body did not spawn within 10 seconds on this client.");
		}
		DeadBodyInfo deadBody = StartOfRound.Instance.allPlayerScripts[playerId].deadBody;
		GrabBody(deadBody);
	}

	private void CalculateSpiderPathToPosition()
	{
		if (NavMesh.CalculatePath(meshContainer.position, navigateToPositionTarget, agent.areaMask, path1))
		{
			if (path1.corners.Length > 1)
			{
				meshContainerTarget = path1.corners[1];
				if (!overrideSpiderLookRotation)
				{
					SetSpiderLookAtPosition(path1.corners[1]);
				}
			}
			else
			{
				meshContainerTarget = navigateToPositionTarget;
				if (!overrideSpiderLookRotation)
				{
					SetSpiderLookAtPosition(navigateToPositionTarget);
				}
			}
		}
		else
		{
			meshContainer.position = RoundManager.Instance.GetNavMeshPosition(meshContainer.position, navHit, 5f, agent.areaMask);
			meshContainerTarget = meshContainer.position;
		}
	}

	public override void Update()
	{
		base.Update();
		if (spiderSafeEnabled != IngamePlayerSettings.Instance.unsavedSettings.spiderSafeMode)
		{
			spiderSafeEnabled = IngamePlayerSettings.Instance.unsavedSettings.spiderSafeMode;
			spiderSafeModeMesh.enabled = spiderSafeEnabled;
			spiderNormalMesh.enabled = !spiderSafeEnabled;
		}
		timeSinceHittingPlayer += Time.deltaTime;
		if (isEnemyDead)
		{
			agent.speed = 0f;
			spiderSpeed = 0f;
			creatureAnimator.SetBool("moving", value: false);
			return;
		}
		if (!base.IsOwner)
		{
			creatureAnimator.SetBool("moving", refVel.sqrMagnitude > 0.002f);
			return;
		}
		creatureAnimator.SetBool("moving", refVel.sqrMagnitude * Time.deltaTime * 25f > 0.002f);
		SyncMeshContainerPositionToClients();
		CalculateMeshMovement();
		if (!base.IsOwner)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
		{
			setDestinationToHomeBase = false;
			lookingForWallPosition = false;
			movingTowardsTargetPlayer = false;
			overrideSpiderLookRotation = false;
			waitOnWallTimer = 11f;
			if (stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
				spiderSpeed = 0f;
			}
			else
			{
				agent.speed = 4.25f;
				spiderSpeed = 4.25f;
			}
			PlayerControllerB closestPlayer = GetClosestPlayer(requireLineOfSight: true);
			if (closestPlayer != null && CheckLineOfSightForPosition(closestPlayer.gameplayCamera.transform.position, 80f, 15, 2f))
			{
				targetPlayer = closestPlayer;
				SwitchToBehaviourState(2);
				chaseTimer = 12.5f;
				watchFromDistance = mostOptimalDistance > 8f;
			}
			if (timeSincePlacingWebTrap > 4f)
			{
				if (AttemptPlaceWebTrap())
				{
					timeSincePlacingWebTrap = Random.Range(0.5f, 1f);
				}
				else
				{
					timeSincePlacingWebTrap = 0.17f;
				}
				if (webTraps.Count > maxWebTrapsToPlace)
				{
					SwitchToBehaviourState(1);
				}
			}
			else
			{
				timeSincePlacingWebTrap += Time.deltaTime;
			}
			break;
		}
		case 1:
			if (spoolingPlayerBody || stunNormalizedTimer > 0f)
			{
				agent.speed = 0f;
				spiderSpeed = 0f;
			}
			else
			{
				agent.speed = 4.5f;
				spiderSpeed = 3.75f;
			}
			if (webTraps.Count < maxWebTrapsToPlace && currentlyHeldBody == null)
			{
				waitOnWallTimer -= Time.deltaTime;
			}
			if (waitOnWallTimer <= 0f)
			{
				SwitchToBehaviourState(0);
			}
			break;
		case 2:
			setDestinationToHomeBase = false;
			reachedWallPosition = false;
			lookingForWallPosition = false;
			waitOnWallTimer = 11f;
			if (spoolingPlayerBody)
			{
				CancelSpoolingBody();
			}
			if (targetPlayer == null)
			{
				StopChasing();
				break;
			}
			if (onWall)
			{
				movingTowardsTargetPlayer = true;
				agent.speed = 4.25f;
				spiderSpeed = 4.25f;
				break;
			}
			if (watchFromDistance)
			{
				if (lookAtPlayerInterval <= 0f)
				{
					lookAtPlayerInterval = 3f;
					movingTowardsTargetPlayer = false;
					overrideSpiderLookRotation = true;
					Vector3 position = targetPlayer.transform.position;
					position.y = meshContainer.position.y;
					SetSpiderLookAtPosition(position);
				}
				else
				{
					lookAtPlayerInterval -= Time.deltaTime;
				}
				agent.speed = 0f;
				spiderSpeed = 0f;
				if (Physics.Linecast(meshContainer.position, targetPlayer.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
				{
					StopChasing();
				}
				else if (Vector3.Distance(targetPlayer.gameplayCamera.transform.position, base.transform.position) < 5f || stunNormalizedTimer > 0f)
				{
					watchFromDistance = false;
				}
				break;
			}
			switch (enemyHP)
			{
			default:
				agent.speed = 4.4f;
				spiderSpeed = 4.4f;
				break;
			case 2:
				agent.speed = 4.58f;
				spiderSpeed = 4.58f;
				break;
			case 1:
				agent.speed = 5.04f;
				spiderSpeed = 5.04f;
				break;
			}
			movingTowardsTargetPlayer = true;
			overrideSpiderLookRotation = false;
			if (timeSinceHittingPlayer < 0.5f)
			{
				agent.speed = 0.7f;
				spiderSpeed = 0.4f;
			}
			if (targetPlayer.isPlayerDead && targetPlayer.deadBody != null)
			{
				if (Vector3.Distance(targetPlayer.deadBody.bodyParts[6].transform.position, meshContainer.position) < 3.7f)
				{
					spooledPlayerBody = false;
					GrabBody(targetPlayer.deadBody);
					GrabBodyServerRpc((int)targetPlayer.playerClientId);
					SwitchToBehaviourState(1);
				}
				else
				{
					targetPlayer = null;
					StopChasing();
				}
			}
			else if (!PlayerIsTargetable(targetPlayer) || (Vector3.Distance(targetPlayer.transform.position, homeNode.position) > 12f && Vector3.Distance(targetPlayer.transform.position, base.transform.position) > 5f))
			{
				chaseTimer -= Time.deltaTime;
				if (chaseTimer <= 0f)
				{
					targetPlayer = null;
					StopChasing();
				}
			}
			break;
		}
		if (stunNormalizedTimer > 0f)
		{
			spiderSpeed = 0f;
			agent.speed = 0f;
		}
	}

	private void StopChasing(bool moveTowardsDeadPlayerBody = false)
	{
		overrideSpiderLookRotation = false;
		movingTowardsTargetPlayer = false;
		lookingForWallPosition = false;
		if (webTraps.Count > maxWebTrapsToPlace || moveTowardsDeadPlayerBody)
		{
			SwitchToBehaviourState(1);
		}
		else
		{
			SwitchToBehaviourState(0);
		}
	}

	private void CalculateMeshMovement()
	{
		if (lookingForWallPosition && gotWallPositionInLOS)
		{
			if (!onWall)
			{
				agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
				navigateMeshTowardsPosition = true;
				navigateToPositionTarget = floorPosition;
				if (!overrideSpiderLookRotation)
				{
					turnCompass.position = meshContainer.position;
					turnCompass.LookAt(floorPosition, Vector3.up);
					meshContainerTargetRotation = turnCompass.rotation;
				}
				if (Vector3.Distance(meshContainer.transform.position, floorPosition) < 0.7f)
				{
					onWall = true;
				}
			}
			else
			{
				agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
				navigateMeshTowardsPosition = false;
				meshContainerTarget = wallPosition;
				if (!reachedWallPosition && Vector3.Distance(meshContainer.position, wallPosition) < 0.1f)
				{
					reachedWallPosition = true;
				}
				if (!overrideSpiderLookRotation)
				{
					turnCompass.position = meshContainer.position;
					turnCompass.LookAt(wallPosition, wallNormal);
					meshContainerTargetRotation = turnCompass.rotation;
				}
			}
			return;
		}
		if (!lookingForWallPosition)
		{
			gotWallPositionInLOS = false;
			reachedWallPosition = false;
		}
		if (!onWall)
		{
			agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
			if (!navigateMeshTowardsPosition)
			{
				CalculateSpiderPathToPosition();
				navigateMeshTowardsPosition = true;
			}
			navigateToPositionTarget = base.transform.position + Vector3.Normalize(agent.desiredVelocity) * 2f;
			return;
		}
		agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		navigateMeshTowardsPosition = false;
		meshContainerTarget = floorPosition;
		if (!overrideSpiderLookRotation)
		{
			turnCompass.position = meshContainer.position;
			turnCompass.LookAt(floorPosition, wallNormal);
			meshContainerTargetRotation = turnCompass.rotation;
		}
		if (Vector3.Distance(meshContainer.transform.position, floorPosition) < 1.1f)
		{
			onWall = false;
		}
	}

	private void SetSpiderLookAtPosition(Vector3 lookAt)
	{
		turnCompass.position = meshContainer.position;
		turnCompass.LookAt(lookAt, Vector3.up);
		meshContainerTargetRotation = turnCompass.rotation;
	}

	private bool GetWallPositionForSpiderMesh()
	{
		float num = 6f;
		if (Physics.Raycast(base.transform.position, Vector3.up, out rayHit, 22f, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore))
		{
			num = ((!(currentlyHeldBody != null)) ? (rayHit.distance - 1.3f) : (rayHit.distance - 2f));
		}
		float num2 = RoundManager.Instance.YRotationThatFacesTheNearestFromPosition(base.transform.position + Vector3.up * num, 10f);
		if (num2 != -777f)
		{
			turnCompass.eulerAngles = new Vector3(0f, num2, 0f);
			ray = new Ray(base.transform.position + Vector3.up * num, turnCompass.forward);
			if (Physics.Raycast(ray, out rayHit, 10.1f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				wallPosition = ray.GetPoint(rayHit.distance - 0.2f);
				wallNormal = rayHit.normal;
				if (Physics.Raycast(wallPosition, Vector3.down, out rayHitB, 7f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
				{
					floorPosition = rayHitB.point;
					return true;
				}
			}
		}
		return false;
	}

	public void LateUpdate()
	{
		if (isEnemyDead)
		{
			meshContainer.eulerAngles = new Vector3(0f, meshContainer.eulerAngles.y, 0f);
			creatureAnimator.SetLayerWeight(creatureAnimator.GetLayerIndex("MoveLegs"), 0f);
		}
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		if (base.IsOwner)
		{
			Vector3 vector = meshContainerPosition;
			meshContainerPosition = Vector3.MoveTowards(meshContainerPosition, meshContainerTarget, spiderSpeed * Time.deltaTime);
			refVel = vector - meshContainerPosition;
			meshContainer.position = meshContainerPosition;
			meshContainer.rotation = Quaternion.Lerp(meshContainer.rotation, meshContainerTargetRotation, 8f * Time.deltaTime);
		}
		else
		{
			meshContainer.position = Vector3.SmoothDamp(meshContainerPosition, meshContainerServerPosition, ref refVel, 4f * Time.deltaTime);
			meshContainerPosition = meshContainer.position;
			meshContainer.rotation = Quaternion.Lerp(meshContainer.rotation, Quaternion.Euler(meshContainerServerRotation), 9f * Time.deltaTime);
		}
		if (overrideAnimation <= 0f && stunNormalizedTimer <= 0f)
		{
			if (overrideAnimationWeight > 0.05f)
			{
				overrideAnimationWeight = Mathf.Lerp(overrideAnimationWeight, 0f, 20f * Time.deltaTime);
			}
			else
			{
				overrideAnimationWeight = 0f;
			}
			MoveLegsProcedurally();
		}
		else
		{
			overrideAnimation -= Time.deltaTime;
			overrideAnimationWeight = Mathf.Lerp(overrideAnimationWeight, 1f, 20f * Time.deltaTime);
		}
		creatureAnimator.SetBool("stunned", stunNormalizedTimer > 0f);
		creatureAnimator.SetLayerWeight(creatureAnimator.GetLayerIndex("MoveLegs"), overrideAnimationWeight);
	}

	public void MoveLegsProcedurally()
	{
		for (int i = 0; i < legTargets.Length; i++)
		{
			legTargets[i].position = Vector3.Lerp(legTargets[i].position, legPositions[i], 35f * Time.deltaTime);
		}
		bool flag = false;
		for (int j = 0; j < legPositions.Length; j++)
		{
			if ((legPositions[j] - legDefaultPositions[j].position).sqrMagnitude > legDistances[j] * 1.4f)
			{
				legPositions[j] = legDefaultPositions[j].position;
				flag = true;
			}
		}
		if (flag)
		{
			footstepAudio.pitch = Random.Range(0.6f, 1.2f);
			footstepAudio.PlayOneShot(footstepSFX[Random.Range(0, footstepSFX.Length)], Random.Range(0.1f, 1f));
			WalkieTalkie.TransmitOneShotAudio(footstepAudio, footstepSFX[Random.Range(0, footstepSFX.Length)], Mathf.Clamp(Random.Range(-0.4f, 0.8f), 0f, 1f));
		}
	}

	[ServerRpc]
	public void SyncMeshContainerPositionServerRpc(Vector3 syncPosition, Vector3 syncRotation)
{		{
			SyncMeshContainerPositionClientRpc(syncPosition, syncRotation);
		}
}
	[ClientRpc]
	public void SyncMeshContainerPositionClientRpc(Vector3 syncPosition, Vector3 syncRotation)
{if(!base.IsOwner)			{
				meshContainerServerPosition = syncPosition;
				meshContainerServerRotation = syncRotation;
			}
}
	public void SyncMeshContainerPositionToClients()
	{
		if (Vector3.Distance(meshContainerServerPosition, base.transform.position) > 0.5f || Vector3.SignedAngle(meshContainerServerRotation, meshContainer.eulerAngles, Vector3.up) > 30f)
		{
			meshContainerServerPosition = meshContainer.position;
			meshContainerServerRotation = meshContainer.eulerAngles;
			if (base.IsServer)
			{
				SyncMeshContainerPositionClientRpc(meshContainerServerPosition, meshContainer.eulerAngles);
			}
			else
			{
				SyncMeshContainerPositionServerRpc(meshContainerServerPosition, meshContainer.eulerAngles);
			}
		}
	}

	private bool AttemptPlaceWebTrap()
	{
		for (int i = 0; i < webTraps.Count; i++)
		{
			if (Vector3.Distance(webTraps[i].transform.position, abdomen.position) < 0.6f)
			{
				return false;
			}
		}
		Vector3 direction = Vector3.Scale(Random.onUnitSphere, new Vector3(1f, Random.Range(0.5f, 1f), 1f));
		direction.y = Mathf.Min(0f, direction.y);
		ray = new Ray(abdomen.position + Vector3.up * 0.4f, direction);
		if (Physics.Raycast(ray, out rayHit, 7f, StartOfRound.Instance.collidersAndRoomMask))
		{
			if (rayHit.distance < 2f)
			{
				return false;
			}
			Debug.Log($"Got spider web raycast; end point: {rayHit.point}; {rayHit.distance}");
			Vector3 point = rayHit.point;
			if (Physics.Raycast(abdomen.position, Vector3.down, out rayHit, 10f, StartOfRound.Instance.collidersAndRoomMask))
			{
				Vector3 startPosition = rayHit.point + Vector3.up * 0.2f;
				SpawnWebTrapServerRpc(startPosition, point);
			}
		}
		return false;
	}

	[ServerRpc]
	public void SpawnWebTrapServerRpc(Vector3 startPosition, Vector3 endPosition)
{		{
			SpawnWebTrapClientRpc(startPosition, endPosition);
		}
}
	[ClientRpc]
	public void SpawnWebTrapClientRpc(Vector3 startPosition, Vector3 endPosition)
			{
				GameObject obj = Object.Instantiate(webTrapPrefab, startPosition, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
				obj.transform.LookAt(endPosition);
				SandSpiderWebTrap componentInChildren = obj.GetComponentInChildren<SandSpiderWebTrap>();
				webTraps.Add(componentInChildren);
				componentInChildren.trapID = webTraps.Count - 1;
				componentInChildren.mainScript = this;
				componentInChildren.zScale = Vector3.Distance(startPosition, endPosition) / 4f;
			}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerTripWebServerRpc(int trapID, int playerNum)
			{
				PlayerTripWebClientRpc(trapID, playerNum);
			}

	[ClientRpc]
	public void PlayerTripWebClientRpc(int trapID, int playerNum)
{		PlayerControllerB playerControllerB = StartOfRound.Instance.allPlayerScripts[playerNum];
		if (webTraps.Count - 1 >= trapID && playerControllerB.isPlayerControlled)
		{
			webTraps[trapID].webAudio.Play();
			webTraps[trapID].webAudio.PlayOneShot(hitWebSFX);
			if (webTraps[trapID].currentTrappedPlayer != null)
			{
				webTraps[trapID].currentTrappedPlayer = playerControllerB;
			}
			if (base.IsOwner)
			{
				TriggerChaseWithPlayer(playerControllerB);
			}
		}
}
	private void ChasePlayer(PlayerControllerB player)
	{
		if (base.IsOwner && PlayerIsTargetable(player))
		{
			TriggerChaseWithPlayer(player);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void BreakWebServerRpc(int trapID, int playerWhoHit)
			{
				Vector3 position = webTraps[trapID].centerOfWeb.position;
				BreakWebClientRpc(position, trapID);
				ChasePlayer(StartOfRound.Instance.allPlayerScripts[playerWhoHit]);
			}

	[ClientRpc]
	public void BreakWebClientRpc(Vector3 webPosition, int trapID)
			{
				AudioSource.PlayClipAtPoint(breakWebSFX, webPosition);
				RemoveWeb(trapID);
			}

	private void RemoveWeb(int trapID)
	{
		if (webTraps[trapID].currentTrappedPlayer != null)
		{
			if (webTraps[trapID].currentTrappedPlayer == GameNetworkManager.Instance.localPlayerController)
			{
				webTraps[trapID].currentTrappedPlayer.isMovementHindered--;
				webTraps[trapID].currentTrappedPlayer.hinderedMultiplier *= 0.5f;
			}
			webTraps[trapID].currentTrappedPlayer = null;
		}
		Object.Destroy(webTraps[trapID].gameObject.transform.parent.gameObject);
		for (int i = 0; i < webTraps.Count; i++)
		{
			if (i > trapID)
			{
				webTraps[i].trapID--;
			}
		}
		webTraps.RemoveAt(trapID);
	}

	public void TriggerChaseWithPlayer(PlayerControllerB playerScript)
	{
		if ((currentBehaviourStateIndex != 2 || watchFromDistance) && (currentBehaviourStateIndex != 1 || !(currentlyHeldBody != null) || !spooledPlayerBody) && !PathIsIntersectedByLineOfSight(playerScript.transform.position, calculatePathDistance: false, avoidLineOfSight: false) && (Vector3.Distance(playerScript.transform.position, homeNode.position) < 25f || Vector3.Distance(playerScript.transform.position, meshContainer.position) < 15f))
		{
			watchFromDistance = false;
			targetPlayer = playerScript;
			chaseTimer = 12.5f;
			SwitchToBehaviourState(2);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PlayerLeaveWebServerRpc(int trapID, int playerNum)
			{
				PlayerLeaveWebClientRpc(trapID, playerNum);
			}

	[ClientRpc]
	public void PlayerLeaveWebClientRpc(int trapID, int playerNum)
{if(webTraps[trapID].currentTrappedPlayer == StartOfRound.Instance.allPlayerScripts[playerNum])			{
				webTraps[trapID].currentTrappedPlayer = null;
				webTraps[trapID].webAudio.Stop();
			}
}
	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!isEnemyDead && !onWall)
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other, spoolingPlayerBody);
			if (playerControllerB != null && timeSinceHittingPlayer > 1f)
			{
				timeSinceHittingPlayer = 0f;
				playerControllerB.DamagePlayer(90, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
				HitPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (!isEnemyDead)
		{
			creatureSFX.PlayOneShot(hitSpiderSFX, 1f);
			WalkieTalkie.TransmitOneShotAudio(creatureSFX, hitSpiderSFX);
			enemyHP -= force;
			if (enemyHP <= 0)
			{
				KillEnemyOnOwnerClient();
			}
			else if (base.IsOwner)
			{
				TriggerChaseWithPlayer(playerWhoHit);
			}
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		CancelSpoolingBody();
		overrideAnimation = 1f;
	}

	[ServerRpc(RequireOwnership = false)]
	public void HitPlayerServerRpc(int playerId)
			{
				HitPlayerClientRpc(playerId);
			}

	[ClientRpc]
	public void HitPlayerClientRpc(int playerId)
			{
				creatureAnimator.SetTrigger("attack");
				overrideAnimation = 0.8f;
				creatureSFX.PlayOneShot(attackSFX);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, attackSFX);
			}
}
