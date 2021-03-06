﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LiquidSimulation
{
    const float MAX_VALUE = 1.0f;
    const float MAX_COMPRESSION = 0.25f;
    const float MAX_FLOW = 4f;

    const float SIMULATION_TICK_RATE = 1f / 30f;

    const float ALIGNED_FLOW_MULTIPLIRE = 1f / 4f;

    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public int CreateChunkIdFromChunkCoordinates(int x, int y, int z)
    {
        return x | (y >> 8) | z >> 16; 
    }

    public void GetChunkCoordinatesFromChunkId(int chunk_id, out int chunk_x, out int chunk_y, out int chunk_z)
    {
        chunk_x = chunk_id & 0x000000FF;
        chunk_y = (chunk_id << 8) & 0x000000FF;
        chunk_z = (chunk_id << 16) & 0x000000FF;
    }

    public LiquidSimulation(Vector3Int dimensions_in_cells, Vector3 cell_size_in_meters, int chunk_dimensions_in_cells, float[][] solid_layers, float solid_iso_level, float min_density_to_allow_flow, int water_plane_layer, LiquidTuning liquid_tuning)
    {        
        m_solid_iso_level = solid_iso_level;
        m_solid_layers = solid_layers;
        m_cell_size_in_meters = cell_size_in_meters;
        m_dimensions_in_cells = dimensions_in_cells;
        m_layers = new float[dimensions_in_cells.y][];
        m_delta_layers = new float[dimensions_in_cells.y][];
        m_dimensions_in_chunks = new Vector3Int(dimensions_in_cells.x / chunk_dimensions_in_cells, dimensions_in_cells.y / chunk_dimensions_in_cells, dimensions_in_cells.z / chunk_dimensions_in_cells);
        m_water_plane_layer = water_plane_layer;
        m_liquid_tuning = liquid_tuning;

        m_min_density_to_allow_flow = min_density_to_allow_flow;

        m_chunk_dimensions_in_cells = chunk_dimensions_in_cells;
        m_one_over_chunk_dimensions_in_cells = 1f / chunk_dimensions_in_cells;

        m_min_dirty_cell_per_layer = new Vector3Int[m_layers.Length];
        m_max_dirty_cell_per_layer = new Vector3Int[m_layers.Length];
        m_min_dirty_cell_delta_per_layer = new Vector3Int[m_layers.Length];
        m_max_dirty_cell_delta_per_layer = new Vector3Int[m_layers.Length];

        for (int i = 0; i < m_dimensions_in_cells.y; ++i)
        {
            m_layers[i] = new float[m_dimensions_in_cells.x * m_dimensions_in_cells.z];
            m_delta_layers[i] = new float[m_dimensions_in_cells.x * m_dimensions_in_cells.z];
        }


        Clear();
    }

    public void AddDensity(Vector3 world_pos, float amount)
    {
        var layer_idx = (int)(world_pos.y / m_cell_size_in_meters.y);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_density_changes.Add(new DensityChange { m_position = world_pos, m_layer_idx = layer_idx, m_amount = amount });
    }

    float CalculateVerticalFlowValue(float remaining_liquid, float destination_liquid)
    {
        float sum = remaining_liquid + destination_liquid;
        float value = 0;

        if (sum <= MAX_VALUE)
        {
            value = MAX_VALUE;
        }
        else if (sum < 2 * MAX_VALUE + MAX_COMPRESSION)
        {
            value = (MAX_VALUE * MAX_VALUE + sum * MAX_COMPRESSION) / (MAX_VALUE + MAX_COMPRESSION);
        }
        else
        {
            value = (sum + MAX_COMPRESSION) / 2f;
        }

        return value;
    }

    public void SetSimulationEnabled(bool is_enabled)
    {
        m_simulation_enabled = is_enabled;
    }

    public bool IsSimulationEnabled() { return m_simulation_enabled; }

    void UpdateSimulation(bool force)
    {
        var min_density_to_allow_flow = m_min_density_to_allow_flow;

        for (int i = 0; i < m_layers.Length; ++i)
        {
            m_min_dirty_cell_delta_per_layer[i] = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            m_max_dirty_cell_delta_per_layer[i] = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }

        float min_mass_for_flow = m_liquid_tuning.m_min_mass_for_flow;
        float min_mass_for_transfer_between_cells = m_liquid_tuning.m_min_mass_for_transfer_between_cells;

        for (int layer_idx = 1; layer_idx < m_layers.Length - 1; ++layer_idx)
        {
            var solid_layer = m_solid_layers[layer_idx];
            var lower_solid_layer = m_solid_layers[layer_idx - 1];
            var upper_solid_layer = m_solid_layers[layer_idx + 1];
            
            var layer = m_layers[layer_idx];
            var lower_layer = m_layers[layer_idx - 1];
            var upper_layer = m_layers[layer_idx + 1];

            var delta_layer = m_delta_layers[layer_idx];
            var lower_delta_layer = m_delta_layers[layer_idx - 1];
            var upper_delta_layer = m_delta_layers[layer_idx + 1];

            var min_dirty_idx = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            var max_dirty_idx = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            var min_lower_dirty_idx = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            var max_lower_dirty_idx = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            var min_upper_dirty_idx = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            var max_upper_dirty_idx = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            var current_min_dirty_idx = m_min_dirty_cell_per_layer[layer_idx];
            var current_max_dirty_idx = m_max_dirty_cell_per_layer[layer_idx];

            var min_x = current_min_dirty_idx.x;
            var max_x = current_max_dirty_idx.x;
            var min_z = current_min_dirty_idx.z;
            var max_z = current_max_dirty_idx.z;

            bool is_target_layer_water_plane = layer_idx - 1 == m_water_plane_layer;

            if(force)
            {
                min_x = 0;
                max_x = m_dimensions_in_cells.x - 1;

                min_z = 0;
                max_z = m_dimensions_in_cells.z - 1;
            }

            for (int z = min_z; z <= max_z; ++z)
            {
                for (int x = min_x; x <= max_x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.x + x;

#if UNITY_EDITOR
                    if(CellDebugger.s_debug_cell.m_x == x && CellDebugger.s_debug_cell.m_z == z && CellDebugger.s_debug_cell.m_layer_idx == layer_idx)
                    {
                        int bp = 0;
                        ++bp;
                    }
#endif

                    var start_liquid = layer[cell_idx];

                    float flow_liquid = start_liquid;

                    if (flow_liquid <= 0)
                    {
                        continue;
                    }

                    FlowAndTryToFinish(ref flow_liquid, min_density_to_allow_flow, cell_idx, cell_idx, delta_layer, lower_layer, lower_delta_layer, lower_solid_layer, x, layer_idx, z, x, z, true, is_target_layer_water_plane, ref min_dirty_idx, ref max_dirty_idx, ref min_lower_dirty_idx, ref max_lower_dirty_idx, ALIGNED_FLOW_MULTIPLIRE);

                    if(flow_liquid <= 0)
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref flow_liquid, min_density_to_allow_flow, cell_idx, cell_idx - 1, delta_layer, layer, delta_layer, solid_layer, x, layer_idx, z, x - 1, z, false, false, ref min_dirty_idx, ref max_dirty_idx, ref min_dirty_idx, ref max_dirty_idx, ALIGNED_FLOW_MULTIPLIRE))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref flow_liquid, min_density_to_allow_flow, cell_idx, cell_idx + 1, delta_layer, layer, delta_layer, solid_layer, x, layer_idx, z, x + 1, z, false, false, ref min_dirty_idx, ref max_dirty_idx, ref min_dirty_idx, ref max_dirty_idx, ALIGNED_FLOW_MULTIPLIRE))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref flow_liquid, min_density_to_allow_flow, cell_idx, cell_idx - m_dimensions_in_cells.x, delta_layer, layer, delta_layer, solid_layer, x, layer_idx, z, x, z - 1, false, false, ref min_dirty_idx, ref max_dirty_idx, ref min_dirty_idx, ref max_dirty_idx, ALIGNED_FLOW_MULTIPLIRE))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref flow_liquid, min_density_to_allow_flow, cell_idx, cell_idx + m_dimensions_in_cells.x, delta_layer, layer, delta_layer, solid_layer, x, layer_idx, z, x - 1, z + 1, false, false, ref min_dirty_idx, ref max_dirty_idx, ref min_dirty_idx, ref max_dirty_idx, ALIGNED_FLOW_MULTIPLIRE))
                    {
                        continue;
                    }
                }
            }

            m_min_dirty_cell_delta_per_layer[layer_idx] = Vector3Int.Min(min_dirty_idx, m_min_dirty_cell_delta_per_layer[layer_idx]);
            m_max_dirty_cell_delta_per_layer[layer_idx] = Vector3Int.Max(max_dirty_idx, m_max_dirty_cell_delta_per_layer[layer_idx]);
            m_min_dirty_cell_delta_per_layer[layer_idx - 1] = Vector3Int.Min(min_lower_dirty_idx, m_min_dirty_cell_delta_per_layer[layer_idx - 1]);
            m_max_dirty_cell_delta_per_layer[layer_idx - 1] = Vector3Int.Max(max_lower_dirty_idx, m_max_dirty_cell_delta_per_layer[layer_idx - 1]);
            m_min_dirty_cell_delta_per_layer[layer_idx + 1] = Vector3Int.Min(min_upper_dirty_idx, m_min_dirty_cell_delta_per_layer[layer_idx + 1]);
            m_max_dirty_cell_delta_per_layer[layer_idx + 1] = Vector3Int.Max(max_upper_dirty_idx, m_max_dirty_cell_delta_per_layer[layer_idx + 1]);
        }

        for (int layer_idx = 0; layer_idx < m_delta_layers.Length; ++layer_idx)
        {
            var layer = m_layers[layer_idx];
            var delta_layer = m_delta_layers[layer_idx];

            var min_dirty_cell_delta_per_layer = m_min_dirty_cell_delta_per_layer[layer_idx];
            var max_dirty_cell_delta_per_layer = m_max_dirty_cell_delta_per_layer[layer_idx];

            min_dirty_cell_delta_per_layer = Vector3Int.Max(min_dirty_cell_delta_per_layer, new Vector3Int(1, 1, 1));
            max_dirty_cell_delta_per_layer = Vector3Int.Max(max_dirty_cell_delta_per_layer, new Vector3Int(1, 1, 1));
            min_dirty_cell_delta_per_layer = Vector3Int.Min(min_dirty_cell_delta_per_layer, new Vector3Int(m_dimensions_in_cells.x - 2, m_dimensions_in_cells.y - 2, m_dimensions_in_cells.z - 2));
            max_dirty_cell_delta_per_layer = Vector3Int.Min(max_dirty_cell_delta_per_layer, new Vector3Int(m_dimensions_in_cells.x - 2, m_dimensions_in_cells.y - 2, m_dimensions_in_cells.z - 2));

            m_min_dirty_cell_per_layer[layer_idx] = min_dirty_cell_delta_per_layer;
            m_max_dirty_cell_per_layer[layer_idx] = max_dirty_cell_delta_per_layer;
            m_min_dirty_cell_delta_per_layer[layer_idx] = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            m_max_dirty_cell_delta_per_layer[layer_idx] = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

            for (int z = min_dirty_cell_delta_per_layer.z; z <= max_dirty_cell_delta_per_layer.z; ++z)
            {
                for (int x = min_dirty_cell_delta_per_layer.x; x <= max_dirty_cell_delta_per_layer.x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.z + x;

                    var delta = delta_layer[cell_idx];
                    if(delta != 0)
                    {
                        var liquid = layer[cell_idx] + delta;
                        layer[cell_idx] = liquid;
                        delta_layer[cell_idx] = 0;
                    }
                }
            }
        }
    }

    public bool FlowAndTryToFinish(ref float remaining_liquid, float min_density_to_allow_flow, int cell_idx, int target_cell_idx, float[] delta_layer, float[] target_layer, float[] target_delta_layer, float[] target_solid_layer, int x, int layer_idx, int z, int target_x, int target_z, bool is_bottom_cell, bool is_target_layer_water_plane, ref Vector3Int min_dirty_idx, ref Vector3Int max_dirty_idx, ref Vector3Int target_min_dirty_idx, ref Vector3Int target_max_dirty_idx, float flow_multiplier)
    {
        bool is_target_solid = target_solid_layer[target_cell_idx] > m_solid_iso_level;
        if (!is_target_solid)
        {
            var target_liquid =target_layer[target_cell_idx];
            var flow = (remaining_liquid - target_liquid) * flow_multiplier;
            flow = flow * 0.5f;
            if(is_bottom_cell)
            {
                flow = Mathf.Min(1f - target_liquid,  remaining_liquid);
            }

            flow = Mathf.Min(remaining_liquid, Mathf.Min(MAX_FLOW, flow));

            if (flow > 0)
            {
                remaining_liquid -= flow;
                delta_layer[cell_idx] -= flow;

                min_dirty_idx = Vector3Int.Min(min_dirty_idx, new Vector3Int(x, layer_idx, z));
                max_dirty_idx = Vector3Int.Max(max_dirty_idx, new Vector3Int(x, layer_idx, z));

                const float MIN_LIQUID = 0.0001f;
                bool is_evaporating = flow < MIN_LIQUID && target_liquid < MIN_LIQUID;
                if (!is_evaporating && !is_target_layer_water_plane)
                {
                    target_delta_layer[target_cell_idx] += flow;
                    target_min_dirty_idx = Vector3Int.Min(target_min_dirty_idx, new Vector3Int(target_x, layer_idx, target_z));
                    target_max_dirty_idx = Vector3Int.Max(target_max_dirty_idx, new Vector3Int(target_x, layer_idx, target_z));
                }

                if (remaining_liquid <= 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void StepOnce(bool force)
    {
        UpdateSimulation(force);
    }

    public void Clear()
    {
        m_min_dirty_cell_per_layer = new Vector3Int[m_layers.Length];
        m_max_dirty_cell_per_layer = new Vector3Int[m_layers.Length];

        for (int i = 0; i < m_layers.Length; ++i)
        {
            m_min_dirty_cell_per_layer[i] = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            m_max_dirty_cell_per_layer[i] = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
            m_min_dirty_cell_delta_per_layer[i] = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            m_max_dirty_cell_delta_per_layer[i] = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }

        for (int layer_idx = 0; layer_idx < m_delta_layers.Length; ++layer_idx)
        {
            var layer = m_layers[layer_idx];
            var delta_layer = m_delta_layers[layer_idx];

            for(int cell_idx = 0; cell_idx < layer.Length; ++cell_idx)
            {
                layer[cell_idx] = 0;
                delta_layer[cell_idx] = 0;
            }
        }
    }

    public void Update(List<DensityCell> dirty_density_cells, HashSet<Vector3Int> liquid_mesh_dirty_chunk_ids)
    {
        if (m_density_changes.Count > 0)
        {
            for (int layer_idx = 0; layer_idx < m_dimensions_in_cells.y; ++layer_idx)
            {
                var layer = m_layers[layer_idx];
                var solid_layer = m_solid_layers[layer_idx];
                foreach (var density_change in m_density_changes)
                {
                    if (density_change.m_layer_idx != layer_idx) continue;

                    var pos = density_change.m_position;

                    var x = (int)(pos.x / (float)m_cell_size_in_meters.x);
                    if (x < 0 || x >= m_dimensions_in_cells.x) return;

                    var z = (int)(pos.z / (float)m_cell_size_in_meters.z);
                    if (z < 0 || z >= m_dimensions_in_cells.z) return;

                    var cell_idx = z * m_dimensions_in_cells.x + x;

                    if (solid_layer[cell_idx] > m_solid_iso_level) continue;

                    var previous_density = layer[cell_idx];
                    var new_density = Mathf.Clamp01(previous_density + density_change.m_amount);
                    if (new_density != previous_density)
                    {
                        layer[cell_idx] = new_density;

                        var cell_coordinates = new Vector3Int(x, layer_idx, z);
                        m_min_dirty_cell_per_layer[layer_idx] = Vector3Int.Min(m_min_dirty_cell_per_layer[layer_idx], cell_coordinates);
                        m_max_dirty_cell_per_layer[layer_idx] = Vector3Int.Max(m_max_dirty_cell_per_layer[layer_idx], cell_coordinates);
                    }
                }
            }
        }

        if (dirty_density_cells.Count > 0)
        {
            foreach (var dirty_solid_density_cell in dirty_density_cells)
            {
                var layer_idx = dirty_solid_density_cell.m_layer_idx;
                var min_x = Math.Max(dirty_solid_density_cell.m_x - 1, 0);
                var max_x = Math.Min(dirty_solid_density_cell.m_x + 1, m_dimensions_in_cells.x - 1);
                var min_z = Math.Max(dirty_solid_density_cell.m_z - 1, 0);
                var max_z = Math.Min(dirty_solid_density_cell.m_z + 1, m_dimensions_in_cells.z - 1);
                var min_layer_idx = Math.Max(dirty_solid_density_cell.m_layer_idx - 1, 0);
                var max_layer_idx = Math.Max(dirty_solid_density_cell.m_layer_idx + 1, m_dimensions_in_cells.y - 1);

                for (int y = min_layer_idx; y <= max_layer_idx; ++y)
                {
                    for (int z = min_z; z <= max_z; ++z)
                    {
                        for (int x = min_x; x <= max_x; ++x)
                        {
                            var cell_coordinates = new Vector3Int(x, y, z);
                            m_min_dirty_cell_per_layer[layer_idx] = Vector3Int.Min(m_min_dirty_cell_per_layer[layer_idx], cell_coordinates);
                            m_max_dirty_cell_per_layer[layer_idx] = Vector3Int.Max(m_max_dirty_cell_per_layer[layer_idx], cell_coordinates);
                        }
                    }
                }
            }
        }

        m_simulation_timer += Time.deltaTime;
        while (m_simulation_timer >= SIMULATION_TICK_RATE)
        {
            Profiler.BeginSample("UpdateSimulation");
            if (m_simulation_enabled)
            {
                UpdateSimulation(false);

                for (int layer_idx = 0; layer_idx < m_layers.Length; ++layer_idx)
                {
                    var min_dirty_cell_coordinates = m_min_dirty_cell_per_layer[layer_idx];
                    var max_dirty_cell_coordinates = m_max_dirty_cell_per_layer[layer_idx];

                    int min_chunk_grid_z = (int)((min_dirty_cell_coordinates.z - 1) * m_one_over_chunk_dimensions_in_cells);
                    int max_chunk_grid_z = (int)((max_dirty_cell_coordinates.z + 1) * m_one_over_chunk_dimensions_in_cells);

                    int min_chunk_grid_x = (int)((min_dirty_cell_coordinates.x - 1) * m_one_over_chunk_dimensions_in_cells);
                    int max_chunk_grid_x = (int)((max_dirty_cell_coordinates.x + 1) * m_one_over_chunk_dimensions_in_cells);

                    max_chunk_grid_x = Math.Min(max_chunk_grid_x, m_dimensions_in_chunks.x - 1);
                    max_chunk_grid_z = Math.Min(max_chunk_grid_z, m_dimensions_in_chunks.z - 1);

                    for (int chunk_grid_z = min_chunk_grid_z; chunk_grid_z <= max_chunk_grid_z; ++chunk_grid_z)
                    {
                        for (int chunk_grid_x = min_chunk_grid_x; chunk_grid_x <= max_chunk_grid_x; ++chunk_grid_x)
                        {
                            liquid_mesh_dirty_chunk_ids.Add(new Vector3Int(chunk_grid_x, layer_idx, chunk_grid_z));
                        }
                    }
                }
            }
            Profiler.EndSample();
            m_simulation_timer = 0;
            //m_simulation_timer -= SIMULATION_TICK_RATE;
        }

        m_density_changes.Clear();
    }

    static Hash LIQUID_SIMULATION_ID = new Hash("LiquidSimulation");

    public void Save(ChunkSerializer serializer)
    {
        serializer.BeginChunk(LIQUID_SIMULATION_ID);

        serializer.Write(m_min_dirty_cell_per_layer);
        serializer.Write(m_max_dirty_cell_per_layer);

        for (int y = 0; y < m_layers.Length; ++y)
        {
            var layer = m_layers[y];
            serializer.Write(layer);
        }

        serializer.EndChunk();
    }

    public void Load(ChunkDeserializer deserializer)
    {
        if (deserializer.TryGetChunk(LIQUID_SIMULATION_ID))
        {
            deserializer.Read(m_min_dirty_cell_per_layer);
            deserializer.Read(m_max_dirty_cell_per_layer);

            for (int y = 0; y < m_layers.Length; ++y)
            {
                var layer = m_layers[y];
                deserializer.Read(layer);
            }
        }
    }

    public float[][] GetLayers() { return m_layers; }

    public bool TryGetDensity(DensityCell cell, out float density)
    {
        if (cell.IsInBounds(m_dimensions_in_cells))
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

    float[][] m_layers;
    float[][] m_delta_layers;
    float[][] m_solid_layers;
    float m_solid_iso_level;
    Vector3Int[] m_min_dirty_cell_per_layer;
    Vector3Int[] m_max_dirty_cell_per_layer;
    Vector3Int[] m_min_dirty_cell_delta_per_layer;
    Vector3Int[] m_max_dirty_cell_delta_per_layer;
    Vector3 m_cell_size_in_meters;

    Vector3Int m_dimensions_in_chunks;
    Vector3Int m_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
    float m_one_over_chunk_dimensions_in_cells;
    List<DensityChange> m_density_changes = new List<DensityChange>();
    float m_simulation_timer;
    bool m_simulation_enabled;
    float m_min_density_to_allow_flow;
    int m_water_plane_layer;
    LiquidTuning m_liquid_tuning;
}