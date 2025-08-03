using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DunGen
{
	public static class UnityUtil
	{
		public static Type ProBuilderMeshType { get; private set; }

		public static PropertyInfo ProBuilderPositionsProperty { get; private set; }

		static UnityUtil()
		{
			FindProBuilderObjectType();
		}

		public static void FindProBuilderObjectType()
		{
			if (ProBuilderMeshType != null)
			{
				return;
			}
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (!assembly.FullName.Contains("ProBuilder"))
				{
					continue;
				}
				ProBuilderMeshType = assembly.GetType("UnityEngine.ProBuilder.ProBuilderMesh");
				if (ProBuilderMeshType != null)
				{
					ProBuilderPositionsProperty = ProBuilderMeshType.GetProperty("positions");
					if (ProBuilderPositionsProperty != null)
					{
						break;
					}
				}
			}
		}

		public static void Restart(this Stopwatch stopwatch)
		{
			if (stopwatch == null)
			{
				stopwatch = Stopwatch.StartNew();
				return;
			}
			stopwatch.Reset();
			stopwatch.Start();
		}

		public static bool Contains(this Bounds bounds, Bounds other)
		{
			if (other.min.x < bounds.min.x || other.min.y < bounds.min.y || other.min.z < bounds.min.z || other.max.x > bounds.max.x || other.max.y > bounds.max.y || other.max.z > bounds.max.z)
			{
				return false;
			}
			return true;
		}

		public static Bounds TransformBounds(this Transform transform, Bounds localBounds)
		{
			Vector3 center = transform.TransformPoint(localBounds.center);
			Vector3 size = transform.rotation * localBounds.size;
			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);
			size.z = Mathf.Abs(size.z);
			return new Bounds(center, size);
		}

		public static Bounds InverseTransformBounds(this Transform transform, Bounds worldBounds)
		{
			Vector3 center = transform.InverseTransformPoint(worldBounds.center);
			Vector3 size = Quaternion.Inverse(transform.rotation) * worldBounds.size;
			size.x = Mathf.Abs(size.x);
			size.y = Mathf.Abs(size.y);
			size.z = Mathf.Abs(size.z);
			return new Bounds(center, size);
		}

		public static void SetLayerRecursive(GameObject gameObject, int layer)
		{
			gameObject.layer = layer;
			for (int i = 0; i < gameObject.transform.childCount; i++)
			{
				SetLayerRecursive(gameObject.transform.GetChild(i).gameObject, layer);
			}
		}

		public static void Destroy(UnityEngine.Object obj)
		{
			if (Application.isPlaying)
			{
				GameObject gameObject = obj as GameObject;
				if (gameObject != null)
				{
					gameObject.SetActive(value: false);
				}
				UnityEngine.Object.Destroy(obj);
			}
			else
			{
				UnityEngine.Object.DestroyImmediate(obj);
			}
		}

		public static string GetUniqueName(string name, IEnumerable<string> usedNames)
		{
			if (string.IsNullOrEmpty(name))
			{
				return GetUniqueName("New", usedNames);
			}
			string text = name;
			int result = 0;
			bool flag = false;
			int num = name.LastIndexOf(' ');
			if (num > -1)
			{
				text = name.Substring(0, num);
				flag = int.TryParse(name.Substring(num + 1), out result);
				result++;
			}
			foreach (string usedName in usedNames)
			{
				if (usedName == name)
				{
					if (flag)
					{
						return GetUniqueName(text + " " + result, usedNames);
					}
					return GetUniqueName(name + " 2", usedNames);
				}
			}
			return name;
		}

		public static Bounds CombineBounds(params Bounds[] bounds)
		{
			if (bounds.Length == 0)
			{
				return default(Bounds);
			}
			if (bounds.Length == 1)
			{
				return bounds[0];
			}
			Bounds result = bounds[0];
			for (int i = 1; i < bounds.Length; i++)
			{
				result.Encapsulate(bounds[i]);
			}
			return result;
		}

		public static Bounds CalculateProxyBounds(GameObject prefab, bool ignoreSpriteRendererBounds, Vector3 upVector)
		{
			Bounds result = CalculateObjectBounds(prefab, includeInactive: true, ignoreSpriteRendererBounds);
			if (ProBuilderMeshType != null && ProBuilderPositionsProperty != null)
			{
				Component[] componentsInChildren = prefab.GetComponentsInChildren(ProBuilderMeshType);
				foreach (Component obj in componentsInChildren)
				{
					Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
					Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
					foreach (Vector3 item in (IList<Vector3>)ProBuilderPositionsProperty.GetValue(obj, null))
					{
						vector = Vector3.Min(vector, item);
						vector2 = Vector3.Max(vector2, item);
					}
					Vector3 vector3 = prefab.transform.TransformDirection(vector2 - vector);
					Vector3 center = prefab.transform.TransformPoint(vector) + vector3 / 2f;
					result.Encapsulate(new Bounds(center, vector3));
				}
			}
			return result;
		}

		public static Bounds CalculateObjectBounds(GameObject obj, bool includeInactive, bool ignoreSpriteRenderers, bool ignoreTriggerColliders = true)
		{
			Bounds result = default(Bounds);
			bool flag = false;
			Tilemap[] componentsInChildren = obj.GetComponentsInChildren<Tilemap>(includeInactive);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].CompressBounds();
			}
			Renderer[] componentsInChildren2 = obj.GetComponentsInChildren<Renderer>(includeInactive);
			foreach (Renderer renderer in componentsInChildren2)
			{
				if ((!ignoreSpriteRenderers || !(renderer is SpriteRenderer)) && !(renderer is ParticleSystemRenderer))
				{
					if (flag)
					{
						result.Encapsulate(renderer.bounds);
					}
					else
					{
						result = renderer.bounds;
					}
					flag = true;
				}
			}
			Collider[] componentsInChildren3 = obj.GetComponentsInChildren<Collider>(includeInactive);
			foreach (Collider collider in componentsInChildren3)
			{
				if (!ignoreTriggerColliders || !collider.isTrigger)
				{
					if (flag)
					{
						result.Encapsulate(collider.bounds);
					}
					else
					{
						result = collider.bounds;
					}
					flag = true;
				}
			}
			Vector3 extents = result.extents;
			if (extents.x == 0f)
			{
				extents.x = 0.01f;
			}
			else if (extents.x < 0f)
			{
				extents.x *= -1f;
			}
			if (extents.y == 0f)
			{
				extents.y = 0.01f;
			}
			else if (extents.y < 0f)
			{
				extents.y *= -1f;
			}
			if (extents.z == 0f)
			{
				extents.z = 0.01f;
			}
			else if (extents.z < 0f)
			{
				extents.z *= -1f;
			}
			result.extents = extents;
			return result;
		}

		public static void PositionObjectBySocket(GameObject objectA, GameObject socketA, GameObject socketB)
		{
			PositionObjectBySocket(objectA.transform, socketA.transform, socketB.transform);
		}

		public static void PositionObjectBySocket(Transform objectA, Transform socketA, Transform socketB)
		{
			Quaternion quaternion = Quaternion.LookRotation(-socketB.forward, socketB.up);
			objectA.rotation = quaternion * Quaternion.Inverse(Quaternion.Inverse(objectA.rotation) * socketA.rotation);
			Vector3 position = socketB.position;
			objectA.position = position - (socketA.position - objectA.position);
		}

		public static Vector3 GetCardinalDirection(Vector3 direction, out float magnitude)
		{
			float num = Math.Abs(direction.x);
			float num2 = Math.Abs(direction.y);
			float num3 = Math.Abs(direction.z);
			float num4 = direction.x / num;
			float num5 = direction.y / num2;
			float num6 = direction.z / num3;
			if (num > num2 && num > num3)
			{
				magnitude = num4;
				return new Vector3(num4, 0f, 0f);
			}
			if (num2 > num && num2 > num3)
			{
				magnitude = num5;
				return new Vector3(0f, num5, 0f);
			}
			if (num3 > num && num3 > num2)
			{
				magnitude = num6;
				return new Vector3(0f, 0f, num6);
			}
			magnitude = num4;
			return new Vector3(num4, 0f, 0f);
		}

		public static Vector3 VectorAbs(Vector3 vector)
		{
			return new Vector3(Math.Abs(vector.x), Math.Abs(vector.y), Math.Abs(vector.z));
		}

		public static void SetVector3Masked(ref Vector3 input, Vector3 value, Vector3 mask)
		{
			if (mask.x != 0f)
			{
				input.x = value.x;
			}
			if (mask.y != 0f)
			{
				input.y = value.y;
			}
			if (mask.z != 0f)
			{
				input.z = value.z;
			}
		}

		public static Bounds CondenseBounds(Bounds bounds, IEnumerable<Doorway> doorways)
		{
			Vector3 input = bounds.center - bounds.extents;
			Vector3 input2 = bounds.center + bounds.extents;
			foreach (Doorway doorway in doorways)
			{
				float magnitude;
				Vector3 cardinalDirection = GetCardinalDirection(doorway.transform.forward, out magnitude);
				if (magnitude < 0f)
				{
					SetVector3Masked(ref input, doorway.transform.position, cardinalDirection);
				}
				else
				{
					SetVector3Masked(ref input2, doorway.transform.position, cardinalDirection);
				}
			}
			Vector3 vector = input2 - input;
			return new Bounds(input + vector / 2f, vector);
		}

		public static IEnumerable<T> GetComponentsInParents<T>(GameObject obj, bool includeInactive = false) where T : Component
		{
			if (obj.activeSelf || includeInactive)
			{
				T[] components = obj.GetComponents<T>();
				for (int i = 0; i < components.Length; i++)
				{
					yield return components[i];
				}
			}
			if (!(obj.transform.parent != null))
			{
				yield break;
			}
			foreach (T componentsInParent in GetComponentsInParents<T>(obj.transform.parent.gameObject, includeInactive))
			{
				yield return componentsInParent;
			}
		}

		public static T GetComponentInParents<T>(GameObject obj, bool includeInactive = false) where T : Component
		{
			if (obj.activeSelf || includeInactive)
			{
				T[] components = obj.GetComponents<T>();
				int num = 0;
				if (num < components.Length)
				{
					return components[num];
				}
			}
			if (obj.transform.parent != null)
			{
				return GetComponentInParents<T>(obj.transform.parent.gameObject, includeInactive);
			}
			return null;
		}

		public static float CalculateOverlap(Bounds boundsA, Bounds boundsB)
		{
			float num = boundsA.max.x - boundsB.min.x;
			float num2 = boundsB.max.x - boundsA.min.x;
			float num3 = boundsA.max.y - boundsB.min.y;
			float num4 = boundsB.max.y - boundsA.min.y;
			float num5 = boundsA.max.z - boundsB.min.z;
			float num6 = boundsB.max.z - boundsA.min.z;
			return Mathf.Min(num, num2, num3, num4, num5, num6);
		}

		public static Vector3 CalculatePerAxisOverlap(Bounds boundsA, Bounds boundsB)
		{
			float a = boundsA.max.x - boundsB.min.x;
			float b = boundsB.max.x - boundsA.min.x;
			float a2 = boundsA.max.y - boundsB.min.y;
			float b2 = boundsB.max.y - boundsA.min.y;
			float a3 = boundsA.max.z - boundsB.min.z;
			return new Vector3(z: Mathf.Min(a3, boundsB.max.z - boundsA.min.z), x: Mathf.Min(a, b), y: Mathf.Min(a2, b2));
		}
	}
}
