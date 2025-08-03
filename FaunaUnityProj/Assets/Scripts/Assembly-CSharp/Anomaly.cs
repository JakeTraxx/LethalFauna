using Unity.Netcode;
using UnityEngine;

public abstract class Anomaly : NetworkBehaviour
{
	public AnomalyType anomalyType;

	public float initialInstability = 10f;

	public float difficultyMultiplier;

	public float normalizedHealth;

	public NetworkObject thisNetworkObject;

	public float maxHealth;

	[HideInInspector]
	public float health;

	[HideInInspector]
	public float removingHealth;

	[HideInInspector]
	public float usedInstability;

	public RoundManager roundManager;

	[Header("Misc properties")]
	public bool raycastToSurface;

	private bool addingInstability;

	public virtual void Start()
	{
		roundManager = Object.FindObjectOfType<RoundManager>(includeInactive: false);
		thisNetworkObject = base.gameObject.GetComponent<NetworkObject>();
		addingInstability = true;
		_ = roundManager.hasInitializedLevelRandomSeed;
	}

	public virtual void AnomalyDespawn(bool removedByPatcher = false)
	{
		if (!base.IsServer)
		{
			DespawnAnomalyServerRpc();
			return;
		}
		addingInstability = false;
		base.gameObject.GetComponent<NetworkObject>().Despawn();
		roundManager.SpawnedAnomalies.Remove(this);
		roundManager.SpawnedAnomalies.TrimExcess();
	}

	[ServerRpc(RequireOwnership = false)]
	public void DespawnAnomalyServerRpc()
{if(base.gameObject.GetComponent<NetworkObject>().IsSpawned)			{
				AnomalyDespawn();
			}
}
	public virtual void Update()
	{
		if (removingHealth > 0f)
		{
			health -= removingHealth * Time.deltaTime;
			if (base.IsServer && health <= 0f)
			{
				AnomalyDespawn(removedByPatcher: true);
			}
		}
		else
		{
			health = Mathf.Clamp(health += Time.deltaTime * 1.5f, anomalyType.anomalyMaxHealth / 3f, anomalyType.anomalyMaxHealth);
			if (base.IsServer && addingInstability)
			{
				if (usedInstability <= initialInstability)
				{
					usedInstability += Time.deltaTime;
				}
				else
				{
					usedInstability += Time.deltaTime / 3f;
				}
			}
		}
		normalizedHealth = Mathf.Abs(anomalyType.anomalyMaxHealth / health - 1f);
	}
}
