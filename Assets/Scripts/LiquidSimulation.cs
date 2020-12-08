using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LiquidSimulation
{
    const float MAX_VALUE = 1.0f;
    const float MIN_VALUE = 0.0005f;
    const float MAX_COMPRESSION = 0.25f;
    const float MIN_FLOW = 0.005f;
    const float MAX_FLOW = 4f;
    const float FLOW_SPEED = 0.1f;

    const float SIMULATION_TICK_RATE = 1f / 30f;
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

    struct ChunkRegion
    {
        public Vector3Int m_chunk_id;
        public int m_min_x;
        public int m_min_y;
        public int m_max_x;
        public int m_max_y;
    }

    public LiquidSimulation(Vector3Int dimensions_in_cells, Vector3 cell_size_in_meters, int chunk_dimensions_in_cells, float[][] solid_layers)
    {
        m_solid_layers = solid_layers;
        m_cell_size_in_meters = cell_size_in_meters;
        m_dimensions_in_cells = dimensions_in_cells;
        m_layers = new float[dimensions_in_cells.y][];
        m_delta_layers = new float[dimensions_in_cells.y][];

        for (int i = 0; i < dimensions_in_cells.y; ++i)
        {
            m_layers[i] = new float[dimensions_in_cells.x * dimensions_in_cells.z];
            m_delta_layers[i] = new float[dimensions_in_cells.x * dimensions_in_cells.z];
        }

        m_chunk_dimensions_in_cells = chunk_dimensions_in_cells;
        m_one_over_chunk_dimensions_in_cells = 1f / chunk_dimensions_in_cells;

        m_min_dirty_cell_per_layer = new Vector3Int[m_layers.Length];
        m_max_dirty_cell_per_layer = new Vector3Int[m_layers.Length];

        for(int i = 0; i < m_layers.Length; ++i)
        {
            m_min_dirty_cell_per_layer[i] = new Vector3Int(int.MaxValue, int.MaxValue, int.MaxValue);
            m_max_dirty_cell_per_layer[i] = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
        }
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

    void UpdateSimulation()
    {
        for(int layer_idx = 1; layer_idx < m_layers.Length - 1; ++layer_idx)
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

            for (int z = m_min_dirty_cell_per_layer[layer_idx].z; z <= m_max_dirty_cell_per_layer[layer_idx].z; ++z)
            {
                for (int x = m_min_dirty_cell_per_layer[layer_idx].x; x <= m_max_dirty_cell_per_layer[layer_idx].x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.x + x;

                    var start_liquid = layer[cell_idx];

                    if (start_liquid == 0) continue;

                    if (start_liquid < MIN_VALUE)
                    {
                        layer[cell_idx] = 0;
                        continue;
                    }

                    var remaining_liquid = start_liquid;

                    bool is_bottom_solid = lower_solid_layer[cell_idx] > 0;

                    if(!is_bottom_solid)
                    {
                        var bottom_liquid = lower_layer[cell_idx];
                        var flow = CalculateVerticalFlowValue(remaining_liquid, bottom_liquid) - bottom_liquid;
                        if (bottom_liquid > 0 && flow > MIN_FLOW)
                        {
                            flow *= FLOW_SPEED;
                        }

                        flow = Mathf.Max(flow, 0);
                        flow = Mathf.Min(MAX_FLOW, remaining_liquid);

                        if (flow != 0)
                        {
                            remaining_liquid -= flow;
                            delta_layer[cell_idx] -= flow;
                            lower_delta_layer[cell_idx] += flow;

                            min_dirty_idx = Vector3Int.Min(min_dirty_idx, new Vector3Int(x, layer_idx, z));
                            max_dirty_idx = Vector3Int.Max(max_dirty_idx, new Vector3Int(x, layer_idx, z));
                            min_lower_dirty_idx = Vector3Int.Min(min_lower_dirty_idx, new Vector3Int(x, layer_idx - 1, z));
                            max_lower_dirty_idx = Vector3Int.Max(max_lower_dirty_idx, new Vector3Int(x, layer_idx - 1, z));


                            if (remaining_liquid < MIN_VALUE)
                            {
                                delta_layer[cell_idx] -= remaining_liquid;
                                continue;
                            }
                        }
                    }


                    if(FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx - 1, layer, delta_layer, solid_layer, x, layer_idx, z, x - 1, z, ref min_dirty_idx, ref max_dirty_idx))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx + 1, layer, delta_layer, solid_layer, x, layer_idx, z, x + 1, z, ref min_dirty_idx, ref max_dirty_idx))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx - m_dimensions_in_cells.x, layer, delta_layer, solid_layer, x, layer_idx, z, x, z - 1, ref min_dirty_idx, ref max_dirty_idx))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx + m_dimensions_in_cells.x, layer, delta_layer, solid_layer, x, layer_idx, z, x - 1, z + 1, ref min_dirty_idx, ref max_dirty_idx))
                    {
                        continue;
                    }

                    bool is_top_solid = upper_solid_layer[cell_idx] > 0;
                    if(!is_top_solid)
                    {
                        var top_liquid = upper_layer[cell_idx];
                        var flow = remaining_liquid - CalculateVerticalFlowValue(remaining_liquid, top_liquid);
                        if (flow > MIN_FLOW)
                        {
                            flow *= FLOW_SPEED;
                        }

                        flow = Mathf.Max(flow, 0);
                        flow = Mathf.Min(MAX_FLOW, remaining_liquid);                            

                        if (flow != 0)
                        {
                            remaining_liquid -= flow;
                            delta_layer[cell_idx] -= flow;
                            upper_delta_layer[cell_idx] += flow;

                            min_dirty_idx = Vector3Int.Min(min_dirty_idx, new Vector3Int(x, layer_idx, z));
                            max_dirty_idx = Vector3Int.Max(max_dirty_idx, new Vector3Int(x, layer_idx, z));
                            min_lower_dirty_idx = Vector3Int.Min(min_lower_dirty_idx, new Vector3Int(x, layer_idx + 1, z));
                            max_lower_dirty_idx = Vector3Int.Max(max_lower_dirty_idx, new Vector3Int(x, layer_idx + 1, z));
                        }
                    }
                }
            }

            m_min_dirty_cell_per_layer[layer_idx] = Vector3Int.Min(min_dirty_idx, m_min_dirty_cell_per_layer[layer_idx]);
            m_max_dirty_cell_per_layer[layer_idx] = Vector3Int.Max(max_dirty_idx, m_max_dirty_cell_per_layer[layer_idx]);
            m_min_dirty_cell_per_layer[layer_idx - 1] = Vector3Int.Min(min_lower_dirty_idx, m_min_dirty_cell_per_layer[layer_idx - 1]);
            m_max_dirty_cell_per_layer[layer_idx - 1] = Vector3Int.Max(max_lower_dirty_idx, m_max_dirty_cell_per_layer[layer_idx - 1]);
            m_min_dirty_cell_per_layer[layer_idx + 1] = Vector3Int.Min(min_upper_dirty_idx, m_min_dirty_cell_per_layer[layer_idx + 1]);
            m_max_dirty_cell_per_layer[layer_idx + 1] = Vector3Int.Max(max_upper_dirty_idx, m_max_dirty_cell_per_layer[layer_idx + 1]);
        }

        for (int layer_idx = 0; layer_idx < m_delta_layers.Length; ++layer_idx)
        {
            var layer = m_layers[layer_idx];
            var delta_layer = m_delta_layers[layer_idx];

            for (int z = m_min_dirty_cell_per_layer[layer_idx].z; z < m_max_dirty_cell_per_layer[layer_idx].z; ++z)
            {
                for (int x = m_min_dirty_cell_per_layer[layer_idx].x; x < m_max_dirty_cell_per_layer[layer_idx].x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.z + x;

                    var liquid = layer[cell_idx] + delta_layer[cell_idx];
                    if (liquid < MIN_VALUE)
                    {
                        liquid = 0;
                    }

                    layer[cell_idx] = liquid;
                    delta_layer[cell_idx] = 0;
                }
            }
        }
    }

    public bool FlowAndTryToFinish(ref float remaining_liquid, int cell_idx, int target_cell_idx, float[] layer, float[] delta_layer, float[] solid_layer, int x, int layer_idx, int z, int target_x, int target_z, ref Vector3Int min_dirty_idx, ref Vector3Int max_dirty_idx)
    {
        bool is_target_solid = solid_layer[target_cell_idx] > 0;
        if (!is_target_solid)
        {
            var left_liquid = layer[target_cell_idx];
            var flow = (remaining_liquid - left_liquid) / 4f;
            if (flow > MIN_FLOW)
            {
                flow *= FLOW_SPEED;
            }

            flow = Mathf.Max(flow, 0);
            flow = Mathf.Min(remaining_liquid, Mathf.Min(MAX_FLOW, flow));

            if (flow != 0)
            {
                remaining_liquid -= flow;
                delta_layer[cell_idx] -= flow;
                delta_layer[target_cell_idx] += flow;

                min_dirty_idx = Vector3Int.Min(min_dirty_idx, new Vector3Int(x, layer_idx, z));
                max_dirty_idx = Vector3Int.Max(max_dirty_idx, new Vector3Int(x, layer_idx, z));
                min_dirty_idx = Vector3Int.Min(min_dirty_idx, new Vector3Int(target_x, layer_idx, target_z));
                max_dirty_idx = Vector3Int.Max(max_dirty_idx, new Vector3Int(target_x, layer_idx, target_z));

                if (remaining_liquid < MIN_VALUE)
                {
                    delta_layer[cell_idx] -= remaining_liquid;
                    return true;
                }
            }
        }
        return false;
    }

    public void Update(HashSet<Vector3Int> dirty_chunk_ids)
    {
        if (m_density_changes.Count > 0)
        {
            for (int layer_idx = 0; layer_idx < m_dimensions_in_cells.y; ++layer_idx)
            {
                var layer = m_layers[layer_idx];
                foreach (var density_change in m_density_changes)
                {
                    if (density_change.m_layer_idx != layer_idx) continue;

                    var pos = density_change.m_position;

                    var x = (int)(pos.x / (float)m_cell_size_in_meters.x);
                    if (x < 0 || x >= m_dimensions_in_cells.x) return;

                    var z = (int)(pos.z / (float)m_cell_size_in_meters.z);
                    if (z < 0 || z >= m_dimensions_in_cells.z) return;

                    var cell_idx = z * m_dimensions_in_cells.x + x;

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

        m_simulation_timer += Time.deltaTime;
        while (m_simulation_timer >= SIMULATION_TICK_RATE)
        {
            Profiler.BeginSample("UpdateSimulation");
            UpdateSimulation();
            Profiler.EndSample();
            m_simulation_timer -= SIMULATION_TICK_RATE;
        }

        for(int layer_idx = 0; layer_idx < m_layers.Length; ++layer_idx)
        {
            var min_dirty_cell_coordinates = m_min_dirty_cell_per_layer[layer_idx];
            var max_dirty_cell_coordinates = m_max_dirty_cell_per_layer[layer_idx];

            int min_chunk_grid_z = (int)((min_dirty_cell_coordinates.z - 1) * m_one_over_chunk_dimensions_in_cells);
            int max_chunk_grid_z = (int)((max_dirty_cell_coordinates.z + 1) * m_one_over_chunk_dimensions_in_cells);

            int min_chunk_grid_x = (int)((min_dirty_cell_coordinates.x - 1) * m_one_over_chunk_dimensions_in_cells);
            int max_chunk_grid_x = (int)((max_dirty_cell_coordinates.x + 1) * m_one_over_chunk_dimensions_in_cells);

            for(int chunk_grid_z = min_chunk_grid_z; chunk_grid_z <= max_chunk_grid_z; ++chunk_grid_z)
            {
                for (int chunk_grid_x = min_chunk_grid_x; chunk_grid_x <= max_chunk_grid_x; ++chunk_grid_x)
                {
                    dirty_chunk_ids.Add(new Vector3Int(chunk_grid_x, layer_idx, chunk_grid_z));
                }
            }
        }

        m_density_changes.Clear();
    }

    public float[][] GetLayers() { return m_layers; }

    float[][] m_layers;
    float[][] m_delta_layers;
    float[][] m_solid_layers;
    Vector3Int[] m_min_dirty_cell_per_layer;
    Vector3Int[] m_max_dirty_cell_per_layer;
    Vector3 m_cell_size_in_meters;

    Vector3Int m_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
    float m_one_over_chunk_dimensions_in_cells;
    List<DensityChange> m_density_changes = new List<DensityChange>();
    float m_simulation_timer;
}