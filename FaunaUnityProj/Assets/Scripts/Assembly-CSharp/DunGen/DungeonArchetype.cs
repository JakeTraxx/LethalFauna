using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	[CreateAssetMenu(fileName = "New Archetype", menuName = "DunGen/Dungeon Archetype", order = 700)]
	public sealed class DungeonArchetype : ScriptableObject
	{
		public List<TileSet> TileSets = new List<TileSet>();

		public List<TileSet> BranchCapTileSets = new List<TileSet>();

		public BranchCapType BranchCapType = BranchCapType.AsWellAs;

		public IntRange BranchingDepth = new IntRange(2, 4);

		public IntRange BranchCount = new IntRange(0, 2);

		public float StraightenChance;

		public bool Unique;

		public bool GetHasValidBranchCapTiles()
		{
			if (BranchCapTileSets.Count == 0)
			{
				return false;
			}
			foreach (TileSet branchCapTileSet in BranchCapTileSets)
			{
				if (branchCapTileSet.TileWeights.Weights.Count > 0)
				{
					return true;
				}
			}
			return false;
		}
	}
}
