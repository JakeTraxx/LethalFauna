using System.Collections;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class MoveToExitSpecialAnimation : NetworkBehaviour
{
	public InteractTrigger interactTrigger;

	public InteractTrigger strapsInteractTrigger;

	public AnimatedObjectTrigger animatedObjectTrigger;

	private float timeSinceAnimationStarted;

	private bool listeningForInput;

	public bool exitingDisabled;

	[Header("Electric chair variables")]
	public bool electricChair;

	private Coroutine shockChairCoroutine;

	public ParticleSystem shockChairParticle;

	public AudioSource shockChairSound;

	private void OnEnable()
	{
		if (electricChair)
		{
			StartOfRound.Instance.ShipPowerSurgedEvent.AddListener(OnShipPowerSurge);
		}
	}

	private void OnDisable()
	{
		if (electricChair)
		{
			StartOfRound.Instance.ShipPowerSurgedEvent.RemoveListener(OnShipPowerSurge);
		}
	}

	private void OnShipPowerSurge()
	{
		if (shockChairCoroutine == null)
		{
			shockChairCoroutine = StartCoroutine(shockChair());
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void PowerSurgeChairServerRpc()
			{
				PowerSurgeChairClientRpc();
			}

	[ClientRpc]
	public void PowerSurgeChairClientRpc()
			{
				OnShipPowerSurge();
			}

	private IEnumerator shockChair()
	{
		shockChairParticle.Play(withChildren: true);
		shockChairSound.Play();
		strapsInteractTrigger.interactable = false;
		if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.gameObject.transform.position) < 8f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
		}
		else if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.gameObject.transform.position) < 14f)
		{
			HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
		}
		for (int i = 0; i < 11; i++)
		{
			if (interactTrigger.lockedPlayer != null)
			{
				interactTrigger.lockedPlayer.GetComponent<PlayerControllerB>().DamagePlayer(10, hasDamageSFX: true, callRPC: true, CauseOfDeath.Electrocution, 3);
			}
			yield return new WaitForSeconds(0.3f);
			if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, base.gameObject.transform.position) < 14f)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
			}
		}
		yield return new WaitForSeconds(0.7f);
		shockChairParticle.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
		shockChairSound.Stop();
		strapsInteractTrigger.interactable = true;
		yield return new WaitForSeconds(0.5f);
		shockChairCoroutine = null;
	}

	public void SetExitingDisabled(bool disabled)
	{
		exitingDisabled = disabled;
		interactTrigger.interactable = !disabled;
	}

	public void Update()
	{
		if (interactTrigger.isPlayingSpecialAnimation && interactTrigger.lockedPlayer != null && interactTrigger.lockedPlayer == GameNetworkManager.Instance.localPlayerController.transform)
		{
			if (!listeningForInput)
			{
				timeSinceAnimationStarted += Time.deltaTime;
				if (timeSinceAnimationStarted > 1f)
				{
					timeSinceAnimationStarted = 0f;
					StartOfRound.Instance.PlayerMoveInputEvent.AddListener(OnMoveInputDetected);
					listeningForInput = true;
				}
			}
		}
		else if (listeningForInput)
		{
			listeningForInput = false;
			StartOfRound.Instance.PlayerMoveInputEvent.RemoveListener(OnMoveInputDetected);
			if (animatedObjectTrigger.boolValue)
			{
				animatedObjectTrigger.TriggerAnimation(GameNetworkManager.Instance.localPlayerController);
			}
			timeSinceAnimationStarted = 0f;
		}
	}

	public void OnMoveInputDetected(PlayerControllerB playerWhoMoved)
	{
		Debug.Log("Sofa chair detecting movement");
		if (!exitingDisabled && !(GameNetworkManager.Instance.localPlayerController != playerWhoMoved) && interactTrigger.isPlayingSpecialAnimation && interactTrigger.lockedPlayer != null && interactTrigger.lockedPlayer == GameNetworkManager.Instance.localPlayerController.transform)
		{
			interactTrigger.CancelAnimationExternally();
			listeningForInput = false;
			StartOfRound.Instance.PlayerMoveInputEvent.RemoveListener(OnMoveInputDetected);
		}
	}
}
