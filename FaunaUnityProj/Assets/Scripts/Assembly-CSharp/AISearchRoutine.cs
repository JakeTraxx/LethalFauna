using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AISearchRoutine
{
	public List<GameObject> unsearchedNodes = new List<GameObject>();

	public GameObject currentTargetNode;

	public GameObject nextTargetNode;

	public bool waitingForTargetNode;

	public bool choseTargetNode;

	public Vector3 currentSearchStartPosition;

	public bool loopSearch = true;

	public int timesFinishingSearch;

	public int nodesEliminatedInCurrentSearch;

	public bool inProgress;

	public bool calculatingNodeInSearch;

	public bool startedSearchAtSelf;

	[Space(5f)]
	public float searchWidth = 200f;

	public float searchPrecision = 5f;

	public bool randomized;

	public bool onlySearchNodesInLOS;

	public float GetCurrentDistanceOfSearch()
	{
		return Vector3.Distance(currentSearchStartPosition, currentTargetNode.transform.position);
	}
}
