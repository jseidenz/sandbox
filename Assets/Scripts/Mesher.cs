using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class Mesher
{
    int m_voxel_chunk_dimensions;
    int m_grid_height_in_voxels;
    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;
    Vector3 m_voxel_size_in_meters;
    VoxelLayer[] m_layers;
    byte[] m_empty_sample_grid;
    VoxelChunk.ScratchBuffer m_voxel_chunk_scratch_buffer;
    HashSet<Vector3Int> m_dirty_mesh_ids = new HashSet<Vector3Int>();
    LayeredBrush m_brush;
    bool m_cast_shadows;
    Material m_prepass_material;
    

    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public void Init(
        string name,
        float[][] density_grids,
        int grid_width_in_voxels,
        int grid_height_in_voxels,
        int grid_depth_in_voxels,
        Vector3 voxel_size_in_meters,
        int voxel_chunk_dimesnions,
        bool generate_collision,
        float iso_level,
        float density_height_weight,
        LayeredBrush brush,
        bool cast_shadows,
        bool is_liquid,
        Material prepass_material,
        BevelTuning bevel_tuning        
        )
    {
        m_voxel_chunk_dimensions = voxel_chunk_dimesnions;
        m_voxel_size_in_meters = voxel_size_in_meters;
        m_grid_width_in_voxels = grid_width_in_voxels;
        m_grid_height_in_voxels = grid_height_in_voxels;
        m_grid_depth_in_voxels = grid_depth_in_voxels;

        m_empty_sample_grid = new byte[m_grid_width_in_voxels * m_grid_depth_in_voxels];
        m_voxel_chunk_scratch_buffer = VoxelChunk.ScratchBuffer.CreateScratchBuffer();
        m_layers = new VoxelLayer[m_grid_height_in_voxels];
        m_brush = brush;
        m_cast_shadows = cast_shadows;
        m_prepass_material = prepass_material;


        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        { 
            float bot_y = (float)(y - 1) * m_voxel_size_in_meters.y;
            float top_y = (float)y * m_voxel_size_in_meters.y;

            var layer = new VoxelLayer(name, density_grids[y], y, m_grid_width_in_voxels, m_grid_depth_in_voxels, m_voxel_chunk_dimensions, m_voxel_size_in_meters, iso_level, bot_y, top_y, generate_collision, density_height_weight, m_vertex_attribute_descriptors, is_liquid, bevel_tuning);
            m_layers[y] = layer;
        }

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            var layer_above_occlusion_grid = m_empty_sample_grid;
            var layer_below_occlusion_grid = m_empty_sample_grid;

            if(y > 0)
            {
                layer_below_occlusion_grid = m_layers[y - 1].GetSampleGrid();
            }

            if(y < m_grid_height_in_voxels - 2)
            {
                layer_above_occlusion_grid = m_layers[y + 1].GetSampleGrid();
            }

            m_layers[y].SetAboveAndBelowSampleGrids(layer_above_occlusion_grid, layer_below_occlusion_grid);
        }
    }

    public void BindCamera(Camera camera, float bounding_sphere_radius_multiplier)
    {
        foreach(var layer in m_layers)
        {
            layer.BindCamera(camera, bounding_sphere_radius_multiplier);
        }
    }

    public void Render(float dt)
    {
        Profiler.BeginSample("Render");
        for (int y = 0; y < m_layers.Length; ++y)
        {
            m_brush.GetMaterialForLayer(y, out var material);

            m_layers[y].Render(dt, m_prepass_material, material, m_cast_shadows);
        }
        Profiler.EndSample();
    }

    public void Triangulate(HashSet<Vector3Int> modified_chunk_ids)
    {
        m_dirty_mesh_ids.Clear();
        foreach(var dirty_chunk_id in modified_chunk_ids)
        {
            m_dirty_mesh_ids.Add(dirty_chunk_id);
        }

        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            var layer = m_layers[y];

            layer.UpdateDensitySamples(m_voxel_chunk_scratch_buffer, modified_chunk_ids, m_dirty_mesh_ids);
        }


        Profiler.BeginSample("Triangulate");
        int max_meshers_per_tick = 50;
        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            var layer = m_layers[y];

            layer.March(m_voxel_chunk_scratch_buffer, m_dirty_mesh_ids, ref max_meshers_per_tick);            
        }
        Profiler.EndSample();
    }

    public void TriangulateAll()
    {
        UpdateDensitySamples();
        Profiler.BeginSample("TriangulateAll");
        for(int y = m_layers.Length - 1; y >= 0; --y)
        {
            var layer = m_layers[y];
            layer.March(m_voxel_chunk_scratch_buffer, false);
        }
        Profiler.EndSample();
    }

    public void OnDestroy()
    {
        foreach(var layer in m_layers)
        {
            layer.OnDestroy();
        }
    }

    public void SetCollisionGenerationEnabled(bool is_enabled)
    {
        foreach(var layer in m_layers)
        {
            layer.SetCollisionGenerationEnabled(is_enabled);
        }
    }

    public void TriangulateLayer(int layer_idx, bool only_visible_chunks)
    {
        m_layers[layer_idx].March(m_voxel_chunk_scratch_buffer, only_visible_chunks);
    }

    public void UpdateDensitySamples()
    {
        m_dirty_mesh_ids.Clear();
        for(int layer_idx = m_grid_height_in_voxels - 1; layer_idx >= 0; layer_idx--)
        {
            var layer = m_layers[layer_idx];
            layer.UpdateDensitySamples();
        }
    }

    public int GetGridHeightInVoxels() { return m_grid_depth_in_voxels; }
    public int GetGridWidthInVoxels() { return m_grid_depth_in_voxels; }


    VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
    };
}