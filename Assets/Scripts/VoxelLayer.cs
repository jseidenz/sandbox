﻿using UnityEngine;
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
        m_density_grid = new float[width_in_voxels * height_in_voxels];
        m_width_in_voxels = width_in_voxels;
        m_height_in_voxels = height_in_voxels;

        var collider_go = new GameObject("VoxelCollider");
        collider_go.gameObject.SetActive(false);
        m_collider = collider_go.AddComponent<MeshCollider>();
        m_material = material;
    }

    public void ApplyHeightmap(Color[] pixels, float min_height, float max_height)
    {
        float one_over_height_range = 1f / (max_height - min_height);

        for (int y = 0; y < m_height_in_voxels; ++y)
        {
            for (int x = 0; x < m_width_in_voxels; ++x)
            {
                var cell_idx = y * m_width_in_voxels + x;
                var height = pixels[cell_idx].r;
                bool is_filled = height >= min_height;
                m_grid[cell_idx] = is_filled;

                float density = Mathf.Clamp01((height - min_height) * one_over_height_range);

                m_density_grid[cell_idx] = density;
                if (is_filled)
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

    struct TriangleWriter
    {
        public int m_triangle_idx;
        public int[] m_triangles;


        public void Triangle(int vert_idx_a, int vert_idx_b, int vert_idx_c)
        {
            m_triangles[m_triangle_idx++] = vert_idx_a;
            m_triangles[m_triangle_idx++] = vert_idx_b;
            m_triangles[m_triangle_idx++] = vert_idx_c;
        }
    }



    struct VertexWriter
    {
        public float m_left_x;
        public float m_right_x;
        public float m_near_z;
        public float m_far_z;
        public float m_bot_y;
        public float m_top_y;
        public float m_left_near_density;
        public float m_right_near_density;
        public float m_left_far_density;
        public float m_right_far_density;
        public int m_vert_idx;
        public Vector3 m_normal;
        public Vertex[] m_vertices;

        public int LeftTopNear()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(m_left_x, m_top_y, m_near_z),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int RightTopNear()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(m_right_x, m_top_y, m_near_z),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int LeftTopFar()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(m_left_x, m_top_y, m_far_z),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int RightTopFar()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(m_right_x, m_top_y, m_far_z),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int LeftTopEdge()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(m_left_x, m_top_y, InterpolatePosition(m_near_z, m_far_z, m_left_near_density, m_left_far_density)),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int RightTopEdge()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(m_right_x, m_top_y, InterpolatePosition(m_near_z, m_far_z, m_right_near_density, m_right_far_density)),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int NearTopEdge()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(InterpolatePosition(m_left_x, m_right_x, m_left_near_density, m_right_near_density), m_top_y, m_near_z),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        public int FarTopEdge()
        {
            m_vertices[m_vert_idx] = new Vertex
            {
                m_position = new Vector3(InterpolatePosition(m_left_x, m_right_x, m_left_far_density, m_right_far_density), m_top_y, m_far_z),
                m_normal = m_normal
            };
            return m_vert_idx++;
        }

        static float InterpolatePosition(float pos_a, float pos_b, float density_a, float density_b)
        {
            if(density_a < density_b)
            {
                return pos_a + (density_b - density_a) * (pos_b - pos_a);
            }
            else
            {
                return pos_b + (density_a - density_b) * (pos_a - pos_b);
            }            
        }
    }


    public Mesh MarchMesh(float bot_y, float top_y)
    {
        var mesh = new Mesh();
        mesh.name = $"VoxelLayer({bot_y})";

        var max_vertex_count = m_width_in_voxels * m_height_in_voxels * 4;
        var max_triangle_count = max_vertex_count * 4;

        var vertices = new Vertex[max_vertex_count];

        float voxel_size = 1f;
        int vert_idx = 0;

        var tris = new TriangleWriter
        {
            m_triangle_idx = 0,
            m_triangles = new int[max_triangle_count]
        };

        for (int y = 0; y < m_height_in_voxels - 1; ++y)
        {
            for (int x = 0; x < m_width_in_voxels - 1; ++x)
            {
                int left_near_cell_idx = y * m_width_in_voxels + x;

                var left_near_density = m_density_grid[left_near_cell_idx];
                var right_near_density = m_density_grid[left_near_cell_idx + 1];
                var left_far_density = m_density_grid[left_near_cell_idx + m_width_in_voxels];
                var right_far_density = m_density_grid[left_near_cell_idx + m_width_in_voxels + 1];

                int sample_type = 0;
                if (left_near_density > 0) sample_type |= 1;
                if (right_near_density > 0) sample_type |= 2;
                if (right_far_density > 0) sample_type |= 4;
                if (left_far_density > 0) sample_type |= 8;

                if (sample_type == 0) continue;

                var left_x = (float)x;
                var right_x = left_x + voxel_size;
                var near_z = (float)y;
                var far_z = near_z + voxel_size;

                var normal = Vector3.up;

                var verts = new VertexWriter
                {
                    m_left_x = left_x,
                    m_right_x = right_x,
                    m_near_z = near_z,
                    m_far_z = far_z,
                    m_bot_y = bot_y,
                    m_top_y = top_y,
                    m_left_near_density = left_near_density,
                    m_right_near_density = right_near_density,
                    m_left_far_density = left_far_density,
                    m_right_far_density = right_far_density,
                    m_vert_idx = vert_idx,
                    m_normal = normal,
                    m_vertices = vertices
                };


                if(sample_type == 1)
                {
                    tris.Triangle(verts.LeftTopNear(), verts.LeftTopEdge(), verts.NearTopEdge());
                }
                else if(sample_type == 2)
                {
                    tris.Triangle(verts.NearTopEdge(), verts.RightTopEdge(), verts.RightTopNear());
                }
                else if (sample_type == 3)
                {
                    var left_top_edge = verts.LeftTopEdge();
                    var right_top_near = verts.RightTopNear();
                    var left_top_near = verts.LeftTopNear();
                    var right_top_edge = verts.RightTopEdge();

                    tris.Triangle(left_top_edge, right_top_near, left_top_near);
                    tris.Triangle(left_top_edge, right_top_edge, right_top_near);
                }
                else if (sample_type == 4)
                {
                    tris.Triangle(verts.FarTopEdge(), verts.RightTopFar(), verts.RightTopEdge());
                }
                else if (sample_type == 5)
                {
                    var left_top_near = verts.LeftTopNear();
                    var left_top_edge = verts.LeftTopEdge();
                    var near_top_edge = verts.NearTopEdge();
                    var right_top_edge = verts.RightTopEdge();
                    var far_top_edge = verts.FarTopEdge();
                    var right_top_far = verts.RightTopFar();

                    tris.Triangle(left_top_near, left_top_edge, near_top_edge);
                    tris.Triangle(left_top_edge, right_top_edge, near_top_edge);
                    tris.Triangle(left_top_edge, far_top_edge, right_top_edge);
                    tris.Triangle(far_top_edge, right_top_far, right_top_edge);
                }
                else if (sample_type == 6)
                {
                    var far_top_edge = verts.FarTopEdge();
                    var right_top_far = verts.RightTopFar();
                    var right_top_near = verts.RightTopNear();
                    var near_top_edge = verts.NearTopEdge();

                    tris.Triangle(far_top_edge, right_top_far, right_top_near);
                    tris.Triangle(far_top_edge, right_top_near, near_top_edge);
                }
                else if (sample_type == 7)
                {
                    var left_top_edge = verts.LeftTopEdge();
                    var right_top_near = verts.RightTopNear();
                    var left_top_near = verts.LeftTopNear();
                    var far_top_edge = verts.FarTopEdge();
                    var right_top_far = verts.RightTopFar();

                    tris.Triangle(left_top_edge, right_top_near, left_top_near);
                    tris.Triangle(left_top_edge, far_top_edge, right_top_near);
                    tris.Triangle(far_top_edge, right_top_far, right_top_near);
                }
                else if (sample_type == 8)
                {
                    tris.Triangle(verts.LeftTopFar(), verts.FarTopEdge(), verts.LeftTopEdge());
                }
                else if (sample_type == 9)
                {
                    var left_top_far = verts.LeftTopFar();
                    var far_top_edge = verts.FarTopEdge();
                    var near_top_edge = verts.NearTopEdge();
                    var left_top_near = verts.LeftTopNear();

                    tris.Triangle(left_top_far, far_top_edge, near_top_edge);
                    tris.Triangle(left_top_far, near_top_edge, left_top_near);
                }
                else if (sample_type == 10)
                {
                    var left_top_far = verts.LeftTopFar();
                    var left_top_edge = verts.LeftTopEdge();
                    var near_top_edge = verts.NearTopEdge();
                    var right_top_edge = verts.RightTopEdge();
                    var far_top_edge = verts.FarTopEdge();
                    var right_top_near = verts.RightTopNear();

                    tris.Triangle(left_top_far, far_top_edge, left_top_edge);
                    tris.Triangle(left_top_edge, far_top_edge, right_top_edge);
                    tris.Triangle(left_top_edge, right_top_edge, near_top_edge);
                    tris.Triangle(near_top_edge, right_top_edge, right_top_near);
                }
                else if (sample_type == 11)
                {
                    var left_top_far = verts.LeftTopFar();
                    var far_top_edge = verts.FarTopEdge();
                    var left_top_near = verts.LeftTopNear();
                    var right_top_edge = verts.RightTopEdge();
                    var right_top_near = verts.RightTopNear();

                    tris.Triangle(left_top_far, far_top_edge, left_top_near);
                    tris.Triangle(left_top_near, far_top_edge, right_top_edge);
                    tris.Triangle(left_top_near, right_top_edge, right_top_near);
                }
                else if (sample_type == 12)
                {
                    var left_top_far = verts.LeftTopFar();
                    var right_top_far = verts.RightTopFar();
                    var left_top_edge = verts.LeftTopEdge();
                    var right_top_edge = verts.RightTopEdge();

                    tris.Triangle(left_top_far, right_top_far, left_top_edge);
                    tris.Triangle(left_top_edge, right_top_far, right_top_edge);
                }
                else if (sample_type == 13)
                {
                    var left_top_near = verts.LeftTopNear();
                    var left_top_far = verts.LeftTopFar();
                    var near_top_edge = verts.NearTopEdge();
                    var right_top_edge = verts.RightTopEdge();
                    var right_top_far = verts.RightTopFar();

                    tris.Triangle(left_top_near, left_top_far, near_top_edge);
                    tris.Triangle(left_top_far, right_top_edge, near_top_edge);
                    tris.Triangle(left_top_far, right_top_far, right_top_edge);
                }
                else if (sample_type == 14)
                {
                    var left_top_far = verts.LeftTopFar();
                    var right_top_far = verts.RightTopFar();
                    var left_top_edge = verts.LeftTopEdge();
                    var near_top_edge = verts.NearTopEdge();
                    var right_top_near = verts.RightTopNear();

                    tris.Triangle(left_top_far, right_top_far, left_top_edge);
                    tris.Triangle(left_top_edge, right_top_far, near_top_edge);
                    tris.Triangle(near_top_edge, right_top_far, right_top_near);

                }
                else if (sample_type == 15)
                {
                    var left_top_near = verts.LeftTopNear();
                    var left_top_far = verts.LeftTopFar();
                    var right_top_near = verts.RightTopNear();
                    var right_top_far = verts.RightTopFar();

                    tris.Triangle(left_top_near, left_top_far, right_top_near);
                    tris.Triangle(left_top_far, right_top_far, right_top_near);
                }

                vert_idx = verts.m_vert_idx;

            }
        }

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertexBufferParams(vert_idx, m_vertex_attribute_descriptors);

        mesh.SetVertexBufferData(vertices, 0, 0, vert_idx);
        mesh.SetTriangles(tris.m_triangles, 0, tris.m_triangle_idx, 0, false);
        mesh.RecalculateBounds();

        return mesh;
    }

    public void Render(float dt, Color color)
    {
        m_material.color = color;
        Graphics.DrawMesh(m_marched_mesh, Matrix4x4.identity, m_material, 0);
    }


    bool[] m_grid;
    float[] m_density_grid;
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