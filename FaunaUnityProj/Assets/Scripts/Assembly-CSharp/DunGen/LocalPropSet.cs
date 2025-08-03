using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Random Props/Local Prop Set")]
	public class LocalPropSet : RandomProp
	{
		private static readonly Dictionary<LocalPropSetCountMode, GetPropCountDelegate> GetCountMethods;

		[AcceptGameObjectTypes(GameObjectFilter.Scene)]
		public GameObjectChanceTable Props = new GameObjectChanceTable();

		public IntRange PropCount = new IntRange(1, 1);

		public LocalPropSetCountMode CountMode;

		public AnimationCurve CountDepthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		public override void Process(RandomStream randomStream, Tile tile)
		{
			GameObjectChanceTable gameObjectChanceTable = Props.Clone();
			if (!GetCountMethods.TryGetValue(CountMode, out var value))
			{
				throw new NotImplementedException("LocalPropSet count mode \"" + CountMode.ToString() + "\" is not yet implemented");
			}
			int num = value(this, randomStream, tile);
			List<GameObject> list = new List<GameObject>(num);
			for (int i = 0; i < num; i++)
			{
				GameObjectChance random = gameObjectChanceTable.GetRandom(randomStream, tile.Placement.IsOnMainPath, tile.Placement.NormalizedDepth, null, allowImmediateRepeats: true, removeFromTable: true);
				if (random != null && random.Value != null)
				{
					list.Add(random.Value);
				}
			}
			foreach (GameObjectChance weight in Props.Weights)
			{
				if (!list.Contains(weight.Value))
				{
					UnityUtil.Destroy(weight.Value);
				}
			}
		}

		static LocalPropSet()
		{
			GetCountMethods = new Dictionary<LocalPropSetCountMode, GetPropCountDelegate>();
			GetCountMethods[LocalPropSetCountMode.Random] = GetCountRandom;
			GetCountMethods[LocalPropSetCountMode.DepthBased] = GetCountDepthBased;
			GetCountMethods[LocalPropSetCountMode.DepthMultiply] = GetCountDepthMultiply;
		}

		private static int GetCountRandom(LocalPropSet propSet, RandomStream randomStream, Tile tile)
		{
			return Mathf.Clamp(propSet.PropCount.GetRandom(randomStream), 0, propSet.Props.Weights.Count);
		}

		private static int GetCountDepthBased(LocalPropSet propSet, RandomStream randomStream, Tile tile)
		{
			float t = Mathf.Clamp(propSet.CountDepthCurve.Evaluate(tile.Placement.NormalizedPathDepth), 0f, 1f);
			return Mathf.RoundToInt(Mathf.Lerp(propSet.PropCount.Min, propSet.PropCount.Max, t));
		}

		private static int GetCountDepthMultiply(LocalPropSet propSet, RandomStream randomStream, Tile tile)
		{
			float num = Mathf.Clamp(propSet.CountDepthCurve.Evaluate(tile.Placement.NormalizedPathDepth), 0f, 1f);
			return Mathf.RoundToInt((float)GetCountRandom(propSet, randomStream, tile) * num);
		}
	}
}
