using UnityEngine;

public class VoxelWorld : MonoBehaviour 
{
    [SerializeField] int m_grid_height_in_voxels;
    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;

    class Layer
    {
        public Layer(int width_in_voxels, int height_in_voxels)
        {
            m_grid = new bool[width_in_voxels * height_in_voxels];
            m_width_in_voxels = width_in_voxels;
            m_height_in_voxels = height_in_voxels;
        }

        public void ApplyHeightmap(Color[] pixels, float heightmap_height)
        {
            for (int y = 0; y < m_height_in_voxels; ++y)
            {
                for (int x = 0; x < m_width_in_voxels; ++x)
                {
                    var cell_idx = y * m_width_in_voxels + x;
                    var height = pixels[cell_idx].r;
                    m_grid[cell_idx] = height >= heightmap_height;
                }
            }
        }

        public void Triangulate(GameObject cube_prefab, float pos_y)
        {
            for (int y = 0; y < m_height_in_voxels; ++y)
            {
                float pos_z = (float)y;
                for (int x = 0; x < m_width_in_voxels; ++x)
                {
                    float pos_x = (float)x;
                    var cell_idx = y * m_width_in_voxels + x;
                    if (!m_grid[cell_idx]) continue;

                    var cube = GameObject.Instantiate(cube_prefab);
                    cube.transform.localPosition = new Vector3(pos_x, pos_y, pos_z);
                }
            }
        }

        bool[] m_grid;
        int m_width_in_voxels;
        int m_height_in_voxels;
    }

    Layer[] m_layers;

    void Awake()
    {
        var height_map_tex = Resources.Load<Texture2D>("heightmap");

        var pixels = height_map_tex.GetPixels();

        m_grid_depth_in_voxels = height_map_tex.height;
        m_grid_width_in_voxels = height_map_tex.width;

        m_layers = new Layer[m_grid_height_in_voxels];

        float cell_height_in_color_space = 1f / m_grid_height_in_voxels;

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            float layer_heightmap_height = y * cell_height_in_color_space;

            m_layers[y] = new Layer(m_grid_width_in_voxels, m_grid_depth_in_voxels);
            m_layers[y].ApplyHeightmap(pixels, layer_heightmap_height);
        }

        var cube_prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            float pos_y = (float)y;

            m_layers[y].Triangulate(cube_prefab, pos_y);
        }

        GameObject.Destroy(cube_prefab);
    }
}