using UnityEngine;

public class RadMechAnimationEvents : MonoBehaviour
{
	public RadMechAI mainScript;

	public void FlickerFace()
	{
		mainScript.FlickerFace();
	}

	public void EnableSpotlight()
	{
		mainScript.EnableSpotlight();
	}

	public void DisableSpotlight()
	{
		mainScript.DisableSpotlight();
	}

	public void StompLeftFoot()
	{
		mainScript.StompLeftFoot();
	}

	public void StompRightFoot()
	{
		mainScript.StompRightFoot();
	}

	public void StompBothFeet()
	{
		mainScript.StompBothFeet();
	}

	public void HasEnteredSky()
	{
		mainScript.HasEnteredSky();
	}

	public void DisableSmoke()
	{
		mainScript.DisableThrusterSmoke();
	}

	public void EnableSmoke()
	{
		mainScript.EnableThrusterSmoke();
	}

	public void EnableBlowtorch()
	{
		mainScript.EnableBlowtorch();
	}

	public void DisableBlowtorch()
	{
		mainScript.DisableBlowtorch();
	}

	public void FinishFlying()
	{
		mainScript.FinishFlyingAnimation();
	}
}
