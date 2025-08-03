using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class FlashlightItem : GrabbableObject
{
	[Space(15f)]
	public bool usingPlayerHelmetLight;

	public int flashlightInterferenceLevel;

	public static int globalFlashlightInterferenceLevel;

	public Light flashlightBulb;

	public Light flashlightBulbGlow;

	public AudioSource flashlightAudio;

	public AudioClip[] flashlightClips;

	public AudioClip outOfBatteriesClip;

	public AudioClip flashlightFlicker;

	public Material bulbLight;

	public Material bulbDark;

	public MeshRenderer flashlightMesh;

	public int flashlightTypeID;

	public bool changeMaterial = true;

	private float initialIntensity;

	private PlayerControllerB previousPlayerHeldBy;

	public override void Start()
	{
		base.Start();
		initialIntensity = flashlightBulb.intensity;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (flashlightInterferenceLevel < 2)
		{
			SwitchFlashlight(used);
		}
		flashlightAudio.PlayOneShot(flashlightClips[Random.Range(0, flashlightClips.Length)]);
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 7f, 0.4f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
	}

	public override void UseUpBatteries()
	{
		base.UseUpBatteries();
		SwitchFlashlight(on: false);
		flashlightAudio.PlayOneShot(outOfBatteriesClip, 1f);
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 13f, 0.65f, 0, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
	}

	public override void PocketItem()
	{
		if (!base.IsOwner)
		{
			base.PocketItem();
			return;
		}
		if (previousPlayerHeldBy != null)
		{
			flashlightBulb.enabled = false;
			flashlightBulbGlow.enabled = false;
			if (isBeingUsed && (previousPlayerHeldBy.ItemSlots[previousPlayerHeldBy.currentItemSlot] == null || previousPlayerHeldBy.ItemSlots[previousPlayerHeldBy.currentItemSlot].itemProperties.itemId != 1 || previousPlayerHeldBy.ItemSlots[previousPlayerHeldBy.currentItemSlot].itemProperties.itemId != 6))
			{
				previousPlayerHeldBy.helmetLight.enabled = true;
				previousPlayerHeldBy.pocketedFlashlight = this;
				usingPlayerHelmetLight = true;
				PocketFlashlightServerRpc(stillUsingFlashlight: true);
			}
			else
			{
				isBeingUsed = false;
				usingPlayerHelmetLight = false;
				flashlightBulbGlow.enabled = false;
				SwitchFlashlight(on: false);
				PocketFlashlightServerRpc();
			}
		}
		else
		{
			Debug.Log("Could not find what player was holding this flashlight item");
		}
		base.PocketItem();
	}

	[ServerRpc]
	public void PocketFlashlightServerRpc(bool stillUsingFlashlight = false)
{		{
			PocketFlashlightClientRpc(stillUsingFlashlight);
		}
}
	[ClientRpc]
	public void PocketFlashlightClientRpc(bool stillUsingFlashlight)
{if(base.IsOwner)		{
			return;
		}
		flashlightBulb.enabled = false;
		flashlightBulbGlow.enabled = false;
		if (stillUsingFlashlight)
		{
			if (!(previousPlayerHeldBy == null))
			{
				previousPlayerHeldBy.helmetLight.enabled = true;
				previousPlayerHeldBy.pocketedFlashlight = this;
				usingPlayerHelmetLight = true;
			}
		}
		else
		{
			isBeingUsed = false;
			usingPlayerHelmetLight = false;
			flashlightBulbGlow.enabled = false;
			SwitchFlashlight(on: false);
		}
}
	public override void DiscardItem()
	{
		if (previousPlayerHeldBy != null)
		{
			previousPlayerHeldBy.helmetLight.enabled = false;
			flashlightBulb.enabled = isBeingUsed;
			flashlightBulbGlow.enabled = isBeingUsed;
		}
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		previousPlayerHeldBy = playerHeldBy;
		playerHeldBy.ChangeHelmetLight(flashlightTypeID);
		playerHeldBy.helmetLight.enabled = false;
		usingPlayerHelmetLight = false;
		if (isBeingUsed)
		{
			SwitchFlashlight(on: true);
		}
		base.EquipItem();
	}

	public void SwitchFlashlight(bool on)
	{
		isBeingUsed = on;
		if (!base.IsOwner)
		{
			Debug.Log($"Flashlight click. playerheldby null?: {playerHeldBy != null}");
			Debug.Log($"Flashlight being disabled or enabled: {on}");
			if (playerHeldBy != null)
			{
				playerHeldBy.ChangeHelmetLight(flashlightTypeID, on);
			}
			flashlightBulb.enabled = false;
			flashlightBulbGlow.enabled = false;
		}
		else
		{
			flashlightBulb.enabled = on;
			flashlightBulbGlow.enabled = on;
		}
		if (usingPlayerHelmetLight && playerHeldBy != null)
		{
			playerHeldBy.helmetLight.enabled = on;
		}
		if (changeMaterial)
		{
			Material[] sharedMaterials = flashlightMesh.sharedMaterials;
			if (on)
			{
				sharedMaterials[1] = bulbLight;
			}
			else
			{
				sharedMaterials[1] = bulbDark;
			}
			flashlightMesh.sharedMaterials = sharedMaterials;
		}
	}

	public override void Update()
	{
		base.Update();
		int num = ((flashlightInterferenceLevel <= globalFlashlightInterferenceLevel) ? globalFlashlightInterferenceLevel : flashlightInterferenceLevel);
		if (num >= 2)
		{
			flashlightBulb.intensity = 0f;
		}
		else if (num == 1)
		{
			flashlightBulb.intensity = Random.Range(0f, 200f);
		}
		else
		{
			flashlightBulb.intensity = initialIntensity;
		}
	}
}
