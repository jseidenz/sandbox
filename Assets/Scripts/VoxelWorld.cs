using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

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
    [SerializeField] Material m_material;
    [SerializeField] float m_voxel_size_in_meters;

    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;
    VoxelLayer[] m_layers;

    public static VoxelWorld Instance;

    void Awake()
    {
        var height_map_tex = Resources.Load<Texture2D>("heightmap");

        m_grid_depth_in_voxels = 100; // height_map_tex.height;
        m_grid_width_in_voxels = 100; //height_map_tex.width;

        var pixels = height_map_tex.GetPixels();
        var height_map_width = height_map_tex.width;
        var height_map_height = height_map_tex.height;
        var densities = new float[m_grid_width_in_voxels * m_grid_depth_in_voxels];

        for(int y = 0; y < m_grid_depth_in_voxels; ++y)
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

        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            float layer_min_height = y * cell_height_in_color_space;
            float layer_max_height = (y + 1) * cell_height_in_color_space;

            var material = GameObject.Instantiate(m_material);

            float iso_level = y / (float)m_grid_height_in_voxels;

            float bot_y = (float)(y - 1) * m_voxel_size_in_meters;
            float top_y = (float)y * m_voxel_size_in_meters;

            var layer = new VoxelLayer(m_grid_width_in_voxels, m_grid_depth_in_voxels, m_voxel_size_in_meters, material, iso_level, bot_y, top_y);
            layer.ApplyHeightmap(densities, layer_min_height, layer_max_height);
            layer.Triangulate();
            m_layers[y] = layer;            
        }

        Instance = this;
    }

    private void LateUpdate()
    {
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

            var layer_brightness = m_tuneables.m_layer_brightness_factor + (1 - m_tuneables.m_layer_brightness_factor) * (y / (m_layers.Length - 1));

            color = new Color(color.r * layer_brightness, color.g * layer_brightness, color.b * layer_brightness);

            m_layers[y].Render(dt, color);
        }
    }

    public void AddDensity(Vector3 pos, float amount)
    {
        var layer_idx = (int)((pos.y / (m_grid_height_in_voxels * m_voxel_size_in_meters)) * (float)m_layers.Length);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_layers[layer_idx].AddDensity(pos, amount);
    }

    public float GetVoxelSizeInMeters() { return m_voxel_size_in_meters;  }
}