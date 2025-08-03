using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

public class CompanyMonsterCollisionDetect : MonoBehaviour
{
	public int monsterAnimationID;

	private void OnTriggerEnter(Collider other)
	{
		if (!(NetworkManager.Singleton == null) && other.CompareTag("Player"))
		{
			PlayerControllerB component = other.gameObject.GetComponent<PlayerControllerB>();
			if (component != null && !component.isPlayerDead && component.IsOwner)
			{
				Object.FindObjectOfType<DepositItemsDesk>().CollisionDetect(monsterAnimationID);
			}
		}
	}
}
