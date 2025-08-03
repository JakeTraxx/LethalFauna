using UnityEngine;

public class animatedSun : MonoBehaviour
{
	public Light indirectLight;

	public Light directLight;

	private void Start()
	{
		TimeOfDay timeOfDay = Object.FindObjectOfType<TimeOfDay>();
		if (timeOfDay != null)
		{
			timeOfDay.sunAnimator = base.gameObject.GetComponent<Animator>();
			timeOfDay.sunIndirect = indirectLight;
			timeOfDay.sunDirect = directLight;
		}
	}

	private void Update()
	{
	}
}
