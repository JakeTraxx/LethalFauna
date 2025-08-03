using GameNetcodeStuff;
using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{
	public PlayerControllerB thisPlayerController;

	public void PlayFootstepServer()
	{
		thisPlayerController.PlayFootstepServer();
	}

	public void PlayFootstepLocal()
	{
		thisPlayerController.PlayFootstepLocal();
	}

	public void LimpForward()
	{
		thisPlayerController.LimpAnimationSpeed();
	}

	public void LockArmsToCamera()
	{
		thisPlayerController.localArmsMatchCamera = true;
	}

	public void UnlockArmsFromCamera()
	{
		thisPlayerController.localArmsMatchCamera = false;
	}
}
