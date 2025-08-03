using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DunGen
{
	[AddComponentMenu("DunGen/Culling/Adjacent Room Culling (Multi-Camera)")]
	public class BasicRoomCullingCamera : MonoBehaviour
	{
		protected struct RendererData
		{
			public Renderer Renderer;

			public bool Enabled;

			public RendererData(Renderer renderer, bool enabled)
			{
				Renderer = renderer;
				Enabled = enabled;
			}
		}

		protected struct LightData
		{
			public Light Light;

			public bool Enabled;

			public LightData(Light light, bool enabled)
			{
				Light = light;
				Enabled = enabled;
			}
		}

		protected struct ReflectionProbeData
		{
			public ReflectionProbe Probe;

			public bool Enabled;

			public ReflectionProbeData(ReflectionProbe probe, bool enabled)
			{
				Probe = probe;
				Enabled = enabled;
			}
		}

		public int AdjacentTileDepth = 1;

		public bool CullBehindClosedDoors = true;

		public Transform TargetOverride;

		public bool CullInEditor;

		public bool CullLights = true;

		protected bool isCulling;

		protected bool isDirty;

		protected DungeonGenerator generator;

		protected Tile currentTile;

		protected List<Tile> allTiles;

		protected List<Door> allDoors;

		protected List<Tile> visibleTiles;

		protected Dictionary<Tile, List<RendererData>> rendererVisibilities = new Dictionary<Tile, List<RendererData>>();

		protected Dictionary<Tile, List<LightData>> lightVisibilities = new Dictionary<Tile, List<LightData>>();

		protected Dictionary<Tile, List<ReflectionProbeData>> reflectionProbeVisibilities = new Dictionary<Tile, List<ReflectionProbeData>>();

		protected Dictionary<Door, List<RendererData>> doorRendererVisibilities = new Dictionary<Door, List<RendererData>>();

		public bool IsReady { get; protected set; }

		protected virtual void Awake()
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

		protected virtual void OnDestroy()
		{
			if (generator != null)
			{
				generator.OnGenerationStatusChanged -= OnDungeonGenerationStatusChanged;
			}
		}

		protected virtual void OnEnable()
		{
			if (RenderPipelineManager.currentPipeline != null)
			{
				RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
				RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
			}
			else
			{
				Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(EnableCulling));
				Camera.onPostRender = (Camera.CameraCallback)Delegate.Combine(Camera.onPostRender, new Camera.CameraCallback(DisableCulling));
			}
		}

		protected virtual void OnDisable()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
			RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(EnableCulling));
			Camera.onPostRender = (Camera.CameraCallback)Delegate.Remove(Camera.onPostRender, new Camera.CameraCallback(DisableCulling));
		}

		private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			EnableCulling(camera);
		}

		private void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
		{
			DisableCulling(camera);
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

		protected virtual void EnableCulling(Camera camera)
		{
			SetCullingEnabled(camera, enabled: true);
		}

		protected virtual void DisableCulling(Camera camera)
		{
			SetCullingEnabled(camera, enabled: false);
		}

		protected void SetCullingEnabled(Camera camera, bool enabled)
		{
			if (IsReady && camera.gameObject == base.gameObject)
			{
				SetIsCulling(enabled);
			}
		}

		protected virtual void LateUpdate()
		{
			if (!IsReady)
			{
				return;
			}
			Transform transform = ((TargetOverride != null) ? TargetOverride : base.transform);
			if (currentTile == null || !currentTile.Bounds.Contains(transform.position))
			{
				foreach (Tile allTile in allTiles)
				{
					if (!(allTile == null) && allTile.Bounds.Contains(transform.position))
					{
						currentTile = allTile;
						break;
					}
				}
				isDirty = true;
			}
			if (!isDirty)
			{
				return;
			}
			UpdateCulling();
			foreach (Tile allTile2 in allTiles)
			{
				if (!visibleTiles.Contains(allTile2))
				{
					UpdateRendererList(allTile2);
				}
			}
		}

		protected void UpdateRendererList(Tile tile)
		{
			if (!rendererVisibilities.TryGetValue(tile, out var value))
			{
				value = (rendererVisibilities[tile] = new List<RendererData>());
			}
			else
			{
				value.Clear();
			}
			Renderer[] componentsInChildren = tile.GetComponentsInChildren<Renderer>();
			foreach (Renderer renderer in componentsInChildren)
			{
				value.Add(new RendererData(renderer, renderer.enabled));
			}
			if (CullLights)
			{
				if (!lightVisibilities.TryGetValue(tile, out var value2))
				{
					value2 = (lightVisibilities[tile] = new List<LightData>());
				}
				else
				{
					value2.Clear();
				}
				Light[] componentsInChildren2 = tile.GetComponentsInChildren<Light>();
				foreach (Light light in componentsInChildren2)
				{
					value2.Add(new LightData(light, light.enabled));
				}
			}
			if (!reflectionProbeVisibilities.TryGetValue(tile, out var value3))
			{
				value3 = (reflectionProbeVisibilities[tile] = new List<ReflectionProbeData>());
			}
			else
			{
				value3.Clear();
			}
			ReflectionProbe[] componentsInChildren3 = tile.GetComponentsInChildren<ReflectionProbe>();
			foreach (ReflectionProbe reflectionProbe in componentsInChildren3)
			{
				value3.Add(new ReflectionProbeData(reflectionProbe, reflectionProbe.enabled));
			}
		}

		protected void SetIsCulling(bool isCulling)
		{
			this.isCulling = isCulling;
			for (int i = 0; i < allTiles.Count; i++)
			{
				Tile tile = allTiles[i];
				if (visibleTiles.Contains(tile))
				{
					continue;
				}
				if (rendererVisibilities.TryGetValue(tile, out var value))
				{
					foreach (RendererData item in value)
					{
						item.Renderer.enabled = !isCulling && item.Enabled;
					}
				}
				if (CullLights && lightVisibilities.TryGetValue(tile, out var value2))
				{
					foreach (LightData item2 in value2)
					{
						item2.Light.enabled = !isCulling && item2.Enabled;
					}
				}
				if (!reflectionProbeVisibilities.TryGetValue(tile, out var value3))
				{
					continue;
				}
				foreach (ReflectionProbeData item3 in value3)
				{
					item3.Probe.enabled = !isCulling && item3.Enabled;
				}
			}
			foreach (Door allDoor in allDoors)
			{
				bool flag = visibleTiles.Contains(allDoor.DoorwayA.Tile) || visibleTiles.Contains(allDoor.DoorwayB.Tile);
				if (!doorRendererVisibilities.TryGetValue(allDoor, out var value4))
				{
					continue;
				}
				foreach (RendererData item4 in value4)
				{
					item4.Renderer.enabled = (isCulling ? flag : item4.Enabled);
				}
			}
		}

		protected void UpdateCulling()
		{
			isDirty = false;
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

		public void SetDungeon(Dungeon dungeon)
		{
			if (IsReady)
			{
				ClearDungeon();
			}
			if (dungeon == null)
			{
				return;
			}
			allTiles = new List<Tile>(dungeon.AllTiles);
			allDoors = new List<Door>(GetAllDoorsInDungeon(dungeon));
			visibleTiles = new List<Tile>(allTiles.Count);
			doorRendererVisibilities.Clear();
			foreach (Door allDoor in allDoors)
			{
				List<RendererData> list = new List<RendererData>();
				doorRendererVisibilities[allDoor] = list;
				Renderer[] componentsInChildren = allDoor.GetComponentsInChildren<Renderer>(includeInactive: true);
				foreach (Renderer renderer in componentsInChildren)
				{
					list.Add(new RendererData(renderer, renderer.enabled));
				}
				allDoor.OnDoorStateChanged += OnDoorStateChanged;
			}
			IsReady = true;
			isDirty = true;
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
			foreach (Door allDoor in allDoors)
			{
				allDoor.OnDoorStateChanged -= OnDoorStateChanged;
			}
			IsReady = false;
		}

		protected virtual void OnDoorStateChanged(Door door, bool isOpen)
		{
			isDirty = true;
		}
	}
}
