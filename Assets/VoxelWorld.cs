using UnityEngine;

public class VoxelWorld : MonoBehaviour 
{
    [SerializeField] int m_grid_height_in_voxels;
    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;

    bool[] m_grid;

    void Awake()
    {
        var height_map_tex = Resources.Load<Texture2D>("heightmap");

        var pixels = height_map_tex.GetPixels();

        m_grid_depth_in_voxels = height_map_tex.height;
        m_grid_width_in_voxels = height_map_tex.width;

        m_grid = new bool[m_grid_width_in_voxels * m_grid_depth_in_voxels * m_grid_height_in_voxels];

        float cell_height_in_color_space = 1f / m_grid_height_in_voxels;

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            float cell_start_height_in_color_space = y * cell_height_in_color_space;
            for (int z = 0; z < m_grid_depth_in_voxels; ++z)
            {
                for (int x = 0; x < m_grid_width_in_voxels; ++x)
                {
                    var pixel_idx = z * m_grid_width_in_voxels + x;
                    var height = pixels[pixel_idx].r;
                    var cell_idx = y * m_grid_width_in_voxels * m_grid_depth_in_voxels + z * m_grid_width_in_voxels + x;
                    m_grid[cell_idx] = height >= cell_start_height_in_color_space;
                }
            }
        }

        var cube_prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);


        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            float pos_y = (float)y;

            for (int z = 0; z < m_grid_depth_in_voxels; ++z)
            {
                float pos_z = (float)z;

                for (int x = 0; x < m_grid_width_in_voxels; ++x)
                {
                    float pos_x = (float)x;

                    int cell_idx = y * m_grid_depth_in_voxels * m_grid_width_in_voxels + z * m_grid_width_in_voxels + x;

                    if (!m_grid[cell_idx]) continue;

                    var cube = GameObject.Instantiate(cube_prefab);
                    cube.transform.localPosition = new Vector3(pos_x, pos_y, pos_z);
                }
            }
        }

        GameObject.Destroy(cube_prefab);
    }
}