using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

public class DeadBodyInfo : MonoBehaviour
{
	public int playerObjectId;

	public bool setMaterialToPlayerSuit = true;

	public PlayerControllerB playerScript;

	public Rigidbody[] bodyParts;

	[Space(3f)]
	public Rigidbody attachedLimb;

	public Transform attachedTo;

	[Space(2f)]
	public Rigidbody secondaryAttachedLimb;

	public Transform secondaryAttachedTo;

	[Space(5f)]
	public int timesOutOfBounds;

	public Vector3 spawnPosition;

	[Space(3f)]
	private Vector3 forceDirection;

	public float maxVelocity;

	public float speedMultiplier;

	public bool matchPositionExactly = true;

	public bool wasMatchingPosition;

	private Rigidbody previousAttachedLimb;

	[Space(3f)]
	public bool bodyBleedingHeavily = true;

	private Vector3 previousBodyPosition;

	private int bloodAmount;

	private int maxBloodAmount = 30;

	public GameObject[] bodyBloodDecals;

	[Space(3f)]
	private bool bodyMovedThisFrame;

	private float syncBodyPositionTimer;

	private bool serverSyncedPositionWithClients;

	public bool seenByLocalPlayer;

	public AudioSource bodyAudio;

	private float velocityLastFrame;

	public Transform radarDot;

	private float timeSinceLastCollisionSFX;

	public bool parentedToShip;

	public bool detachedHead;

	public Transform detachedHeadObject;

	public Vector3 detachedHeadVelocity;

	public ParticleSystem bloodSplashParticle;

	public ParticleSystem beamUpParticle;

	public ParticleSystem beamOutParticle;

	public AudioSource playAudioOnDeath;

	public CauseOfDeath causeOfDeath;

	private float resetBodyPartsTimer;

	public GrabbableObject grabBodyObject;

	private bool bodySetToKinematic;

	public bool lerpBeforeMatchingPosition;

	private float moveToExactPositionTimer;

	public bool canBeGrabbedBackByPlayers;

	public bool isInShip;

	public bool deactivated;

	public bool overrideSpawnPosition;

	public Transform physicsParent;

	private Collider physicsParentCollider;

	public bool isParentedToPhysicsRegion;

	public Light nightVisionRadar;

	private void Awake()
	{
		Debug.Log($"Body spawned with id {playerObjectId}: {base.gameObject.name}");
	}

	private void FloatBodyToWaterSurface()
	{
		for (int i = 0; i < bodyParts.Length; i++)
		{
			float num = playerScript.underwaterCollider.transform.position.y + playerScript.underwaterCollider.bounds.extents.y - bodyParts[i].transform.position.y;
			bodyParts[i].AddForce(-Physics.gravity * num * 5f, ForceMode.Force);
			bodyParts[i].drag = 2.5f;
			bodyParts[i].useGravity = false;
		}
	}

	private void StopFloatingBody()
	{
		playerScript.underwaterCollider = null;
		for (int i = 0; i < bodyParts.Length; i++)
		{
			bodyParts[i].drag = 0f;
			bodyParts[i].useGravity = true;
		}
	}

	private void FixedUpdate()
	{
		if (!deactivated && !wasMatchingPosition && causeOfDeath == CauseOfDeath.Drowning && playerScript != null && playerScript.underwaterCollider != null && !isInShip)
		{
			FloatBodyToWaterSurface();
		}
	}

	private void OnDestroy()
	{
		if (grabBodyObject != null)
		{
			grabBodyObject.grabbable = false;
		}
	}

	private void Start()
	{
		Debug.DrawRay(base.transform.position, base.transform.up, Color.blue, 10f);
		spawnPosition = base.transform.position;
		previousBodyPosition = Vector3.zero;
		if (StartOfRound.Instance != null)
		{
			playerScript = StartOfRound.Instance.allPlayerScripts[playerObjectId];
			Debug.Log($"PlayerScript set for body with id {playerObjectId} {base.gameObject.name}");
			if (setMaterialToPlayerSuit)
			{
				base.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = StartOfRound.Instance.unlockablesList.unlockables[playerScript.currentSuitID].suitMaterial;
				base.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().renderingLayerMask = 0x201u | (uint)(1 << playerObjectId + 12);
			}
			for (int i = 0; i < playerScript.bodyParts.Length; i++)
			{
				if (!overrideSpawnPosition)
				{
					bodyParts[i].position = playerScript.bodyParts[i].position;
				}
				if (playerObjectId == 0)
				{
					bodyParts[i].gameObject.tag = "PlayerRagdoll";
				}
				else
				{
					bodyParts[i].gameObject.tag = $"PlayerRagdoll{playerObjectId}";
				}
			}
		}
		if (detachedHead)
		{
			if (RoundManager.Instance != null && RoundManager.Instance.mapPropsContainer != null)
			{
				detachedHeadObject.SetParent(RoundManager.Instance.mapPropsContainer.transform);
			}
			detachedHeadObject.GetComponent<Rigidbody>().AddForce(detachedHeadVelocity * 350f, ForceMode.Impulse);
		}
		if (bloodSplashParticle != null)
		{
			ParticleSystem.MainModule main = bloodSplashParticle.main;
			main.customSimulationSpace = RoundManager.Instance.mapPropsContainer.transform;
		}
		if ((bool)playAudioOnDeath)
		{
			playAudioOnDeath.Play();
			WalkieTalkie.TransmitOneShotAudio(playAudioOnDeath, playAudioOnDeath.clip);
		}
	}

	public void SetPhysicsParent(Transform setPhysicsParent, Collider physicsCollider = null)
	{
		physicsParent = setPhysicsParent;
		if (!(physicsCollider == null))
		{
			return;
		}
		new List<Collider>();
		PlayerPhysicsRegion[] componentsInChildren = physicsParent.GetComponentsInChildren<PlayerPhysicsRegion>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].physicsCollider == null)
			{
				Debug.LogError("The player physics region '" + componentsInChildren[i].gameObject.name + "' is missing physicsCollider property!");
			}
			else if (componentsInChildren[i].physicsCollider.ClosestPoint(bodyParts[5].position) == bodyParts[5].position)
			{
				physicsParentCollider = componentsInChildren[i].physicsCollider;
				break;
			}
		}
	}

	private void Update()
	{
		if (deactivated)
		{
			isInShip = false;
			if (grabBodyObject != null && grabBodyObject.grabbable)
			{
				grabBodyObject.grabbable = false;
				grabBodyObject.grabbableToEnemies = false;
				grabBodyObject.EnablePhysics(enable: false);
				GetComponentInChildren<ScanNodeProperties>().GetComponent<Collider>().enabled = false;
			}
			return;
		}
		isInShip = parentedToShip || (grabBodyObject != null && grabBodyObject.isHeld && grabBodyObject.playerHeldBy != null && grabBodyObject.playerHeldBy.isInElevator);
		if (attachedLimb != null && attachedTo != null && matchPositionExactly)
		{
			syncBodyPositionTimer = 5f;
			ResetBodyPositionIfTooFarFromAttachment();
			resetBodyPartsTimer += Time.deltaTime;
			if (resetBodyPartsTimer >= 0.25f)
			{
				resetBodyPartsTimer = 0f;
				EnableCollisionOnBodyParts();
			}
		}
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
		{
			return;
		}
		DetectIfSeenByLocalPlayer();
		DetectBodyMovedDistanceThreshold();
		if (bodyMovedThisFrame)
		{
			syncBodyPositionTimer = 5f;
			if (bodyBleedingHeavily && bloodAmount < maxBloodAmount)
			{
				bloodAmount++;
				playerScript.DropBlood(Vector3.down);
			}
		}
		if (attachedLimb != null && attachedTo != null)
		{
			syncBodyPositionTimer = 5f;
		}
		else if (GameNetworkManager.Instance.localPlayerController.IsOwnedByServer && !serverSyncedPositionWithClients)
		{
			if (syncBodyPositionTimer >= 0f)
			{
				syncBodyPositionTimer -= Time.deltaTime;
			}
			else
			{
				if (Physics.CheckSphere(base.transform.position, 30f, StartOfRound.Instance.playersMask))
				{
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
					{
						if (StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled && !Physics.Linecast(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position, base.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
						{
							syncBodyPositionTimer = 0.3f;
							return;
						}
					}
				}
				serverSyncedPositionWithClients = true;
				playerScript.SyncBodyPositionWithClients();
			}
		}
		if (timeSinceLastCollisionSFX <= 0.5f)
		{
			timeSinceLastCollisionSFX += Time.deltaTime;
			return;
		}
		timeSinceLastCollisionSFX = 0f;
		velocityLastFrame = bodyParts[5].velocity.sqrMagnitude;
	}

	public void DetectIfSeenByLocalPlayer()
	{
		if (seenByLocalPlayer)
		{
			return;
		}
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		Rigidbody rigidbody = null;
		float num = Vector3.Distance(localPlayerController.gameplayCamera.transform.position, base.transform.position);
		for (int i = 0; i < bodyParts.Length; i++)
		{
			if (bodyParts[i] == rigidbody)
			{
				continue;
			}
			rigidbody = bodyParts[i];
			if (localPlayerController.HasLineOfSightToPosition(bodyParts[i].transform.position, 30f / (num / 5f)))
			{
				if (num < 10f)
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.9f);
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.55f);
				}
				seenByLocalPlayer = true;
				break;
			}
		}
	}

	private void LateUpdate()
	{
		if (deactivated)
		{
			radarDot.gameObject.SetActive(value: false);
			if (parentedToShip)
			{
				parentedToShip = false;
				base.transform.SetParent(null, worldPositionStays: true);
			}
			return;
		}
		radarDot.eulerAngles = new Vector3(0f, radarDot.eulerAngles.y, 0f);
		if (attachedLimb == null || attachedTo == null || attachedTo.parent == base.transform)
		{
			if (grabBodyObject != null)
			{
				grabBodyObject.grabbable = true;
			}
			moveToExactPositionTimer = 0f;
			if (wasMatchingPosition)
			{
				wasMatchingPosition = false;
				if (physicsParentCollider != null && physicsParentCollider.enabled && physicsParentCollider.ClosestPoint(bodyParts[5].position) == bodyParts[5].position)
				{
					isParentedToPhysicsRegion = true;
					base.transform.SetParent(physicsParent);
					parentedToShip = true;
					StopFloatingBody();
				}
				else if (StartOfRound.Instance.shipBounds.bounds.Contains(base.transform.position))
				{
					base.transform.SetParent(StartOfRound.Instance.elevatorTransform);
					parentedToShip = true;
					StopFloatingBody();
					isParentedToPhysicsRegion = false;
				}
				else if (isParentedToPhysicsRegion)
				{
					isParentedToPhysicsRegion = false;
					base.transform.SetParent(null, worldPositionStays: true);
				}
				previousAttachedLimb.ResetCenterOfMass();
				previousAttachedLimb.ResetInertiaTensor();
				previousAttachedLimb.freezeRotation = false;
				previousAttachedLimb.isKinematic = false;
				EnableCollisionOnBodyParts();
			}
			return;
		}
		if (grabBodyObject != null)
		{
			grabBodyObject.grabbable = canBeGrabbedBackByPlayers;
		}
		if (parentedToShip)
		{
			parentedToShip = false;
			base.transform.SetParent(null, worldPositionStays: true);
		}
		if (matchPositionExactly)
		{
			if (!lerpBeforeMatchingPosition || !(moveToExactPositionTimer < 0.3f))
			{
				if (!wasMatchingPosition)
				{
					wasMatchingPosition = true;
					Vector3 vector = base.transform.position - attachedLimb.position;
					base.transform.GetComponent<Rigidbody>().position = attachedTo.position + vector;
					previousAttachedLimb = attachedLimb;
					attachedLimb.freezeRotation = true;
					attachedLimb.isKinematic = true;
					attachedLimb.transform.position = attachedTo.position;
					attachedLimb.transform.rotation = attachedTo.rotation;
					for (int i = 0; i < bodyParts.Length; i++)
					{
						bodyParts[i].angularDrag = 1f;
						bodyParts[i].maxAngularVelocity = 2f;
						bodyParts[i].maxDepenetrationVelocity = 0.3f;
						bodyParts[i].velocity = Vector3.zero;
						bodyParts[i].angularVelocity = Vector3.zero;
						bodyParts[i].WakeUp();
					}
				}
				else
				{
					attachedLimb.position = attachedTo.position;
					attachedLimb.rotation = attachedTo.rotation;
					attachedLimb.centerOfMass = Vector3.zero;
					attachedLimb.inertiaTensorRotation = Quaternion.identity;
				}
				return;
			}
			moveToExactPositionTimer += Time.deltaTime;
			speedMultiplier = 25f;
		}
		forceDirection = Vector3.Normalize(attachedTo.position - attachedLimb.position);
		attachedLimb.AddForce(forceDirection * speedMultiplier * Mathf.Clamp(Vector3.Distance(attachedTo.position, attachedLimb.position), 0.2f, 2.5f), ForceMode.VelocityChange);
		if (attachedLimb.velocity.sqrMagnitude > maxVelocity)
		{
			attachedLimb.velocity = attachedLimb.velocity.normalized * maxVelocity;
		}
		if (!(secondaryAttachedLimb == null) && !(secondaryAttachedTo == null))
		{
			forceDirection = Vector3.Normalize(secondaryAttachedTo.position - secondaryAttachedLimb.position);
			secondaryAttachedLimb.AddForce(forceDirection * speedMultiplier * Mathf.Clamp(Vector3.Distance(secondaryAttachedTo.position, secondaryAttachedLimb.position), 0.2f, 2.5f), ForceMode.VelocityChange);
			if (secondaryAttachedLimb.velocity.sqrMagnitude > maxVelocity)
			{
				secondaryAttachedLimb.velocity = secondaryAttachedLimb.velocity.normalized * maxVelocity;
			}
		}
	}

	private void DetectBodyMovedDistanceThreshold()
	{
		bodyMovedThisFrame = false;
		if (isInShip)
		{
			if (Vector3.Distance(previousBodyPosition, base.transform.localPosition) > 1f)
			{
				previousBodyPosition = base.transform.localPosition;
				bodyMovedThisFrame = true;
			}
		}
		else if (Vector3.Distance(previousBodyPosition, base.transform.position) > 1f)
		{
			previousBodyPosition = base.transform.position;
			bodyMovedThisFrame = true;
		}
	}

	private void ResetBodyPositionIfTooFarFromAttachment()
	{
		for (int i = 0; i < bodyParts.Length; i++)
		{
			if (Vector3.Distance(bodyParts[i].position, attachedTo.position) > 4f)
			{
				resetBodyPartsTimer = 0f;
				bodyParts[i].GetComponent<Collider>().enabled = false;
			}
		}
	}

	private void EnableCollisionOnBodyParts()
	{
		for (int i = 0; i < bodyParts.Length; i++)
		{
			bodyParts[i].GetComponent<Collider>().enabled = true;
		}
	}

	public void MakeCorpseBloody()
	{
		for (int i = 0; i < bodyBloodDecals.Length; i++)
		{
			bodyBloodDecals[i].SetActive(value: true);
		}
	}

	public void SetBodyPartsKinematic(bool setKinematic = true)
	{
		if (setKinematic)
		{
			bodySetToKinematic = true;
			for (int i = 0; i < bodyParts.Length; i++)
			{
				bodyParts[i].velocity = Vector3.zero;
				bodyParts[i].isKinematic = true;
			}
			return;
		}
		for (int j = 0; j < bodyParts.Length; j++)
		{
			bodyParts[j].velocity = Vector3.zero;
			if (!(bodyParts[j] == attachedLimb) || !matchPositionExactly)
			{
				bodyParts[j].isKinematic = false;
			}
		}
	}

	public void DeactivateBody(bool setActive)
	{
		base.gameObject.SetActive(setActive);
		SetBodyPartsKinematic();
		isInShip = false;
		deactivated = true;
	}

	public void ResetRagdollPosition()
	{
		if (attachedLimb != null && attachedTo != null)
		{
			base.transform.position = attachedTo.position + Vector3.up * 2f;
		}
		else
		{
			base.transform.position = spawnPosition;
		}
		for (int i = 0; i < bodyParts.Length; i++)
		{
			bodyParts[i].velocity = Vector3.zero;
		}
	}

	public void SetRagdollPositionSafely(Vector3 newPosition, bool disableSpecialEffects = false)
	{
		base.transform.position = newPosition + Vector3.up * 2.5f;
		if (disableSpecialEffects)
		{
			StopFloatingBody();
		}
		for (int i = 0; i < bodyParts.Length; i++)
		{
			bodyParts[i].velocity = Vector3.zero;
		}
		timeSinceLastCollisionSFX = -1f;
	}

	public void AddForceToBodyPart(int bodyPartIndex, Vector3 force)
	{
		bodyParts[bodyPartIndex].AddForce(force, ForceMode.Impulse);
	}

	public void ChangeMesh(Mesh changeMesh, Material changeMaterial = null)
	{
		base.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = changeMesh;
		if (changeMaterial != null)
		{
			base.gameObject.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial = changeMaterial;
		}
	}
}
