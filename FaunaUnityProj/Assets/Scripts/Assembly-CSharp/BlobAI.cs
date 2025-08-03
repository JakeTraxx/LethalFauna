using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BlobAI : EnemyAI
{
	[Header("Fluid simulation")]
	public Transform centerPoint;

	public Transform[] SlimeRaycastTargets;

	public Rigidbody[] SlimeBones;

	private Vector3[] SlimeBonePositions = new Vector3[8];

	public float slimeRange = 8f;

	public float currentSlimeRange;

	private float[] maxDistanceForSlimeRays = new float[8];

	private float[] distanceOfRaysLastFrame = new float[8];

	private int partsMovingUpSlope;

	private Ray slimeRay;

	private RaycastHit slimeRayHit;

	private RaycastHit slimePlayerRayHit;

	private float timeSinceHittingLocalPlayer;

	[Header("Behaviors")]
	public AISearchRoutine searchForPlayers;

	private float tamedTimer;

	private float angeredTimer;

	private Material thisSlimeMaterial;

	private float slimeJiggleAmplitude;

	private float slimeJiggleDensity;

	[Header("SFX")]
	public AudioSource movableAudioSource;

	public AudioClip agitatedSFX;

	public AudioClip jiggleSFX;

	public AudioClip hitSlimeSFX;

	public AudioClip killPlayerSFX;

	public AudioClip idleSFX;

	private Collider[] ragdollColliders;

	private Coroutine eatPlayerBodyCoroutine;

	private DeadBodyInfo bodyBeingCarried;

	private int slimeMask = 268470529;

	public Mesh emptySuitMesh;

	public override void Start()
	{
		ragdollColliders = new Collider[4];
		base.Start();
		thisSlimeMaterial = skinnedMeshRenderers[0].material;
		for (int i = 0; i < maxDistanceForSlimeRays.Length; i++)
		{
			maxDistanceForSlimeRays[i] = 3.7f;
			SlimeBonePositions[i] = SlimeBones[i].transform.position;
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (!isEnemyDead && !StartOfRound.Instance.allPlayersDead)
		{
			if (TargetClosestPlayer(4f))
			{
				StopSearch(searchForPlayers);
				movingTowardsTargetPlayer = true;
			}
			else
			{
				movingTowardsTargetPlayer = false;
				StartSearch(base.transform.position, searchForPlayers);
			}
		}
	}

	private void SimulateSurfaceTensionInRaycasts(int i)
	{
		float num = distanceOfRaysLastFrame[(i + 1) % SlimeRaycastTargets.Length];
		float num2 = ((i != 0) ? distanceOfRaysLastFrame[i - 1] : distanceOfRaysLastFrame[SlimeRaycastTargets.Length - 1]);
		float num3 = Mathf.Clamp((num2 + num) / 2f, 0.5f, 200f);
		float num4 = 1f;
		if (num3 < 2f)
		{
			num4 = 2f;
		}
		maxDistanceForSlimeRays[i] = Mathf.Clamp(num3 * 2f * num4, 0f, currentSlimeRange);
	}

	private void FixedUpdate()
	{
		if (!ventAnimationFinished)
		{
			return;
		}
		for (int i = 0; i < SlimeBonePositions.Length; i++)
		{
			if (Vector3.Distance(centerPoint.position, SlimeBonePositions[i]) > distanceOfRaysLastFrame[i])
			{
				SlimeBones[i].MovePosition(Vector3.Lerp(SlimeBones[i].position, SlimeBonePositions[i], 10f * Time.deltaTime));
			}
			else
			{
				SlimeBones[i].MovePosition(Vector3.Lerp(SlimeBones[i].position, SlimeBonePositions[i], 5f * Time.deltaTime));
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if (!ventAnimationFinished || !(creatureAnimator != null))
		{
			return;
		}
		creatureAnimator.enabled = false;
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		timeSinceHittingLocalPlayer += Time.deltaTime;
		partsMovingUpSlope = 0;
		Vector3 vector = serverPosition;
		for (int i = 0; i < SlimeRaycastTargets.Length; i++)
		{
			Vector3 direction = SlimeRaycastTargets[i].position - centerPoint.position;
			slimeRay = new Ray(vector, direction);
			RaycastCollisionWithPlayers(Vector3.Distance(vector, SlimeBones[i].transform.position));
			if (Physics.Raycast(slimeRay, out slimeRayHit, maxDistanceForSlimeRays[i], slimeMask, QueryTriggerInteraction.Ignore))
			{
				MoveSlimeBoneToRaycastHit(0f, i);
				continue;
			}
			Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(slimeRay.GetPoint(maxDistanceForSlimeRays[i]));
			SlimeBonePositions[i] = Vector3.Lerp(SlimeBonePositions[i], navMeshPosition, 1f * Time.deltaTime);
			distanceOfRaysLastFrame[i] = maxDistanceForSlimeRays[i];
		}
		if (stunNormalizedTimer > 0f)
		{
			thisSlimeMaterial.SetFloat("_Frequency", 4f);
			slimeJiggleDensity = Mathf.Lerp(slimeJiggleDensity, 1f, 10f * Time.deltaTime);
			thisSlimeMaterial.SetFloat("_Ripple_Density", slimeJiggleDensity);
			slimeJiggleAmplitude = Mathf.Lerp(slimeJiggleAmplitude, 0.17f, 10f * Time.deltaTime);
			thisSlimeMaterial.SetFloat("_Amplitude", slimeJiggleAmplitude);
			agent.speed = 0f;
			currentSlimeRange = Mathf.Lerp(currentSlimeRange, 2f, Time.deltaTime * 4f);
			angeredTimer = 7f;
			return;
		}
		if (angeredTimer > 0f)
		{
			angeredTimer -= Time.deltaTime;
			currentSlimeRange = Mathf.Lerp(currentSlimeRange, slimeRange + 6f, Time.deltaTime * 3f);
			thisSlimeMaterial.SetFloat("_Frequency", 3f);
			slimeJiggleDensity = Mathf.Lerp(slimeJiggleDensity, 1f, 10f * Time.deltaTime);
			thisSlimeMaterial.SetFloat("_Ripple_Density", slimeJiggleDensity);
			slimeJiggleAmplitude = Mathf.Lerp(slimeJiggleAmplitude, 0.14f, 10f * Time.deltaTime);
			thisSlimeMaterial.SetFloat("_Amplitude", slimeJiggleAmplitude);
			if (creatureSFX.clip != agitatedSFX)
			{
				creatureSFX.clip = agitatedSFX;
				creatureSFX.Play();
			}
			if (base.IsOwner)
			{
				agent.stoppingDistance = 0.1f;
				agent.speed = 0.6f;
			}
			return;
		}
		if (tamedTimer > 0f)
		{
			tamedTimer -= Time.deltaTime;
			currentSlimeRange = 1.5f;
			thisSlimeMaterial.SetFloat("_Frequency", 4.3f);
			slimeJiggleDensity = Mathf.Lerp(slimeJiggleDensity, 1.3f, 10f * Time.deltaTime);
			thisSlimeMaterial.SetFloat("_Ripple_Density", slimeJiggleDensity);
			slimeJiggleAmplitude = Mathf.Lerp(slimeJiggleAmplitude, 0.2f, 10f * Time.deltaTime);
			thisSlimeMaterial.SetFloat("_Amplitude", slimeJiggleAmplitude);
			if (creatureSFX.clip != jiggleSFX)
			{
				creatureSFX.clip = jiggleSFX;
				creatureSFX.Play();
			}
			if (base.IsOwner)
			{
				agent.stoppingDistance = 5f;
				agent.speed = Mathf.Lerp(agent.speed, 3f, 0.7f * Time.deltaTime);
			}
			return;
		}
		if (partsMovingUpSlope >= 2)
		{
			currentSlimeRange = Mathf.Clamp(slimeRange / 2f, 1.5f, 100f);
		}
		else
		{
			currentSlimeRange = slimeRange;
		}
		thisSlimeMaterial.SetFloat("_Frequency", 2f);
		slimeJiggleDensity = Mathf.Lerp(slimeJiggleDensity, 0.6f, 10f * Time.deltaTime);
		thisSlimeMaterial.SetFloat("_Ripple_Density", slimeJiggleDensity);
		slimeJiggleAmplitude = Mathf.Lerp(slimeJiggleAmplitude, 0.15f, 10f * Time.deltaTime);
		thisSlimeMaterial.SetFloat("_Amplitude", slimeJiggleAmplitude);
		if (creatureSFX.clip != idleSFX)
		{
			creatureSFX.clip = idleSFX;
			creatureSFX.Play();
		}
		if (base.IsOwner)
		{
			agent.stoppingDistance = 0.1f;
			agent.speed = 0.5f;
		}
	}

	private void MoveSlimeBoneToRaycastHit(float currentRangeOfRaycast, int i)
	{
		float num = 1.8f;
		if (slimeRayHit.distance + currentRangeOfRaycast < distanceOfRaysLastFrame[i])
		{
			num = 5f;
		}
		SlimeBonePositions[i] = Vector3.Lerp(SlimeBonePositions[i], slimeRay.GetPoint(slimeRayHit.distance), num * Time.deltaTime);
		distanceOfRaysLastFrame[i] = slimeRayHit.distance + currentRangeOfRaycast;
	}

	private void RaycastCollisionWithPlayers(float maxDistance)
	{
		maxDistance -= 1.55f;
		if (Physics.SphereCast(slimeRay, 0.7f, out slimePlayerRayHit, maxDistance, 2312) && slimePlayerRayHit.collider.gameObject.CompareTag("Player"))
		{
			OnCollideWithPlayer(slimePlayerRayHit.collider);
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (timeSinceHittingLocalPlayer < 0.25f || (tamedTimer > 0f && angeredTimer < 0f))
		{
			return;
		}
		PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
		if (playerControllerB != null)
		{
			timeSinceHittingLocalPlayer = 0f;
			playerControllerB.DamagePlayer(35);
			if (playerControllerB.isPlayerDead)
			{
				SlimeKillPlayerEffectServerRpc((int)playerControllerB.playerClientId);
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SlimeKillPlayerEffectServerRpc(int playerKilled)
			{
				SlimeKillPlayerEffectClientRpc(playerKilled);
			}

	[ClientRpc]
	public void SlimeKillPlayerEffectClientRpc(int playerKilled)
{		{
			creatureSFX.PlayOneShot(killPlayerSFX);
			angeredTimer = 0f;
			if (eatPlayerBodyCoroutine == null)
			{
				eatPlayerBodyCoroutine = StartCoroutine(eatPlayerBody(playerKilled));
			}
		}
}
	private IEnumerator eatPlayerBody(int playerKilled)
	{
		yield return null;
		PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerKilled];
		float startTime = Time.realtimeSinceStartup;
		yield return new WaitUntil(() => playerScript.deadBody != null || Time.realtimeSinceStartup - startTime > 2f);
		if (playerScript.deadBody == null)
		{
			Debug.Log("Blob: Player body was not spawned or found within 2 seconds.");
			yield break;
		}
		playerScript.deadBody.attachedLimb = playerScript.deadBody.bodyParts[6];
		playerScript.deadBody.attachedTo = centerPoint;
		playerScript.deadBody.matchPositionExactly = false;
		yield return new WaitForSeconds(2f);
		playerScript.deadBody.attachedTo = null;
		playerScript.deadBody.ChangeMesh(emptySuitMesh);
	}

	public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
		angeredTimer = 18f;
		if (playerWhoHit != null)
		{
			movableAudioSource.transform.position = playerWhoHit.gameplayCamera.transform.position + playerWhoHit.gameplayCamera.transform.forward * 1.5f;
		}
		else
		{
			movableAudioSource.transform.position = centerPoint.position;
		}
		movableAudioSource.PlayOneShot(hitSlimeSFX);
	}

	public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesPlayedInOneSpot = 0, int noiseID = 0)
	{
		base.DetectNoise(noisePosition, noiseLoudness, timesPlayedInOneSpot, noiseID);
		if (noiseID == 5 && !Physics.Linecast(base.transform.position, noisePosition, StartOfRound.Instance.collidersAndRoomMask) && Vector3.Distance(base.transform.position, noisePosition) < 12f)
		{
			tamedTimer = 2f;
		}
	}
}
