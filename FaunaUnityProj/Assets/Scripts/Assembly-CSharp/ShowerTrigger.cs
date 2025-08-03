using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;

public class ShowerTrigger : MonoBehaviour
{
	private float cleanInterval = 10f;

	private bool showerOn;

	private int cleanDecalIndex;

	private List<PlayerControllerB> playersInShower = new List<PlayerControllerB>();

	private int playerIndex;

	private bool everyOtherFrame;

	public Collider showerCollider;

	public void ToggleShower(bool on)
	{
		showerOn = on;
	}

	private void AddPlayerToShower(PlayerControllerB playerScript)
	{
		if (!playersInShower.Contains(playerScript))
		{
			Debug.Log($"Added player #{playerScript.playerClientId} to shower");
			playersInShower.Add(playerScript);
		}
	}

	private void RemovePlayerFromShower(PlayerControllerB playerScript)
	{
		if (playersInShower.Contains(playerScript))
		{
			playersInShower.Remove(playerScript);
		}
	}

	private void CheckBoundsForPlayers()
	{
		if (Time.realtimeSinceStartup - cleanInterval < 1.5f)
		{
			return;
		}
		cleanInterval = Time.realtimeSinceStartup;
		for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
		{
			if (playersInShower.Contains(StartOfRound.Instance.allPlayerScripts[i]))
			{
				if (!StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled || !StartOfRound.Instance.allPlayerScripts[i].isInElevator)
				{
					RemovePlayerFromShower(StartOfRound.Instance.allPlayerScripts[i]);
				}
			}
			else if (showerCollider.bounds.Contains(StartOfRound.Instance.allPlayerScripts[i].gameplayCamera.transform.position))
			{
				AddPlayerToShower(StartOfRound.Instance.allPlayerScripts[i]);
			}
		}
	}

	private void Update()
	{
		if (!showerOn)
		{
			return;
		}
		CheckBoundsForPlayers();
		if (playersInShower.Count <= 0 || SprayPaintItem.sprayPaintDecals.Count == 0)
		{
			return;
		}
		Debug.Log("Shower is running with players inside!");
		for (int i = 0; i < 10; i++)
		{
			for (int j = 0; j < playersInShower.Count; j++)
			{
				if (!playersInShower[j].isInElevator)
				{
					playersInShower.RemoveAt(j);
				}
				else if (SprayPaintItem.sprayPaintDecals != null && cleanDecalIndex < SprayPaintItem.sprayPaintDecals.Count && SprayPaintItem.sprayPaintDecals[cleanDecalIndex] != null)
				{
					if (SprayPaintItem.sprayPaintDecals[cleanDecalIndex].transform.IsChildOf(playersInShower[j].transform))
					{
						Debug.Log($"spray decal #{cleanDecalIndex} found as child of {playersInShower[j].transform}");
						SprayPaintItem.sprayPaintDecals[cleanDecalIndex].SetActive(value: false);
						break;
					}
				}
				else
				{
					cleanDecalIndex = 0;
				}
			}
			cleanDecalIndex = (cleanDecalIndex + 1) % SprayPaintItem.sprayPaintDecals.Count;
		}
	}
}
