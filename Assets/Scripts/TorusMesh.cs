using UnityEngine;
using System.Collections.Generic;

public class TorusMesh
{
	Mesh m_mesh;
	float m_radius;
	float m_thickness;
	int m_slices;
	int m_slice_tesselation;

	List<Vector3> m_vertices = new List<Vector3>();
	List<Vector3> m_normals = new List<Vector3>();
	List<Vector2> m_uvs = new List<Vector2>();
	List<int> m_indices = new List<int>();

	public TorusMesh(float radius, float thickness, int tesselation)
    {
		m_mesh = new Mesh();
		m_mesh.name = "Torus";

		m_radius = radius;
		m_thickness = thickness;
		m_slices = tesselation * 2;
		m_slice_tesselation = tesselation;

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

		var vertices = m_vertices;
		var normals = m_normals;
		var uvs = m_uvs;
		var indices = m_indices;

		m_vertices.Clear();
		m_normals.Clear();
		m_uvs.Clear();
		m_indices.Clear();

		int circle_vertex_count = slices + 1;
		int slive_vertex_count = slice_tesselation + 1;
		float pi = Mathf.PI;
		float two_pi = Mathf.PI * 2.0f;
		float inv_slices = 1f / slices;
		float inv_slice_tesselation = 1f / slice_tesselation;
		var axis_y = Vector3.up;

		for (int i = 0; i <= slices; ++i)
		{
			float u = i * inv_slices;
			float circleAngle = u * two_pi;

			var axis_x = new Vector3(Mathf.Cos(circleAngle), 0f, Mathf.Sin(circleAngle));
			var center = new Vector3(axis_x.x * radius, 0f, axis_x.z * radius);

			for (int j = 0; j <= slice_tesselation; ++j)
			{
				float v = j * inv_slice_tesselation;
				float tubeAngle = v * two_pi + pi;

				float x = Mathf.Cos(tubeAngle);
				float y = Mathf.Sin(tubeAngle);

				var x_vector = x * axis_x;
				var y_vector = y * axis_y;

				var vertex = x_vector * thickness + y_vector * thickness + center;
				var normal = x_vector + y_vector;
				var uv = new Vector2(u, v);

				vertices.Add(vertex);
				normals.Add(normal);
				uvs.Add(uv);

				int i_next = (i + 1) % circle_vertex_count;
				int jNext = (j + 1) % slive_vertex_count;

				indices.Add(i * slive_vertex_count + j);
				indices.Add(i * slive_vertex_count + jNext);
				indices.Add(i_next * slive_vertex_count + j);

				indices.Add(i * slive_vertex_count + jNext);
				indices.Add(i_next * slive_vertex_count + jNext);
				indices.Add(i_next * slive_vertex_count + j);
			}
		}

		mesh.SetVertices(m_vertices);
		mesh.SetNormals(m_normals);
		mesh.SetUVs(0, m_uvs);
		mesh.SetIndices(m_indices, MeshTopology.Triangles, 0);
	}
}