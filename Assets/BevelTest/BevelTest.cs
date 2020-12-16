using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class BevelTest : MonoBehaviour
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 m_position;
        public Vector3 m_normal;
        public Vector2 m_uv0;
    }

    Mesh m_mesh;
    Mesh m_mesh2;
    [SerializeField] Material m_material;
    [SerializeField] BevelTuning m_bevel_tuning;

    void Awake()
    {
        m_mesh = new Mesh();
        m_mesh.name = "BevelMesh";
        m_mesh.MarkDynamic();


        m_mesh2 = new Mesh();
        m_mesh2.name = "BevelEdge";
        m_mesh2.MarkDynamic();
    }

    void LateUpdate()
    {
        m_bevel_tuning.ApplyParameters(m_material);

        m_mesh.Clear();
        m_mesh.indexFormat = IndexFormat.UInt16;

        m_mesh2.Clear();
        m_mesh2.indexFormat = IndexFormat.UInt16;

        Vector3 cube_size = Vector3.one;
        var corners = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(cube_size.x, 0, 0),
            new Vector3(cube_size.x, cube_size.y, 0),
            new Vector3(0, cube_size.y, 0),
            new Vector3(0, cube_size.y, cube_size.z),
            new Vector3(cube_size.x, cube_size.y, cube_size.z),
            new Vector3(cube_size.x, 0, cube_size.z),
            new Vector3(0, 0, cube_size.z)
        };


        var faces = new ushort[]
        {
            0, 2, 1, //face front
	        0, 3, 2,
            2, 3, 4, //face top
	        2, 4, 5,
            1, 2, 5, //face right
	        1, 5, 6,
            0, 7, 4, //face left
	        0, 4, 3,
            5, 4, 7, //face back
	        5, 7, 6,
            0, 6, 7, //face bottom
	        0, 1, 6
        };

        var triangles = new ushort[faces.Length];
        var vertices = new Vertex[faces.Length];
        for(int i = 0; i < vertices.Length; ++i)
        {
            var pos = corners[faces[i]];
            vertices[i] = new Vertex
            {
                m_position = pos,
                m_normal = pos.normalized
            };
            triangles[i] = (ushort)i;
        }

        for(int i = 0; i < triangles.Length; i+=3)
        {
            var idx0 = triangles[i + 0];
            var idx1 = triangles[i + 1];
            var idx2 = triangles[i + 2];

            var v0 = vertices[idx0];
            var v1 = vertices[idx1];
            var v2 = vertices[idx2];
            var normal = Vector3.Cross(v1.m_position - v0.m_position, v2.m_position - v0.m_position).normalized;

            v0.m_normal = normal;
            v1.m_normal = normal;
            v2.m_normal = normal;

            vertices[idx0] = v0;
            vertices[idx1] = v1;
            vertices[idx2] = v2;
        }

        m_mesh.SetVertexBufferParams(vertices.Length, m_vertex_attribute_descriptors);
        m_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, m_mesh_update_flags);
        m_mesh.SetTriangles(triangles, 0, triangles.Length, 0, false);

        CreateEdgeMesh(vertices);

        Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 0, null, 0);
        Graphics.DrawMesh(m_mesh2, transform.localToWorldMatrix, m_material, 0, null, 0);
        
    }

    void CreateEdgeMesh(Vertex[] cube_vertices)
    {
        var edges = new VoxelChunk.Edge[4];
        edges[0] = new VoxelChunk.Edge { m_vertex_idx_a = 4, m_vertex_idx_b = 5 };
        edges[1] = new VoxelChunk.Edge { m_vertex_idx_a = 13, m_vertex_idx_b = 14 };
        edges[2] = new VoxelChunk.Edge { m_vertex_idx_a = 22, m_vertex_idx_b = 23 };
        edges[3] = new VoxelChunk.Edge { m_vertex_idx_a = 24, m_vertex_idx_b = 25 };

        var rectangle_verts = new List<Vertex>();
        var rectangle_triangles = new List<ushort>();

        for (int i = 0; i < edges.Length; ++i)
        {
            var edge = edges[i];
            var ev0 = cube_vertices[edge.m_vertex_idx_a];
            var ev1 = cube_vertices[edge.m_vertex_idx_b];
            var ev2 = ev0;
            var ev3 = ev1;
            ev2.m_position = ev2.m_position + ev2.m_normal * m_bevel_tuning.m_extrusion_distance;
            ev3.m_position = ev3.m_position + ev3.m_normal * m_bevel_tuning.m_extrusion_distance;
            ev2.m_position.y += m_bevel_tuning.m_extrusion_vertical_offset;
            ev3.m_position.y += m_bevel_tuning.m_extrusion_vertical_offset;

            var left_points = new List<Vector3>();
            var right_points = new List<Vector3>();
            left_points.Add(ev0.m_position);
            left_points.Add(ev0.m_position + ev0.m_normal * m_bevel_tuning.m_extrusion_distance);
            left_points.Add(ev0.m_position + ev0.m_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, m_bevel_tuning.m_extrusion_vertical_offset, 0));

            right_points.Add(ev1.m_position);
            right_points.Add(ev1.m_position + ev1.m_normal * m_bevel_tuning.m_extrusion_distance);
            right_points.Add(ev1.m_position + ev1.m_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, m_bevel_tuning.m_extrusion_vertical_offset, 0));

            for(int j = 0; j < m_bevel_tuning.m_subdivision_count; ++j)
            {
                Subdivide(left_points);
                Subdivide(right_points);
            }

            CreateRectangle(ev0.m_position, ev1.m_position, ev2.m_position, ev3.m_position, rectangle_verts, rectangle_triangles);
        }

        var vertices = new Vertex[rectangle_verts.Count];
        for(int i = 0; i < rectangle_verts.Count; ++i)
        {
            vertices[i] = rectangle_verts[i];
        }


        var triangles = new ushort[rectangle_triangles.Count];
        for (int i = 0; i < rectangle_triangles.Count; ++i)
        {
            triangles[i] = rectangle_triangles[i];
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            var idx0 = triangles[i + 0];
            var idx1 = triangles[i + 1];
            var idx2 = triangles[i + 2];

            var v0 = vertices[idx0];
            var v1 = vertices[idx1];
            var v2 = vertices[idx2];
            var normal = Vector3.Cross(v1.m_position - v0.m_position, v2.m_position - v0.m_position).normalized;

            v0.m_normal = normal;
            v1.m_normal = normal;
            v2.m_normal = normal;

            vertices[idx0] = v0;
            vertices[idx1] = v1;
            vertices[idx2] = v2;
        }

        var edge_mesh = m_mesh2;
        edge_mesh.SetVertexBufferParams(vertices.Length, m_vertex_attribute_descriptors);
        edge_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, m_mesh_update_flags);
        edge_mesh.SetTriangles(triangles, 0, triangles.Length, 0, false);
    }

    public void Subdivide(List<Vector3> points)
    {
        int existing_point_count = points.Count;
        var end = points[points.Count - 1];
        for (int i = 1; i < existing_point_count - 1; i++)
        {
            var p_prev = points[i - 1];
            var p_curr = points[i + 0];
            var p_next = points[i + 1];
            points.Add((p_prev - p_curr) * 0.25f);
            points.Add((p_next - p_curr) * 0.25f);
        }
        
        points.RemoveRange(1, existing_point_count - 1);
        points.Add(end);
    }

    public void CreateRectangle(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, List<Vertex> vertices, List<ushort> indices)
    {
        var starting_vert_idx = (ushort)vertices.Count;
        vertices.Add(new Vertex
        {
            m_position = p0
        });

        vertices.Add(new Vertex
        {
            m_position = p1
        });

        vertices.Add(new Vertex
        {
            m_position = p2
        });

        vertices.Add(new Vertex
        {
            m_position = p3
        });

        indices.Add((ushort)(starting_vert_idx + 0));
        indices.Add((ushort)(starting_vert_idx + 1));
        indices.Add((ushort)(starting_vert_idx + 2));
        indices.Add((ushort)(starting_vert_idx + 2));
        indices.Add((ushort)(starting_vert_idx + 1));
        indices.Add((ushort)(starting_vert_idx + 3));
    }

    VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
    };

    MeshUpdateFlags m_mesh_update_flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds
#if !UNITY_EDITOR
        | MeshUpdateFlags.DontValidateIndices
#endif
        ;
}