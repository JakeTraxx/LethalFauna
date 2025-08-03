using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class JetpackItem : GrabbableObject
{
	public bool streamlineJetpack;

	public Transform backPart;

	public Vector3 backPartRotationOffset;

	public Vector3 backPartPositionOffset;

	private float jetpackPower;

	private bool jetpackActivated;

	private Vector3 forces;

	private bool jetpackActivatedPreviousFrame;

	public GameObject fireEffect;

	public AudioSource jetpackAudio;

	public AudioSource jetpackBeepsAudio;

	public AudioClip startJetpackSFX;

	public AudioClip jetpackSustainSFX;

	public AudioClip jetpackBrokenSFX;

	public AudioClip jetpackWarningBeepSFX;

	public AudioClip jetpackLowBatteriesSFX;

	public ParticleSystem smokeTrailParticle;

	private PlayerControllerB previousPlayerHeldBy;

	private bool jetpackBroken;

	private bool jetpackPlayingWarningBeep;

	private bool jetpackPlayingLowBatteryBeep;

	private float noiseInterval;

	private RaycastHit rayHit;

	public float jetpackAcceleration = 4f;

	public float jetpackDeaccelleration = 75f;

	public float jetpackForceChangeSpeed;

	public float verticalMultiplier = 0.5f;

	public AudioClip applause;

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		if (buttonDown)
		{
			ActivateJetpack();
		}
		else
		{
			DeactivateJetpack();
		}
	}

	private void DeactivateJetpack()
	{
		if (previousPlayerHeldBy.jetpackControls)
		{
			previousPlayerHeldBy.disablingJetpackControls = true;
		}
		jetpackActivated = false;
		jetpackActivatedPreviousFrame = false;
		jetpackPlayingLowBatteryBeep = false;
		jetpackPlayingWarningBeep = false;
		jetpackBeepsAudio.Stop();
		jetpackAudio.Stop();
		smokeTrailParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		if (!jetpackBroken)
		{
			JetpackEffect(enable: false);
		}
	}

	private void ActivateJetpack()
	{
		if (streamlineJetpack)
		{
			if (Vector3.Distance(playerHeldBy.transform.position, StartOfRound.Instance.elevatorTransform.position) < 40f)
			{
				EntranceTeleport[] array = Object.FindObjectsOfType<EntranceTeleport>();
				for (int i = 0; i < array.Length; i++)
				{
					if (array[i].entranceId == 0 && array[i].isEntranceToBuilding)
					{
						playerHeldBy.TeleportPlayer(array[i].entrancePoint.position);
						playerHeldBy.SetAllItemsInElevator(inShipRoom: false, inElevator: false);
						playerHeldBy.isInElevator = false;
						playerHeldBy.isInHangarShipRoom = false;
						AudioReverbPresets audioReverbPresets = Object.FindObjectOfType<AudioReverbPresets>();
						if (audioReverbPresets != null)
						{
							audioReverbPresets.audioPresets[2].ChangeAudioReverbForPlayer(playerHeldBy);
						}
					}
				}
			}
			else
			{
				playerHeldBy.TeleportPlayer(StartOfRound.Instance.outsideShipSpawnPosition.position);
				playerHeldBy.SetAllItemsInElevator(inShipRoom: false, inElevator: true);
				playerHeldBy.isInElevator = true;
				playerHeldBy.isInsideFactory = false;
				AudioReverbPresets audioReverbPresets2 = Object.FindObjectOfType<AudioReverbPresets>();
				if (audioReverbPresets2 != null)
				{
					audioReverbPresets2.audioPresets[3].ChangeAudioReverbForPlayer(playerHeldBy);
				}
			}
			if (playerHeldBy == GameNetworkManager.Instance.localPlayerController)
			{
				SoundManager.Instance.misc2DAudio.volume = 1f;
				SoundManager.Instance.misc2DAudio.PlayOneShot(applause);
			}
		}
		if (jetpackBroken)
		{
			jetpackAudio.PlayOneShot(jetpackBrokenSFX);
			return;
		}
		if (!jetpackActivatedPreviousFrame)
		{
			playerHeldBy.jetpackTurnCompass.rotation = playerHeldBy.transform.rotation;
			JetpackEffect(enable: true);
			jetpackActivatedPreviousFrame = true;
		}
		playerHeldBy.disablingJetpackControls = false;
		playerHeldBy.jetpackControls = true;
		jetpackActivated = true;
		playerHeldBy.syncFullRotation = playerHeldBy.transform.eulerAngles;
	}

	private void JetpackEffect(bool enable)
	{
		if (streamlineJetpack)
		{
			return;
		}
		fireEffect.SetActive(enable);
		if (enable)
		{
			if (!jetpackActivatedPreviousFrame)
			{
				jetpackAudio.PlayOneShot(startJetpackSFX);
			}
			smokeTrailParticle.Play();
			jetpackAudio.clip = jetpackSustainSFX;
			jetpackAudio.Play();
			Debug.Log($"Is jetpack audio playing?: {jetpackAudio.isPlaying}");
		}
		else
		{
			smokeTrailParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
			jetpackAudio.Stop();
		}
		if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) < 10f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
		}
	}

	public override void UseUpBatteries()
	{
		DeactivateJetpack();
	}

	public override void DiscardItem()
	{
		Debug.Log($"Owner of jetpack?: {base.IsOwner}");
		Debug.Log($"Is dead?: {playerHeldBy.isPlayerDead}");
		if (base.IsOwner && playerHeldBy.isPlayerDead && !jetpackBroken && playerHeldBy.jetpackControls)
		{
			ExplodeJetpackServerRpc();
		}
		JetpackEffect(enable: false);
		DeactivateJetpack();
		jetpackPower = 0f;
		base.DiscardItem();
	}

	[ServerRpc(RequireOwnership = false)]
	public void ExplodeJetpackServerRpc()
			{
				ExplodeJetpackClientRpc();
			}

	[ClientRpc]
	public void ExplodeJetpackClientRpc()
{if(!jetpackBroken)			{
				jetpackBroken = true;
				itemUsedUp = true;
				Debug.Log("Spawning explosion");
				Landmine.SpawnExplosion(base.transform.position, spawnExplosionEffect: true, 5f, 6f);
			}
}
	public override void EquipItem()
	{
		base.EquipItem();
		previousPlayerHeldBy = playerHeldBy;
	}

	public override void Update()
	{
		base.Update();
		if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || streamlineJetpack)
		{
			return;
		}
		SetJetpackAudios();
		if (playerHeldBy == null || !base.IsOwner || playerHeldBy != GameNetworkManager.Instance.localPlayerController)
		{
			return;
		}
		if (jetpackActivated)
		{
			jetpackPower = Mathf.Clamp(jetpackPower + Time.deltaTime * jetpackAcceleration, 0f, 500f);
		}
		else
		{
			jetpackPower = Mathf.Clamp(jetpackPower - Time.deltaTime * jetpackDeaccelleration, 0f, 1000f);
			if (playerHeldBy.thisController.isGrounded)
			{
				jetpackPower = 0f;
			}
		}
		forces = Vector3.Lerp(forces, Vector3.ClampMagnitude(playerHeldBy.transform.up * jetpackPower, 400f), Time.deltaTime * 50f);
		if (!playerHeldBy.jetpackControls || (jetpackPower > 10f && playerHeldBy.thisController.isGrounded))
		{
			forces = Vector3.zero;
		}
		if (!playerHeldBy.isPlayerDead && Physics.Raycast(playerHeldBy.transform.position, forces, out rayHit, 25f, StartOfRound.Instance.allPlayersCollideWithMask, QueryTriggerInteraction.Ignore) && forces.magnitude - rayHit.distance > 50f && rayHit.distance < 4f)
		{
			playerHeldBy.KillPlayer(forces, spawnBody: true, CauseOfDeath.Gravity);
		}
		if (playerHeldBy != null && !playerHeldBy.isPlayerDead)
		{
			playerHeldBy.externalForces += forces;
		}
	}

	private void SetJetpackAudios()
	{
		if (jetpackActivated)
		{
			if (noiseInterval >= 0.5f)
			{
				noiseInterval = 0f;
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, 25f, 0.85f, 0, playerHeldBy.isInHangarShipRoom && StartOfRound.Instance.hangarDoorsClosed, 41);
			}
			else
			{
				noiseInterval += Time.deltaTime;
			}
			if (insertedBattery.charge < 0.15f)
			{
				if (!jetpackPlayingLowBatteryBeep)
				{
					jetpackPlayingLowBatteryBeep = true;
					jetpackBeepsAudio.clip = jetpackLowBatteriesSFX;
					jetpackBeepsAudio.Play();
				}
			}
			else if (Physics.CheckSphere(base.transform.position, 6f, StartOfRound.Instance.collidersAndRoomMaskAndDefault))
			{
				if (!jetpackPlayingWarningBeep && !jetpackPlayingLowBatteryBeep)
				{
					jetpackPlayingWarningBeep = true;
					jetpackBeepsAudio.clip = jetpackWarningBeepSFX;
					jetpackBeepsAudio.Play();
				}
			}
			else
			{
				jetpackBeepsAudio.Stop();
			}
		}
		else
		{
			jetpackPlayingWarningBeep = false;
			jetpackPlayingLowBatteryBeep = false;
			jetpackBeepsAudio.Stop();
		}
	}

	public override void LateUpdate()
	{
		base.LateUpdate();
		if (playerHeldBy != null && isHeld)
		{
			backPart.position = playerHeldBy.lowerSpine.position;
			backPart.rotation = playerHeldBy.lowerSpine.rotation;
			base.transform.Rotate(backPartRotationOffset);
			backPart.position = playerHeldBy.lowerSpine.position;
			Vector3 vector = backPartPositionOffset;
			vector = playerHeldBy.lowerSpine.rotation * vector;
			backPart.position += vector;
		}
	}
}
