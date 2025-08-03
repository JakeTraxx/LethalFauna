using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class Landmine : NetworkBehaviour, IHittable
{
	private bool mineActivated = true;

	public bool hasExploded;

	public ParticleSystem explosionParticle;

	public Animator mineAnimator;

	public AudioSource mineAudio;

	public AudioSource mineFarAudio;

	public AudioClip mineDetonate;

	public AudioClip mineTrigger;

	public AudioClip mineDetonateFar;

	public AudioClip mineDeactivate;

	public AudioClip minePress;

	private bool sendingExplosionRPC;

	private RaycastHit hit;

	private RoundManager roundManager;

	private float pressMineDebounceTimer;

	private bool localPlayerOnMine;

	private void Start()
	{
		StartCoroutine(StartIdleAnimation());
	}

	private void Update()
	{
		if (pressMineDebounceTimer > 0f)
		{
			pressMineDebounceTimer -= Time.deltaTime;
		}
		if (localPlayerOnMine && GameNetworkManager.Instance.localPlayerController.teleportedLastFrame)
		{
			localPlayerOnMine = false;
			TriggerMineOnLocalClientByExiting();
		}
	}

	public void ToggleMine(bool enabled)
	{
		if (mineActivated != enabled)
		{
			mineActivated = enabled;
			if (!enabled)
			{
				mineAudio.PlayOneShot(mineDeactivate);
				WalkieTalkie.TransmitOneShotAudio(mineAudio, mineDeactivate);
			}
			ToggleMineServerRpc(enabled);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ToggleMineServerRpc(bool enable)
			{
				ToggleMineClientRpc(enable);
			}

	[ClientRpc]
	public void ToggleMineClientRpc(bool enable)
			{
				ToggleMineEnabledLocalClient(enable);
			}

	public void ToggleMineEnabledLocalClient(bool enabled)
	{
		if (mineActivated != enabled)
		{
			mineActivated = enabled;
			if (!enabled)
			{
				mineAudio.PlayOneShot(mineDeactivate);
				WalkieTalkie.TransmitOneShotAudio(mineAudio, mineDeactivate);
			}
		}
	}

	private IEnumerator StartIdleAnimation()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
		if (!(roundManager == null))
		{
			if (roundManager.BreakerBoxRandom != null)
			{
				yield return new WaitForSeconds((float)roundManager.BreakerBoxRandom.NextDouble() + 0.5f);
			}
			mineAnimator.SetTrigger("startIdle");
			mineAudio.pitch = Random.Range(0.9f, 1.1f);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (hasExploded || pressMineDebounceTimer > 0f)
		{
			return;
		}
		if (other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (!(component != GameNetworkManager.Instance.localPlayerController) && component != null && !component.isPlayerDead)
			{
				localPlayerOnMine = true;
				pressMineDebounceTimer = 0.5f;
				PressMineServerRpc();
			}
		}
		else
		{
			if (!other.CompareTag("PhysicsProp") && !other.tag.StartsWith("PlayerRagdoll"))
			{
				return;
			}
			if ((bool)other.GetComponent<DeadBodyInfo>())
			{
				if (other.GetComponent<DeadBodyInfo>().playerScript != GameNetworkManager.Instance.localPlayerController)
				{
					return;
				}
			}
			else if ((bool)other.GetComponent<GrabbableObject>() && !other.GetComponent<GrabbableObject>().NetworkObject.IsOwner)
			{
				return;
			}
			pressMineDebounceTimer = 0.5f;
			PressMineServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PressMineServerRpc()
			{
				PressMineClientRpc();
			}

	[ClientRpc]
	public void PressMineClientRpc()
			{
				pressMineDebounceTimer = 0.5f;
				mineAudio.PlayOneShot(minePress);
				WalkieTalkie.TransmitOneShotAudio(mineAudio, minePress);
			}

	private void OnTriggerExit(Collider other)
	{
		if (hasExploded || !mineActivated)
		{
			return;
		}
		if (other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && !component.isPlayerDead && !(component != GameNetworkManager.Instance.localPlayerController))
			{
				localPlayerOnMine = false;
				TriggerMineOnLocalClientByExiting();
			}
		}
		else
		{
			if (!other.tag.StartsWith("PlayerRagdoll") && !other.CompareTag("PhysicsProp"))
			{
				return;
			}
			if ((bool)other.GetComponent<DeadBodyInfo>())
			{
				if (other.GetComponent<DeadBodyInfo>().playerScript != GameNetworkManager.Instance.localPlayerController)
				{
					return;
				}
			}
			else if ((bool)other.GetComponent<GrabbableObject>() && !other.GetComponent<GrabbableObject>().NetworkObject.IsOwner)
			{
				return;
			}
			TriggerMineOnLocalClientByExiting();
		}
	}

	private void TriggerMineOnLocalClientByExiting()
	{
		if (!hasExploded)
		{
			SetOffMineAnimation();
			sendingExplosionRPC = true;
			ExplodeMineServerRpc();
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ExplodeMineServerRpc()
			{
				ExplodeMineClientRpc();
			}

	[ClientRpc]
	public void ExplodeMineClientRpc()
{		{
			if (sendingExplosionRPC)
			{
				sendingExplosionRPC = false;
			}
			else
			{
				SetOffMineAnimation();
			}
		}
}
	public void SetOffMineAnimation()
	{
		hasExploded = true;
		mineAnimator.SetTrigger("detonate");
		mineAudio.PlayOneShot(mineTrigger, 1f);
	}

	private IEnumerator TriggerOtherMineDelayed(Landmine mine)
	{
		if (!mine.hasExploded)
		{
			mine.mineAudio.pitch = Random.Range(0.75f, 1.07f);
			mine.hasExploded = true;
			yield return new WaitForSeconds(0.2f);
			mine.SetOffMineAnimation();
		}
	}

	public void Detonate()
	{
		mineAudio.pitch = Random.Range(0.93f, 1.07f);
		mineAudio.PlayOneShot(mineDetonate, 1f);
		SpawnExplosion(base.transform.position + Vector3.up, spawnExplosionEffect: false, 5.7f, 6f);
	}

	public static void SpawnExplosion(Vector3 explosionPosition, bool spawnExplosionEffect = false, float killRange = 1f, float damageRange = 1f, int nonLethalDamage = 50, float physicsForce = 0f, GameObject overridePrefab = null, bool goThroughCar = false)
	{
		if (spawnExplosionEffect)
		{
			GameObject gameObject = ((!(overridePrefab != null)) ? Object.Instantiate(StartOfRound.Instance.explosionPrefab, explosionPosition, Quaternion.Euler(-90f, 0f, 0f), RoundManager.Instance.mapPropsContainer.transform) : Object.Instantiate(overridePrefab, explosionPosition, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform));
			gameObject.SetActive(value: true);
		}
		float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, explosionPosition);
		if (num < 14f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
		else if (num < 25f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
		bool flag = false;
		Collider[] array = Physics.OverlapSphere(explosionPosition, damageRange, 2621448, QueryTriggerInteraction.Collide);
		PlayerControllerB playerControllerB = null;
		RaycastHit hitInfo;
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = Vector3.Distance(explosionPosition, array[i].transform.position);
			if (Physics.Linecast(explosionPosition, array[i].transform.position + Vector3.up * 0.3f, out hitInfo, 1073742080, QueryTriggerInteraction.Ignore) && ((!goThroughCar && hitInfo.collider.gameObject.layer == 30) || num2 > 4f))
			{
				continue;
			}
			if (array[i].gameObject.layer == 3 && !flag)
			{
				playerControllerB = array[i].gameObject.GetComponent<PlayerControllerB>();
				if (playerControllerB != null && playerControllerB.IsOwner)
				{
					flag = true;
					if (num2 < killRange)
					{
						Vector3 bodyVelocity = Vector3.Normalize(playerControllerB.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(playerControllerB.gameplayCamera.transform.position, explosionPosition);
						playerControllerB.KillPlayer(bodyVelocity, spawnBody: true, CauseOfDeath.Blast);
					}
					else if (num2 < damageRange)
					{
						Vector3 bodyVelocity = Vector3.Normalize(playerControllerB.gameplayCamera.transform.position - explosionPosition) * 80f / Vector3.Distance(playerControllerB.gameplayCamera.transform.position, explosionPosition);
						playerControllerB.DamagePlayer(nonLethalDamage, hasDamageSFX: true, callRPC: true, CauseOfDeath.Blast, 0, fallDamage: false, bodyVelocity);
					}
				}
			}
			else if (array[i].gameObject.layer == 21)
			{
				Landmine componentInChildren = array[i].gameObject.GetComponentInChildren<Landmine>();
				if (componentInChildren != null && !componentInChildren.hasExploded && num2 < 6f)
				{
					componentInChildren.StartCoroutine(componentInChildren.TriggerOtherMineDelayed(componentInChildren));
				}
			}
			else if (array[i].gameObject.layer == 19)
			{
				EnemyAICollisionDetect componentInChildren2 = array[i].gameObject.GetComponentInChildren<EnemyAICollisionDetect>();
				if (componentInChildren2 != null && componentInChildren2.mainScript.IsOwner && num2 < 4.5f)
				{
					componentInChildren2.mainScript.HitEnemyOnLocalClient(6);
					componentInChildren2.mainScript.HitFromExplosion(num2);
				}
			}
		}
		playerControllerB = GameNetworkManager.Instance.localPlayerController;
		if (physicsForce > 0f && Vector3.Distance(playerControllerB.transform.position, explosionPosition) < 35f && !Physics.Linecast(explosionPosition, playerControllerB.transform.position + Vector3.up * 0.3f, out hitInfo, 256, QueryTriggerInteraction.Ignore))
		{
			float num3 = Vector3.Distance(playerControllerB.transform.position, explosionPosition);
			Vector3 vector = Vector3.Normalize(playerControllerB.transform.position + Vector3.up * num3 - explosionPosition) / (num3 * 0.35f) * physicsForce;
			if (vector.magnitude > 2f)
			{
				if (vector.magnitude > 10f)
				{
					playerControllerB.CancelSpecialTriggerAnimations();
				}
				if (!playerControllerB.inVehicleAnimation || (playerControllerB.externalForceAutoFade + vector).magnitude > 50f)
				{
					playerControllerB.externalForceAutoFade += vector;
				}
			}
		}
		VehicleController vehicleController = Object.FindObjectOfType<VehicleController>();
		if (vehicleController != null && !vehicleController.magnetedToShip && physicsForce > 0f && Vector3.Distance(vehicleController.transform.position, explosionPosition) < 35f)
		{
			vehicleController.mainRigidbody.AddExplosionForce(physicsForce * 50f, explosionPosition, 12f, 3f, ForceMode.Impulse);
		}
		int num4 = ~LayerMask.GetMask("Room");
		num4 = ~LayerMask.GetMask("Colliders");
		array = Physics.OverlapSphere(explosionPosition, 10f, num4);
		for (int j = 0; j < array.Length; j++)
		{
			Rigidbody component = array[j].GetComponent<Rigidbody>();
			if (component != null)
			{
				component.AddExplosionForce(70f, explosionPosition, 10f);
			}
		}
	}

	public bool MineHasLineOfSight(Vector3 pos)
	{
		return !Physics.Linecast(base.transform.position, pos, out hit, 256);
	}

	bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		SetOffMineAnimation();
		sendingExplosionRPC = true;
		ExplodeMineServerRpc();
		return true;
	}
}
