using System;
using UnityEngine;

namespace DunGen
{
	[CreateAssetMenu(fileName = "New Doorway Socket", menuName = "DunGen/Doorway Socket", order = 700)]
	public class DoorwaySocket : ScriptableObject
	{
		[SerializeField]
		private Vector2 size = new Vector2(1f, 2f);

		[Obsolete("Use DoorwayPairFinder.CustomConnectionRules instead")]
		public static SocketConnectionDelegate CustomSocketConnectionDelegate;

		public Vector2 Size => size;

		public static bool CanSocketsConnect(DoorwaySocket a, DoorwaySocket b)
		{
			if (CustomSocketConnectionDelegate != null)
			{
				return CustomSocketConnectionDelegate(a, b);
			}
			return a == b;
		}
	}
}
