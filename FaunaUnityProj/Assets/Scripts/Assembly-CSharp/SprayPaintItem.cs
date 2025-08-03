using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class SprayPaintItem : GrabbableObject
{
	public AudioSource sprayAudio;

	public AudioClip spraySFX;

	public AudioClip sprayNeedsShakingSFX;

	public AudioClip sprayStart;

	public AudioClip sprayStop;

	public AudioClip sprayCanEmptySFX;

	public AudioClip sprayCanNeedsShakingSFX;

	public AudioClip sprayCanShakeEmptySFX;

	public AudioClip[] sprayCanShakeSFX;

	public ParticleSystem sprayParticle;

	public ParticleSystem sprayCanNeedsShakingParticle;

	private bool isSpraying;

	private float sprayInterval;

	public float sprayIntervalSpeed = 0.2f;

	private Vector3 previousSprayPosition;

	public static List<GameObject> sprayPaintDecals = new List<GameObject>();

	public static int sprayPaintDecalsIndex;

	public GameObject sprayPaintPrefab;

	public int maxSprayPaintDecals = 1000;

	private float sprayCanTank = 1f;

	private float sprayCanShakeMeter;

	public static DecalProjector previousSprayDecal;

	private float shakingCanTimer;

	private bool tryingToUseEmptyCan;

	public Material[] sprayCanMats;

	public Material[] particleMats;

	private int sprayCanMatsIndex;

	private RaycastHit sprayHit;

	public bool debugSprayPaint;

	private int addSprayPaintWithFrameDelay;

	private DecalProjector delayedSprayPaintDecal;

	private int sprayPaintMask = 605030721;

	private bool makingAudio;

	private float audioInterval;

	[Space(5f)]
	public bool isWeedKillerSprayBottle;

	private Transform killingWeed;

	private Collider[] weedColliders;

	private float killWeedSpeed = 1f;

	private float addVehicleHPInterval;

	public override void Start()
	{
		base.Start();
		weedColliders = new Collider[3];
		sprayHit = default(RaycastHit);
		if (!isWeedKillerSprayBottle)
		{
			System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 151);
			sprayCanMatsIndex = random.Next(0, sprayCanMats.Length);
			sprayParticle.GetComponent<ParticleSystemRenderer>().material = particleMats[sprayCanMatsIndex];
			sprayCanNeedsShakingParticle.GetComponent<ParticleSystemRenderer>().material = particleMats[sprayCanMatsIndex];
		}
	}

	public override void LoadItemSaveData(int saveData)
	{
		base.LoadItemSaveData(saveData);
		sprayCanTank = (float)saveData / 100f;
	}

	public override int GetItemDataToSave()
	{
		return (int)(sprayCanTank * 100f);
	}

	public override void EquipItem()
	{
		base.EquipItem();
		playerHeldBy.equippedUsableItemQE = true;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (buttonDown)
		{
			Debug.Log("Start using spray");
			if (sprayCanTank <= 0f || sprayCanShakeMeter <= 0f)
			{
				Debug.Log("Spray empty");
				if (isSpraying)
				{
					StopSpraying();
				}
				PlayCanEmptyEffect(sprayCanTank <= 0f);
			}
			else
			{
				Debug.Log("Spray not empty");
				StartSpraying();
			}
			return;
		}
		Debug.Log("Stop using spray");
		if (tryingToUseEmptyCan)
		{
			addVehicleHPInterval = 0f;
			tryingToUseEmptyCan = false;
			sprayAudio.Stop();
			sprayCanNeedsShakingParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		}
		if (isWeedKillerSprayBottle)
		{
			sprayCanShakeMeter = 1f;
		}
		if (isSpraying)
		{
			StopSpraying();
		}
	}

	private void PlayCanEmptyEffect(bool isEmpty)
	{
		if (tryingToUseEmptyCan)
		{
			return;
		}
		tryingToUseEmptyCan = true;
		if (!isEmpty)
		{
			if (sprayCanNeedsShakingParticle.isPlaying)
			{
				sprayCanNeedsShakingParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}
			sprayCanNeedsShakingParticle.Play();
			sprayAudio.clip = sprayNeedsShakingSFX;
			sprayAudio.Play();
		}
		else
		{
			sprayAudio.PlayOneShot(sprayCanEmptySFX);
		}
	}

	public override void ItemInteractLeftRight(bool right)
	{
		base.ItemInteractLeftRight(right);
		Debug.Log($"interact {right} ; {playerHeldBy == null}; {isSpraying}");
		if (!isWeedKillerSprayBottle && !right && !(playerHeldBy == null) && !isSpraying)
		{
			if (sprayCanTank <= 0f)
			{
				sprayAudio.PlayOneShot(sprayCanShakeEmptySFX);
				WalkieTalkie.TransmitOneShotAudio(sprayAudio, sprayCanShakeEmptySFX);
			}
			else
			{
				RoundManager.PlayRandomClip(sprayAudio, sprayCanShakeSFX);
				WalkieTalkie.TransmitOneShotAudio(sprayAudio, sprayCanShakeEmptySFX);
			}
			playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
			sprayCanShakeMeter = Mathf.Min(sprayCanShakeMeter + 0.15f, 1f);
		}
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if (isWeedKillerSprayBottle && killingWeed != null)
		{
			killingWeed.localScale *= 1f - killWeedSpeed * Time.deltaTime;
			if (killingWeed.localScale.x < 0.5f)
			{
				KillWeedServerRpc(killingWeed.transform.position);
				UnityEngine.Object.FindObjectOfType<MoldSpreadManager>().DestroyMoldAtPosition(killingWeed.transform.position, playEffect: true);
				killingWeed = null;
			}
		}
		if (makingAudio)
		{
			if (audioInterval <= 0f)
			{
				audioInterval = 1f;
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.65f, 0, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed);
			}
			else
			{
				audioInterval -= Time.deltaTime;
			}
		}
		if (addSprayPaintWithFrameDelay > 1)
		{
			addSprayPaintWithFrameDelay--;
		}
		else if (addSprayPaintWithFrameDelay == 1)
		{
			addSprayPaintWithFrameDelay = 0;
			delayedSprayPaintDecal.enabled = true;
		}
		if (isSpraying && isHeld)
		{
			if (isWeedKillerSprayBottle)
			{
				Debug.Log("Spraying, depleting tank");
				sprayCanTank = Mathf.Max(sprayCanTank - Time.deltaTime / 15f, 0f);
				sprayCanShakeMeter = Mathf.Max(sprayCanShakeMeter - Time.deltaTime * 2f, 0f);
			}
			else
			{
				sprayCanTank = Mathf.Max(sprayCanTank - Time.deltaTime / 30f, 0f);
				sprayCanShakeMeter = Mathf.Max(sprayCanShakeMeter - Time.deltaTime / 10f, 0f);
			}
			if (sprayCanTank <= 0f || sprayCanShakeMeter <= 0f)
			{
				isSpraying = false;
				StopSpraying();
				PlayCanEmptyEffect(sprayCanTank <= 0f);
			}
			else
			{
				if (!base.IsOwner)
				{
					return;
				}
				if (sprayInterval <= 0f)
				{
					if (isWeedKillerSprayBottle)
					{
						sprayInterval = sprayIntervalSpeed;
						TrySprayingWeedKillerBottle();
					}
					else if (TrySpraying())
					{
						sprayInterval = sprayIntervalSpeed;
					}
					else
					{
						sprayInterval = 0.037f;
					}
				}
				else
				{
					sprayInterval -= Time.deltaTime;
				}
			}
		}
		else if (isWeedKillerSprayBottle)
		{
			StopKillingWeedLocalClient();
		}
	}

	private void TrySprayingWeedKillerBottle()
	{
		Vector3 vector = GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position - GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward * 0.7f;
		if (!Physics.Raycast(vector, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, out sprayHit, 4.5f, 1073807616, QueryTriggerInteraction.Collide))
		{
			return;
		}
		if (sprayHit.collider.gameObject.layer == 16 && sprayHit.collider.gameObject.CompareTag("MoldSporeCollider"))
		{
			StartKillingWeedLocalClient(sprayHit.collider.transform);
			return;
		}
		StopKillingWeedLocalClient();
		if (addVehicleHPInterval <= 0f)
		{
			addVehicleHPInterval = 0.3f;
			VehicleController vehicleController = UnityEngine.Object.FindObjectOfType<VehicleController>();
			if (vehicleController != null && Vector3.Distance(sprayHit.point, vehicleController.oilPipePoint.position) < 5f)
			{
				if (vehicleController.carHP >= vehicleController.baseCarHP)
				{
					vehicleController.AddTurboBoost();
					Debug.Log("Add turbo boost");
				}
				else
				{
					vehicleController.AddEngineOil();
					Debug.Log("Add turbo boost");
				}
			}
			Debug.DrawRay(sprayHit.point, Vector3.up * 0.5f, Color.red, 1f);
			Debug.DrawLine(vector, sprayHit.point, Color.green, 5f);
		}
		else
		{
			addVehicleHPInterval -= Time.deltaTime;
		}
	}

	private void StopKillingWeedLocalClient()
	{
		if (!(killingWeed == null))
		{
			killingWeed = null;
			StopKillingWeedServerRpc();
		}
	}

	private void StartKillingWeedLocalClient(Transform newWeed)
	{
		if (!(killingWeed == sprayHit.collider.transform))
		{
			killingWeed = newWeed;
			StartKillingWeedServerRpc(newWeed.transform.position);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void StartKillingWeedServerRpc(Vector3 atPosition)
			{
				StartKillingWeedClientRpc(atPosition);
			}

	[ClientRpc]
	public void StartKillingWeedClientRpc(Vector3 atPosition)
{if(!base.IsOwner && Physics.OverlapSphereNonAlloc(atPosition, 0.25f, weedColliders, 65536, QueryTriggerInteraction.Collide) > 0)			{
				killingWeed = weedColliders[0].transform;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void StopKillingWeedServerRpc()
			{
				StopKillingWeedClientRpc();
			}

	[ClientRpc]
	public void StopKillingWeedClientRpc()
{if(!base.IsOwner)			{
				killingWeed = null;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void KillWeedServerRpc(Vector3 weedPos)
			{
				KillWeedClientRpc(weedPos);
			}

	[ClientRpc]
	public void KillWeedClientRpc(Vector3 weedPos)
{if(!base.IsOwner)			{
				UnityEngine.Object.FindObjectOfType<MoldSpreadManager>().DestroyMoldAtPosition(weedPos, playEffect: true);
			}
}
	public bool TrySpraying()
	{
		Debug.DrawRay(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward, Color.magenta, 0.05f);
		if (AddSprayPaintLocal(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position + GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward * 1f, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward))
		{
			SprayPaintServerRpc(GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.position + GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward * 1f, GameNetworkManager.Instance.localPlayerController.gameplayCamera.transform.forward);
			return true;
		}
		return false;
	}

	[ServerRpc]
	public void SprayPaintServerRpc(Vector3 sprayPos, Vector3 sprayRot)
{		{
			SprayPaintClientRpc(sprayPos, sprayRot);
		}
}
	[ClientRpc]
	public void SprayPaintClientRpc(Vector3 sprayPos, Vector3 sprayRot)
{if(!base.IsOwner)			{
				AddSprayPaintLocal(sprayPos, sprayRot);
			}
}
	private void ToggleSprayCollisionOnHolder(bool enable)
	{
		if (playerHeldBy == null)
		{
			Debug.Log("playerheldby is null!!!!!");
		}
		else if (!enable)
		{
			for (int i = 0; i < playerHeldBy.bodyPartSpraypaintColliders.Length; i++)
			{
				playerHeldBy.bodyPartSpraypaintColliders[i].enabled = false;
				playerHeldBy.bodyPartSpraypaintColliders[i].gameObject.layer = 2;
			}
		}
		else
		{
			for (int j = 0; j < playerHeldBy.bodyPartSpraypaintColliders.Length; j++)
			{
				playerHeldBy.bodyPartSpraypaintColliders[j].enabled = false;
				playerHeldBy.bodyPartSpraypaintColliders[j].gameObject.layer = 29;
			}
		}
	}

	private bool AddSprayPaintLocal(Vector3 sprayPos, Vector3 sprayRot)
	{
		if (playerHeldBy == null)
		{
			return false;
		}
		ToggleSprayCollisionOnHolder(enable: false);
		if (RoundManager.Instance.mapPropsContainer == null)
		{
			RoundManager.Instance.mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
		}
		Ray ray = new Ray(sprayPos, sprayRot);
		if (!Physics.Raycast(ray, out sprayHit, 7f, sprayPaintMask, QueryTriggerInteraction.Collide))
		{
			ToggleSprayCollisionOnHolder(enable: true);
			return false;
		}
		if (Vector3.Distance(sprayHit.point, previousSprayPosition) < 0.175f)
		{
			ToggleSprayCollisionOnHolder(enable: true);
			return false;
		}
		if (debugSprayPaint)
		{
			Debug.DrawRay(sprayPos, sprayRot * 7f, Color.green, 5f);
		}
		int num = -1;
		Transform transform;
		if (sprayHit.collider.gameObject.layer == 11 || sprayHit.collider.gameObject.layer == 8 || sprayHit.collider.gameObject.layer == 0)
		{
			transform = ((!playerHeldBy.isInElevator && !StartOfRound.Instance.inShipPhase && !(RoundManager.Instance.mapPropsContainer == null)) ? RoundManager.Instance.mapPropsContainer.transform : StartOfRound.Instance.elevatorTransform);
		}
		else
		{
			if (debugSprayPaint)
			{
				Debug.Log("spray paint parenting to this object : " + sprayHit.collider.gameObject.name);
				Debug.Log($"{sprayHit.collider.tag}; {sprayHit.collider.tag.Length}");
			}
			if (sprayHit.collider.tag.StartsWith("PlayerBody"))
			{
				switch (sprayHit.collider.tag)
				{
				case "PlayerBody":
					num = 0;
					break;
				case "PlayerBody1":
					num = 1;
					break;
				case "PlayerBody2":
					num = 2;
					break;
				case "PlayerBody3":
					num = 3;
					break;
				}
				if (num == (int)playerHeldBy.playerClientId)
				{
					ToggleSprayCollisionOnHolder(enable: true);
					return false;
				}
			}
			else if (sprayHit.collider.tag.StartsWith("PlayerRagdoll"))
			{
				switch (sprayHit.collider.tag)
				{
				case "PlayerRagdoll":
					num = 0;
					break;
				case "PlayerRagdoll1":
					num = 1;
					break;
				case "PlayerRagdoll2":
					num = 2;
					break;
				case "PlayerRagdoll3":
					num = 3;
					break;
				}
			}
			transform = sprayHit.collider.transform;
		}
		sprayPaintDecalsIndex = (sprayPaintDecalsIndex + 1) % maxSprayPaintDecals;
		DecalProjector decalProjector = null;
		GameObject gameObject;
		if (sprayPaintDecals.Count <= sprayPaintDecalsIndex)
		{
			if (debugSprayPaint)
			{
				Debug.Log("Adding to spray paint decals pool");
			}
			for (int i = 0; i < 200; i++)
			{
				if (sprayPaintDecals.Count >= maxSprayPaintDecals)
				{
					break;
				}
				gameObject = UnityEngine.Object.Instantiate(sprayPaintPrefab, transform);
				sprayPaintDecals.Add(gameObject);
				decalProjector = gameObject.GetComponent<DecalProjector>();
				if (decalProjector.material != sprayCanMats[sprayCanMatsIndex])
				{
					decalProjector.material = sprayCanMats[sprayCanMatsIndex];
				}
			}
		}
		if (debugSprayPaint)
		{
			Debug.Log($"Spraypaint B {sprayPaintDecals.Count}; index: {sprayPaintDecalsIndex}");
		}
		if (sprayPaintDecals[sprayPaintDecalsIndex] == null)
		{
			Debug.LogError($"ERROR: spray paint at index {sprayPaintDecalsIndex} is null; creating new object in its place");
			gameObject = UnityEngine.Object.Instantiate(sprayPaintPrefab, transform);
			sprayPaintDecals[sprayPaintDecalsIndex] = gameObject;
		}
		else
		{
			if (!sprayPaintDecals[sprayPaintDecalsIndex].activeSelf)
			{
				sprayPaintDecals[sprayPaintDecalsIndex].SetActive(value: true);
			}
			gameObject = sprayPaintDecals[sprayPaintDecalsIndex];
		}
		decalProjector = gameObject.GetComponent<DecalProjector>();
		if (decalProjector.material != sprayCanMats[sprayCanMatsIndex])
		{
			decalProjector.material = sprayCanMats[sprayCanMatsIndex];
		}
		if (debugSprayPaint)
		{
			Debug.Log($"decal player num: {num}");
		}
		switch (num)
		{
		case 0:
			decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer4;
			break;
		case 1:
			decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer5;
			break;
		case 2:
			decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer6;
			break;
		case 3:
			decalProjector.decalLayerMask = DecalLayerEnum.DecalLayer7;
			break;
		case -1:
			decalProjector.decalLayerMask = DecalLayerEnum.DecalLayerDefault;
			break;
		}
		gameObject.transform.position = ray.GetPoint(sprayHit.distance - 0.1f);
		gameObject.transform.forward = sprayRot;
		if (gameObject.transform.parent != transform)
		{
			gameObject.transform.SetParent(transform);
		}
		previousSprayPosition = sprayHit.point;
		addSprayPaintWithFrameDelay = 2;
		delayedSprayPaintDecal = decalProjector;
		ToggleSprayCollisionOnHolder(enable: true);
		return true;
	}

	public void StartSpraying()
	{
		sprayAudio.clip = spraySFX;
		sprayAudio.Play();
		sprayParticle.Play(withChildren: true);
		isSpraying = true;
		sprayAudio.PlayOneShot(sprayStart);
	}

	public void StopSpraying()
	{
		sprayAudio.Stop();
		sprayParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		isSpraying = false;
		sprayAudio.PlayOneShot(sprayStop);
	}

	public override void PocketItem()
	{
		base.PocketItem();
		if (playerHeldBy != null)
		{
			playerHeldBy.activatingItem = false;
			playerHeldBy.equippedUsableItemQE = false;
		}
		StopSpraying();
	}

	public override void DiscardItem()
	{
		if (playerHeldBy != null)
		{
			playerHeldBy.activatingItem = false;
			playerHeldBy.equippedUsableItemQE = false;
		}
		base.DiscardItem();
		StopSpraying();
	}
}
