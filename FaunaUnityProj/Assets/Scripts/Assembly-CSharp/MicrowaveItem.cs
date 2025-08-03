using System.Collections;
using UnityEngine;

public class MicrowaveItem : MonoBehaviour
{
	public GameObject mainObject;

	private Coroutine microwaveOnDelay;

	public AudioSource whirringAudio;

	public AudioClip microwaveOpen;

	public AudioClip microwaveClose;

	public void TurnOnMicrowave(bool on)
	{
		if (!on)
		{
			whirringAudio.PlayOneShot(microwaveClose);
			GrabbableObject[] componentsInChildren = mainObject.GetComponentsInChildren<GrabbableObject>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].rotateObject = true;
			}
			if (microwaveOnDelay != null)
			{
				StopCoroutine(microwaveOnDelay);
			}
			microwaveOnDelay = StartCoroutine(startMicrowaveOnDelay());
		}
		else
		{
			if (microwaveOnDelay != null)
			{
				StopCoroutine(microwaveOnDelay);
			}
			Collider[] array = Physics.OverlapSphere(mainObject.transform.position, 5f, 64, QueryTriggerInteraction.Collide);
			for (int j = 0; j < array.Length; j++)
			{
				array[j].GetComponent<GrabbableObject>().rotateObject = false;
			}
			whirringAudio.Stop();
			whirringAudio.PlayOneShot(microwaveOpen);
		}
	}

	private IEnumerator startMicrowaveOnDelay()
	{
		yield return new WaitForSeconds(0.25f);
		RoundManager.Instance.PlayAudibleNoise(mainObject.transform.position, 8f, 0.6f, 0, StartOfRound.Instance.hangarDoorsClosed);
		yield return new WaitForSeconds(0.5f);
		whirringAudio.Play();
	}
}
