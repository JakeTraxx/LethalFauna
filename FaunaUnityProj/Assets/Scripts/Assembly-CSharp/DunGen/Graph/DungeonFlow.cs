using System;
using System.Collections.Generic;
using System.Linq;
using DunGen.Tags;
using UnityEngine;
using UnityEngine.Serialization;

namespace DunGen.Graph
{
	[Serializable]
	[CreateAssetMenu(fileName = "New Dungeon", menuName = "DunGen/Dungeon Flow", order = 700)]
	public class DungeonFlow : ScriptableObject, ISerializationCallbackReceiver
	{
		[Serializable]
		public sealed class GlobalPropSettings
		{
			public int ID;

			public IntRange Count;

			public GlobalPropSettings()
			{
				ID = 0;
				Count = new IntRange(0, 1);
			}

			public GlobalPropSettings(int id, IntRange count)
			{
				ID = id;
				Count = count;
			}
		}

		public enum TagConnectionMode
		{
			Accept = 0,
			Reject = 1
		}

		public enum BranchPruneMode
		{
			AnyTagPresent = 0,
			AllTagsMissing = 1
		}

		public const int FileVersion = 1;

		[SerializeField]
		[FormerlySerializedAs("GlobalPropGroupIDs")]
		private List<int> globalPropGroupID_obsolete = new List<int>();

		[SerializeField]
		[FormerlySerializedAs("GlobalPropRanges")]
		private List<IntRange> globalPropRanges_obsolete = new List<IntRange>();

		public IntRange Length = new IntRange(5, 10);

		public BranchMode BranchMode;

		public IntRange BranchCount = new IntRange(1, 5);

		public List<GlobalPropSettings> GlobalProps = new List<GlobalPropSettings>();

		public KeyManager KeyManager;

		[Range(0f, 1f)]
		public float DoorwayConnectionChance;

		public bool RestrictConnectionToSameSection;

		public List<TileInjectionRule> TileInjectionRules = new List<TileInjectionRule>();

		public TagConnectionMode TileTagConnectionMode;

		public List<TagPair> TileConnectionTags = new List<TagPair>();

		public BranchPruneMode BranchTagPruneMode = BranchPruneMode.AllTagsMissing;

		public List<Tag> BranchPruneTags = new List<Tag>();

		public List<GraphNode> Nodes = new List<GraphNode>();

		public List<GraphLine> Lines = new List<GraphLine>();

		[SerializeField]
		private int currentFileVersion;

		public void Reset()
		{
			TileSet[] tileSets = new TileSet[0];
			DungeonArchetype[] archetypes = new DungeonArchetype[0];
			new DungeonFlowBuilder(this).AddNode(tileSets, "Start").AddLine(archetypes).AddNode(tileSets, "Goal")
				.Complete();
		}

		public GraphLine GetLineAtDepth(float normalizedDepth)
		{
			normalizedDepth = Mathf.Clamp(normalizedDepth, 0f, 1f);
			if (normalizedDepth == 0f)
			{
				return Lines[0];
			}
			if (normalizedDepth == 1f)
			{
				return Lines[Lines.Count - 1];
			}
			foreach (GraphLine line in Lines)
			{
				if (normalizedDepth >= line.Position && normalizedDepth < line.Position + line.Length)
				{
					return line;
				}
			}
			Debug.LogError("GetLineAtDepth was unable to find a line at depth " + normalizedDepth + ". This shouldn't happen.");
			return null;
		}

		public DungeonArchetype[] GetUsedArchetypes()
		{
			return Lines.SelectMany((GraphLine x) => x.DungeonArchetypes).ToArray();
		}

		public TileSet[] GetUsedTileSets()
		{
			List<TileSet> list = new List<TileSet>();
			foreach (GraphNode node in Nodes)
			{
				list.AddRange(node.TileSets);
			}
			foreach (GraphLine line in Lines)
			{
				foreach (DungeonArchetype dungeonArchetype in line.DungeonArchetypes)
				{
					list.AddRange(dungeonArchetype.TileSets);
					list.AddRange(dungeonArchetype.BranchCapTileSets);
				}
			}
			return list.ToArray();
		}

		public bool ShouldPruneTileWithTags(TagContainer tileTags)
		{
			return BranchTagPruneMode switch
			{
				BranchPruneMode.AnyTagPresent => tileTags.HasAnyTag(BranchPruneTags.ToArray()), 
				BranchPruneMode.AllTagsMissing => !tileTags.HasAnyTag(BranchPruneTags.ToArray()), 
				_ => throw new NotImplementedException($"BranchPruneMode {BranchTagPruneMode} is not implemented"), 
			};
		}

		public void OnBeforeSerialize()
		{
			currentFileVersion = 1;
		}

		public void OnAfterDeserialize()
		{
			if (currentFileVersion < 1)
			{
				for (int i = 0; i < globalPropGroupID_obsolete.Count; i++)
				{
					int id = globalPropGroupID_obsolete[i];
					IntRange count = globalPropRanges_obsolete[i];
					GlobalProps.Add(new GlobalPropSettings(id, count));
				}
				globalPropGroupID_obsolete.Clear();
				globalPropRanges_obsolete.Clear();
			}
		}

		public bool CanTilesConnect(Tile tileA, Tile tileB)
		{
			if (tileA == null || tileB == null)
			{
				return false;
			}
			if (TileConnectionTags.Count == 0)
			{
				return true;
			}
			return TileTagConnectionMode switch
			{
				TagConnectionMode.Accept => HasMatchingTagPair(tileA, tileB), 
				TagConnectionMode.Reject => !HasMatchingTagPair(tileA, tileB), 
				_ => throw new NotImplementedException($"{typeof(TagConnectionMode).Name}.{TileTagConnectionMode} is not implemented"), 
			};
		}

		public bool CanDoorwaysConnect(Tile tileA, Tile tileB, Doorway doorwayA, Doorway doorwayB)
		{
			foreach (TileConnectionRule item in DoorwayPairFinder.CustomConnectionRules.OrderByDescending((TileConnectionRule r) => r.Priority))
			{
				TileConnectionRule.ConnectionResult connectionResult = item.Delegate(tileA, tileB, doorwayA, doorwayB);
				if (connectionResult != TileConnectionRule.ConnectionResult.Passthrough)
				{
					return connectionResult == TileConnectionRule.ConnectionResult.Allow;
				}
			}
			if (DoorwaySocket.CanSocketsConnect(doorwayA.Socket, doorwayB.Socket))
			{
				return CanTilesConnect(tileA, tileB);
			}
			return false;
		}

		private bool HasMatchingTagPair(Tile tileA, Tile tileB)
		{
			foreach (TagPair tileConnectionTag in TileConnectionTags)
			{
				if ((tileA.Tags.HasTag(tileConnectionTag.TagA) && tileB.Tags.HasTag(tileConnectionTag.TagB)) || (tileB.Tags.HasTag(tileConnectionTag.TagA) && tileA.Tags.HasTag(tileConnectionTag.TagB)))
				{
					return true;
				}
			}
			return false;
		}
	}
}
