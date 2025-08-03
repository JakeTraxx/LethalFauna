using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EntranceTeleport : NetworkBehaviour
{
	public bool isEntranceToBuilding;

	public Transform entrancePoint;

	public Transform exitPoint;

	public int entranceId;

	public StartOfRound playersManager;

	public int audioReverbPreset = -1;

	public AudioSource entrancePointAudio;

	private AudioSource exitPointAudio;

	public AudioClip[] doorAudios;

	private InteractTrigger triggerScript;

	private float checkForEnemiesInterval;

	private bool enemyNearLastCheck;

	private bool gotExitPoint;

	private bool checkedForFirstTime;

	public float timeAtLastUse;

	private void Awake()
	{
		playersManager = Object.FindObjectOfType<StartOfRound>();
		triggerScript = base.gameObject.GetComponent<InteractTrigger>();
		checkForEnemiesInterval = 10f;
	}

	public bool FindExitPoint()
	{
		EntranceTeleport[] array = Object.FindObjectsOfType<EntranceTeleport>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].isEntranceToBuilding != isEntranceToBuilding && array[i].entranceId == entranceId)
			{
				if (array[i].entrancePointAudio != null)
				{
					exitPointAudio = array[i].entrancePointAudio;
				}
				exitPoint = array[i].entrancePoint;
			}
		}
		if (exitPoint == null)
		{
			return false;
		}
		return true;
	}

	public void TeleportPlayer()
	{
		bool flag = false;
		if (!FindExitPoint())
		{
			flag = true;
		}
		if (flag)
		{
			HUDManager.Instance.DisplayTip("???", "The entrance appears to be blocked.");
			return;
		}
		Transform thisPlayerBody = GameNetworkManager.Instance.localPlayerController.thisPlayerBody;
		GameNetworkManager.Instance.localPlayerController.TeleportPlayer(exitPoint.position);
		GameNetworkManager.Instance.localPlayerController.isInElevator = false;
		GameNetworkManager.Instance.localPlayerController.isInHangarShipRoom = false;
		thisPlayerBody.eulerAngles = new Vector3(thisPlayerBody.eulerAngles.x, exitPoint.eulerAngles.y, thisPlayerBody.eulerAngles.z);
		SetAudioPreset((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		if (!checkedForFirstTime)
		{
			checkedForFirstTime = true;
			if (RoundManager.Instance.currentDungeonType != -1 && isEntranceToBuilding && (RoundManager.Instance.currentDungeonType == 0 || RoundManager.Instance.currentDungeonType == 1 || RoundManager.Instance.currentDungeonType == 4) && !ES3.Load($"PlayedDungeonEntrance{RoundManager.Instance.currentDungeonType}", "LCGeneralSaveData", defaultValue: false))
			{
				StartCoroutine(playMusicOnDelay());
			}
		}
		for (int i = 0; i < GameNetworkManager.Instance.localPlayerController.ItemSlots.Length; i++)
		{
			if (GameNetworkManager.Instance.localPlayerController.ItemSlots[i] != null)
			{
				GameNetworkManager.Instance.localPlayerController.ItemSlots[i].isInFactory = isEntranceToBuilding;
			}
		}
		timeAtLastUse = Time.realtimeSinceStartup;
		TeleportPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
		GameNetworkManager.Instance.localPlayerController.isInsideFactory = isEntranceToBuilding;
	}

	private IEnumerator playMusicOnDelay()
	{
		yield return new WaitForSeconds(0.6f);
		ES3.Save($"PlayedDungeonEntrance{RoundManager.Instance.currentDungeonType}", value: true, "LCGeneralSaveData");
		HUDManager.Instance.UIAudio.PlayOneShot(RoundManager.Instance.dungeonFlowTypes[RoundManager.Instance.currentDungeonType].firstTimeAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	public void TeleportPlayerServerRpc(int playerObj)
			{
				TeleportPlayerClientRpc(playerObj);
			}

	[ClientRpc]
	public void TeleportPlayerClientRpc(int playerObj)
{if(playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController)		{
			return;
		}
		FindExitPoint();
		playersManager.allPlayerScripts[playerObj].TeleportPlayer(exitPoint.position, withRotation: true, exitPoint.eulerAngles.y);
		playersManager.allPlayerScripts[playerObj].isInElevator = false;
		playersManager.allPlayerScripts[playerObj].isInHangarShipRoom = false;
		PlayAudioAtTeleportPositions();
		playersManager.allPlayerScripts[playerObj].isInsideFactory = isEntranceToBuilding;
		for (int i = 0; i < playersManager.allPlayerScripts[playerObj].ItemSlots.Length; i++)
		{
			if (playersManager.allPlayerScripts[playerObj].ItemSlots[i] != null)
			{
				playersManager.allPlayerScripts[playerObj].ItemSlots[i].isInFactory = isEntranceToBuilding;
			}
		}
		if (GameNetworkManager.Instance.localPlayerController.isPlayerDead && playersManager.allPlayerScripts[playerObj] == GameNetworkManager.Instance.localPlayerController.spectatedPlayerScript)
		{
			SetAudioPreset(playerObj);
		}
		timeAtLastUse = Time.realtimeSinceStartup;
}
	private void SetAudioPreset(int playerObj)
	{
		if (audioReverbPreset != -1)
		{
			Object.FindObjectOfType<AudioReverbPresets>().audioPresets[audioReverbPreset].ChangeAudioReverbForPlayer(StartOfRound.Instance.allPlayerScripts[playerObj]);
			if (entrancePointAudio != null)
			{
				PlayAudioAtTeleportPositions();
			}
		}
	}

	public void PlayAudioAtTeleportPositions()
	{
		if (doorAudios.Length != 0)
		{
			entrancePointAudio.PlayOneShot(doorAudios[Random.Range(0, doorAudios.Length)]);
			exitPointAudio.PlayOneShot(doorAudios[Random.Range(0, doorAudios.Length)]);
		}
	}

	private void Update()
	{
		if (triggerScript == null || !isEntranceToBuilding)
		{
			return;
		}
		if (checkForEnemiesInterval <= 0f)
		{
			if (!gotExitPoint)
			{
				if (FindExitPoint())
				{
					gotExitPoint = true;
				}
				return;
			}
			checkForEnemiesInterval = 1f;
			bool flag = false;
			for (int i = 0; i < RoundManager.Instance.SpawnedEnemies.Count; i++)
			{
				if (Vector3.Distance(RoundManager.Instance.SpawnedEnemies[i].transform.position, exitPoint.transform.position) < 7.7f && !RoundManager.Instance.SpawnedEnemies[i].isEnemyDead)
				{
					flag = true;
					break;
				}
			}
			if (flag && !enemyNearLastCheck)
			{
				enemyNearLastCheck = true;
				triggerScript.hoverTip = "[Near activity detected!]";
			}
			else if (enemyNearLastCheck)
			{
				enemyNearLastCheck = false;
				triggerScript.hoverTip = "Enter: [LMB]";
			}
		}
		else
		{
			checkForEnemiesInterval -= Time.deltaTime;
		}
	}
}
