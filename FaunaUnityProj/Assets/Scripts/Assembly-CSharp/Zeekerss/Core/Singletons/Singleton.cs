using UnityEngine;

namespace Zeekerss.Core.Singletons
{
	public class Singleton<T> : MonoBehaviour where T : Component
	{
		private static T _instance;

		public static T Instance
		{
			get
			{
				if (_instance == null)
				{
					T[] array = Object.FindObjectsOfType(typeof(T)) as T[];
					if (array.Length != 0)
					{
						_instance = array[0];
					}
					if (array.Length > 1)
					{
						Debug.LogError("There is more than one " + typeof(T).Name + " in the scene.");
					}
					if (_instance == null)
					{
						_instance = new GameObject
						{
							name = $"_{typeof(T).Name}"
						}.AddComponent<T>();
					}
				}
				return _instance;
			}
		}
	}
}
