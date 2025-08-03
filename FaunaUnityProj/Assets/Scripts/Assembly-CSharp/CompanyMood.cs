using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/CompanyMoodPreset", order = 2)]
public class CompanyMood : ScriptableObject
{
	public float timeToWaitBeforeGrabbingItem = 10f;

	public float irritability = 1f;

	public float judgementSpeed = 1f;

	public float startingPatience = 3f;

	public bool desiresSilence;

	public bool mustBeWokenUp;

	public int maximumItemsToAnger = -1;

	public float sensitivity = 1f;

	[Space(3f)]
	public AudioClip noiseBehindWallSFX;

	[Space(5f)]
	public AudioClip[] grabItemsSFX;

	public AudioClip[] angerSFX;

	public AudioClip[] attackSFX;

	public AudioClip wallAttackSFX;

	public AudioClip insideWindowSFX;

	public AudioClip behindWallSFX;

	public bool stopWallSFXWhenOpening;

	[Space(5f)]
	public CompanyMonster manifestation;

	public int maxPlayersToKillBeforeSatisfied = 1;

	public int[] enableMonsterAnimationIndex;

	public float grabPlayerAnimationTime = 2f;
}
