﻿using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class VoxelChunk
{
    const int SAMPLE_TYPE_FULL_SQUARE = 15;
    const int SAMPLE_TYPE_EMTPY = 0;

    public struct ScratchBuffer
    {
        public static ScratchBuffer CreateScratchBuffer()
        {
            var scratch_buffer = new ScratchBuffer();

            scratch_buffer.m_vertices = new Vertex[System.UInt16.MaxValue];
            scratch_buffer.m_triangles = new System.UInt16[System.UInt16.MaxValue * 24];
            scratch_buffer.m_edges = new Edge[System.UInt16.MaxValue];
            scratch_buffer.m_vertex_id_to_vertex_idx = new Dictionary<uint, ushort>();
            scratch_buffer.m_vertex_entries = new VertexEntry[System.UInt16.MaxValue];
            scratch_buffer.m_vert_idx_to_normal_welding_info = new Dictionary<ushort, NormalWeldingInfo>();

            return scratch_buffer;
        }


        public Vertex[] m_vertices;
        public System.UInt16[] m_triangles;
        public Edge[] m_edges;
        public Dictionary<uint, ushort> m_vertex_id_to_vertex_idx;
        public VertexEntry[] m_vertex_entries;
        public Dictionary<ushort, NormalWeldingInfo> m_vert_idx_to_normal_welding_info;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 m_position;
        public Vector3 m_normal;
    }

    public struct Edge
    {
        public System.UInt16 m_vertex_idx_a;
        public System.UInt16 m_vertex_idx_b;
    }

    public struct NormalWeldingInfo
    {
        public Vector3 m_normal;
        public int m_normal_count;
    }

    public struct VertexEntry
    {
        public VertexLocation m_vertex_location;
        public ushort m_vertex_idx;
        public Vector3 m_position;
    }

    [System.Flags]
    public enum VertexLocation : ushort
    {
        Left = 1,
        Right = 2,
        Near = 4,
        Far = 8,
        Bot = 16,
        Top = 32,
        LeftNearTop = Left | Near | Top,
        RightNearTop = Right | Near | Top,
        LeftFarTop = Left | Far | Top,
        RightFarTop = Right | Far | Top,
        LeftTop = Left | Top,
        RightTop = Right | Top,
        NearTop = Near | Top,
        FarTop = Far | Top,
        LeftNearBot = Left | Near | Bot,
        RightNearBot = Right | Near | Bot,
        LeftFarBot = Left | Far | Bot,
        RightFarBot = Right | Far | Bot,
        LeftBot = Left | Bot,
        RightBot = Right | Bot,
        NearBot = Near | Bot,
        FarBot = Far | Bot,
    }

    public VoxelChunk(
        int density_grid_x, 
        int density_grid_y, 
        int dimensions_in_voxels, 
        int layer_width_in_voxels, 
        int layer_height_in_voxels, 
        float[] layer_density_grid, 
        bool[] layer_occlusion_grid, 
        float voxel_size_in_meters, 
        float iso_level, 
        float bot_y, 
        float top_y
        )
    {
        m_density_grid_x = density_grid_x;
        m_density_grid_y = density_grid_y;
        m_chunk_dimension_in_voxels = dimensions_in_voxels;
        m_layer_density_grid = layer_density_grid;
        m_layer_occlusion_grid = layer_occlusion_grid;
        m_layer_width_in_voxels = layer_width_in_voxels;
        m_layer_height_in_voxels = layer_height_in_voxels;
        m_iso_level = iso_level;
        m_voxel_size_in_meters = voxel_size_in_meters;

        m_mesh = new Mesh();
        m_mesh.name = "VoxelChunk";

        var collider_go = new GameObject("VoxelCollider");
        collider_go.gameObject.SetActive(false);
        m_collider = collider_go.AddComponent<MeshCollider>();
        m_collider.sharedMesh = m_mesh;

        m_bot_y = bot_y;
        m_top_y = top_y;
    }

    public void SetAboveAndBelowOcclusionGrids(bool[] layer_above_occlusion_grid, bool[] layer_below_occlusion_grid)
    {
        m_layer_above_occlusion_grid = layer_above_occlusion_grid;
        m_layer_below_occlusion_grid = layer_below_occlusion_grid;
    }

    public bool Triangulate(VoxelChunk.ScratchBuffer scratch_buffer)
    {
        Profiler.BeginSample("March");
        bool has_occlusion_changed = MarchMesh(scratch_buffer);
        Profiler.EndSample();


        Profiler.BeginSample("UpdateCollision");
        m_collider.gameObject.SetActive(false);
        if (!m_is_empty)
        {
            m_collider.gameObject.SetActive(true);
        }
        Profiler.EndSample();

        return has_occlusion_changed;
    }

    struct MeshMarcher
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
        public float m_iso_level;
        public int m_triangle_idx;
        public int m_chunk_dimensions_in_voxels;
        public System.UInt16[] m_triangles;
        public int m_edge_idx;        
        public Edge[] m_edges;
        public ushort m_chunk_relative_cell_idx;
        public Dictionary<uint, ushort> m_vertex_id_to_vertex_idx;
        public VertexEntry[] m_vertex_entries;

        public ushort CreateVertexEntry(VertexLocation location, Vector3 position, int chunk_relative_cell_idx)
        {
            var shifted_cell_number = ((uint)chunk_relative_cell_idx) << 16;
            var shifted_location = (uint)location;
            var vertex_id = shifted_cell_number | shifted_location;

            m_vertex_id_to_vertex_idx.TryGetValue(vertex_id, out var vertex_idx);
            {
                vertex_idx = (ushort)m_vertex_id_to_vertex_idx.Count;
                m_vertex_id_to_vertex_idx[vertex_id] = vertex_idx;

                m_vertex_entries[vertex_idx] = new VertexEntry
                {
                    m_vertex_location = location,
                    m_vertex_idx = vertex_idx,
                    m_position = position
                };
            }

            return vertex_idx;
        }

        public ushort LeftNear()
        {
            var pos = new Vector3(m_left_x, m_top_y, m_near_z);
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + 0;
            return CreateVertexEntry(VertexLocation.LeftNearTop, pos, chunk_relative_cell_idx);
        }

        public ushort RightNear()
        {
            var pos = new Vector3(m_right_x, m_top_y, m_near_z);
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + 1;
            return CreateVertexEntry(VertexLocation.LeftNearTop, pos, chunk_relative_cell_idx);
        }

        public ushort LeftFar()
        {
            var pos = new Vector3(m_left_x, m_top_y, m_far_z);
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + m_chunk_dimensions_in_voxels;
            return CreateVertexEntry(VertexLocation.LeftNearTop, pos, chunk_relative_cell_idx);
        }

        public ushort RightFar()
        {
            var pos = new Vector3(m_right_x, m_top_y, m_far_z);
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + m_chunk_dimensions_in_voxels + 1;
            return CreateVertexEntry(VertexLocation.LeftNearTop, pos, chunk_relative_cell_idx);
        }

        public ushort LeftEdge()
        {
            var pos = new Vector3(m_left_x, m_top_y, InterpolatePosition(m_near_z, m_far_z, m_left_near_density, m_left_far_density));
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + 0;
            return CreateVertexEntry(VertexLocation.LeftTop, pos, chunk_relative_cell_idx);
        }

        public ushort RightEdge()
        {
            var pos = new Vector3(m_right_x, m_top_y, InterpolatePosition(m_near_z, m_far_z, m_right_near_density, m_right_far_density));
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + 1;
            return CreateVertexEntry(VertexLocation.LeftTop, pos, chunk_relative_cell_idx);
        }

        public ushort NearEdge()
        {
            var pos = new Vector3(InterpolatePosition(m_left_x, m_right_x, m_left_near_density, m_right_near_density), m_top_y, m_near_z);
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + 0;
            return CreateVertexEntry(VertexLocation.NearTop, pos, chunk_relative_cell_idx);
        }

        public ushort FarEdge()
        {
            var pos = new Vector3(InterpolatePosition(m_left_x, m_right_x, m_left_far_density, m_right_far_density), m_top_y, m_far_z);
            var chunk_relative_cell_idx = m_chunk_relative_cell_idx + m_chunk_dimensions_in_voxels;
            return CreateVertexEntry(VertexLocation.NearTop, pos, chunk_relative_cell_idx);
        }

        public float AverageDensity()
        {
            return (m_left_near_density + m_left_far_density + m_right_near_density + m_right_far_density) / 4f;
        }

        float InterpolatePosition(float pos_a, float pos_b, float density_a, float density_b)
        {
            return pos_a + (m_iso_level - density_a) * (pos_b - pos_a) / (density_b - density_a);
        }

        public void Triangle(System.UInt16 vert_idx_a, System.UInt16 vert_idx_b, System.UInt16 vert_idx_c)
        {
            m_triangles[m_triangle_idx++] = vert_idx_a;
            m_triangles[m_triangle_idx++] = vert_idx_b;
            m_triangles[m_triangle_idx++] = vert_idx_c;
        }

        public void ExtrudeTopToBot(System.UInt16 vert_idx_a, System.UInt16 vert_idx_b)
        {
            m_edges[m_edge_idx++] = new Edge
            {
                m_vertex_idx_a = vert_idx_a,
                m_vertex_idx_b = vert_idx_b
            };
        }
    }


    bool MarchMesh(ScratchBuffer scratch_buffer)
    {
        var mesh = m_mesh;
        mesh.Clear();

        int triangle_idx = 0;
        int edge_idx = 0;

        bool has_occlusion_changed = false;

        scratch_buffer.m_vertex_id_to_vertex_idx.Clear();

        for (int y = m_density_grid_y; y < m_density_grid_y + m_chunk_dimension_in_voxels; ++y)
        {
            var near_z = (float)y * m_voxel_size_in_meters;
            var far_z = near_z + m_voxel_size_in_meters;

            var top_density_idx_offset = m_layer_width_in_voxels;
            if (y == m_layer_height_in_voxels - 1)
            {
                top_density_idx_offset = 0;
            }

            for (int x = m_density_grid_x; x < m_density_grid_x + m_chunk_dimension_in_voxels; ++x)
            {
                int left_near_cell_idx = y * m_layer_width_in_voxels + x;

                var right_density_idx_offset = 1;
                if(x == m_layer_width_in_voxels - 1)
                {
                    right_density_idx_offset = 0;
                }

                var left_near_density = m_layer_density_grid[left_near_cell_idx];
                var right_near_density = m_layer_density_grid[left_near_cell_idx + right_density_idx_offset];
                var left_far_density = m_layer_density_grid[left_near_cell_idx + top_density_idx_offset];
                var right_far_density = m_layer_density_grid[left_near_cell_idx + top_density_idx_offset + right_density_idx_offset];

                int sample_type = 0;
                if (left_near_density >= m_iso_level) sample_type |= 1;
                if (right_near_density >= m_iso_level) sample_type |= 2;
                if (right_far_density >= m_iso_level) sample_type |= 4;
                if (left_far_density >= m_iso_level) sample_type |= 8;

                if (sample_type != SAMPLE_TYPE_FULL_SQUARE)
                {
                    var is_occluding = false;
                    if (m_layer_occlusion_grid[left_near_cell_idx] != is_occluding)
                    {
                        has_occlusion_changed = true;
                        m_layer_occlusion_grid[left_near_cell_idx] = is_occluding;
                    }

                    if(sample_type == SAMPLE_TYPE_EMTPY)
                    {
                        continue;
                    }
                }
                else
                {
                    var is_occluding = true;
                    if (m_layer_occlusion_grid[left_near_cell_idx] != is_occluding)
                    {
                        has_occlusion_changed = true;
                        m_layer_occlusion_grid[left_near_cell_idx] = is_occluding;
                    }
                    
                    bool is_occluded = m_layer_above_occlusion_grid[left_near_cell_idx];
                    if (is_occluded) continue;
                }             


                int chunk_relative_x = x - m_density_grid_x;
                int chunk_relative_y = y - m_density_grid_y;
                int chunk_relative_cell_idx = chunk_relative_y * m_chunk_dimension_in_voxels + chunk_relative_x;

                var left_x = (float)x * m_voxel_size_in_meters;
                var right_x = left_x + m_voxel_size_in_meters;

                var marcher = new MeshMarcher
                {
                    m_left_x = left_x,
                    m_right_x = right_x,
                    m_near_z = near_z,
                    m_far_z = far_z,
                    m_bot_y = m_bot_y,
                    m_top_y = m_top_y,
                    m_left_near_density = left_near_density,
                    m_right_near_density = right_near_density,
                    m_left_far_density = left_far_density,
                    m_right_far_density = right_far_density,
                    m_iso_level = m_iso_level,
                    m_triangle_idx = triangle_idx,
                    m_chunk_dimensions_in_voxels = m_chunk_dimension_in_voxels,
                    m_triangles = scratch_buffer.m_triangles,
                    m_edge_idx = edge_idx,
                    m_edges = scratch_buffer.m_edges,
                    m_chunk_relative_cell_idx = (ushort)chunk_relative_cell_idx,
                    m_vertex_id_to_vertex_idx = scratch_buffer.m_vertex_id_to_vertex_idx,
                    m_vertex_entries = scratch_buffer.m_vertex_entries
                };


                if(sample_type == 1)
                {
                    var left_near = marcher.LeftNear();
                    var left_edge = marcher.LeftEdge();
                    var near_edge = marcher.NearEdge();

                    marcher.Triangle(left_near, left_edge, near_edge);
                    marcher.ExtrudeTopToBot(near_edge, left_edge);
                }
                else if(sample_type == 2)
                {
                    var near_edge = marcher.NearEdge();
                    var right_edge = marcher.RightEdge();
                    var right_near = marcher.RightNear();

                    marcher.Triangle(near_edge, right_edge, right_near);
                    marcher.ExtrudeTopToBot(right_edge, near_edge);
                }
                else if (sample_type == 3)
                {
                    var left_edge = marcher.LeftEdge();
                    var right_near = marcher.RightNear();
                    var left_near = marcher.LeftNear();
                    var right_edge = marcher.RightEdge();

                    marcher.Triangle(left_edge, right_near, left_near);
                    marcher.Triangle(left_edge, right_edge, right_near);
                    marcher.ExtrudeTopToBot(right_edge, left_edge);
                }
                else if (sample_type == 4)
                {
                    var far_edge = marcher.FarEdge();
                    var right_far = marcher.RightFar();
                    var right_edge = marcher.RightEdge();

                    marcher.Triangle(far_edge, right_far, right_edge);
                    marcher.ExtrudeTopToBot(far_edge, right_edge);
                }
                else if (sample_type == 5)
                {
                    if (marcher.AverageDensity() > m_iso_level)
                    {
                        var left_near = marcher.LeftNear();
                        var left_edge = marcher.LeftEdge();
                        var near_edge = marcher.NearEdge();
                        var right_edge = marcher.RightEdge();
                        var far_edge = marcher.FarEdge();
                        var right_far = marcher.RightFar();

                        marcher.Triangle(left_near, left_edge, near_edge);
                        marcher.Triangle(left_edge, right_edge, near_edge);
                        marcher.Triangle(left_edge, far_edge, right_edge);
                        marcher.Triangle(far_edge, right_far, right_edge);
                        marcher.ExtrudeTopToBot(far_edge, left_edge);
                        marcher.ExtrudeTopToBot(near_edge, right_edge);
                    }
                    else
                    {
                        var left_near = marcher.LeftNear();
                        var left_edge = marcher.LeftEdge();
                        var near_edge = marcher.NearEdge();
                        var far_edge = marcher.FarEdge();
                        var right_far = marcher.RightFar();
                        var right_edge = marcher.RightEdge();

                        marcher.Triangle(left_near, left_edge, near_edge);
                        marcher.Triangle(far_edge, right_far, right_edge);
                        marcher.ExtrudeTopToBot(near_edge, left_edge);
                        marcher.ExtrudeTopToBot(far_edge, right_edge);
                    }
                }
                else if (sample_type == 6)
                {
                    var far_edge = marcher.FarEdge();
                    var right_far = marcher.RightFar();
                    var right_near = marcher.RightNear();
                    var near_edge = marcher.NearEdge();

                    marcher.Triangle(far_edge, right_far, right_near);
                    marcher.Triangle(far_edge, right_near, near_edge);
                    marcher.ExtrudeTopToBot(far_edge, near_edge);
                }
                else if (sample_type == 7)
                {
                    var left_edge = marcher.LeftEdge();
                    var right_near = marcher.RightNear();
                    var left_near = marcher.LeftNear();
                    var far_edge = marcher.FarEdge();
                    var right_far = marcher.RightFar();

                    marcher.Triangle(left_edge, right_near, left_near);
                    marcher.Triangle(left_edge, far_edge, right_near);
                    marcher.Triangle(far_edge, right_far, right_near);
                    marcher.ExtrudeTopToBot(far_edge, left_edge);
                }
                else if (sample_type == 8)
                {
                    var left_far = marcher.LeftFar();
                    var far_edge = marcher.FarEdge();
                    var left_edge = marcher.LeftEdge();

                    marcher.Triangle(left_far, far_edge, left_edge);
                    marcher.ExtrudeTopToBot(left_edge, far_edge);
                }
                else if (sample_type == 9)
                {
                    var left_far = marcher.LeftFar();
                    var far_edge = marcher.FarEdge();
                    var near_edge = marcher.NearEdge();
                    var left_near = marcher.LeftNear();

                    marcher.Triangle(left_far, far_edge, near_edge);
                    marcher.Triangle(left_far, near_edge, left_near);
                    marcher.ExtrudeTopToBot(near_edge, far_edge);
                }
                else if (sample_type == 10)
                {
                    if (marcher.AverageDensity() > m_iso_level)
                    {
                        var left_far = marcher.LeftFar();
                        var left_edge = marcher.LeftEdge();
                        var near_edge = marcher.NearEdge();
                        var right_edge = marcher.RightEdge();
                        var far_edge = marcher.FarEdge();
                        var right_near = marcher.RightNear();

                        marcher.Triangle(left_far, far_edge, left_edge);
                        marcher.Triangle(left_edge, far_edge, right_edge);
                        marcher.Triangle(left_edge, right_edge, near_edge);
                        marcher.Triangle(near_edge, right_edge, right_near);
                        marcher.ExtrudeTopToBot(left_edge, near_edge);
                        marcher.ExtrudeTopToBot(right_edge, far_edge);
                    }
                    else
                    {
                        var near_edge = marcher.NearEdge();
                        var right_edge = marcher.RightEdge();
                        var right_near = marcher.RightNear();
                        var left_edge = marcher.LeftEdge();
                        var far_edge = marcher.FarEdge();
                        var left_far = marcher.LeftFar();

                        marcher.Triangle(near_edge, right_edge, right_near);
                        marcher.Triangle(left_far, far_edge, left_edge);
                        marcher.ExtrudeTopToBot(left_edge, far_edge);
                        marcher.ExtrudeTopToBot(right_edge, near_edge);

                    }
                }
                else if (sample_type == 11)
                {
                    var left_far = marcher.LeftFar();
                    var far_edge = marcher.FarEdge();
                    var left_near = marcher.LeftNear();
                    var right_edge = marcher.RightEdge();
                    var right_near = marcher.RightNear();

                    marcher.Triangle(left_far, far_edge, left_near);
                    marcher.Triangle(left_near, far_edge, right_edge);
                    marcher.Triangle(left_near, right_edge, right_near);
                    marcher.ExtrudeTopToBot(right_edge, far_edge);
                }
                else if (sample_type == 12)
                {
                    var left_far = marcher.LeftFar();
                    var right_far = marcher.RightFar();
                    var left_edge = marcher.LeftEdge();
                    var right_edge = marcher.RightEdge();

                    marcher.Triangle(left_far, right_far, left_edge);
                    marcher.Triangle(left_edge, right_far, right_edge);
                    marcher.ExtrudeTopToBot(left_edge, right_edge);
                }
                else if (sample_type == 13)
                {
                    var left_near = marcher.LeftNear();
                    var left_far = marcher.LeftFar();
                    var near_edge = marcher.NearEdge();
                    var right_edge = marcher.RightEdge();
                    var right_far = marcher.RightFar();

                    marcher.Triangle(left_near, left_far, near_edge);
                    marcher.Triangle(left_far, right_edge, near_edge);
                    marcher.Triangle(left_far, right_far, right_edge);
                    marcher.ExtrudeTopToBot(near_edge, right_edge);
                }
                else if (sample_type == 14)
                {
                    var left_far = marcher.LeftFar();
                    var right_far = marcher.RightFar();
                    var left_edge = marcher.LeftEdge();
                    var near_edge = marcher.NearEdge();
                    var right_near = marcher.RightNear();

                    marcher.Triangle(left_far, right_far, left_edge);
                    marcher.Triangle(left_edge, right_far, near_edge);
                    marcher.Triangle(near_edge, right_far, right_near);
                    marcher.ExtrudeTopToBot(left_edge, near_edge);

                }
                else if (sample_type == SAMPLE_TYPE_FULL_SQUARE)
                {
                    var left_near = marcher.LeftNear();
                    var left_far = marcher.LeftFar();
                    var right_near = marcher.RightNear();
                    var right_far = marcher.RightFar();

                    marcher.Triangle(left_near, left_far, right_near);
                    marcher.Triangle(left_far, right_far, right_near);
                }

                edge_idx = marcher.m_edge_idx;
                triangle_idx = marcher.m_triangle_idx;
            }
        }

        var normal = Vector3.up;


        var vertex_entry_count = scratch_buffer.m_vertex_id_to_vertex_idx.Count;
        for (int i = 0; i < vertex_entry_count; ++i)
        {
            var entry = scratch_buffer.m_vertex_entries[i];

            scratch_buffer.m_vertices[entry.m_vertex_idx] = new Vertex
            {
                m_position = entry.m_position,
                m_normal = normal
            };
        }


        ushort vert_idx = (ushort)vertex_entry_count;

        scratch_buffer.m_vert_idx_to_normal_welding_info.Clear();
        FinalizeEdges(scratch_buffer.m_vertices, scratch_buffer.m_triangles, scratch_buffer.m_edges, scratch_buffer.m_vert_idx_to_normal_welding_info, ref vert_idx, ref triangle_idx, ref edge_idx);

        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertexBufferParams(vert_idx, m_vertex_attribute_descriptors);

        mesh.SetVertexBufferData(scratch_buffer.m_vertices, 0, 0, vert_idx);
        mesh.SetTriangles(scratch_buffer.m_triangles, 0, triangle_idx, 0, false);
        mesh.RecalculateBounds();

        m_is_empty = triangle_idx == 0;

        return has_occlusion_changed;
    }

    void FinalizeEdges(
        Vertex[] vertices, 
        ushort[] triangles, 
        Edge[] edges,
        Dictionary<ushort, NormalWeldingInfo> vertex_id_to_normal_welding_info,
        ref ushort vert_idx, 
        ref int triangle_idx, 
        ref int edge_count
        )
    {
        for (int i = 0; i < edge_count; ++i)
        {
            var edge = edges[i];
            var vert_idx_a = edge.m_vertex_idx_a;
            var vert_idx_b = edge.m_vertex_idx_b;

            var vert_a = vertices[vert_idx_a];
            var vert_b = vertices[vert_idx_b];

            var vert_c = vert_a;
            vert_c.m_position.y = m_bot_y;

            var vert_d = vert_b;
            vert_d.m_position.y = m_bot_y;

            var normal = Vector3.Cross(vert_b.m_position - vert_a.m_position, vert_c.m_position - vert_a.m_position).normalized;
            var packed_normal = new Vector2(normal.x, normal.z);
            vert_c.m_normal = packed_normal;
            vert_d.m_normal = packed_normal;

            var vert_idx_c = vert_idx;
            vertices[vert_idx++] = vert_c;

            var vert_idx_d = vert_idx;
            vertices[vert_idx++] = vert_d;

            triangles[triangle_idx++] = vert_idx_a;
            triangles[triangle_idx++] = vert_idx_b;
            triangles[triangle_idx++] = vert_idx_c;
            triangles[triangle_idx++] = vert_idx_c;
            triangles[triangle_idx++] = vert_idx_b;
            triangles[triangle_idx++] = vert_idx_d;
        }

    /*
    for(int i = 0; i < edge_count; ++i)
    {
        var edge = edges[i];

        var vert_idx_a = edge.m_vertex_idx_a;
        var vert_idx_b = edge.m_vertex_idx_b;

        var vert_a = vertices[vert_idx_a];  
        var vert_b = vertices[vert_idx_b];

        var vert_c = vert_a;
        vert_c.m_position.y = m_bot_y;

        var normal = Vector3.Cross(vert_b.m_position - vert_a.m_position, vert_c.m_position - vert_a.m_position).normalized;

        vertex_id_to_normal_welding_info.TryGetValue(vert_idx_a, out var welding_info_a);
        welding_info_a.m_normal += normal;
        welding_info_a.m_normal_count++;
        vertex_id_to_normal_welding_info[vert_idx_a] = welding_info_a;

        vertex_id_to_normal_welding_info.TryGetValue(vert_idx_b, out var welding_info_b);
        welding_info_b.m_normal += normal;
        welding_info_b.m_normal_count++;
        vertex_id_to_normal_welding_info[vert_idx_b] = welding_info_b;
    }

    for (int i = 0; i < edge_count; ++i)
    {
        var edge = edges[i];
        var vert_idx_a = edge.m_vertex_idx_a;
        var vert_idx_b = edge.m_vertex_idx_b;

        var vert_a = vertices[vert_idx_a];
        var vert_b = vertices[vert_idx_b];

        var vert_c = vert_a;
        vert_c.m_position.y = m_bot_y;

        var vert_d = vert_b;
        vert_d.m_position.y = m_bot_y;

        var left_welding_info = vertex_id_to_normal_welding_info[vert_idx_a];
        var right_welding_info = vertex_id_to_normal_welding_info[vert_idx_b];

        var left_normal = (left_welding_info.m_normal / (float)left_welding_info.m_normal_count).normalized;
        var right_normal = (right_welding_info.m_normal / (float)right_welding_info.m_normal_count).normalized;

        vert_a.m_normal = left_normal;
        vert_b.m_normal = right_normal;
        vert_c.m_normal = left_normal;
        vert_d.m_normal = right_normal;

        vertices[vert_idx_a] = vert_a;
        vertices[vert_idx_b] = vert_b;

        var vert_idx_c = vert_idx;
        vertices[vert_idx++] = vert_c;

        var vert_idx_d = vert_idx;
        vertices[vert_idx++] = vert_d;

        triangles[triangle_idx++] = vert_idx_a;
        triangles[triangle_idx++] = vert_idx_b;
        triangles[triangle_idx++] = vert_idx_c;
        triangles[triangle_idx++] = vert_idx_c;
        triangles[triangle_idx++] = vert_idx_b;
        triangles[triangle_idx++] = vert_idx_d;
    }
    */
}

    public void Render(float dt, Material material)
    {
        if (!m_is_empty)
        {
            Graphics.DrawMesh(m_mesh, Matrix4x4.identity, material, 0);
        }
    }

    bool[] m_layer_above_occlusion_grid;
    bool[] m_layer_below_occlusion_grid;
    float[] m_layer_density_grid;
    bool[] m_layer_occlusion_grid;
    int m_layer_width_in_voxels;
    int m_layer_height_in_voxels;
    Mesh m_mesh;
    MeshCollider m_collider;
    float m_iso_level;
    float m_bot_y;
    float m_top_y;
    float m_voxel_size_in_meters;
    int m_density_grid_x;
    int m_density_grid_y;
    int m_chunk_dimension_in_voxels;
    bool m_is_empty;

    VertexAttributeDescriptor[] m_vertex_attribute_descriptors = new VertexAttributeDescriptor[]
    {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
    };
}