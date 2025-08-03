using GameNetcodeStuff;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsKnockbackOnHit : MonoBehaviour, IHittable
{
	public AudioClip playSFX;

	public bool Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
	{
		if (base.gameObject.GetComponent<Rigidbody>() == null)
		{
			return false;
		}
		base.gameObject.GetComponent<Rigidbody>().AddForce(hitDirection * force * 10f, ForceMode.Impulse);
		if (playSFX != null && (bool)base.gameObject.GetComponent<AudioSource>())
		{
			base.gameObject.GetComponent<AudioSource>().PlayOneShot(playSFX);
		}
		return true;
	}
}
