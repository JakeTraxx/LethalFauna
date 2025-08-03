using UnityEngine;

public class WhoopieCushionItem : GrabbableObject
{
	public AudioSource whoopieCushionAudio;

	public AudioClip[] fartAudios;

	private float fartDebounce;

	private Vector3 lastPositionAtFart;

	private int timesPlayingInOneSpot;

	public void Fart()
	{
		if (Vector3.Distance(lastPositionAtFart, base.transform.position) > 2f)
		{
			timesPlayingInOneSpot = 0;
		}
		timesPlayingInOneSpot++;
		lastPositionAtFart = base.transform.position;
		RoundManager.PlayRandomClip(whoopieCushionAudio, fartAudios, randomize: true, 1f, -1);
		RoundManager.Instance.PlayAudibleNoise(base.transform.position, 8f, 0.8f, timesPlayingInOneSpot, isInShipRoom && StartOfRound.Instance.hangarDoorsClosed, 101158);
	}

	public override void ActivatePhysicsTrigger(Collider other)
	{
		if (Time.realtimeSinceStartup - fartDebounce > 0.2f)
		{
			fartDebounce = Time.realtimeSinceStartup;
			Fart();
		}
	}
}
