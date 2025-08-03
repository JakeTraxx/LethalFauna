using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace DunGen
{
	[AddComponentMenu("DunGen/Character")]
	public class DungenCharacter : MonoBehaviour
	{
		private static readonly List<DungenCharacter> allCharacters;

		private List<Tile> overlappingTiles;

		public static ReadOnlyCollection<DungenCharacter> AllCharacters { get; private set; }

		public Tile CurrentTile
		{
			get
			{
				if (overlappingTiles == null || overlappingTiles.Count == 0)
				{
					return null;
				}
				return overlappingTiles[overlappingTiles.Count - 1];
			}
		}

		public static event DungenCharacterDelegate CharacterAdded;

		public static event DungenCharacterDelegate CharacterRemoved;

		public event CharacterTileChangedEvent OnTileChanged;

		static DungenCharacter()
		{
			allCharacters = new List<DungenCharacter>();
			AllCharacters = new ReadOnlyCollection<DungenCharacter>(allCharacters);
		}

		protected virtual void OnEnable()
		{
			if (overlappingTiles == null)
			{
				overlappingTiles = new List<Tile>();
			}
			allCharacters.Add(this);
			if (DungenCharacter.CharacterAdded != null)
			{
				DungenCharacter.CharacterAdded(this);
			}
		}

		protected virtual void OnDisable()
		{
			allCharacters.Remove(this);
			if (DungenCharacter.CharacterRemoved != null)
			{
				DungenCharacter.CharacterRemoved(this);
			}
		}

		internal void ForceRecheckTile()
		{
			overlappingTiles.Clear();
			Tile[] array = Object.FindObjectsOfType<Tile>();
			foreach (Tile tile in array)
			{
				if (tile.Placement.Bounds.Contains(base.transform.position))
				{
					OnTileEntered(tile);
					break;
				}
			}
		}

		protected virtual void OnTileChangedEvent(Tile previousTile, Tile newTile)
		{
		}

		internal void OnTileEntered(Tile tile)
		{
			if (!overlappingTiles.Contains(tile))
			{
				Tile currentTile = CurrentTile;
				overlappingTiles.Add(tile);
				if (CurrentTile != currentTile)
				{
					this.OnTileChanged?.Invoke(this, currentTile, CurrentTile);
					OnTileChangedEvent(currentTile, CurrentTile);
				}
			}
		}

		internal void OnTileExited(Tile tile)
		{
			if (overlappingTiles.Contains(tile))
			{
				Tile currentTile = CurrentTile;
				overlappingTiles.Remove(tile);
				if (CurrentTile != currentTile)
				{
					this.OnTileChanged?.Invoke(this, currentTile, CurrentTile);
					OnTileChangedEvent(currentTile, CurrentTile);
				}
			}
		}
	}
}
