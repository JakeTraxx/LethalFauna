using System.Collections;
using UnityEngine;

public class AnimatedObjectFloatSetter : MonoBehaviour
{
	public AnimatedObjectTrigger animatedObjectTrigger;

	public string animatorFloatName;

	private float animatorFloatValue = 1f;

	public float valueChangeSpeed;

	public GameObject[] conditionalObjects;

	private int currentFifth = -1;

	private bool boolWasTrue;

	private bool completed;

	public AudioSource thisAudio;

	public AudioClip trueAudio;

	public AudioClip falseAudio;

	public AudioClip completionTrueAudio;

	public AudioClip completionFalseAudio;

	public Transform killPlayerPoint;

	public bool ignoreVerticalDistance = true;

	public float killRange;

	private bool deactivated = true;

	private void KillPlayerAtPoint()
	{
		if (!(killPlayerPoint == null) && !GameNetworkManager.Instance.localPlayerController.isPlayerDead)
		{
			Vector3 position = killPlayerPoint.position;
			if (ignoreVerticalDistance)
			{
				position.y = GameNetworkManager.Instance.localPlayerController.transform.position.y;
			}
			if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, position) < killRange)
			{
				GameNetworkManager.Instance.localPlayerController.KillPlayer(Vector3.zero, spawnBody: true, CauseOfDeath.Crushing);
			}
		}
	}

	private void Start()
	{
		StartCoroutine(waitForNavMeshBake());
	}

	private IEnumerator waitForNavMeshBake()
	{
		yield return new WaitUntil(() => RoundManager.Instance.bakedNavMesh);
		yield return new WaitForSeconds(2f);
		deactivated = false;
	}

	private void Update()
	{
		if (deactivated || !animatedObjectTrigger.isBool)
		{
			return;
		}
		if (animatedObjectTrigger.boolValue)
		{
			if (!boolWasTrue)
			{
				boolWasTrue = true;
				thisAudio.clip = trueAudio;
				thisAudio.Play();
			}
			animatorFloatValue = Mathf.Min(animatorFloatValue + valueChangeSpeed * Time.deltaTime, 1f);
			if (animatorFloatValue == 1f && !completed)
			{
				completed = true;
				thisAudio.Stop();
				thisAudio.PlayOneShot(completionTrueAudio);
			}
		}
		else
		{
			if (boolWasTrue)
			{
				boolWasTrue = false;
				thisAudio.clip = falseAudio;
				thisAudio.Play();
			}
			animatorFloatValue = Mathf.Max(animatorFloatValue - valueChangeSpeed * Time.deltaTime, 0f);
			if (animatorFloatValue == 0f && completed)
			{
				completed = false;
				thisAudio.Stop();
				thisAudio.PlayOneShot(completionFalseAudio);
				KillPlayerAtPoint();
			}
		}
		animatedObjectTrigger.triggerAnimator.SetFloat(animatorFloatName, animatorFloatValue);
		if (conditionalObjects.Length >= 5 && RoundManager.Instance.bakedNavMesh)
		{
			SetObjectBasedOnAnimatorFloat();
		}
	}

	public void SetObjectBasedOnAnimatorFloat()
	{
		int num = -1;
		num = ((!(animatorFloatValue < 0.125f)) ? ((animatorFloatValue < 0.375f) ? 1 : ((animatorFloatValue < 0.625f) ? 2 : ((!(animatorFloatValue < 0.875f)) ? 4 : 3))) : 0);
		if (num != currentFifth)
		{
			conditionalObjects[num].SetActive(value: true);
			if (currentFifth != -1)
			{
				conditionalObjects[currentFifth].SetActive(value: false);
			}
			currentFifth = num;
		}
	}
}
