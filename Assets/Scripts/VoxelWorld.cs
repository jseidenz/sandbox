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

    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;
    VoxelLayer[] m_layers;

    void Awake()
    {
        var height_map_tex = Resources.Load<Texture2D>("heightmap");

        var pixels = height_map_tex.GetPixels();

        m_grid_depth_in_voxels = height_map_tex.height;
        m_grid_width_in_voxels = height_map_tex.width;

        m_layers = new VoxelLayer[m_grid_height_in_voxels];

        float cell_height_in_color_space = 1f / m_grid_height_in_voxels;

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            float layer_min_height = y * cell_height_in_color_space;
            float layer_max_height = (y + 1) * cell_height_in_color_space;

            var material = GameObject.Instantiate(m_material);

            float iso_level = y / (float)m_grid_height_in_voxels;

            var layer = new VoxelLayer(m_grid_width_in_voxels, m_grid_depth_in_voxels, material, iso_level);
            layer.ApplyHeightmap(pixels, layer_min_height, layer_max_height);

            m_layers[y] = layer;
            
        }


        var layer_colors = m_tuneables.m_layer_colors;

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            float bot_y = (float)(y - 1) * 1f;
            float top_y = (float)y * 1f;


            m_layers[y].Triangulate(bot_y, top_y);
        }
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
}