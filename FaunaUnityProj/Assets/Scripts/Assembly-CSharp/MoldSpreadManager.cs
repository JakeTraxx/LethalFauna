using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class MoldSpreadManager : NetworkBehaviour
{
	private bool finishedGeneratingMold;

	public PlanetMoldState[] planetMoldStates;

	public List<GameObject> generatedMold = new List<GameObject>();

	public GameObject moldPrefab;

	public Transform moldContainer;

	public int moldBranchCount = 3;

	public int maxSporesInSingleIteration = 25;

	public int maxIterations = 25;

	public int moldDistance = 9;

	private Collider[] weedColliders;

	public GameObject destroyParticle;

	public AudioSource destroyAudio;

	public int iterationsThisDay;

	private void Start()
	{
		planetMoldStates = new PlanetMoldState[StartOfRound.Instance.levels.Length];
		for (int i = 0; i < planetMoldStates.Length; i++)
		{
			planetMoldStates[i] = new PlanetMoldState();
			if (base.IsServer)
			{
				planetMoldStates[i].destroyedMold = ES3.Load($"Level{StartOfRound.Instance.levels[i].levelID}DestroyedMold", GameNetworkManager.Instance.currentSaveFileName, new int[0]).ToList();
			}
			else
			{
				planetMoldStates[i].destroyedMold = new List<int>();
			}
		}
		Debug.Log($"planet mold states length: ${planetMoldStates.Length}");
		weedColliders = new Collider[3];
	}

	public void ResetMoldData()
	{
		for (int i = 0; i < planetMoldStates.Length; i++)
		{
			planetMoldStates[i].destroyedMold.Clear();
			generatedMold.Clear();
		}
	}

	public void SyncDestroyedMoldPositions(int[] destroyedMoldSpots)
	{
		if (!base.IsServer)
		{
			Debug.Log($"Sync A; {destroyedMoldSpots.Length}");
			for (int i = 0; i < destroyedMoldSpots.Length; i++)
			{
				Debug.Log($"Sync B{i}; {destroyedMoldSpots[i]}");
				DestroyMoldAtIndex(destroyedMoldSpots[i], 2f);
			}
		}
	}

	public void DestroyMoldAtIndex(int index, float radius = 1.5f, bool playEffect = false)
	{
		Debug.Log($"C {planetMoldStates[StartOfRound.Instance.currentLevelID] != null}");
		Debug.Log($"D {planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold != null}");
		if (!planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Contains(index))
		{
			planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Add(index);
		}
		if (index < generatedMold.Count)
		{
			Debug.Log($"E; null? {generatedMold[index] == null}");
			generatedMold[index].SetActive(value: false);
		}
	}

	public void DestroyMoldAtPosition(Vector3 pos, bool playEffect = false)
	{
		int num = Physics.OverlapSphereNonAlloc(pos, 0.5f, weedColliders, 65536, QueryTriggerInteraction.Collide);
		Debug.Log($"weeds found at pos {pos}: {num}");
		for (int i = 0; i < num; i++)
		{
			int num2 = generatedMold.IndexOf(weedColliders[i].gameObject);
			if (num2 != -1)
			{
				weedColliders[i].gameObject.SetActive(value: false);
				if (!planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Contains(num2))
				{
					planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Add(num2);
				}
				if (playEffect)
				{
					UnityEngine.Object.Instantiate(destroyParticle, weedColliders[i].transform.position + Vector3.up * 0.5f, Quaternion.identity, null);
					destroyAudio.Stop();
					destroyAudio.transform.position = weedColliders[i].transform.position + Vector3.up * 0.5f;
					destroyAudio.Play();
					RoundManager.Instance.PlayAudibleNoise(destroyAudio.transform.position, 6f, 0.5f, 0, noiseIsInsideClosedShip: false, 99611);
				}
			}
			Debug.Log($"Index: {num2}");
		}
		CheckIfAllSporesDestroyed();
	}

	private void CheckIfAllSporesDestroyed()
	{
		bool flag = true;
		for (int i = 0; i < generatedMold.Count; i++)
		{
			if (generatedMold[i].activeSelf)
			{
				flag = false;
			}
		}
		if (flag)
		{
			StartOfRound.Instance.currentLevel.moldSpreadIterations = 0;
			StartOfRound.Instance.currentLevel.moldStartPosition = -1;
		}
	}

	private Vector3 ChooseMoldSpawnPosition(Vector3 pos, int xOffset, int zOffset)
	{
		pos += new Vector3(xOffset, pos.y, zOffset);
		pos = RoundManager.Instance.GetNavMeshPosition(pos, default(NavMeshHit), 12f, 1);
		if (!RoundManager.Instance.GotNavMeshPositionResult)
		{
			return Vector3.zero;
		}
		return pos;
	}

	public void GenerateMold(Vector3 startingPosition, int iterations)
	{
		if (iterations == 0 || finishedGeneratingMold)
		{
			return;
		}
		iterationsThisDay = iterations;
		System.Random random = new System.Random((int)startingPosition.x + (int)startingPosition.z);
		Vector3 vector = startingPosition;
		if (Physics.Raycast(startingPosition, Vector3.down, out var hitInfo, 100f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
		{
			vector = new Vector3(Mathf.Round(hitInfo.point.x), Mathf.Round(hitInfo.point.y), Mathf.Round(hitInfo.point.z));
		}
		GameObject item = UnityEngine.Object.Instantiate(moldPrefab, vector, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(-180f, 180f), 0f)), moldContainer);
		generatedMold.Add(item);
		List<MoldSpore> list = new List<MoldSpore>();
		List<MoldSpore> list2 = new List<MoldSpore>();
		Vector3 zero = Vector3.zero;
		list.Add(new MoldSpore(vector, marked: false, 0));
		int num = 0;
		bool flag = true;
		bool flag2 = false;
		iterations = Mathf.Min(iterations, maxIterations);
		List<MoldSpore> list3 = new List<MoldSpore>();
		for (int i = 0; i < iterations; i++)
		{
			GameObject gameObject = GameObject.FindGameObjectWithTag("MoldAttractionPoint");
			bool flag3 = gameObject != null;
			int num2 = Mathf.Min(list.Count, maxSporesInSingleIteration);
			for (int j = 0; j < num2; j++)
			{
				int num3 = random.Next(1, moldBranchCount);
				for (int k = 0; k < num3; k++)
				{
					int num4;
					int num5;
					if (flag3 && Vector3.Distance(list[j].spawnPosition, gameObject.transform.position) > 35f && Vector3.Distance(list[j].spawnPosition, StartOfRound.Instance.elevatorTransform.position) > 35f)
					{
						num4 = ((!(list[j].spawnPosition.x < gameObject.transform.position.x)) ? random.Next(-moldDistance, -1) : random.Next(1, moldDistance));
						num5 = ((!(list[j].spawnPosition.z < gameObject.transform.position.z)) ? random.Next(-moldDistance, -1) : random.Next(1, moldDistance));
					}
					else
					{
						num4 = random.Next(-moldDistance, moldDistance);
						num5 = random.Next(-moldDistance, moldDistance);
					}
					zero = ChooseMoldSpawnPosition(xOffset: (num4 >= 0) ? Mathf.Max(num4, 4) : Mathf.Min(num4, -4), zOffset: (num5 >= 0) ? Mathf.Max(num5, 4) : Mathf.Min(num5, -4), pos: list[j].spawnPosition);
					flag2 = zero != Vector3.zero;
					bool flag4 = false;
					float num6 = (float)random.Next(75, 130) / 100f;
					num++;
					MoldSpore moldSpore = new MoldSpore(zero, flag4, num);
					bool flag5 = Physics.CheckSphere(zero, 1f, 65536, QueryTriggerInteraction.Ignore);
					if (!flag5 && (planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Contains(num) || (list[j].destroyedByPlayer && i + 1 == iterations)))
					{
						list3.Add(moldSpore);
						moldSpore.destroyedByPlayer = true;
					}
					else if (!flag2 || list[j].markedForDestruction || flag5)
					{
						flag4 = true;
					}
					else
					{
						item = UnityEngine.Object.Instantiate(moldPrefab, zero, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(-180f, 180f), 0f)), moldContainer);
						item.transform.localScale = item.transform.localScale * num6;
						flag = false;
						generatedMold.Add(item);
					}
					moldSpore.markedForDestruction = flag4;
					list2.Add(moldSpore);
					if (num == 1)
					{
						Debug.DrawRay(zero, Vector3.up * 10f, Color.yellow, 10f);
					}
					if (moldSpore.destroyedByPlayer || moldSpore.markedForDestruction)
					{
						Debug.DrawRay(list[j].spawnPosition, zero - list[j].spawnPosition, Color.red, 10f);
					}
					else
					{
						Debug.DrawRay(list[j].spawnPosition, zero - list[j].spawnPosition, Color.green, 10f);
					}
				}
			}
			if (!flag)
			{
				list = new List<MoldSpore>(list2);
				list2.Clear();
				continue;
			}
			Debug.Log("No spores generated; setting iterations to 0");
			if (i == 0)
			{
				iterationsThisDay = 0;
				StartOfRound.Instance.currentLevel.moldSpreadIterations = 0;
				StartOfRound.Instance.currentLevel.moldStartPosition = -1;
			}
			break;
		}
		list.Clear();
		for (int l = 0; l < list3.Count; l++)
		{
			if (Physics.CheckSphere(list3[l].spawnPosition, 8f, 65536, QueryTriggerInteraction.Ignore))
			{
				if (planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Contains(list3[l].generationId))
				{
					planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Remove(list3[l].generationId);
					list.Add(list3[l]);
					Debug.Log($"Growing back mold at index {list3[l].generationId}");
				}
			}
			else if (!planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Contains(list3[l].generationId))
			{
				planetMoldStates[StartOfRound.Instance.currentLevelID].destroyedMold.Add(list3[l].generationId);
			}
		}
		for (int m = 0; m < list.Count; m++)
		{
			zero = list[m].spawnPosition;
			item = UnityEngine.Object.Instantiate(moldPrefab, zero, Quaternion.Euler(new Vector3(0f, UnityEngine.Random.Range(-180f, 180f), 0f)), moldContainer);
			item.transform.localScale = item.transform.localScale * 1f;
		}
		finishedGeneratingMold = true;
	}

	public void RemoveAllMold()
	{
		for (int i = 0; i < generatedMold.Count; i++)
		{
			if (generatedMold[i] != null)
			{
				UnityEngine.Object.Destroy(generatedMold[i]);
			}
		}
		generatedMold.Clear();
		finishedGeneratingMold = false;
	}
}
