using System.Collections.Generic;
using System.Linq;
using DunGen.Tags;
using UnityEngine;
using UnityEngine.Serialization;

namespace DunGen
{
	[AddComponentMenu("DunGen/Tile")]
	public class Tile : MonoBehaviour, ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 1;

		[SerializeField]
		[FormerlySerializedAs("AllowImmediateRepeats")]
		private bool allowImmediateRepeats = true;

		public bool AllowRotation = true;

		public TileRepeatMode RepeatMode;

		public bool OverrideAutomaticTileBounds;

		public Bounds TileBoundsOverride = new Bounds(Vector3.zero, Vector3.one);

		public Doorway Entrance;

		public Doorway Exit;

		public bool OverrideConnectionChance;

		public float ConnectionChance;

		public TagContainer Tags = new TagContainer();

		public List<Doorway> AllDoorways = new List<Doorway>();

		public List<Doorway> UsedDoorways = new List<Doorway>();

		public List<Doorway> UnusedDoorways = new List<Doorway>();

		[SerializeField]
		private TilePlacementData placement;

		[SerializeField]
		private int fileVersion;

		[HideInInspector]
		public Bounds Bounds => base.transform.TransformBounds(Placement.LocalBounds);

		public TilePlacementData Placement
		{
			get
			{
				return placement;
			}
			internal set
			{
				placement = value;
			}
		}

		public Dungeon Dungeon { get; internal set; }

		internal void AddTriggerVolume()
		{
			BoxCollider boxCollider = base.gameObject.AddComponent<BoxCollider>();
			boxCollider.center = Placement.LocalBounds.center;
			boxCollider.size = Placement.LocalBounds.size;
			boxCollider.isTrigger = true;
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!(other == null))
			{
				DungenCharacter component = other.gameObject.GetComponent<DungenCharacter>();
				if (component != null)
				{
					component.OnTileEntered(this);
				}
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (!(other == null))
			{
				DungenCharacter component = other.gameObject.GetComponent<DungenCharacter>();
				if (component != null)
				{
					component.OnTileExited(this);
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Bounds? bounds = null;
			if (OverrideAutomaticTileBounds)
			{
				bounds = base.transform.TransformBounds(TileBoundsOverride);
			}
			else if (placement != null)
			{
				bounds = Bounds;
			}
			if (bounds.HasValue)
			{
				Gizmos.DrawWireCube(bounds.Value.center, bounds.Value.size);
			}
		}

		public IEnumerable<Tile> GetAdjactedTiles()
		{
			return UsedDoorways.Select((Doorway x) => x.ConnectedDoorway.Tile).Distinct();
		}

		public bool IsAdjacentTo(Tile other)
		{
			foreach (Doorway usedDoorway in UsedDoorways)
			{
				if (usedDoorway.ConnectedDoorway.Tile == other)
				{
					return true;
				}
			}
			return false;
		}

		public Doorway GetEntranceDoorway()
		{
			foreach (Doorway usedDoorway in UsedDoorways)
			{
				Tile tile = usedDoorway.ConnectedDoorway.Tile;
				if (Placement.IsOnMainPath)
				{
					if (tile.Placement.IsOnMainPath && Placement.PathDepth > tile.Placement.PathDepth)
					{
						return usedDoorway;
					}
				}
				else if (tile.Placement.IsOnMainPath || Placement.Depth > tile.Placement.Depth)
				{
					return usedDoorway;
				}
			}
			return null;
		}

		public Doorway GetExitDoorway()
		{
			foreach (Doorway usedDoorway in UsedDoorways)
			{
				Tile tile = usedDoorway.ConnectedDoorway.Tile;
				if (Placement.IsOnMainPath)
				{
					if (tile.Placement.IsOnMainPath && Placement.PathDepth < tile.Placement.PathDepth)
					{
						return usedDoorway;
					}
				}
				else if (!tile.Placement.IsOnMainPath && Placement.Depth < tile.Placement.Depth)
				{
					return usedDoorway;
				}
			}
			return null;
		}

		public void OnBeforeSerialize()
		{
			fileVersion = 1;
		}

		public void OnAfterDeserialize()
		{
			if (fileVersion < 1)
			{
				RepeatMode = ((!allowImmediateRepeats) ? TileRepeatMode.DisallowImmediate : TileRepeatMode.Allow);
			}
		}
	}
}
