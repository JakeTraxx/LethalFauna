using System;
using System.Collections.Generic;
using UnityEngine;

namespace DunGen.Tags
{
	[Serializable]
	public sealed class TagManager : ISerializationCallbackReceiver
	{
		private Dictionary<int, string> tags = new Dictionary<int, string>();

		[SerializeField]
		private List<int> keys = new List<int>();

		[SerializeField]
		private List<string> values = new List<string>();

		public int TagCount => tags.Count;

		public string TryGetNameFromID(int id)
		{
			string value = null;
			tags.TryGetValue(id, out value);
			return value;
		}

		public bool TagExists(string name, out int id)
		{
			foreach (KeyValuePair<int, string> tag in tags)
			{
				if (tag.Value == name)
				{
					id = tag.Key;
					return true;
				}
			}
			id = -1;
			return false;
		}

		public bool TryRenameTag(int id, string newName)
		{
			if (!tags.TryGetValue(id, out var value))
			{
				return false;
			}
			if (value == newName)
			{
				return true;
			}
			if (TagExists(newName, out var _))
			{
				return false;
			}
			tags[id] = newName;
			return true;
		}

		public int AddTag(string tagName)
		{
			tagName = GetUnusedTagName(tagName);
			int num = 0;
			foreach (int key in tags.Keys)
			{
				num = Mathf.Max(num, key + 1);
			}
			tags[num] = tagName;
			return num;
		}

		private string GetUnusedTagName(string desiredTagName)
		{
			bool flag = false;
			foreach (KeyValuePair<int, string> tag in tags)
			{
				if (tag.Value == desiredTagName)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return desiredTagName;
			}
			int num = 2;
			string text = desiredTagName + " " + num;
			int id;
			while (TagExists(text, out id))
			{
				text = desiredTagName + " " + num;
				num++;
			}
			return text;
		}

		public bool RemoveTag(int id)
		{
			if (!tags.ContainsKey(id))
			{
				return false;
			}
			tags.Remove(id);
			return true;
		}

		public int[] GetTagIDs()
		{
			int[] array = new int[tags.Count];
			int num = 0;
			foreach (int key in tags.Keys)
			{
				array[num] = key;
				num++;
			}
			Array.Sort(array);
			return array;
		}

		public void OnAfterDeserialize()
		{
			tags = new Dictionary<int, string>();
			for (int i = 0; i < keys.Count; i++)
			{
				tags[keys[i]] = values[i];
			}
			keys.Clear();
			values.Clear();
		}

		public void OnBeforeSerialize()
		{
			keys = new List<int>();
			values = new List<string>();
			foreach (KeyValuePair<int, string> tag in tags)
			{
				keys.Add(tag.Key);
				values.Add(tag.Value);
			}
		}
	}
}
