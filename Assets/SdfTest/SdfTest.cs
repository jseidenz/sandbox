using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public class SdfTest : MonoBehaviour
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
    [SerializeField] float m_radius;
    [SerializeField] float m_iso_dist;
    [SerializeField] int m_corner_idx;
    [SerializeField] Vector3Int m_texture_dimensions;
    [SerializeField] Vector3Int m_world_size_in_cells;
    [SerializeField] Vector3 m_cell_size_in_meters;
    [SerializeField] SdfTuning m_sdf_tuning;
    public Texture3D m_texture;
    byte[] m_texture_data;

    void Awake()
    {
        m_mesh = new Mesh();
        m_mesh.name = "SdfMesh";
        m_mesh.MarkDynamic();
        //m_texture = new Texture3D(m_texture_dimensions.x, m_texture_dimensions.y, m_texture_dimensions.z, TextureFormat.R8, false);
        m_texture = new Texture3D(m_texture_dimensions.x, m_texture_dimensions.y, m_texture_dimensions.z, UnityEngine.Experimental.Rendering.GraphicsFormat.R8_UNorm, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
        m_texture.filterMode = FilterMode.Bilinear;
        m_texture.wrapMode = TextureWrapMode.Clamp;

        m_texture_data = new byte[m_texture_dimensions.x * m_texture_dimensions.y * m_texture_dimensions.z];
        
        for(int i = 0; i < m_texture_data.Length; ++i)
        {
            var value = (byte)255;
            if(i == 13)
            {
                value = 0;
            }
            m_texture_data[i] = value;
        }
    }

    void LateUpdate()
    {
        m_sdf_tuning.ApplyParameters(m_material);

        Profiler.BeginSample("SetPixelData");
        m_texture.SetPixelData(m_texture_data, 0, 0);
        Profiler.EndSample();
        Profiler.BeginSample("Apply");
        m_texture.Apply();
        Profiler.EndSample();


        m_material.SetTexture("_LiquidTex", m_texture);

        var world_size_in_meters = new Vector3((float)m_world_size_in_cells.x * m_cell_size_in_meters.x, (float)m_world_size_in_cells.y * m_cell_size_in_meters.y, (float)m_world_size_in_cells.z * m_cell_size_in_meters.z);

        m_material.SetVector("_WorldSizeInMeters", world_size_in_meters);

        m_mesh.Clear();
        m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        int vert_count = 8;

        var vertices = new Vertex[vert_count];

        int vert_idx = 0;
        Vector3 cube_size = m_cell_size_in_meters;
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

        for (int i = 0; i < vert_count; ++i)
        {
            float radius = m_radius;
            if(m_corner_idx == i)
            {
                radius += m_iso_dist;
            }
            vertices[i].m_uv0 = new Vector2(radius, 0);
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

        var offset = m_sdf_tuning.m_offset * Vector3.one;
        var transform = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one);
        Graphics.DrawMesh(m_mesh, transform, m_material, 0, null, 0);
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