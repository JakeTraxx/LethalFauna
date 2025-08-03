using System.Collections;
using UnityEngine;

public class MapDevice : GrabbableObject
{
	public Camera mapCamera;

	public Animator mapAnimatorTransition;

	public Light mapLight;

	private Coroutine pingMapCoroutine;

	public override void Start()
	{
		base.Start();
		mapCamera = GameObject.FindGameObjectWithTag("MapCamera").GetComponent<Camera>();
		mapAnimatorTransition = mapCamera.gameObject.GetComponentInChildren<Animator>();
		mapLight = mapCamera.gameObject.GetComponentInChildren<Light>();
	}

	public override void ItemActivate(bool used, bool buttonDown = true)
	{
		if (pingMapCoroutine != null)
		{
			StopCoroutine(pingMapCoroutine);
		}
		pingMapCoroutine = StartCoroutine(pingMapSystem());
		base.ItemActivate(used);
	}

	private IEnumerator pingMapSystem()
	{
		mapCamera.enabled = true;
		mapAnimatorTransition.SetTrigger("Transition");
		yield return new WaitForSeconds(0.035f);
		if (playerHeldBy.isInsideFactory)
		{
			mapCamera.transform.position = new Vector3(playerHeldBy.transform.position.x + 8.6f, -20f, playerHeldBy.transform.position.z - 3f);
		}
		else
		{
			mapCamera.transform.position = new Vector3(playerHeldBy.transform.position.x + 8.6f, 50f, playerHeldBy.transform.position.z - 3f);
		}
		yield return new WaitForSeconds(0.2f);
		mapLight.enabled = true;
		mapCamera.Render();
		mapLight.enabled = false;
		mapCamera.enabled = false;
	}

	public override void DiscardItem()
	{
		isBeingUsed = false;
		base.DiscardItem();
	}

	public override void EquipItem()
	{
		base.EquipItem();
		playerHeldBy.equippedUsableItemQE = true;
	}
}
