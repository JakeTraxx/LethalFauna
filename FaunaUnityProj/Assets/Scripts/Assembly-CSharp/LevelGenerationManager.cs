using DunGen;
using UnityEngine;

public class LevelGenerationManager : MonoBehaviour
{
	public RuntimeDungeon dungeonGenerator;

	private StartOfRound playersManager;

	private RoundManager roundManager;

	private void Awake()
	{
		roundManager = Object.FindObjectOfType<RoundManager>();
		playersManager = Object.FindObjectOfType<StartOfRound>();
		if (playersManager != null)
		{
			dungeonGenerator.Generator.ShouldRandomizeSeed = false;
			dungeonGenerator.Generator.Seed = playersManager.randomMapSeed;
			dungeonGenerator.Generate();
		}
		else
		{
			Debug.Log("PLAYERS MANAGER WAS NOT FOUND FROM OTHER SCENE!");
		}
	}

	private void Update()
	{
	}
}
