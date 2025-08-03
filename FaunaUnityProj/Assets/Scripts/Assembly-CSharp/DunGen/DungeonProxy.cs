using System.Collections.Generic;
using System.Linq;
using DunGen.Graph;
using UnityEngine;

namespace DunGen
{
	public sealed class DungeonProxy
	{
		public List<TileProxy> AllTiles = new List<TileProxy>();

		public List<TileProxy> MainPathTiles = new List<TileProxy>();

		public List<TileProxy> BranchPathTiles = new List<TileProxy>();

		public List<ProxyDoorwayConnection> Connections = new List<ProxyDoorwayConnection>();

		private Transform visualsRoot;

		private Dictionary<TileProxy, GameObject> tileVisuals = new Dictionary<TileProxy, GameObject>();

		public DungeonProxy(Transform debugVisualsRoot = null)
		{
			visualsRoot = debugVisualsRoot;
		}

		public void ClearDebugVisuals()
		{
			GameObject[] array = tileVisuals.Values.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				Object.DestroyImmediate(array[i]);
			}
			tileVisuals.Clear();
		}

		public void MakeConnection(DoorwayProxy a, DoorwayProxy b)
		{
			DoorwayProxy.Connect(a, b);
			ProxyDoorwayConnection item = new ProxyDoorwayConnection(a, b);
			Connections.Add(item);
		}

		public void RemoveLastConnection()
		{
			RemoveConnection(Connections.Last());
		}

		public void RemoveConnection(ProxyDoorwayConnection connection)
		{
			connection.A.Disconnect();
			Connections.Remove(connection);
		}

		internal void AddTile(TileProxy tile)
		{
			AllTiles.Add(tile);
			if (tile.Placement.IsOnMainPath)
			{
				MainPathTiles.Add(tile);
			}
			else
			{
				BranchPathTiles.Add(tile);
			}
			if (visualsRoot != null)
			{
				GameObject gameObject = Object.Instantiate(tile.Prefab, visualsRoot);
				gameObject.transform.localPosition = tile.Placement.Position;
				gameObject.transform.localRotation = tile.Placement.Rotation;
				tileVisuals[tile] = gameObject;
			}
		}

		internal void RemoveTile(TileProxy tile)
		{
			AllTiles.Remove(tile);
			if (tile.Placement.IsOnMainPath)
			{
				MainPathTiles.Remove(tile);
			}
			else
			{
				BranchPathTiles.Remove(tile);
			}
			if (tileVisuals.TryGetValue(tile, out var value))
			{
				Object.DestroyImmediate(value);
				tileVisuals.Remove(tile);
			}
		}

		internal void ConnectOverlappingDoorways(float globalChance, DungeonFlow dungeonFlow, RandomStream randomStream)
		{
			IEnumerable<DoorwayProxy> enumerable = AllTiles.SelectMany((TileProxy t) => t.UnusedDoorways);
			foreach (DoorwayProxy item in enumerable)
			{
				foreach (DoorwayProxy item2 in enumerable)
				{
					if (item.Used || item2.Used || item == item2 || item.TileProxy == item2.TileProxy || !dungeonFlow.CanDoorwaysConnect(item.TileProxy.PrefabTile, item2.TileProxy.PrefabTile, item.DoorwayComponent, item2.DoorwayComponent) || (item.Position - item2.Position).sqrMagnitude >= 1E-05f)
					{
						continue;
					}
					if (dungeonFlow.RestrictConnectionToSameSection)
					{
						bool flag = item.TileProxy.Placement.GraphLine == item2.TileProxy.Placement.GraphLine;
						if (item.TileProxy.Placement.GraphLine == null)
						{
							flag = false;
						}
						if (!flag)
						{
							continue;
						}
					}
					float num = globalChance;
					if (item.TileProxy.PrefabTile.OverrideConnectionChance && item2.TileProxy.PrefabTile.OverrideConnectionChance)
					{
						num = Mathf.Min(item.TileProxy.PrefabTile.ConnectionChance, item2.TileProxy.PrefabTile.ConnectionChance);
					}
					else if (item.TileProxy.PrefabTile.OverrideConnectionChance)
					{
						num = item.TileProxy.PrefabTile.ConnectionChance;
					}
					else if (item2.TileProxy.PrefabTile.OverrideConnectionChance)
					{
						num = item2.TileProxy.PrefabTile.ConnectionChance;
					}
					if (!(num <= 0f) && randomStream.NextDouble() < (double)num)
					{
						MakeConnection(item, item2);
					}
				}
			}
		}
	}
}
