using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace DunGen.Adapters
{
	[AddComponentMenu("DunGen/NavMesh/Unity NavMesh Adapter")]
	public class UnityNavMeshAdapter : NavMeshAdapter
	{
		public enum RuntimeNavMeshBakeMode
		{
			PreBakedOnly = 0,
			AddIfNoSurfaceExists = 1,
			AlwaysRebake = 2,
			FullDungeonBake = 3
		}

		[Serializable]
		public sealed class NavMeshAgentLinkInfo
		{
			public int AgentTypeID;

			public int AreaTypeID;

			public bool DisableLinkWhenDoorIsClosed = true;
		}

		public RuntimeNavMeshBakeMode BakeMode = RuntimeNavMeshBakeMode.AddIfNoSurfaceExists;

		public LayerMask LayerMask = -1;

		public bool AddNavMeshLinksBetweenRooms = true;

		public List<NavMeshAgentLinkInfo> NavMeshAgentTypes = new List<NavMeshAgentLinkInfo>
		{
			new NavMeshAgentLinkInfo()
		};

		public float NavMeshLinkDistanceFromDoorway = 2.5f;

		public bool AutoGenerateFullRebakeSurfaces = true;

		public List<NavMeshSurface> FullRebakeTargets = new List<NavMeshSurface>();

		private List<NavMeshSurface> addedSurfaces = new List<NavMeshSurface>();

		private List<NavMeshSurface> fullBakeSurfaces = new List<NavMeshSurface>();

		public override void Generate(Dungeon dungeon)
		{
			if (BakeMode == RuntimeNavMeshBakeMode.FullDungeonBake)
			{
				BakeFullDungeon(dungeon);
				return;
			}
			if (BakeMode != 0)
			{
				foreach (Tile allTile in dungeon.AllTiles)
				{
					NavMeshSurface[] componentsInChildren = allTile.gameObject.GetComponentsInChildren<NavMeshSurface>();
					IEnumerable<NavMeshSurface> enumerable = AddMissingSurfaces(allTile, componentsInChildren);
					if (BakeMode == RuntimeNavMeshBakeMode.AlwaysRebake)
					{
						enumerable = enumerable.Concat(componentsInChildren);
					}
					else if (BakeMode == RuntimeNavMeshBakeMode.AddIfNoSurfaceExists)
					{
						IEnumerable<NavMeshSurface> second = componentsInChildren.Where((NavMeshSurface x) => x.navMeshData == null);
						enumerable = enumerable.Concat(second);
					}
					foreach (NavMeshSurface item in enumerable.Distinct())
					{
						item.BuildNavMesh();
					}
				}
			}
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

		private void BakeFullDungeon(Dungeon dungeon)
		{
			if (AutoGenerateFullRebakeSurfaces)
			{
				foreach (NavMeshSurface fullBakeSurface in fullBakeSurfaces)
				{
					if (fullBakeSurface != null)
					{
						fullBakeSurface.RemoveData();
					}
				}
				fullBakeSurfaces.Clear();
				int settingsCount = NavMesh.GetSettingsCount();
				for (int i = 0; i < settingsCount; i++)
				{
					NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
					NavMeshSurface navMeshSurface = (from s in dungeon.gameObject.GetComponents<NavMeshSurface>()
						where s.agentTypeID == settings.agentTypeID
						select s).FirstOrDefault();
					if (navMeshSurface == null)
					{
						navMeshSurface = dungeon.gameObject.AddComponent<NavMeshSurface>();
						navMeshSurface.agentTypeID = settings.agentTypeID;
						navMeshSurface.collectObjects = CollectObjects.Children;
						navMeshSurface.layerMask = LayerMask;
					}
					fullBakeSurfaces.Add(navMeshSurface);
					navMeshSurface.BuildNavMesh();
				}
			}
			else
			{
				foreach (NavMeshSurface fullRebakeTarget in FullRebakeTargets)
				{
					fullRebakeTarget.BuildNavMesh();
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

		private NavMeshSurface[] AddMissingSurfaces(Tile tile, NavMeshSurface[] existingSurfaces)
		{
			addedSurfaces.Clear();
			int settingsCount = NavMesh.GetSettingsCount();
			for (int i = 0; i < settingsCount; i++)
			{
				NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(i);
				if (!existingSurfaces.Where((NavMeshSurface x) => x.agentTypeID == settings.agentTypeID).Any())
				{
					NavMeshSurface navMeshSurface = tile.gameObject.AddComponent<NavMeshSurface>();
					navMeshSurface.agentTypeID = settings.agentTypeID;
					navMeshSurface.collectObjects = CollectObjects.Children;
					navMeshSurface.layerMask = LayerMask;
					addedSurfaces.Add(navMeshSurface);
				}
			}
			return addedSurfaces.ToArray();
		}

		private void AddNavMeshLink(DoorwayConnection connection, NavMeshAgentLinkInfo agentLinkInfo)
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
	}
}
