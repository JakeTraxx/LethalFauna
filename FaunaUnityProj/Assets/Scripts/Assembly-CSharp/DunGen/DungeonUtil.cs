using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	public static class DungeonUtil
	{
		public static void AddAndSetupDoorComponent(Dungeon dungeon, GameObject doorPrefab, Doorway doorway)
		{
			Door door = doorPrefab.GetComponent<Door>();
			if (door == null)
			{
				door = doorPrefab.AddComponent<Door>();
			}
			door.Dungeon = dungeon;
			door.DoorwayA = doorway;
			door.DoorwayB = doorway.ConnectedDoorway;
			door.TileA = doorway.Tile;
			door.TileB = doorway.ConnectedDoorway.Tile;
			dungeon.AddAdditionalDoor(door);
		}

		public static bool HasAnyViableEntries(this List<GameObjectWeight> weights)
		{
			if (weights == null || weights.Count == 0)
			{
				return false;
			}
			foreach (GameObjectWeight weight in weights)
			{
				if (weight.GameObject != null && weight.Weight > 0f)
				{
					return true;
				}
			}
			return false;
		}

		public static GameObject GetRandom(this List<GameObjectWeight> weights, RandomStream randomStream)
		{
			float num = 0f;
			foreach (GameObjectWeight weight in weights)
			{
				if (weight.GameObject != null)
				{
					num += weight.Weight;
				}
			}
			float num2 = (float)(randomStream.NextDouble() * (double)num);
			foreach (GameObjectWeight weight2 in weights)
			{
				if (weight2 != null && !(weight2.GameObject == null))
				{
					if (num2 < weight2.Weight)
					{
						return weight2.GameObject;
					}
					num2 -= weight2.Weight;
				}
			}
			return null;
		}
	}
}
