using System;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	public sealed class GameObjectChance
	{
		public GameObject Value;

		public float MainPathWeight = 1f;

		public float BranchPathWeight = 1f;

		public AnimationCurve DepthWeightScale = AnimationCurve.Linear(0f, 1f, 1f, 1f);

		public TileSet TileSet;

		public GameObjectChance()
			: this(null, 1f, 1f, null)
		{
		}

		public GameObjectChance(GameObject value)
			: this(value, 1f, 1f, null)
		{
		}

		public GameObjectChance(GameObject value, float mainPathWeight, float branchPathWeight, TileSet tileSet)
		{
			Value = value;
			MainPathWeight = mainPathWeight;
			BranchPathWeight = branchPathWeight;
			TileSet = tileSet;
		}

		public float GetWeight(bool isOnMainPath, float normalizedDepth)
		{
			return (isOnMainPath ? MainPathWeight : BranchPathWeight) * DepthWeightScale.Evaluate(normalizedDepth);
		}
	}
}
