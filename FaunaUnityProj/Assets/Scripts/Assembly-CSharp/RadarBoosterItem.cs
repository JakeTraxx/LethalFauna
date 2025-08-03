using System;
using Unity.Netcode;
using UnityEngine;

public class RadarBoosterItem : GrabbableObject
{
	public bool radarEnabled;

	public Animator radarBoosterAnimator;

	public GameObject radarDot;

	public AudioSource pingAudio;

	public AudioClip pingSFX;

	public AudioSource radarBoosterAudio;

	public AudioClip turnOnSFX;

	public AudioClip turnOffSFX;

	public AudioClip flashSFX;

	public string radarBoosterName;

	private bool setRandomBoosterName;

	private int timesPlayingPingAudioInOneSpot;

	private Vector3 pingAudioLastPosition;

	private float timeSincePlayingPingAudio;

	private int radarBoosterNameIndex = -1;

	private float flashCooldown;

	public Transform radarSpherePosition;

	public override void Start()
	{
		base.Start();
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		if (radarEnabled)
		{
			RemoveBoosterFromRadar();
		}
	}

	public override int GetItemDataToSave()
	{
		base.GetItemDataToSave();
		return radarBoosterNameIndex;
	}

	public override void LoadItemSaveData(int saveData)
	{
		base.LoadItemSaveData(saveData);
		radarBoosterNameIndex = saveData;
	}

	public void FlashAndSync()
	{
		Flash();
		RadarBoosterFlashServerRpc();
	}

	[ServerRpc(RequireOwnership = false)]
	public void RadarBoosterFlashServerRpc()
			{
				RadarBoosterFlashClientRpc();
			}

	[ClientRpc]
	public void RadarBoosterFlashClientRpc()
{if(!(flashCooldown >= 0f))			{
				Flash();
			}
}
	public void Flash()
	{
		if (radarEnabled && !(flashCooldown >= 0f))
		{
			flashCooldown = 2.25f;
			radarBoosterAnimator.SetTrigger("Flash");
			radarBoosterAudio.PlayOneShot(flashSFX);
			WalkieTalkie.TransmitOneShotAudio(radarBoosterAudio, flashSFX);
			StunGrenadeItem.StunExplosion(radarSpherePosition.position, affectAudio: false, 0.8f, 1.75f, 2f, isHeldItem: false, null, null, 0.3f);
		}
	}

	public void SetRadarBoosterNameLocal(string newName)
	{
		radarBoosterName = newName;
		base.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = radarBoosterName;
		StartOfRound.Instance.mapScreen.ChangeNameOfTargetTransform(base.transform, newName);
	}

	private void RemoveBoosterFromRadar()
	{
		StartOfRound.Instance.mapScreen.RemoveTargetFromRadar(base.transform);
	}

	private void AddBoosterToRadar()
	{
		if (!setRandomBoosterName)
		{
			setRandomBoosterName = true;
			int num = (radarBoosterNameIndex = ((radarBoosterNameIndex != -1) ? radarBoosterNameIndex : new System.Random(Mathf.Min(StartOfRound.Instance.randomMapSeed + (int)base.NetworkObjectId, 99999999)).Next(0, StartOfRound.Instance.randomNames.Length)));
			radarBoosterName = StartOfRound.Instance.randomNames[num];
			base.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = radarBoosterName;
		}
		string text = StartOfRound.Instance.mapScreen.AddTransformAsTargetToRadar(base.transform, radarBoosterName, isNonPlayer: true);
		if (!string.IsNullOrEmpty(text))
		{
			base.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText = text;
		}
		StartOfRound.Instance.mapScreen.SyncOrderOfRadarBoostersInList();
	}

	public void EnableRadarBooster(bool enable)
	{
		radarBoosterAnimator.SetBool("on", enable);
		radarDot.SetActive(enable);
		if (enable)
		{
			AddBoosterToRadar();
			radarBoosterAudio.Play();
			radarBoosterAudio.PlayOneShot(turnOnSFX);
			WalkieTalkie.TransmitOneShotAudio(radarBoosterAudio, turnOnSFX);
		}
		else
		{
			RemoveBoosterFromRadar();
			if (radarBoosterAudio.isPlaying)
			{
				radarBoosterAudio.Stop();
				radarBoosterAudio.PlayOneShot(turnOffSFX);
				WalkieTalkie.TransmitOneShotAudio(radarBoosterAudio, turnOffSFX);
			}
		}
		radarEnabled = enable;
	}

	public void PlayPingAudio()
	{
		timesPlayingPingAudioInOneSpot += 2;
		timeSincePlayingPingAudio = 0f;
		pingAudio.PlayOneShot(pingSFX);
		WalkieTalkie.TransmitOneShotAudio(pingAudio, pingSFX);
		RoundManager.Instance.PlayAudibleNoise(pingAudio.transform.position, 12f, 0.8f, timesPlayingPingAudioInOneSpot, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed, 1015);
	}

	public void PlayPingAudioAndSync()
	{
		PlayPingAudio();
		PingRadarBoosterServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
	}

	[ServerRpc(RequireOwnership = false)]
	public void PingRadarBoosterServerRpc(int playerWhoPlayedPing)
			{
				PingRadarBoosterClientRpc(playerWhoPlayedPing);
			}

	[ClientRpc]
	public void PingRadarBoosterClientRpc(int playerWhoPlayedPing)
{if(!(GameNetworkManager.Instance.localPlayerController == null) && playerWhoPlayedPing != (int)GameNetworkManager.Instance.localPlayerController.playerClientId)			{
				PlayPingAudio();
			}
}
	public override void EquipItem()
	{
		base.EquipItem();
		pingAudioLastPosition = base.transform.position;
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		base.ItemActivate(used, buttonDown);
		EnableRadarBooster(used);
	}

	public override void PocketItem()
	{
		base.PocketItem();
		isBeingUsed = false;
		EnableRadarBooster(enable: false);
	}

	public override void DiscardItem()
	{
		if (Vector3.Distance(base.transform.position, pingAudioLastPosition) > 5f)
		{
			timesPlayingPingAudioInOneSpot = 0;
		}
		base.DiscardItem();
	}

	public override void Update()
	{
		base.Update();
		if (timeSincePlayingPingAudio > 5f)
		{
			timeSincePlayingPingAudio = 0f;
			timesPlayingPingAudioInOneSpot = Mathf.Max(timesPlayingPingAudioInOneSpot - 1, 0);
		}
		else
		{
			timeSincePlayingPingAudio += Time.deltaTime;
		}
		if (flashCooldown >= 0f)
		{
			flashCooldown -= Time.deltaTime;
		}
	}
}
