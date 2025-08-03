using System.Collections;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class ToggleFogTrigger : MonoBehaviour
{
	public LocalVolumetricFog fog1;

	public float fog1EnabledAmount;

	public LocalVolumetricFog fog2;

	public float fog2EnabledAmount;

	private Coroutine fadeOutFogCoroutine;

	private bool fadingInFog;

	private void Update()
	{
		if (fadingInFog)
		{
			fog1.parameters.meanFreePath = Mathf.Lerp(fog1.parameters.meanFreePath, fog1EnabledAmount, 5f * Time.deltaTime);
			fog2.parameters.meanFreePath = Mathf.Lerp(fog2.parameters.meanFreePath, 27f, 5f * Time.deltaTime);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (fadingInFog || !other.CompareTag("Player"))
		{
			return;
		}
		PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
		if (component != null && component == GameNetworkManager.Instance.localPlayerController)
		{
			fadingInFog = true;
			if (fadeOutFogCoroutine != null)
			{
				StopCoroutine(fadeOutFogCoroutine);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (fadingInFog && other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && component == GameNetworkManager.Instance.localPlayerController)
			{
				fadingInFog = false;
				fadeOutFogCoroutine = StartCoroutine(fadeOutFog());
			}
		}
	}

	private IEnumerator fadeOutFog()
	{
		yield return null;
		float fog1StartingValue = fog1.parameters.meanFreePath;
		float fog2StartingValue = fog2.parameters.meanFreePath;
		for (int i = 0; i < 50; i++)
		{
			fog1.parameters.meanFreePath = Mathf.Lerp(fog1StartingValue, 27f, (float)i / 65f);
			fog2.parameters.meanFreePath = Mathf.Clamp(Mathf.Lerp(fog2StartingValue, fog2EnabledAmount, (float)i / 12f), fog2EnabledAmount, 27f);
			yield return new WaitForSeconds(0.01f);
		}
		fog1.parameters.meanFreePath = 27f;
		fog2.parameters.meanFreePath = fog2EnabledAmount;
	}
}
