using Unity.Netcode;
using UnityEngine;

public class SyncWithServerOnSpawn : NetworkBehaviour
{
	public RoundManager roundManager;

	private bool hasSynced;

	private void Start()
	{
	}

	public void SyncWithServer()
	{
		if (!base.IsServer)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		NetworkObject component = base.gameObject.GetComponent<NetworkObject>();
		if (component != null)
		{
			component.Spawn(destroyWithScene: true);
		}
	}
}
