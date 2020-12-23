using UnityEngine;
using System.Collections.Generic;

public class TorusMesh
{
	Mesh m_mesh;
	float m_radius;
	float m_thickness;
	int m_slices;
	int m_slice_tesselation;

	public TorusMesh(float radius, float thickness, int slices, int slice_tessellation)
    {
		m_mesh = new Mesh();
		m_mesh.name = "Torus";

		m_radius = radius;
		m_thickness = thickness;
		m_slices = slices;
		m_slice_tesselation = slice_tessellation;

		RebuildMesh();
    }

	public void Destroy()
    {
		GameObject.DestroyImmediate(m_mesh);
    }

	public void SetRadius(float new_radius)
    {
		m_radius = new_radius;
    }

	public void RebuildMesh()
    {
		var mesh = m_mesh;
		var radius = m_radius;
		var thickness = m_thickness;
		var slices = m_slices;
		var slice_tesselation = m_slice_tesselation;

		mesh.Clear();

		if (slices < 3) slices = 3;
		if (slice_tesselation < 3) slice_tesselation = 3;

		List<Vector3> vertices = new List<Vector3>();
		List<Vector3> normals = new List<Vector3>();
		List<Vector2> uvs = new List<Vector2>();
		List<int> indices = new List<int>();

		int circleVertexCount = slices + 1;
		int sliceVertexCount = slice_tesselation + 1;
		float pi = Mathf.PI;
		float twoPi = Mathf.PI * 2.0f;
		float invSlices = 1f / slices;
		float intSliceTessellation = 1f / slice_tesselation;
		Vector3 axisY = Vector3.up;

		for (int i = 0; i <= slices; ++i)
		{
			float u = i * invSlices;
			float circleAngle = u * twoPi;

			Vector3 axisX = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle));
			Vector3 center = new Vector3(axisX.x * radius, 0f, axisX.z * radius);

			for (int j = 0; j <= slice_tesselation; ++j)
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

		m_mesh.vertices = vertices.ToArray();
		m_mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
		m_mesh.normals = normals.ToArray();
		m_mesh.uv = uvs.ToArray();
	}
}