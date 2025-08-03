using UnityEngine;

public class AnimationStopPoints : MonoBehaviour
{
	public bool canAnimationStop;

	public int animationPosition = 1;

	public void SetAnimationStopPosition1()
	{
		canAnimationStop = true;
		animationPosition = 1;
	}

	public void SetAnimationGo()
	{
		canAnimationStop = false;
	}

	public void SetAnimationStopPosition2()
	{
		canAnimationStop = true;
		animationPosition = 2;
	}
}
