using Unity.Netcode;
using UnityEngine;

public class GlobalEffects : NetworkBehaviour
{
	private StartOfRound playersManager;

	public bool ownedByPlayer;

	public static GlobalEffects Instance { get; private set; }

	private void Awake()
	{
		if (!ownedByPlayer)
		{
			if (!(Instance == null))
			{
				Object.Destroy(base.gameObject);
				return;
			}
			Instance = this;
		}
		playersManager = Object.FindObjectOfType<StartOfRound>();
	}

	public void PlayAnimAndAudioServer(ServerAnimAndAudio serverAnimAndAudio)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAnimAndAudioServerFromSenderObject(serverAnimAndAudio);
	}

	public void PlayAnimAndAudioServerFromSenderObject(ServerAnimAndAudio serverAnimAndAudio)
	{
		PlayAnimAndAudioServerRpc(serverAnimAndAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAnimAndAudioServerRpc(ServerAnimAndAudio serverAnimAndAudio)
			{
				PlayAnimAndAudioClientRpc(serverAnimAndAudio);
			}

	[ClientRpc]
	private void PlayAnimAndAudioClientRpc(ServerAnimAndAudio serverAnimAndAudio)
{		{
			if (serverAnimAndAudio.animatorObj.TryGet(out var networkObject))
			{
				networkObject.GetComponent<Animator>().SetTrigger(serverAnimAndAudio.animationString);
			}
			if (serverAnimAndAudio.audioObj.TryGet(out var networkObject2))
			{
				networkObject2.GetComponent<AudioSource>().PlayOneShot(networkObject2.GetComponent<AudioSource>().clip);
			}
		}
}
	public void PlayAnimationServer(ServerAnimation serverAnimation)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAnimationServerFromSenderObject(serverAnimation);
	}

	public void PlayAnimationServerFromSenderObject(ServerAnimation serverAnimation)
	{
		PlayAnimationServerRpc(serverAnimation);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAnimationServerRpc(ServerAnimation serverAnimation)
			{
				PlayAnimationClientRpc(serverAnimation);
			}

	[ClientRpc]
	private void PlayAnimationClientRpc(ServerAnimation serverAnimation)
{if(base.IsOwner)		{
			return;
		}
		if (serverAnimation.animatorObj.TryGet(out var networkObject))
		{
			if (serverAnimation.isTrigger)
			{
				networkObject.GetComponent<Animator>().SetTrigger(serverAnimation.animationString);
			}
			else
			{
				networkObject.GetComponent<Animator>().SetBool(serverAnimation.animationString, serverAnimation.setTrue);
			}
		}
		else
		{
			Debug.LogWarning("Was not able to retrieve NetworkObject from NetworkObjectReference; string " + serverAnimation.animationString);
		}
}
	public void PlayAudioServer(ServerAudio serverAudio)
	{
		playersManager.allPlayerObjects[playersManager.thisClientPlayerId].GetComponentInChildren<GlobalEffects>().PlayAudioServerFromSenderObject(serverAudio);
	}

	public void PlayAudioServerFromSenderObject(ServerAudio serverAudio)
	{
		PlayAudioServerRpc(serverAudio);
	}

	[ServerRpc(RequireOwnership = false)]
	private void PlayAudioServerRpc(ServerAudio serverAudio)
			{
				PlayAudioClientRpc(serverAudio);
			}

	[ClientRpc]
	private void PlayAudioClientRpc(ServerAudio serverAudio)
{if(base.IsOwner)		{
			return;
		}
		if (serverAudio.audioObj.TryGet(out var networkObject))
		{
			AudioSource component = networkObject.gameObject.GetComponent<AudioSource>();
			if (serverAudio.oneshot)
			{
				component.PlayOneShot(component.clip, 1f);
				return;
			}
			component.loop = serverAudio.looped;
			component.Play();
		}
		else
		{
			Debug.LogWarning("Was not able to retrieve NetworkObject from NetworkObjectReference; audio");
		}
}}
