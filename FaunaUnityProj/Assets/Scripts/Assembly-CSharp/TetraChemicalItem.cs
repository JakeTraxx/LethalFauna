using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class TetraChemicalItem : GrabbableObject
{
	private PlayerControllerB previousPlayerHeldBy;

	private Coroutine useTZPCoroutine;

	private bool emittingGas;

	private float fuel = 1f;

	public AudioSource localHelmetSFX;

	public AudioSource thisAudioSource;

	public AudioClip twistCanSFX;

	public AudioClip releaseGasSFX;

	public AudioClip holdCanSFX;

	public AudioClip removeCanSFX;

	public AudioClip outOfGasSFX;

	private bool triedUsingWithoutFuel;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (buttonDown)
		{
			isBeingUsed = true;
			if (fuel <= 0f)
			{
				if (!triedUsingWithoutFuel)
				{
					triedUsingWithoutFuel = true;
					thisAudioSource.PlayOneShot(outOfGasSFX);
					WalkieTalkie.TransmitOneShotAudio(thisAudioSource, outOfGasSFX);
					previousPlayerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
				}
				return;
			}
			previousPlayerHeldBy = playerHeldBy;
			useTZPCoroutine = StartCoroutine(UseTZPAnimation());
		}
		else
		{
			isBeingUsed = false;
			if (triedUsingWithoutFuel)
			{
				triedUsingWithoutFuel = false;
			}
			else if (useTZPCoroutine != null)
			{
				StopCoroutine(useTZPCoroutine);
				emittingGas = false;
				previousPlayerHeldBy.activatingItem = false;
				thisAudioSource.Stop();
				localHelmetSFX.Stop();
				thisAudioSource.PlayOneShot(removeCanSFX);
			}
		}
		if (base.IsOwner)
		{
			previousPlayerHeldBy.activatingItem = buttonDown;
			previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", buttonDown);
		}
	}

	private IEnumerator UseTZPAnimation()
	{
		thisAudioSource.PlayOneShot(holdCanSFX);
		WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, holdCanSFX);
		yield return new WaitForSeconds(0.75f);
		emittingGas = true;
		HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: true);
		if (base.IsOwner)
		{
			localHelmetSFX.Play();
			localHelmetSFX.PlayOneShot(twistCanSFX);
		}
		else
		{
			thisAudioSource.clip = releaseGasSFX;
			thisAudioSource.Play();
			thisAudioSource.PlayOneShot(twistCanSFX);
		}
		WalkieTalkie.TransmitOneShotAudio(previousPlayerHeldBy.itemAudio, twistCanSFX);
	}

	public override void Update()
	{
		if (emittingGas)
		{
			if (previousPlayerHeldBy == null || !isHeld || fuel <= 0f)
			{
				emittingGas = false;
				thisAudioSource.Stop();
				localHelmetSFX.Stop();
				RunOutOfFuelServerRpc();
			}
			previousPlayerHeldBy.drunknessInertia = Mathf.Clamp(previousPlayerHeldBy.drunknessInertia + Time.deltaTime / 1.75f * previousPlayerHeldBy.drunknessSpeed, 0.1f, 3f);
			previousPlayerHeldBy.increasingDrunknessThisFrame = true;
			fuel -= Time.deltaTime / 38f;
		}
		base.Update();
	}

	public override void EquipItem()
	{
		base.EquipItem();
		StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
		if (playerHeldBy != null)
		{
			previousPlayerHeldBy = playerHeldBy;
		}
	}

	[ServerRpc]
	public void RunOutOfFuelServerRpc()
{		{
			RunOutOfFuelClientRpc();
		}
}
	[ClientRpc]
	public void RunOutOfFuelClientRpc()
			{
				itemUsedUp = true;
				emittingGas = false;
				fuel = 0f;
				thisAudioSource.Stop();
				localHelmetSFX.Stop();
			}

	public override void DiscardItem()
	{
		emittingGas = false;
		thisAudioSource.Stop();
		localHelmetSFX.Stop();
		playerHeldBy.playerBodyAnimator.ResetTrigger("shakeItem");
		previousPlayerHeldBy.playerBodyAnimator.SetBool("useTZPItem", value: false);
		if (previousPlayerHeldBy != null)
		{
			previousPlayerHeldBy.activatingItem = false;
		}
		base.DiscardItem();
	}
}
