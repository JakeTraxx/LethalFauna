using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class DoublewingAI : EnemyAI
{
	public Animator bodyAnimator;

	private int behaviourStateLastFrame = -1;

	public AudioSource flappingAudio;

	public AudioClip[] birdScreechSFX;

	public AudioClip birdHitGroundSFX;

	public AISearchRoutine roamGlide;

	private bool alertingBird;

	private float glideTime = 10f;

	private float currentGlideTime;

	private RaycastHit hit;

	private bool flyingToOtherBirdLanding;

	private float avoidingPlayer;

	public Transform Body;

	private Vector3 previousPosition;

	private float flyLayerWeight;

	[Space(5f)]
	public float maxSpeed;

	[Space(5f)]
	public float speedElevationMultiplier;

	private float randomYRot;

	private int velocityAverageCount;

	private float averageVelocity;

	private float lerpedElevation;

	private float timeSinceEnteringFlight;

	private float randomHeightOffset;

	private bool birdStunned;

	private bool oddInterval;

	private int birdNoisiness = 5;

	private float timeSinceSquawking;

	private float velocityInterval;

	public Rigidbody birdRigidbody;

	private int timesSyncingPosition;

	public override void Start()
	{
		base.Start();
		creatureAnimator.SetInteger("idleType", UnityEngine.Random.Range(0, 2));
		creatureAnimator.SetFloat("speedMultiplier", UnityEngine.Random.Range(0.73f, 1.3f));
		bodyAnimator.SetFloat("speedMultiplier", UnityEngine.Random.Range(0.8f, 1.2f));
		randomHeightOffset = (float)new System.Random(StartOfRound.Instance.randomMapSeed / (int)(base.NetworkObjectId + 1)).NextDouble();
	}

	public override void DaytimeEnemyLeave()
	{
		base.DaytimeEnemyLeave();
		if (stunNormalizedTimer < 0f && !isEnemyDead)
		{
			bodyAnimator.SetBool("flying", value: true);
			creatureAnimator.SetBool("gliding", value: true);
			bodyAnimator.SetTrigger("Leave");
			bodyAnimator.SetBool("Leaving", value: true);
		}
		StartCoroutine(flyAwayThenDespawn());
	}

	private IEnumerator flyAwayThenDespawn()
	{
		yield return new WaitForSeconds(7f);
		if (base.IsOwner)
		{
			KillEnemyOnOwnerClient(overrideDestroy: true);
		}
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if (noiseID != 911 && !isEnemyDead && !(stunNormalizedTimer > 0f))
		{
			float num = Vector3.Distance(noisePosition, base.transform.position + Vector3.up * 0.5f);
			if (Physics.Linecast(base.transform.position, noisePosition, 256))
			{
				noiseLoudness /= 2f;
			}
			float num2 = 0.01f;
			if (!(noiseLoudness / num <= num2) && currentBehaviourStateIndex == 0 && !alertingBird)
			{
				alertingBird = true;
				AlertBirdServerRpc();
			}
		}
	}

	public void StunBird()
	{
		if (birdStunned)
		{
			return;
		}
		birdStunned = true;
		agent.speed = 0f;
		DoublewingAI[] array = UnityEngine.Object.FindObjectsByType<DoublewingAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == this) && Vector3.Distance(array[i].transform.position, base.transform.position) < 8f)
			{
				array[i].AlertBirdByOther();
			}
		}
		flappingAudio.Stop();
		creatureAnimator.SetBool("stunned", value: true);
		bodyAnimator.SetBool("stunned", value: true);
	}

	public void UnstunBird()
	{
		if (birdStunned)
		{
			birdStunned = false;
			creatureAnimator.SetBool("stunned", value: false);
			bodyAnimator.SetBool("stunned", value: false);
			if (currentBehaviourStateIndex == 0)
			{
				SwitchToBehaviourStateOnLocalClient(1);
			}
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (daytimeEnemyLeaving || isEnemyDead || StartOfRound.Instance.allPlayersDead || stunNormalizedTimer > 0f)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			oddInterval = !oddInterval;
			if (oddInterval && !alertingBird && (bool)CheckLineOfSightForPlayer(80f, 8, 4))
			{
				alertingBird = true;
				AlertBirdServerRpc();
			}
			break;
		case 1:
		{
			behaviourStateLastFrame = 1;
			creatureAnimator.SetBool("gliding", value: true);
			bodyAnimator.SetBool("flying", value: true);
			agent.speed = Mathf.Clamp(agent.speed + AIIntervalTime * 4f, 5f, 19f);
			if (!flyingToOtherBirdLanding && avoidingPlayer <= 0f && !roamGlide.inProgress)
			{
				StartSearch(base.transform.position, roamGlide);
			}
			if (avoidingPlayer > 0f)
			{
				avoidingPlayer -= AIIntervalTime;
				if (Vector3.Distance(base.transform.position, destination) < 3f)
				{
					avoidingPlayer = 0f;
				}
				break;
			}
			PlayerControllerB playerControllerB = CheckLineOfSightForPlayer(80f, 10, 8);
			if (oddInterval && (bool)playerControllerB)
			{
				Transform transform = ChooseFarthestNodeFromPosition(playerControllerB.transform.position, avoidLineOfSight: false, UnityEngine.Random.Range(0, allAINodes.Length / 2));
				if (SetDestinationToPosition(transform.position))
				{
					avoidingPlayer = UnityEngine.Random.Range(10, 20);
					StopSearch(roamGlide);
				}
			}
			currentGlideTime += AIIntervalTime;
			if (!(currentGlideTime > glideTime))
			{
				break;
			}
			currentGlideTime = 0f;
			if (flyingToOtherBirdLanding)
			{
				if (!SetDestinationToPosition(destination, checkForPath: true))
				{
					flyingToOtherBirdLanding = false;
					glideTime = 5f;
					break;
				}
				if (Vector3.Distance(base.transform.position, destination) < 3f)
				{
					if (!TryLanding())
					{
						flyingToOtherBirdLanding = false;
						glideTime = 5f;
					}
					else
					{
						SwitchToBehaviourState(0);
					}
					break;
				}
			}
			for (int i = 0; i < RoundManager.Instance.SpawnedEnemies.Count; i++)
			{
				if (RoundManager.Instance.SpawnedEnemies[i].enemyType == enemyType && RoundManager.Instance.SpawnedEnemies[i].currentBehaviourStateIndex == 0 && Vector3.Distance(base.transform.position, RoundManager.Instance.SpawnedEnemies[i].transform.position) < 100f)
				{
					Vector3 randomNavMeshPositionInRadius = RoundManager.Instance.GetRandomNavMeshPositionInRadius(RoundManager.Instance.SpawnedEnemies[i].transform.position);
					if (SetDestinationToPosition(randomNavMeshPositionInRadius, checkForPath: true))
					{
						StopSearch(roamGlide);
						flyingToOtherBirdLanding = true;
						glideTime = 2f;
					}
					break;
				}
			}
			if (!flyingToOtherBirdLanding)
			{
				if (TryLanding())
				{
					SwitchToBehaviourState(0);
				}
				else
				{
					glideTime = 10f;
				}
			}
			break;
		}
		}
	}

	public bool TryLanding()
	{
		if (Physics.Raycast(new Vector3(base.transform.position.x, eye.position.y, base.transform.position.z), Vector3.down, out hit, 60f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
		{
			if (Physics.CheckSphere(hit.point, 16f, StartOfRound.Instance.playersMask, QueryTriggerInteraction.Ignore))
			{
				return false;
			}
			if (Vector3.Distance(hit.point, base.transform.position) > 1f)
			{
				if (SetDestinationToPosition(hit.point, checkForPath: true))
				{
					agent.Warp(destination);
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	[ServerRpc(RequireOwnership = false)]
	public void AlertBirdServerRpc()
			{
				AlertBirdClientRpc();
			}

	[ClientRpc]
	public void AlertBirdClientRpc()
			{
				AlertBird();
			}

	public void AlertBird()
	{
		DoublewingAI[] array = UnityEngine.Object.FindObjectsByType<DoublewingAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (!(array[i] == this) && Vector3.Distance(array[i].transform.position, base.transform.position) < 8f)
			{
				array[i].AlertBirdByOther();
			}
		}
		SwitchToBehaviourStateOnLocalClient(1);
		alertingBird = false;
	}

	public void AlertBirdByOther()
	{
		if (!daytimeEnemyLeaving)
		{
			if (base.IsServer)
			{
				SwitchToBehaviourState(1);
			}
			else
			{
				SwitchToBehaviourStateOnLocalClient(1);
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		SetFlyDirection();
		if (daytimeEnemyLeaving)
		{
			return;
		}
		timeSinceSquawking += Time.deltaTime;
		if (stunNormalizedTimer > 0f)
		{
			StunBird();
		}
		else
		{
			UnstunBird();
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (behaviourStateLastFrame != 0)
			{
				behaviourStateLastFrame = 0;
				randomYRot = UnityEngine.Random.Range(0f, 360f);
				agent.speed = 0f;
				creatureAnimator.SetBool("gliding", value: false);
				bodyAnimator.SetBool("flying", value: false);
				flyingToOtherBirdLanding = false;
				timeSinceEnteringFlight = 0f;
			}
			flyLayerWeight = Mathf.Max(0f, flyLayerWeight - Time.deltaTime * 0.28f);
			timeSinceEnteringFlight += Time.deltaTime;
			break;
		case 1:
			if (behaviourStateLastFrame != 1)
			{
				behaviourStateLastFrame = 1;
				timeSinceEnteringFlight = 0f;
				creatureAnimator.SetBool("gliding", value: true);
				bodyAnimator.SetBool("flying", value: true);
				int num = RoundManager.PlayRandomClip(creatureSFX, enemyType.audioClips);
				WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[num], 0.7f);
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 12f, 0.6f, 0, noiseIsInsideClosedShip: false, 911);
				glideTime = UnityEngine.Random.Range(8f, 20f);
			}
			timeSinceEnteringFlight += Time.deltaTime;
			flyLayerWeight = Mathf.Min(1f, flyLayerWeight + Time.deltaTime * 0.33f);
			break;
		}
	}

	private void BirdScreech()
	{
		RoundManager.PlayRandomClip(creatureVoice, birdScreechSFX);
		WalkieTalkie.TransmitOneShotAudio(creatureVoice, birdScreechSFX[UnityEngine.Random.Range(0, birdScreechSFX.Length)]);
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 12f, 0.6f, 0, noiseIsInsideClosedShip: false, 911);
	}

	public void SetFlyDirection()
	{
		if (birdStunned || isEnemyDead)
		{
			Vector3 localEulerAngles = Body.localEulerAngles;
			localEulerAngles.x = 0f;
			localEulerAngles.z = 0f;
			Body.localEulerAngles = localEulerAngles;
			return;
		}
		bool flag = false;
		flag = averageVelocity * speedElevationMultiplier < 12f;
		float num = (Body.position - previousPosition).magnitude / Time.deltaTime;
		if (daytimeEnemyLeaving)
		{
			if ((Body.position - previousPosition).sqrMagnitude >= 0f)
			{
				Body.rotation = Quaternion.Lerp(Body.rotation, Quaternion.LookRotation(Body.position - previousPosition, Vector3.up), 5f * Time.deltaTime);
			}
		}
		else if (currentBehaviourStateIndex == 0 || timeSinceEnteringFlight < 1f)
		{
			flag = false;
			Body.rotation = Quaternion.Lerp(Body.rotation, Quaternion.Euler(new Vector3(0f, randomYRot, 0f)), 10f * Time.deltaTime);
		}
		else if (averageVelocity * speedElevationMultiplier > 0f && Body.position - previousPosition != Vector3.zero)
		{
			Body.rotation = Quaternion.Lerp(Body.rotation, Quaternion.LookRotation(Body.position - previousPosition, Vector3.up), 5f * Time.deltaTime);
		}
		if (velocityInterval <= 0f)
		{
			velocityInterval = 0.1f;
			velocityAverageCount++;
			if (velocityAverageCount > 5)
			{
				averageVelocity += (num - averageVelocity) / 6f;
			}
			else
			{
				averageVelocity += num;
				if (velocityAverageCount == 5)
				{
					averageVelocity /= velocityAverageCount;
				}
			}
		}
		else
		{
			velocityInterval -= Time.deltaTime;
		}
		creatureAnimator.SetBool("flapping", flag);
		if (flag)
		{
			if (flappingAudio.volume <= 0.99f)
			{
				flappingAudio.volume = Mathf.Min(flappingAudio.volume + Time.deltaTime, 1f);
			}
			if (!flappingAudio.isPlaying)
			{
				flappingAudio.Play();
			}
		}
		else if (flappingAudio.volume >= 0.05f)
		{
			flappingAudio.volume = Mathf.Max(flappingAudio.volume - Time.deltaTime, 0f);
		}
		else
		{
			flappingAudio.Stop();
		}
		lerpedElevation = Mathf.Lerp(lerpedElevation, averageVelocity * speedElevationMultiplier / maxSpeed, Time.deltaTime * 0.5f);
		bodyAnimator.SetFloat("elevation", Mathf.Clamp(lerpedElevation * randomHeightOffset, 0.02f, 0.98f));
		previousPosition = Body.position;
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		if (base.IsOwner)
		{
			KillEnemyOnOwnerClient();
		}
	}

	public override void KillEnemy(bool destroy = false)
	{
		base.KillEnemy(destroy);
		bodyAnimator.SetBool("stunned", value: true);
		creatureAnimator.SetBool("dead", value: true);
	}

	public override void AnimationEventA()
	{
		base.AnimationEventA();
		if (base.IsServer && timeSinceSquawking > 0.7f && UnityEngine.Random.Range(0, 100) < birdNoisiness)
		{
			timeSinceSquawking = 0f;
			BirdScreechClientRpc();
		}
	}

	[ClientRpc(Delivery = RpcDelivery.Unreliable)]
	public void BirdScreechClientRpc()
			{
				BirdScreech();
			}

	public override void AnimationEventB()
	{
		base.AnimationEventB();
		creatureSFX.PlayOneShot(birdHitGroundSFX);
	}
}
