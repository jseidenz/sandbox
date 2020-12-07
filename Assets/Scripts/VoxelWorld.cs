﻿using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;

public class VoxelWorld : MonoBehaviour
{
    [System.Serializable]
    public class LiveTuneable
    {
        public float m_water_height;
        public Gradient m_height_gradient = new Gradient();

        public float m_saturation_variation_rate;
        public float m_saturation_variation_amount;
        public float m_hue_variation_rate;
        public float m_hue_variaton_amount;
    }

    public LiveTuneable m_tuneables;

    [SerializeField] Material m_material;
    [SerializeField] float m_ground_plane_size;
    [SerializeField] GameObject m_water;

    int m_voxel_chunk_dimensions;
    int m_grid_height_in_voxels;
    int m_grid_width_in_voxels;
    int m_grid_depth_in_voxels;
    float m_voxel_size_in_meters;
    VoxelLayer[] m_layers;
    bool[] m_empty_occlusion_grid;
    VoxelChunk.ScratchBuffer m_voxel_chunk_scratch_buffer;
    HashSet<int> m_dirty_chunk_occlusion_indices = new HashSet<int>();
    GameObject m_ground_plane;

    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public void Init(float[][] density_grids, int grid_width_in_voxels, int grid_height_in_voxels, int grid_depth_in_voxels, float voxel_size_in_meters, int voxel_chunk_dimesnions)
    {
        m_voxel_chunk_dimensions = voxel_chunk_dimesnions;
        m_voxel_size_in_meters = voxel_size_in_meters;
        m_grid_width_in_voxels = grid_width_in_voxels;
        m_grid_height_in_voxels = grid_height_in_voxels;
        m_grid_depth_in_voxels = grid_depth_in_voxels;

        m_empty_occlusion_grid = new bool[m_grid_width_in_voxels * m_grid_depth_in_voxels];

        m_voxel_chunk_scratch_buffer = VoxelChunk.ScratchBuffer.CreateScratchBuffer();

        m_layers = new VoxelLayer[m_grid_height_in_voxels];

        var camera = Camera.main;

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        { 
            var material = GameObject.Instantiate(m_material);

            float iso_level = y / (float)m_grid_height_in_voxels;

            float bot_y = (float)(y - 1) * m_voxel_size_in_meters;
            float top_y = (float)y * m_voxel_size_in_meters;

            var layer = new VoxelLayer(density_grids[y], y, m_grid_width_in_voxels, m_grid_depth_in_voxels, m_voxel_chunk_dimensions, m_voxel_size_in_meters, material, iso_level, bot_y, top_y, camera);
            m_layers[y] = layer;
        }

        for (int y = 0; y < m_grid_height_in_voxels; ++y)
        {
            var layer_above_occlusion_grid = m_empty_occlusion_grid;
            var layer_below_occlusion_grid = m_empty_occlusion_grid;

            if(y > 0)
            {
                layer_below_occlusion_grid = m_layers[y - 1].GetOcclusionGrid();
            }

            if(y < m_grid_height_in_voxels - 2)
            {
                layer_above_occlusion_grid = m_layers[y + 1].GetOcclusionGrid();
            }

            m_layers[y].SetAboveAndBelowOcclusionGrids(layer_above_occlusion_grid, layer_below_occlusion_grid);
        }

        m_ground_plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        m_ground_plane.name = "GroundPlane";

        var ground_plane_material = GameObject.Instantiate(m_material);
        ground_plane_material.color = GetColorForLayer(0);

        var ground_plane_mesh_renderer = m_ground_plane.GetComponent<MeshRenderer>();
        ground_plane_mesh_renderer.sharedMaterial = ground_plane_material;
        ground_plane_mesh_renderer.receiveShadows = false;
        m_ground_plane.transform.localScale = new Vector3(m_ground_plane_size, 1, m_ground_plane_size);
        m_ground_plane.transform.localPosition = new Vector3(0, -0.5f, 0);

        m_water = GameObject.Instantiate(m_water);
    }

    public void BindCamera(Camera camera)
    {
        foreach(var layer in m_layers)
        {
            layer.BindCamera(camera);
        }
    }

    private void LateUpdate()
    {
        m_water.transform.position = new Vector3(0, m_tuneables.m_water_height, 0);

        float dt = Time.deltaTime;

        Profiler.BeginSample("Render");
        for (int y = 0; y < m_layers.Length; ++y)
        {
            var color = GetColorForLayer(y);

            m_layers[y].Render(dt, color);
        }
        Profiler.EndSample();
    }

    public Color GetColorForLayer(int layer_idx)
    {
        float layer_height_in_meters = m_grid_height_in_voxels * m_voxel_size_in_meters;
        float layer_height_over_world_height = layer_idx / layer_height_in_meters;

        var color = m_tuneables.m_height_gradient.Evaluate(layer_height_over_world_height);
        if(layer_height_in_meters > m_tuneables.m_water_height)
        {
            Color.RGBToHSV(color, out var hue, out var saturation, out var value);

            hue += Mathf.Sin(layer_height_over_world_height * m_tuneables.m_hue_variation_rate) * m_tuneables.m_hue_variaton_amount;
            saturation += Mathf.Sin(layer_height_over_world_height * m_tuneables.m_saturation_variation_rate) * m_tuneables.m_saturation_variation_amount;

            color = Color.HSVToRGB(hue, saturation, value);
        }
        return color;
    }

    public void Triangulate(HashSet<Vector3Int> dirty_chunk_ids)
    {
        if (dirty_chunk_ids.Count == 0) return;

        m_dirty_chunk_occlusion_indices.Clear();

        for (int y = m_grid_height_in_voxels - 1; y >= 0; --y)
        {
            var layer = m_layers[y];

            Profiler.BeginSample("Triangulate");
            layer.Triangulate(m_voxel_chunk_scratch_buffer, dirty_chunk_ids);
            Profiler.EndSample();
        }        
    }

    public void TriangulateAll()
    {
        foreach(var layer in m_layers)
        {
            layer.Triangulate(m_voxel_chunk_scratch_buffer);
        }        
    }

    void OnDestroy()
    {
        foreach(var layer in m_layers)
        {
            layer.OnDestroy();
        }
    }

    public int GetGridHeightInVoxels() { return m_grid_depth_in_voxels; }
    public int GetGridWidthInVoxels() { return m_grid_depth_in_voxels; }

    public float GetVoxelSizeInMeters() { return m_voxel_size_in_meters;  }
}