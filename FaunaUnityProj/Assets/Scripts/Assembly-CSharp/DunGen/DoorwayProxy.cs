using UnityEngine;

namespace DunGen
{
	public sealed class DoorwayProxy
	{
		public bool Used => ConnectedDoorway != null;

		public TileProxy TileProxy { get; private set; }

		public int Index { get; private set; }

		public DoorwaySocket Socket { get; private set; }

		public Doorway DoorwayComponent { get; private set; }

		public Vector3 LocalPosition { get; private set; }

		public Quaternion LocalRotation { get; private set; }

		public DoorwayProxy ConnectedDoorway { get; private set; }

		public Vector3 Forward => TileProxy.Placement.Rotation * LocalRotation * Vector3.forward;

		public Vector3 Up => TileProxy.Placement.Rotation * LocalRotation * Vector3.up;

		public Vector3 Position => TileProxy.Placement.Transform.MultiplyPoint(LocalPosition);

		public DoorwayProxy(TileProxy tileProxy, DoorwayProxy other)
		{
			TileProxy = tileProxy;
			Index = other.Index;
			Socket = other.Socket;
			DoorwayComponent = other.DoorwayComponent;
			LocalPosition = other.LocalPosition;
			LocalRotation = other.LocalRotation;
		}

		public DoorwayProxy(TileProxy tileProxy, int index, Doorway doorwayComponent, Vector3 localPosition, Quaternion localRotation)
		{
			TileProxy = tileProxy;
			Index = index;
			Socket = doorwayComponent.Socket;
			DoorwayComponent = doorwayComponent;
			LocalPosition = localPosition;
			LocalRotation = localRotation;
		}

		public static void Connect(DoorwayProxy a, DoorwayProxy b)
		{
			a.ConnectedDoorway = b;
			b.ConnectedDoorway = a;
		}

		public void Disconnect()
		{
			if (ConnectedDoorway != null)
			{
				ConnectedDoorway.ConnectedDoorway = null;
				ConnectedDoorway = null;
			}
		}
	}
}
