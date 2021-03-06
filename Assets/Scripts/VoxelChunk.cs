﻿using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;

// Need to notify neighbors of occlusion mishaps.
// Need to mesh bottom.
public class VoxelChunk
{
    const int SAMPLE_TYPE_FULL_SQUARE = 15;
    const int SAMPLE_TYPE_EMTPY = 0;

    public class VertexTable
    {
        public VertexEntry[] m_vertex_entries = new VertexEntry[ushort.MaxValue];
        public Dictionary<uint, ushort> m_vertex_id_to_vertex_idx = new Dictionary<uint, ushort>();

        public void Clear()
        {
            m_vertex_id_to_vertex_idx.Clear();
        }

        public ushort CreateVertexEntry(uint vertex_id, Vector3 position)
        {
            if (!m_vertex_id_to_vertex_idx.TryGetValue(vertex_id, out var vertex_idx))
            {
                vertex_idx = (ushort)m_vertex_id_to_vertex_idx.Count;
                m_vertex_id_to_vertex_idx[vertex_id] = vertex_idx;

                m_vertex_entries[vertex_idx] = new VertexEntry
                {
                    m_vertex_idx = vertex_idx,
                    m_position = position
                };
            }
            else
            {
                int bp = 0;
                ++bp;
            }

            return vertex_idx;
        }

        public VertexPair CreateTopAndBottomVertexPair(VertexLocation location, Vector3 top_pos, Vector3 bot_pos, int chunk_relative_cell_idx)
        {
            var shifted_cell_number = ((uint)chunk_relative_cell_idx) << 16;
            
            var top_shifted_location = (uint)(location | VertexLocation.Top);
            var top_vertex_id = shifted_cell_number | top_shifted_location;

            var bot_shifted_location = (uint)(location | VertexLocation.Bot);
            var bot_vertex_id = shifted_cell_number | bot_shifted_location;

            var top_vertex_idx = CreateVertexEntry(top_vertex_id, top_pos);
            var bot_vertex_idx = CreateVertexEntry(bot_vertex_id, bot_pos);

            var vertex_pair = new VertexPair
            {
                m_top_vertex_idx = top_vertex_idx,
                m_bot_vertex_idx = bot_vertex_idx
            };

            return vertex_pair;
        }
    }

    public struct ScratchBuffer
    {
        public static ScratchBuffer CreateScratchBuffer()
        {
            var scratch_buffer = new ScratchBuffer();

            scratch_buffer.m_positions = new Vector3[ushort.MaxValue];
            scratch_buffer.m_vertices = new Vertex[ushort.MaxValue];
            scratch_buffer.m_triangles = new System.UInt16[ushort.MaxValue * 24];
            scratch_buffer.m_edges = new Edge[ushort.MaxValue];
            scratch_buffer.m_accumulated_normals = new Vector3[ushort.MaxValue];
            scratch_buffer.m_vertex_table = new VertexTable();
            scratch_buffer.m_density_samples = new DensitySample[ushort.MaxValue];
            scratch_buffer.m_vertex_id_to_incoming_edge_idx = new Dictionary<ushort, int>();
            scratch_buffer.m_vertex_id_to_outgoing_edge_idx = new Dictionary<ushort, int>();
            scratch_buffer.m_edge_connections = new EdgeConnections[ushort.MaxValue];
            scratch_buffer.m_edge_face_infos = new EdgeFaceInfo[ushort.MaxValue];
            scratch_buffer.m_border_triangles = new List<ushort>();
            scratch_buffer.m_processed_chunks = new HashSet<Vector3Int>();

            var occlusion_region_variation_count = (int)DirtyOcclusionRegion.TotalVariations;
            var occlusion_region_chunk_offset_table = new Vector3Int[occlusion_region_variation_count][];
            var occlusion_regions = new List<Vector3Int>();

            for(int i = 0; i < occlusion_region_variation_count; ++i)
            {
                var occlusion_region_flags = (DirtyOcclusionRegion)i;

                if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Center))
                {
                    occlusion_regions.Add(new Vector3Int(0, -1, 0));
                }
                if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Left))
                {
                    occlusion_regions.Add(new Vector3Int(-1, -1, 0));

                    if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Near))
                    {
                        occlusion_regions.Add(new Vector3Int(-1, -1, -1));
                    }
                    if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Far))
                    {
                        occlusion_regions.Add(new Vector3Int(-1, -1, 1));
                    }
                }
                if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Right))
                {
                    occlusion_regions.Add(new Vector3Int(1, -1, 0));

                    if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Near))
                    {
                        occlusion_regions.Add(new Vector3Int(1, -1, -1));
                    }
                    if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Far))
                    {
                        occlusion_regions.Add(new Vector3Int(1, -1, 1));
                    }
                }
                if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Near))
                {
                    occlusion_regions.Add(new Vector3Int(0, -1, -1));
                }
                if (occlusion_region_flags.HasFlag(DirtyOcclusionRegion.Far))
                {
                    occlusion_regions.Add(new Vector3Int(0, -1, 1));
                }

                int region_count = occlusion_regions.Count;
                for(int j = 0; j <region_count; ++j)
                {
                    var region = occlusion_regions[j];
                    occlusion_regions.Add(new Vector3Int(region.x, 1, region.z));
                }

                occlusion_region_chunk_offset_table[i] = occlusion_regions.ToArray();
                occlusion_regions.Clear();
            }

            scratch_buffer.m_occlusion_region_chunk_offset_table = occlusion_region_chunk_offset_table;

            return scratch_buffer;
        }

        public void Clear()
        {
            m_vertex_table.Clear();
            m_vertex_id_to_incoming_edge_idx.Clear();
            m_vertex_id_to_outgoing_edge_idx.Clear();
            m_border_triangles.Clear();
        }

        public Vector3[] m_positions;
        public Vertex[] m_vertices;
        public System.UInt16[] m_triangles;
        public Edge[] m_edges;
        public VertexTable m_vertex_table;
        public Vector3[] m_accumulated_normals;
        public Dictionary<ushort, int> m_vertex_id_to_incoming_edge_idx;
        public Dictionary<ushort, int> m_vertex_id_to_outgoing_edge_idx;
        public HashSet<Vector3Int> m_processed_chunks;
        public EdgeConnections[] m_edge_connections;
        public EdgeFaceInfo[] m_edge_face_infos;
        public DensitySample[] m_density_samples;
        public List<ushort> m_border_triangles;
        public Vector3Int[][] m_occlusion_region_chunk_offset_table;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 m_position;
        public Vector3 m_normal;
    }

    public struct Edge
    {
        public VertexPair m_top_bot_vertex_pair_a;
        public VertexPair m_top_bot_vertex_pair_b;
        public bool m_is_border;
    }

    public struct EdgeFaceInfo
    {
        public ushort m_vertex_idx_a;
        public ushort m_vertex_idx_b;
        public ushort m_vertex_idx_c;
        public ushort m_vertex_idx_d;
        public Vector3 m_pos_a;
        public Vector3 m_pos_b;
        public Vector3 m_normal;
        public bool m_is_border;
    }

    public struct NormalWeldingInfo
    {
        public Vector3 m_normal;
        public int m_normal_count;
    }

    public struct VertexEntry
    {
        public ushort m_vertex_idx;
        public Vector3 m_position;
    }

    public struct DensitySample
    {
        public int m_sample_type;
        public float m_left_near_density;
        public float m_right_near_density;
        public float m_left_far_density;
        public float m_right_far_density;
        public int m_x;
        public int m_y;
        public int m_cell_idx;
        public bool m_is_border_sample;
    }

    [System.Flags]
    public enum DirtyOcclusionRegion : byte
    {
        None = 0,
        Center = 1, 
        Left = 2,
        Right = 4,
        Near = 8,
        Far = 16,

        TotalVariations = 32
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
        LeftNear = Left | Near,
        RightNear = Right | Near,
        LeftFar = Left | Far,
        RightFar = Right | Far,
    }

    public struct EdgeConnections
    {
        public ushort m_vertex_idx_a;
        public ushort m_vertex_idx_b;
        public ushort m_vertex_idx_c;
        public ushort m_vertex_idx_d;
        public ushort m_vertex_idx_e;
        public ushort m_vertex_idx_f;
        public ushort m_vertex_idx_g;
        public bool m_is_border_edge;
    }

    public struct VertexPair
    {
        public ushort m_top_vertex_idx;
        public ushort m_bot_vertex_idx;
    }

    public VoxelChunk(
        string name,
        int density_grid_x, 
        int density_grid_y, 
        int dimensions_in_voxels, 
        int layer_width_in_voxels, 
        int layer_height_in_voxels, 
        float[] layer_density_grid, 
        byte[] layer_sample_grid, 
        Vector3 voxel_size_in_meters, 
        float iso_level, 
        float bot_y, 
        float top_y, 
        bool generate_collision,
        float density_height_weight,        
        Bounds bounds,
        bool is_liquid,
        BevelTuning bevel_tuning
        )
    {
        m_density_height_weight = density_height_weight;
        m_density_grid_x = density_grid_x;
        m_density_grid_y = density_grid_y;
        m_chunk_dimension_in_voxels = dimensions_in_voxels;
        m_layer_density_grid = layer_density_grid;
        m_layer_sample_grid = layer_sample_grid;
        m_layer_width_in_voxels = layer_width_in_voxels;
        m_layer_height_in_voxels = layer_height_in_voxels;
        m_iso_level = iso_level;
        m_voxel_size_in_meters = voxel_size_in_meters;
        m_generate_collision = generate_collision;
        m_is_liquid = is_liquid;
        m_bevel_tuning = bevel_tuning;

        m_mesh = new Mesh();
        m_mesh.MarkDynamic();
        m_mesh.name = "VoxelChunk";
        m_mesh.bounds = bounds;

        if (m_generate_collision)
        {
            var collider_go = new GameObject($"{name}VoxelCollider");
            collider_go.gameObject.SetActive(false);
            m_collider = collider_go.AddComponent<MeshCollider>();
            m_collider.sharedMesh = m_mesh;
        }

        m_bot_y = bot_y;
        m_top_y = top_y;
    }

    public void SetAboveAndBelowSampleGrids(byte[] layer_above_sample_grid, byte[] layer_below_sample_grid)
    {
        m_layer_above_sample_grid = layer_above_sample_grid;
        m_layer_below_sample_grid = layer_below_sample_grid;
    }

    public void March(VoxelChunk.ScratchBuffer scratch_buffer, VertexAttributeDescriptor[] vertex_attribute_descriptors)
    {
        scratch_buffer.Clear();

        var triangle_count = 0;
        if (!m_has_any_samples)
        {
            m_mesh.Clear();
        }
        else
        {
            Profiler.BeginSample("GatherDensitySamples");
            var density_samples = scratch_buffer.m_density_samples;
            GatherDensitySamples(scratch_buffer.m_density_samples, out var density_sample_count);
            Profiler.EndSample();

            Profiler.BeginSample("March");
            Triangulate(scratch_buffer, density_samples, density_sample_count, out var vert_count, out triangle_count);
            Profiler.EndSample();


            Profiler.BeginSample("UpdateMeshData");
            m_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            m_mesh.SetVertexBufferParams(vert_count, vertex_attribute_descriptors);

            m_mesh.SetVertexBufferData(scratch_buffer.m_vertices, 0, 0, vert_count, 0, m_mesh_update_flags);
            m_mesh.SetTriangles(scratch_buffer.m_triangles, 0, triangle_count, 0, false);
            Profiler.EndSample();
        }

        m_is_empty = triangle_count == 0;

        if (m_generate_collision)
        {
            Profiler.BeginSample("UpdateCollision");
            m_collider.gameObject.SetActive(false);
            if (!m_is_empty)
            {
                m_collider.gameObject.SetActive(true);
            }
            Profiler.EndSample();
        }
    }

    struct MeshMarcher
    {
        public float m_left_x;
        public float m_right_x;
        public float m_near_z;
        public float m_far_z;
        public float m_left_near_top_y;
        public float m_left_far_top_y;
        public float m_right_near_top_y;
        public float m_right_far_top_y;
        public float m_bot_y;
        public float m_left_near_density;
        public float m_right_near_density;
        public float m_left_far_density;
        public float m_right_far_density;
        public float m_iso_level;
        public ushort m_triangle_idx;
        public System.UInt16[] m_triangles;
        public int m_edge_idx;        
        public Edge[] m_edges;
        public int m_cell_idx;
        public int m_layer_width_in_voxels;
        public VertexTable m_vertex_table;
        public bool m_is_liquid;
        public List<ushort> m_border_triangles;

        public VertexPair LeftNear()
        {
            var top_pos = new Vector3(m_left_x, m_left_near_top_y, m_near_z);
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.LeftNear, top_pos, bot_pos, m_cell_idx);
        }

        public VertexPair RightNear()
        {
            var top_pos = new Vector3(m_right_x, m_right_near_top_y, m_near_z);
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.LeftNear, top_pos, bot_pos, m_cell_idx + 1);
        }

        public VertexPair LeftFar()
        {
            var top_pos = new Vector3(m_left_x, m_left_far_top_y, m_far_z);
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.LeftNear, top_pos, bot_pos, m_cell_idx + m_layer_width_in_voxels);
        }

        public VertexPair RightFar()
        {
            var top_pos = new Vector3(m_right_x, m_right_far_top_y, m_far_z);
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);

            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.LeftNear, top_pos, bot_pos, m_cell_idx + m_layer_width_in_voxels + 1);
        }

        public VertexPair LeftEdge()
        {
            var top_pos = new Vector3(m_left_x, InterpolatePosition(m_left_near_top_y, m_left_far_top_y, m_left_near_density, m_left_far_density), InterpolatePosition(m_near_z, m_far_z, m_left_near_density, m_left_far_density));
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.Left, top_pos, bot_pos, m_cell_idx);
        }

        public VertexPair RightEdge()
        {
            var top_pos = new Vector3(m_right_x, InterpolatePosition(m_right_near_top_y, m_right_far_top_y, m_right_near_density, m_right_far_density), InterpolatePosition(m_near_z, m_far_z, m_right_near_density, m_right_far_density));
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.Left, top_pos, bot_pos, m_cell_idx + 1);
        }

        public VertexPair NearEdge()
        {
            var top_pos = new Vector3(InterpolatePosition(m_left_x, m_right_x, m_left_near_density, m_right_near_density), InterpolatePosition(m_left_near_top_y, m_right_near_top_y, m_left_near_density, m_right_near_density), m_near_z);
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.Near, top_pos, bot_pos, m_cell_idx);
        }

        public VertexPair FarEdge()
        {
            var top_pos = new Vector3(InterpolatePosition(m_left_x, m_right_x, m_left_far_density, m_right_far_density), InterpolatePosition(m_left_far_top_y, m_right_far_top_y, m_left_far_density, m_right_far_density), m_far_z);
            var bot_pos = new Vector3(top_pos.x, m_bot_y, top_pos.z);
            return m_vertex_table.CreateTopAndBottomVertexPair(VertexLocation.Near, top_pos, bot_pos, m_cell_idx + m_layer_width_in_voxels);
        }

        public float AverageDensity()
        {
            return (m_left_near_density + m_left_far_density + m_right_near_density + m_right_far_density) / 4f;
        }

        float InterpolatePosition(float pos_a, float pos_b, float density_a, float density_b)
        {
            return pos_a + (m_iso_level - density_a) * (pos_b - pos_a) / (density_b - density_a);
        }

        public void Triangle(VertexPair top_bot_vertex_pair_a, VertexPair top_bot_vertex_pair_b, VertexPair top_bot_vertex_pair_c, bool is_border_sample)
        {
            if (is_border_sample)
            {
                m_border_triangles.Add(top_bot_vertex_pair_a.m_top_vertex_idx);
                m_border_triangles.Add(top_bot_vertex_pair_b.m_top_vertex_idx);
                m_border_triangles.Add(top_bot_vertex_pair_c.m_top_vertex_idx);
                if (!m_is_liquid)
                {
                    m_border_triangles.Add(top_bot_vertex_pair_b.m_bot_vertex_idx);
                    m_border_triangles.Add(top_bot_vertex_pair_a.m_bot_vertex_idx);
                    m_border_triangles.Add(top_bot_vertex_pair_c.m_bot_vertex_idx);
                }
            }
            else
            {
                m_triangles[m_triangle_idx++] = top_bot_vertex_pair_a.m_top_vertex_idx;
                m_triangles[m_triangle_idx++] = top_bot_vertex_pair_b.m_top_vertex_idx;
                m_triangles[m_triangle_idx++] = top_bot_vertex_pair_c.m_top_vertex_idx;
                if (!m_is_liquid)
                {
                    m_triangles[m_triangle_idx++] = top_bot_vertex_pair_b.m_bot_vertex_idx;
                    m_triangles[m_triangle_idx++] = top_bot_vertex_pair_a.m_bot_vertex_idx;
                    m_triangles[m_triangle_idx++] = top_bot_vertex_pair_c.m_bot_vertex_idx;
                }
            }
        }

        public void ExtrudeTopToBot(VertexPair top_bot_vertex_pair_a, VertexPair top_bot_vertex_pair_b, bool is_border_sample)
        {
            m_edges[m_edge_idx++] = new Edge
            {
                m_top_bot_vertex_pair_a = top_bot_vertex_pair_a,
                m_top_bot_vertex_pair_b = top_bot_vertex_pair_b,
                m_is_border = is_border_sample
            };
        }
    }

    public DirtyOcclusionRegion UpdateDensitySamples()
    {
        Profiler.BeginSample("UpdateDensitySamples");
        var dirty_occlusion_regions = DirtyOcclusionRegion.None;

        var max_y = System.Math.Min(m_density_grid_y + m_chunk_dimension_in_voxels + 1, m_layer_height_in_voxels - 1);
        var start_y = System.Math.Max(m_density_grid_y - 1, 0);
        var max_x = System.Math.Min(m_density_grid_x + m_chunk_dimension_in_voxels + 1, m_layer_width_in_voxels - 1);
        var start_x = System.Math.Max(m_density_grid_x - 1, 0);

        bool has_any_samples = false;
        for (int y = start_y; y < max_y; ++y)
        {
            var vertical_occlusion_region = DirtyOcclusionRegion.Center;
            if (y == start_y)
            {
                vertical_occlusion_region = DirtyOcclusionRegion.Near;
            }
            else if (y == max_y - 1)
            {
                vertical_occlusion_region = DirtyOcclusionRegion.Far;
            }

            for (int x = start_x; x < max_x; ++x)
            {
                int left_near_cell_idx = y * m_layer_width_in_voxels + x;

                var left_near_density = m_layer_density_grid[left_near_cell_idx];
                var right_near_density = m_layer_density_grid[left_near_cell_idx + 1];
                var left_far_density = m_layer_density_grid[left_near_cell_idx + m_layer_width_in_voxels];
                var right_far_density = m_layer_density_grid[left_near_cell_idx + m_layer_width_in_voxels + 1];

                int sample_type = 0;
                if (left_near_density >= m_iso_level) sample_type |= 1;
                if (right_near_density >= m_iso_level) sample_type |= 2;
                if (right_far_density >= m_iso_level) sample_type |= 4;
                if (left_far_density >= m_iso_level) sample_type |= 8;

                bool is_occluding = sample_type == SAMPLE_TYPE_FULL_SQUARE;
                var was_occluding = m_layer_sample_grid[left_near_cell_idx] == SAMPLE_TYPE_FULL_SQUARE;
                if (was_occluding != is_occluding)
                {
                    var horizontal_occlusion_region = DirtyOcclusionRegion.Center;
                    if (x == start_x)
                    {
                        horizontal_occlusion_region = DirtyOcclusionRegion.Left;
                    }
                    else if (x == max_x - 1)
                    {
                        horizontal_occlusion_region = DirtyOcclusionRegion.Right;
                    }

                    dirty_occlusion_regions = dirty_occlusion_regions | horizontal_occlusion_region | vertical_occlusion_region;
                }

                m_layer_sample_grid[left_near_cell_idx] = (byte)sample_type;

                if(sample_type != SAMPLE_TYPE_EMTPY)
                {
                    has_any_samples = true;
                }
            }
        }

        m_has_any_samples = has_any_samples;

        Profiler.EndSample();

        return dirty_occlusion_regions;
    }

    void GatherDensitySamples(DensitySample[] density_samples, out int sample_count)
    {
        sample_count = 0;

        var max_y = System.Math.Min(m_density_grid_y + m_chunk_dimension_in_voxels + 1, m_layer_height_in_voxels - 1);
        var start_y = System.Math.Max(m_density_grid_y - 1, 0);
        for (int y = start_y; y < max_y; ++y)
        {
            var max_x = System.Math.Min(m_density_grid_x + m_chunk_dimension_in_voxels + 1, m_layer_width_in_voxels - 1);
            var start_x = System.Math.Max(m_density_grid_x - 1, 0);
            for (int x = start_x; x < max_x; ++x)
            {
                int left_near_cell_idx = y * m_layer_width_in_voxels + x;

                var sample_type = m_layer_sample_grid[left_near_cell_idx];
                if (sample_type == SAMPLE_TYPE_EMTPY) continue;

                var left_near_density = m_layer_density_grid[left_near_cell_idx];
                var right_near_density = m_layer_density_grid[left_near_cell_idx + 1];
                var left_far_density = m_layer_density_grid[left_near_cell_idx + m_layer_width_in_voxels];
                var right_far_density = m_layer_density_grid[left_near_cell_idx + m_layer_width_in_voxels + 1];

                bool is_occluded = m_layer_above_sample_grid[left_near_cell_idx] == SAMPLE_TYPE_FULL_SQUARE && sample_type == SAMPLE_TYPE_FULL_SQUARE && m_layer_below_sample_grid[left_near_cell_idx] == SAMPLE_TYPE_FULL_SQUARE;
                if (is_occluded) continue;

                bool is_border_sample = x == start_x || x == max_x - 1 || y == start_y || y == max_y - 1;
                density_samples[sample_count++] = new DensitySample
                {
                    m_x = x,
                    m_y = y,
                    m_cell_idx = left_near_cell_idx,
                    m_left_near_density = left_near_density,
                    m_left_far_density = left_far_density,
                    m_right_near_density = right_near_density,
                    m_right_far_density = right_far_density,
                    m_sample_type = sample_type,
                    m_is_border_sample = is_border_sample
                };
            }
        }
    }


    void Triangulate(ScratchBuffer scratch_buffer, DensitySample[] density_samples, int density_sample_count, out ushort vert_count, out int triangle_count)
    {
        var mesh = m_mesh;
        mesh.Clear();

        triangle_count = 0;
        int edge_idx = 0;

        var extrusion_bottom_vertical_offset = m_bevel_tuning.m_extrusion_bottom_vertical_offset;
        if(m_is_liquid)
        {
            extrusion_bottom_vertical_offset = -0.5f;
        }

        Profiler.BeginSample("GenerateTriangles");
        for (int density_sample_idx = 0; density_sample_idx < density_sample_count; ++density_sample_idx)
        {
            var density_sample = density_samples[density_sample_idx];
            var y = density_sample.m_y;
            var x = density_sample.m_x;
            var left_near_density = density_sample.m_left_near_density;
            var left_far_density = density_sample.m_left_far_density;
            var right_near_density = density_sample.m_right_near_density;
            var right_far_density = density_sample.m_right_far_density;
            var sample_type = density_sample.m_sample_type;
            bool is_border_sample = density_sample.m_is_border_sample;

            var near_z = (float)y * m_voxel_size_in_meters.z;
            var far_z = near_z + m_voxel_size_in_meters.z;


            int chunk_relative_x = x - m_density_grid_x;
            int chunk_relative_y = y - m_density_grid_y;
            int chunk_relative_cell_idx = chunk_relative_y * (m_chunk_dimension_in_voxels + 2) + chunk_relative_x;

            var left_x = (float)x * m_voxel_size_in_meters.x;
            var right_x = left_x + m_voxel_size_in_meters.x;

            m_density_height_weight = 0.8f;
            var left_near_top_y_delta = m_density_height_weight * -m_voxel_size_in_meters.y * (1 - left_near_density);
            var left_far_top_y_delta = m_density_height_weight * -m_voxel_size_in_meters.y * (1 - left_far_density);
            var right_near_top_y_delta = m_density_height_weight * -m_voxel_size_in_meters.y * (1 - right_near_density);
            var right_far_top_y_delta = m_density_height_weight * -m_voxel_size_in_meters.y * (1 - right_far_density);


            var marcher = new MeshMarcher
            {
                m_left_x = left_x,
                m_right_x = right_x,
                m_near_z = near_z,
                m_far_z = far_z,
                m_bot_y = m_bot_y + extrusion_bottom_vertical_offset,
                m_left_near_top_y = m_top_y + left_near_top_y_delta,
                m_left_far_top_y = m_top_y + left_far_top_y_delta,
                m_right_near_top_y = m_top_y + right_near_top_y_delta,
                m_right_far_top_y = m_top_y + right_far_top_y_delta,
                m_left_near_density = left_near_density,
                m_right_near_density = right_near_density,
                m_left_far_density = left_far_density,
                m_right_far_density = right_far_density,
                m_iso_level = m_iso_level,
                m_triangle_idx = (ushort)triangle_count,
                m_cell_idx = density_sample.m_cell_idx,
                m_layer_width_in_voxels = m_layer_width_in_voxels,
                m_triangles = scratch_buffer.m_triangles,
                m_edge_idx = edge_idx,
                m_edges = scratch_buffer.m_edges,
                m_vertex_table = scratch_buffer.m_vertex_table,
                m_is_liquid = m_is_liquid,
                m_border_triangles = scratch_buffer.m_border_triangles
            };


            if (sample_type == 1)
            {
                var left_near = marcher.LeftNear();
                var left_edge = marcher.LeftEdge();
                var near_edge = marcher.NearEdge();

                marcher.Triangle(left_near, left_edge, near_edge, is_border_sample);
                marcher.ExtrudeTopToBot(near_edge, left_edge, is_border_sample);
            }
            else if (sample_type == 2)
            {
                var near_edge = marcher.NearEdge();
                var right_edge = marcher.RightEdge();
                var right_near = marcher.RightNear();

                marcher.Triangle(near_edge, right_edge, right_near, is_border_sample);
                marcher.ExtrudeTopToBot(right_edge, near_edge, is_border_sample);
            }
            else if (sample_type == 3)
            {
                var left_edge = marcher.LeftEdge();
                var right_near = marcher.RightNear();
                var left_near = marcher.LeftNear();
                var right_edge = marcher.RightEdge();

                marcher.Triangle(left_edge, right_near, left_near, is_border_sample);
                marcher.Triangle(left_edge, right_edge, right_near, is_border_sample);
                marcher.ExtrudeTopToBot(right_edge, left_edge, is_border_sample);
            }
            else if (sample_type == 4)
            {
                var far_edge = marcher.FarEdge();
                var right_far = marcher.RightFar();
                var right_edge = marcher.RightEdge();

                marcher.Triangle(far_edge, right_far, right_edge, is_border_sample);
                marcher.ExtrudeTopToBot(far_edge, right_edge, is_border_sample);
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

                    marcher.Triangle(left_near, left_edge, near_edge, is_border_sample);
                    marcher.Triangle(left_edge, right_edge, near_edge, is_border_sample);
                    marcher.Triangle(left_edge, far_edge, right_edge, is_border_sample);
                    marcher.Triangle(far_edge, right_far, right_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(far_edge, left_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(near_edge, right_edge, is_border_sample);
                }
                else
                {
                    var left_near = marcher.LeftNear();
                    var left_edge = marcher.LeftEdge();
                    var near_edge = marcher.NearEdge();
                    var far_edge = marcher.FarEdge();
                    var right_far = marcher.RightFar();
                    var right_edge = marcher.RightEdge();

                    marcher.Triangle(left_near, left_edge, near_edge, is_border_sample);
                    marcher.Triangle(far_edge, right_far, right_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(near_edge, left_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(far_edge, right_edge, is_border_sample);
                }
            }
            else if (sample_type == 6)
            {
                var far_edge = marcher.FarEdge();
                var right_far = marcher.RightFar();
                var right_near = marcher.RightNear();
                var near_edge = marcher.NearEdge();

                marcher.Triangle(far_edge, right_far, right_near, is_border_sample);
                marcher.Triangle(far_edge, right_near, near_edge, is_border_sample);
                marcher.ExtrudeTopToBot(far_edge, near_edge, is_border_sample);
            }
            else if (sample_type == 7)
            {
                var left_edge = marcher.LeftEdge();
                var right_near = marcher.RightNear();
                var left_near = marcher.LeftNear();
                var far_edge = marcher.FarEdge();
                var right_far = marcher.RightFar();

                marcher.Triangle(left_edge, right_near, left_near, is_border_sample);
                marcher.Triangle(left_edge, far_edge, right_near, is_border_sample);
                marcher.Triangle(far_edge, right_far, right_near, is_border_sample);
                marcher.ExtrudeTopToBot(far_edge, left_edge, is_border_sample);
            }
            else if (sample_type == 8)
            {
                var left_far = marcher.LeftFar();
                var far_edge = marcher.FarEdge();
                var left_edge = marcher.LeftEdge();

                marcher.Triangle(left_far, far_edge, left_edge, is_border_sample);
                marcher.ExtrudeTopToBot(left_edge, far_edge, is_border_sample);
            }
            else if (sample_type == 9)
            {
                var left_far = marcher.LeftFar();
                var far_edge = marcher.FarEdge();
                var near_edge = marcher.NearEdge();
                var left_near = marcher.LeftNear();

                marcher.Triangle(left_far, far_edge, near_edge, is_border_sample);
                marcher.Triangle(left_far, near_edge, left_near, is_border_sample);
                marcher.ExtrudeTopToBot(near_edge, far_edge, is_border_sample);
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

                    marcher.Triangle(left_far, far_edge, left_edge, is_border_sample);
                    marcher.Triangle(left_edge, far_edge, right_edge, is_border_sample);
                    marcher.Triangle(left_edge, right_edge, near_edge, is_border_sample);
                    marcher.Triangle(near_edge, right_edge, right_near, is_border_sample);
                    marcher.ExtrudeTopToBot(left_edge, near_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(right_edge, far_edge, is_border_sample);
                }
                else
                {
                    var near_edge = marcher.NearEdge();
                    var right_edge = marcher.RightEdge();
                    var right_near = marcher.RightNear();
                    var left_edge = marcher.LeftEdge();
                    var far_edge = marcher.FarEdge();
                    var left_far = marcher.LeftFar();

                    marcher.Triangle(near_edge, right_edge, right_near, is_border_sample);
                    marcher.Triangle(left_far, far_edge, left_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(left_edge, far_edge, is_border_sample);
                    marcher.ExtrudeTopToBot(right_edge, near_edge, is_border_sample);

                }
            }
            else if (sample_type == 11)
            {
                var left_far = marcher.LeftFar();
                var far_edge = marcher.FarEdge();
                var left_near = marcher.LeftNear();
                var right_edge = marcher.RightEdge();
                var right_near = marcher.RightNear();

                marcher.Triangle(left_far, far_edge, left_near, is_border_sample);
                marcher.Triangle(left_near, far_edge, right_edge, is_border_sample);
                marcher.Triangle(left_near, right_edge, right_near, is_border_sample);
                marcher.ExtrudeTopToBot(right_edge, far_edge, is_border_sample);
            }
            else if (sample_type == 12)
            {
                var left_far = marcher.LeftFar();
                var right_far = marcher.RightFar();
                var left_edge = marcher.LeftEdge();
                var right_edge = marcher.RightEdge();

                marcher.Triangle(left_far, right_far, left_edge, is_border_sample);
                marcher.Triangle(left_edge, right_far, right_edge, is_border_sample);
                marcher.ExtrudeTopToBot(left_edge, right_edge, is_border_sample);
            }
            else if (sample_type == 13)
            {
                var left_near = marcher.LeftNear();
                var left_far = marcher.LeftFar();
                var near_edge = marcher.NearEdge();
                var right_edge = marcher.RightEdge();
                var right_far = marcher.RightFar();

                marcher.Triangle(left_near, left_far, near_edge, is_border_sample);
                marcher.Triangle(left_far, right_edge, near_edge, is_border_sample);
                marcher.Triangle(left_far, right_far, right_edge, is_border_sample);
                marcher.ExtrudeTopToBot(near_edge, right_edge, is_border_sample);
            }
            else if (sample_type == 14)
            {
                var left_far = marcher.LeftFar();
                var right_far = marcher.RightFar();
                var left_edge = marcher.LeftEdge();
                var near_edge = marcher.NearEdge();
                var right_near = marcher.RightNear();

                marcher.Triangle(left_far, right_far, left_edge, is_border_sample);
                marcher.Triangle(left_edge, right_far, near_edge, is_border_sample);
                marcher.Triangle(near_edge, right_far, right_near, is_border_sample);
                marcher.ExtrudeTopToBot(left_edge, near_edge, is_border_sample);

            }
            else if (sample_type == SAMPLE_TYPE_FULL_SQUARE)
            {
                var left_near = marcher.LeftNear();
                var left_far = marcher.LeftFar();
                var right_near = marcher.RightNear();
                var right_far = marcher.RightFar();

                marcher.Triangle(left_near, left_far, right_near, is_border_sample);
                marcher.Triangle(left_far, right_far, right_near, is_border_sample);
            }

            edge_idx = marcher.m_edge_idx;
            triangle_count = marcher.m_triangle_idx;
        }
        Profiler.EndSample();


        var vertex_entry_count = scratch_buffer.m_vertex_table.m_vertex_id_to_vertex_idx.Count;
        var vertex_entries = scratch_buffer.m_vertex_table.m_vertex_entries;
        var positions = scratch_buffer.m_positions;
        for (int i = 0; i < vertex_entry_count; ++i)
        {
            var entry = vertex_entries[i];

            positions[entry.m_vertex_idx] = entry.m_position;
        }

        vert_count = (ushort)vertex_entry_count;

        Profiler.BeginSample("FinalizeEdges");
        FinalizeEdges(
            scratch_buffer.m_positions, 
            scratch_buffer.m_vertices, 
            scratch_buffer.m_triangles, 
            scratch_buffer.m_edges,
            scratch_buffer.m_accumulated_normals,
            scratch_buffer.m_vertex_id_to_incoming_edge_idx,
            scratch_buffer.m_vertex_id_to_outgoing_edge_idx,
            scratch_buffer.m_edge_connections,
            scratch_buffer.m_edge_face_infos,
            scratch_buffer.m_border_triangles,
            ref vert_count, 
            ref triangle_count, 
            ref edge_idx
            );
        Profiler.EndSample();
    }

    void FinalizeEdges(
        Vector3[] positions,
        Vertex[] vertices, 
        ushort[] triangles, 
        Edge[] edges,
        Vector3[] accumulated_normals,
        Dictionary<ushort, int> vertex_id_to_incoming_edge_idx,
        Dictionary<ushort, int> vertex_id_to_outoing_edge_idx,
        EdgeConnections[] edge_connections,
        EdgeFaceInfo[] edge_face_infos,
        List<ushort> border_triangles,
        ref ushort vert_idx, 
        ref int triangle_idx, 
        ref int edge_count
        )
    {
        var pos_writer = new PositionWriter(positions, vert_idx);
        var triangle_writer = new TriangleWriter(triangles, (ushort)triangle_idx, border_triangles);

        var extrusion_distance = m_bevel_tuning.m_extrusion_distance;
        var upper_vertical_offset = new Vector3(0, m_bevel_tuning.m_extrusion_vertical_offset, 0);
        var lower_vertical_offset = new Vector3(0, -m_voxel_size_in_meters.y + m_bevel_tuning.m_extrusion_lower_vertical_offset, 0);

        if(m_is_liquid)
        {
            lower_vertical_offset = new Vector3(0, -m_voxel_size_in_meters.y, 0);
        }

        for(int i = 0; i < edge_count; ++i)
        {
            var edge = edges[i];
            var pos_a = pos_writer.m_positions[edge.m_top_bot_vertex_pair_a.m_top_vertex_idx];
            var pos_b = pos_writer.m_positions[edge.m_top_bot_vertex_pair_b.m_top_vertex_idx];
            var pos_for_normal = pos_a - Vector3.up;
            var normal = Vector3.Cross(pos_b - pos_a, pos_for_normal - pos_a).normalized;

            var edge_idx = i;

#if UNITY_EDITOR
            if (vertex_id_to_outoing_edge_idx.ContainsKey(edge.m_top_bot_vertex_pair_a.m_top_vertex_idx))
            {
                throw new System.Exception($"Error edge already exists {edge.m_top_bot_vertex_pair_a}");
            }

            if (vertex_id_to_incoming_edge_idx.ContainsKey(edge.m_top_bot_vertex_pair_b.m_top_vertex_idx))
            {
                throw new System.Exception($"Error edge already exists {edge.m_top_bot_vertex_pair_b.m_top_vertex_idx}");
            }
#endif

            vertex_id_to_outoing_edge_idx[edge.m_top_bot_vertex_pair_a.m_top_vertex_idx] = edge_idx;
            vertex_id_to_incoming_edge_idx[edge.m_top_bot_vertex_pair_b.m_top_vertex_idx] = edge_idx;
            edge_face_infos[i] = new EdgeFaceInfo
            {
                m_vertex_idx_a = edge.m_top_bot_vertex_pair_a.m_top_vertex_idx,
                m_vertex_idx_b = edge.m_top_bot_vertex_pair_b.m_top_vertex_idx,
                m_vertex_idx_c = edge.m_top_bot_vertex_pair_a.m_bot_vertex_idx,
                m_vertex_idx_d = edge.m_top_bot_vertex_pair_b.m_bot_vertex_idx,
                m_pos_a = pos_a,
                m_pos_b = pos_b,
                m_normal = normal,
                m_is_border = edge.m_is_border
            };
        }

        float max_edge_seperation = m_bevel_tuning.m_max_edge_seperation;

        for (int i = 0; i < edge_count; ++i)
        {
            var edge_face_info = edge_face_infos[i];

            bool is_border_edge = edge_face_info.m_is_border;

            var vert_idx_a = edge_face_info.m_vertex_idx_a;
            var vert_idx_b = edge_face_info.m_vertex_idx_b;
            var vert_idx_g = edge_face_info.m_vertex_idx_c;
            var vert_idx_h = edge_face_info.m_vertex_idx_d;

            var pos_a = edge_face_info.m_pos_a;
            var pos_b = edge_face_info.m_pos_b;
            var top_normal = edge_face_info.m_normal;
            var horizontal_offset = top_normal * extrusion_distance;

            var a_to_b_normalized = (pos_b - pos_a);

            float left_offset_multiplier = 1f;
            /*
            if(vertex_id_to_incoming_edge_idx.TryGetValue(vert_idx_a, out var incoming_edge_idx))
            {
                var incoming_edge = edge_face_infos[incoming_edge_idx];
                var edge_face_normals_dp = Vector3.Dot(edge_face_info.m_normal, incoming_edge.m_normal);
                //if(edge_face_normals_dp > 0)
                {
                    left_offset_multiplier = Mathf.Abs(edge_face_normals_dp);
                }
            }
            */

            float right_offset_multiplier = 1f;
            /*
            if (vertex_id_to_outoing_edge_idx.TryGetValue(vert_idx_b, out var outgoing_edge_idx))
            {
                var outgoing_edge = edge_face_infos[outgoing_edge_idx];
                var edge_face_normals_dp = Vector3.Dot(edge_face_info.m_normal, outgoing_edge.m_normal);
                //if (edge_face_normals_dp > 0)
                {
                    right_offset_multiplier = Mathf.Abs(edge_face_normals_dp);
                }
            }

            if (m_bevel_tuning.m_disable_dot_product_check)            
            {
                left_offset_multiplier = 1f;
                right_offset_multiplier = 1f;
            }
            */


            var left_offset = a_to_b_normalized * max_edge_seperation * left_offset_multiplier;
            var right_offset = a_to_b_normalized * -max_edge_seperation * right_offset_multiplier;

            var vert_idx_c = pos_writer.Write(pos_a + horizontal_offset + upper_vertical_offset + left_offset);
            var vert_idx_d = pos_writer.Write(pos_b + horizontal_offset + upper_vertical_offset + right_offset);

            triangle_writer.Write(vert_idx_a, vert_idx_b, vert_idx_c, is_border_edge);
            triangle_writer.Write(vert_idx_c, vert_idx_b, vert_idx_d, is_border_edge);

            var vert_idx_e = pos_writer.Write(pos_a + horizontal_offset + lower_vertical_offset + left_offset);
            var vert_idx_f = pos_writer.Write(pos_b + horizontal_offset + lower_vertical_offset + right_offset);

            triangle_writer.Write(vert_idx_c, vert_idx_d, vert_idx_e, is_border_edge);
            triangle_writer.Write(vert_idx_e, vert_idx_d, vert_idx_f, is_border_edge);


            triangle_writer.Write(vert_idx_e, vert_idx_f, vert_idx_g, is_border_edge);
            triangle_writer.Write(vert_idx_g, vert_idx_f, vert_idx_h, is_border_edge);

            edge_connections[i] = new EdgeConnections
            {
                m_vertex_idx_a = vert_idx_a,
                m_vertex_idx_b = vert_idx_b,
                m_vertex_idx_c = vert_idx_c,
                m_vertex_idx_d = vert_idx_d,
                m_vertex_idx_e = vert_idx_e,
                m_vertex_idx_f = vert_idx_f,
                m_vertex_idx_g = vert_idx_g,
                m_is_border_edge = is_border_edge
            };
        }

        for(int i = 0; i < edge_count; ++i)
        {
            var start_edge = edge_connections[i];
            if (!vertex_id_to_outoing_edge_idx.TryGetValue(start_edge.m_vertex_idx_b, out var end_edge_idx)) continue;

            bool is_border_edge = start_edge.m_is_border_edge;

            var end_edge = edge_connections[end_edge_idx];

            triangle_writer.Write(start_edge.m_vertex_idx_d, start_edge.m_vertex_idx_b, end_edge.m_vertex_idx_c, is_border_edge);

            triangle_writer.Write(start_edge.m_vertex_idx_d, end_edge.m_vertex_idx_c, start_edge.m_vertex_idx_f, is_border_edge);
            triangle_writer.Write(start_edge.m_vertex_idx_f, end_edge.m_vertex_idx_c, end_edge.m_vertex_idx_e, is_border_edge);

            triangle_writer.Write(end_edge.m_vertex_idx_g, start_edge.m_vertex_idx_f, end_edge.m_vertex_idx_e, is_border_edge);
        }

        triangle_idx = triangle_writer.Count;
        vert_idx = (ushort)pos_writer.Count;

        for(int i = 0; i < vert_idx; ++i)
        {
            accumulated_normals[i] = Vector3.zero;
        }

        for (int i = 0; i < triangle_idx; i += 3)
        {
            var vert_idx0 = triangles[i + 0];
            var vert_idx1 = triangles[i + 1];
            var vert_idx2 = triangles[i + 2];

            var v0 = pos_writer.m_positions[vert_idx0];
            var v1 = pos_writer.m_positions[vert_idx1];
            var v2 = pos_writer.m_positions[vert_idx2];
            var normal = Vector3.Cross(v1 - v0, v2 - v0);

            accumulated_normals[vert_idx0] = accumulated_normals[vert_idx0] + normal;
            accumulated_normals[vert_idx1] = accumulated_normals[vert_idx1] + normal;
            accumulated_normals[vert_idx2] = accumulated_normals[vert_idx2] + normal;
        }

        for(int i = 0; i < border_triangles.Count; i += 3)
        {
            var vert_idx0 = border_triangles[i + 0];
            var vert_idx1 = border_triangles[i + 1];
            var vert_idx2 = border_triangles[i + 2];

            var v0 = pos_writer.m_positions[vert_idx0];
            var v1 = pos_writer.m_positions[vert_idx1];
            var v2 = pos_writer.m_positions[vert_idx2];
            var normal = Vector3.Cross(v1 - v0, v2 - v0);

            accumulated_normals[vert_idx0] = accumulated_normals[vert_idx0] + normal;
            accumulated_normals[vert_idx1] = accumulated_normals[vert_idx1] + normal;
            accumulated_normals[vert_idx2] = accumulated_normals[vert_idx2] + normal;
        }

        for (int i = 0; i < vert_idx; ++i)
        {
            vertices[i] = new Vertex
            {
                m_position = positions[i],
                m_normal = accumulated_normals[i].normalized
            };
        }
    }

    public void SetCollisionGenerationEnabled(bool is_enabled)
    {
        m_generate_collision = is_enabled;
    }

    public void Render(float dt, Material prepass_material, Material material, bool cast_shadows)
    {
        if (!m_is_empty)
        {
            var matrix = Matrix4x4.identity;
            if(prepass_material != null)
            {
                Graphics.DrawMesh(m_mesh, matrix, prepass_material, 0, null, 0, null, false, false, false);
            }
            Graphics.DrawMesh(m_mesh, matrix, material, 0, null, 0, null, cast_shadows);
        }
    }

    byte[] m_layer_above_sample_grid;
    byte[] m_layer_below_sample_grid;
    float[] m_layer_density_grid;
    byte[] m_layer_sample_grid;
    int m_layer_width_in_voxels;
    int m_layer_height_in_voxels;
    Mesh m_mesh;
    MeshCollider m_collider;
    float m_iso_level;
    float m_bot_y;
    float m_top_y;
    Vector3 m_voxel_size_in_meters;
    float m_density_height_weight;
    int m_density_grid_x;
    int m_density_grid_y;
    int m_chunk_dimension_in_voxels;
    bool m_has_any_samples;
    bool m_is_empty = true;
    bool m_generate_collision;
    BevelTuning m_bevel_tuning;
    bool m_is_liquid;
    MeshUpdateFlags m_mesh_update_flags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds
#if !UNITY_EDITOR
        | MeshUpdateFlags.DontValidateIndices
#endif
        ;

    struct PositionWriter
    {
        public PositionWriter(Vector3[] positions, int vert_count)
        {
            m_positions = positions;
            m_vert_count = vert_count;
        }
        public ushort Write(Vector3 pos)
        {
            var vert_idx = (ushort)m_vert_count++;
            m_positions[vert_idx] = pos;
            return vert_idx;
        }

        public Vector3 this[int idx] { get => m_positions[idx]; }

        public int Count { get => m_vert_count; }

        public Vector3[] m_positions;
        public int m_vert_count;
    }

    public struct TriangleWriter
    {
        public TriangleWriter(ushort[] triangles, ushort triangle_count, List<ushort> m_border_triangles)
        {
            m_triangles = triangles;
            m_triangle_count = triangle_count;
            this.m_border_triangles = m_border_triangles;
        }

        public void Write(ushort vert_idx0, ushort vert_idx1, ushort vert_idx2, bool is_border_triangle)
        {
            if(is_border_triangle)
            {
                m_border_triangles.Add(vert_idx0);
                m_border_triangles.Add(vert_idx1);
                m_border_triangles.Add(vert_idx2);
            }
            else
            {
                m_triangles[m_triangle_count++] = vert_idx0;
                m_triangles[m_triangle_count++] = vert_idx1;
                m_triangles[m_triangle_count++] = vert_idx2;
            }
        }

        public ushort this[int idx] { get => m_triangles[idx]; }

        public int Count { get => m_triangle_count; }

        public ushort[] m_triangles;
        public ushort m_triangle_count;
        public List<ushort> m_border_triangles;
    }

}