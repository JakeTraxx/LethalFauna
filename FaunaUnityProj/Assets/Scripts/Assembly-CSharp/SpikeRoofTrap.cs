using System;
using System.Collections;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class SpikeRoofTrap : NetworkBehaviour
{
	public bool slammingDown;

	public float timeSinceMovingUp;

	public bool trapActive = true;

	public Animator spikeTrapAnimator;

	private Coroutine slamCoroutine;

	private List<DeadBodyInfo> deadBodiesSlammed;

	private List<GameObject> slammedBodyStickingPoints;

	public GameObject deadBodyStickingPointPrefab;

	public Transform stickingPointsContainer;

	public Transform laserEye;

	private RaycastHit hit;

	private bool slamOnIntervals;

	private float slamInterval = 1f;

	private Light laserLight;

	public AudioSource spikeTrapAudio;

	private EntranceTeleport nearEntrance;

	public void ToggleSpikesEnabled(bool enabled)
	{
		if (trapActive != enabled)
		{
			Debug.Log($"Toggling turret to {enabled}!");
			ToggleSpikesEnabledLocalClient(enabled);
			ToggleSpikesServerRpc(enabled);
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void ToggleSpikesServerRpc(bool enabled)
			{
				ToggleSpikesClientRpc(enabled);
			}

	[ClientRpc]
	public void ToggleSpikesClientRpc(bool enabled)
{if(trapActive != enabled)			{
				ToggleSpikesEnabledLocalClient(enabled);
			}
}
	private void ToggleSpikesEnabledLocalClient(bool enabled)
	{
		trapActive = enabled;
	}

	private bool GetNearEntrance()
	{
		bool flag = false;
		EntranceTeleport[] array = UnityEngine.Object.FindObjectsByType<EntranceTeleport>(FindObjectsSortMode.None);
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].isEntranceToBuilding && Vector3.Distance(spikeTrapAudio.transform.position, array[i].entrancePoint.position) < 7f)
			{
				flag = true;
				nearEntrance = array[i];
			}
		}
		if (flag)
		{
			for (int j = 0; j < array.Length; j++)
			{
				if (array[j].entrancePoint == nearEntrance.exitPoint)
				{
					nearEntrance = array[j];
					return true;
				}
			}
		}
		return false;
	}

	public void Start()
	{
		System.Random random = new System.Random(StartOfRound.Instance.randomMapSeed + 30 + (int)base.transform.position.x + (int)base.transform.position.z);
		bool flag = GetNearEntrance();
		if (random.Next(0, 10) < 8 || flag)
		{
			slamOnIntervals = true;
			int num = random.Next(0, 100);
			if (num < 10)
			{
				slamInterval = 1f * ((float)random.Next(1, 35) / 1.3f);
			}
			else if (num > 90)
			{
				slamInterval = 1f * ((float)random.Next(1, 4) / 1.4f);
			}
			else
			{
				slamInterval = 1f * ((float)random.Next(1, 15) / 1.3f);
			}
			if ((bool)nearEntrance)
			{
				slamInterval = Mathf.Max(slamInterval, 1.25f);
			}
		}
	}

	public void OnTriggerStay(Collider other)
	{
		if (!trapActive || !slammingDown || Time.realtimeSinceStartup - timeSinceMovingUp < 0.75f)
		{
			return;
		}
		PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
		if (component != null && component == GameNetworkManager.Instance.localPlayerController && !component.isPlayerDead)
		{
			GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.down * 17f, spawnBody: true, CauseOfDeath.Crushing);
			return;
		}
		DeadBodyInfo component2 = other.gameObject.GetComponent<DeadBodyInfo>();
		if (component2 != null && (deadBodiesSlammed == null || !deadBodiesSlammed.Contains(component2)))
		{
			StickBodyToSpikes(component2);
			return;
		}
		EnemyAICollisionDetect component3 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
		if (component3 != null && component3.mainScript != null && component3.mainScript.IsOwner && component3.mainScript.enemyType.canDie && !component3.mainScript.isEnemyDead)
		{
			component3.mainScript.KillEnemyOnOwnerClient();
		}
	}

	private void StickBodyToSpikes(DeadBodyInfo body)
	{
		if (deadBodiesSlammed == null)
		{
			deadBodiesSlammed = new List<DeadBodyInfo>();
		}
		if (slammedBodyStickingPoints == null)
		{
			slammedBodyStickingPoints = new List<GameObject>();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(position: new Vector3(body.bodyParts[5].transform.position.x, stickingPointsContainer.position.y, body.bodyParts[5].transform.position.z), original: deadBodyStickingPointPrefab, rotation: body.bodyParts[5].rotation * Quaternion.Euler(65f, 0f, 0f), parent: stickingPointsContainer);
		deadBodiesSlammed.Add(body);
		body.attachedLimb = body.bodyParts[5];
		body.attachedTo = gameObject.transform;
		body.matchPositionExactly = true;
		body.canBeGrabbedBackByPlayers = true;
	}

	public void Update()
	{
		if (!trapActive || slammingDown)
		{
			return;
		}
		if (nearEntrance != null && Time.realtimeSinceStartup - nearEntrance.timeAtLastUse < 1.2f)
		{
			timeSinceMovingUp = Time.realtimeSinceStartup;
		}
		if (slamOnIntervals)
		{
			if (base.IsServer && Time.realtimeSinceStartup - timeSinceMovingUp > slamInterval && (!Physics.CheckSphere(laserEye.position, 8f, 524288, QueryTriggerInteraction.Collide) || Physics.CheckSphere(laserEye.position, 14f, 8, QueryTriggerInteraction.Collide)))
			{
				slammingDown = true;
				SpikeTrapSlam();
				SpikeTrapSlamServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
		else if (!(Time.realtimeSinceStartup - timeSinceMovingUp < 1.2f) && Physics.Raycast(laserEye.position, laserEye.forward, out hit, 4.4f, StartOfRound.Instance.collidersRoomMaskDefaultAndPlayers, QueryTriggerInteraction.Ignore))
		{
			PlayerControllerB component = hit.collider.GetComponent<PlayerControllerB>();
			if (component != null && !component.isPlayerDead && component == GameNetworkManager.Instance.localPlayerController)
			{
				slammingDown = true;
				SpikeTrapSlam();
				SpikeTrapSlamServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
			}
		}
	}

	public void SpikeTrapSlam()
	{
		slammingDown = true;
		if (slamCoroutine != null)
		{
			StopCoroutine(slamCoroutine);
		}
		slamCoroutine = StartCoroutine(SlamSpikeTrapSequence());
	}

	private IEnumerator SlamSpikeTrapSequence()
	{
		spikeTrapAnimator.SetBool("Slamming", value: true);
		SetRandomSpikeTrapAudioPitch();
		yield return new WaitForSeconds(0.8f);
		spikeTrapAnimator.SetBool("Slamming", value: false);
		timeSinceMovingUp = Time.realtimeSinceStartup;
		slammingDown = false;
	}

	private void SetRandomSpikeTrapAudioPitch()
	{
		spikeTrapAudio.pitch = 1f;
		switch (UnityEngine.Random.Range(1, 7))
		{
		case 1:
			spikeTrapAudio.pitch *= Mathf.Pow(1.05946f, 2f);
			break;
		case 2:
			spikeTrapAudio.pitch *= Mathf.Pow(1.05946f, 4f);
			break;
		case 3:
			spikeTrapAudio.pitch /= Mathf.Pow(1.05946f, 1f);
			break;
		case 4:
			spikeTrapAudio.pitch /= Mathf.Pow(1.05946f, 3f);
			break;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void SpikeTrapSlamServerRpc(int playerWhoTriggered)
{		{
			SpikeTrapSlamClientRpc(playerWhoTriggered);
			if ((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerWhoTriggered)
			{
				SpikeTrapSlam();
			}
		}
}
	[ClientRpc]
	public void SpikeTrapSlamClientRpc(int playerWhoTriggered)
{if((int)GameNetworkManager.Instance.localPlayerController.playerClientId != playerWhoTriggered && !base.IsServer)			{
				SpikeTrapSlam();
			}
}}
