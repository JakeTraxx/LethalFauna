using System;
using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class StunGrenadeItem : GrabbableObject
{
	[Header("Stun grenade settings")]
	public float TimeToExplode = 2.25f;

	public bool DestroyGrenade;

	public string playerAnimation = "PullGrenadePin";

	[Space(5f)]
	public bool explodeOnCollision;

	public bool dontRequirePullingPin;

	public float chanceToExplode = 100f;

	public bool spawnDamagingShockwave;

	private bool explodeOnThrow;

	private bool gotExplodeOnThrowRPC;

	private bool hasCollided;

	[Space(3f)]
	public bool pinPulled;

	public bool inPullingPinAnimation;

	public string throwString = "Throw grenade: [RMB]";

	private Coroutine pullPinCoroutine;

	public Animator itemAnimator;

	public AudioSource itemAudio;

	public AudioClip pullPinSFX;

	public AudioClip explodeSFX;

	public AnimationCurve grenadeFallCurve;

	public AnimationCurve grenadeVerticalFallCurve;

	public AnimationCurve grenadeVerticalFallCurveNoBounce;

	public RaycastHit grenadeHit;

	public Ray grenadeThrowRay;

	private int stunGrenadeMask = 268437761;

	public float explodeTimer;

	public bool hasExploded;

	public GameObject stunGrenadeExplosion;

	private PlayerControllerB playerThrownBy;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (inPullingPinAnimation)
		{
			return;
		}
		if (!pinPulled)
		{
			if (pullPinCoroutine == null)
			{
				playerHeldBy.activatingItem = true;
				pullPinCoroutine = StartCoroutine(pullPinAnimation());
			}
		}
		else if (base.IsOwner)
		{
			playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination());
		}
	}

	public override void DiscardItem()
	{
		if (playerHeldBy != null && !explodeOnCollision)
		{
			playerHeldBy.activatingItem = false;
		}
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		if (explodeOnCollision)
		{
			playerThrownBy = playerHeldBy;
		}
		gotExplodeOnThrowRPC = false;
		hasCollided = false;
		SetControlTipForGrenade();
		EnableItemMeshes(enable: true);
		isPocketed = false;
		if (chanceToExplode < 100f)
		{
			SetExplodeOnThrowServerRpc();
		}
		if (!hasBeenHeld)
		{
			hasBeenHeld = true;
			if (!isInShipRoom && !StartOfRound.Instance.inShipPhase && StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap)
			{
				RoundManager.Instance.valueOfFoundScrapItems += scrapValue;
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SetExplodeOnThrowServerRpc()
			{
				bool explodeOnThrowClientRpc = (float)new System.Random(StartOfRound.Instance.randomMapSeed + 10 + (int)base.transform.position.x + (int)base.transform.position.z).Next(0, 100) <= chanceToExplode;
				SetExplodeOnThrowClientRpc(explodeOnThrowClientRpc);
			}

	[ClientRpc]
	public void SetExplodeOnThrowClientRpc(bool explode)
			{
				gotExplodeOnThrowRPC = true;
				explodeOnThrow = explode;
			}

	private void SetControlTipForGrenade()
	{
		string[] allLines = ((!pinPulled) ? new string[1] { "Pull pin: [RMB]" } : new string[1] { throwString });
		if (base.IsOwner)
		{
			HUDManager.Instance.ChangeControlTipMultiple(allLines, holdingItem: true, itemProperties);
		}
	}

	public override void FallWithCurve()
	{
		float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
		base.transform.rotation = Quaternion.Lerp(base.transform.rotation, Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z), 14f * Time.deltaTime / magnitude);
		base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, grenadeFallCurve.Evaluate(fallTime));
		if (magnitude > 5f)
		{
			base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), grenadeVerticalFallCurveNoBounce.Evaluate(fallTime));
		}
		else
		{
			base.transform.localPosition = Vector3.Lerp(new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z), new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z), grenadeVerticalFallCurve.Evaluate(fallTime));
		}
		fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
	}

	private IEnumerator pullPinAnimation()
	{
		inPullingPinAnimation = true;
		playerHeldBy.activatingItem = true;
		playerHeldBy.doingUpperBodyEmote = 1.16f;
		playerHeldBy.playerBodyAnimator.SetTrigger(playerAnimation);
		itemAnimator.SetTrigger("pullPin");
		itemAudio.PlayOneShot(pullPinSFX);
		WalkieTalkie.TransmitOneShotAudio(itemAudio, pullPinSFX, 0.8f);
		yield return new WaitForSeconds(1f);
		if (playerHeldBy != null)
		{
			if (!DestroyGrenade)
			{
				playerHeldBy.activatingItem = false;
			}
			playerThrownBy = playerHeldBy;
		}
		inPullingPinAnimation = false;
		pinPulled = true;
		itemUsedUp = true;
		if (base.IsOwner && playerHeldBy != null)
		{
			SetControlTipForGrenade();
		}
	}

	public override void Update()
	{
		base.Update();
		if (hasCollided)
		{
			if (gotExplodeOnThrowRPC)
			{
				gotExplodeOnThrowRPC = false;
				ExplodeStunGrenade(DestroyGrenade);
			}
		}
		else if (!dontRequirePullingPin && pinPulled && !hasExploded)
		{
			explodeTimer += Time.deltaTime;
			if (explodeTimer > TimeToExplode)
			{
				ExplodeStunGrenade(DestroyGrenade);
			}
		}
	}

	public override void Start()
	{
		base.Start();
		if (dontRequirePullingPin)
		{
			pinPulled = true;
		}
	}

	public override void OnHitGround()
	{
		base.OnHitGround();
		if (!(playerThrownBy == null) && explodeOnCollision && pinPulled)
		{
			hasCollided = true;
		}
	}

	private void ExplodeStunGrenade(bool destroy = false)
	{
		if (hasExploded)
		{
			return;
		}
		if ((chanceToExplode < 100f && !explodeOnThrow) || (explodeOnCollision && !StartOfRound.Instance.currentLevel.spawnEnemiesAndScrap && parentObject == UnityEngine.Object.FindObjectOfType<DepositItemsDesk>().deskObjectsContainer))
		{
			if (playerThrownBy != null)
			{
				playerThrownBy.activatingItem = false;
			}
			return;
		}
		hasExploded = true;
		Transform parent = ((!isInElevator) ? RoundManager.Instance.mapPropsContainer.transform : StartOfRound.Instance.elevatorTransform);
		if (spawnDamagingShockwave)
		{
			Landmine.SpawnExplosion(base.transform.position + Vector3.up * 0.2f, spawnExplosionEffect: false, 0.5f, 3f, 40, 45f);
		}
		else
		{
			StunExplosion(base.transform.position, affectAudio: true, 1f, 7.5f, 1f, isHeld, playerHeldBy, playerThrownBy);
		}
		UnityEngine.Object.Instantiate(stunGrenadeExplosion, base.transform.position, Quaternion.identity, parent);
		itemAudio.PlayOneShot(explodeSFX);
		WalkieTalkie.TransmitOneShotAudio(itemAudio, explodeSFX);
		if (DestroyGrenade)
		{
			DestroyObjectInHand(playerThrownBy);
		}
	}

	public static void StunExplosion(Vector3 explosionPosition, bool affectAudio, float flashSeverityMultiplier, float enemyStunTime, float flashSeverityDistanceRolloff = 1f, bool isHeldItem = false, PlayerControllerB playerHeldBy = null, PlayerControllerB playerThrownBy = null, float addToFlashSeverity = 0f)
	{
		PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript != null)
		{
			playerControllerB = GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript;
		}
		float num = Vector3.Distance(playerControllerB.transform.position, explosionPosition);
		float num2 = 7f / (num * flashSeverityDistanceRolloff);
		if (Physics.Linecast(explosionPosition + Vector3.up * 0.5f, playerControllerB.gameplayCamera.transform.position, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			num2 /= 13f;
		}
		else if (num < 2f)
		{
			num2 = 1f;
		}
		else if (!playerControllerB.HasLineOfSightToPosition(explosionPosition, 60f, 15, 2f))
		{
			num2 = Mathf.Clamp(num2 / 3f, 0f, 1f);
		}
		if (isHeldItem && playerHeldBy == GameNetworkManager.Instance.localPlayerController)
		{
			num2 = 1f;
			GameNetworkManager.Instance.localPlayerController.DamagePlayer(20, hasDamageSFX: false, callRPC: true, CauseOfDeath.Blast);
		}
		num2 = Mathf.Clamp(num2 * flashSeverityMultiplier, 0f, 1f);
		if (num2 > 0.6f)
		{
			num2 += addToFlashSeverity;
		}
		HUDManager.Instance.flashFilter = num2;
		if (affectAudio)
		{
			SoundManager.Instance.earsRingingTimer = num2;
		}
		if (enemyStunTime <= 0f)
		{
			return;
		}
		Collider[] array = Physics.OverlapSphere(explosionPosition, 12f, 524288);
		if (array.Length == 0)
		{
			return;
		}
		for (int i = 0; i < array.Length; i++)
		{
			EnemyAICollisionDetect component = array[i].GetComponent<EnemyAICollisionDetect>();
			if (component == null)
			{
				continue;
			}
			Vector3 b = component.mainScript.transform.position + Vector3.up * 0.5f;
			if (component.mainScript.CheckLineOfSightForPosition(explosionPosition + Vector3.up * 0.5f, 120f, 23, 7f) || (!Physics.Linecast(explosionPosition + Vector3.up * 0.5f, component.mainScript.transform.position + Vector3.up * 0.5f, 256) && Vector3.Distance(explosionPosition, b) < 11f))
			{
				if (playerThrownBy != null)
				{
					component.mainScript.SetEnemyStunned(setToStunned: true, enemyStunTime, playerThrownBy);
				}
				else
				{
					component.mainScript.SetEnemyStunned(setToStunned: true, enemyStunTime);
				}
			}
		}
	}

	public Vector3 GetGrenadeThrowDestination()
	{
		Vector3 position = base.transform.position;
		Debug.DrawRay(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
		grenadeThrowRay = new Ray(playerHeldBy.gameplayCamera.transform.position, playerHeldBy.gameplayCamera.transform.forward);
		position = ((!Physics.Raycast(grenadeThrowRay, out grenadeHit, 12f, stunGrenadeMask, QueryTriggerInteraction.Ignore)) ? grenadeThrowRay.GetPoint(10f) : grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f));
		Debug.DrawRay(position, Vector3.down, Color.blue, 15f);
		grenadeThrowRay = new Ray(position, Vector3.down);
		if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 30f, stunGrenadeMask, QueryTriggerInteraction.Ignore))
		{
			return grenadeHit.point + Vector3.up * 0.05f;
		}
		return grenadeThrowRay.GetPoint(30f);
	}
}
