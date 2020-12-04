using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

public class VoxelLayer
{
    [StructLayout(LayoutKind.Sequential)]
    struct Vertex
    {
        public Vector3 m_position;
        public Vector3 m_normal;
    }

    public VoxelLayer(int width_in_voxels, int height_in_voxels, Material material)
    {
        m_grid = new bool[width_in_voxels * height_in_voxels];
        m_width_in_voxels = width_in_voxels;
        m_height_in_voxels = height_in_voxels;

        var collider_go = new GameObject("VoxelCollider");
        collider_go.gameObject.SetActive(false);
        m_collider = collider_go.AddComponent<MeshCollider>();
        m_material = material;
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

    public Mesh TriangulateMesh(float bot_y, float top_y)
    {
        var mesh = new Mesh();
        mesh.name = $"VoxelLayer({bot_y})";

        var vertex_count = m_voxel_count * 8;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertexBufferParams(vertex_count, m_vertex_attribute_descriptors);

        var vertices = new Vertex[vertex_count];
        var triangles = new int[m_voxel_count * 30];

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
                    m_position = new Vector3(pos_x, top_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 1] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, top_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 2] = new Vertex
                {
                    m_position = new Vector3(pos_x, top_y, pos_z_plus_one),
                    m_normal = normal
                };

                vertices[vert_idx + 3] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, top_y, pos_z_plus_one),
                    m_normal = normal
                };

                vertices[vert_idx + 4] = new Vertex
                {
                    m_position = new Vector3(pos_x, bot_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 5] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, bot_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 6] = new Vertex
                {
                    m_position = new Vector3(pos_x, bot_y, pos_z_plus_one),
                    m_normal = normal
                };

                vertices[vert_idx + 7] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, bot_y, pos_z_plus_one),
                    m_normal = normal
                };

                triangles[triangle_idx + 0] = vert_idx + 0;
                triangles[triangle_idx + 1] = vert_idx + 2;
                triangles[triangle_idx + 2] = vert_idx + 1;
                triangles[triangle_idx + 3] = vert_idx + 1;
                triangles[triangle_idx + 4] = vert_idx + 2;
                triangles[triangle_idx + 5] = vert_idx + 3;

                triangles[triangle_idx + 6] = vert_idx + 0;
                triangles[triangle_idx + 7] = vert_idx + 1;
                triangles[triangle_idx + 8] = vert_idx + 4;
                triangles[triangle_idx + 9] = vert_idx + 4;
                triangles[triangle_idx + 10] = vert_idx + 1;
                triangles[triangle_idx + 11] = vert_idx + 5;

                triangles[triangle_idx + 12] = vert_idx + 0;
                triangles[triangle_idx + 13] = vert_idx + 4;
                triangles[triangle_idx + 14] = vert_idx + 2;
                triangles[triangle_idx + 15] = vert_idx + 2;
                triangles[triangle_idx + 16] = vert_idx + 4;
                triangles[triangle_idx + 17] = vert_idx + 6;

                triangles[triangle_idx + 18] = vert_idx + 1;
                triangles[triangle_idx + 19] = vert_idx + 3;
                triangles[triangle_idx + 20] = vert_idx + 5;
                triangles[triangle_idx + 21] = vert_idx + 3;
                triangles[triangle_idx + 22] = vert_idx + 7;
                triangles[triangle_idx + 23] = vert_idx + 5;

                triangles[triangle_idx + 24] = vert_idx + 2;
                triangles[triangle_idx + 25] = vert_idx + 6;
                triangles[triangle_idx + 26] = vert_idx + 3;
                triangles[triangle_idx + 27] = vert_idx + 3;
                triangles[triangle_idx + 28] = vert_idx + 6;
                triangles[triangle_idx + 29] = vert_idx + 7;

                vert_idx += 8;
                triangle_idx += 30;
            }
        }

        mesh.SetVertexBufferData(vertices, 0, 0, vertex_count);
        mesh.SetTriangles(triangles, 0, false);
        mesh.RecalculateBounds();

        return mesh;
    }

    public void Triangulate(float bot_y, float top_y)
    {
        m_mesh = TriangulateMesh(bot_y, top_y);
        m_marched_mesh = MarchMesh(bot_y, top_y);

        m_collider.sharedMesh = m_mesh;
        if (!m_collider.gameObject.activeSelf)
        {
            m_collider.gameObject.SetActive(true);
        }
    }


    public Mesh MarchMesh(float bot_y, float top_y)
    {
        var mesh = new Mesh();
        mesh.name = $"VoxelLayer({bot_y})";

        var vertex_count = m_voxel_count * 8;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertexBufferParams(vertex_count, m_vertex_attribute_descriptors);

        var vertices = new Vertex[vertex_count];
        var triangles = new int[m_voxel_count * 30];

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
                    m_position = new Vector3(pos_x, top_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 1] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, top_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 2] = new Vertex
                {
                    m_position = new Vector3(pos_x, top_y, pos_z_plus_one),
                    m_normal = normal
                };

                vertices[vert_idx + 3] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, top_y, pos_z_plus_one),
                    m_normal = normal
                };

                vertices[vert_idx + 4] = new Vertex
                {
                    m_position = new Vector3(pos_x, bot_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 5] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, bot_y, pos_z),
                    m_normal = normal
                };

                vertices[vert_idx + 6] = new Vertex
                {
                    m_position = new Vector3(pos_x, bot_y, pos_z_plus_one),
                    m_normal = normal
                };

                vertices[vert_idx + 7] = new Vertex
                {
                    m_position = new Vector3(pos_x_plus_one, bot_y, pos_z_plus_one),
                    m_normal = normal
                };

                triangles[triangle_idx + 0] = vert_idx + 0;
                triangles[triangle_idx + 1] = vert_idx + 2;
                triangles[triangle_idx + 2] = vert_idx + 1;
                triangles[triangle_idx + 3] = vert_idx + 1;
                triangles[triangle_idx + 4] = vert_idx + 2;
                triangles[triangle_idx + 5] = vert_idx + 3;

                triangles[triangle_idx + 6] = vert_idx + 0;
                triangles[triangle_idx + 7] = vert_idx + 1;
                triangles[triangle_idx + 8] = vert_idx + 4;
                triangles[triangle_idx + 9] = vert_idx + 4;
                triangles[triangle_idx + 10] = vert_idx + 1;
                triangles[triangle_idx + 11] = vert_idx + 5;

                triangles[triangle_idx + 12] = vert_idx + 0;
                triangles[triangle_idx + 13] = vert_idx + 4;
                triangles[triangle_idx + 14] = vert_idx + 2;
                triangles[triangle_idx + 15] = vert_idx + 2;
                triangles[triangle_idx + 16] = vert_idx + 4;
                triangles[triangle_idx + 17] = vert_idx + 6;

                triangles[triangle_idx + 18] = vert_idx + 1;
                triangles[triangle_idx + 19] = vert_idx + 3;
                triangles[triangle_idx + 20] = vert_idx + 5;
                triangles[triangle_idx + 21] = vert_idx + 3;
                triangles[triangle_idx + 22] = vert_idx + 7;
                triangles[triangle_idx + 23] = vert_idx + 5;

                triangles[triangle_idx + 24] = vert_idx + 2;
                triangles[triangle_idx + 25] = vert_idx + 6;
                triangles[triangle_idx + 26] = vert_idx + 3;
                triangles[triangle_idx + 27] = vert_idx + 3;
                triangles[triangle_idx + 28] = vert_idx + 6;
                triangles[triangle_idx + 29] = vert_idx + 7;

                vert_idx += 8;
                triangle_idx += 30;
            }
        }

        mesh.SetVertexBufferData(vertices, 0, 0, vertex_count);
        mesh.SetTriangles(triangles, 0, false);
        mesh.RecalculateBounds();

        return mesh;
    }

    public void Render(float dt, Color color)
    {
        m_material.color = color;
        Graphics.DrawMesh(m_mesh, Matrix4x4.identity, m_material, 0);
    }

    bool[] m_grid;
    int m_width_in_voxels;
    int m_height_in_voxels;
    Mesh m_mesh;
    Mesh m_marched_mesh;
    Material m_material;
    MeshCollider m_collider;        
    int m_voxel_count;

    VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
    };
}