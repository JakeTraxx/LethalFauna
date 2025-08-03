using UnityEngine;

namespace DigitalRuby.ThunderAndLightning
{
	public class MeshHelper
	{
		private Mesh mesh;

		private int[] triangles;

		private Vector3[] vertices;

		private Vector3[] normals;

		private float[] normalizedAreaWeights;

		public Mesh Mesh => mesh;

		public int[] Triangles => triangles;

		public Vector3[] Vertices => vertices;

		public Vector3[] Normals => normals;

		public MeshHelper(Mesh mesh)
		{
			this.mesh = mesh;
			triangles = mesh.triangles;
			vertices = mesh.vertices;
			normals = mesh.normals;
			CalculateNormalizedAreaWeights();
		}

		public void GenerateRandomPoint(ref RaycastHit hit, out int triangleIndex)
		{
			triangleIndex = SelectRandomTriangle();
			GetRaycastFromTriangleIndex(triangleIndex, ref hit);
		}

		public void GetRaycastFromTriangleIndex(int triangleIndex, ref RaycastHit hit)
		{
			Vector3 barycentricCoordinate = GenerateRandomBarycentricCoordinates();
			Vector3 vector = vertices[triangles[triangleIndex]];
			Vector3 vector2 = vertices[triangles[triangleIndex + 1]];
			Vector3 vector3 = vertices[triangles[triangleIndex + 2]];
			hit.barycentricCoordinate = barycentricCoordinate;
			hit.point = vector * barycentricCoordinate.x + vector2 * barycentricCoordinate.y + vector3 * barycentricCoordinate.z;
			if (normals == null)
			{
				hit.normal = Vector3.Cross(vector3 - vector2, vector - vector2).normalized;
				return;
			}
			vector = normals[triangles[triangleIndex]];
			vector2 = normals[triangles[triangleIndex + 1]];
			vector3 = normals[triangles[triangleIndex + 2]];
			hit.normal = vector * barycentricCoordinate.x + vector2 * barycentricCoordinate.y + vector3 * barycentricCoordinate.z;
		}

		private float[] CalculateSurfaceAreas(out float totalSurfaceArea)
		{
			int num = 0;
			totalSurfaceArea = 0f;
			float[] array = new float[triangles.Length / 3];
			for (int i = 0; i < triangles.Length; i += 3)
			{
				Vector3 vector = vertices[triangles[i]];
				Vector3 vector2 = vertices[triangles[i + 1]];
				Vector3 vector3 = vertices[triangles[i + 2]];
				float sqrMagnitude = (vector - vector2).sqrMagnitude;
				float sqrMagnitude2 = (vector - vector3).sqrMagnitude;
				float sqrMagnitude3 = (vector2 - vector3).sqrMagnitude;
				float num2 = PathGenerator.SquareRoot((2f * sqrMagnitude * sqrMagnitude2 + 2f * sqrMagnitude2 * sqrMagnitude3 + 2f * sqrMagnitude3 * sqrMagnitude - sqrMagnitude * sqrMagnitude - sqrMagnitude2 * sqrMagnitude2 - sqrMagnitude3 * sqrMagnitude3) / 16f);
				array[num++] = num2;
				totalSurfaceArea += num2;
			}
			return array;
		}

		private void CalculateNormalizedAreaWeights()
		{
			normalizedAreaWeights = CalculateSurfaceAreas(out var totalSurfaceArea);
			if (normalizedAreaWeights.Length != 0)
			{
				float num = 0f;
				for (int i = 0; i < normalizedAreaWeights.Length; i++)
				{
					float num2 = normalizedAreaWeights[i] / totalSurfaceArea;
					normalizedAreaWeights[i] = num;
					num += num2;
				}
			}
		}

		private int SelectRandomTriangle()
		{
			float value = Random.value;
			int num = 0;
			int num2 = normalizedAreaWeights.Length - 1;
			while (num < num2)
			{
				int num3 = (num + num2) / 2;
				if (normalizedAreaWeights[num3] < value)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3;
				}
			}
			return num * 3;
		}

		private Vector3 GenerateRandomBarycentricCoordinates()
		{
			Vector3 vector = new Vector3(Random.Range(Mathf.Epsilon, 1f), Random.Range(Mathf.Epsilon, 1f), Random.Range(Mathf.Epsilon, 1f));
			return vector / (vector.x + vector.y + vector.z);
		}
	}
}
