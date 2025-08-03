using System;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	public sealed class Key
	{
		public GameObject Prefab;

		public Color Colour;

		public IntRange KeysPerLock = new IntRange(1, 1);

		[SerializeField]
		private int id;

		[SerializeField]
		private string name;

		public int ID
		{
			get
			{
				return id;
			}
			set
			{
				id = value;
			}
		}

		public string Name
		{
			get
			{
				return name;
			}
			internal set
			{
				name = value;
			}
		}

		internal Key(int id)
		{
			this.id = id;
		}
	}
}
