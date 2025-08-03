using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	[CreateAssetMenu(menuName = "DunGen/Tile Set", order = 700)]
	public sealed class TileSet : ScriptableObject
	{
		public GameObjectChanceTable TileWeights = new GameObjectChanceTable();

		public List<LockedDoorwayAssociation> LockPrefabs = new List<LockedDoorwayAssociation>();

		public void AddTile(GameObject tilePrefab, float mainPathWeight, float branchPathWeight)
		{
			TileWeights.Weights.Add(new GameObjectChance(tilePrefab, mainPathWeight, branchPathWeight, this));
		}

		public void AddTiles(IEnumerable<GameObject> tilePrefab, float mainPathWeight, float branchPathWeight)
		{
			foreach (GameObject item in tilePrefab)
			{
				AddTile(item, mainPathWeight, branchPathWeight);
			}
		}
	}
}
