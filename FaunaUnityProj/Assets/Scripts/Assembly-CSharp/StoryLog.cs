using Unity.Netcode;
using UnityEngine;

public class StoryLog : NetworkBehaviour
{
	public int storyLogID = -1;

	private bool collected;

	public void CollectLog()
	{
		if (!(NetworkManager.Singleton == null) && !(GameNetworkManager.Instance == null) && !collected && storyLogID != -1)
		{
			collected = true;
			RemoveLogCollectible();
			if (!Object.FindObjectOfType<Terminal>().unlockedStoryLogs.Contains(storyLogID))
			{
				HUDManager.Instance.GetNewStoryLogServerRpc(storyLogID);
			}
		}
	}

	private void Start()
	{
		if (Object.FindObjectOfType<Terminal>().unlockedStoryLogs.Contains(storyLogID))
		{
			RemoveLogCollectible();
		}
	}

	private void RemoveLogCollectible()
	{
		MeshRenderer[] componentsInChildren = base.gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = false;
		}
		base.gameObject.GetComponent<InteractTrigger>().interactable = false;
		Collider[] componentsInChildren2 = GetComponentsInChildren<Collider>();
		for (int j = 0; j < componentsInChildren2.Length; j++)
		{
			componentsInChildren2[j].enabled = false;
		}
	}

	public void SetStoryLogID(int logID)
	{
		storyLogID = logID;
	}
}
