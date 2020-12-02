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
            m_mesh = new Mesh();
            m_mesh.name = $"VoxelLayer({pos_y})";

            var vertices = new Vector3[(m_height_in_voxels) * (m_width_in_voxels) * 4];
            var normals = new Vector3[(m_height_in_voxels) * (m_width_in_voxels) * 4];
            var triangles = new int[m_width_in_voxels * m_height_in_voxels * 6];

            float voxel_size = 1f;
            int vert_idx = 0;
            int triangle_idx = 0;

            for (int y = 0; y < m_height_in_voxels; ++y)
            {
                float pos_z = (float)y;
                float pos_z_plus_one = pos_z + voxel_size;
                for (int x = 0; x < m_width_in_voxels; ++x)
                {
                    float pos_x = (float)x;
                    float pos_x_plus_one = pos_x + voxel_size;

                    var normal = Vector3.up;

                    vertices[vert_idx + 0] = new Vector3(pos_x, pos_y, pos_z);
                    vertices[vert_idx + 1] = new Vector3(pos_x_plus_one, pos_y, pos_z);
                    vertices[vert_idx + 2] = new Vector3(pos_x, pos_y, pos_z_plus_one);
                    vertices[vert_idx + 3] = new Vector3(pos_x_plus_one, pos_y, pos_z_plus_one);

                    normals[vert_idx + 0] = normal;
                    normals[vert_idx + 1] = normal;
                    normals[vert_idx + 2] = normal;
                    normals[vert_idx + 3] = normal;

                    triangles[triangle_idx + 0] = vert_idx + 0;
                    triangles[triangle_idx + 1] = vert_idx + 1;
                    triangles[triangle_idx + 2] = vert_idx + 2;
                    triangles[triangle_idx + 3] = vert_idx + 3;
                    triangles[triangle_idx + 4] = vert_idx + 1;
                    triangles[triangle_idx + 5] = vert_idx + 2;

                    vert_idx += 4;
                    triangle_idx += 6;
                }
            }

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

            m_mesh.vertices = vertices;
            m_mesh.normals = normals;
            m_mesh.triangles = triangles;
        }

        bool[] m_grid;
        int m_width_in_voxels;
        int m_height_in_voxels;
        Mesh m_mesh;
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