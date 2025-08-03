using Unity.Netcode;
using UnityEngine;

public class MineshaftElevatorController : NetworkBehaviour
{
	public Animator elevatorAnimator;

	public Transform elevatorPoint;

	public bool elevatorFinishedMoving;

	public float elevatorFinishTimer;

	public bool elevatorIsAtBottom;

	public bool elevatorCalled;

	public bool elevatorMovingDown;

	private bool movingDownLastFrame = true;

	public bool calledDown;

	public float callCooldown;

	public AudioSource elevatorAudio;

	public AudioClip elevatorStartUpSFX;

	public AudioClip elevatorStartDownSFX;

	public AudioClip elevatorTravelSFX;

	public AudioClip elevatorFinishUpSFX;

	public AudioClip elevatorFinishDownSFX;

	public GameObject elevatorCalledBottomButton;

	public GameObject elevatorCalledTopButton;

	public Transform elevatorTopPoint;

	public Transform elevatorBottomPoint;

	public Transform elevatorInsidePoint;

	public Vector3 previousElevatorPosition;

	public bool elevatorDoorOpen;

	public AudioSource elevatorJingleMusic;

	private bool playMusic;

	private bool startedMusic;

	private float stopPlayingMusicTimer;

	public AudioClip[] elevatorHalloweenClips;

	public AudioClip[] elevatorHalloweenClipsLoop;

	[ServerRpc]
	public void SetElevatorMusicServerRpc(bool setOn)
{		{
			SetElevatorMusicClientRpc(setOn);
		}
}
	[ClientRpc]
	public void SetElevatorMusicClientRpc(bool setOn)
{if(!base.IsServer)			{
				playMusic = setOn;
			}
}
	private void OnEnable()
	{
		RoundManager.Instance.currentMineshaftElevator = this;
	}

	private void OnDisable()
	{
		RoundManager.Instance.currentMineshaftElevator = null;
	}

	public void LateUpdate()
	{
		previousElevatorPosition = elevatorInsidePoint.position;
	}

	public void Update()
	{
		if (!playMusic)
		{
			if (stopPlayingMusicTimer <= 0f)
			{
				if (elevatorJingleMusic.isPlaying)
				{
					if (elevatorJingleMusic.pitch < 0.5f)
					{
						elevatorJingleMusic.volume -= Time.deltaTime * 3f;
						if (elevatorJingleMusic.volume <= 0.01f)
						{
							elevatorJingleMusic.Stop();
						}
					}
					else
					{
						elevatorJingleMusic.pitch -= Time.deltaTime;
						elevatorJingleMusic.volume = Mathf.Max(elevatorJingleMusic.volume - Time.deltaTime * 2f, 0.4f);
					}
				}
			}
			else
			{
				stopPlayingMusicTimer -= Time.deltaTime;
			}
		}
		else
		{
			stopPlayingMusicTimer = 1.5f;
			if (!elevatorJingleMusic.isPlaying)
			{
				if (elevatorMovingDown)
				{
					elevatorJingleMusic.Play();
					elevatorJingleMusic.volume = 1f;
				}
				else
				{
					elevatorJingleMusic.Play();
					elevatorJingleMusic.volume = 1f;
				}
			}
			elevatorJingleMusic.pitch = Mathf.Clamp(elevatorJingleMusic.pitch += Time.deltaTime * 2f, 0.3f, 1f);
		}
		elevatorAnimator.SetBool("ElevatorGoingUp", !elevatorMovingDown);
		elevatorCalledTopButton.SetActive(!elevatorMovingDown || elevatorCalled);
		elevatorCalledBottomButton.SetActive(elevatorMovingDown || elevatorCalled);
		if (elevatorMovingDown != movingDownLastFrame)
		{
			movingDownLastFrame = elevatorMovingDown;
			if (elevatorMovingDown)
			{
				elevatorAudio.PlayOneShot(elevatorStartDownSFX);
			}
			else
			{
				elevatorAudio.PlayOneShot(elevatorStartUpSFX);
			}
			if (base.IsServer)
			{
				SetElevatorMovingServerRpc(elevatorMovingDown);
			}
		}
		if (!base.IsServer)
		{
			return;
		}
		if (elevatorFinishedMoving)
		{
			if (base.IsServer && startedMusic)
			{
				playMusic = false;
				startedMusic = false;
				SetElevatorMusicServerRpc(setOn: false);
			}
		}
		else if (base.IsServer && !startedMusic)
		{
			startedMusic = true;
			playMusic = true;
			SetElevatorMusicServerRpc(setOn: true);
		}
		if (elevatorFinishedMoving)
		{
			if (elevatorCalled)
			{
				if (callCooldown <= 0f)
				{
					SwitchElevatorDirection();
					SetElevatorCalledClientRpc(setCalled: false, elevatorMovingDown);
				}
				else
				{
					callCooldown -= Time.deltaTime;
				}
			}
		}
		else if (elevatorFinishTimer <= 0f)
		{
			elevatorFinishedMoving = true;
			Debug.Log("Elevator finished moving!");
			PlayFinishAudio(!elevatorMovingDown);
			ElevatorFinishServerRpc(!elevatorMovingDown);
		}
		else
		{
			elevatorFinishTimer -= Time.deltaTime;
		}
	}

	private void SwitchElevatorDirection()
	{
		elevatorMovingDown = !elevatorMovingDown;
		elevatorFinishedMoving = false;
		elevatorFinishTimer = 14f;
		elevatorCalled = false;
		SetElevatorFinishedMovingClientRpc(finished: false);
	}

	[ClientRpc]
	public void SetElevatorFinishedMovingClientRpc(bool finished)
{if(!base.IsServer)			{
				elevatorFinishedMoving = finished;
			}
}
	public void AnimationEvent_ElevatorFinishTop()
	{
		if (!elevatorMovingDown && !elevatorFinishedMoving)
		{
			elevatorFinishedMoving = true;
			if (base.IsServer)
			{
				PlayFinishAudio(!elevatorMovingDown);
				ElevatorFinishServerRpc(!elevatorMovingDown);
			}
		}
	}

	public void AnimationEvent_ElevatorStartFromBottom()
	{
		ShakePlayerCamera(shakeHard: false);
	}

	public void AnimationEvent_ElevatorHitBottom()
	{
		ShakePlayerCamera(shakeHard: true);
	}

	public void AnimationEvent_ElevatorTravel()
	{
		elevatorAudio.PlayOneShot(elevatorTravelSFX);
	}

	public void AnimationEvent_ElevatorFinishBottom()
	{
		if (elevatorMovingDown && !elevatorFinishedMoving)
		{
			elevatorFinishedMoving = true;
			if (base.IsServer)
			{
				Debug.Log("Elevator finished moving B!");
				PlayFinishAudio(!elevatorMovingDown);
				ElevatorFinishServerRpc(!elevatorMovingDown);
			}
		}
	}

	private void ShakePlayerCamera(bool shakeHard)
	{
		if (Vector3.Distance(StartOfRound.Instance.audioListener.transform.position, elevatorPoint.position) < 4f)
		{
			if (shakeHard)
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
			}
			else
			{
				HUDManager.Instance.ShakeCamera(ScreenShakeType.Long);
			}
		}
	}

	[ServerRpc]
	public void ElevatorFinishServerRpc(bool atTop)
{		{
			ElevatorFinishClientRpc(atTop);
		}
}
	[ClientRpc]
	public void ElevatorFinishClientRpc(bool atTop)
{if(!base.IsServer)			{
				PlayFinishAudio(atTop);
				elevatorFinishedMoving = true;
			}
}
	private void PlayFinishAudio(bool atTop)
	{
		if (atTop)
		{
			elevatorAudio.PlayOneShot(elevatorFinishUpSFX);
		}
		else
		{
			elevatorAudio.PlayOneShot(elevatorFinishDownSFX);
		}
	}

	[ServerRpc]
	public void SetElevatorMovingServerRpc(bool movingDown)
{		{
			SetElevatorMovingClientRpc(movingDown);
		}
}
	[ClientRpc]
	public void SetElevatorMovingClientRpc(bool movingDown)
{if(!base.IsServer)			{
				elevatorMovingDown = movingDown;
			}
}
	[ServerRpc(RequireOwnership = false)]
	public void CallElevatorServerRpc(bool callDown)
			{
				CallElevatorOnServer(callDown);
			}

	public void CallElevatorOnServer(bool callDown)
	{
		if (elevatorMovingDown != callDown)
		{
			elevatorCalled = true;
			callCooldown = 4f;
			SetElevatorCalledClientRpc(elevatorCalled, elevatorMovingDown);
		}
	}

	public void SetElevatorDoorOpen()
	{
		elevatorDoorOpen = true;
	}

	public void SetElevatorDoorClosed()
	{
		elevatorDoorOpen = false;
	}

	[ClientRpc]
	public void SetElevatorCalledClientRpc(bool setCalled, bool elevatorDown)
{if(!base.IsServer)			{
				elevatorCalled = setCalled;
				elevatorMovingDown = elevatorDown;
			}
}
	public void CallElevator(bool callDown)
	{
		Debug.Log($"Call elevator 0; call down: {callDown}; elevator moving down: {elevatorMovingDown}");
		CallElevatorServerRpc(callDown);
	}

	[ServerRpc(RequireOwnership = false)]
	public void PressElevatorButtonServerRpc()
			{
				PressElevatorButtonOnServer();
			}

	public void PressElevatorButtonOnServer(bool requireFinishedMoving = false)
	{
		if (elevatorFinishedMoving || (elevatorFinishTimer < 0.16f && !requireFinishedMoving))
		{
			SwitchElevatorDirection();
		}
	}

	public void PressElevatorButton()
	{
		PressElevatorButtonServerRpc();
	}
}
