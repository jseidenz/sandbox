﻿using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class VoxelLayer
{
    static int COLOR_ID = Shader.PropertyToID("_Color");

    struct BoundsEntry
    {
        public Bounds m_bounds;
        public bool m_is_visible;
    }

    public VoxelLayer(
        string name, 
        float[] density_grid, 
        int layer_idx, 
        int width_in_voxels, 
        int height_in_voxels, 
        int voxel_chunk_dimensions, 
        Vector3 voxel_size_in_meters, 
        float iso_level, 
        float bot_y, 
        float top_y, 
        bool generate_collision, 
        float density_height_weight, 
        VertexAttributeDescriptor[] vertex_attribute_descriptors,
        bool is_liquid,
        BevelTuning bevel_tuning
        )
    {
        if (width_in_voxels % voxel_chunk_dimensions != 0) throw new System.Exception($"width_in_voxels={width_in_voxels} is not a multiple of voxel_chunk_dimensions={voxel_chunk_dimensions}");
        if (height_in_voxels % voxel_chunk_dimensions != 0) throw new System.Exception($"width_in_voxels={height_in_voxels} is not a multiple of voxel_chunk_dimensions={voxel_chunk_dimensions}");

        m_density_grid = density_grid;
        m_sample_grid = new byte[width_in_voxels * height_in_voxels];
        m_width_in_voxels = width_in_voxels;
        m_height_in_voxels = height_in_voxels;
        m_voxel_size_in_meters = voxel_size_in_meters;
        m_voxel_chunk_dimensions = voxel_chunk_dimensions;
        m_one_over_voxel_chunk_dimensions = 1f / (float)voxel_chunk_dimensions;
        m_bot_y = bot_y;
        m_top_y = top_y;
        m_layer_idx = layer_idx;
        m_vertex_attribute_descriptors = vertex_attribute_descriptors;
        m_bevel_tuning = bevel_tuning;

        m_width_in_chunks = width_in_voxels / voxel_chunk_dimensions;
        m_height_in_chunks = height_in_voxels / voxel_chunk_dimensions;
        m_voxel_chunks = new VoxelChunk[m_width_in_chunks * m_height_in_chunks];
        m_bounds_grid = new BoundsEntry[m_width_in_chunks * m_height_in_chunks];

        for (int y = 0, chunk_idx = 0; y < m_height_in_chunks; ++y)
        {
            for(int x = 0; x < m_width_in_chunks; ++x, ++chunk_idx)
            {
                var bounds = GetChunkBounds(x, y);
                m_voxel_chunks[chunk_idx] = new VoxelChunk(name, x * voxel_chunk_dimensions, y * voxel_chunk_dimensions, voxel_chunk_dimensions, width_in_voxels, height_in_voxels, m_density_grid, m_sample_grid, voxel_size_in_meters, iso_level, bot_y, top_y, generate_collision, density_height_weight, bounds, is_liquid, bevel_tuning);
                m_bounds_grid[chunk_idx] = new BoundsEntry
                {
                    m_bounds = bounds,
                    m_is_visible = false
                };
            }
        }
    }

    public void SetAboveAndBelowSampleGrids(byte[] layer_above_occlusion_grid, byte[] layer_below_occlusion_grid)
    {
        foreach(var chunk in m_voxel_chunks)
        {
            chunk.SetAboveAndBelowSampleGrids(layer_above_occlusion_grid, layer_below_occlusion_grid);
        }
    }

    public void ApplyHeightmap(float[] densities)
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

    public void March(VoxelChunk.ScratchBuffer scratch_buffer, bool only_visible_chunks)
    {
        if(only_visible_chunks)
        {
            foreach (var chunk in m_visible_voxel_chunks)
            {
                chunk.March(scratch_buffer, m_vertex_attribute_descriptors);
            }
        }
        else
        {
            foreach (var chunk in m_voxel_chunks)
            {
                chunk.March(scratch_buffer, m_vertex_attribute_descriptors);
            }
        }
    }

    public void UpdateDensitySamples()
    {
        foreach(var chunk in m_voxel_chunks)
        {
            chunk.UpdateDensitySamples();
        }
    }

    public void UpdateDensitySamples(VoxelChunk.ScratchBuffer scratch_buffer, HashSet<Vector3Int> modified_chunk_ids, HashSet<Vector3Int> dirty_mesh_ids)
    {
        foreach (var chunk_id in modified_chunk_ids)
        {
            if (chunk_id.y != m_layer_idx) continue;

            if (chunk_id.x < 0 || chunk_id.x >= m_width_in_chunks) continue;
            if (chunk_id.z < 0 || chunk_id.z >= m_height_in_chunks) continue;

            var chunk_idx = chunk_id.z * m_width_in_chunks + chunk_id.x;

            var chunk = m_voxel_chunks[chunk_idx];
            var dirty_occlusion_regions = chunk.UpdateDensitySamples();

            if (m_layer_idx > 0)
            {
                var dirty_occlusion_offsets = scratch_buffer.m_occlusion_region_chunk_offset_table[(int)dirty_occlusion_regions];
                foreach (var dirty_occlusion_offset in dirty_occlusion_offsets)
                {
                    dirty_mesh_ids.Add(chunk_id + dirty_occlusion_offset);
                }
            }
        }
    }

    public void March(VoxelChunk.ScratchBuffer scratch_buffer, HashSet<Vector3Int> dirty_mesh_chunk_ids, ref int max_meshes_per_tick)
    {
        scratch_buffer.m_processed_chunks.Clear();

        foreach (var chunk_id in dirty_mesh_chunk_ids)
        {
            if (max_meshes_per_tick <= 0) break;

            if (chunk_id.y != m_layer_idx) continue;

            if (chunk_id.x < 0 || chunk_id.x >= m_width_in_chunks) 
            {
                scratch_buffer.m_processed_chunks.Add(chunk_id);
                continue; 
            }
            if (chunk_id.z < 0 || chunk_id.z >= m_height_in_chunks) 
            {
                scratch_buffer.m_processed_chunks.Add(chunk_id);
                continue; 
            }

            var chunk_idx = chunk_id.z * m_width_in_chunks + chunk_id.x;

            var chunk = m_voxel_chunks[chunk_idx];
            if (!m_visible_voxel_chunks.Contains(chunk)) continue;

            chunk.March(scratch_buffer, m_vertex_attribute_descriptors);
            max_meshes_per_tick--;

            scratch_buffer.m_processed_chunks.Add(chunk_id);
        }

        foreach(var processed_chunk_id in scratch_buffer.m_processed_chunks)
        {
            dirty_mesh_chunk_ids.Remove(processed_chunk_id);
        }

        scratch_buffer.m_processed_chunks.Clear();
    }

    public Bounds GetChunkBounds(int chunk_x, int chunk_y)
    {
        var world_left = chunk_x * m_voxel_chunk_dimensions * m_voxel_size_in_meters.x;
        var world_right = world_left + m_voxel_chunk_dimensions * m_voxel_size_in_meters.x;
        var world_near = chunk_y * m_voxel_chunk_dimensions * m_voxel_size_in_meters.z;
        var world_far = world_near + m_voxel_chunk_dimensions * m_voxel_size_in_meters.z;

        var pt_a = new Vector3(world_left, m_bot_y, world_near);
        var pt_b = new Vector3(world_right, m_top_y, world_far);

        var diameter_vector = pt_b - pt_a;
        var radius_vector = diameter_vector * 0.5f;
        var center = pt_a + radius_vector;

        var bounds = new Bounds(center, diameter_vector);
        return bounds;
    }

    public void Render(float dt, Material prepass_material, Material material, bool cast_shadows)
    {
        foreach(var chunk in m_visible_voxel_chunks)
        {
            chunk.Render(dt, prepass_material, material, cast_shadows);
        }
    }

    public byte[] GetSampleGrid()
    {
        return m_sample_grid;
    }

    public void AddDensity(Vector3 pos, float amount, HashSet<int> dirty_chunk_indices)
    {
        var x = (int)(pos.x / m_voxel_size_in_meters.x);
        if (x < 0 || x > m_width_in_voxels) return;

        var y = (int)(pos.z / m_voxel_size_in_meters.z);
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

    public void SetCollisionGenerationEnabled(bool is_enabled)
    {
        foreach(var chunk in m_voxel_chunks)
        {
            chunk.SetCollisionGenerationEnabled(is_enabled);
        }
    }

    public void UpdateVisibility(Vector3Int min_visible_chunk_idx, Vector3Int max_visible_chunk_idx, Plane[] frustum_planes)
    {
        var min_changed_idx = Vector3Int.Min(min_visible_chunk_idx, m_previous_min_visible_idx);
        var max_changed_idx = Vector3Int.Max(max_visible_chunk_idx, m_previous_max_visible_idx);

        bool is_layer_visible = m_layer_idx >= min_visible_chunk_idx.y && m_layer_idx <= max_visible_chunk_idx.y;

        if (is_layer_visible)
        {
            m_newly_visible_chunk_indices.Clear();
            m_newly_invisible_chunk_indices.Clear();

            for (int chunk_y = min_changed_idx.z; chunk_y <= max_changed_idx.z; ++chunk_y)
            {
                for (int chunk_x = min_changed_idx.x; chunk_x <= max_changed_idx.x; ++chunk_x)
                {
                    var chunk_idx = chunk_y * m_width_in_chunks + chunk_x;
                    var bounds_entry = m_bounds_grid[chunk_idx];

                    bool is_visible = frustum_planes == null || GeometryUtility.TestPlanesAABB(frustum_planes, bounds_entry.m_bounds);


                    bool was_visible = bounds_entry.m_is_visible;
                    if (was_visible == is_visible) continue;

                    if (is_visible)
                    {
                        m_newly_visible_chunk_indices.Add(chunk_idx);
                    }
                    else
                    {
                        m_newly_invisible_chunk_indices.Add(chunk_idx);
                    }
                }
            }

            foreach (var chunk_idx in m_newly_invisible_chunk_indices)
            {
                ref var bounds_entry = ref m_bounds_grid[chunk_idx];
                bounds_entry.m_is_visible = false;
                m_visible_voxel_chunks.Remove(m_voxel_chunks[chunk_idx]);
            }

            foreach (var chunk_idx in m_newly_visible_chunk_indices)
            {
                ref var bounds_entry = ref m_bounds_grid[chunk_idx];
                bounds_entry.m_is_visible = true;
                m_visible_voxel_chunks.Add(m_voxel_chunks[chunk_idx]);
            }
        }
        else
        {
            for(int chunk_idx = 0; chunk_idx < m_bounds_grid.Length; ++chunk_idx)
            {
                ref var bounds_entry = ref m_bounds_grid[chunk_idx];
                if (!bounds_entry.m_is_visible) continue;

                bounds_entry.m_is_visible = false;
                m_visible_voxel_chunks.Remove(m_voxel_chunks[chunk_idx]);
            }
        }

        m_previous_min_visible_idx = min_visible_chunk_idx;
        m_previous_max_visible_idx = max_visible_chunk_idx;
    }

    Color m_color;
    float[] m_density_grid;
    byte[] m_sample_grid;
    int m_width_in_voxels;
    int m_height_in_voxels;
    int m_width_in_chunks;
    int m_height_in_chunks;
    Vector3 m_voxel_size_in_meters;
    float m_one_over_voxel_chunk_dimensions;
    int m_voxel_chunk_dimensions;
    VoxelChunk[] m_voxel_chunks;
    BoundsEntry[] m_bounds_grid;
    List<int> m_newly_visible_chunk_indices = new List<int>();
    List<int> m_newly_invisible_chunk_indices = new List<int>();
    float m_bot_y;
    float m_top_y;
    int m_layer_idx;
    Vector3Int m_previous_min_visible_idx;
    Vector3Int m_previous_max_visible_idx;
    HashSet<VoxelChunk> m_visible_voxel_chunks = new HashSet<VoxelChunk>();
    VertexAttributeDescriptor[] m_vertex_attribute_descriptors;
    BevelTuning m_bevel_tuning;
}