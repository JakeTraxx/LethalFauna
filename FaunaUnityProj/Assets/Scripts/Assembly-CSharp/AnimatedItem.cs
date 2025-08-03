using System;
using UnityEngine;

public class AnimatedItem : GrabbableObject
{
	public string grabItemBoolString;

	public string dropItemTriggerString;

	public bool makeAnimationWhenDropping;

	public Animator itemAnimator;

	public AudioSource itemAudio;

	public AudioClip grabAudio;

	public AudioClip dropAudio;

	public bool loopGrabAudio;

	public bool loopDropAudio;

	[Range(0f, 100f)]
	public int chanceToTriggerAnimation = 100;

	public int chanceToTriggerAlternateMesh;

	public Mesh alternateMesh;

	private Mesh normalMesh;

	private System.Random itemRandomChance;

	public float noiseRange;

	public float noiseLoudness;

	private int timesPlayedInOneSpot;

	private float makeNoiseInterval;

	private Vector3 lastPosition;

	public AudioLowPassFilter itemAudioLowPassFilter;

	private bool wasInPocket;

	public override void Start()
	{
		base.Start();
		itemRandomChance = new System.Random(StartOfRound.Instance.randomMapSeed + StartOfRound.Instance.currentLevelID + itemProperties.itemId);
		if (chanceToTriggerAlternateMesh > 0)
		{
			normalMesh = base.gameObject.GetComponent<MeshFilter>().mesh;
		}
	}

	public override void EquipItem()
	{
		base.EquipItem();
		if (itemAudioLowPassFilter != null)
		{
			itemAudioLowPassFilter.cutoffFrequency = 20000f;
		}
		itemAudio.volume = 1f;
		if (chanceToTriggerAlternateMesh > 0)
		{
			if (itemRandomChance.Next(0, 100) < chanceToTriggerAlternateMesh)
			{
				base.gameObject.GetComponent<MeshFilter>().mesh = alternateMesh;
				itemAudio.Stop();
				return;
			}
			base.gameObject.GetComponent<MeshFilter>().mesh = normalMesh;
		}
		if (!wasInPocket)
		{
			if (itemRandomChance.Next(0, 100) > chanceToTriggerAnimation)
			{
				itemAudio.Stop();
				return;
			}
		}
		else
		{
			wasInPocket = false;
		}
		if (itemAnimator != null)
		{
			itemAnimator.SetBool(grabItemBoolString, value: true);
		}
		if (itemAudio != null)
		{
			itemAudio.clip = grabAudio;
			itemAudio.loop = loopGrabAudio;
			itemAudio.Play();
		}
	}

	public override void DiscardItem()
	{
		base.DiscardItem();
		if (itemAnimator != null)
		{
			itemAnimator.SetBool(grabItemBoolString, value: false);
		}
		if (chanceToTriggerAlternateMesh > 0)
		{
			base.gameObject.GetComponent<MeshFilter>().mesh = normalMesh;
		}
		if (!makeAnimationWhenDropping)
		{
			itemAudio.Stop();
			return;
		}
		if (itemRandomChance.Next(0, 100) < chanceToTriggerAnimation)
		{
			itemAudio.Stop();
			return;
		}
		if (itemAnimator != null)
		{
			itemAnimator.SetTrigger(dropItemTriggerString);
		}
		if (itemAudio != null)
		{
			itemAudio.loop = loopDropAudio;
			itemAudio.clip = dropAudio;
			itemAudio.Play();
			if (itemAudioLowPassFilter != null)
			{
				itemAudioLowPassFilter.cutoffFrequency = 20000f;
			}
			itemAudio.volume = 1f;
		}
	}

	public override void PocketItem()
	{
		base.PocketItem();
		wasInPocket = true;
		if (itemAudio != null)
		{
			if (itemAudioLowPassFilter != null)
			{
				itemAudioLowPassFilter.cutoffFrequency = 1700f;
			}
			itemAudio.volume = 0.5f;
		}
	}

	public override void Update()
	{
		base.Update();
		if (itemAudio == null || !itemAudio.isPlaying)
		{
			return;
		}
		if (makeNoiseInterval <= 0f)
		{
			makeNoiseInterval = 0.75f;
			if (Vector3.Distance(lastPosition, base.transform.position) < 4f)
			{
				timesPlayedInOneSpot++;
			}
			else
			{
				timesPlayedInOneSpot = 0;
			}
			if (isPocketed)
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange / 2f, noiseLoudness / 2f, timesPlayedInOneSpot, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
			}
			else
			{
				RoundManager.Instance.PlayAudibleNoise(base.transform.position, noiseRange, noiseLoudness, timesPlayedInOneSpot, isInElevator && StartOfRound.Instance.hangarDoorsClosed);
			}
		}
		else
		{
			makeNoiseInterval -= Time.deltaTime;
		}
	}
}
