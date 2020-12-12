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
    bool[] m_empty_occlusion_grid;
    VoxelChunk.ScratchBuffer m_voxel_chunk_scratch_buffer;
    HashSet<int> m_dirty_chunk_occlusion_indices = new HashSet<int>();
    LayeredBrush m_brush;

    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public void Init(string name, float[][] density_grids, int grid_width_in_voxels, int grid_height_in_voxels, int grid_depth_in_voxels, Vector3 voxel_size_in_meters, int voxel_chunk_dimesnions, bool generate_collision, float iso_level, float density_height_weight, LayeredBrush brush)
    {
        m_voxel_chunk_dimensions = voxel_chunk_dimesnions;
        m_voxel_size_in_meters = voxel_size_in_meters;
        m_grid_width_in_voxels = grid_width_in_voxels;
        m_grid_height_in_voxels = grid_height_in_voxels;
        m_grid_depth_in_voxels = grid_depth_in_voxels;

        m_empty_occlusion_grid = new bool[m_grid_width_in_voxels * m_grid_depth_in_voxels];
        m_voxel_chunk_scratch_buffer = VoxelChunk.ScratchBuffer.CreateScratchBuffer();
        m_layers = new VoxelLayer[m_grid_height_in_voxels];
        m_brush = brush;


        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        { 
            float bot_y = (float)(y - 1) * m_voxel_size_in_meters.y;
            float top_y = (float)y * m_voxel_size_in_meters.y;

            var layer = new VoxelLayer(name, density_grids[y], y, m_grid_width_in_voxels, m_grid_depth_in_voxels, m_voxel_chunk_dimensions, m_voxel_size_in_meters, iso_level, bot_y, top_y, generate_collision, density_height_weight, m_vertex_attribute_descriptors);
            m_layers[y] = layer;
        }

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            var layer_above_occlusion_grid = m_empty_occlusion_grid;
            var layer_below_occlusion_grid = m_empty_occlusion_grid;

            if(y > 0)
            {
                layer_below_occlusion_grid = m_layers[y - 1].GetOcclusionGrid();
            }

            if(y < m_grid_height_in_voxels - 2)
            {
                layer_above_occlusion_grid = m_layers[y + 1].GetOcclusionGrid();
            }

            m_layers[y].SetAboveAndBelowOcclusionGrids(layer_above_occlusion_grid, layer_below_occlusion_grid);
        }
    }

    public void BindCamera(Camera camera)
    {
        foreach(var layer in m_layers)
        {
            layer.BindCamera(camera);
        }
    }

    public void Render(float dt)
    {
#if UNITY_EDITOR
        Profiler.BeginSample("RefreshLookupTable");
        m_brush.RefreshLookupTable();
        Profiler.EndSample();
#endif

        Profiler.BeginSample("Render");
        for (int y = 0; y < m_layers.Length; ++y)
        {
            m_brush.GetMaterialForLayer(y, out var material);

            m_layers[y].Render(dt, material);
        }
        Profiler.EndSample();
    }

    public void Triangulate(HashSet<Vector3Int> dirty_chunk_ids)
    {
        if (dirty_chunk_ids.Count == 0) return;

        m_dirty_chunk_occlusion_indices.Clear();


        Profiler.BeginSample("Triangulate");
        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            var layer = m_layers[y];

            layer.Triangulate(m_voxel_chunk_scratch_buffer, dirty_chunk_ids);            
        }
        Profiler.EndSample();
    }

    public void TriangulateAll()
    {
        Profiler.BeginSample("TriangulateAll");
        for(int y = m_layers.Length - 1; y >= 0; --y)
        {
            m_layers[y].Triangulate(m_voxel_chunk_scratch_buffer, false);
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

    public void SetOcclusionChecksEnabled(bool is_enabled)
    {
        foreach (var layer in m_layers)
        {
            layer.SetOcclusionChecksEnabled(is_enabled);
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
        m_layers[layer_idx].Triangulate(m_voxel_chunk_scratch_buffer, only_visible_chunks);
    }

    public void UpdateOcclusion()
    {
        for(int layer_idx = m_grid_height_in_voxels - 1; layer_idx >= 0; layer_idx--)
        {
            var layer = m_layers[layer_idx];
            layer.UpdateOcclusion(m_voxel_chunk_scratch_buffer);
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