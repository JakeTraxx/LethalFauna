using GameNetcodeStuff;
using UnityEngine;

public class QuicksandTrigger : MonoBehaviour
{
	public bool isWater;

	public bool isInsideWater;

	public int audioClipIndex;

	[Space(5f)]
	public bool sinkingLocalPlayer;

	public float movementHinderance = 1.6f;

	public float sinkingSpeedMultiplier = 0.15f;

	private void OnTriggerStay(Collider other)
	{
		if (isWater)
		{
			if (!other.gameObject.CompareTag("Player"))
			{
				return;
			}
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != GameNetworkManager.Instance.localPlayerController && component != null && component.underwaterCollider != this)
			{
				component.underwaterCollider = base.gameObject.GetComponent<Collider>();
				return;
			}
		}
		if (!isWater && !other.gameObject.CompareTag("Player"))
		{
			return;
		}
		PlayerControllerB component2 = other.gameObject.GetComponent<PlayerControllerB>();
		if (component2 != GameNetworkManager.Instance.localPlayerController)
		{
			return;
		}
		if ((isWater && component2.isInsideFactory != isInsideWater) || component2.isInElevator)
		{
			if (sinkingLocalPlayer)
			{
				StopSinkingLocalPlayer(component2);
			}
			return;
		}
		if (isWater && !component2.isUnderwater)
		{
			component2.underwaterCollider = base.gameObject.GetComponent<Collider>();
			component2.isUnderwater = true;
		}
		component2.statusEffectAudioIndex = audioClipIndex;
		if (component2.isSinking)
		{
			return;
		}
		if (sinkingLocalPlayer)
		{
			if (!component2.CheckConditionsForSinkingInQuicksand())
			{
				StopSinkingLocalPlayer(component2);
			}
		}
		else if (component2.CheckConditionsForSinkingInQuicksand())
		{
			Debug.Log("Set local player to sinking!");
			sinkingLocalPlayer = true;
			component2.sourcesCausingSinking++;
			component2.isMovementHindered++;
			component2.hinderedMultiplier *= movementHinderance;
			if (isWater)
			{
				component2.sinkingSpeedMultiplier = 0f;
			}
			else
			{
				component2.sinkingSpeedMultiplier = sinkingSpeedMultiplier;
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		OnExit(other);
	}

	public void OnExit(Collider other)
	{
		if (!sinkingLocalPlayer)
		{
			if (isWater)
			{
				if (!other.CompareTag("Player") || other.gameObject.GetComponent<PlayerControllerB>() == GameNetworkManager.Instance.localPlayerController)
				{
					return;
				}
				other.gameObject.GetComponent<PlayerControllerB>().isUnderwater = false;
			}
			Debug.Log("Quicksand is not sinking local player!");
			return;
		}
		Debug.Log("Quicksand is sinking local player!");
		if (other.CompareTag("Player"))
		{
			Debug.Log("Quicksand is sinking local player! B");
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (!(component != GameNetworkManager.Instance.localPlayerController))
			{
				Debug.Log("Quicksand is sinking local player! C");
				StopSinkingLocalPlayer(component);
			}
		}
	}

	public void StopSinkingLocalPlayer(PlayerControllerB playerScript)
	{
		if (sinkingLocalPlayer)
		{
			sinkingLocalPlayer = false;
			playerScript.sourcesCausingSinking = Mathf.Clamp(playerScript.sourcesCausingSinking - 1, 0, 100);
			playerScript.isMovementHindered = Mathf.Clamp(playerScript.isMovementHindered - 1, 0, 100);
			playerScript.hinderedMultiplier = Mathf.Clamp(playerScript.hinderedMultiplier / movementHinderance, 1f, 100f);
			if (playerScript.isMovementHindered == 0 && isWater)
			{
				playerScript.isUnderwater = false;
			}
		}
	}
}
