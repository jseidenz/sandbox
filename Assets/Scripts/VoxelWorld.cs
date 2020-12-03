using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

public class VoxelWorld : MonoBehaviour
{
    [SerializeField] int m_grid_height_in_voxels;
    [SerializeField] Color[] m_colors;
    [SerializeField] Material m_material;

    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;

    class Layer
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Vertex
        {
            public Vector3 m_position;
            public Vector3 m_normal;
        }

        public Layer(int width_in_voxels, int height_in_voxels)
        {
            m_grid = new bool[width_in_voxels * height_in_voxels];
            m_width_in_voxels = width_in_voxels;
            m_height_in_voxels = height_in_voxels;

            var collider_go = new GameObject("VoxelCollider");
            collider_go.gameObject.SetActive(false);
            m_collider = collider_go.AddComponent<MeshCollider>();
        }

        public void ApplyHeightmap(Color[] pixels, float heightmap_height)
        {
            for (int y = 0; y < m_height_in_voxels; ++y)
            {
                for (int x = 0; x < m_width_in_voxels; ++x)
                {
                    var cell_idx = y * m_width_in_voxels + x;
                    var height = pixels[cell_idx].r;
                    bool is_filled = height >= heightmap_height;
                    m_grid[cell_idx] = is_filled;
                    if(is_filled)
                    {
                        ++m_voxel_count;
                    }
                }
            }
        }

        public void Triangulate(GameObject cube_prefab, float pos_y, Material material)
        {
            m_mesh = new Mesh();
            m_mesh.name = $"VoxelLayer({pos_y})";
            m_material = material;

            var vertex_count = m_voxel_count * 4;
            m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m_mesh.SetVertexBufferParams(vertex_count, m_vertex_attribute_descriptors);

            var vertices = new Vertex[vertex_count];
            var triangles = new int[m_voxel_count * 6];

            float voxel_size = 1f;
            int vert_idx = 0;
            int triangle_idx = 0;

            for (int y = 0; y < m_height_in_voxels; ++y)
            {
                float pos_z = (float)y;
                float pos_z_plus_one = pos_z + voxel_size;
                for (int x = 0; x < m_width_in_voxels; ++x)
                {
                    int cell_idx = y * m_width_in_voxels + x;
                    if (!m_grid[cell_idx]) continue;

                    float pos_x = (float)x;
                    float pos_x_plus_one = pos_x + voxel_size;

                    var normal = Vector3.up;

                    vertices[vert_idx + 0] = new Vertex
                    {
                        m_position = new Vector3(pos_x, pos_y, pos_z),
                        m_normal = normal
                    };

                    vertices[vert_idx + 1] = new Vertex
                    {
                        m_position = new Vector3(pos_x_plus_one, pos_y, pos_z),
                        m_normal = normal
                    };

                    vertices[vert_idx + 2] = new Vertex
                    {
                        m_position = new Vector3(pos_x, pos_y, pos_z_plus_one),
                        m_normal = normal
                    };

                    vertices[vert_idx + 3] = new Vertex
                    {
                        m_position = new Vector3(pos_x_plus_one, pos_y, pos_z_plus_one),
                        m_normal = normal
                    };

                    triangles[triangle_idx + 0] = vert_idx + 0;
                    triangles[triangle_idx + 1] = vert_idx + 2;
                    triangles[triangle_idx + 2] = vert_idx + 1;
                    triangles[triangle_idx + 3] = vert_idx + 1;
                    triangles[triangle_idx + 4] = vert_idx + 2;
                    triangles[triangle_idx + 5] = vert_idx + 3;

                    vert_idx += 4;
                    triangle_idx += 6;
                }
            }

            m_mesh.SetVertexBufferData(vertices, 0, 0, vertex_count);
            m_mesh.SetTriangles(triangles, 0, false);
            m_mesh.RecalculateBounds();

            m_collider.sharedMesh = m_mesh;
            if (!m_collider.gameObject.activeSelf)
            {
                m_collider.gameObject.SetActive(true);
            }
        }

        public void Render(float dt)
        {
            Graphics.DrawMesh(m_mesh, Matrix4x4.identity, m_material, 0);
        }

        bool[] m_grid;
        int m_width_in_voxels;
        int m_height_in_voxels;
        Mesh m_mesh;
        Material m_material;
        MeshCollider m_collider;        
        int m_voxel_count;

        VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
        };
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

            float layer_heightmap_height = y * cell_height_in_color_space;

            var color_idx = (int)((float)m_colors.Length * (float)y / (float)m_grid_height_in_voxels);

            var color = m_colors[color_idx];

            float layer_brightness = 0.25f + 0.75f * layer_heightmap_height;
            color = new Color(color.r * layer_brightness, color.g * layer_brightness, color.b * layer_brightness);

            var material = GameObject.Instantiate(m_material);
            material.color = color;

            m_layers[y].Triangulate(cube_prefab, pos_y, material);
        }

        GameObject.Destroy(cube_prefab);        
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        foreach(var layer in m_layers)
        {
            layer.Render(dt);
        }
    }
}