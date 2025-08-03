using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DunGen.Graph;
using UnityEngine;
using UnityEngine.Serialization;

namespace DunGen
{
	[Serializable]
	public class DungeonGenerator : ISerializationCallbackReceiver
	{
		public const int CurrentFileVersion = 1;

		[SerializeField]
		[FormerlySerializedAs("AllowImmediateRepeats")]
		private bool allowImmediateRepeats;

		public int Seed;

		public bool ShouldRandomizeSeed = true;

		public int MaxAttemptCount = 20;

		public bool UseMaximumPairingAttempts;

		public int MaxPairingAttempts = 5;

		public bool IgnoreSpriteBounds;

		public AxisDirection UpDirection = AxisDirection.PosY;

		[FormerlySerializedAs("OverrideAllowImmediateRepeats")]
		public bool OverrideRepeatMode;

		public TileRepeatMode RepeatMode;

		public bool OverrideAllowTileRotation;

		public bool AllowTileRotation;

		public bool DebugRender;

		public float LengthMultiplier = 1f;

		public bool PlaceTileTriggers = true;

		public int TileTriggerLayer = 2;

		public bool GenerateAsynchronously;

		public float MaxAsyncFrameMilliseconds = 50f;

		public float PauseBetweenRooms;

		public bool RestrictDungeonToBounds;

		public Bounds TilePlacementBounds = new Bounds(Vector3.zero, Vector3.one * 10f);

		public float OverlapThreshold = 0.01f;

		public float Padding;

		public bool DisallowOverhangs;

		public GameObject Root;

		public DungeonFlow DungeonFlow;

		protected int retryCount;

		protected DungeonProxy proxyDungeon;

		protected readonly Dictionary<TilePlacementResult, int> tilePlacementResultCounters = new Dictionary<TilePlacementResult, int>();

		protected readonly List<GameObject> useableTiles = new List<GameObject>();

		protected int targetLength;

		protected List<InjectedTile> tilesPendingInjection;

		protected List<DungeonGeneratorPostProcessStep> postProcessSteps = new List<DungeonGeneratorPostProcessStep>();

		[SerializeField]
		private int fileVersion;

		private int nextNodeIndex;

		private DungeonArchetype currentArchetype;

		private GraphLine previousLineSegment;

		private List<TileProxy> preProcessData = new List<TileProxy>();

		private Stopwatch yieldTimer = new Stopwatch();

		private Dictionary<TileProxy, InjectedTile> injectedTiles = new Dictionary<TileProxy, InjectedTile>();

		public RandomStream RandomStream { get; protected set; }

		public Vector3 UpVector => UpDirection switch
		{
			AxisDirection.PosX => new Vector3(1f, 0f, 0f), 
			AxisDirection.NegX => new Vector3(-1f, 0f, 0f), 
			AxisDirection.PosY => new Vector3(0f, 1f, 0f), 
			AxisDirection.NegY => new Vector3(0f, -1f, 0f), 
			AxisDirection.PosZ => new Vector3(0f, 0f, 1f), 
			AxisDirection.NegZ => new Vector3(0f, 0f, -1f), 
			_ => throw new NotImplementedException("AxisDirection '" + UpDirection.ToString() + "' not implemented"), 
		};

		public GenerationStatus Status { get; private set; }

		public GenerationStats GenerationStats { get; private set; }

		public int ChosenSeed { get; protected set; }

		public Dungeon CurrentDungeon { get; private set; }

		public bool IsGenerating { get; private set; }

		public bool IsAnalysis { get; set; }

		public event GenerationStatusDelegate OnGenerationStatusChanged;

		public static event GenerationStatusDelegate OnAnyDungeonGenerationStatusChanged;

		public event TileInjectionDelegate TileInjectionMethods;

		public event Action Cleared;

		public event Action Retrying;

		public DungeonGenerator()
		{
			GenerationStats = new GenerationStats();
		}

		public DungeonGenerator(GameObject root)
			: this()
		{
			Root = root;
		}

		public void Generate()
		{
			if (!IsGenerating)
			{
				IsAnalysis = false;
				IsGenerating = true;
				Wait(OuterGenerate());
			}
		}

		public void Cancel()
		{
			if (IsGenerating)
			{
				Clear(stopCoroutines: true);
				IsGenerating = false;
			}
		}

		public Dungeon DetachDungeon()
		{
			if (CurrentDungeon == null)
			{
				return null;
			}
			Dungeon currentDungeon = CurrentDungeon;
			CurrentDungeon = null;
			Root = null;
			Clear(stopCoroutines: true);
			return currentDungeon;
		}

		protected virtual IEnumerator OuterGenerate()
		{
			Clear(stopCoroutines: false);
			yieldTimer.Restart();
			Status = GenerationStatus.NotStarted;
			ChosenSeed = (ShouldRandomizeSeed ? new RandomStream().Next() : Seed);
			RandomStream = new RandomStream(ChosenSeed);
			if (Root == null)
			{
				Root = new GameObject("Dungeon");
			}
			yield return Wait(InnerGenerate(isRetry: false));
			IsGenerating = false;
		}

		private Coroutine Wait(IEnumerator routine)
		{
			if (GenerateAsynchronously)
			{
				return CoroutineHelper.Start(routine);
			}
			while (routine.MoveNext())
			{
			}
			return null;
		}

		public void RandomizeSeed()
		{
			Seed = new RandomStream().Next();
		}

		protected virtual IEnumerator InnerGenerate(bool isRetry)
		{
			if (isRetry)
			{
				ChosenSeed = RandomStream.Next();
				RandomStream = new RandomStream(ChosenSeed);
				if (retryCount >= MaxAttemptCount && Application.isEditor)
				{
					string text = "Failed to generate the dungeon " + MaxAttemptCount + " times.\nThis could indicate a problem with the way the tiles are set up. Try to make sure most rooms have more than one doorway and that all doorways are easily accessible.\nHere are a list of all reasons a tile placement had to be retried:";
					foreach (KeyValuePair<TilePlacementResult, int> tilePlacementResultCounter in tilePlacementResultCounters)
					{
						if (tilePlacementResultCounter.Value > 0)
						{
							text = text + "\n" + tilePlacementResultCounter.Key.ToString() + " (x" + tilePlacementResultCounter.Value + ")";
						}
					}
					UnityEngine.Debug.LogError(text);
					ChangeStatus(GenerationStatus.Failed);
					yield break;
				}
				retryCount++;
				GenerationStats.IncrementRetryCount();
				if (this.Retrying != null)
				{
					this.Retrying();
				}
			}
			else
			{
				retryCount = 0;
				GenerationStats.Clear();
			}
			CurrentDungeon = Root.GetComponent<Dungeon>();
			if (CurrentDungeon == null)
			{
				CurrentDungeon = Root.AddComponent<Dungeon>();
			}
			CurrentDungeon.DebugRender = DebugRender;
			CurrentDungeon.PreGenerateDungeon(this);
			Clear(stopCoroutines: false);
			targetLength = Mathf.RoundToInt((float)DungeonFlow.Length.GetRandom(RandomStream) * LengthMultiplier);
			targetLength = Mathf.Max(targetLength, 2);
			Transform debugVisualsRoot = ((PauseBetweenRooms > 0f) ? Root.transform : null);
			proxyDungeon = new DungeonProxy(debugVisualsRoot);
			GenerationStats.BeginTime(GenerationStatus.TileInjection);
			if (tilesPendingInjection == null)
			{
				tilesPendingInjection = new List<InjectedTile>();
			}
			else
			{
				tilesPendingInjection.Clear();
			}
			injectedTiles.Clear();
			GatherTilesToInject();
			GenerationStats.BeginTime(GenerationStatus.PreProcessing);
			PreProcess();
			GenerationStats.BeginTime(GenerationStatus.MainPath);
			yield return Wait(GenerateMainPath());
			if (Status == GenerationStatus.Complete || Status == GenerationStatus.Failed)
			{
				yield break;
			}
			GenerationStats.BeginTime(GenerationStatus.Branching);
			yield return Wait(GenerateBranchPaths());
			foreach (InjectedTile item in tilesPendingInjection)
			{
				if (item.IsRequired)
				{
					yield return Wait(InnerGenerate(isRetry: true));
					yield break;
				}
			}
			if (Status == GenerationStatus.Complete || Status == GenerationStatus.Failed)
			{
				yield break;
			}
			if (DungeonFlow.BranchPruneTags.Count > 0)
			{
				PruneBranches();
			}
			proxyDungeon.ConnectOverlappingDoorways(DungeonFlow.DoorwayConnectionChance, DungeonFlow, RandomStream);
			CurrentDungeon.FromProxy(proxyDungeon, this);
			yield return Wait(PostProcess());
			yield return null;
			IDungeonCompleteReceiver[] componentsInChildren = CurrentDungeon.gameObject.GetComponentsInChildren<IDungeonCompleteReceiver>(includeInactive: false);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].OnDungeonComplete(CurrentDungeon);
			}
			ChangeStatus(GenerationStatus.Complete);
			if (true)
			{
				DungenCharacter[] array = UnityEngine.Object.FindObjectsOfType<DungenCharacter>();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].ForceRecheckTile();
				}
			}
		}

		private void PruneBranches()
		{
			Stack<TileProxy> stack = new Stack<TileProxy>();
			foreach (TileProxy tile2 in proxyDungeon.BranchPathTiles)
			{
				if (!tile2.UsedDoorways.Select((DoorwayProxy d) => d.ConnectedDoorway.TileProxy).Any((TileProxy t) => t.Placement.BranchDepth > tile2.Placement.BranchDepth))
				{
					stack.Push(tile2);
				}
			}
			while (stack.Count > 0)
			{
				TileProxy tile = stack.Pop();
				if ((tile.Placement.InjectionData == null || !tile.Placement.InjectionData.IsRequired) && DungeonFlow.ShouldPruneTileWithTags(tile.PrefabTile.Tags))
				{
					ProxyDoorwayConnection connection = (from d in tile.UsedDoorways
						select d.ConnectedDoorway into d
						where d.TileProxy.Placement.IsOnMainPath || d.TileProxy.Placement.BranchDepth < tile.Placement.BranchDepth
						select new ProxyDoorwayConnection(d, d.ConnectedDoorway)).First();
					proxyDungeon.RemoveTile(tile);
					proxyDungeon.RemoveConnection(connection);
					GenerationStats.PrunedBranchTileCount++;
					TileProxy tileProxy = connection.A.TileProxy;
					if (!tileProxy.Placement.IsOnMainPath)
					{
						stack.Push(tileProxy);
					}
				}
			}
		}

		public virtual void Clear(bool stopCoroutines)
		{
			if (stopCoroutines)
			{
				CoroutineHelper.StopAll();
			}
			if (proxyDungeon != null)
			{
				proxyDungeon.ClearDebugVisuals();
			}
			proxyDungeon = null;
			if (CurrentDungeon != null)
			{
				CurrentDungeon.Clear();
			}
			useableTiles.Clear();
			preProcessData.Clear();
			previousLineSegment = null;
			tilePlacementResultCounters.Clear();
			if (this.Cleared != null)
			{
				this.Cleared();
			}
		}

		private void ChangeStatus(GenerationStatus status)
		{
			GenerationStatus status2 = Status;
			Status = status;
			if (status == GenerationStatus.Complete || status == GenerationStatus.Failed)
			{
				IsGenerating = false;
			}
			if (status == GenerationStatus.Failed)
			{
				Clear(stopCoroutines: true);
			}
			if (status2 != status)
			{
				this.OnGenerationStatusChanged?.Invoke(this, status);
				DungeonGenerator.OnAnyDungeonGenerationStatusChanged?.Invoke(this, status);
			}
		}

		protected virtual void PreProcess()
		{
			if (preProcessData.Count > 0)
			{
				return;
			}
			ChangeStatus(GenerationStatus.PreProcessing);
			foreach (TileSet item in DungeonFlow.GetUsedTileSets().Concat(tilesPendingInjection.Select((InjectedTile x) => x.TileSet)).Distinct())
			{
				foreach (GameObjectChance weight in item.TileWeights.Weights)
				{
					if (weight.Value != null)
					{
						useableTiles.Add(weight.Value);
						weight.TileSet = item;
					}
				}
			}
		}

		protected virtual void GatherTilesToInject()
		{
			RandomStream randomStream = new RandomStream(ChosenSeed);
			foreach (TileInjectionRule tileInjectionRule in DungeonFlow.TileInjectionRules)
			{
				if (!(tileInjectionRule.TileSet == null) && (tileInjectionRule.CanAppearOnMainPath || tileInjectionRule.CanAppearOnBranchPath))
				{
					bool isOnMainPath = !tileInjectionRule.CanAppearOnBranchPath || (tileInjectionRule.CanAppearOnMainPath && randomStream.NextDouble() > 0.5);
					InjectedTile item = new InjectedTile(tileInjectionRule, isOnMainPath, randomStream);
					tilesPendingInjection.Add(item);
				}
			}
			if (this.TileInjectionMethods != null)
			{
				this.TileInjectionMethods(randomStream, ref tilesPendingInjection);
			}
		}

		protected virtual IEnumerator GenerateMainPath()
		{
			ChangeStatus(GenerationStatus.MainPath);
			nextNodeIndex = 0;
			List<GraphNode> list = new List<GraphNode>(DungeonFlow.Nodes.Count);
			bool flag = false;
			int num = 0;
			List<List<TileSet>> tileSets = new List<List<TileSet>>(targetLength);
			List<DungeonArchetype> archetypes = new List<DungeonArchetype>(targetLength);
			List<GraphNode> nodes = new List<GraphNode>(targetLength);
			List<GraphLine> lines = new List<GraphLine>(targetLength);
			while (!flag)
			{
				float num2 = Mathf.Clamp((float)num / (float)(targetLength - 1), 0f, 1f);
				GraphLine lineAtDepth = DungeonFlow.GetLineAtDepth(num2);
				if (lineAtDepth == null)
				{
					yield return Wait(InnerGenerate(isRetry: true));
					yield break;
				}
				if (lineAtDepth != previousLineSegment)
				{
					currentArchetype = lineAtDepth.GetRandomArchetype(RandomStream, archetypes);
					previousLineSegment = lineAtDepth;
				}
				GraphNode graphNode = null;
				GraphNode[] array = DungeonFlow.Nodes.OrderBy((GraphNode x) => x.Position).ToArray();
				GraphNode[] array2 = array;
				foreach (GraphNode graphNode2 in array2)
				{
					if (num2 >= graphNode2.Position && !list.Contains(graphNode2))
					{
						graphNode = graphNode2;
						list.Add(graphNode2);
						break;
					}
				}
				List<TileSet> tileSets2;
				if (graphNode != null)
				{
					tileSets2 = graphNode.TileSets;
					nextNodeIndex = ((nextNodeIndex >= array.Length - 1) ? (-1) : (nextNodeIndex + 1));
					archetypes.Add(null);
					lines.Add(null);
					nodes.Add(graphNode);
					if (graphNode == array[^1])
					{
						flag = true;
					}
				}
				else
				{
					tileSets2 = currentArchetype.TileSets;
					archetypes.Add(currentArchetype);
					lines.Add(lineAtDepth);
					nodes.Add(null);
				}
				tileSets.Add(tileSets2);
				num++;
			}
			int tileRetryCount = 0;
			int totalForLoopRetryCount = 0;
			for (int i = 0; i < tileSets.Count; i++)
			{
				TileProxy attachTo = ((i == 0) ? null : proxyDungeon.MainPathTiles[proxyDungeon.MainPathTiles.Count - 1]);
				TileProxy tileProxy = AddTile(attachTo, tileSets[i], (float)i / (float)(tileSets.Count - 1), archetypes[i]);
				if (i > 5 && tileProxy == null && tileRetryCount < 5 && totalForLoopRetryCount < 20)
				{
					TileProxy tileProxy2 = proxyDungeon.MainPathTiles[i - 1];
					if (injectedTiles.TryGetValue(tileProxy2, out var value))
					{
						tilesPendingInjection.Add(value);
						injectedTiles.Remove(tileProxy2);
					}
					proxyDungeon.RemoveLastConnection();
					proxyDungeon.RemoveTile(tileProxy2);
					i -= 2;
					tileRetryCount++;
					totalForLoopRetryCount++;
				}
				else
				{
					if (tileProxy == null)
					{
						yield return Wait(InnerGenerate(isRetry: true));
						break;
					}
					tileProxy.Placement.GraphNode = nodes[i];
					tileProxy.Placement.GraphLine = lines[i];
					tileRetryCount = 0;
					if (ShouldSkipFrame(isRoomPlacement: true))
					{
						yield return GetRoomPause();
					}
				}
			}
		}

		private bool ShouldSkipFrame(bool isRoomPlacement)
		{
			if (!GenerateAsynchronously)
			{
				return false;
			}
			if (isRoomPlacement && PauseBetweenRooms > 0f)
			{
				return true;
			}
			if (yieldTimer.Elapsed.TotalMilliseconds >= (double)MaxAsyncFrameMilliseconds)
			{
				yieldTimer.Restart();
				return true;
			}
			return false;
		}

		private YieldInstruction GetRoomPause()
		{
			if (PauseBetweenRooms > 0f)
			{
				return new WaitForSeconds(PauseBetweenRooms);
			}
			return null;
		}

		protected virtual IEnumerator GenerateBranchPaths()
		{
			ChangeStatus(GenerationStatus.Branching);
			int[] mainPathBranches = new int[proxyDungeon.MainPathTiles.Count];
			BranchCountHelper.ComputeBranchCounts(DungeonFlow, RandomStream, proxyDungeon, ref mainPathBranches);
			for (int b = 0; b < mainPathBranches.Length; b++)
			{
				TileProxy tile = proxyDungeon.MainPathTiles[b];
				int branchCount = mainPathBranches[b];
				if (tile.Placement.Archetype == null || branchCount == 0)
				{
					continue;
				}
				for (int i = 0; i < branchCount; i++)
				{
					TileProxy previousTile = tile;
					int branchDepth = tile.Placement.Archetype.BranchingDepth.GetRandom(RandomStream);
					for (int j = 0; j < branchDepth; j++)
					{
						List<TileSet> useableTileSets = ((j != branchDepth - 1 || !tile.Placement.Archetype.GetHasValidBranchCapTiles()) ? tile.Placement.Archetype.TileSets : ((tile.Placement.Archetype.BranchCapType != 0) ? tile.Placement.Archetype.TileSets.Concat(tile.Placement.Archetype.BranchCapTileSets).ToList() : tile.Placement.Archetype.BranchCapTileSets));
						float num = ((branchDepth <= 1) ? 1f : ((float)j / (float)(branchDepth - 1)));
						TileProxy tileProxy = AddTile(previousTile, useableTileSets, num, tile.Placement.Archetype);
						if (tileProxy == null)
						{
							break;
						}
						tileProxy.Placement.BranchDepth = j;
						tileProxy.Placement.NormalizedBranchDepth = num;
						tileProxy.Placement.GraphNode = previousTile.Placement.GraphNode;
						tileProxy.Placement.GraphLine = previousTile.Placement.GraphLine;
						previousTile = tileProxy;
						if (ShouldSkipFrame(isRoomPlacement: true))
						{
							yield return GetRoomPause();
						}
					}
				}
			}
		}

		protected virtual TileProxy AddTile(TileProxy attachTo, IEnumerable<TileSet> useableTileSets, float normalizedDepth, DungeonArchetype archetype, TilePlacementResult result = TilePlacementResult.None)
		{
			bool flag = Status == GenerationStatus.MainPath;
			bool flag2 = attachTo == null;
			InjectedTile injectedTile = null;
			int index = -1;
			bool flag3 = flag && archetype == null;
			if (tilesPendingInjection != null && !flag3)
			{
				float pathDepth = (flag ? normalizedDepth : ((float)attachTo.Placement.PathDepth / ((float)targetLength - 1f)));
				float branchDepth = (flag ? 0f : normalizedDepth);
				for (int i = 0; i < tilesPendingInjection.Count; i++)
				{
					InjectedTile injectedTile2 = tilesPendingInjection[i];
					if (injectedTile2.ShouldInjectTileAtPoint(flag, pathDepth, branchDepth))
					{
						injectedTile = injectedTile2;
						index = i;
						break;
					}
				}
			}
			IEnumerable<GameObjectChance> collection = ((injectedTile == null) ? useableTileSets.SelectMany((TileSet x) => x.TileWeights.Weights) : new List<GameObjectChance>(injectedTile.TileSet.TileWeights.Weights));
			bool value = !flag2 && attachTo.PrefabTile.AllowRotation;
			if (OverrideAllowTileRotation)
			{
				value = AllowTileRotation;
			}
			DoorwayPairFinder obj = new DoorwayPairFinder
			{
				DungeonFlow = DungeonFlow,
				RandomStream = RandomStream,
				Archetype = archetype,
				GetTileTemplateDelegate = GetTileTemplate,
				IsOnMainPath = flag,
				NormalizedDepth = normalizedDepth,
				PreviousTile = attachTo,
				UpVector = UpVector,
				AllowRotation = value,
				TileWeights = new List<GameObjectChance>(collection),
				IsTileAllowedPredicate = delegate(TileProxy previousTile, TileProxy potentialNextTile, ref float weight)
				{
					bool flag4 = previousTile != null && potentialNextTile.Prefab == previousTile.Prefab;
					TileRepeatMode tileRepeatMode = TileRepeatMode.Allow;
					if (OverrideRepeatMode)
					{
						tileRepeatMode = RepeatMode;
					}
					else if (potentialNextTile != null)
					{
						tileRepeatMode = potentialNextTile.PrefabTile.RepeatMode;
					}
					bool flag5 = true;
					return tileRepeatMode switch
					{
						TileRepeatMode.Allow => true, 
						TileRepeatMode.DisallowImmediate => !flag4, 
						TileRepeatMode.Disallow => !proxyDungeon.AllTiles.Where((TileProxy t) => t.Prefab == potentialNextTile.Prefab).Any(), 
						_ => throw new NotImplementedException("TileRepeatMode " + tileRepeatMode.ToString() + " is not implemented"), 
					};
				}
			};
			int? maxCount = (UseMaximumPairingAttempts ? new int?(MaxPairingAttempts) : null);
			Queue<DoorwayPair> doorwayPairs = obj.GetDoorwayPairs(maxCount);
			TilePlacementResult tilePlacementResult = TilePlacementResult.NoValidTile;
			TileProxy tile = null;
			while (doorwayPairs.Count > 0)
			{
				DoorwayPair pair = doorwayPairs.Dequeue();
				tilePlacementResult = TryPlaceTile(pair, archetype, out tile);
				if (tilePlacementResult == TilePlacementResult.None)
				{
					break;
				}
				AddTilePlacementResult(tilePlacementResult);
			}
			if (tilePlacementResult == TilePlacementResult.None)
			{
				if (injectedTile != null)
				{
					tile.Placement.InjectionData = injectedTile;
					injectedTiles[tile] = injectedTile;
					tilesPendingInjection.RemoveAt(index);
					if (flag)
					{
						targetLength++;
					}
				}
				return tile;
			}
			return null;
		}

		protected void AddTilePlacementResult(TilePlacementResult result)
		{
			if (!tilePlacementResultCounters.TryGetValue(result, out var value))
			{
				tilePlacementResultCounters[result] = 1;
			}
			else
			{
				tilePlacementResultCounters[result] = value + 1;
			}
		}

		protected TilePlacementResult TryPlaceTile(DoorwayPair pair, DungeonArchetype archetype, out TileProxy tile)
		{
			tile = null;
			TileProxy nextTemplate = pair.NextTemplate;
			DoorwayProxy previousDoorway = pair.PreviousDoorway;
			if (nextTemplate == null)
			{
				return TilePlacementResult.TemplateIsNull;
			}
			int index = pair.NextTemplate.Doorways.IndexOf(pair.NextDoorway);
			tile = new TileProxy(nextTemplate);
			tile.Placement.IsOnMainPath = Status == GenerationStatus.MainPath;
			tile.Placement.Archetype = archetype;
			tile.Placement.TileSet = pair.NextTileSet;
			if (previousDoorway != null)
			{
				DoorwayProxy myDoorway = tile.Doorways[index];
				tile.PositionBySocket(myDoorway, previousDoorway);
				Bounds bounds = tile.Placement.Bounds;
				if (RestrictDungeonToBounds && !TilePlacementBounds.Contains(bounds))
				{
					return TilePlacementResult.OutOfBounds;
				}
				if (IsCollidingWithAnyTile(tile, previousDoorway.TileProxy))
				{
					return TilePlacementResult.TileIsColliding;
				}
			}
			if (tile == null)
			{
				return TilePlacementResult.NewTileIsNull;
			}
			if (tile.Placement.IsOnMainPath)
			{
				if (pair.PreviousTile != null)
				{
					tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth + 1;
				}
			}
			else
			{
				tile.Placement.PathDepth = pair.PreviousTile.Placement.PathDepth;
				tile.Placement.BranchDepth = ((!pair.PreviousTile.Placement.IsOnMainPath) ? (pair.PreviousTile.Placement.BranchDepth + 1) : 0);
			}
			if (previousDoorway != null)
			{
				DoorwayProxy b = tile.Doorways[index];
				proxyDungeon.MakeConnection(previousDoorway, b);
			}
			proxyDungeon.AddTile(tile);
			return TilePlacementResult.None;
		}

		protected TileProxy GetTileTemplate(GameObject prefab)
		{
			TileProxy tileProxy = preProcessData.Where((TileProxy x) => x.Prefab == prefab).FirstOrDefault();
			if (tileProxy == null)
			{
				tileProxy = new TileProxy(prefab, IgnoreSpriteBounds, UpVector);
				preProcessData.Add(tileProxy);
			}
			return tileProxy;
		}

		protected TileProxy PickRandomTemplate(DoorwaySocket socketGroupFilter)
		{
			GameObject prefab = useableTiles[RandomStream.Next(0, useableTiles.Count)];
			TileProxy tileTemplate = GetTileTemplate(prefab);
			if (socketGroupFilter != null && !tileTemplate.UnusedDoorways.Where((DoorwayProxy d) => d.Socket == socketGroupFilter).Any())
			{
				return PickRandomTemplate(socketGroupFilter);
			}
			return tileTemplate;
		}

		protected int NormalizedDepthToIndex(float normalizedDepth)
		{
			return Mathf.RoundToInt(normalizedDepth * (float)(targetLength - 1));
		}

		protected float IndexToNormalizedDepth(int index)
		{
			return (float)index / (float)targetLength;
		}

		protected bool IsCollidingWithAnyTile(TileProxy newTile, TileProxy previousTile)
		{
			foreach (TileProxy allTile in proxyDungeon.AllTiles)
			{
				bool flag = previousTile == allTile;
				float maxOverlap = (flag ? OverlapThreshold : (0f - Padding));
				if (DisallowOverhangs && !flag)
				{
					if (newTile.IsOverlappingOrOverhanging(allTile, UpDirection, maxOverlap))
					{
						return true;
					}
				}
				else if (newTile.IsOverlapping(allTile, maxOverlap))
				{
					return true;
				}
			}
			return false;
		}

		public void RegisterPostProcessStep(Action<DungeonGenerator> postProcessCallback, int priority = 0, PostProcessPhase phase = PostProcessPhase.AfterBuiltIn)
		{
			postProcessSteps.Add(new DungeonGeneratorPostProcessStep(postProcessCallback, priority, phase));
		}

		public void UnregisterPostProcessStep(Action<DungeonGenerator> postProcessCallback)
		{
			for (int i = 0; i < postProcessSteps.Count; i++)
			{
				if (postProcessSteps[i].PostProcessCallback == postProcessCallback)
				{
					postProcessSteps.RemoveAt(i);
				}
			}
		}

		protected virtual IEnumerator PostProcess()
		{
			int length = proxyDungeon.MainPathTiles.Count;
			int maxBranchDepth = 0;
			if (proxyDungeon.BranchPathTiles.Count > 0)
			{
				List<TileProxy> list = proxyDungeon.BranchPathTiles.ToList();
				list.Sort((TileProxy a, TileProxy b) => b.Placement.BranchDepth.CompareTo(a.Placement.BranchDepth));
				maxBranchDepth = list[0].Placement.BranchDepth;
			}
			yield return null;
			GenerationStats.BeginTime(GenerationStatus.PostProcessing);
			ChangeStatus(GenerationStatus.PostProcessing);
			postProcessSteps.Sort((DungeonGeneratorPostProcessStep a, DungeonGeneratorPostProcessStep b) => b.Priority.CompareTo(a.Priority));
			foreach (DungeonGeneratorPostProcessStep step2 in postProcessSteps)
			{
				if (ShouldSkipFrame(isRoomPlacement: false))
				{
					yield return null;
				}
				if (step2.Phase == PostProcessPhase.BeforeBuiltIn)
				{
					step2.PostProcessCallback(this);
				}
			}
			yield return null;
			if (!IsAnalysis)
			{
				foreach (Tile tile2 in CurrentDungeon.AllTiles)
				{
					if (ShouldSkipFrame(isRoomPlacement: false))
					{
						yield return null;
					}
					tile2.Placement.NormalizedPathDepth = (float)tile2.Placement.PathDepth / (float)(length - 1);
				}
				CurrentDungeon.PostGenerateDungeon(this);
				foreach (Tile tile2 in CurrentDungeon.AllTiles)
				{
					if (ShouldSkipFrame(isRoomPlacement: false))
					{
						yield return null;
					}
					ProcessProps(tile2, tile2.gameObject);
				}
				ProcessGlobalProps();
				if (DungeonFlow.KeyManager != null)
				{
					PlaceLocksAndKeys();
				}
			}
			GenerationStats.SetRoomStatistics(CurrentDungeon.MainPathTiles.Count, CurrentDungeon.BranchPathTiles.Count, maxBranchDepth);
			preProcessData.Clear();
			yield return null;
			foreach (DungeonGeneratorPostProcessStep step2 in postProcessSteps)
			{
				if (ShouldSkipFrame(isRoomPlacement: false))
				{
					yield return null;
				}
				if (step2.Phase == PostProcessPhase.AfterBuiltIn)
				{
					step2.PostProcessCallback(this);
				}
			}
			GenerationStats.EndTime();
			foreach (GameObject door in CurrentDungeon.Doors)
			{
				if (door != null)
				{
					door.SetActive(value: true);
				}
			}
		}

		protected void ProcessProps(Tile tile, GameObject root)
		{
			if (root == null)
			{
				return;
			}
			RandomProp[] components = root.GetComponents<RandomProp>();
			for (int i = 0; i < components.Length; i++)
			{
				components[i].Process(RandomStream, tile);
			}
			if (root == null)
			{
				return;
			}
			int childCount = root.transform.childCount;
			List<GameObject> list = new List<GameObject>(childCount);
			for (int j = 0; j < childCount; j++)
			{
				GameObject gameObject = root.transform.GetChild(j).gameObject;
				list.Add(gameObject);
			}
			foreach (GameObject item in list)
			{
				if (item != null)
				{
					ProcessProps(tile, item);
				}
			}
		}

		protected virtual void ProcessGlobalProps()
		{
			Dictionary<int, GameObjectChanceTable> dictionary = new Dictionary<int, GameObjectChanceTable>();
			foreach (Tile allTile in CurrentDungeon.AllTiles)
			{
				GlobalProp[] componentsInChildren = allTile.GetComponentsInChildren<GlobalProp>();
				foreach (GlobalProp globalProp in componentsInChildren)
				{
					GameObjectChanceTable value = null;
					if (!dictionary.TryGetValue(globalProp.PropGroupID, out value))
					{
						value = new GameObjectChanceTable();
						dictionary[globalProp.PropGroupID] = value;
					}
					float num = (allTile.Placement.IsOnMainPath ? globalProp.MainPathWeight : globalProp.BranchPathWeight);
					num *= globalProp.DepthWeightScale.Evaluate(allTile.Placement.NormalizedDepth);
					value.Weights.Add(new GameObjectChance(globalProp.gameObject, num, 0f, null));
				}
			}
			foreach (GameObjectChanceTable value2 in dictionary.Values)
			{
				foreach (GameObjectChance weight in value2.Weights)
				{
					weight.Value.SetActive(value: false);
				}
			}
			List<int> list = new List<int>(dictionary.Count);
			foreach (KeyValuePair<int, GameObjectChanceTable> pair in dictionary)
			{
				if (list.Contains(pair.Key))
				{
					UnityEngine.Debug.LogWarning("Dungeon Flow contains multiple entries for the global prop group ID: " + pair.Key + ". Only the first entry will be used.");
					continue;
				}
				DungeonFlow.GlobalPropSettings globalPropSettings = DungeonFlow.GlobalProps.Where((DungeonFlow.GlobalPropSettings x) => x.ID == pair.Key).FirstOrDefault();
				if (globalPropSettings == null)
				{
					continue;
				}
				GameObjectChanceTable gameObjectChanceTable = pair.Value.Clone();
				int random = globalPropSettings.Count.GetRandom(RandomStream);
				random = Mathf.Clamp(random, 0, gameObjectChanceTable.Weights.Count);
				for (int j = 0; j < random; j++)
				{
					GameObjectChance random2 = gameObjectChanceTable.GetRandom(RandomStream, isOnMainPath: true, 0f, null, allowImmediateRepeats: true, removeFromTable: true);
					if (random2 != null && random2.Value != null)
					{
						random2.Value.SetActive(value: true);
					}
				}
				list.Add(pair.Key);
			}
		}

		protected virtual void PlaceLocksAndKeys()
		{
			GraphNode[] array = (from x in CurrentDungeon.ConnectionGraph.Nodes
				select x.Tile.Placement.GraphNode into x
				where x != null
				select x).Distinct().ToArray();
			GraphLine[] array2 = (from x in CurrentDungeon.ConnectionGraph.Nodes
				select x.Tile.Placement.GraphLine into x
				where x != null
				select x).Distinct().ToArray();
			Dictionary<Doorway, Key> lockedDoorways = new Dictionary<Doorway, Key>();
			GraphNode[] array3 = array;
			foreach (GraphNode node in array3)
			{
				foreach (KeyLockPlacement @lock in node.Locks)
				{
					Tile tile = CurrentDungeon.AllTiles.Where((Tile x) => x.Placement.GraphNode == node).FirstOrDefault();
					List<DungeonGraphConnection> connections = CurrentDungeon.ConnectionGraph.Nodes.Where((DungeonGraphNode x) => x.Tile == tile).FirstOrDefault().Connections;
					Doorway doorway = null;
					Doorway doorway2 = null;
					foreach (DungeonGraphConnection item in connections)
					{
						if (item.DoorwayA.Tile == tile)
						{
							doorway2 = item.DoorwayA;
						}
						else if (item.DoorwayB.Tile == tile)
						{
							doorway = item.DoorwayB;
						}
					}
					Key keyByID = node.Graph.KeyManager.GetKeyByID(@lock.ID);
					if (doorway != null && (node.LockPlacement & NodeLockPlacement.Entrance) == NodeLockPlacement.Entrance)
					{
						lockedDoorways.Add(doorway, keyByID);
					}
					if (doorway2 != null && (node.LockPlacement & NodeLockPlacement.Exit) == NodeLockPlacement.Exit)
					{
						lockedDoorways.Add(doorway2, keyByID);
					}
				}
			}
			GraphLine[] array4 = array2;
			foreach (GraphLine line in array4)
			{
				List<Doorway> list = (from x in CurrentDungeon.ConnectionGraph.Connections.Where(delegate(DungeonGraphConnection x)
					{
						bool flag4 = lockedDoorways.ContainsKey(x.DoorwayA) || lockedDoorways.ContainsKey(x.DoorwayB);
						bool flag5 = x.DoorwayA.Tile.Placement.TileSet.LockPrefabs.Count > 0;
						return x.DoorwayA.Tile.Placement.GraphLine == line && x.DoorwayB.Tile.Placement.GraphLine == line && !flag4 && flag5;
					})
					select x.DoorwayA).ToList();
				if (list.Count == 0)
				{
					continue;
				}
				foreach (KeyLockPlacement lock2 in line.Locks)
				{
					int random = lock2.Range.GetRandom(RandomStream);
					random = Mathf.Clamp(random, 0, list.Count);
					for (int j = 0; j < random; j++)
					{
						if (list.Count == 0)
						{
							break;
						}
						Doorway doorway3 = list[RandomStream.Next(0, list.Count)];
						list.Remove(doorway3);
						if (!lockedDoorways.ContainsKey(doorway3))
						{
							Key keyByID2 = line.Graph.KeyManager.GetKeyByID(lock2.ID);
							lockedDoorways.Add(doorway3, keyByID2);
						}
					}
				}
			}
			foreach (Tile allTile in CurrentDungeon.AllTiles)
			{
				if (allTile.Placement.InjectionData == null || !allTile.Placement.InjectionData.IsLocked)
				{
					continue;
				}
				List<Doorway> list2 = new List<Doorway>();
				foreach (Doorway usedDoorway in allTile.UsedDoorways)
				{
					bool num = lockedDoorways.ContainsKey(usedDoorway) || lockedDoorways.ContainsKey(usedDoorway.ConnectedDoorway);
					bool flag = allTile.Placement.TileSet.LockPrefabs.Count > 0;
					bool flag2 = allTile.GetEntranceDoorway() == usedDoorway;
					if (!num && flag && flag2)
					{
						list2.Add(usedDoorway);
					}
				}
				if (list2.Any())
				{
					Doorway key2 = list2.First();
					Key keyByID3 = DungeonFlow.KeyManager.GetKeyByID(allTile.Placement.InjectionData.LockID);
					lockedDoorways.Add(key2, keyByID3);
				}
			}
			List<Doorway> list3 = new List<Doorway>();
			foreach (KeyValuePair<Doorway, Key> item2 in lockedDoorways)
			{
				Doorway key3 = item2.Key;
				Key key = item2.Value;
				List<Tile> list4 = new List<Tile>();
				foreach (Tile allTile2 in CurrentDungeon.AllTiles)
				{
					if (!(allTile2.Placement.NormalizedPathDepth >= key3.Tile.Placement.NormalizedPathDepth))
					{
						bool flag3 = false;
						if (allTile2.Placement.GraphNode != null && allTile2.Placement.GraphNode.Keys.Where((KeyLockPlacement x) => x.ID == key.ID).Count() > 0)
						{
							flag3 = true;
						}
						else if (allTile2.Placement.GraphLine != null && allTile2.Placement.GraphLine.Keys.Where((KeyLockPlacement x) => x.ID == key.ID).Count() > 0)
						{
							flag3 = true;
						}
						if (flag3)
						{
							list4.Add(allTile2);
						}
					}
				}
				List<IKeySpawnable> list5 = list4.SelectMany((Tile x) => x.GetComponentsInChildren<Component>().OfType<IKeySpawnable>()).ToList();
				if (list5.Count == 0)
				{
					list3.Add(key3);
					continue;
				}
				int random2 = key.KeysPerLock.GetRandom(RandomStream);
				random2 = Math.Min(random2, list5.Count);
				for (int k = 0; k < random2; k++)
				{
					int index = RandomStream.Next(0, list5.Count);
					IKeySpawnable keySpawnable = list5[index];
					keySpawnable.SpawnKey(key, DungeonFlow.KeyManager);
					foreach (IKeyLock item3 in (keySpawnable as Component).GetComponentsInChildren<Component>().OfType<IKeyLock>())
					{
						item3.OnKeyAssigned(key, DungeonFlow.KeyManager);
					}
					list5.RemoveAt(index);
				}
			}
			foreach (Doorway item4 in list3)
			{
				lockedDoorways.Remove(item4);
			}
			foreach (KeyValuePair<Doorway, Key> item5 in lockedDoorways)
			{
				item5.Key.RemoveUsedPrefab();
				LockDoorway(item5.Key, item5.Value, DungeonFlow.KeyManager);
			}
		}

		protected virtual void LockDoorway(Doorway doorway, Key key, KeyManager keyManager)
		{
			TilePlacementData placement = doorway.Tile.Placement;
			GameObjectChanceTable[] array = (from x in doorway.Tile.Placement.TileSet.LockPrefabs.Where(delegate(LockedDoorwayAssociation x)
				{
					DoorwaySocket socket = x.Socket;
					return socket == null || DoorwaySocket.CanSocketsConnect(socket, doorway.Socket);
				})
				select x.LockPrefabs).ToArray();
			if (array.Length == 0)
			{
				return;
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(array[RandomStream.Next(0, array.Length)].GetRandom(RandomStream, placement.IsOnMainPath, placement.NormalizedDepth, null, allowImmediateRepeats: true).Value, doorway.transform);
			DungeonUtil.AddAndSetupDoorComponent(CurrentDungeon, gameObject, doorway);
			doorway.SetUsedPrefab(gameObject);
			doorway.ConnectedDoorway.SetUsedPrefab(gameObject);
			foreach (IKeyLock item in gameObject.GetComponentsInChildren<Component>().OfType<IKeyLock>())
			{
				item.OnKeyAssigned(key, keyManager);
			}
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
