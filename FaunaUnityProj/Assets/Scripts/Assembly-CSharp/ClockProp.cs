using UnityEngine;

public class ClockProp : GrabbableObject
{
	public Transform hourHand;

	public Transform minuteHand;

	public Transform secondHand;

	private float timeOfLastSecond;

	private int secondsPassed;

	private int minutesPassed;

	public AudioSource tickAudio;

	public AudioClip tickSFX;

	public AudioClip tockSFX;

	private bool tickOrTock;

	public override void Update()
	{
		base.Update();
		if (Time.realtimeSinceStartup - timeOfLastSecond > 1f)
		{
			secondHand.Rotate(-6f, 0f, 0f, Space.Self);
			secondsPassed++;
			if (secondsPassed >= 60)
			{
				secondsPassed = 0;
				minutesPassed++;
				minuteHand.Rotate(-6f, 0f, 0f, Space.Self);
			}
			if (minutesPassed > 60)
			{
				minutesPassed = 0;
				hourHand.Rotate(-30f, 0f, 0f, Space.Self);
			}
			timeOfLastSecond = Time.realtimeSinceStartup;
			tickOrTock = !tickOrTock;
			if (tickOrTock)
			{
				tickAudio.PlayOneShot(tickSFX);
			}
			else
			{
				tickAudio.PlayOneShot(tockSFX);
			}
		}
	}
}
