using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class VoxelLayer
{
    public VoxelLayer(int width_in_voxels, int height_in_voxels, int voxel_chunk_dimensions, float voxel_size_in_meters, Material material, float iso_level, float bot_y, float top_y)
    {
        if (width_in_voxels % voxel_chunk_dimensions != 0) throw new System.Exception($"width_in_voxels={width_in_voxels} is not a multiple of voxel_chunk_dimensions={voxel_chunk_dimensions}");
        if (height_in_voxels % voxel_chunk_dimensions != 0) throw new System.Exception($"width_in_voxels={height_in_voxels} is not a multiple of voxel_chunk_dimensions={voxel_chunk_dimensions}");

        m_density_grid = new float[width_in_voxels * height_in_voxels];
        m_occlusion_grid = new bool[width_in_voxels * height_in_voxels];
        m_width_in_voxels = width_in_voxels;
        m_height_in_voxels = height_in_voxels;
        m_voxel_size_in_meters = voxel_size_in_meters;

        m_material = material;

        m_width_in_chunks = width_in_voxels / voxel_chunk_dimensions;
        m_height_in_chunks = height_in_voxels / voxel_chunk_dimensions;
        m_voxel_chunks = new VoxelChunk[m_width_in_chunks * m_height_in_chunks];

        for(int y = 0; y < m_height_in_chunks; ++y)
        {
            for(int x = 0; x < m_width_in_chunks; ++x)
            {
                m_voxel_chunks[y * m_width_in_chunks + x] = new VoxelChunk(x * voxel_chunk_dimensions, y * voxel_chunk_dimensions, voxel_chunk_dimensions, width_in_voxels, height_in_voxels, m_density_grid, m_occlusion_grid, voxel_size_in_meters, iso_level, bot_y, top_y);
            }
        }
    }

    public void SetAboveAndBelowOcclusionGrids(bool[] layer_above_occlusion_grid, bool[] layer_below_occlusion_grid)
    {
        foreach(var chunk in m_voxel_chunks)
        {
            chunk.SetAboveAndBelowOcclusionGrids(layer_above_occlusion_grid, layer_below_occlusion_grid);
        }

        m_layer_above_occlusion_grid = layer_above_occlusion_grid;
        m_layer_below_occlusion_grid = layer_below_occlusion_grid;
    }

    public void ApplyHeightmap(float[] densities, float min_height, float max_height)
    {
        for (int y = 0; y < m_height_in_voxels; ++y)
        {
            for (int x = 0; x < m_width_in_voxels; ++x)
            {
                var cell_idx = y * m_width_in_voxels + x;

                m_density_grid[cell_idx] = densities[cell_idx];
            }
        }
    }

    public bool Triangulate(VoxelChunk.Vertex[] vertices_scratch_buffer, System.UInt16[] indices_screatch_buffer, VoxelChunk.Edge[] edge_scratch_space, Dictionary<int, VoxelChunk.WeldingInfo> welding_info_scratch_space)
    {
        bool has_occlusion_changed = false;
        foreach(var chunk in m_voxel_chunks)
        {
            var has_chunk_occlusion_changed = chunk.Triangulate(vertices_scratch_buffer, indices_screatch_buffer, edge_scratch_space, welding_info_scratch_space);
            has_occlusion_changed = has_occlusion_changed || has_chunk_occlusion_changed;
        }

        return has_occlusion_changed;
    }


    public void Render(float dt, Color color)
    {
        m_material.color = color;

        foreach(var chunk in m_voxel_chunks)
        {
            chunk.Render(dt, m_material);
        }
    }

    public bool[] GetOcclusionGrid()
    {
        return m_occlusion_grid;
    }

    public void AddDensity(Vector3 pos, float amount)
    {
        var x = (int)(pos.x / m_voxel_size_in_meters);
        if (x < 0 || x > m_width_in_voxels) return;

        var y = (int)(pos.z / m_voxel_size_in_meters);
        if (y < 0 || y > m_height_in_voxels) return;

        var cell_idx = y * m_width_in_voxels + x;

        m_density_grid[cell_idx] += amount;
    }

    bool[] m_layer_above_occlusion_grid;
    bool[] m_layer_below_occlusion_grid;
    float[] m_density_grid;
    bool[] m_occlusion_grid;
    int m_width_in_voxels;
    int m_height_in_voxels;
    int m_width_in_chunks;
    int m_height_in_chunks;
    Material m_material;
    float m_voxel_size_in_meters;
    VoxelChunk[] m_voxel_chunks;

    VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
    };
}