using System;
using System.Collections.Generic;
using System.Linq;
using DunGen.Graph;
using UnityEngine;

namespace DunGen
{
	public sealed class DoorwayPairFinder
	{
		public static List<TileConnectionRule> CustomConnectionRules = new List<TileConnectionRule>();

		public RandomStream RandomStream;

		public List<GameObjectChance> TileWeights;

		public TileProxy PreviousTile;

		public bool IsOnMainPath;

		public float NormalizedDepth;

		public DungeonArchetype Archetype;

		public bool? AllowRotation;

		public Vector3 UpVector;

		public TileMatchDelegate IsTileAllowedPredicate;

		public GetTileTemplateDelegate GetTileTemplateDelegate;

		public DungeonFlow DungeonFlow;

		private List<GameObjectChance> tileOrder;

		public Queue<DoorwayPair> GetDoorwayPairs(int? maxCount)
		{
			tileOrder = CalculateOrderedListOfTiles();
			List<DoorwayPair> list = ((PreviousTile != null) ? GetPotentialDoorwayPairsForNonFirstTile().ToList() : GetPotentialDoorwayPairsForFirstTile().ToList());
			int num = list.Count;
			if (maxCount.HasValue)
			{
				num = Math.Min(num, maxCount.Value);
			}
			Queue<DoorwayPair> queue = new Queue<DoorwayPair>(num);
			foreach (DoorwayPair item in OrderDoorwayPairs(list, num))
			{
				queue.Enqueue(item);
			}
			return queue;
		}

		private int CompareDoorwaysTileWeight(DoorwayPair x, DoorwayPair y)
		{
			return y.TileWeight.CompareTo(x.TileWeight);
		}

		private IEnumerable<DoorwayPair> OrderDoorwayPairs(List<DoorwayPair> potentialPairs, int count)
		{
			potentialPairs.Sort(CompareDoorwaysTileWeight);
			for (int i = 0; i < potentialPairs.Count - 1; i++)
			{
				for (int j = 0; j < potentialPairs.Count - 1; j++)
				{
					if (potentialPairs[j].TileWeight == potentialPairs[j + 1].TileWeight && potentialPairs[j].DoorwayWeight < potentialPairs[j + 1].DoorwayWeight)
					{
						DoorwayPair value = potentialPairs[j];
						potentialPairs[j] = potentialPairs[j + 1];
						potentialPairs[j + 1] = value;
					}
				}
			}
			return potentialPairs.Take(count);
		}

		private List<GameObjectChance> CalculateOrderedListOfTiles()
		{
			List<GameObjectChance> list = new List<GameObjectChance>(TileWeights.Count);
			GameObjectChanceTable gameObjectChanceTable = new GameObjectChanceTable();
			gameObjectChanceTable.Weights.AddRange(TileWeights);
			while (gameObjectChanceTable.Weights.Any((GameObjectChance x) => x.Value != null && x.GetWeight(IsOnMainPath, NormalizedDepth) > 0f))
			{
				list.Add(gameObjectChanceTable.GetRandom(RandomStream, IsOnMainPath, NormalizedDepth, null, allowImmediateRepeats: true, removeFromTable: true));
			}
			return list;
		}

		private IEnumerable<DoorwayPair> GetPotentialDoorwayPairsForNonFirstTile()
		{
			foreach (DoorwayProxy previousDoor in PreviousTile.UnusedDoorways)
			{
				if (PreviousTile.Exit != null && !PreviousTile.UsedDoorways.Contains(PreviousTile.Exit) && PreviousTile.Exit != previousDoor)
				{
					continue;
				}
				foreach (GameObjectChance tileWeight in TileWeights)
				{
					if (!tileOrder.Contains(tileWeight))
					{
						continue;
					}
					TileProxy nextTile = GetTileTemplateDelegate(tileWeight.Value);
					float weight = tileOrder.Count - tileOrder.IndexOf(tileWeight);
					if (IsTileAllowedPredicate != null && !IsTileAllowedPredicate(PreviousTile, nextTile, ref weight))
					{
						continue;
					}
					foreach (DoorwayProxy doorway in nextTile.Doorways)
					{
						if ((nextTile == null || nextTile.Entrance == null || nextTile.Entrance == doorway) && (nextTile == null || nextTile.Exit != doorway))
						{
							float weight2 = 0f;
							if (IsValidDoorwayPairing(previousDoor, doorway, PreviousTile, nextTile, ref weight2))
							{
								yield return new DoorwayPair(PreviousTile, previousDoor, nextTile, doorway, tileWeight.TileSet, weight, weight2);
							}
						}
					}
				}
			}
		}

		private IEnumerable<DoorwayPair> GetPotentialDoorwayPairsForFirstTile()
		{
			foreach (GameObjectChance tileWeight in TileWeights)
			{
				if (!tileOrder.Contains(tileWeight))
				{
					continue;
				}
				TileProxy nextTile = GetTileTemplateDelegate(tileWeight.Value);
				float weight = tileWeight.GetWeight(IsOnMainPath, NormalizedDepth) * (float)RandomStream.NextDouble();
				if (IsTileAllowedPredicate != null && !IsTileAllowedPredicate(PreviousTile, nextTile, ref weight))
				{
					continue;
				}
				foreach (DoorwayProxy doorway in nextTile.Doorways)
				{
					float doorwayWeight = CalculateDoorwayWeight(doorway);
					yield return new DoorwayPair(null, null, nextTile, doorway, tileWeight.TileSet, weight, doorwayWeight);
				}
			}
		}

		private bool IsValidDoorwayPairing(DoorwayProxy a, DoorwayProxy b, TileProxy previousTile, TileProxy nextTile, ref float weight)
		{
			if (!DungeonFlow.CanDoorwaysConnect(PreviousTile.PrefabTile, nextTile.PrefabTile, a.DoorwayComponent, b.DoorwayComponent))
			{
				return false;
			}
			Vector3? vector = null;
			bool flag = (AllowRotation.HasValue && !AllowRotation.Value) || (nextTile != null && !nextTile.PrefabTile.AllowRotation);
			if (Vector3.Angle(a.Forward, UpVector) < 1f)
			{
				vector = -UpVector;
			}
			else if (Vector3.Angle(a.Forward, -UpVector) < 1f)
			{
				vector = UpVector;
			}
			else if (flag)
			{
				vector = -a.Forward;
			}
			if (vector.HasValue && Vector3.Angle(vector.Value, b.Forward) > 1f)
			{
				return false;
			}
			weight = CalculateDoorwayWeight(b);
			return true;
		}

		private float CalculateDoorwayWeight(DoorwayProxy doorway)
		{
			float num = (float)RandomStream.NextDouble();
			float num2 = ((Archetype == null) ? 0f : Archetype.StraightenChance);
			if (num2 > 0f && IsOnMainPath && PreviousTile.UsedDoorways.Count() == 1 && PreviousTile.UsedDoorways.First().Forward == -doorway.Forward && RandomStream.NextDouble() < (double)num2)
			{
				num *= 100f;
			}
			return num;
		}
	}
}
