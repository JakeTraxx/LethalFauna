using UnityEngine;

public class AlarmButton : MonoBehaviour
{
	private Animator buttonAnimator;

	public float timeSincePushing;

	public void PushAlarmButton()
	{
		if (!(timeSincePushing < 1f))
		{
			buttonAnimator.SetTrigger("press");
			HUDManager.Instance.TriggerAlarmHornEffect();
		}
	}

	private void Update()
	{
		if (timeSincePushing <= 5f)
		{
			timeSincePushing += Time.deltaTime;
		}
	}
}
