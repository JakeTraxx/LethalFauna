using System;
using System.Collections;
using System.Collections.Generic;

namespace DunGen.Tags
{
	[Serializable]
	public sealed class TagContainer : IEnumerable<Tag>, IEnumerable
	{
		public List<Tag> Tags = new List<Tag>();

		public bool HasTag(Tag tag)
		{
			return Tags.Contains(tag);
		}

		public bool HasAnyTag(params Tag[] tags)
		{
			foreach (Tag tag in tags)
			{
				if (HasTag(tag))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAnyTag(TagContainer tags)
		{
			foreach (Tag tag in tags)
			{
				if (HasTag(tag))
				{
					return true;
				}
			}
			return false;
		}

		public bool HasAllTags(params Tag[] tags)
		{
			bool result = true;
			foreach (Tag tag in tags)
			{
				if (!HasTag(tag))
				{
					result = false;
					break;
				}
			}
			return result;
		}

		public bool HasAllTags(TagContainer tags)
		{
			bool result = true;
			foreach (Tag tag in tags)
			{
				if (!HasTag(tag))
				{
					result = false;
					break;
				}
			}
			return result;
		}

		public IEnumerator<Tag> GetEnumerator()
		{
			return Tags.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Tags.GetEnumerator();
		}
	}
}
