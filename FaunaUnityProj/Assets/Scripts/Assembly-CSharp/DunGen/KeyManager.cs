using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace DunGen
{
	[Serializable]
	[CreateAssetMenu(menuName = "DunGen/Key Manager", order = 700)]
	public sealed class KeyManager : ScriptableObject
	{
		private ReadOnlyCollection<Key> keysReadOnly;

		[SerializeField]
		private List<Key> keys = new List<Key>();

		public ReadOnlyCollection<Key> Keys
		{
			get
			{
				if (keysReadOnly == null)
				{
					keysReadOnly = new ReadOnlyCollection<Key>(keys);
				}
				return keysReadOnly;
			}
		}

		public Key CreateKey()
		{
			Key key = new Key(GetNextAvailableID());
			key.Name = UnityUtil.GetUniqueName("New Key", keys.Select((Key x) => x.Name));
			key.Colour = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
			keys.Add(key);
			return key;
		}

		public void DeleteKey(int index)
		{
			keys.RemoveAt(index);
		}

		public Key GetKeyByID(int id)
		{
			return keys.Where((Key x) => x.ID == id).FirstOrDefault();
		}

		public Key GetKeyByName(string name)
		{
			return keys.Where((Key x) => x.Name == name).FirstOrDefault();
		}

		public bool RenameKey(int index, string newName)
		{
			if (keys[index].Name == newName)
			{
				return false;
			}
			newName = UnityUtil.GetUniqueName(newName, keys.Select((Key x) => x.Name));
			keys[index].Name = newName;
			return true;
		}

		private int GetNextAvailableID()
		{
			int num = 0;
			foreach (Key item in keys.OrderBy((Key x) => x.ID))
			{
				if (item.ID >= num)
				{
					num = item.ID + 1;
				}
			}
			return num;
		}
	}
}
