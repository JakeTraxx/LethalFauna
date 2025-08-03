using Unity.Netcode;
using UnityEngine;

public class KiwiBabyItem : GrabbableObject
{
	public Animator babyAnimator;

	public bool screaming;

	public int currentAnimation;

	private float timeHeld;

	public AudioSource eggAudio;

	public AudioClip peepAudio;

	public AudioClip screamAudio;

	public AudioClip breakEggSFX;

	private float screamingOnFloorTimer;

	private bool staring;

	private float screamTimer;

	private float stopScreamTimer;

	private bool countingScreamTimer;

	private bool takenIntoOrbit;

	public GiantKiwiAI mamaAI;

	public float timeLastAbandoned;

	public Vector3 positionWhenLastAbandoned;

	public bool hasScreamed;

	private float makeNoiseInterval;

	public AudioClip scream3SFX;

	[ServerRpc]
	public void SetScreamingServerRpc(bool scream)
{		{
			SetScreamingClientRpc(scream);
		}
}
	[ClientRpc]
	public void SetScreamingClientRpc(bool scream)
{if(screaming != scream)		{
			screaming = scream;
			if (scream)
			{
				countingScreamTimer = false;
				stopScreamTimer = 3f;
				currentAnimation = 3;
				hasScreamed = true;
			}
			else
			{
				currentAnimation = 1;
			}
		}
}
	[ServerRpc]
	public void BreakOutServerRpc()
{		{
			BreakOutClientRpc();
		}
}
	[ClientRpc]
	public void BreakOutClientRpc()
{if(currentAnimation == 0)			{
				currentAnimation = 1;
				eggAudio.PlayOneShot(breakEggSFX);
			}
}
	public override void Start()
	{
		base.Start();
		screamTimer = 13f;
		mamaAI = Object.FindObjectOfType<GiantKiwiAI>();
		if (StartOfRound.Instance.inShipPhase)
		{
			takenIntoOrbit = true;
			currentAnimation = 4;
		}
	}

	public override void ReactToSellingItemOnCounter()
	{
		base.ReactToSellingItemOnCounter();
		currentAnimation = 5;
		eggAudio.pitch = Random.Range(0.93f, 1.15f);
		eggAudio.PlayOneShot(scream3SFX);
		babyAnimator.SetInteger("babyAnimation", 5);
	}

	public override void Update()
	{
		base.Update();
		if (currentAnimation == 5)
		{
			return;
		}
		if (StartOfRound.Instance.inShipPhase || takenIntoOrbit)
		{
			currentAnimation = 4;
			if (screaming)
			{
				screaming = false;
				takenIntoOrbit = true;
			}
			if (currentAnimation == 4)
			{
				eggAudio.Stop();
			}
		}
		else if (!eggAudio.isPlaying)
		{
			if (screaming)
			{
				eggAudio.clip = screamAudio;
				eggAudio.Play();
			}
			else if (currentAnimation == 1)
			{
				eggAudio.clip = peepAudio;
				eggAudio.Play();
			}
		}
		else if (currentAnimation == 4)
		{
			eggAudio.Stop();
		}
		babyAnimator.SetInteger("babyAnimation", currentAnimation);
		if (mamaAI == null)
		{
			return;
		}
		if (isHeld || isHeldByEnemy)
		{
			float num = Vector3.Distance(base.transform.position, mamaAI.birdNest.transform.position);
			Debug.Log($"Baby bird distance to nest : {num}", base.gameObject);
			if (num > 5f)
			{
				if (!countingScreamTimer)
				{
					countingScreamTimer = true;
				}
			}
			else if (num < 4.8f && countingScreamTimer)
			{
				countingScreamTimer = false;
				screamTimer = 6f;
				screaming = false;
			}
		}
		if (countingScreamTimer)
		{
			Debug.Log($"Counting scream timer; timer: {screamTimer}");
			if (screamTimer > 0f)
			{
				screamTimer -= Time.deltaTime;
				if (countingScreamTimer)
				{
					switch (currentAnimation)
					{
					case 0:
						if (screamTimer < 7f && base.IsOwner)
						{
							eggAudio.PlayOneShot(breakEggSFX);
							currentAnimation = 1;
							BreakOutServerRpc();
						}
						break;
					case 1:
						makeNoiseInterval -= Time.deltaTime;
						if (makeNoiseInterval < 0f)
						{
							makeNoiseInterval = 0.5f;
							RoundManager.Instance.PlayAudibleNoise(base.transform.position, 10f, 0.6f, 0, StartOfRound.Instance.hangarDoorsClosed && isInShipRoom);
						}
						if (screamTimer < 3f)
						{
							currentAnimation = 2;
						}
						break;
					case 2:
						if (eggAudio.isPlaying)
						{
							eggAudio.Stop();
						}
						break;
					case 4:
						if (eggAudio.isPlaying)
						{
							eggAudio.Stop();
						}
						break;
					}
				}
			}
			else if (!screaming)
			{
				Debug.Log($"Start screaming; {screamTimer}");
				countingScreamTimer = false;
				screaming = true;
				hasScreamed = true;
				currentAnimation = 3;
				stopScreamTimer = 3f;
				if (base.IsOwner)
				{
					SetScreamingServerRpc(scream: true);
				}
			}
		}
		if (screaming && !isHeld)
		{
			stopScreamTimer -= Time.deltaTime;
			if (stopScreamTimer < 0f && base.IsOwner)
			{
				screaming = false;
				currentAnimation = 4;
				SetScreamingServerRpc(scream: false);
			}
		}
	}
}
