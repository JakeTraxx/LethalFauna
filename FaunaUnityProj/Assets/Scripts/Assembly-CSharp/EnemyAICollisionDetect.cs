using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class EnemyAICollisionDetect : MonoBehaviour, IHittable, INoiseListener, IShockableWithGun
{
	public EnemyAI mainScript;

	public bool canCollideWithEnemies;

	public bool onlyCollideWhenGrounded;

	private void OnTriggerStay(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			if (onlyCollideWhenGrounded)
			{
				CharacterController component = other.gameObject.GetComponent<CharacterController>();
				if (!(component != null) || !component.isGrounded)
				{
					return;
				}
				mainScript.OnCollideWithPlayer(other);
			}
			mainScript.OnCollideWithPlayer(other);
		}
		else if (!onlyCollideWhenGrounded && canCollideWithEnemies && other.CompareTag("Enemy"))
		{
			EnemyAICollisionDetect component2 = other.gameObject.GetComponent<EnemyAICollisionDetect>();
			if (component2 != null && component2.mainScript != mainScript)
			{
				mainScript.OnCollideWithEnemy(other, component2.mainScript);
			}
		}
	}

	bool IHittable.Hit(int force, Vector3 hitDirection, PlayerControllerB playerWhoHit, bool playHitSFX, int hitID)
	{
		if (onlyCollideWhenGrounded)
		{
			Debug.Log("Enemy collision detect returned false");
			return false;
		}
		mainScript.HitEnemyOnLocalClient(force, hitDirection, playerWhoHit, playHitSFX, hitID);
		return true;
	}

	void INoiseListener.DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot, int noiseID)
	{
		if (!onlyCollideWhenGrounded)
		{
			mainScript.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
		}
	}

	bool IShockableWithGun.CanBeShocked()
	{
		if (!onlyCollideWhenGrounded && mainScript.postStunInvincibilityTimer <= 0f && mainScript.enemyType.canBeStunned)
		{
			return !mainScript.isEnemyDead;
		}
		return false;
	}

	Vector3 IShockableWithGun.GetShockablePosition()
	{
		if (mainScript.eye != null)
		{
			return mainScript.eye.position;
		}
		return base.transform.position + Vector3.up * 0.5f;
	}

	float IShockableWithGun.GetDifficultyMultiplier()
	{
		return mainScript.enemyType.stunGameDifficultyMultiplier;
	}

	void IShockableWithGun.ShockWithGun(PlayerControllerB shockedByPlayer)
	{
		mainScript.SetEnemyStunned(setToStunned: true, 0.25f, shockedByPlayer);
		mainScript.stunnedIndefinitely++;
	}

	Transform IShockableWithGun.GetShockableTransform()
	{
		return base.transform;
	}

	NetworkObject IShockableWithGun.GetNetworkObject()
	{
		return mainScript.NetworkObject;
	}

	void IShockableWithGun.StopShockingWithGun()
	{
		mainScript.stunnedIndefinitely = Mathf.Clamp(mainScript.stunnedIndefinitely - 1, 0, 100);
	}
}
