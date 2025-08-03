using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class RadMechAI : EnemyAI, IVisibleThreat
{
	public Vector3 lookVector;

	private int previousBehaviour;

	public AISearchRoutine searchForPlayers;

	[Header("Sight variables")]
	public float fov;

	[Header("Movement Variables")]
	public float timeBetweenSteps;

	public float timeToTakeStep;

	public float stepMovementSpeed;

	private float walkStepTimer = 1f;

	private bool takingStep;

	private bool leftFoot;

	private bool disableWalking;

	public Transform torsoContainer;

	public Threat targetedThreat;

	public Transform focusedThreatTransform;

	private Collider targetedThreatCollider;

	private Coroutine spotlightCoroutine;

	public Material defaultMat;

	public Material spotlightMat;

	public GameObject spotlight;

	public AudioClip spotlightOff;

	public AudioClip spotlightFlicker;

	public Collider ownCollider;

	public Transform leftFootPoint;

	public Transform rightFootPoint;

	public ParticleSystem rightFootParticle;

	public ParticleSystem leftFootParticle;

	public ParticleSystem bothFeetParticle;

	private int visibleThreatsMask = 524296;

	private bool lostCreatureInChase;

	private bool lostCreatureInChaseDebounce;

	private float losTimer;

	private bool hasLOS;

	private float syncLOSInterval;

	private bool SyncedLOSState;

	private int checkForPathInterval;

	public bool isAlerted;

	public float alertTimer;

	public AudioSource LocalLRADAudio;

	public AudioSource LocalLRADAudio2;

	private float LRADAudio2BroadcastTimer;

	public GameObject lradAudioPrefab;

	public GameObject lradAudio2Prefab;

	public Transform torsoDefaultRotation;

	public GameObject missilePrefab;

	private float explodeMissileTimer;

	private float currentMissileSpeed;

	public GameObject explosionPrefab;

	public GameObject blastMarkPrefab;

	public ParticleSystem gunArmParticle;

	public bool aimingGun;

	private bool waitingForNextShot;

	private float shootTimer;

	public Coroutine aimGunCoroutine;

	public Transform gunPoint;

	public Transform gunArm;

	public AudioClip[] shootGunSFX;

	public AudioClip[] largeExplosionSFX;

	public AudioSource explosionAudio;

	public float forwardAngleCompensation = 0.25f;

	private bool hadLOSDuringLastShot;

	public Transform defaultArmRotation;

	private float shootCooldown;

	private bool inFlyingMode;

	private bool inSky;

	private Vector3 flightDestination;

	private Coroutine flyingCoroutine;

	private Vector3 landingPosition;

	private bool finishingFlight;

	private bool changedDirectionInFlight;

	private float flyTimer;

	public Transform flyingModeEye;

	public ParticleSystem smokeRightLeg;

	public ParticleSystem smokeLeftLeg;

	public static List<GameObject> PooledBlastMarks = new List<GameObject>();

	private Vector3 previousExplosionPosition;

	[Header("Grab and torch players")]
	public ParticleSystem blowtorchParticle;

	public AudioSource blowtorchAudio;

	public bool attemptingGrab;

	private bool waitingToAttemptGrab;

	public bool inTorchPlayerAnimation;

	public Coroutine torchPlayerCoroutine;

	public Transform AttemptGrabPoint;

	private float attemptGrabTimer;

	private float timeSinceGrabbingPlayer;

	public Transform centerPosition;

	public Transform holdPlayerPoint;

	private bool blowtorchActivated;

	private bool startedUpdatePlayerPosCoroutine;

	public AudioSource flyingDistantAudio;

	public AudioSource spotlightOnAudio;

	[Header("Firing variables")]
	public int missilesFired;

	public float missileWarbleLevel = 1f;

	public float fireRate;

	public float fireRateVariance = 0.4f;

	public float missileSpeed = 0.5f;

	public float gunArmSpeed = 80f;

	[Space(3f)]
	public float shootUptime = 1.25f;

	public float shootDowntime = 0.92f;

	[Space(5f)]
	public bool chargingForward;

	public float chargeForwardSpeed;

	public GameObject startChargingEffectContainer;

	private bool startedChargeEffect;

	private float beginChargingTimer;

	public AudioSource chargeForwardAudio;

	public AudioSource engineSFX;

	public ParticleSystem chargeParticle;

	ThreatType IVisibleThreat.type => ThreatType.RadMech;

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
		num = 7;
		if (isAlerted)
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
		if (spotlight.activeSelf)
		{
			if (isAlerted)
			{
				return 1f;
			}
			return 0.85f;
		}
		return 0.5f;
	}

	public override void Start()
	{
		base.Start();
	}

	private void SpawnBlastMark(Vector3 pos, Quaternion rot)
	{
		if (PooledBlastMarks.Count >= 64)
		{
			for (int num = 63; num >= 0; num--)
			{
				if (PooledBlastMarks[num] == null)
				{
					PooledBlastMarks.RemoveAt(num);
				}
				else
				{
					PooledBlastMarks[num].transform.position = pos;
					PooledBlastMarks[num].transform.rotation = rot;
				}
			}
		}
		else
		{
			GameObject item = Object.Instantiate(blastMarkPrefab, pos, rot, RoundManager.Instance.mapPropsContainer.transform);
			PooledBlastMarks.Add(item);
		}
	}

	private void LateUpdate()
	{
		if (inSpecialAnimationWithPlayer != null)
		{
			if (inSpecialAnimationWithPlayer.isPlayerDead && inSpecialAnimationWithPlayer.deadBody != null)
			{
				inSpecialAnimationWithPlayer.deadBody.matchPositionExactly = true;
				inSpecialAnimationWithPlayer.deadBody.attachedLimb = inSpecialAnimationWithPlayer.deadBody.bodyParts[5];
				inSpecialAnimationWithPlayer.deadBody.attachedTo = holdPlayerPoint;
			}
			inSpecialAnimationWithPlayer.transform.position = holdPlayerPoint.position - holdPlayerPoint.up * 0.7f;
			inSpecialAnimationWithPlayer.transform.rotation = holdPlayerPoint.rotation;
		}
		if (base.IsServer && !LocalLRADAudio2.isPlaying && isAlerted)
		{
			if (LRADAudio2BroadcastTimer > 0.05f)
			{
				LRADAudio2BroadcastTimer = 0f;
				int num = Random.Range(4, enemyType.audioClips.Length);
				LocalLRADAudio2.clip = enemyType.audioClips[num];
				LocalLRADAudio2.Play();
				ChangeBroadcastClipClientRpc(num);
			}
			else
			{
				LRADAudio2BroadcastTimer += Time.deltaTime;
			}
		}
	}

	[ClientRpc]
	public void ChangeBroadcastClipClientRpc(int clipIndex)
{if(!base.IsServer)			{
				LocalLRADAudio2.clip = enemyType.audioClips[clipIndex];
				LocalLRADAudio2.Play();
			}
}
	public void ChangeFlightLandingPosition(Vector3 newLandingPosition)
	{
		if (base.IsServer && inSky)
		{
			RoundManager.Instance.GetNavMeshPosition(newLandingPosition, default(NavMeshHit), 8f, -353);
			if (RoundManager.Instance.GotNavMeshPositionResult)
			{
				changedDirectionInFlight = true;
				landingPosition = newLandingPosition;
				ChangeFlightLandingPositionClientRpc(newLandingPosition);
			}
		}
	}

	[ClientRpc]
	public void ChangeFlightLandingPositionClientRpc(Vector3 newLandingPosition)
{if(!base.IsServer)			{
				landingPosition = newLandingPosition;
				serverPosition = newLandingPosition;
			}
}
	public void EndFlight()
	{
		if (base.IsServer)
		{
			finishingFlight = true;
			flyTimer = 0f;
			EndFlightClientRpc();
		}
	}

	[ClientRpc]
	public void EndFlightClientRpc()
{if(!base.IsServer)			{
				finishingFlight = true;
				flyTimer = 0f;
			}
}
	public void SetChargingForward(bool setCharging)
	{
		if (chargingForward != setCharging)
		{
			SetChargingForwardOnLocalClient(setCharging);
			SetChargingForwardClientRpc(setCharging);
		}
	}

	[ClientRpc]
	public void SetChargingForwardClientRpc(bool charging)
{if(!base.IsServer)			{
				SetChargingForwardOnLocalClient(charging);
			}
}
	public void SetChargingForwardOnLocalClient(bool charging)
	{
		if (charging != chargingForward)
		{
			creatureAnimator.SetBool("charging", charging);
			startChargingEffectContainer.SetActive(charging);
			beginChargingTimer = 0f;
			startedChargeEffect = false;
			if (charging)
			{
				StompBothFeet();
				chargeParticle.Play();
				agent.angularSpeed = 120f;
				agent.acceleration = 25f;
			}
			else
			{
				agent.speed = 0f;
				chargeParticle.Stop();
				agent.angularSpeed = 220f;
				agent.acceleration = 60f;
			}
			chargingForward = charging;
		}
	}

	public void StartFlying()
	{
		if (base.IsServer && !inFlyingMode)
		{
			Vector3 vector = ChooseLandingPosition();
			if (vector == Vector3.zero)
			{
				Debug.Log($"Mech #{thisEnemyIndex} could not get a landing position!");
				disableWalking = false;
				agent.enabled = true;
				SwitchToBehaviourState(0);
			}
			else
			{
				EnterFlight(vector);
				EnterFlightClientRpc(vector);
			}
		}
	}

	private void EnterFlight(Vector3 newLandingPosition)
	{
		creatureAnimator.SetBool("Flying", value: true);
		inFlyingMode = true;
		agent.enabled = false;
		inSpecialAnimation = true;
		serverPosition = newLandingPosition;
		flyTimer = 0f;
		smokeRightLeg.Play();
		smokeLeftLeg.Play();
		changedDirectionInFlight = false;
		inSky = false;
		landingPosition = newLandingPosition;
	}

	private Vector3 ChooseLandingPosition()
	{
		for (int i = Random.Range(0, Mathf.Min(15, allAINodes.Length)); i < allAINodes.Length; i++)
		{
			Transform transform = ChooseFarthestNodeFromPosition(base.transform.position, avoidLineOfSight: false, i);
			if (!Physics.Raycast(transform.position, Vector3.up, 15f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(transform.position, default(NavMeshHit), 10f, -353);
				if (RoundManager.Instance.GotNavMeshPositionResult)
				{
					return navMeshPosition;
				}
			}
		}
		return Vector3.zero;
	}

	[ClientRpc]
	public void EnterFlightClientRpc(Vector3 newLandingPosition)
{if(!base.IsServer)			{
				EnterFlight(newLandingPosition);
			}
}
	public void SetMechAlertedToThreat()
	{
		if (!isAlerted)
		{
			isAlerted = true;
			alertTimer = 0f;
			LocalLRADAudio.Play();
		}
	}

	[ClientRpc]
	public void SetMechAlertedClientRpc()
{if(!base.IsServer)			{
				isAlerted = true;
				alertTimer = 0f;
			}
}
	public void SetAimingGun(bool setAiming)
	{
		if (base.IsServer)
		{
			if (setAiming)
			{
				shootCooldown = 0f;
			}
			aimingGun = setAiming;
			SetChargingForwardOnLocalClient(charging: false);
			if (!aimingGun)
			{
				creatureAnimator.SetBool("AimGun", value: false);
			}
			SetAimingClientRpc(setAiming);
		}
	}

	[ClientRpc]
	public void SetAimingClientRpc(bool aiming)
{if(!base.IsServer)		{
			aimingGun = aiming;
			SetChargingForwardOnLocalClient(charging: false);
			if (!aimingGun)
			{
				creatureAnimator.SetBool("AimGun", value: false);
			}
		}
}
	public void StartShootGun()
	{
		if (base.IsServer)
		{
			ShootGun(gunPoint.position, gunPoint.rotation.eulerAngles);
			ShootGunClientRpc(gunPoint.position, gunPoint.rotation.eulerAngles);
		}
	}

	[ClientRpc]
	public void ShootGunClientRpc(Vector3 startPos, Vector3 startRot)
{if(!base.IsServer)			{
				ShootGun(startPos, startRot);
			}
}
	public void ShootGun(Vector3 startPos, Vector3 startRot)
	{
		if (creatureAnimator.GetBool("AimGun"))
		{
			creatureAnimator.SetTrigger("ShootGun");
		}
		currentMissileSpeed = 0.2f;
		GameObject obj = Object.Instantiate(missilePrefab, startPos, Quaternion.Euler(startRot), RoundManager.Instance.mapPropsContainer.transform);
		missilesFired++;
		obj.GetComponent<RadMechMissile>().RadMechScript = this;
		gunArmParticle.Play();
		RoundManager.PlayRandomClip(creatureSFX, shootGunSFX);
		if (Vector3.Distance(base.transform.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position) < 16f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
	}

	public void StartExplosion(Vector3 explosionPosition, Vector3 forwardRotation, bool calledByClient = false)
	{
		if (base.IsServer)
		{
			SetExplosion(explosionPosition, forwardRotation);
			SetExplosionClientRpc(explosionPosition, forwardRotation);
		}
		else if (calledByClient)
		{
			SetExplosionServerRpc(explosionPosition, forwardRotation);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetExplosionServerRpc(Vector3 explosionPosition, Vector3 forwardRotation)
			{
				SetExplosionClientRpc(explosionPosition, forwardRotation, calledByClient: true);
			}

	[ClientRpc]
	public void SetExplosionClientRpc(Vector3 explosionPosition, Vector3 forwardRotation, bool calledByClient = false)
{if((!base.IsServer || calledByClient))			{
				SetExplosion(explosionPosition, forwardRotation);
			}
}
	public void SetExplosion(Vector3 explosionPosition, Vector3 forwardRotation)
	{
		if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, explosionPosition - forwardRotation * 0.1f) < 8f)
		{
			Landmine.SpawnExplosion(explosionPosition - forwardRotation * 0.1f, spawnExplosionEffect: true, 1f, 7f, 30, 65f, explosionPrefab);
		}
		else
		{
			Landmine.SpawnExplosion(explosionPosition - forwardRotation * 0.1f, spawnExplosionEffect: true, 1f, 7f, 30, 35f, explosionPrefab);
		}
		explosionAudio.transform.position = explosionPosition + Vector3.up * 0.5f;
		RoundManager.PlayRandomClip(explosionAudio, largeExplosionSFX);
		if (!(Vector3.Distance(previousExplosionPosition, explosionPosition) < 4f))
		{
			if (Physics.Raycast(explosionPosition - forwardRotation * 0.1f, forwardRotation, out var hitInfo, 4f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				_ = hitInfo.normal;
			}
			SpawnBlastMark(explosionPosition, Quaternion.Euler(forwardRotation));
		}
	}

	public void AimGunArmTowardsTarget()
	{
		if (!aimingGun || inSpecialAnimation)
		{
			gunArm.rotation = Quaternion.Lerp(gunArm.rotation, defaultArmRotation.rotation, 3f * Time.deltaTime);
			return;
		}
		RoundManager.Instance.tempTransform.position = gunArm.position;
		RoundManager.Instance.tempTransform.LookAt(targetedThreat.lastSeenPosition);
		RoundManager.Instance.tempTransform.rotation *= Quaternion.Euler(90f, 0f, 0f);
		RoundManager.Instance.tempTransform.localEulerAngles = new Vector3(gunArm.eulerAngles.x, RoundManager.Instance.tempTransform.localEulerAngles.y, RoundManager.Instance.tempTransform.localEulerAngles.z);
		gunArm.rotation = Quaternion.RotateTowards(gunArm.rotation, RoundManager.Instance.tempTransform.rotation, gunArmSpeed * Time.deltaTime);
		gunArm.localEulerAngles = new Vector3(0f, 0f, gunArm.localEulerAngles.z);
	}

	public void CancelSpecialAnimations()
	{
		if (aimGunCoroutine != null)
		{
			StopCoroutine(aimGunCoroutine);
		}
	}

	public override void FinishedCurrentSearchRoutine()
	{
		base.FinishedCurrentSearchRoutine();
		if (currentBehaviourStateIndex == 0 && base.IsServer && !Physics.Raycast(base.transform.position, Vector3.up, 60f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			SwitchToBehaviourState(2);
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			CheckSightForThreat();
			break;
		case 1:
			if (inSpecialAnimation || attemptingGrab)
			{
				break;
			}
			MoveTowardsThreat();
			if (!isAlerted)
			{
				if (alertTimer > 1.4f || (hasLOS && Vector3.Distance(base.transform.position, focusedThreatTransform.position) < 8f) || Vector3.Distance(base.transform.position, focusedThreatTransform.position) < 4f)
				{
					SetMechAlertedToThreat();
				}
			}
			else
			{
				if (aimingGun || !(shootCooldown <= 1f))
				{
					break;
				}
				float num = Vector3.Distance(base.transform.position, focusedThreatTransform.position);
				if (hasLOS)
				{
					if (num > 38f)
					{
						SetChargingForward(setCharging: true);
					}
					else if (!chargingForward)
					{
						SetAimingGun(setAiming: true);
					}
					else if (num < 25f)
					{
						SetAimingGun(setAiming: true);
					}
				}
			}
			break;
		}
	}

	private void LookForPlayersInFlight()
	{
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (CheckLineOfSightForPosition(StartOfRound.Instance.allPlayerScripts[i].transform.position, 35f, 120, -1f, flyingModeEye) && StartOfRound.Instance.allPlayerScripts[i].TryGetComponent<IVisibleThreat>(out var component) && !(component.GetVisibility() < 0.8f) && Physics.Raycast(StartOfRound.Instance.allPlayerScripts[i].transform.position, Vector3.down, out var hitInfo, 7f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				ChangeFlightLandingPosition(hitInfo.point);
				break;
			}
		}
	}

	public void MoveTowardsThreat()
	{
		if (targetedThreat.threatScript == null || focusedThreatTransform == null)
		{
			if (!CheckSightForThreat())
			{
				losTimer = 0f;
				SwitchToBehaviourState(0);
			}
			return;
		}
		if (!lostCreatureInChase)
		{
			if (lostCreatureInChaseDebounce)
			{
				lostCreatureInChaseDebounce = false;
				SetLostCreatureInChaseClientRpc(lostInChase: false);
				losTimer = 0f;
			}
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
			}
			hasLOS = CheckLineOfSightForPosition(focusedThreatTransform.position) && targetedThreat.threatScript.GetVisibility() > 0f;
			if (hasLOS || (losTimer < 2f && Vector3.Distance(focusedThreatTransform.position, base.transform.position) < 14f))
			{
				losTimer = 0f;
				alertTimer += AIIntervalTime * Mathf.Max(targetedThreat.threatScript.GetVisibility(), 0.3f);
			}
			else
			{
				if (!aimingGun)
				{
					losTimer += AIIntervalTime;
					alertTimer = Mathf.Max(alertTimer - AIIntervalTime, 0f);
				}
				if (CheckSightForThreat())
				{
					hasLOS = true;
					return;
				}
			}
			if (hasLOS != SyncedLOSState)
			{
				syncLOSInterval += AIIntervalTime;
				if (syncLOSInterval > 0.6f)
				{
					SyncedLOSState = hasLOS;
					SetHasLineOfSightClientRpc(hasLOS);
				}
			}
			checkForPathInterval = (checkForPathInterval + 1) % 10;
			if (!SetDestinationToPosition(targetedThreat.lastSeenPosition, checkForPathInterval == 0) || Vector3.Distance(agent.destination, base.transform.position) < 1.75f || losTimer > 10f)
			{
				lostCreatureInChase = true;
			}
			return;
		}
		if (!lostCreatureInChaseDebounce)
		{
			lostCreatureInChaseDebounce = true;
			losTimer = 0f;
			SetLostCreatureInChaseClientRpc(lostInChase: true);
			SetChargingForwardOnLocalClient(charging: false);
			if (!searchForPlayers.inProgress)
			{
				StartSearch(base.transform.position, searchForPlayers);
			}
		}
		if (CheckLineOfSightForPosition(focusedThreatTransform.position) && targetedThreat.threatScript.GetVisibility() > 0f && SetDestinationToPosition(targetedThreat.lastSeenPosition, checkForPath: true))
		{
			targetedThreat.lastSeenPosition = focusedThreatTransform.position;
			lostCreatureInChase = false;
		}
		else if (CheckSightForThreat())
		{
			lostCreatureInChase = false;
		}
		else if (!aimingGun)
		{
			losTimer += AIIntervalTime;
			if (losTimer > 10f || (losTimer > 5f && Vector3.Distance(base.transform.position, targetedThreat.lastSeenPosition) < 2f))
			{
				losTimer = 0f;
				SwitchToBehaviourState(0);
			}
		}
	}

	[ClientRpc]
	public void SetHasLineOfSightClientRpc(bool hasLineOfSight)
{if(!base.IsServer)			{
				hasLOS = hasLineOfSight;
			}
}
	[ClientRpc]
	public void SetLostCreatureInChaseClientRpc(bool lostInChase)
{if(!base.IsServer)			{
				lostCreatureInChase = lostInChase;
				SetChargingForwardOnLocalClient(charging: false);
			}
}
	public override void Update()
	{
		base.Update();
		timeSinceGrabbingPlayer += Time.deltaTime;
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		beginChargingTimer += Time.deltaTime;
		if (chargingForward)
		{
			chargeForwardAudio.volume = Mathf.Lerp(chargeForwardAudio.volume, 0.35f, 2f * Time.deltaTime);
			if (!chargeForwardAudio.isPlaying)
			{
				chargeForwardAudio.Play();
			}
		}
		else
		{
			if (!engineSFX.isPlaying && !inSky)
			{
				engineSFX.Play();
			}
			if (chargeForwardAudio.volume > 0.02f)
			{
				chargeForwardAudio.volume = Mathf.Lerp(chargeForwardAudio.volume, 0f, 2f * Time.deltaTime);
			}
			else if (chargeForwardAudio.isPlaying)
			{
				chargeForwardAudio.Stop();
			}
		}
		if (!ventAnimationFinished)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			flyingDistantAudio.volume = Mathf.Lerp(flyingDistantAudio.volume, 0f, 2f * Time.deltaTime);
			if (!inSpecialAnimation)
			{
				if (currentBehaviourStateIndex != previousBehaviour)
				{
					lostCreatureInChase = false;
					timeBetweenSteps = 0.7f;
					DisableSpotlight();
					isAlerted = false;
					alertTimer = 0f;
					LocalLRADAudio.Stop();
					LocalLRADAudio2.Stop();
					if (aimingGun && base.IsServer)
					{
						SetAimingGun(setAiming: false);
					}
					SetChargingForwardOnLocalClient(charging: false);
					previousBehaviour = currentBehaviourStateIndex;
				}
				if (!base.IsServer)
				{
					break;
				}
				if (!searchForPlayers.inProgress)
				{
					StartSearch(base.transform.position, searchForPlayers);
				}
			}
			if (base.IsServer && currentBehaviourStateIndex != previousBehaviour && previousBehaviourStateIndex == 2)
			{
				finishingFlight = false;
				if (inFlyingMode)
				{
					inFlyingMode = false;
					disableWalking = false;
				}
				if (!inTorchPlayerAnimation)
				{
					inSpecialAnimation = false;
				}
			}
			break;
		case 1:
			flyingDistantAudio.volume = Mathf.Lerp(flyingDistantAudio.volume, 0f, 2f * Time.deltaTime);
			if (inSpecialAnimation)
			{
				break;
			}
			if (currentBehaviourStateIndex != previousBehaviour)
			{
				EnableSpotlight();
				previousBehaviour = currentBehaviourStateIndex;
			}
			if (aimingGun)
			{
				shootCooldown = Mathf.Min(shootCooldown + Time.deltaTime * shootUptime, 10f);
				if (!inSpecialAnimation)
				{
					AimAndShootCycle();
				}
			}
			else
			{
				shootCooldown = Mathf.Max(shootCooldown - Time.deltaTime * shootDowntime, 0f);
			}
			if (hasLOS && focusedThreatTransform != null)
			{
				targetedThreat.lastSeenPosition = focusedThreatTransform.position + targetedThreat.threatScript.GetThreatVelocity();
			}
			if (isAlerted)
			{
				timeBetweenSteps = 0.2f;
			}
			else if (lostCreatureInChase)
			{
				timeBetweenSteps = 0.7f;
			}
			else
			{
				timeBetweenSteps = 1.1f;
			}
			break;
		case 2:
			if (currentBehaviourStateIndex != previousBehaviour)
			{
				if (previousBehaviour == 1)
				{
					DisableSpotlight();
				}
				previousBehaviour = currentBehaviourStateIndex;
			}
			if (searchForPlayers.inProgress)
			{
				StopSearch(searchForPlayers);
				break;
			}
			if (finishingFlight)
			{
				creatureAnimator.SetBool("Flying", value: false);
				flyTimer += Time.deltaTime;
				if (!inSky || flyTimer > 10f)
				{
					inFlyingMode = false;
					agent.enabled = true;
					inSpecialAnimation = false;
					disableWalking = false;
					if (smokeRightLeg.isPlaying)
					{
						smokeRightLeg.Stop();
						smokeLeftLeg.Stop();
					}
					if (base.IsServer)
					{
						disableWalking = false;
						agent.enabled = true;
						finishingFlight = false;
						SwitchToBehaviourState(0);
						return;
					}
				}
			}
			else if (inFlyingMode)
			{
				flyTimer += Time.deltaTime;
				if (inSky || flyTimer > 10f)
				{
					flyingDistantAudio.volume = Mathf.Lerp(flyingDistantAudio.volume, 1f, 2f * Time.deltaTime);
					inSky = true;
					base.transform.position = Vector3.MoveTowards(base.transform.position, landingPosition, 12f * Time.deltaTime);
					RoundManager.Instance.tempTransform.position = base.transform.position;
					RoundManager.Instance.tempTransform.LookAt(landingPosition);
					base.transform.eulerAngles = new Vector3(0f, Mathf.LerpAngle(base.transform.eulerAngles.y, RoundManager.Instance.tempTransform.eulerAngles.y, 2f * Time.deltaTime), 0f);
					if (base.IsServer && Vector3.Distance(base.transform.position, new Vector3(landingPosition.x, base.transform.position.y, landingPosition.z)) < 0.4f)
					{
						EndFlight();
					}
					else if (base.IsOwner)
					{
						if (inFlyingMode && inSky && !changedDirectionInFlight)
						{
							LookForPlayersInFlight();
						}
						break;
					}
				}
				else
				{
					flyingDistantAudio.volume = Mathf.Lerp(flyingDistantAudio.volume, 0f, 2f * Time.deltaTime);
				}
			}
			if (!base.IsServer)
			{
				return;
			}
			if (!inFlyingMode)
			{
				disableWalking = true;
				if (!takingStep)
				{
					StartFlying();
				}
			}
			break;
		}
		if (!inSpecialAnimation)
		{
			AttemptGrabIfClose();
			DoFootstepCycle();
			AimGunArmTowardsTarget();
			TurnTorsoToTarget();
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!(timeSinceGrabbingPlayer < 1f) && attemptingGrab && !inSpecialAnimation && !GameNetworkManager.Instance.localPlayerController.isInElevator)
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null && !Physics.Linecast(centerPosition.position, GameNetworkManager.Instance.localPlayerController.transform.position + Vector3.up * 0.6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				timeSinceGrabbingPlayer = 0f;
				GrabPlayerServerRpc((int)playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void GrabPlayerServerRpc(int playerId)
{if(attemptingGrab && !inTorchPlayerAnimation && !inSpecialAnimationWithPlayer && !inSpecialAnimation && !StartOfRound.Instance.allPlayerScripts[playerId].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[playerId].inAnimationWithEnemy)		{
			inTorchPlayerAnimation = true;
			inSpecialAnimationWithPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
			inSpecialAnimation = true;
			agent.enabled = false;
			attemptingGrab = false;
			int enemyYRot = (int)base.transform.eulerAngles.y;
			if (Physics.Raycast(centerPosition.position, centerPosition.forward, out var _, 6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				enemyYRot = (int)RoundManager.Instance.YRotationThatFacesTheFarthestFromPosition(centerPosition.position, 20f, 5);
			}
			GrabPlayerClientRpc(playerId, base.transform.position, enemyYRot);
		}
}
	[ClientRpc]
	public void GrabPlayerClientRpc(int playerId, Vector3 enemyPosition, int enemyYRot)
			{
				BeginTorchPlayer(StartOfRound.Instance.allPlayerScripts[playerId], enemyPosition, enemyYRot);
			}

	private void BeginTorchPlayer(PlayerControllerB playerBeingTorched, Vector3 enemyPosition, int enemyYRot)
	{
		inSpecialAnimationWithPlayer = playerBeingTorched;
		if (inSpecialAnimationWithPlayer.inSpecialInteractAnimation && inSpecialAnimationWithPlayer.currentTriggerInAnimationWith != null)
		{
			inSpecialAnimationWithPlayer.currentTriggerInAnimationWith.CancelAnimationExternally();
		}
		inSpecialAnimationWithPlayer.inSpecialInteractAnimation = true;
		inSpecialAnimationWithPlayer.inAnimationWithEnemy = this;
		inSpecialAnimationWithPlayer.isInElevator = false;
		inSpecialAnimationWithPlayer.isInHangarShipRoom = false;
		inSpecialAnimationWithPlayer.freeRotationInInteractAnimation = true;
		if (torchPlayerCoroutine != null)
		{
			StopCoroutine(torchPlayerCoroutine);
		}
		torchPlayerCoroutine = StartCoroutine(TorchPlayerAnimation(enemyPosition, enemyYRot));
	}

	private IEnumerator TorchPlayerAnimation(Vector3 enemyPosition, int enemyYRot)
	{
		creatureAnimator.SetBool("AttemptingGrab", value: true);
		creatureAnimator.SetBool("GrabSuccessful", value: true);
		creatureAnimator.SetBool("GrabUnsuccessful", value: false);
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => blowtorchActivated || Time.realtimeSinceStartup - startTime > 6f);
		startTime = Time.realtimeSinceStartup;
		while (blowtorchActivated && Time.realtimeSinceStartup - startTime < 6f)
		{
			yield return new WaitForSeconds(0.25f);
			inSpecialAnimationWithPlayer.DamagePlayer(20, hasDamageSFX: true, callRPC: true, CauseOfDeath.Burning, 6);
		}
		if (inSpecialAnimationWithPlayer == GameNetworkManager.Instance.localPlayerController && !inSpecialAnimationWithPlayer.isPlayerDead)
		{
			inSpecialAnimationWithPlayer.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Burning, 6);
		}
		yield return new WaitForSeconds(1.5f);
		CancelTorchPlayerAnimation();
		if (base.IsServer)
		{
			inTorchPlayerAnimation = false;
			inSpecialAnimationWithPlayer = null;
			inSpecialAnimation = false;
			agent.enabled = true;
		}
	}

	public void CancelTorchPlayerAnimation()
	{
		inTorchPlayerAnimation = false;
		inSpecialAnimation = false;
		disableWalking = false;
		attemptGrabTimer = 5f;
		if (base.IsServer)
		{
			agent.enabled = true;
		}
		creatureAnimator.SetBool("GrabSuccessful", value: false);
		creatureAnimator.SetBool("AttemptingGrab", value: false);
		creatureAnimator.SetBool("GrabUnsuccessful", value: false);
		if (inSpecialAnimationWithPlayer != null)
		{
			inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
			inSpecialAnimationWithPlayer.inAnimationWithEnemy = null;
			inSpecialAnimationWithPlayer.freeRotationInInteractAnimation = false;
			if (inSpecialAnimationWithPlayer.deadBody != null)
			{
				inSpecialAnimationWithPlayer.deadBody.matchPositionExactly = false;
				inSpecialAnimationWithPlayer.deadBody.attachedLimb = null;
				inSpecialAnimationWithPlayer.deadBody.attachedTo = null;
			}
			inSpecialAnimationWithPlayer.ResetZAndXRotation();
			inSpecialAnimationWithPlayer = null;
		}
		if (blowtorchActivated)
		{
			DisableBlowtorch();
		}
		if (torchPlayerCoroutine != null)
		{
			StopCoroutine(torchPlayerCoroutine);
		}
	}

	public void StartGrabAttempt()
	{
		if (currentBehaviourStateIndex != 2)
		{
			attemptingGrab = true;
			attemptGrabTimer = 1.5f;
			creatureAnimator.SetBool("AttemptingGrab", value: true);
			creatureAnimator.SetBool("GrabUnsuccessful", value: false);
			creatureAnimator.SetBool("GrabSuccessful", value: false);
			disableWalking = true;
			StartGrabAttemptClientRpc();
		}
	}

	[ClientRpc]
	public void StartGrabAttemptClientRpc()
{if(!base.IsServer)			{
				creatureAnimator.SetBool("AttemptingGrab", value: true);
				creatureAnimator.SetBool("GrabUnsuccessful", value: false);
				creatureAnimator.SetBool("GrabSuccessful", value: false);
				attemptingGrab = true;
			}
}
	public void FinishAttemptGrab()
	{
		attemptingGrab = false;
		attemptGrabTimer = 5f;
		creatureAnimator.SetBool("GrabUnsuccessful", value: true);
		creatureAnimator.SetBool("AttemptingGrab", value: false);
		disableWalking = false;
		FinishAttemptingGrabClientRpc();
	}

	[ClientRpc]
	public void FinishAttemptingGrabClientRpc()
{if(!base.IsServer)			{
				attemptingGrab = false;
				disableWalking = false;
				creatureAnimator.SetBool("GrabUnsuccessful", value: true);
				creatureAnimator.SetBool("AttemptingGrab", value: false);
			}
}
	public void AttemptGrabIfClose()
	{
		if (!base.IsServer || inSpecialAnimation || currentBehaviourStateIndex == 2)
		{
			return;
		}
		if (waitingToAttemptGrab)
		{
			if (!takingStep)
			{
				waitingToAttemptGrab = false;
				StartGrabAttempt();
			}
		}
		else if (attemptingGrab)
		{
			attemptGrabTimer -= Time.deltaTime;
			if (attemptGrabTimer < 0f)
			{
				FinishAttemptGrab();
			}
		}
		else if (attemptGrabTimer < 0f)
		{
			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
			{
				if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && Vector3.Distance(base.transform.position, StartOfRound.Instance.allPlayerScripts[i].transform.position) < 5.2f)
				{
					waitingToAttemptGrab = true;
					disableWalking = true;
					return;
				}
			}
			attemptGrabTimer = 0.4f;
		}
		else
		{
			attemptGrabTimer -= Time.deltaTime;
		}
	}

	public void AimAndShootCycle()
	{
		if (takingStep || !(stunNormalizedTimer <= 0f))
		{
			return;
		}
		creatureAnimator.SetBool("AimGun", value: true);
		if (!base.IsServer)
		{
			return;
		}
		if (shootTimer > fireRate)
		{
			float num = Vector3.Distance(targetedThreat.lastSeenPosition, base.transform.position);
			if (shootCooldown > 4.75f || chargingForward)
			{
				SetAimingGun(setAiming: false);
				return;
			}
			if (!hadLOSDuringLastShot && (!hasLOS || num < 6f))
			{
				SetAimingGun(setAiming: false);
				return;
			}
			shootTimer = 0f + Random.Range((0f - fireRateVariance) * 0.5f, fireRateVariance * 0.5f);
			StartShootGun();
			hadLOSDuringLastShot = hasLOS && num < 40f;
		}
		else
		{
			shootTimer += Time.deltaTime;
		}
	}

	public void TurnTorsoToTarget()
	{
		if (currentBehaviourStateIndex == 1 && !lostCreatureInChase && Vector3.Distance(base.transform.position, targetedThreat.lastSeenPosition) > 3f)
		{
			RoundManager.Instance.tempTransform.position = torsoContainer.position + Vector3.up * 1f;
			RoundManager.Instance.tempTransform.LookAt(targetedThreat.lastSeenPosition);
			RoundManager.Instance.tempTransform.rotation *= Quaternion.Euler(0f, 90f, 0f);
			float num = 0f;
			if (aimingGun)
			{
				num = Vector3.Angle(targetedThreat.lastSeenPosition - base.transform.position, base.transform.forward) * forwardAngleCompensation;
			}
			RoundManager.Instance.tempTransform.localEulerAngles = new Vector3(torsoContainer.eulerAngles.x, torsoContainer.eulerAngles.y, RoundManager.Instance.tempTransform.localEulerAngles.z + num);
			torsoContainer.rotation = Quaternion.RotateTowards(torsoContainer.rotation, RoundManager.Instance.tempTransform.rotation, 80f * Time.deltaTime);
		}
		else
		{
			torsoContainer.rotation = Quaternion.Lerp(torsoContainer.rotation, torsoDefaultRotation.rotation, 3f * Time.deltaTime);
		}
	}

	public bool CheckSightForThreat()
	{
		if (!base.IsServer)
		{
			return false;
		}
		int num = Physics.OverlapSphereNonAlloc(eye.position + eye.forward * 58f + -eye.up * 10f, 60f, RoundManager.Instance.tempColliderResults, visibleThreatsMask, QueryTriggerInteraction.Collide);
		Collider collider = null;
		RaycastHit hitInfo;
		IVisibleThreat component2;
		for (int i = 0; i < num; i++)
		{
			if (RoundManager.Instance.tempColliderResults[i] == ownCollider)
			{
				continue;
			}
			if (RoundManager.Instance.tempColliderResults[i] == targetedThreatCollider && currentBehaviourStateIndex == 1)
			{
				collider = RoundManager.Instance.tempColliderResults[i];
				continue;
			}
			float num2 = Vector3.Distance(eye.position, RoundManager.Instance.tempColliderResults[i].transform.position);
			float num3 = Vector3.Angle(RoundManager.Instance.tempColliderResults[i].transform.position - eye.position, eye.forward);
			if (num2 > 2f && num3 > fov)
			{
				continue;
			}
			if (Physics.Linecast(base.transform.position + Vector3.up * 0.7f, RoundManager.Instance.tempColliderResults[i].transform.position + Vector3.up * 0.5f, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				if (debugEnemyAI)
				{
					Debug.DrawRay(hitInfo.point, Vector3.up * 0.5f, Color.magenta, AIIntervalTime);
				}
				continue;
			}
			EnemyAI component = RoundManager.Instance.tempColliderResults[i].transform.GetComponent<EnemyAI>();
			if ((!(component != null) || !(component.GetType() == typeof(RadMechAI))) && RoundManager.Instance.tempColliderResults[i].transform.TryGetComponent<IVisibleThreat>(out component2))
			{
				float visibility = component2.GetVisibility();
				if (!(visibility < 0.2f) && (!(visibility <= 0.58f) || !(num2 > 30f)))
				{
					targetedThreatCollider = RoundManager.Instance.tempColliderResults[i];
					SetTargetedThreat(component2, RoundManager.Instance.tempColliderResults[i].transform.position + Vector3.up * 0.5f, num2);
					focusedThreatTransform = component2.GetThreatTransform();
					NetworkObject component3 = focusedThreatTransform.gameObject.GetComponent<NetworkObject>();
					SwitchToBehaviourStateOnLocalClient(1);
					SetTargetToThreatClientRpc(component3, targetedThreat.lastSeenPosition);
					return true;
				}
			}
		}
		if ((bool)collider)
		{
			if (Physics.Linecast(base.transform.position + Vector3.up * 0.7f, collider.transform.position + Vector3.up * 0.5f, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			if (!collider.transform.TryGetComponent<IVisibleThreat>(out component2))
			{
				return false;
			}
			if (component2.GetVisibility() < 0.2f)
			{
				return false;
			}
			return true;
		}
		return false;
	}

	[ClientRpc]
	public void SetTargetToThreatClientRpc(NetworkObjectReference netObject, Vector3 lastSeenPos)
{if(base.IsServer)		{
			return;
		}
		if (netObject.TryGet(out var networkObject))
		{
			if (!networkObject.transform.TryGetComponent<IVisibleThreat>(out var component))
			{
				Debug.LogError("Error: RadMech could not get IVisibleThreat in transform of network object sent in RPC (SetTargetToThreatClientRpc)");
				return;
			}
			focusedThreatTransform = component.GetThreatTransform();
			float dist = Vector3.Distance(eye.position, focusedThreatTransform.position);
			SetTargetedThreat(component, lastSeenPos, dist);
		}
		else
		{
			Debug.LogError($"Error: RadMech could not find threat NetworkObject sent by RPC through reference; ID: {netObject.NetworkObjectId}");
		}
		SwitchToBehaviourStateOnLocalClient(1);
}
	public void SetTargetedThreat(IVisibleThreat newThreat, Vector3 lastSeenPos, float dist)
	{
		targetedThreat.type = newThreat.type;
		targetedThreat.timeLastSeen = Time.realtimeSinceStartup;
		targetedThreat.lastSeenPosition = lastSeenPos;
		targetedThreat.distanceToThreat = dist;
		targetedThreat.threatLevel = newThreat.GetThreatLevel(eye.position);
		targetedThreat.threatScript = newThreat;
	}

	private void DoFootstepCycle()
	{
		if (!base.IsServer)
		{
			return;
		}
		if (chargingForward && !disableWalking && !aimingGun)
		{
			if (beginChargingTimer < 0.4f)
			{
				agent.speed = 4f;
				return;
			}
			agent.speed = chargeForwardSpeed;
			if (startedChargeEffect)
			{
				return;
			}
			startedChargeEffect = true;
			startChargingEffectContainer.SetActive(value: true);
			engineSFX.Stop();
			float num = Vector3.Distance(base.transform.position - base.transform.forward * 5f, GameNetworkManager.Instance.localPlayerController.transform.position);
			if (num < 25f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
				if (num < 12f)
				{
					Landmine.SpawnExplosion(base.transform.position + Vector3.up * 0.2f, spawnExplosionEffect: false, 2f, 5f, 30, 45f);
				}
			}
		}
		else if (takingStep)
		{
			if (walkStepTimer <= 0f)
			{
				walkStepTimer = timeBetweenSteps;
				takingStep = false;
				agent.speed = 0f;
			}
			else
			{
				agent.speed = stepMovementSpeed;
				walkStepTimer -= Time.deltaTime;
			}
		}
		else if (!disableWalking && !aimingGun && !(stunNormalizedTimer > 0f))
		{
			if (walkStepTimer <= 0f)
			{
				walkStepTimer = timeToTakeStep;
				leftFoot = !leftFoot;
				takingStep = true;
				TakeStepForwardAnimation(leftFoot);
			}
			else
			{
				walkStepTimer -= Time.deltaTime;
			}
		}
	}

	private void TakeStepForwardAnimation(bool leftFootForward)
	{
		if (base.IsServer)
		{
			creatureAnimator.SetBool("leftFootForward", leftFootForward);
			if (leftFootForward)
			{
				TakeLeftStepForwardClientRpc();
			}
			else
			{
				TakeRightStepForwardClientRpc();
			}
		}
	}

	[ClientRpc]
	public void TakeLeftStepForwardClientRpc()
			{
				creatureAnimator.SetBool("leftFootForward", value: true);
			}

	[ClientRpc]
	public void TakeRightStepForwardClientRpc()
			{
				creatureAnimator.SetBool("leftFootForward", value: false);
			}

	public void EnableBlowtorch()
	{
		blowtorchParticle.gameObject.SetActive(value: false);
		blowtorchParticle.gameObject.SetActive(value: true);
		blowtorchParticle.Play();
		blowtorchAudio.Play();
		blowtorchActivated = true;
	}

	public void DisableBlowtorch()
	{
		blowtorchParticle.Stop();
		blowtorchAudio.Stop();
		blowtorchActivated = false;
	}

	public void DisableThrusterSmoke()
	{
		smokeLeftLeg.Stop();
		smokeRightLeg.Stop();
	}

	public void EnableThrusterSmoke()
	{
		smokeLeftLeg.Play();
		smokeRightLeg.Play();
	}

	public void HasEnteredSky()
	{
		inSky = true;
		engineSFX.Stop();
	}

	public void FinishFlyingAnimation()
	{
		inSky = false;
	}

	public void FlickerFace()
	{
		creatureVoice.PlayOneShot(spotlightFlicker);
		if (spotlightCoroutine != null)
		{
			StopCoroutine(spotlightCoroutine);
			spotlightCoroutine = null;
		}
		spotlightCoroutine = StartCoroutine(flickerSpotlightAnim());
	}

	private IEnumerator flickerSpotlightAnim()
	{
		for (int i = 0; i < 5; i++)
		{
			spotlight.SetActive(value: true);
			skinnedMeshRenderers[0].sharedMaterial = spotlightMat;
			yield return new WaitForSeconds(Random.Range(0.06f, 0.3f));
			spotlight.SetActive(value: false);
			skinnedMeshRenderers[0].sharedMaterial = defaultMat;
		}
	}

	public void EnableSpotlight()
	{
		spotlight.SetActive(value: true);
		skinnedMeshRenderers[0].sharedMaterial = spotlightMat;
		spotlightOnAudio.Play();
	}

	public void DisableSpotlight()
	{
		spotlight.SetActive(value: false);
		skinnedMeshRenderers[0].sharedMaterial = defaultMat;
		creatureVoice.PlayOneShot(spotlightOff);
	}

	public void StompLeftFoot()
	{
		Stomp(leftFootPoint, leftFootParticle);
	}

	public void StompRightFoot()
	{
		Stomp(rightFootPoint, rightFootParticle);
	}

	public void StompBothFeet()
	{
		Stomp(base.transform, leftFootParticle, rightFootParticle, 10f);
	}

	private void Stomp(Transform stompTransform, ParticleSystem particle, ParticleSystem particle2 = null, float radius = 5f)
	{
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		float num = Vector3.Distance(localPlayerController.transform.position, stompTransform.position);
		particle.Play();
		if (particle2 != null)
		{
			particle2.Play();
		}
		RoundManager.PlayRandomClip(creatureSFX, enemyType.audioClips, randomize: true, 1f, 1115, 4);
		if (num < 12f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
		else if (num < 24f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
		Vector3 vector = Vector3.Normalize(localPlayerController.gameplayCamera.transform.position - stompTransform.position) * 15f / Vector3.Distance(localPlayerController.gameplayCamera.transform.position, stompTransform.position);
		if (num < radius)
		{
			if ((double)num < (double)radius * 0.175)
			{
				localPlayerController.DamagePlayer(70, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing, 0, fallDamage: false, Vector3.down * 40f);
			}
			else if (num < radius * 0.5f)
			{
				localPlayerController.DamagePlayer(30, hasDamageSFX: true, callRPC: true, CauseOfDeath.Crushing, 0, fallDamage: false, Vector3.down * 40f);
			}
			localPlayerController.externalForceAutoFade += vector;
		}
	}
}
