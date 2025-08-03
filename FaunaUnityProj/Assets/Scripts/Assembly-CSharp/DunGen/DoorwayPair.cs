namespace DunGen
{
	public struct DoorwayPair
	{
		public TileProxy PreviousTile { get; private set; }

		public DoorwayProxy PreviousDoorway { get; private set; }

		public TileProxy NextTemplate { get; private set; }

		public DoorwayProxy NextDoorway { get; private set; }

		public TileSet NextTileSet { get; private set; }

		public float TileWeight { get; private set; }

		public float DoorwayWeight { get; private set; }

		public DoorwayPair(TileProxy previousTile, DoorwayProxy previousDoorway, TileProxy nextTemplate, DoorwayProxy nextDoorway, TileSet nextTileSet, float tileWeight, float doorwayWeight)
		{
			PreviousTile = previousTile;
			PreviousDoorway = previousDoorway;
			NextTemplate = nextTemplate;
			NextDoorway = nextDoorway;
			NextTileSet = nextTileSet;
			TileWeight = tileWeight;
			DoorwayWeight = doorwayWeight;
		}
	}
}
