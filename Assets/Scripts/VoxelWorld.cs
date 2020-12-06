using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class VoxelWorld : MonoBehaviour
{
    [System.Serializable]
    public class LiveTuneable
    {
        public float m_water_height;
        public Gradient m_height_gradient = new Gradient();

        public float m_saturation_variation_rate;
        public float m_saturation_variation_amount;
        public float m_hue_variation_rate;
        public float m_hue_variaton_amount;
    }

    public LiveTuneable m_tuneables;

    [SerializeField] int m_grid_height_in_voxels;
    [SerializeField] int m_voxel_chunk_dimensions;
    [SerializeField] int m_grid_width_in_voxels;
    [SerializeField] int m_grid_depth_in_voxels;
    [SerializeField] Material m_material;
    [SerializeField] float m_voxel_size_in_meters;
    [SerializeField] float m_ground_plane_size;
    [SerializeField] GameObject m_water;

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

        Resources.UnloadAsset(height_map_tex);

        var densities = new float[m_grid_width_in_voxels * m_grid_depth_in_voxels];
        m_empty_occlusion_grid = new bool[m_grid_width_in_voxels * m_grid_depth_in_voxels];

        m_voxel_chunk_scratch_buffer = VoxelChunk.ScratchBuffer.CreateScratchBuffer();

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
        ground_plane_material.color = GetColorForLayer(0);

        var ground_plane_mesh_renderer = m_ground_plane.GetComponent<MeshRenderer>();
        ground_plane_mesh_renderer.sharedMaterial = ground_plane_material;
        ground_plane_mesh_renderer.receiveShadows = false;
        m_ground_plane.transform.localScale = new Vector3(m_ground_plane_size, 1, m_ground_plane_size);
        m_ground_plane.transform.localPosition = new Vector3(0, -0.5f, 0);

        Instance = this;

        m_water = GameObject.Instantiate(m_water);
    }

    private void LateUpdate()
    {
        m_water.transform.position = new Vector3(0, m_tuneables.m_water_height, 0);

        Profiler.BeginSample("UpdateDensityChanged");
        UpdateDensityChanges();
        Profiler.EndSample();

        float dt = Time.deltaTime;

        Profiler.BeginSample("Render");
        for (int y = 0; y < m_layers.Length; ++y)
        {
            var color = GetColorForLayer(y);

            m_layers[y].Render(dt, color);
        }
        Profiler.EndSample();
    }

    public Color GetColorForLayer(int layer_idx)
    {
        float layer_height_in_meters = m_grid_height_in_voxels * m_voxel_size_in_meters;
        float layer_height_over_world_height = layer_idx / layer_height_in_meters;

        var color = m_tuneables.m_height_gradient.Evaluate(layer_height_over_world_height);
        if(layer_height_in_meters > m_tuneables.m_water_height)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);

            hue += Mathf.Sin(layer_height_over_world_height * m_tuneables.m_hue_variation_rate) * m_tuneables.m_hue_variaton_amount;
            saturation += Mathf.Sin(layer_height_over_world_height * m_tuneables.m_saturation_variation_rate) * m_tuneables.m_saturation_variation_amount;

            color = Color.HSVToRGB(hue, saturation, value);
        }
        return color;
    }

    void UpdateDensityChanges()
    {
        if (m_density_changes.Count == 0) return;

        m_dirty_chunk_indices.Clear();
        m_dirty_chunk_occlusion_indices.Clear();

        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            var layer = m_layers[y];
            Profiler.BeginSample("AddDensity");
            foreach(var density_change in m_density_changes)
            {
                if (density_change.m_layer_idx != y) continue;

                layer.AddDensity(density_change.m_position, density_change.m_amount, m_dirty_chunk_indices);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Triangulate");
            if(m_dirty_chunk_indices.Count != 0)
            {
                layer.Triangulate(m_voxel_chunk_scratch_buffer, m_dirty_chunk_indices, m_dirty_chunk_occlusion_indices);
            }
            Profiler.EndSample();

            var temp = m_dirty_chunk_indices;
            m_dirty_chunk_indices = m_dirty_chunk_occlusion_indices;
            m_dirty_chunk_occlusion_indices = temp;
            m_dirty_chunk_occlusion_indices.Clear();
        }
        m_density_changes.Clear();
        
    }

    public void AddDensity(Vector3 world_pos, float amount)
    {
        var layer_idx = (int)(world_pos.y / m_voxel_size_in_meters);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_density_changes.Add(new DensityChange { m_position = world_pos, m_layer_idx = layer_idx, m_amount = amount });
    }

    public float GetVoxelSizeInMeters() { return m_voxel_size_in_meters;  }
}