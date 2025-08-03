using DunGen;
using DunGen.Tags;
using UnityEngine;

public class SpawnPropOnDoorwayPair : MonoBehaviour
{
	public Tag CaveTag;

	private TileConnectionRule rule;

	public GameObject caveEntranceProp;

	private void OnEnable()
	{
		rule = new TileConnectionRule(CanTilesConnect);
		DoorwayPairFinder.CustomConnectionRules.Add(rule);
	}

	private void OnDisable()
	{
		DoorwayPairFinder.CustomConnectionRules.Remove(rule);
		rule = null;
	}

	private TileConnectionRule.ConnectionResult CanTilesConnect(Tile tileA, Tile tileB, Doorway doorwayA, Doorway doorwayB)
	{
		bool flag = tileA.Tags.HasTag(CaveTag);
		bool flag2 = tileB.Tags.HasTag(CaveTag);
		if (flag != flag2)
		{
			Doorway doorway = ((!flag) ? doorwayB : doorwayA);
			Object.Instantiate(caveEntranceProp, doorway.transform, worldPositionStays: false);
			Debug.Log($"got tile: {tileA.gameObject}", tileA.gameObject);
			Debug.Log($"got doorway! {doorwayA}; {doorwayA.name}; {doorwayA.gameObject}", doorwayA.gameObject);
			Debug.Log($"got doorway B! {doorwayB}; {doorwayB.name}; {doorwayB.gameObject}");
		}
		return TileConnectionRule.ConnectionResult.Passthrough;
	}
}
