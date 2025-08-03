using UnityEngine;

namespace DunGen
{
	public static class DebugDraw
	{
		public static void Bounds(Bounds localBounds, Matrix4x4 transform, Color colour, float duration = 0f, bool depthTest = false)
		{
			Vector3 min = localBounds.min;
			Vector3 max = localBounds.max;
			Vector3 start = transform.MultiplyPoint(new Vector3(min.x, max.y, max.z));
			Vector3 vector = transform.MultiplyPoint(new Vector3(min.x, max.y, min.z));
			Vector3 vector2 = transform.MultiplyPoint(new Vector3(max.x, max.y, max.z));
			Vector3 vector3 = transform.MultiplyPoint(new Vector3(max.x, max.y, min.z));
			Vector3 vector4 = transform.MultiplyPoint(new Vector3(min.x, min.y, max.z));
			Vector3 vector5 = transform.MultiplyPoint(new Vector3(min.x, min.y, min.z));
			Vector3 vector6 = transform.MultiplyPoint(new Vector3(max.x, min.y, max.z));
			Vector3 end = transform.MultiplyPoint(new Vector3(max.x, min.y, min.z));
			Debug.DrawLine(start, vector, colour, duration, depthTest);
			Debug.DrawLine(start, vector2, colour, duration, depthTest);
			Debug.DrawLine(vector, vector3, colour, duration, depthTest);
			Debug.DrawLine(vector2, vector3, colour, duration, depthTest);
			Debug.DrawLine(vector4, vector5, colour, duration, depthTest);
			Debug.DrawLine(vector4, vector6, colour, duration, depthTest);
			Debug.DrawLine(vector5, end, colour, duration, depthTest);
			Debug.DrawLine(vector6, end, colour, duration, depthTest);
			Debug.DrawLine(start, vector4, colour, duration, depthTest);
			Debug.DrawLine(vector2, vector6, colour, duration, depthTest);
			Debug.DrawLine(vector3, end, colour, duration, depthTest);
			Debug.DrawLine(vector, vector5, colour, duration, depthTest);
		}
	}
}
