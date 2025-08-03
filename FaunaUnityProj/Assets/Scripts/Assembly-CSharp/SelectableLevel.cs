using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "ScriptableObjects/Level", order = 2)]
public class SelectableLevel : ScriptableObject
{
	public GameObject planetPrefab;

	public string sceneName;

	public int levelID;

	public bool lockedForDemo;

	[Space(3f)]
	public bool spawnEnemiesAndScrap = true;

	public string PlanetName;

	[TextArea(5, 15)]
	public string LevelDescription;

	public VideoClip videoReel;

	public string riskLevel;

	public float timeToArrive = 8f;

	[Header("Time")]
	public float OffsetFromGlobalTime;

	public float DaySpeedMultiplier = 1f;

	public bool planetHasTime = true;

	[Space(5f)]
	public RandomWeatherWithVariables[] randomWeathers;

	public bool overrideWeather;

	public LevelWeatherType overrideWeatherType;

	[Space(2f)]
	public LevelWeatherType currentWeather;

	[Space(7f)]
	[Header("Level Values")]
	public float factorySizeMultiplier;

	public IntWithRarity[] dungeonFlowTypes;

	[Space(3f)]
	public SpawnableMapObject[] spawnableMapObjects;

	public SpawnableOutsideObjectWithRarity[] spawnableOutsideObjects;

	public bool canSpawnMold = true;

	public int moldSpreadIterations;

	public int moldStartPosition;

	[Space(3f)]
	public List<SpawnableItemWithRarity> spawnableScrap = new List<SpawnableItemWithRarity>();

	public int minScrap;

	public int maxScrap;

	public int minTotalScrapValue;

	public int maxTotalScrapValue;

	[Space(3f)]
	public LevelAmbienceLibrary levelAmbienceClips;

	[Header("Level enemy values")]
	public int maxEnemyPowerCount = 8;

	public int maxOutsideEnemyPowerCount = 15;

	public int maxDaytimeEnemyPowerCount = 20;

	[Space(3f)]
	public List<SpawnableEnemyWithRarity> Enemies = new List<SpawnableEnemyWithRarity>();

	public List<SpawnableEnemyWithRarity> OutsideEnemies = new List<SpawnableEnemyWithRarity>();

	[Space(4f)]
	public List<SpawnableEnemyWithRarity> DaytimeEnemies = new List<SpawnableEnemyWithRarity>();

	[Space(3f)]
	public AnimationCurve enemySpawnChanceThroughoutDay;

	public AnimationCurve outsideEnemySpawnChanceThroughDay;

	public AnimationCurve daytimeEnemySpawnChanceThroughDay;

	public float spawnProbabilityRange = 3f;

	public float daytimeEnemiesProbabilityRange = 10f;

	public bool levelIncludesSnowFootprints;

	public string levelIconString;
}
