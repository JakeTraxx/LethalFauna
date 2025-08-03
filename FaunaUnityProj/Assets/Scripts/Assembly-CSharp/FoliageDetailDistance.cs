using System.Collections.Generic;
using UnityEngine;

public class FoliageDetailDistance : MonoBehaviour
{
	public List<MeshRenderer> allBushRenderers = new List<MeshRenderer>();

	private float updateInterval;

	private int bushIndex;

	private Coroutine updateBushesLODCoroutine;

	public Material highDetailMaterial;

	public Material lowDetailMaterial;

	public Transform localPlayerTransform;

	private void Start()
	{
		GameObject[] array = GameObject.FindGameObjectsWithTag("Bush");
		for (int i = 0; i < array.Length; i++)
		{
			MeshRenderer component = array[i].GetComponent<MeshRenderer>();
			if ((bool)component)
			{
				allBushRenderers.Add(component);
			}
		}
		localPlayerTransform = Object.FindObjectOfType<StartOfRound>().localPlayerController.transform;
	}

	private void Update()
	{
		if (localPlayerTransform == null)
		{
			return;
		}
		if (updateInterval >= 0f)
		{
			updateInterval -= Time.deltaTime;
		}
		else if (bushIndex < allBushRenderers.Count)
		{
			if (allBushRenderers[bushIndex] == null)
			{
				return;
			}
			if (Vector3.Distance(localPlayerTransform.position, allBushRenderers[bushIndex].transform.position) > 75f)
			{
				if (allBushRenderers[bushIndex].material != lowDetailMaterial)
				{
					allBushRenderers[bushIndex].material = lowDetailMaterial;
				}
			}
			else if (allBushRenderers[bushIndex].material != highDetailMaterial)
			{
				allBushRenderers[bushIndex].material = highDetailMaterial;
			}
			bushIndex++;
		}
		else
		{
			bushIndex = 0;
			updateInterval = 1f;
		}
	}
}
