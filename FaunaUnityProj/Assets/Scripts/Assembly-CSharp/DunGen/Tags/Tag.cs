using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen.Tags
{
	[Serializable]
	public sealed class Tag : IEqualityComparer<Tag>
	{
		[SerializeField]
		private int id = -1;

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
				return DunGenSettings.Instance.TagManager.TryGetNameFromID(id);
			}
			set
			{
				DunGenSettings.Instance.TagManager.TryRenameTag(id, value);
			}
		}

		public Tag(int id)
		{
			this.id = id;
		}

		public Tag(string name)
		{
			DunGenSettings.Instance.TagManager.TagExists(name, out id);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			Tag tag = obj as Tag;
			if (tag == null)
			{
				return false;
			}
			return Equals(this, tag);
		}

		public override int GetHashCode()
		{
			return id;
		}

		public override string ToString()
		{
			return $"[{id}] {DunGenSettings.Instance.TagManager.TryGetNameFromID(id)}";
		}

		public int GetHashCode(Tag tag)
		{
			return id;
		}

		public bool Equals(Tag x, Tag y)
		{
			if (x == null && y == null)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			return x.id == y.id;
		}

		public static bool operator ==(Tag a, Tag b)
		{
			if ((object)a == null && (object)b == null)
			{
				return true;
			}
			if ((object)a == null || (object)b == null)
			{
				return false;
			}
			return a.id == b.id;
		}

		public static bool operator !=(Tag a, Tag b)
		{
			if (a == null && b == null)
			{
				return false;
			}
			if (a == null && b != null)
			{
				return true;
			}
			return a.id != b.id;
		}
	}
}
