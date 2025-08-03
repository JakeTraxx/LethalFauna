using System.Collections.Generic;
using DunGen.Tags;
using UnityEngine;
using UnityEngine.Serialization;

namespace DunGen
{
	[AddComponentMenu("DunGen/Doorway")]
	public class Doorway : MonoBehaviour, ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 1;

		public int DoorPrefabPriority;

		public List<GameObjectWeight> ConnectorPrefabWeights = new List<GameObjectWeight>();

		public List<GameObjectWeight> BlockerPrefabWeights = new List<GameObjectWeight>();

		public bool AvoidRotatingDoorPrefab;

		public bool AvoidRotatingBlockerPrefab;

		[FormerlySerializedAs("AddWhenInUse")]
		public List<GameObject> ConnectorSceneObjects = new List<GameObject>();

		[FormerlySerializedAs("AddWhenNotInUse")]
		public List<GameObject> BlockerSceneObjects = new List<GameObject>();

		public TagContainer Tags = new TagContainer();

		public int? LockID;

		[SerializeField]
		[FormerlySerializedAs("SocketGroup")]
		private DoorwaySocketType socketGroup_obsolete = (DoorwaySocketType)(-1);

		[SerializeField]
		[FormerlySerializedAs("DoorPrefabs")]
		private List<GameObject> doorPrefabs_obsolete = new List<GameObject>();

		[SerializeField]
		[FormerlySerializedAs("BlockerPrefabs")]
		private List<GameObject> blockerPrefabs_obsolete = new List<GameObject>();

		[SerializeField]
		private DoorwaySocket socket;

		[SerializeField]
		private GameObject doorPrefabInstance;

		[SerializeField]
		private Door doorComponent;

		[SerializeField]
		private Tile tile;

		[SerializeField]
		private Doorway connectedDoorway;

		[SerializeField]
		private bool hideConditionalObjects;

		[SerializeField]
		private int fileVersion;

		internal bool placedByGenerator;

		public bool HasSocketAssigned => socket != null;

		public DoorwaySocket Socket
		{
			get
			{
				if (!(socket != null))
				{
					return DunGenSettings.Instance.DefaultSocket;
				}
				return socket;
			}
		}

		public Tile Tile
		{
			get
			{
				return tile;
			}
			internal set
			{
				tile = value;
			}
		}

		public bool IsLocked => LockID.HasValue;

		public bool HasDoorPrefabInstance => doorPrefabInstance != null;

		public GameObject UsedDoorPrefabInstance => doorPrefabInstance;

		public Door DoorComponent => doorComponent;

		public Dungeon Dungeon { get; internal set; }

		public Doorway ConnectedDoorway
		{
			get
			{
				return connectedDoorway;
			}
			internal set
			{
				connectedDoorway = value;
			}
		}

		public bool HideConditionalObjects
		{
			get
			{
				return hideConditionalObjects;
			}
			set
			{
				hideConditionalObjects = value;
				foreach (GameObject connectorSceneObject in ConnectorSceneObjects)
				{
					if (connectorSceneObject != null)
					{
						connectorSceneObject.SetActive(!hideConditionalObjects);
					}
				}
				foreach (GameObject blockerSceneObject in BlockerSceneObjects)
				{
					if (blockerSceneObject != null)
					{
						blockerSceneObject.SetActive(!hideConditionalObjects);
					}
				}
			}
		}

		private void OnDrawGizmos()
		{
			if (!placedByGenerator)
			{
				DebugDraw();
			}
		}

		internal void SetUsedPrefab(GameObject doorPrefab)
		{
			doorPrefabInstance = doorPrefab;
			if (doorPrefab != null)
			{
				doorComponent = doorPrefab.GetComponent<Door>();
			}
		}

		internal void RemoveUsedPrefab()
		{
			if (doorPrefabInstance != null)
			{
				UnityUtil.Destroy(doorPrefabInstance);
			}
			doorPrefabInstance = null;
		}

		internal void DebugDraw()
		{
			Vector2 size = Socket.Size;
			Vector2 vector = size * 0.5f;
			float num = Mathf.Min(size.x, size.y);
			Gizmos.color = EditorConstants.DoorDirectionColour;
			Gizmos.DrawLine(base.transform.position + base.transform.up * vector.y, base.transform.position + base.transform.up * vector.y + base.transform.forward * num);
			Gizmos.color = EditorConstants.DoorUpColour;
			Gizmos.DrawLine(base.transform.position + base.transform.up * vector.y, base.transform.position + base.transform.up * size.y);
			Gizmos.color = EditorConstants.DoorRectColour;
			Vector3 vector2 = base.transform.position - base.transform.right * vector.x + base.transform.up * size.y;
			Vector3 vector3 = base.transform.position + base.transform.right * vector.x + base.transform.up * size.y;
			Vector3 vector4 = base.transform.position - base.transform.right * vector.x;
			Vector3 vector5 = base.transform.position + base.transform.right * vector.x;
			Gizmos.DrawLine(vector2, vector3);
			Gizmos.DrawLine(vector3, vector5);
			Gizmos.DrawLine(vector5, vector4);
			Gizmos.DrawLine(vector4, vector2);
		}

		public void OnBeforeSerialize()
		{
			fileVersion = 1;
		}

		public void OnAfterDeserialize()
		{
			if (fileVersion >= 1)
			{
				return;
			}
			foreach (GameObject item in doorPrefabs_obsolete)
			{
				ConnectorPrefabWeights.Add(new GameObjectWeight(item));
			}
			foreach (GameObject item2 in blockerPrefabs_obsolete)
			{
				BlockerPrefabWeights.Add(new GameObjectWeight(item2));
			}
			doorPrefabs_obsolete.Clear();
			blockerPrefabs_obsolete.Clear();
		}
	}
}
