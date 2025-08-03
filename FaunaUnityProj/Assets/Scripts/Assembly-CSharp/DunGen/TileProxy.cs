using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	public sealed class TileProxy
	{
		private List<DoorwayProxy> doorways = new List<DoorwayProxy>();

		public GameObject Prefab { get; private set; }

		public Tile PrefabTile { get; private set; }

		public TilePlacementData Placement { get; internal set; }

		public DoorwayProxy Entrance { get; private set; }

		public DoorwayProxy Exit { get; private set; }

		public ReadOnlyCollection<DoorwayProxy> Doorways { get; private set; }

		public IEnumerable<DoorwayProxy> UsedDoorways => doorways.Where((DoorwayProxy d) => d.Used);

		public IEnumerable<DoorwayProxy> UnusedDoorways => doorways.Where((DoorwayProxy d) => !d.Used);

		public TileProxy(TileProxy existingTile)
		{
			Prefab = existingTile.Prefab;
			PrefabTile = existingTile.PrefabTile;
			Placement = new TilePlacementData(existingTile.Placement);
			Doorways = new ReadOnlyCollection<DoorwayProxy>(doorways);
			foreach (DoorwayProxy doorway in existingTile.doorways)
			{
				DoorwayProxy doorwayProxy = new DoorwayProxy(this, doorway);
				doorways.Add(doorwayProxy);
				if (existingTile.Entrance == doorway)
				{
					Entrance = doorwayProxy;
				}
				if (existingTile.Exit == doorway)
				{
					Exit = doorwayProxy;
				}
			}
		}

		public TileProxy(GameObject prefab, bool ignoreSpriteRendererBounds, Vector3 upVector)
		{
			prefab.transform.localPosition = Vector3.zero;
			prefab.transform.localRotation = Quaternion.identity;
			Prefab = prefab;
			PrefabTile = prefab.GetComponent<Tile>();
			if (PrefabTile == null)
			{
				PrefabTile = prefab.AddComponent<Tile>();
			}
			Placement = new TilePlacementData();
			Doorways = new ReadOnlyCollection<DoorwayProxy>(doorways);
			Doorway[] componentsInChildren = prefab.GetComponentsInChildren<Doorway>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Doorway doorway = componentsInChildren[i];
				Vector3 position = doorway.transform.position;
				Quaternion rotation = doorway.transform.rotation;
				DoorwayProxy doorwayProxy = new DoorwayProxy(this, i, doorway, position, rotation);
				doorways.Add(doorwayProxy);
				if (PrefabTile.Entrance == doorway)
				{
					Entrance = doorwayProxy;
				}
				if (PrefabTile.Exit == doorway)
				{
					Exit = doorwayProxy;
				}
			}
			Bounds bounds = ((!(PrefabTile != null) || !PrefabTile.OverrideAutomaticTileBounds) ? UnityUtil.CalculateProxyBounds(Prefab, ignoreSpriteRendererBounds, upVector) : PrefabTile.TileBoundsOverride);
			if (bounds.size.x <= 0f || bounds.size.y <= 0f || bounds.size.z <= 0f)
			{
				Debug.LogError($"Tile prefab '{prefab}' has automatic bounds that are zero or negative in size. The bounding volume for this tile will need to be manually defined.", prefab);
			}
			Placement.LocalBounds = UnityUtil.CondenseBounds(bounds, Prefab.GetComponentsInChildren<Doorway>());
		}

		public void PositionBySocket(DoorwayProxy myDoorway, DoorwayProxy otherDoorway)
		{
			Quaternion quaternion = Quaternion.LookRotation(-otherDoorway.Forward, otherDoorway.Up);
			Placement.Rotation = quaternion * Quaternion.Inverse(Quaternion.Inverse(Placement.Rotation) * (Placement.Rotation * myDoorway.LocalRotation));
			Vector3 position = otherDoorway.Position;
			Placement.Position = position - (myDoorway.Position - Placement.Position);
		}

		private Vector3 CalculateOverlap(TileProxy other)
		{
			Bounds bounds = Placement.Bounds;
			Bounds bounds2 = other.Placement.Bounds;
			float a = bounds.max.x - bounds2.min.x;
			float b = bounds2.max.x - bounds.min.x;
			float a2 = bounds.max.y - bounds2.min.y;
			float b2 = bounds2.max.y - bounds.min.y;
			float a3 = bounds.max.z - bounds2.min.z;
			return new Vector3(z: Mathf.Min(a3, bounds2.max.z - bounds.min.z), x: Mathf.Min(a, b), y: Mathf.Min(a2, b2));
		}

		public bool IsOverlapping(TileProxy other, float maxOverlap)
		{
			Vector3 vector = CalculateOverlap(other);
			return Mathf.Min(vector.x, vector.y, vector.z) > maxOverlap;
		}

		public bool IsOverlappingOrOverhanging(TileProxy other, AxisDirection upDirection, float maxOverlap)
		{
			Vector3 vector = UnityUtil.CalculatePerAxisOverlap(other.Placement.Bounds, Placement.Bounds);
			float num;
			switch (upDirection)
			{
			case AxisDirection.PosX:
			case AxisDirection.NegX:
				num = Mathf.Min(vector.y, vector.z);
				break;
			case AxisDirection.PosY:
			case AxisDirection.NegY:
				num = Mathf.Min(vector.x, vector.z);
				break;
			case AxisDirection.PosZ:
			case AxisDirection.NegZ:
				num = Mathf.Min(vector.x, vector.y);
				break;
			default:
				throw new NotImplementedException("AxisDirection '" + upDirection.ToString() + "' is not implemented");
			}
			return num > maxOverlap;
		}
	}
}
