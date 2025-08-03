using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Culling/Adjacent Room Culling")]
	public class AdjacentRoomCulling : MonoBehaviour
	{
		public delegate void VisibilityChangedDelegate(Tile tile, bool visible);

		public int AdjacentTileDepth = 1;

		public bool CullBehindClosedDoors = true;

		public Transform TargetOverride;

		public bool IncludeDisabledComponents;

		[NonSerialized]
		public Dictionary<Renderer, bool> OverrideRendererVisibilities = new Dictionary<Renderer, bool>();

		[NonSerialized]
		public Dictionary<Light, bool> OverrideLightVisibilities = new Dictionary<Light, bool>();

		protected List<Tile> allTiles;

		protected List<Door> allDoors;

		protected List<Tile> oldVisibleTiles;

		protected List<Tile> visibleTiles;

		protected Dictionary<Tile, bool> tileVisibilities;

		protected Dictionary<Tile, List<Renderer>> tileRenderers;

		protected Dictionary<Tile, List<Light>> lightSources;

		protected Dictionary<Tile, List<ReflectionProbe>> reflectionProbes;

		protected Dictionary<Door, List<Renderer>> doorRenderers;

		private bool dirty;

		private DungeonGenerator generator;

		private Tile currentTile;

		private Queue<Tile> tilesToSearch;

		private List<Tile> searchedTiles;

		public bool Ready { get; protected set; }

		protected Transform targetTransform
		{
			get
			{
				if (!(TargetOverride != null))
				{
					return base.transform;
				}
				return TargetOverride;
			}
		}

		public event VisibilityChangedDelegate TileVisibilityChanged;

		protected virtual void OnEnable()
		{
			RuntimeDungeon runtimeDungeon = UnityEngine.Object.FindObjectOfType<RuntimeDungeon>();
			if (runtimeDungeon != null)
			{
				generator = runtimeDungeon.Generator;
				generator.OnGenerationStatusChanged += OnDungeonGenerationStatusChanged;
				if (generator.Status == GenerationStatus.Complete)
				{
					SetDungeon(generator.CurrentDungeon);
				}
			}
		}

		protected virtual void OnDisable()
		{
			if (generator != null)
			{
				generator.OnGenerationStatusChanged -= OnDungeonGenerationStatusChanged;
			}
			ClearDungeon();
		}

		public virtual void SetDungeon(Dungeon dungeon)
		{
			if (Ready)
			{
				ClearDungeon();
			}
			if (dungeon == null)
			{
				return;
			}
			allTiles = new List<Tile>(dungeon.AllTiles);
			allDoors = new List<Door>(GetAllDoorsInDungeon(dungeon));
			oldVisibleTiles = new List<Tile>(allTiles.Count);
			visibleTiles = new List<Tile>(allTiles.Count);
			tileVisibilities = new Dictionary<Tile, bool>();
			tileRenderers = new Dictionary<Tile, List<Renderer>>();
			lightSources = new Dictionary<Tile, List<Light>>();
			reflectionProbes = new Dictionary<Tile, List<ReflectionProbe>>();
			doorRenderers = new Dictionary<Door, List<Renderer>>();
			UpdateRendererLists();
			foreach (Tile allTile in allTiles)
			{
				SetTileVisibility(allTile, visible: false);
			}
			foreach (Door allDoor in allDoors)
			{
				allDoor.OnDoorStateChanged += OnDoorStateChanged;
				SetDoorVisibility(allDoor, visible: false);
			}
			Ready = true;
			dirty = true;
		}

		public virtual bool IsTileVisible(Tile tile)
		{
			if (tileVisibilities.TryGetValue(tile, out var value))
			{
				return value;
			}
			return false;
		}

		protected IEnumerable<Door> GetAllDoorsInDungeon(Dungeon dungeon)
		{
			foreach (GameObject door in dungeon.Doors)
			{
				if (!(door == null))
				{
					Door component = door.GetComponent<Door>();
					if (component != null)
					{
						yield return component;
					}
				}
			}
		}

		protected virtual void ClearDungeon()
		{
			if (!Ready)
			{
				return;
			}
			foreach (Door allDoor in allDoors)
			{
				SetDoorVisibility(allDoor, visible: true);
				allDoor.OnDoorStateChanged -= OnDoorStateChanged;
			}
			foreach (Tile allTile in allTiles)
			{
				SetTileVisibility(allTile, visible: true);
			}
			Ready = false;
		}

		protected virtual void OnDoorStateChanged(Door door, bool isOpen)
		{
			dirty = true;
		}

		protected virtual void OnDungeonGenerationStatusChanged(DungeonGenerator generator, GenerationStatus status)
		{
			switch (status)
			{
			case GenerationStatus.Complete:
				SetDungeon(generator.CurrentDungeon);
				break;
			case GenerationStatus.Failed:
				ClearDungeon();
				break;
			}
		}

		protected virtual void LateUpdate()
		{
			if (Ready)
			{
				Tile tile = currentTile;
				if (currentTile == null)
				{
					currentTile = FindCurrentTile();
				}
				else if (!currentTile.Bounds.Contains(targetTransform.position))
				{
					currentTile = SearchForNewCurrentTile();
				}
				if (currentTile != tile)
				{
					dirty = true;
				}
				if (dirty)
				{
					RefreshVisibility();
				}
				dirty = false;
			}
		}

		protected virtual void RefreshVisibility()
		{
			List<Tile> list = visibleTiles;
			visibleTiles = oldVisibleTiles;
			oldVisibleTiles = list;
			UpdateVisibleTiles();
			foreach (Tile oldVisibleTile in oldVisibleTiles)
			{
				if (!visibleTiles.Contains(oldVisibleTile))
				{
					SetTileVisibility(oldVisibleTile, visible: false);
				}
			}
			foreach (Tile visibleTile in visibleTiles)
			{
				if (!oldVisibleTiles.Contains(visibleTile))
				{
					SetTileVisibility(visibleTile, visible: true);
				}
			}
			oldVisibleTiles.Clear();
			RefreshDoorVisibilities();
		}

		protected virtual void RefreshDoorVisibilities()
		{
			foreach (Door allDoor in allDoors)
			{
				bool visible = visibleTiles.Contains(allDoor.DoorwayA.Tile) || visibleTiles.Contains(allDoor.DoorwayB.Tile);
				SetDoorVisibility(allDoor, visible);
			}
		}

		protected virtual void SetDoorVisibility(Door door, bool visible)
		{
			if (!doorRenderers.TryGetValue(door, out var value))
			{
				return;
			}
			for (int num = value.Count - 1; num >= 0; num--)
			{
				Renderer renderer = value[num];
				bool value2;
				if (renderer == null)
				{
					value.RemoveAt(num);
				}
				else if (OverrideRendererVisibilities.TryGetValue(renderer, out value2))
				{
					renderer.enabled = value2;
				}
				else
				{
					renderer.enabled = visible;
				}
			}
		}

		protected virtual void UpdateVisibleTiles()
		{
			visibleTiles.Clear();
			if (currentTile != null)
			{
				visibleTiles.Add(currentTile);
			}
			int num = 0;
			for (int i = 0; i < AdjacentTileDepth; i++)
			{
				int count = visibleTiles.Count;
				for (int j = num; j < count; j++)
				{
					foreach (Doorway usedDoorway in visibleTiles[j].UsedDoorways)
					{
						Tile tile = usedDoorway.ConnectedDoorway.Tile;
						if (visibleTiles.Contains(tile))
						{
							continue;
						}
						if (CullBehindClosedDoors)
						{
							Door doorComponent = usedDoorway.DoorComponent;
							if (doorComponent != null && doorComponent.ShouldCullBehind)
							{
								continue;
							}
						}
						visibleTiles.Add(tile);
					}
				}
				num = count;
			}
		}

		protected virtual void SetTileVisibility(Tile tile, bool visible)
		{
			tileVisibilities[tile] = visible;
			if (tileRenderers.TryGetValue(tile, out var value))
			{
				for (int num = value.Count - 1; num >= 0; num--)
				{
					Renderer renderer = value[num];
					bool value2;
					if (renderer == null)
					{
						value.RemoveAt(num);
					}
					else if (OverrideRendererVisibilities.TryGetValue(renderer, out value2))
					{
						renderer.enabled = value2;
					}
					else
					{
						renderer.enabled = visible;
					}
				}
			}
			if (lightSources.TryGetValue(tile, out var value3))
			{
				for (int num2 = value3.Count - 1; num2 >= 0; num2--)
				{
					Light light = value3[num2];
					bool value4;
					if (light == null)
					{
						value3.RemoveAt(num2);
					}
					else if (OverrideLightVisibilities.TryGetValue(light, out value4))
					{
						light.enabled = value4;
					}
					else
					{
						light.enabled = visible;
					}
				}
			}
			if (reflectionProbes.TryGetValue(tile, out var value5))
			{
				for (int num3 = value5.Count - 1; num3 >= 0; num3--)
				{
					ReflectionProbe reflectionProbe = value5[num3];
					if (reflectionProbe == null)
					{
						value5.RemoveAt(num3);
					}
					else
					{
						reflectionProbe.enabled = visible;
					}
				}
			}
			if (this.TileVisibilityChanged != null)
			{
				this.TileVisibilityChanged(tile, visible);
			}
		}

		public virtual void UpdateRendererLists()
		{
			foreach (Tile allTile in allTiles)
			{
				if (!tileRenderers.TryGetValue(allTile, out var value))
				{
					value = (tileRenderers[allTile] = new List<Renderer>());
				}
				Renderer[] componentsInChildren = allTile.GetComponentsInChildren<Renderer>();
				foreach (Renderer renderer in componentsInChildren)
				{
					if (IncludeDisabledComponents || (renderer.enabled && renderer.gameObject.activeInHierarchy))
					{
						value.Add(renderer);
					}
				}
				if (!lightSources.TryGetValue(allTile, out var value2))
				{
					value2 = (lightSources[allTile] = new List<Light>());
				}
				Light[] componentsInChildren2 = allTile.GetComponentsInChildren<Light>();
				foreach (Light light in componentsInChildren2)
				{
					if (IncludeDisabledComponents || (light.enabled && light.gameObject.activeInHierarchy))
					{
						value2.Add(light);
					}
				}
				if (!reflectionProbes.TryGetValue(allTile, out var value3))
				{
					value3 = (reflectionProbes[allTile] = new List<ReflectionProbe>());
				}
				ReflectionProbe[] componentsInChildren3 = allTile.GetComponentsInChildren<ReflectionProbe>();
				foreach (ReflectionProbe reflectionProbe in componentsInChildren3)
				{
					if (IncludeDisabledComponents || (reflectionProbe.enabled && reflectionProbe.gameObject.activeInHierarchy))
					{
						value3.Add(reflectionProbe);
					}
				}
			}
			foreach (Door allDoor in allDoors)
			{
				List<Renderer> list4 = new List<Renderer>();
				doorRenderers[allDoor] = list4;
				Renderer[] componentsInChildren = allDoor.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer renderer2 in componentsInChildren)
				{
					if (IncludeDisabledComponents || (renderer2.enabled && renderer2.gameObject.activeInHierarchy))
					{
						list4.Add(renderer2);
					}
				}
			}
		}

		protected Tile FindCurrentTile()
		{
			Dungeon dungeon = UnityEngine.Object.FindObjectOfType<Dungeon>();
			if (dungeon == null)
			{
				return null;
			}
			foreach (Tile allTile in dungeon.AllTiles)
			{
				if (allTile.Bounds.Contains(targetTransform.position))
				{
					return allTile;
				}
			}
			return null;
		}

		protected Tile SearchForNewCurrentTile()
		{
			if (tilesToSearch == null)
			{
				tilesToSearch = new Queue<Tile>();
			}
			if (searchedTiles == null)
			{
				searchedTiles = new List<Tile>();
			}
			foreach (Doorway usedDoorway in currentTile.UsedDoorways)
			{
				Tile tile = usedDoorway.ConnectedDoorway.Tile;
				if (!tilesToSearch.Contains(tile))
				{
					tilesToSearch.Enqueue(tile);
				}
			}
			while (tilesToSearch.Count > 0)
			{
				Tile tile2 = tilesToSearch.Dequeue();
				if (tile2.Bounds.Contains(targetTransform.position))
				{
					tilesToSearch.Clear();
					searchedTiles.Clear();
					return tile2;
				}
				searchedTiles.Add(tile2);
				foreach (Doorway usedDoorway2 in tile2.UsedDoorways)
				{
					Tile tile3 = usedDoorway2.ConnectedDoorway.Tile;
					if (!tilesToSearch.Contains(tile3) && !searchedTiles.Contains(tile3))
					{
						tilesToSearch.Enqueue(tile3);
					}
				}
			}
			searchedTiles.Clear();
			return null;
		}
	}
}
