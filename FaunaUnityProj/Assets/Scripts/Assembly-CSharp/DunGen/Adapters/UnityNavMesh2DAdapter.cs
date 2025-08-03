using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

namespace DunGen.Adapters
{
	[AddComponentMenu("DunGen/NavMesh/Unity NavMesh Adapter (2D)")]
	public class UnityNavMesh2DAdapter : NavMeshAdapter
	{
		[Serializable]
		public sealed class NavMeshAgentLinkInfo
		{
			public int AgentTypeID;

			public int AreaTypeID;

			public bool DisableLinkWhenDoorIsClosed = true;
		}

		private static Quaternion rotation = Quaternion.Euler(-90f, 0f, 0f);

		public bool AddNavMeshLinksBetweenRooms = true;

		public List<NavMeshAgentLinkInfo> NavMeshAgentTypes = new List<NavMeshAgentLinkInfo>
		{
			new NavMeshAgentLinkInfo()
		};

		public float NavMeshLinkDistanceFromDoorway = 1f;

		[SerializeField]
		private int agentTypeID;

		[SerializeField]
		private bool overrideTileSize;

		[SerializeField]
		private int tileSize = 256;

		[SerializeField]
		private bool overrideVoxelSize;

		[SerializeField]
		private float voxelSize;

		[SerializeField]
		private NavMeshData navMeshData;

		[SerializeField]
		private LayerMask layerMask = -1;

		[SerializeField]
		private int defaultArea;

		[SerializeField]
		private bool ignoreNavMeshAgent = true;

		[SerializeField]
		private bool ignoreNavMeshObstacle = true;

		[SerializeField]
		private int unwalkableArea = 1;

		private NavMeshDataInstance m_NavMeshDataInstance;

		private Dictionary<Sprite, Mesh> cachedSpriteMeshes = new Dictionary<Sprite, Mesh>();

		public int AgentTypeID
		{
			get
			{
				return agentTypeID;
			}
			set
			{
				agentTypeID = value;
			}
		}

		public bool OverrideTileSize
		{
			get
			{
				return overrideTileSize;
			}
			set
			{
				overrideTileSize = value;
			}
		}

		public int TileSize
		{
			get
			{
				return tileSize;
			}
			set
			{
				tileSize = value;
			}
		}

		public bool OverrideVoxelSize
		{
			get
			{
				return overrideVoxelSize;
			}
			set
			{
				overrideVoxelSize = value;
			}
		}

		public float VoxelSize
		{
			get
			{
				return voxelSize;
			}
			set
			{
				voxelSize = value;
			}
		}

		public NavMeshData NavMeshData
		{
			get
			{
				return navMeshData;
			}
			set
			{
				navMeshData = value;
			}
		}

		public LayerMask LayerMask
		{
			get
			{
				return layerMask;
			}
			set
			{
				layerMask = value;
			}
		}

		public int DefaultArea
		{
			get
			{
				return defaultArea;
			}
			set
			{
				defaultArea = value;
			}
		}

		public bool IgnoreNavMeshAgent
		{
			get
			{
				return ignoreNavMeshAgent;
			}
			set
			{
				ignoreNavMeshAgent = value;
			}
		}

		public bool IgnoreNavMeshObstacle
		{
			get
			{
				return ignoreNavMeshObstacle;
			}
			set
			{
				ignoreNavMeshObstacle = value;
			}
		}

		public int UnwalkableArea
		{
			get
			{
				return unwalkableArea;
			}
			set
			{
				unwalkableArea = value;
			}
		}

		public override void Generate(Dungeon dungeon)
		{
			BakeNavMesh(dungeon);
			if (AddNavMeshLinksBetweenRooms)
			{
				foreach (DoorwayConnection connection in dungeon.Connections)
				{
					foreach (NavMeshAgentLinkInfo navMeshAgentType in NavMeshAgentTypes)
					{
						AddNavMeshLink(connection, navMeshAgentType);
					}
				}
			}
			if (OnProgress != null)
			{
				OnProgress(new NavMeshGenerationProgress
				{
					Description = "Done",
					Percentage = 1f
				});
			}
		}

		protected void AddData()
		{
			if (!m_NavMeshDataInstance.valid && navMeshData != null)
			{
				m_NavMeshDataInstance = NavMesh.AddNavMeshData(navMeshData, base.transform.position, rotation);
				m_NavMeshDataInstance.owner = this;
			}
		}

		protected void RemoveData()
		{
			m_NavMeshDataInstance.Remove();
			m_NavMeshDataInstance = default(NavMeshDataInstance);
			foreach (KeyValuePair<Sprite, Mesh> cachedSpriteMesh in cachedSpriteMeshes)
			{
				UnityEngine.Object.DestroyImmediate(cachedSpriteMesh.Value);
			}
			cachedSpriteMeshes.Clear();
		}

		protected virtual void BakeNavMesh(Dungeon dungeon)
		{
			List<NavMeshBuildSource> sources = CollectSources();
			Bounds localBounds = CalculateWorldBounds(sources);
			NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(GetBuildSettings(), sources, localBounds, base.transform.position, rotation);
			if (navMeshData != null)
			{
				navMeshData.name = base.gameObject.name;
				RemoveData();
				this.navMeshData = navMeshData;
				if (base.isActiveAndEnabled)
				{
					AddData();
				}
			}
			if (OnProgress != null)
			{
				OnProgress(new NavMeshGenerationProgress
				{
					Description = "Done",
					Percentage = 1f
				});
			}
		}

		protected void AppendModifierVolumes(ref List<NavMeshBuildSource> sources)
		{
			List<NavMeshModifierVolume> list = new List<NavMeshModifierVolume>(GetComponentsInChildren<NavMeshModifierVolume>());
			list.RemoveAll((NavMeshModifierVolume x) => !x.isActiveAndEnabled);
			foreach (NavMeshModifierVolume item2 in list)
			{
				if (((int)layerMask & (1 << item2.gameObject.layer)) != 0 && item2.AffectsAgentType(agentTypeID))
				{
					Vector3 pos = item2.transform.TransformPoint(item2.center);
					Vector3 lossyScale = item2.transform.lossyScale;
					Vector3 size = new Vector3(item2.size.x * Mathf.Abs(lossyScale.x), item2.size.y * Mathf.Abs(lossyScale.y), item2.size.z * Mathf.Abs(lossyScale.z));
					NavMeshBuildSource item = default(NavMeshBuildSource);
					item.shape = NavMeshBuildSourceShape.ModifierBox;
					item.transform = Matrix4x4.TRS(pos, item2.transform.rotation, Vector3.one);
					item.size = size;
					item.area = item2.area;
					sources.Add(item);
				}
			}
		}

		protected virtual List<NavMeshBuildSource> CollectSources()
		{
			List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
			List<NavMeshBuildMarkup> list = new List<NavMeshBuildMarkup>();
			List<NavMeshModifier> list2 = new List<NavMeshModifier>(GetComponentsInChildren<NavMeshModifier>());
			list2.RemoveAll((NavMeshModifier x) => !x.isActiveAndEnabled);
			foreach (NavMeshModifier item3 in list2)
			{
				if (((int)layerMask & (1 << item3.gameObject.layer)) != 0 && item3.AffectsAgentType(agentTypeID))
				{
					NavMeshBuildMarkup item = default(NavMeshBuildMarkup);
					item.root = item3.transform;
					item.overrideArea = item3.overrideArea;
					item.area = item3.area;
					item.ignoreFromBuild = item3.ignoreFromBuild;
					list.Add(item);
				}
			}
			SpriteRenderer[] array = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
			foreach (SpriteRenderer spriteRenderer in array)
			{
				Sprite sprite = spriteRenderer.sprite;
				Mesh mesh = GetMesh(sprite);
				if (mesh != null)
				{
					int area = ((((int)layerMask & (1 << spriteRenderer.gameObject.layer)) == 0) ? unwalkableArea : 0);
					sources.Add(new NavMeshBuildSource
					{
						transform = spriteRenderer.transform.localToWorldMatrix,
						size = mesh.bounds.extents * 2f,
						shape = NavMeshBuildSourceShape.Mesh,
						area = area,
						sourceObject = mesh,
						component = spriteRenderer
					});
				}
			}
			NavMeshBuildSource navMeshBuildSource = default(NavMeshBuildSource);
			navMeshBuildSource.shape = NavMeshBuildSourceShape.Mesh;
			navMeshBuildSource.area = 0;
			NavMeshBuildSource item2 = navMeshBuildSource;
			Tilemap[] array2 = UnityEngine.Object.FindObjectsOfType<Tilemap>();
			foreach (Tilemap tilemap in array2)
			{
				for (int j = tilemap.cellBounds.xMin; j < tilemap.cellBounds.xMax; j++)
				{
					for (int k = tilemap.cellBounds.yMin; k < tilemap.cellBounds.yMax; k++)
					{
						Vector3Int position = new Vector3Int(j, k, 0);
						if (tilemap.HasTile(position))
						{
							UnityEngine.Tilemaps.Tile tile = tilemap.GetTile<UnityEngine.Tilemaps.Tile>(position);
							Mesh mesh2 = GetMesh(tilemap.GetSprite(position));
							if (mesh2 != null)
							{
								item2.transform = Matrix4x4.TRS(tilemap.GetCellCenterWorld(position) - tilemap.layoutGrid.cellGap, tilemap.transform.rotation, tilemap.transform.lossyScale) * tilemap.orientationMatrix * tilemap.GetTransformMatrix(position);
								item2.sourceObject = mesh2;
								item2.component = tilemap;
								item2.area = ((tile.colliderType != 0) ? unwalkableArea : 0);
								sources.Add(item2);
							}
						}
					}
				}
			}
			if (ignoreNavMeshAgent)
			{
				sources.RemoveAll((NavMeshBuildSource x) => x.component != null && x.component.gameObject.GetComponent<NavMeshAgent>() != null);
			}
			if (ignoreNavMeshObstacle)
			{
				sources.RemoveAll((NavMeshBuildSource x) => x.component != null && x.component.gameObject.GetComponent<NavMeshObstacle>() != null);
			}
			AppendModifierVolumes(ref sources);
			return sources;
		}

		protected Mesh GetMesh(Sprite sprite)
		{
			if (sprite == null)
			{
				return null;
			}
			if (!cachedSpriteMeshes.TryGetValue(sprite, out var value))
			{
				value = new Mesh
				{
					vertices = sprite.vertices.Select((Vector2 v) => new Vector3(v.x, v.y, 0f)).ToArray(),
					triangles = ((IEnumerable<ushort>)sprite.triangles).Select((Func<ushort, int>)((ushort i) => i)).ToArray()
				};
				value.RecalculateBounds();
				value.RecalculateNormals();
				value.RecalculateTangents();
				cachedSpriteMeshes[sprite] = value;
			}
			return value;
		}

		protected void AddNavMeshLink(DoorwayConnection connection, NavMeshAgentLinkInfo agentLinkInfo)
		{
			GameObject gameObject = connection.A.gameObject;
			NavMeshBuildSettings settingsByID = NavMesh.GetSettingsByID(agentLinkInfo.AgentTypeID);
			float width = Mathf.Max(connection.A.Socket.Size.x - settingsByID.agentRadius * 2f, 0.01f);
			NavMeshLink link = gameObject.AddComponent<NavMeshLink>();
			link.agentTypeID = agentLinkInfo.AgentTypeID;
			link.bidirectional = true;
			link.area = agentLinkInfo.AreaTypeID;
			link.startPoint = new Vector3(0f, 0f, 0f - NavMeshLinkDistanceFromDoorway);
			link.endPoint = new Vector3(0f, 0f, NavMeshLinkDistanceFromDoorway);
			link.width = width;
			if (!agentLinkInfo.DisableLinkWhenDoorIsClosed)
			{
				return;
			}
			GameObject gameObject2 = ((connection.A.UsedDoorPrefabInstance != null) ? connection.A.UsedDoorPrefabInstance : ((connection.B.UsedDoorPrefabInstance != null) ? connection.B.UsedDoorPrefabInstance : null));
			if (!(gameObject2 != null))
			{
				return;
			}
			Door component = gameObject2.GetComponent<Door>();
			link.enabled = component.IsOpen;
			if (component != null)
			{
				component.OnDoorStateChanged += delegate(Door d, bool o)
				{
					link.enabled = o;
				};
			}
		}

		public NavMeshBuildSettings GetBuildSettings()
		{
			NavMeshBuildSettings settingsByID = NavMesh.GetSettingsByID(agentTypeID);
			if (settingsByID.agentTypeID == -1)
			{
				Debug.LogWarning("No build settings for agent type ID " + AgentTypeID, this);
				settingsByID.agentTypeID = agentTypeID;
			}
			if (OverrideTileSize)
			{
				settingsByID.overrideTileSize = true;
				settingsByID.tileSize = TileSize;
			}
			if (OverrideVoxelSize)
			{
				settingsByID.overrideVoxelSize = true;
				settingsByID.voxelSize = VoxelSize;
			}
			return settingsByID;
		}

		protected Bounds CalculateWorldBounds(List<NavMeshBuildSource> sources)
		{
			Matrix4x4 inverse = Matrix4x4.TRS(base.transform.position, rotation, Vector3.one).inverse;
			Bounds result = default(Bounds);
			foreach (NavMeshBuildSource source in sources)
			{
				switch (source.shape)
				{
				case NavMeshBuildSourceShape.Mesh:
				{
					Mesh mesh = source.sourceObject as Mesh;
					result.Encapsulate(GetWorldBounds(inverse * source.transform, mesh.bounds));
					break;
				}
				case NavMeshBuildSourceShape.Terrain:
				{
					TerrainData terrainData = source.sourceObject as TerrainData;
					result.Encapsulate(GetWorldBounds(inverse * source.transform, new Bounds(0.5f * terrainData.size, terrainData.size)));
					break;
				}
				case NavMeshBuildSourceShape.Box:
				case NavMeshBuildSourceShape.Sphere:
				case NavMeshBuildSourceShape.Capsule:
				case NavMeshBuildSourceShape.ModifierBox:
					result.Encapsulate(GetWorldBounds(inverse * source.transform, new Bounds(Vector3.zero, source.size)));
					break;
				}
			}
			result.Expand(0.1f);
			return result;
		}

		private static Vector3 Abs(Vector3 v)
		{
			return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
		}

		private static Bounds GetWorldBounds(Matrix4x4 mat, Bounds bounds)
		{
			Vector3 vector = Abs(mat.MultiplyVector(Vector3.right));
			Vector3 vector2 = Abs(mat.MultiplyVector(Vector3.up));
			Vector3 vector3 = Abs(mat.MultiplyVector(Vector3.forward));
			Vector3 center = mat.MultiplyPoint(bounds.center);
			Vector3 size = vector * bounds.size.x + vector2 * bounds.size.y + vector3 * bounds.size.z;
			return new Bounds(center, size);
		}
	}
}
