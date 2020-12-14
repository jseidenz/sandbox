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
    [SerializeField] Vector3 m_world_size_in_meters;
    [SerializeField] Vector3Int m_cube_dimensions;
    public Texture3D m_texture;
    byte[] m_texture_data;

    void Awake()
    {
        m_mesh = new Mesh();
        m_mesh.name = "SdfMesh";
        m_mesh.MarkDynamic();
        m_texture = new Texture3D(m_texture_dimensions.x, m_texture_dimensions.y, m_texture_dimensions.z, TextureFormat.R8, false);
        m_texture.filterMode = FilterMode.Bilinear;
        m_texture.wrapMode = TextureWrapMode.Clamp;

        m_texture_data = new byte[m_texture_dimensions.x * m_texture_dimensions.y * m_texture_dimensions.z];

        var densities = new HeightMapGenerator().GenerateHeightMap(m_texture_dimensions.x, m_texture_dimensions.z, 4f);

        float one_layer_height_in_density_space = (float)m_texture_dimensions.y;

        for (int layer_idx = 0; layer_idx < m_texture_dimensions.y; ++layer_idx)
        {
            float iso_level = layer_idx / (float)m_texture_dimensions.y;

            for (int z = 0; z < m_texture_dimensions.z; ++z)
            {
                for (int x = 0; x < m_texture_dimensions.x; ++x)
                {
                    var density_cell_idx = z * m_texture_dimensions.x + x;

                    float input_density = densities[density_cell_idx];
                    float deltaed_density = input_density - iso_level;
                    float normalized_density = deltaed_density * one_layer_height_in_density_space;
                    float clamped_density = 1f - Mathf.Clamp01(normalized_density);

                    byte density_byte = (byte)(clamped_density * 255f);
                    var pixel_idx = layer_idx * m_texture_dimensions.z * m_texture_dimensions.x + z * m_texture_dimensions.x + x;
                    m_texture_data[pixel_idx] = density_byte;
                }
            }
        }

        /*
        for (int layer_idx = 0; layer_idx < m_texture_dimensions.y; ++layer_idx)
        {
            for (int z = 0; z < m_texture_dimensions.z; ++z)
            {
                for (int x = 0; x < m_texture_dimensions.x; ++x)
                {
                    byte value = 255;

                    if ((Math.Abs(x - m_texture_dimensions.x / 2) < m_cube_dimensions.x) && (Math.Abs(layer_idx - m_texture_dimensions.y / 2) < m_cube_dimensions.y) && (Math.Abs(z - m_texture_dimensions.z / 2) < m_cube_dimensions.z))
                    {
                        value = 0;
                    }
                    
                    var pixel_idx = layer_idx * m_texture_dimensions.z * m_texture_dimensions.x + z * m_texture_dimensions.x + x;
                    m_texture_data[pixel_idx] = value;
                }
            }
        }
        */

    }

    void LateUpdate()
    {
        Profiler.BeginSample("SetPixelData");
        m_texture.SetPixelData(m_texture_data, 0, 0);
        Profiler.EndSample();
        Profiler.BeginSample("Apply");
        m_texture.Apply();
        Profiler.EndSample();


        m_material.SetTexture("_LiquidTex", m_texture);
        m_material.SetVector("_WorldSizeInMeters", m_world_size_in_meters);

        m_mesh.Clear();
        m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

        int vert_count = 8;

        var vertices = new Vertex[vert_count];

        const float cube_size = 1f;
        int vert_idx = 0;
        vertices[vert_idx++].m_position = new Vector3(-cube_size, -cube_size, -cube_size);
        vertices[vert_idx++].m_position = new Vector3(+cube_size, -cube_size, -cube_size);
        vertices[vert_idx++].m_position = new Vector3(+cube_size, +cube_size, -cube_size);
        vertices[vert_idx++].m_position = new Vector3(-cube_size, +cube_size, -cube_size);
        vertices[vert_idx++].m_position = new Vector3(-cube_size, +cube_size, +cube_size);
        vertices[vert_idx++].m_position = new Vector3(+cube_size, +cube_size, +cube_size);
        vertices[vert_idx++].m_position = new Vector3(+cube_size, -cube_size, +cube_size);
        vertices[vert_idx++].m_position = new Vector3(-cube_size, -cube_size, +cube_size);

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

        Graphics.DrawMesh(m_mesh, Matrix4x4.identity, m_material, 0, null, 0);
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