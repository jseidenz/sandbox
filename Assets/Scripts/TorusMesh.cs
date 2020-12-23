using UnityEngine;
using System.Collections.Generic;

public class TorusMesh
{
	public TorusMesh()
    {

    }

	public static Mesh CreateTorus(float radius, float thickness, int slices, int sliceTessellation, bool generateNormals, bool generateUVs)
	{
		Mesh m = new Mesh();
		m.name = "Torus";

		if (slices < 3) slices = 3;
		if (sliceTessellation < 3) sliceTessellation = 3;

		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> indices = new List<int>();

		int circleVertexCount = slices + 1;
		int sliceVertexCount = sliceTessellation + 1;
		float pi = Mathf.PI;
		float twoPi = Mathf.PI * 2.0f;
		float invSlices = 1f / slices;
		float intSliceTessellation = 1f / sliceTessellation;
		Vector3 axisY = Vector3.up;

		for (int i = 0; i <= slices; ++i)
		{
			float u = i * invSlices;
			float circleAngle = u * twoPi;

			Vector3 axisX = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle));
			Vector3 center = new Vector3(axisX.x * radius, 0f, axisX.z * radius);

			for (int j = 0; j <= sliceTessellation; ++j)
			{
				float v = j * intSliceTessellation;
				float tubeAngle = v * twoPi + pi;

				float x = Mathf.Cos(tubeAngle);
				float y = Mathf.Sin(tubeAngle);

				Vector3 xVector = x * axisX;
				Vector3 yVector = y * axisY;

				Vector3 vertex = xVector * thickness + yVector * thickness + center;
				Vector3 normal = xVector + yVector;
				Vector2 uv = new Vector2(u, v);

				vertices.Add(vertex);
				normals.Add(normal);
				uvs.Add(uv);

				int iNext = (i + 1) % circleVertexCount;
				int jNext = (j + 1) % sliceVertexCount;

				indices.Add(i * sliceVertexCount + j);
				indices.Add(i * sliceVertexCount + jNext);
				indices.Add(iNext * sliceVertexCount + j);

				indices.Add(i * sliceVertexCount + jNext);
				indices.Add(iNext * sliceVertexCount + jNext);
				indices.Add(iNext * sliceVertexCount + j);
			}
		}

		m.vertices = vertices.ToArray();
		m.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
		if (generateNormals)
		{
			m.normals = normals.ToArray();
		}
		if (generateUVs)
		{
			m.uv = uvs.ToArray();
		}

		return m;
	}
}