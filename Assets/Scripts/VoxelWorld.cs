using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;

public class VoxelWorld : MonoBehaviour
{
    [System.Serializable]
    public class LiveTuneable
    {
        [System.Serializable]        
        public struct LayerColor
        {
            public Color m_color;
            public float m_height;
        }


        public float m_layer_brightness_factor;
        public LayerColor[] m_layer_colors;
    }

    public LiveTuneable m_tuneables;

    [SerializeField] int m_grid_height_in_voxels;
    [SerializeField] int m_voxel_chunk_dimensions;
    [SerializeField] int m_grid_width_in_voxels;
    [SerializeField] int m_grid_depth_in_voxels;
    [SerializeField] Material m_material;
    [SerializeField] float m_voxel_size_in_meters;
    [SerializeField] float m_ground_plane_size;

    VoxelLayer[] m_layers;
    bool[] m_empty_occlusion_grid;
    List<DensityChange> m_density_changes = new List<DensityChange>();
    VoxelChunk.ScratchBuffer m_voxel_chunk_scratch_buffer;
    HashSet<int> m_dirty_chunk_indices = new HashSet<int>();
    HashSet<int> m_dirty_chunk_occlusion_indices = new HashSet<int>();
    GameObject m_ground_plane;

    public static VoxelWorld Instance;

    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    void Awake()
    {
        var height_map_tex = Resources.Load<Texture2D>("heightmap");

        var pixels = height_map_tex.GetPixels();
        var height_map_width = height_map_tex.width;
        var densities = new float[m_grid_width_in_voxels * m_grid_depth_in_voxels];
        m_empty_occlusion_grid = new bool[m_grid_width_in_voxels * m_grid_depth_in_voxels];

        m_voxel_chunk_scratch_buffer = new VoxelChunk.ScratchBuffer
        {
            m_vertices = new VoxelChunk.Vertex[System.UInt16.MaxValue],
            m_triangles = new System.UInt16[System.UInt16.MaxValue * 24],
            m_edges = new VoxelChunk.Edge[System.UInt16.MaxValue],
        };

        for (int y = 0; y < m_grid_depth_in_voxels; ++y)
        {
            for(int x = 0; x < m_grid_width_in_voxels; ++x)
            {
                var density_idx = y * m_grid_width_in_voxels + x;
                var pixel_idx = y * height_map_width + x;
                var density = pixels[pixel_idx].r;

                densities[density_idx] = density;
            }
        }

        m_layers = new VoxelLayer[m_grid_height_in_voxels];

        float cell_height_in_color_space = 1f / m_grid_height_in_voxels;

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        { 
            var material = GameObject.Instantiate(m_material);

            float iso_level = y / (float)m_grid_height_in_voxels;

            float bot_y = (float)(y - 1) * m_voxel_size_in_meters;
            float top_y = (float)y * m_voxel_size_in_meters;

            var layer = new VoxelLayer(m_grid_width_in_voxels, m_grid_depth_in_voxels, m_voxel_chunk_dimensions, m_voxel_size_in_meters, material, iso_level, bot_y, top_y);
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

        for(int y = m_layers.Length - 1; y >= 0; --y)
        {
            float layer_min_height = y * cell_height_in_color_space;
            float layer_max_height = (y + 1) * cell_height_in_color_space;

            var layer = m_layers[y];

            layer.ApplyHeightmap(densities, layer_min_height, layer_max_height);
            layer.Triangulate(m_voxel_chunk_scratch_buffer);
        }

        m_ground_plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        m_ground_plane.name = "GroundPlane";

        var ground_plane_material = GameObject.Instantiate(m_material);
        ground_plane_material.color = ApplyLayerBrightnessColor(0, m_tuneables.m_layer_colors[0].m_color);

        m_ground_plane.GetComponent<MeshRenderer>().sharedMaterial = ground_plane_material;
        m_ground_plane.transform.localScale = new Vector3(m_ground_plane_size, 1, m_ground_plane_size);
        m_ground_plane.transform.localPosition = new Vector3(0, -0.5f, 0);

        Instance = this;
    }

    private void LateUpdate()
    {
        UpdateDensityChanges();

        float dt = Time.deltaTime;
        var layer_colors = m_tuneables.m_layer_colors;

        for (int y = 0; y < m_layers.Length; ++y)
        {
            var color = Color.white;
            var top_y = (float)y;

            foreach(var layer_color in layer_colors)
            {
                if (top_y > layer_color.m_height) continue;                

                color = layer_color.m_color;
                break;
            }


            color = ApplyLayerBrightnessColor(y, color);

            m_layers[y].Render(dt, color);
        }
    }

    Color ApplyLayerBrightnessColor(int y, Color color)
    {   
        var layer_brightness = m_tuneables.m_layer_brightness_factor + (1 - m_tuneables.m_layer_brightness_factor) * (y / (m_layers.Length - 1));
        return new Color(color.r * layer_brightness, color.g * layer_brightness, color.b * layer_brightness);
    }

    void UpdateDensityChanges()
    {
        if (m_density_changes.Count == 0) return;

        m_dirty_chunk_indices.Clear();
        m_dirty_chunk_occlusion_indices.Clear();

        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            var layer = m_layers[y];
            foreach(var density_change in m_density_changes)
            {
                if (density_change.m_layer_idx != y) continue;

                layer.AddDensity(density_change.m_position, density_change.m_amount, m_dirty_chunk_indices);
            }

            if(m_dirty_chunk_indices.Count != 0)
            {
                layer.Triangulate(m_voxel_chunk_scratch_buffer, m_dirty_chunk_indices, m_dirty_chunk_occlusion_indices);
            }

            var temp = m_dirty_chunk_indices;
            m_dirty_chunk_indices = m_dirty_chunk_occlusion_indices;
            m_dirty_chunk_occlusion_indices = temp;
            m_dirty_chunk_occlusion_indices.Clear();
        }
        m_density_changes.Clear();
        
    }

    public void AddDensity(Vector3 pos, float amount)
    {
        var layer_idx = (int)((pos.y / (m_grid_height_in_voxels * m_voxel_size_in_meters)) * (float)m_layers.Length);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_density_changes.Add(new DensityChange { m_position = pos, m_layer_idx = layer_idx, m_amount = amount });
    }

    public float GetVoxelSizeInMeters() { return m_voxel_size_in_meters;  }
}