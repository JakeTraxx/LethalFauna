using System;
using UnityEngine;

[Serializable]
public class BabyPlayerMemory
{
	public ulong playerId;

	public float likeMeter = 0.5f;

	public float timeSpentSoothing;

	public int orderSeen = -1;

	public float timeAtLastNoticing;

	public float timeAtLastSighting;

	public bool isPlayerDead;

	public BabyPlayerMemory(ulong player)
	{
		playerId = player;
		likeMeter = UnityEngine.Random.Range(0f, 0.7f);
	}
}
