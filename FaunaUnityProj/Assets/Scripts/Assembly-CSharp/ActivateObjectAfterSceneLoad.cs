using System.Collections;
using UnityEngine;

public class ActivateObjectAfterSceneLoad : MonoBehaviour
{
	private GameObject activateObject;

	private void Start()
	{
	}

	private IEnumerator waitForNavMeshBake()
	{
		yield return new WaitUntil(() => RoundManager.Instance.bakedNavMesh);
		activateObject.SetActive(value: true);
	}

	public void SetInitialState()
	{
	}
}
