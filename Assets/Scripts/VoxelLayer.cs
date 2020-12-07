﻿using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class VoxelLayer
{
    public VoxelLayer(int width_in_voxels, int height_in_voxels, int voxel_chunk_dimensions, float voxel_size_in_meters, Material material, float iso_level, float bot_y, float top_y, Camera camera)
    {
        if (width_in_voxels % voxel_chunk_dimensions != 0) throw new System.Exception($"width_in_voxels={width_in_voxels} is not a multiple of voxel_chunk_dimensions={voxel_chunk_dimensions}");
        if (height_in_voxels % voxel_chunk_dimensions != 0) throw new System.Exception($"width_in_voxels={height_in_voxels} is not a multiple of voxel_chunk_dimensions={voxel_chunk_dimensions}");

        m_density_grid = new float[width_in_voxels * height_in_voxels];
        m_occlusion_grid = new bool[width_in_voxels * height_in_voxels];
        m_width_in_voxels = width_in_voxels;
        m_height_in_voxels = height_in_voxels;
        m_voxel_size_in_meters = voxel_size_in_meters;
        m_voxel_chunk_dimensions = voxel_chunk_dimensions;
        m_one_over_voxel_chunk_dimensions = 1f / (float)voxel_chunk_dimensions;
        m_bot_y = bot_y;
        m_top_y = top_y;

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

    public void Triangulate(VoxelChunk.ScratchBuffer scratch_buffer)
    {
        foreach(var chunk in m_voxel_chunks)
        {
            chunk.Triangulate(scratch_buffer);
        }
    }

    public void Triangulate(VoxelChunk.ScratchBuffer scratch_buffer, HashSet<int> dirty_chunk_indices, HashSet<int> dirty_chunk_occlusion_indices)
    {
        foreach(var chunk_idx in dirty_chunk_indices)
        {
            var chunk = m_voxel_chunks[chunk_idx];
            bool occlusion_dirtied = chunk.Triangulate(scratch_buffer);
            if(occlusion_dirtied)
            {
                dirty_chunk_occlusion_indices.Add(chunk_idx);
            }
        }
    }

    public void BindCamera(Camera camera)
    {
        m_culling_group = new CullingGroup();
        m_culling_group.targetCamera = camera;
        m_culling_group.onStateChanged += OnCullingGroupStateChanged;

        var bounding_spheres = new BoundingSphere[m_voxel_chunks.Length];
        int bounding_sphere_idx = 0;
        for (int chunk_y = 0; chunk_y < m_height_in_chunks; ++chunk_y)
        {
            for (int chunk_x = 0; chunk_x < m_width_in_chunks; ++chunk_x)
            {
                var world_left = chunk_x * m_voxel_chunk_dimensions * m_voxel_size_in_meters;
                var world_right = world_left + m_voxel_chunk_dimensions * m_voxel_size_in_meters;
                var world_near = chunk_y * m_voxel_chunk_dimensions * m_voxel_size_in_meters;
                var world_far = world_near + m_voxel_chunk_dimensions * m_voxel_size_in_meters;

                var pt_a = new Vector3(world_left, m_bot_y, world_near);
                var pt_b = new Vector3(world_right, m_top_y, world_far);

                var radius_vector = (pt_b - pt_a) * 0.5f;
                var center = pt_a + radius_vector;

                bounding_spheres[bounding_sphere_idx++] = new BoundingSphere(center, radius_vector.magnitude);
            }
        }

        m_culling_group.SetBoundingSpheres(bounding_spheres);
        m_culling_group.SetBoundingSphereCount(bounding_spheres.Length);
    }


    public void Render(float dt, Color color)
    {
        m_material.color = color;

        foreach(var chunk in m_visible_voxel_chunks)
        {
            chunk.Render(dt, m_material);
        }
    }

    public bool[] GetOcclusionGrid()
    {
        return m_occlusion_grid;
    }

    public void AddDensity(Vector3 pos, float amount, HashSet<int> dirty_chunk_indices)
    {
        var x = (int)(pos.x / m_voxel_size_in_meters);
        if (x < 0 || x > m_width_in_voxels) return;

        var y = (int)(pos.z / m_voxel_size_in_meters);
        if (y < 0 || y > m_height_in_voxels) return;

        var cell_idx = y * m_width_in_voxels + x;

        m_density_grid[cell_idx] = Mathf.Clamp01(m_density_grid[cell_idx] + amount);

        for(int i = -1; i <= 1; ++i)
        {
            for(int j = -1; j <= 1; ++j)
            {
                dirty_chunk_indices.Add(GetVoxelChunkIdxClamped(x + i, y + j));
            }
        }        
    }

    int GetVoxelChunkIdxClamped(int density_grid_x, int density_grid_y)
    {
        density_grid_x = System.Math.Min(System.Math.Max(density_grid_x, 0), m_width_in_voxels);
        density_grid_y = System.Math.Min(System.Math.Max(density_grid_y, 0), m_height_in_voxels);

        var chunk_grid_x = (int)(density_grid_x * m_one_over_voxel_chunk_dimensions);
        var chunk_grid_y = (int)(density_grid_y * m_one_over_voxel_chunk_dimensions);

        return chunk_grid_y * m_width_in_chunks + chunk_grid_x;
    }
    void OnCullingGroupStateChanged(CullingGroupEvent evt)
    {
        if(evt.hasBecomeVisible)
        {
            m_visible_voxel_chunks.Add(m_voxel_chunks[evt.index]);
        }
        else
        {
            m_visible_voxel_chunks.Remove(m_voxel_chunks[evt.index]);
        }
    }

    internal void OnDestroy()
    {
        m_culling_group.Dispose();
        m_culling_group = null;
    }

    bool[] m_layer_above_occlusion_grid;
    bool[] m_layer_below_occlusion_grid;
    float[] m_density_grid;
    bool[] m_occlusion_grid;
    int m_width_in_voxels;
    int m_height_in_voxels;
    int m_width_in_chunks;
    int m_height_in_chunks;
    CullingGroup m_culling_group;
    Material m_material;
    float m_voxel_size_in_meters;
    float m_one_over_voxel_chunk_dimensions;
    int m_voxel_chunk_dimensions;
    VoxelChunk[] m_voxel_chunks;
    float m_bot_y;
    float m_top_y;
    HashSet<VoxelChunk> m_visible_voxel_chunks = new HashSet<VoxelChunk>();

    VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
    };
}