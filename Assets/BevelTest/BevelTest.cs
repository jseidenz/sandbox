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

    public struct VertWriter
    {
        public ushort Write(Vector3 point)
        {
            var vert_idx = (ushort)m_positions.Count;
            m_positions.Add(point);
            return vert_idx;
        }

        public static VertWriter Start()
        {
            var writer = new VertWriter();
            writer.m_positions = new List<Vector3>();
            return writer;
        }

        public Vector3 this[int idx] { get => m_positions[idx]; }

        public int Count { get => m_positions.Count; }

        public List<Vector3> m_positions;
    }

    public struct TriangleWriter
    {
        public void Write(ushort vert_idx)
        {
            m_triangles.Add(vert_idx);
        }

        public void Write(ushort vert_idx0, ushort vert_idx1, ushort vert_idx2)
        {
            m_triangles.Add(vert_idx0);
            m_triangles.Add(vert_idx1);
            m_triangles.Add(vert_idx2);
        }

        public static TriangleWriter Start()
        {
            var writer = new TriangleWriter();
            writer.m_triangles = new List<ushort>();
            return writer;
        }

        public ushort this[int idx] { get => m_triangles[idx]; }

        public int Count { get => m_triangles.Count; }

        public List<ushort> m_triangles;
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

        for (int i = 0; i < triangles.Length; i+=3)
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
        m_mesh.RecalculateBounds();
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

        var vert_writer = VertWriter.Start();
        var triangle_writer = TriangleWriter.Start();


        var top_verts = new List<ushort>();

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

            var idx0 = vert_writer.Write(left_points[0]);
            var idx1 = vert_writer.Write(right_points[0]);
            top_verts.Add(idx0);
            top_verts.Add(idx1);
            for (int j = 1; j < left_points.Count - 1; ++j)
            {
                var idx2 = vert_writer.Write(left_points[j + 1]);
                var idx3 = vert_writer.Write(right_points[j + 1]);

                triangle_writer.Write(idx0, idx1, idx2);
                triangle_writer.Write(idx2, idx1, idx3);

                idx0 = idx2;
                idx1 = idx3;
            }

            var bottom_idx0 = vert_writer.Write(ev0.m_position + ev0.m_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, -1, 0));
            var bottom_idx1 = vert_writer.Write(ev1.m_position + ev1.m_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, -1, 0));

            triangle_writer.Write(idx0, idx1, bottom_idx0);
            triangle_writer.Write(bottom_idx0, idx1, bottom_idx1);
        }

        var vertices = new Vertex[vert_writer.Count];
        for(int i = 0; i < vert_writer.Count; ++i)
        {
            vertices[i] = new Vertex { m_position = vert_writer[i] };                
        }


        var triangles = new ushort[triangle_writer.Count];
        for (int i = 0; i < triangle_writer.Count; ++i)
        {
            triangles[i] = triangle_writer[i];
        }

        var normal_accumulator = new Vector3[vert_writer.Count];

        for (int i = 0; i < triangles.Length; i += 3)
        {
            var vert_idx0 = triangles[i + 0];
            var vert_idx1 = triangles[i + 1];
            var vert_idx2 = triangles[i + 2];

            var v0 = vertices[vert_idx0];
            var v1 = vertices[vert_idx1];
            var v2 = vertices[vert_idx2];
            var normal = Vector3.Cross(v1.m_position - v0.m_position, v2.m_position - v0.m_position);

            normal_accumulator[vert_idx0] = normal_accumulator[vert_idx0] + normal;
            normal_accumulator[vert_idx1] = normal_accumulator[vert_idx1] + normal;
            normal_accumulator[vert_idx2] = normal_accumulator[vert_idx2] + normal;
        }

        for(int i = 0; i < vertices.Length; ++i)
        {
            var v = vertices[i];
            v.m_normal = normal_accumulator[i].normalized;
            vertices[i] = v;
        }

        foreach(var top_vert_idx in top_verts)
        {
            vertices[top_vert_idx].m_normal = Vector3.up;
        }


        var edge_mesh = m_mesh2;
        edge_mesh.SetVertexBufferParams(vertices.Length, m_vertex_attribute_descriptors);
        edge_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, m_mesh_update_flags);
        edge_mesh.SetTriangles(triangles, 0, triangles.Length, 0, false);
        edge_mesh.RecalculateBounds();
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
            points.Add(p_curr + (p_prev - p_curr) * 0.25f);
            points.Add(p_curr + (p_next - p_curr) * 0.25f);
        }
        
        points.RemoveRange(1, existing_point_count - 1);
        points.Add(end);
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