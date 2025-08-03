using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class DocileLocustBeesAI : EnemyAI
{
	private int previousBehaviour;

	public AISearchRoutine bugsRoam;

	public VisualEffect bugsEffect;

	private float timeSinceReturning;

	public ScanNodeProperties scanNode;

	public override void DoAIInterval()
	{
		base.DoAIInterval();
		if (StartOfRound.Instance.allPlayersDead || daytimeEnemyLeaving)
		{
			return;
		}
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (!bugsRoam.inProgress)
			{
				StartSearch(base.transform.position, bugsRoam);
			}
			if (Physics.CheckSphere(base.transform.position, 8f, 520, QueryTriggerInteraction.Collide))
			{
				SwitchToBehaviourState(1);
			}
			break;
		case 1:
			if (!Physics.CheckSphere(base.transform.position, 14f, 520, QueryTriggerInteraction.Collide))
			{
				SwitchToBehaviourState(0);
			}
			break;
		}
	}

	public override void Update()
	{
		base.Update();
		bugsEffect.SetBool("Alive", Vector3.Distance(StartOfRound.Instance.activeCamera.transform.position, base.transform.position) < 62f);
		switch (currentBehaviourStateIndex)
		{
		case 0:
			if (previousBehaviour != 0)
			{
				previousBehaviour = 0;
				bugsEffect.SetFloat("MoveToTargetForce", 6f);
				creatureVoice.Play();
			}
			scanNode.maxRange = 18;
			timeSinceReturning += Time.deltaTime;
			creatureVoice.volume = Mathf.Min(creatureVoice.volume + Time.deltaTime * 0.25f, 1f);
			break;
		case 1:
			if (previousBehaviour != 1)
			{
				previousBehaviour = 1;
				bugsEffect.SetFloat("MoveToTargetForce", -35f);
				if (timeSinceReturning > 2f)
				{
					creatureSFX.PlayOneShot(enemyType.audioClips[0]);
					WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[0], 0.8f);
					RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.35f, 0, noiseIsInsideClosedShip: false, 14152);
				}
				timeSinceReturning = 0f;
			}
			scanNode.maxRange = 1;
			if (creatureVoice.volume > 0f)
			{
				creatureVoice.volume = Mathf.Max(creatureVoice.volume - Time.deltaTime * 1.75f, 0f);
			}
			else
			{
				creatureVoice.Stop();
			}
			break;
		}
	}

	public override void DaytimeEnemyLeave()
	{
		base.DaytimeEnemyLeave();
		bugsEffect.SetFloat("MoveToTargetForce", -15f);
		creatureSFX.PlayOneShot(enemyType.audioClips[0], 0.5f);
		WalkieTalkie.TransmitOneShotAudio(creatureSFX, enemyType.audioClips[0], 0.4f);
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 6f, 0.2f, 0, noiseIsInsideClosedShip: false, 14152);
		StartCoroutine(bugsLeave());
	}

	private IEnumerator bugsLeave()
	{
		yield return new WaitForSeconds(6f);
		KillEnemyOnOwnerClient(overrideDestroy: true);
	}
}
