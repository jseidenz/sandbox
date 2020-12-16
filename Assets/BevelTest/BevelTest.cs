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
    [SerializeField] Material m_material;
    [SerializeField] BevelTuning m_bevel_tuning;

    void Awake()
    {
        m_mesh = new Mesh();
        m_mesh.name = "BevelMesh";
        m_mesh.MarkDynamic();
    }

    void LateUpdate()
    {
        m_bevel_tuning.ApplyParameters(m_material);

        m_mesh.Clear();
        m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        int vert_count = 8;

        var vertices = new Vertex[vert_count];

        int vert_idx = 0;
        Vector3 cube_size = Vector3.one;
        vertices[vert_idx++].m_position = new Vector3(0, 0, 0);
        vertices[vert_idx++].m_position = new Vector3(cube_size.x, 0, 0);
        vertices[vert_idx++].m_position = new Vector3(cube_size.x, cube_size.y, 0);
        vertices[vert_idx++].m_position = new Vector3(0, cube_size.y, 0);
        vertices[vert_idx++].m_position = new Vector3(0, cube_size.y, cube_size.z);
        vertices[vert_idx++].m_position = new Vector3(cube_size.x, cube_size.y, cube_size.z);
        vertices[vert_idx++].m_position = new Vector3(cube_size.x, 0, cube_size.z);
        vertices[vert_idx++].m_position = new Vector3(0, 0, cube_size.z);

        for(int i = 0; i < vert_count; ++i)
        {
            vertices[i].m_normal = vertices[i].m_position.normalized;
        }

        var triangles = new ushort[]
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


        m_mesh.SetVertexBufferParams(vert_count, m_vertex_attribute_descriptors);

        m_mesh.SetVertexBufferData(vertices, 0, 0, vert_count, 0, m_mesh_update_flags);
        m_mesh.SetTriangles(triangles, 0, triangles.Length, 0, false);
        m_mesh.RecalculateNormals();
        m_mesh.RecalculateTangents();

        Graphics.DrawMesh(m_mesh, transform.localToWorldMatrix, m_material, 0, null, 0);
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