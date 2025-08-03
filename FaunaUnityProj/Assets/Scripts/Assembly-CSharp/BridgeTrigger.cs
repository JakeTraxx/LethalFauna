using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class BridgeTrigger : NetworkBehaviour
{
	public float bridgeDurability = 1f;

	private PlayerControllerB playerOnBridge;

	private List<PlayerControllerB> playersOnBridge = new List<PlayerControllerB>();

	public AudioSource bridgeAudioSource;

	public AudioClip[] bridgeCreakSFX;

	public AudioClip bridgeFallSFX;

	public Animator bridgeAnimator;

	private bool hasBridgeFallen;

	public Transform bridgePhysicsPartsContainer;

	private bool giantOnBridge;

	private bool giantOnBridgeLastFrame;

	public Collider[] fallenBridgeColliders;

	public int fallType;

	public float weightCapacityAmount = 0.04f;

	public float playerCapacityAmount = 0.02f;

	private void OnEnable()
	{
		StartOfRound.Instance.playerTeleportedEvent.AddListener(RemovePlayerFromBridge);
	}

	private void OnDisable()
	{
		StartOfRound.Instance.playerTeleportedEvent.RemoveListener(RemovePlayerFromBridge);
	}

	private void Update()
	{
		if (hasBridgeFallen)
		{
			return;
		}
		if (giantOnBridge)
		{
			bridgeDurability -= Time.deltaTime / 4.25f;
		}
		if (playersOnBridge.Count > 0)
		{
			bridgeDurability = Mathf.Clamp(bridgeDurability - Time.deltaTime * (playerCapacityAmount * (float)(playersOnBridge.Count * playersOnBridge.Count)), 0f, 1f);
			for (int i = 0; i < playersOnBridge.Count; i++)
			{
				if (playersOnBridge[i].carryWeight > 1.1f)
				{
					bridgeDurability -= Time.deltaTime * (weightCapacityAmount * playersOnBridge[i].carryWeight);
				}
			}
		}
		else if (bridgeDurability < 1f && !giantOnBridge)
		{
			bridgeDurability = Mathf.Clamp(bridgeDurability + Time.deltaTime * 0.2f, 0f, 1f);
		}
		if (base.IsServer && bridgeDurability <= 0f && !hasBridgeFallen)
		{
			hasBridgeFallen = true;
			BridgeFallServerRpc();
			Debug.Log("Bridge collapsed! On server");
		}
		bridgeAnimator.SetFloat("durability", Mathf.Clamp(Mathf.Abs(bridgeDurability - 1f), 0f, 1f));
	}

	private void LateUpdate()
	{
		if (giantOnBridge)
		{
			if (giantOnBridgeLastFrame)
			{
				giantOnBridge = false;
				giantOnBridgeLastFrame = false;
			}
			else
			{
				giantOnBridgeLastFrame = true;
			}
		}
	}

	[ServerRpc]
	public void BridgeFallServerRpc()
{		{
			BridgeFallClientRpc();
		}
}
	[ClientRpc]
	public void BridgeFallClientRpc()
{		{
			hasBridgeFallen = true;
			switch (fallType)
			{
			case 0:
				bridgeAnimator.SetTrigger("Fall");
				break;
			case 2:
				bridgeAnimator.SetTrigger("FallType2");
				break;
			}
			EnableFallenBridgeColliders();
			bridgeAudioSource.PlayOneShot(bridgeFallSFX);
			float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, bridgeAudioSource.transform.position);
			if (num < 30f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
			}
			else if (num < 50f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			}
		}
}
	private void EnableFallenBridgeColliders()
	{
		for (int i = 0; i < fallenBridgeColliders.Length; i++)
		{
			fallenBridgeColliders[i].enabled = true;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			playerOnBridge = other.gameObject.GetComponent<PlayerControllerB>();
			if (playerOnBridge != null && !playersOnBridge.Contains(playerOnBridge))
			{
				playersOnBridge.Add(playerOnBridge);
				if (Random.Range(playersOnBridge.Count * 25, 100) > 60)
				{
					RoundManager.PlayRandomClip(bridgeAudioSource, bridgeCreakSFX);
				}
			}
		}
		else if (other.gameObject.CompareTag("Enemy"))
		{
			EnemyAICollisionDetect component = other.gameObject.GetComponent<EnemyAICollisionDetect>();
			if (component != null && component.mainScript.enemyType.enemyName == "ForestGiant")
			{
				giantOnBridge = true;
				giantOnBridgeLastFrame = false;
			}
		}
	}

	public void RemovePlayerFromBridge(PlayerControllerB playerOnBridge)
	{
		if (playerOnBridge != null && playersOnBridge.Contains(playerOnBridge))
		{
			playersOnBridge.Remove(playerOnBridge);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.gameObject.CompareTag("Player"))
		{
			playerOnBridge = other.gameObject.GetComponent<PlayerControllerB>();
			RemovePlayerFromBridge(playerOnBridge);
		}
	}
}
