using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DunGen.Graph;
using UnityEngine;

namespace DunGen
{
	public class Dungeon : MonoBehaviour
	{
		public bool DebugRender;

		private readonly List<Tile> allTiles = new List<Tile>();

		private readonly List<Tile> mainPathTiles = new List<Tile>();

		private readonly List<Tile> branchPathTiles = new List<Tile>();

		private readonly List<GameObject> doors = new List<GameObject>();

		private readonly List<DoorwayConnection> connections = new List<DoorwayConnection>();

		public Bounds Bounds { get; protected set; }

		public DungeonFlow DungeonFlow { get; protected set; }

		public ReadOnlyCollection<Tile> AllTiles { get; private set; }

		public ReadOnlyCollection<Tile> MainPathTiles { get; private set; }

		public ReadOnlyCollection<Tile> BranchPathTiles { get; private set; }

		public ReadOnlyCollection<GameObject> Doors { get; private set; }

		public ReadOnlyCollection<DoorwayConnection> Connections { get; private set; }

		public DungeonGraph ConnectionGraph { get; private set; }

		public Dungeon()
		{
			AllTiles = new ReadOnlyCollection<Tile>(allTiles);
			MainPathTiles = new ReadOnlyCollection<Tile>(mainPathTiles);
			BranchPathTiles = new ReadOnlyCollection<Tile>(branchPathTiles);
			Doors = new ReadOnlyCollection<GameObject>(doors);
			Connections = new ReadOnlyCollection<DoorwayConnection>(connections);
		}

		internal void AddAdditionalDoor(Door door)
		{
			if (door != null)
			{
				doors.Add(door.gameObject);
			}
		}

		internal void PreGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			DungeonFlow = dungeonGenerator.DungeonFlow;
		}

		internal void PostGenerateDungeon(DungeonGenerator dungeonGenerator)
		{
			ConnectionGraph = new DungeonGraph(this);
			Bounds = UnityUtil.CombineBounds(allTiles.Select((Tile x) => x.Placement.Bounds).ToArray());
		}

		public void Clear()
		{
			foreach (Tile allTile in allTiles)
			{
				foreach (Doorway usedDoorway in allTile.UsedDoorways)
				{
					if (usedDoorway.UsedDoorPrefabInstance != null)
					{
						UnityUtil.Destroy(usedDoorway.UsedDoorPrefabInstance);
					}
				}
				UnityUtil.Destroy(allTile.gameObject);
			}
			for (int i = 0; i < base.transform.childCount; i++)
			{
				UnityUtil.Destroy(base.transform.GetChild(i).gameObject);
			}
			allTiles.Clear();
			mainPathTiles.Clear();
			branchPathTiles.Clear();
			doors.Clear();
			connections.Clear();
		}

		public Doorway GetConnectedDoorway(Doorway doorway)
		{
			foreach (DoorwayConnection connection in connections)
			{
				if (connection.A == doorway)
				{
					return connection.B;
				}
				if (connection.B == doorway)
				{
					return connection.A;
				}
			}
			return null;
		}

		public void FromProxy(DungeonProxy proxyDungeon, DungeonGenerator generator)
		{
			Clear();
			Dictionary<TileProxy, Tile> dictionary = new Dictionary<TileProxy, Tile>();
			foreach (TileProxy allTile in proxyDungeon.AllTiles)
			{
				GameObject obj = Object.Instantiate(allTile.Prefab, generator.Root.transform);
				obj.transform.localPosition = allTile.Placement.Position;
				obj.transform.localRotation = allTile.Placement.Rotation;
				Tile component = obj.GetComponent<Tile>();
				component.Dungeon = this;
				component.Placement = new TilePlacementData(allTile.Placement);
				dictionary[allTile] = component;
				allTiles.Add(component);
				if (component.Placement.IsOnMainPath)
				{
					mainPathTiles.Add(component);
				}
				else
				{
					branchPathTiles.Add(component);
				}
				if (generator.PlaceTileTriggers)
				{
					component.AddTriggerVolume();
					component.gameObject.layer = generator.TileTriggerLayer;
				}
				Doorway[] componentsInChildren = obj.GetComponentsInChildren<Doorway>();
				Doorway[] array = componentsInChildren;
				foreach (Doorway doorway in array)
				{
					doorway.Tile = component;
					doorway.placedByGenerator = true;
					doorway.HideConditionalObjects = false;
					component.AllDoorways.Add(doorway);
				}
				foreach (DoorwayProxy usedDoorway in allTile.UsedDoorways)
				{
					Doorway doorway2 = componentsInChildren[usedDoorway.Index];
					component.UsedDoorways.Add(doorway2);
					foreach (GameObject blockerSceneObject in doorway2.BlockerSceneObjects)
					{
						if (blockerSceneObject != null)
						{
							Object.DestroyImmediate(blockerSceneObject, allowDestroyingAssets: false);
						}
					}
				}
				foreach (DoorwayProxy unusedDoorway in allTile.UnusedDoorways)
				{
					Doorway doorway3 = componentsInChildren[unusedDoorway.Index];
					component.UnusedDoorways.Add(doorway3);
					foreach (GameObject connectorSceneObject in doorway3.ConnectorSceneObjects)
					{
						if (connectorSceneObject != null)
						{
							Object.DestroyImmediate(connectorSceneObject, allowDestroyingAssets: false);
						}
					}
					if (doorway3.BlockerPrefabWeights.HasAnyViableEntries())
					{
						GameObject gameObject = Object.Instantiate(doorway3.BlockerPrefabWeights.GetRandom(generator.RandomStream));
						gameObject.transform.parent = doorway3.gameObject.transform;
						gameObject.transform.localPosition = Vector3.zero;
						gameObject.transform.localScale = Vector3.one;
						if (!doorway3.AvoidRotatingBlockerPrefab)
						{
							gameObject.transform.localRotation = Quaternion.identity;
						}
					}
				}
			}
			foreach (ProxyDoorwayConnection connection in proxyDungeon.Connections)
			{
				Tile tile = dictionary[connection.A.TileProxy];
				Tile tile2 = dictionary[connection.B.TileProxy];
				Doorway doorway4 = tile.AllDoorways[connection.A.Index];
				Doorway doorway6 = (doorway4.ConnectedDoorway = tile2.AllDoorways[connection.B.Index]);
				doorway6.ConnectedDoorway = doorway4;
				DoorwayConnection item = new DoorwayConnection(doorway4, doorway6);
				connections.Add(item);
				SpawnDoorPrefab(doorway4, doorway6, generator.RandomStream);
			}
		}

		private void SpawnDoorPrefab(Doorway a, Doorway b, RandomStream randomStream)
		{
			if (a.HasDoorPrefabInstance || b.HasDoorPrefabInstance)
			{
				return;
			}
			bool flag = a.ConnectorPrefabWeights.HasAnyViableEntries();
			bool flag2 = b.ConnectorPrefabWeights.HasAnyViableEntries();
			if (!flag && !flag2)
			{
				return;
			}
			Doorway doorway = ((!(flag && flag2)) ? (flag ? a : b) : ((a.DoorPrefabPriority < b.DoorPrefabPriority) ? b : a));
			GameObject random = doorway.ConnectorPrefabWeights.GetRandom(randomStream);
			if (random != null)
			{
				GameObject gameObject = Object.Instantiate(random, doorway.transform);
				gameObject.transform.localPosition = Vector3.zero;
				if (!doorway.AvoidRotatingDoorPrefab)
				{
					gameObject.transform.localRotation = Quaternion.identity;
				}
				doors.Add(gameObject);
				DungeonUtil.AddAndSetupDoorComponent(this, gameObject, doorway);
				a.SetUsedPrefab(gameObject);
				b.SetUsedPrefab(gameObject);
			}
		}

		public void OnDrawGizmos()
		{
			if (DebugRender)
			{
				DebugDraw();
			}
		}

		public void DebugDraw()
		{
			Color red = Color.red;
			Color green = Color.green;
			Color blue = Color.blue;
			Color b = new Color(0.5f, 0f, 0.5f);
			float a = 0.75f;
			foreach (Tile allTile in allTiles)
			{
				Bounds bounds = allTile.Placement.Bounds;
				bounds.size *= 1.01f;
				Color color = (allTile.Placement.IsOnMainPath ? Color.Lerp(red, green, allTile.Placement.NormalizedDepth) : Color.Lerp(blue, b, allTile.Placement.NormalizedDepth));
				color.a = a;
				Gizmos.color = color;
				Gizmos.DrawCube(bounds.center, bounds.size);
			}
		}
	}
}
