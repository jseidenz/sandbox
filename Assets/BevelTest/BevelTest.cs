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

    public class EdgeLoop
    {
        public EdgeLoop(int max_edge_count, int max_vert_count)
        {
            m_edge_map = new ushort[max_vert_count];
            m_edges = new Edge[max_edge_count];
        }

        public void AddEdge(ushort vert_idx_a, ushort vert_idx_b, ushort vert_idx_c, ushort vert_idx_d, ushort vert_idx_e, ushort vert_idx_f, ushort vert_idx_g)
        {
            var edge_idx = m_edge_count++;
            m_edges[edge_idx] = new Edge
            {
                m_vertex_idx_a = vert_idx_a,
                m_vertex_idx_b = vert_idx_b,
                m_vertex_idx_c = vert_idx_c,
                m_vertex_idx_d = vert_idx_d,
                m_vertex_idx_e = vert_idx_e,
                m_vertex_idx_f = vert_idx_f,
                m_vertex_idx_g = vert_idx_g,
            };

            m_edge_map[vert_idx_a] = edge_idx;
        }


        public int Count { get => m_edge_count; }

        public ushort m_edge_count;
        public ushort[] m_edge_map;
        public Edge[] m_edges;

        public struct Edge
        {
            public ushort m_vertex_idx_a;
            public ushort m_vertex_idx_b;
            public ushort m_vertex_idx_c;
            public ushort m_vertex_idx_d;
            public ushort m_vertex_idx_e;
            public ushort m_vertex_idx_f;
            public ushort m_vertex_idx_g;

        }
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
            2, 3, 4, //face top
	        2, 4, 5,
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
        var edge_loop = new EdgeLoop(4, 4 * 32);

        var vert_writer = VertWriter.Start();
        var triangle_writer = TriangleWriter.Start();

        var top_verts = new List<ushort>();

        var top_corners = new ushort[4];
        top_corners[0] = vert_writer.Write(new Vector3(0, 1, 0));
        top_corners[1] = vert_writer.Write(new Vector3(1, 1, 0));
        top_corners[2] = vert_writer.Write(new Vector3(0, 1, 1));
        top_corners[3] = vert_writer.Write(new Vector3(1, 1, 1));

        foreach(var top_corner_vert_idx in top_corners)
        {
            top_verts.Add(top_corner_vert_idx);
        }

        var edges = new VoxelChunk.Edge[4];
        edges[0] = new VoxelChunk.Edge { m_vertex_idx_a = top_corners[0], m_vertex_idx_b = top_corners[1] };
        edges[1] = new VoxelChunk.Edge { m_vertex_idx_a = top_corners[1], m_vertex_idx_b = top_corners[3] };
        edges[2] = new VoxelChunk.Edge { m_vertex_idx_a = top_corners[3], m_vertex_idx_b = top_corners[2] };
        edges[3] = new VoxelChunk.Edge { m_vertex_idx_a = top_corners[2], m_vertex_idx_b = top_corners[0] };

        for (int i = 0; i < edges.Length; ++i)
        {
            var original_edge = edges[i];

            var vert_idx_a = original_edge.m_vertex_idx_a;
            var vert_idx_b = original_edge.m_vertex_idx_b;

            var pos_a = vert_writer.m_positions[vert_idx_a];
            var pos_b = vert_writer.m_positions[vert_idx_b];
            var pos_for_normal = pos_a - Vector3.up;
            var top_normal = Vector3.Cross(pos_b - pos_a, pos_for_normal - pos_a).normalized;

            var vert_idx_c = vert_writer.Write(pos_a + top_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, m_bevel_tuning.m_extrusion_vertical_offset, 0));
            var vert_idx_d = vert_writer.Write(pos_b + top_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, m_bevel_tuning.m_extrusion_vertical_offset, 0));

            triangle_writer.Write(vert_idx_a, vert_idx_b, vert_idx_c);
            triangle_writer.Write(vert_idx_c, vert_idx_b, vert_idx_d);

            var vert_idx_e = vert_writer.Write(pos_a + top_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, -1 + m_bevel_tuning.m_extrusion_lower_vertical_offset, 0));
            var vert_idx_f = vert_writer.Write(pos_b + top_normal * m_bevel_tuning.m_extrusion_distance + new Vector3(0, -1 + m_bevel_tuning.m_extrusion_lower_vertical_offset, 0));

            triangle_writer.Write(vert_idx_c, vert_idx_d, vert_idx_e);
            triangle_writer.Write(vert_idx_e, vert_idx_d, vert_idx_f);

            var vert_idx_g = vert_writer.Write(pos_a + new Vector3(0, -1, 0));

            edge_loop.AddEdge(vert_idx_a, vert_idx_b, vert_idx_c, vert_idx_d, vert_idx_e, vert_idx_f, vert_idx_g);
        }

        for(int i = 0; i < edge_loop.Count; ++i)
        {
            var start_edge = edge_loop.m_edges[i];
            var end_edge = edge_loop.m_edges[edge_loop.m_edge_map[start_edge.m_vertex_idx_b]];

            triangle_writer.Write(start_edge.m_vertex_idx_d, start_edge.m_vertex_idx_b, end_edge.m_vertex_idx_c);

            triangle_writer.Write(start_edge.m_vertex_idx_d, end_edge.m_vertex_idx_c, start_edge.m_vertex_idx_f);
            triangle_writer.Write(start_edge.m_vertex_idx_f, end_edge.m_vertex_idx_c, end_edge.m_vertex_idx_e);

            triangle_writer.Write(start_edge.m_vertex_idx_e, start_edge.m_vertex_idx_f, start_edge.m_vertex_idx_g);
            triangle_writer.Write(start_edge.m_vertex_idx_g, start_edge.m_vertex_idx_f, end_edge.m_vertex_idx_g);

            triangle_writer.Write(end_edge.m_vertex_idx_g, start_edge.m_vertex_idx_f, end_edge.m_vertex_idx_e);
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
            v.m_normal = (normal_accumulator[i]).normalized;
            vertices[i] = v;
        }

        foreach (var top_vert_idx in top_verts)
        {
            vertices[top_vert_idx].m_normal = Vector3.up;
        }

        var edge_mesh = m_mesh2;
        edge_mesh.SetVertexBufferParams(vertices.Length, m_vertex_attribute_descriptors);
        edge_mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, m_mesh_update_flags);
        edge_mesh.SetTriangles(triangles, 0, triangles.Length, 0, false);
        edge_mesh.RecalculateBounds();
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