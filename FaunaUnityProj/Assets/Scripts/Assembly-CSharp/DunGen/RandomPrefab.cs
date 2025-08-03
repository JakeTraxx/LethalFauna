using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Random Props/Random Prefab")]
	public class RandomPrefab : RandomProp
	{
		[AcceptGameObjectTypes(GameObjectFilter.Asset)]
		public GameObjectChanceTable Props = new GameObjectChanceTable();

		public bool ZeroPosition = true;

		public bool ZeroRotation = true;

		public override void Process(RandomStream randomStream, Tile tile)
		{
			if (Props.Weights.Count > 0)
			{
				GameObject value = Props.GetRandom(randomStream, tile.Placement.IsOnMainPath, tile.Placement.NormalizedDepth, null, allowImmediateRepeats: true, removeFromTable: true).Value;
				GameObject gameObject = Object.Instantiate(value);
				gameObject.transform.parent = base.transform;
				if (ZeroPosition)
				{
					gameObject.transform.localPosition = Vector3.zero;
				}
				else
				{
					gameObject.transform.localPosition = value.transform.localPosition;
				}
				if (ZeroRotation)
				{
					gameObject.transform.localRotation = Quaternion.identity;
				}
				else
				{
					gameObject.transform.localRotation = value.transform.localRotation;
				}
			}
		}
	}
}
