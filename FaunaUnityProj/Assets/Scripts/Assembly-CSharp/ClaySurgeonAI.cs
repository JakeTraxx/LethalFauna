using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class ClaySurgeonAI : EnemyAI
{
	public float minDistance;

	public float maxDistance;

	[Space(5f)]
	public float jumpSpeed = 5f;

	public float jumpTime = 0.25f;

	public float startingInterval = 2f;

	public float endingInterval = 0.2f;

	public int snareBeatAmount;

	[Space(3f)]
	public float currentInterval;

	private float beatTimer;

	[Space(5f)]
	public ClaySurgeonAI master;

	private bool isMaster;

	[Space(5f)]
	public bool isJumping;

	private float jumpTimer;

	private bool jumpingLastFrame;

	private int beatsSinceSeeingPlayer;

	private bool hasLOS;

	public SimpleEvent SendDanceBeat;

	private int currentHour;

	private bool jumpCycle;

	public AISearchRoutine searchRoutine;

	private float timeSinceSnip;

	private float snareIntervalTimer;

	public AudioSource musicAudio;

	public AudioSource musicAudio2;

	public AudioClip snareDrum;

	public AudioClip[] paradeClips;

	public AudioClip snipScissors;

	private int previousParadeClip;

	public List<ClaySurgeonAI> allClaySurgeons;

	private int snareNum;

	public float snareOffset;

	public MeshRenderer[] scissorBlades;

	public SkinnedMeshRenderer skin;

	public Material scissorGuyMat;

	private Material thisMaterial;

	private RaycastHit hit;

	private bool listeningToMasterSurgeon;

	private bool choseMasterSurgeon;

	private void Awake()
	{
		thisMaterial = scissorBlades[0].material;
		scissorBlades[1].sharedMaterial = thisMaterial;
		skin.sharedMaterial = thisMaterial;
	}

	[ClientRpc]
	public void SyncMasterClaySurgeonClientRpc()
{if(base.IsServer)		{
			return;
		}
		master = this;
		isMaster = true;
		TimeOfDay.Instance.onHourChanged.AddListener(HourChanged);
		SendDanceBeat = new SimpleEvent();
		allClaySurgeons = Object.FindObjectsByType<ClaySurgeonAI>(FindObjectsSortMode.None).ToList();
		for (int i = 0; i < allClaySurgeons.Count; i++)
		{
			if (!(allClaySurgeons[i] == this))
			{
				allClaySurgeons[i].master = this;
				allClaySurgeons[i].ListenToMasterSurgeon();
				Object.Destroy(allClaySurgeons[i].musicAudio2.gameObject);
			}
		}
}
	public void ListenToMasterSurgeon()
	{
		if (!listeningToMasterSurgeon)
		{
			listeningToMasterSurgeon = true;
			master.SendDanceBeat.AddListener(DanceBeat);
			if (musicAudio2 != null)
			{
				Object.Destroy(musicAudio2.gameObject);
			}
			if (!master.allClaySurgeons.Contains(this))
			{
				master.allClaySurgeons.Add(this);
			}
		}
	}

	public override void Start()
	{
		base.Start();
		HourChanged();
	}

	private void LateUpdate()
	{
		if (!choseMasterSurgeon)
		{
			choseMasterSurgeon = true;
			ChooseMasterSurgeon();
		}
	}

	private void ChooseMasterSurgeon()
	{
		if (!base.IsServer)
		{
			return;
		}
		if (base.IsServer && !listeningToMasterSurgeon)
		{
			ClaySurgeonAI[] array = Object.FindObjectsByType<ClaySurgeonAI>(FindObjectsSortMode.None);
			int num = 1000;
			int num2 = -50;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].thisEnemyIndex < num)
				{
					num = array[i].thisEnemyIndex;
					num2 = i;
				}
			}
			master = array[num2];
			if (master == this)
			{
				isMaster = true;
				TimeOfDay.Instance.onHourChanged.AddListener(HourChanged);
				SendDanceBeat = new SimpleEvent();
				allClaySurgeons = array.ToList();
				for (int j = 0; j < allClaySurgeons.Count; j++)
				{
					if (!(allClaySurgeons[j] == this))
					{
						allClaySurgeons[j].master = this;
						allClaySurgeons[j].ListenToMasterSurgeon();
					}
				}
				SyncMasterClaySurgeonClientRpc();
			}
			if (!isMaster && musicAudio2 != null)
			{
				Object.Destroy(musicAudio2.gameObject);
			}
		}
		else if (musicAudio2 != null)
		{
			Object.Destroy(musicAudio2.gameObject);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (isMaster)
		{
			TimeOfDay.Instance.onHourChanged.RemoveListener(HourChanged);
		}
		else if (master != null)
		{
			master.allClaySurgeons.Remove(this);
			if (master.SendDanceBeat != null)
			{
				master.SendDanceBeat.RemoveListener(DanceBeat);
			}
		}
	}

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (isEnemyDead || StartOfRound.Instance.allPlayersDead)
		{
			return;
		}
		PlayerControllerB playerControllerB = targetPlayer;
		if (TargetClosestPlayer(5f, requireLineOfSight: true, 120f))
		{
			hasLOS = true;
			beatsSinceSeeingPlayer = 0;
			if (searchRoutine.inProgress)
			{
				StopSearch(searchRoutine);
			}
		}
		else if (beatsSinceSeeingPlayer > 6)
		{
			if (hasLOS)
			{
				hasLOS = false;
			}
			if (!searchRoutine.inProgress)
			{
				StartSearch(base.transform.position, searchRoutine);
			}
		}
		else if (playerControllerB != null)
		{
			targetPlayer = playerControllerB;
		}
	}

	private void PlayMusic()
	{
		if (snareIntervalTimer <= 0f)
		{
			snareIntervalTimer = 100f;
			musicAudio.PlayOneShot(snareDrum);
			WalkieTalkie.TransmitOneShotAudio(musicAudio, snareDrum);
			if (isMaster)
			{
				musicAudio2.PlayOneShot(snareDrum);
			}
		}
		else
		{
			snareIntervalTimer -= Time.deltaTime;
		}
	}

	private void SetMusicVolume()
	{
		float num = 0f;
		float num2 = 1000f;
		for (int i = 0; i < allClaySurgeons.Count; i++)
		{
			if (!(allClaySurgeons[i] == null) && !allClaySurgeons[i].isEnemyDead)
			{
				num = Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, allClaySurgeons[i].transform.position);
				if (num < num2)
				{
					num2 = num;
				}
			}
		}
		if (num2 < 40f)
		{
			float b = ((!(num2 < 20f)) ? Mathf.Lerp(1f, 0.25f, num2 / 50f) : Mathf.Lerp(0.4f, 0f, num2 / 10f));
			musicAudio2.volume = Mathf.Lerp(musicAudio2.volume, b, 2.5f * Time.deltaTime);
		}
		else
		{
			musicAudio2.volume = Mathf.Lerp(musicAudio2.volume, 0f, 4f * Time.deltaTime);
		}
	}

	private void SetVisibility()
	{
		float num = Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.transform.position + Vector3.up * 0.7f);
		thisMaterial.SetFloat("_AlphaCutoff", (num - minDistance) / (maxDistance - minDistance));
		PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
		if (!localPlayerController.isPlayerDead && localPlayerController != null && num < 15f && num > maxDistance + 2f)
		{
			localPlayerController.IncreaseFearLevelOverTime(0.37f, 0.25f);
		}
	}

	public override void Update()
	{
		base.Update();
		SetVisibility();
		if (isMaster)
		{
			SetDanceClock();
			SetMusicVolume();
		}
		if (StartOfRound.Instance.allPlayersDead || master == null)
		{
			return;
		}
		if (isMaster)
		{
			PlayMusic();
		}
		timeSinceSnip += Time.deltaTime;
		if (!base.IsOwner)
		{
			return;
		}
		if (isEnemyDead || stunNormalizedTimer > 0f)
		{
			agent.speed = 0f;
		}
		else if (isJumping)
		{
			if (timeSinceSnip < 0.7f)
			{
				timeSinceSnip += Time.deltaTime;
				agent.speed = 0f;
			}
			jumpTimer -= Time.deltaTime;
			if (jumpTimer <= 0f)
			{
				isJumping = false;
				agent.speed = 0f;
			}
			else
			{
				agent.speed = jumpSpeed;
			}
		}
		if (isMaster)
		{
			if (beatTimer <= 0f)
			{
				beatTimer = currentInterval;
				DoBeatOnOwnerClient();
			}
			else
			{
				beatTimer -= Time.deltaTime;
			}
		}
	}

	public override void OnCollideWithPlayer(Collider other)
	{
		base.OnCollideWithPlayer(other);
		if (!(timeSinceSnip < 1f))
		{
			PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
			if (playerControllerB != null)
			{
				playerControllerB.KillPlayer(Vector3.up * 14f, spawnBody: true, CauseOfDeath.Snipped, 7);
				KillPlayerServerRpc();
			}
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void KillPlayerServerRpc()
			{
				KillPlayerClientRpc();
			}

	[ClientRpc]
	public void KillPlayerClientRpc()
{		{
			if (base.IsOwner)
			{
				agent.speed = 0f;
				beatTimer += 2f;
			}
			snareIntervalTimer = Mathf.Min(snareIntervalTimer + 2f, 4f);
			timeSinceSnip = 0f;
			creatureAnimator.SetTrigger("snip");
			creatureSFX.PlayOneShot(snipScissors);
		}
}
	private void DoBeatOnOwnerClient()
	{
		DanceBeat();
		SendDanceBeat.Invoke();
		DoBeatServerRpc();
	}

	[ServerRpc]
	public void DoBeatServerRpc()
{		{
			DoBeatClientRpc();
		}
}
	[ClientRpc]
	public void DoBeatClientRpc()
{if(!base.IsOwner)		{
			if (isMaster)
			{
				DanceBeat();
			}
			if (SendDanceBeat != null)
			{
				SendDanceBeat.Invoke();
			}
		}
}
	private void HourChanged()
	{
		currentInterval = Mathf.Clamp(Mathf.Lerp(startingInterval, endingInterval, (float)TimeOfDay.Instance.hour / (float)TimeOfDay.Instance.numberOfHours), endingInterval, startingInterval);
	}

	private void DanceBeat()
	{
		if (stunNormalizedTimer > 0f)
		{
			return;
		}
		isJumping = true;
		jumpTimer = jumpTime;
		jumpCycle = !jumpCycle;
		creatureAnimator.SetBool("walkCycle", jumpCycle);
		if (!base.IsOwner)
		{
			return;
		}
		beatsSinceSeeingPlayer++;
		if (hasLOS && targetPlayer != null)
		{
			Vector3 vector = targetPlayer.transform.position;
			if (Physics.Raycast(targetPlayer.transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 7f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
			{
				vector = hit.point;
			}
			Vector3 onUnitSphere = Random.onUnitSphere;
			onUnitSphere.y = 0f;
			Ray ray = new Ray(vector, onUnitSphere);
			float distance = Vector3.Distance(base.transform.position, vector);
			vector = ((!Physics.Raycast(ray, out hit, distance, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore)) ? ray.GetPoint(distance) : hit.point);
			SetDestinationToPosition(vector);
		}
	}

	public override void AnimationEventA()
	{
		base.AnimationEventA();
		if (base.IsOwner)
		{
			SyncPositionToClients();
		}
		if (!(timeSinceSnip < 0.4f))
		{
			int num = Random.Range(0, paradeClips.Length);
			if (num == previousParadeClip)
			{
				num = (num + 1) % paradeClips.Length;
			}
			previousParadeClip = num;
			float pitch = Random.Range(0.95f, 1.05f);
			musicAudio.pitch = pitch;
			musicAudio.PlayOneShot(paradeClips[num]);
			WalkieTalkie.TransmitOneShotAudio(musicAudio, paradeClips[num]);
			snareIntervalTimer = currentInterval - snareOffset;
			snareNum = 0;
			PlayerControllerB localPlayerController = GameNetworkManager.Instance.localPlayerController;
			if (!localPlayerController.isPlayerDead && localPlayerController.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.7f, 70f, (int)maxDistance - 1))
			{
				localPlayerController.JumpToFearLevel(0.85f);
			}
			if (isMaster)
			{
				musicAudio2.pitch = pitch;
				musicAudio2.PlayOneShot(paradeClips[num]);
			}
		}
	}

	private void SetDanceClock()
	{
	}
}
