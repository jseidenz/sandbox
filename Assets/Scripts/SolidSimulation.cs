﻿using UnityEngine;
using System.Collections.Generic;
using System;

public struct DensityCell
{
    public int m_x;
    public int m_layer_idx;
    public int m_z;

    public static DensityCell FromXZLayer(int x, int z, int layer_idx)
    {
        return new DensityCell { m_x = x, m_z = z, m_layer_idx = layer_idx };
    }

    public bool IsInBounds(Vector3Int dimensions_in_cells)
    {
        return m_x >= 0 && m_x < dimensions_in_cells.x && m_z >= 0 && m_z < dimensions_in_cells.z && m_layer_idx >= 0 && m_layer_idx < dimensions_in_cells.y;
    }

    public int ToCellIdx(Vector3Int dimensions_in_cells)
    {
        return m_z * dimensions_in_cells.x + m_x;
    }

    public DensityCell Dx(int delta_x)
    {
        return DensityCell.FromXZLayer(m_x + delta_x, m_z, m_layer_idx);
    }

    public DensityCell Dz(int delta_z)
    {
        return DensityCell.FromXZLayer(m_x, m_z + delta_z, m_layer_idx);
    }

    public DensityCell Dl(int delta_layer)
    {
        return DensityCell.FromXZLayer(m_x, m_z, m_layer_idx + delta_layer);
    }

    public Vector3 ToWorldPositionCenter(Vector3 cell_size_in_meters)
    {
        return new Vector3((float)m_x * cell_size_in_meters.x, (float)m_layer_idx * cell_size_in_meters.y, (float)m_z * cell_size_in_meters.z);
    }
}

public class SolidSimulation
{
    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public SolidSimulation(Vector3Int dimensions_in_cells, Vector3 cell_size_in_meters, int chunk_dimensions_in_cells)
    {
        m_cell_size_in_meters = cell_size_in_meters;
        m_dimensions_in_cells = dimensions_in_cells;
        m_layers = new float[dimensions_in_cells.y][];

        for(int i = 0; i < dimensions_in_cells.y; ++i)
        {
            m_layers[i] = new float[dimensions_in_cells.x * dimensions_in_cells.z];
        }

        m_chunk_dimensions_in_cells = chunk_dimensions_in_cells;
        m_one_over_chunk_dimensions_in_cells = 1f / chunk_dimensions_in_cells;
    }

    public void AddDensity(Vector3 world_pos, float amount)
    {
        var layer_idx = (int)(world_pos.y / m_cell_size_in_meters.y);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_density_changes.Add(new DensityChange { m_position = world_pos, m_layer_idx = layer_idx, m_amount = amount });
    }

    public void Update(List<DensityCell> dirty_density_cells, HashSet<Vector3Int> dirty_chunk_ids)
    {
        if (m_density_changes.Count <= 0) return;

        for(int layer_idx = 0; layer_idx < m_dimensions_in_cells.y; ++layer_idx)
        {
            var layer = m_layers[layer_idx];
            foreach(var density_change in m_density_changes)
            {
                if (density_change.m_layer_idx != layer_idx) continue;

                int radius = 0;
                if (density_change.m_amount > 0)
                {
                    radius = 1;
                }

                for (int range_offset_z = -radius; range_offset_z <= radius; ++range_offset_z)
                {
                    for (int range_offset_x = -radius; range_offset_x <= radius; ++range_offset_x)
                    {
                        float amount_multiplier = 1f;
                        bool is_center = range_offset_x == 0 && range_offset_z == 0;
                        if (!is_center)
                        {
                            amount_multiplier = 0.5f;
                        }

                        var pos = density_change.m_position;

                        var x = (int)(pos.x / (float)m_cell_size_in_meters.x) + range_offset_x;
                        if (x < 0 || x >= m_dimensions_in_cells.x) continue;

                        var z = (int)(pos.z / (float)m_cell_size_in_meters.z) + range_offset_z;
                        if (z < 0 || z >= m_dimensions_in_cells.z) continue;

                        var cell_idx = z * m_dimensions_in_cells.x + x;

                        var previous_density = layer[cell_idx];

                        float amount = density_change.m_amount * amount_multiplier;
                        if(amount > 0)
                        {
                            if(is_center)
                            {
                                amount = 1f;
                            }
                            else
                            {
                                amount = 0.5f;
                            }
                        }
                        var new_density = Mathf.Clamp01(previous_density + amount);
                        if (new_density != previous_density)
                        {
                            layer[cell_idx] = new_density;

                            dirty_density_cells.Add(DensityCell.FromXZLayer(x, z, layer_idx));

                            for (int j = -2; j <= 2; ++j)
                            {
                                for (int i = -2; i <= 2; ++i)
                                {
                                    var offset_x = Math.Min(Math.Max(x + i, 0), m_dimensions_in_cells.x - 1);
                                    var offset_z = Math.Min(Math.Max(z + j, 0), m_dimensions_in_cells.z - 1);

                                    var chunk_grid_x = (int)(offset_x * m_one_over_chunk_dimensions_in_cells);
                                    var chunk_grid_z = (int)(offset_z * m_one_over_chunk_dimensions_in_cells);

                                    dirty_chunk_ids.Add(new Vector3Int(chunk_grid_x, layer_idx, chunk_grid_z));
                                }
                            }
                        }
                    }
                }
            }
        }

        m_density_changes.Clear();
    }

    public void ApplyHeightMap(float[] densities)
    {
        float one_layer_height_in_density_space = (float)m_layers.Length;

        for (int layer_idx = 0; layer_idx < m_layers.Length; ++layer_idx)
        {
            var layer = m_layers[layer_idx];

            float iso_level = layer_idx / (float)m_dimensions_in_cells.y;

            for (int z = 0; z < m_dimensions_in_cells.z; ++z)
            {
                for(int x = 0; x < m_dimensions_in_cells.x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.x + x;

                    float input_density = densities[cell_idx];
                    float deltaed_density = input_density - iso_level;
                    float normalized_density = deltaed_density * one_layer_height_in_density_space;
                    float clamped_density = Mathf.Clamp01(normalized_density);

                    layer[cell_idx] = clamped_density;
                }
            }
        }
    }

    public float[][] GetLayers() { return m_layers; }


    static Hash SOLID_SIMULATION_ID = new Hash("SolidSimulation");

    public void Save(ChunkSerializer serializer)
    {
        serializer.BeginChunk(SOLID_SIMULATION_ID);

        for(int y = 0; y < m_layers.Length; ++y)
        {
            var layer = m_layers[y];
            serializer.Write(layer);
        }

        serializer.EndChunk();
    }

    public void Load(ChunkDeserializer deserializer)
    {
        if(deserializer.TryGetChunk(SOLID_SIMULATION_ID))
        {
            for (int y = 0; y < m_layers.Length; ++y)
            {
                var layer = m_layers[y];
                deserializer.Read(layer);
            }
        }        
    }

    public bool TryGetDensityCellFromWorldPosition(Vector3 world_pos, out DensityCell density_cell)
    {
        var layer_idx = (int)(world_pos.y / m_cell_size_in_meters.y);
        if (layer_idx < 0 || layer_idx >= m_layers.Length)
        {
            density_cell = default;
            return false;
        }

        var x = (int)(world_pos.x / (float)m_cell_size_in_meters.x);
        if (x < 0 || x >= m_dimensions_in_cells.x)
        {
            density_cell = default;
            return false;
        }

        var z = (int)(world_pos.z / (float)m_cell_size_in_meters.z);
        if (z < 0 || z >= m_dimensions_in_cells.z)
        {
            density_cell = default;
            return false;
        };

        density_cell = new DensityCell { m_x = x, m_layer_idx = layer_idx, m_z = z };
        return true;
    }

    public bool TryGetDensity(DensityCell cell, out float density)
    {
        if(cell.IsInBounds(m_dimensions_in_cells))
        {
            var cell_idx = cell.ToCellIdx(m_dimensions_in_cells);
            density = m_layers[cell.m_layer_idx][cell_idx];
            return true;
        }
        else
        {
            density = default;
            return false;
        }
    }

    public Vector3Int GetDimensionsInCells() { return m_dimensions_in_cells; }

    float[][] m_layers;
    Vector3 m_cell_size_in_meters;

    Vector3Int m_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
    float m_one_over_chunk_dimensions_in_cells;
    List<DensityChange> m_density_changes = new List<DensityChange>();
}