using System;
using DunGen.Graph;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	public sealed class TilePlacementData
	{
		[SerializeField]
		private int pathDepth;

		[SerializeField]
		private float normalizedPathDepth;

		[SerializeField]
		private int branchDepth;

		[SerializeField]
		private float normalizedBranchDepth;

		[SerializeField]
		private bool isOnMainPath;

		[SerializeField]
		private Bounds localBounds;

		[SerializeField]
		private GraphNode graphNode;

		[SerializeField]
		private GraphLine graphLine;

		[SerializeField]
		private DungeonArchetype archetype;

		[SerializeField]
		private TileSet tileSet;

		[SerializeField]
		private Vector3 position = Vector3.zero;

		[SerializeField]
		private Quaternion rotation = Quaternion.identity;

		public int PathDepth
		{
			get
			{
				return pathDepth;
			}
			internal set
			{
				pathDepth = value;
			}
		}

		public float NormalizedPathDepth
		{
			get
			{
				return normalizedPathDepth;
			}
			internal set
			{
				normalizedPathDepth = value;
			}
		}

		public int BranchDepth
		{
			get
			{
				return branchDepth;
			}
			internal set
			{
				branchDepth = value;
			}
		}

		public float NormalizedBranchDepth
		{
			get
			{
				return normalizedBranchDepth;
			}
			internal set
			{
				normalizedBranchDepth = value;
			}
		}

		public bool IsOnMainPath
		{
			get
			{
				return isOnMainPath;
			}
			internal set
			{
				isOnMainPath = value;
			}
		}

		public Bounds Bounds { get; private set; }

		public Bounds LocalBounds
		{
			get
			{
				return localBounds;
			}
			internal set
			{
				localBounds = value;
				RecalculateTransform();
			}
		}

		public GraphNode GraphNode
		{
			get
			{
				return graphNode;
			}
			internal set
			{
				graphNode = value;
			}
		}

		public GraphLine GraphLine
		{
			get
			{
				return graphLine;
			}
			internal set
			{
				graphLine = value;
			}
		}

		public DungeonArchetype Archetype
		{
			get
			{
				return archetype;
			}
			internal set
			{
				archetype = value;
			}
		}

		public TileSet TileSet
		{
			get
			{
				return tileSet;
			}
			internal set
			{
				tileSet = value;
			}
		}

		public Vector3 Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
				RecalculateTransform();
			}
		}

		public Quaternion Rotation
		{
			get
			{
				return rotation;
			}
			set
			{
				rotation = value;
				RecalculateTransform();
			}
		}

		public Matrix4x4 Transform { get; private set; }

		public int Depth
		{
			get
			{
				if (!isOnMainPath)
				{
					return branchDepth;
				}
				return pathDepth;
			}
		}

		public float NormalizedDepth
		{
			get
			{
				if (!isOnMainPath)
				{
					return normalizedBranchDepth;
				}
				return normalizedPathDepth;
			}
		}

		public InjectedTile InjectionData { get; set; }

		public TilePlacementData()
		{
			RecalculateTransform();
		}

		public TilePlacementData(TilePlacementData copy)
		{
			PathDepth = copy.PathDepth;
			NormalizedPathDepth = copy.NormalizedPathDepth;
			BranchDepth = copy.BranchDepth;
			NormalizedBranchDepth = copy.NormalizedDepth;
			IsOnMainPath = copy.IsOnMainPath;
			LocalBounds = copy.LocalBounds;
			Transform = copy.Transform;
			GraphNode = copy.GraphNode;
			GraphLine = copy.GraphLine;
			Archetype = copy.Archetype;
			TileSet = copy.TileSet;
			InjectionData = copy.InjectionData;
			position = copy.position;
			rotation = copy.rotation;
			RecalculateTransform();
		}

		private void RecalculateTransform()
		{
			Transform = Matrix4x4.TRS(position, rotation, Vector3.one);
			Vector3 vector = Transform.MultiplyPoint(localBounds.min);
			Vector3 vector2 = Transform.MultiplyPoint(localBounds.max) - vector;
			Vector3 center = vector + vector2 / 2f;
			vector2.x = Mathf.Abs(vector2.x);
			vector2.y = Mathf.Abs(vector2.y);
			vector2.z = Mathf.Abs(vector2.z);
			Bounds = new Bounds(center, vector2);
		}
	}
}
