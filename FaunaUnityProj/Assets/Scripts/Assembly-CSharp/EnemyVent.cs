using Unity.Netcode;
using UnityEngine;

public class EnemyVent : NetworkBehaviour
{
	public float spawnTime;

	public bool occupied;

	[Space(5f)]
	public EnemyType enemyType;

	public int enemyTypeIndex;

	[Space(10f)]
	public AudioSource ventAudio;

	public AudioLowPassFilter lowPassFilter;

	public AudioClip ventCrawlSFX;

	public Transform floorNode;

	private bool isPlayingAudio;

	private RoundManager roundManager;

	public Animator ventAnimator;

	public bool ventIsOpen;

	public bool caveVent;

	private void Start()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
	}

	private void BeginVentSFX()
	{
		if (enemyType.overrideVentSFX != null)
		{
			ventAudio.clip = enemyType.overrideVentSFX;
			ventAudio.Play();
			ventAudio.volume = 0f;
		}
		else if (!caveVent)
		{
			ventAudio.clip = ventCrawlSFX;
			ventAudio.Play();
			ventAudio.volume = 0f;
		}
	}

	[ClientRpc]
	public void OpenVentClientRpc()
{		{
			if (!ventIsOpen)
			{
				ventIsOpen = true;
				ventAnimator.SetTrigger("openVent");
				lowPassFilter.lowpassResonanceQ = 0f;
			}
			occupied = false;
		}
}
	[ClientRpc]
	public void SyncVentSpawnTimeClientRpc(int time, int enemyIndex)
			{
				enemyTypeIndex = enemyIndex;
				enemyType = roundManager.currentLevel.Enemies[enemyIndex].enemyType;
				spawnTime = time;
				occupied = true;
			}

	private void Update()
	{
		if (occupied)
		{
			if (!isPlayingAudio)
			{
				if (spawnTime - roundManager.timeScript.currentDayTime < enemyType.timeToPlayAudio)
				{
					isPlayingAudio = true;
					BeginVentSFX();
				}
			}
			else
			{
				ventAudio.volume = Mathf.Abs((spawnTime - roundManager.timeScript.currentDayTime) / enemyType.timeToPlayAudio - 1f);
				lowPassFilter.lowpassResonanceQ = Mathf.Abs(ventAudio.volume * 2f - 2f);
			}
		}
		else if (isPlayingAudio)
		{
			isPlayingAudio = false;
			ventAudio.Stop();
		}
	}
}
