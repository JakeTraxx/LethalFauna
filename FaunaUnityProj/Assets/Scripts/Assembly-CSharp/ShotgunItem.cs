using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class ShotgunItem : GrabbableObject
{
	public int gunCompatibleAmmoID = 1410;

	public bool isReloading;

	public int shellsLoaded;

	public Animator gunAnimator;

	public AudioSource gunAudio;

	public AudioSource gunShootAudio;

	public AudioSource gunBulletsRicochetAudio;

	private Coroutine gunCoroutine;

	public AudioClip[] gunShootSFX;

	public AudioClip gunReloadSFX;

	public AudioClip gunReloadFinishSFX;

	public AudioClip noAmmoSFX;

	public AudioClip gunSafetySFX;

	public AudioClip switchSafetyOnSFX;

	public AudioClip switchSafetyOffSFX;

	public bool safetyOn;

	private float misfireTimer = 30f;

	private bool hasHitGroundWithSafetyOff = true;

	private int ammoSlotToUse = -1;

	private bool localClientSendingShootGunRPC;

	private PlayerControllerB previousPlayerHeldBy;

	public ParticleSystem gunShootParticle;

	public Transform shotgunRayPoint;

	public MeshRenderer shotgunShellLeft;

	public MeshRenderer shotgunShellRight;

	public MeshRenderer shotgunShellInHand;

	public Transform shotgunShellInHandTransform;

	private RaycastHit[] enemyColliders;

	private EnemyAI heldByEnemy;

	public override void Start()
	{
		base.Start();
		misfireTimer = 30f;
		hasHitGroundWithSafetyOff = true;
	}

	public override int GetItemDataToSave()
	{
		base.GetItemDataToSave();
		return shellsLoaded;
	}

	public override void LoadItemSaveData(int saveData)
	{
		base.LoadItemSaveData(saveData);
		safetyOn = true;
		shellsLoaded = saveData;
	}

	public override void Update()
	{
		base.Update();
		if (!base.IsOwner || shellsLoaded <= 0 || isReloading || heldByEnemy != null || isPocketed)
		{
			return;
		}
		if (hasHitGround && !safetyOn && !hasHitGroundWithSafetyOff && !isHeld)
		{
			if (Random.Range(0, 100) < 5)
			{
				ShootGunAndSync(heldByPlayer: false);
			}
			hasHitGroundWithSafetyOff = true;
		}
		else if (!safetyOn && misfireTimer <= 0f && !StartOfRound.Instance.inShipPhase)
		{
			if (Random.Range(0, 100) < 4)
			{
				ShootGunAndSync(isHeld);
			}
			if (Random.Range(0, 100) < 5)
			{
				misfireTimer = 2f;
			}
			else
			{
				misfireTimer = Random.Range(28f, 50f);
			}
		}
		else if (!safetyOn)
		{
			misfireTimer -= Time.deltaTime;
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		previousPlayerHeldBy = playerHeldBy;
		previousPlayerHeldBy.equippedUsableItemQE = true;
		hasHitGroundWithSafetyOff = false;
	}

	public override void GrabItemFromEnemy(EnemyAI enemy)
	{
		base.GrabItemFromEnemy(enemy);
		heldByEnemy = enemy;
		hasHitGroundWithSafetyOff = false;
	}

	public override void DiscardItemFromEnemy()
	{
		base.DiscardItemFromEnemy();
		heldByEnemy = null;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (!isReloading)
		{
			if (shellsLoaded == 0)
			{
				StartReloadGun();
			}
			else if (safetyOn)
			{
				gunAudio.PlayOneShot(gunSafetySFX);
			}
			else if (base.IsOwner)
			{
				ShootGunAndSync(heldByPlayer: true);
			}
		}
	}

	public void ShootGunAndSync(bool heldByPlayer)
	{
		Vector3 shotgunPosition;
		Vector3 forward;
		if (!heldByPlayer)
		{
			shotgunPosition = shotgunRayPoint.position;
			forward = shotgunRayPoint.forward;
		}
		else
		{
			shotgunPosition = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position - GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.up * 0.45f;
			forward = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward;
		}
		Debug.Log("Calling shoot gun....");
		ShootGun(shotgunPosition, forward);
		Debug.Log("Calling shoot gun and sync");
		localClientSendingShootGunRPC = true;
		ShootGunServerRpc(shotgunPosition, forward);
	}

	[ServerRpc(RequireOwnership = false)]
	public void ShootGunServerRpc(Vector3 shotgunPosition, Vector3 shotgunForward)
			{
				ShootGunClientRpc(shotgunPosition, shotgunForward);
			}

	[ClientRpc]
	public void ShootGunClientRpc(Vector3 shotgunPosition, Vector3 shotgunForward)
{		{
			Debug.Log("Shoot gun client rpc received");
			if (localClientSendingShootGunRPC)
			{
				localClientSendingShootGunRPC = false;
				Debug.Log("localClientSendingShootGunRPC was true");
			}
			else
			{
				ShootGun(shotgunPosition, shotgunForward);
			}
		}
}
	public void ShootGun(Vector3 shotgunPosition, Vector3 shotgunForward)
	{
		isReloading = false;
		bool flag = false;
		if (isHeld && playerHeldBy != null && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			playerHeldBy.playerBodyAnimator.SetTrigger("ShootShotgun");
			flag = true;
		}
		RoundManager.PlayRandomClip(gunShootAudio, gunShootSFX, randomize: true, 1f, 1840);
		WalkieTalkie.TransmitOneShotAudio(gunShootAudio, gunShootSFX[0]);
		gunShootParticle.Play(withChildren: true);
		shellsLoaded = Mathf.Clamp(shellsLoaded - 1, 0, 2);
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		if (localPlayerController == null)
		{
			return;
		}
		float num = Vector3.Distance(localPlayerController.transform.position, shotgunRayPoint.transform.position);
		bool flag2 = false;
		int damageNumber = 0;
		float num2 = 0f;
		Vector3 vector = localPlayerController.playerCollider.ClosestPoint(shotgunPosition);
		if (!flag && !Physics.Linecast(shotgunPosition, vector, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && Vector3.Angle(shotgunForward, vector - shotgunPosition) < 30f)
		{
			flag2 = true;
		}
		if (num < 5f)
		{
			num2 = 0.8f;
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			damageNumber = 100;
		}
		if (num < 15f)
		{
			num2 = 0.5f;
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			damageNumber = 100;
		}
		else if (num < 23f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			damageNumber = 40;
		}
		else if (num < 30f)
		{
			damageNumber = 20;
		}
		if (num2 > 0f && SoundManager.Instance.timeSinceEarsStartedRinging > 16f)
		{
			StartCoroutine(delayedEarsRinging(num2));
		}
		Ray ray = new Ray(shotgunPosition, shotgunForward);
		if (Physics.Raycast(ray, out var hitInfo, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			gunBulletsRicochetAudio.transform.position = ray.GetPoint(hitInfo.distance - 0.5f);
			gunBulletsRicochetAudio.Play();
		}
		if (flag2)
		{
			localPlayerController.DamagePlayer(damageNumber, hasDamageSFX: true, callRPC: true, CauseOfDeath.Gunshots, 0, fallDamage: false, shotgunRayPoint.forward * 30f);
		}
		if (!base.IsOwner)
		{
			return;
		}
		if (enemyColliders == null)
		{
			enemyColliders = new RaycastHit[10];
		}
		ray = new Ray(shotgunPosition - shotgunForward * 10f, shotgunForward);
		int num3 = Physics.SphereCastNonAlloc(ray, 5f, enemyColliders, 15f, 524288, QueryTriggerInteraction.Collide);
		List<EnemyAI> list = new List<EnemyAI>();
		for (int i = 0; i < num3; i++)
		{
			if (!enemyColliders[i].transform.GetComponent<EnemyAICollisionDetect>())
			{
				continue;
			}
			EnemyAI mainScript = enemyColliders[i].transform.GetComponent<EnemyAICollisionDetect>().mainScript;
			if (heldByEnemy != null && heldByEnemy == mainScript)
			{
				continue;
			}
			IHittable component;
			if (enemyColliders[i].distance == 0f)
			{
				Debug.Log("Spherecast started inside enemy collider");
			}
			else if (!Physics.Linecast(shotgunPosition, enemyColliders[i].point, out hitInfo, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore) && enemyColliders[i].transform.TryGetComponent<IHittable>(out component))
			{
				float num4 = Vector3.Distance(shotgunPosition, enemyColliders[i].point);
				int force = ((num4 < 3.7f) ? 5 : ((!(num4 < 6f)) ? 2 : 3));
				EnemyAICollisionDetect component2 = enemyColliders[i].collider.GetComponent<EnemyAICollisionDetect>();
				if ((!(component2 != null) || (!(component2.mainScript == null) && !list.Contains(component2.mainScript))) && component.Hit(force, shotgunForward, playerHeldBy, playHitSFX: true) && component2 != null)
				{
					list.Add(component2.mainScript);
				}
			}
		}
	}

	private IEnumerator delayedEarsRinging(float effectSeverity)
	{
		yield return new WaitForSeconds(0.6f);
		SoundManager.Instance.earsRingingTimer = effectSeverity;
	}

	public override void ItemInteractLeftRight(bool right)
	{
		base.ItemInteractLeftRight(right);
		if (playerHeldBy == null)
		{
			return;
		}
		Debug.Log($"r/l activate: {right}");
		if (!right)
		{
			if (safetyOn)
			{
				safetyOn = false;
				gunAudio.PlayOneShot(switchSafetyOffSFX);
				WalkieTalkie.TransmitOneShotAudio(gunAudio, switchSafetyOffSFX);
				SetSafetyControlTip();
			}
			else
			{
				safetyOn = true;
				gunAudio.PlayOneShot(switchSafetyOnSFX);
				WalkieTalkie.TransmitOneShotAudio(gunAudio, switchSafetyOnSFX);
				SetSafetyControlTip();
			}
			playerHeldBy.playerBodyAnimator.SetTrigger("SwitchGunSafety");
		}
		else if (!isReloading && shellsLoaded < 2)
		{
			StartReloadGun();
		}
	}

	public override void SetControlTipsForItem()
	{
		string[] toolTips = itemProperties.toolTips;
		if (toolTips.Length <= 2)
		{
			Debug.LogError("Shotgun control tips array length is too short to set tips!");
			return;
		}
		if (safetyOn)
		{
			toolTips[2] = "Turn safety off: [Q]";
		}
		else
		{
			toolTips[2] = "Turn safety on: [Q]";
		}
		HUDManager.Instance.ChangeControlTipMultiple(toolTips, holdingItem: true, itemProperties);
	}

	private void SetSafetyControlTip()
	{
		string changeTo = ((!safetyOn) ? "Turn safety on: [Q]" : "Turn safety off: [Q]");
		if (base.IsOwner)
		{
			HUDManager.Instance.ChangeControlTip(3, changeTo);
		}
	}

	private void StartReloadGun()
	{
		if (ReloadedGun())
		{
			if (base.IsOwner)
			{
				if (gunCoroutine != null)
				{
					StopCoroutine(gunCoroutine);
				}
				gunCoroutine = StartCoroutine(reloadGunAnimation());
			}
		}
		else
		{
			gunAudio.PlayOneShot(noAmmoSFX);
		}
	}

	[ServerRpc]
	public void ReloadGunEffectsServerRpc(bool start = true)
{		{
			ReloadGunEffectsClientRpc(start);
		}
}
	[ClientRpc]
	public void ReloadGunEffectsClientRpc(bool start = true)
{if(!base.IsOwner)		{
			if (start)
			{
				gunAudio.PlayOneShot(gunReloadSFX);
				WalkieTalkie.TransmitOneShotAudio(gunAudio, gunReloadSFX);
				gunAnimator.SetBool("Reloading", value: true);
				isReloading = true;
			}
			else
			{
				shellsLoaded = Mathf.Clamp(shellsLoaded + 1, 0, 2);
				gunAudio.PlayOneShot(gunReloadFinishSFX);
				gunAnimator.SetBool("Reloading", value: false);
				isReloading = false;
			}
		}
}
	private IEnumerator reloadGunAnimation()
	{
		isReloading = true;
		if (shellsLoaded <= 0)
		{
			playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: true);
			shotgunShellLeft.enabled = false;
			shotgunShellRight.enabled = false;
		}
		else
		{
			playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun2", value: true);
			shotgunShellRight.enabled = false;
		}
		yield return new WaitForSeconds(0.3f);
		gunAudio.PlayOneShot(gunReloadSFX);
		gunAnimator.SetBool("Reloading", value: true);
		ReloadGunEffectsServerRpc();
		yield return new WaitForSeconds(0.95f);
		shotgunShellInHand.enabled = true;
		shotgunShellInHandTransform.SetParent(playerHeldBy.leftHandItemTarget);
		shotgunShellInHandTransform.localPosition = new Vector3(-0.0555f, 0.1469f, -0.0655f);
		shotgunShellInHandTransform.localEulerAngles = new Vector3(-1.956f, 143.856f, -16.427f);
		yield return new WaitForSeconds(0.95f);
		playerHeldBy.DestroyItemInSlotAndSync(ammoSlotToUse);
		ammoSlotToUse = -1;
		shellsLoaded = Mathf.Clamp(shellsLoaded + 1, 0, 2);
		shotgunShellLeft.enabled = true;
		if (shellsLoaded == 2)
		{
			shotgunShellRight.enabled = true;
		}
		shotgunShellInHand.enabled = false;
		shotgunShellInHandTransform.SetParent(base.transform);
		yield return new WaitForSeconds(0.45f);
		gunAudio.PlayOneShot(gunReloadFinishSFX);
		gunAnimator.SetBool("Reloading", value: false);
		playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: false);
		playerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun2", value: false);
		isReloading = false;
		ReloadGunEffectsServerRpc(start: false);
	}

	private bool ReloadedGun()
	{
		int num = FindAmmoInInventory();
		if (num == -1)
		{
			Debug.Log("not reloading");
			return false;
		}
		Debug.Log("reloading!");
		ammoSlotToUse = num;
		return true;
	}

	private int FindAmmoInInventory()
	{
		for (int i = 0; i < playerHeldBy.ItemSlots.Length; i++)
		{
			if (!(playerHeldBy.ItemSlots[i] == null))
			{
				GunAmmo gunAmmo = playerHeldBy.ItemSlots[i] as GunAmmo;
				Debug.Log($"Ammo null in slot #{i}?: {gunAmmo == null}");
				if (gunAmmo != null)
				{
					Debug.Log($"Ammo in slot #{i} id: {gunAmmo.ammoType}");
				}
				if (gunAmmo != null && gunAmmo.ammoType == gunCompatibleAmmoID)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public override void PocketItem()
	{
		base.PocketItem();
		StopUsingGun();
	}

	public override void DiscardItem()
	{
		base.DiscardItem();
		StopUsingGun();
	}

	private void StopUsingGun()
	{
		previousPlayerHeldBy.equippedUsableItemQE = false;
		if (isReloading)
		{
			if (gunCoroutine != null)
			{
				StopCoroutine(gunCoroutine);
			}
			gunAnimator.SetBool("Reloading", value: false);
			gunAudio.Stop();
			if (previousPlayerHeldBy != null)
			{
				previousPlayerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun", value: false);
				previousPlayerHeldBy.playerBodyAnimator.SetBool("ReloadShotgun2", value: false);
			}
			shotgunShellInHand.enabled = false;
			shotgunShellInHandTransform.SetParent(base.transform);
			isReloading = false;
		}
	}
}
