using System;
using UnityEngine;

[Serializable]
public class UnlockableItem
{
	public string unlockableName;

	public GameObject prefabObject;

	public int unlockableType;

	[Space(5f)]
	public TerminalNode shopSelectionNode;

	public bool alwaysInStock;

	[Space(3f)]
	public bool IsPlaceable;

	[Space(3f)]
	public bool hasBeenMoved;

	public Vector3 placedPosition;

	public Vector3 placedRotation;

	[Space(3f)]
	public float luckValue;

	public bool inStorage;

	public bool canBeStored = true;

	public int maxNumber = 1;

	[Space(3f)]
	public bool hasBeenUnlockedByPlayer;

	[Space(3f)]
	public Material suitMaterial;

	public GameObject headCostumeObject;

	public GameObject lowerTorsoCostumeObject;

	[Space(3f)]
	public bool alreadyUnlocked;

	public bool unlockedInChallengeFile;

	public bool spawnPrefab = true;

	[Header("Misc.")]
	public AudioClip jumpAudio;
}
