namespace DunGen
{
	public struct ProxyDoorwayConnection
	{
		public DoorwayProxy A { get; private set; }

		public DoorwayProxy B { get; private set; }

		public ProxyDoorwayConnection(DoorwayProxy a, DoorwayProxy b)
		{
			A = a;
			B = b;
		}
	}
}
