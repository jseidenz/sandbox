using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LiquidSimulation
{
    const float MAX_VALUE = 1.0f;
    const float MIN_VALUE = 0.005f;
    const float MAX_COMPRESSION = 0.25f;
    const float MIN_FLOW = 0.005f;
    const float MAX_FLOW = 4f;
    const float FLOW_SPEED = 1f;

    const float SIMULATION_TICK_RATE = 1f / 30f;
    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public LiquidSimulation(Vector3Int dimensions_in_cells, float cell_size_in_meters, int chunk_dimensions_in_cells, float[][] solid_layers)
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
    }

    public void AddDensity(Vector3 world_pos, float amount)
    {
        var layer_idx = (int)(world_pos.y / m_cell_size_in_meters);
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

            for(int z = 1; z < m_dimensions_in_cells.z - 1; ++z)
            {
                for(int x = 1; x < m_dimensions_in_cells.x - 1; ++x)
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

                            if(remaining_liquid < MIN_VALUE)
                            {
                                delta_layer[cell_idx] -= remaining_liquid;
                                continue;
                            }
                        }
                    }


                    if(FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx - 1, layer, delta_layer, solid_layer))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx + 1, layer, delta_layer, solid_layer))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx - m_dimensions_in_cells.x, layer, delta_layer, solid_layer))
                    {
                        continue;
                    }

                    if (FlowAndTryToFinish(ref remaining_liquid, cell_idx, cell_idx + m_dimensions_in_cells.x, layer, delta_layer, solid_layer))
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
                        }
                    }

                }
            }
        }

        for (int y = 0; y < m_delta_layers.Length; ++y)
        {
            var layer = m_layers[y];
            var delta_layer = m_delta_layers[y];
            for(int i = 0; i < delta_layer.Length; ++i)
            {
                var liquid = layer[i] + delta_layer[i];
                if(liquid < MIN_VALUE)
                {
                    liquid = 0;
                }

                layer[i] = liquid;
                delta_layer[i] = 0;
            }
        }
    }

    public bool FlowAndTryToFinish(ref float remaining_liquid, int cell_idx, int target_cell_idx, float[] layer, float[] delta_layer, float[] solid_layer)
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
            flow = Mathf.Min(MAX_FLOW, remaining_liquid);

            if (flow != 0)
            {
                remaining_liquid -= flow;
                delta_layer[cell_idx] -= flow;
                delta_layer[target_cell_idx] += flow;

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
        for (int y = 0; y < m_dimensions_in_cells.y; ++y)
        {
            var layer = m_layers[y];
            foreach (var density_change in m_density_changes)
            {
                if (density_change.m_layer_idx != y) continue;

                var pos = density_change.m_position;

                var x = (int)(pos.x / (float)m_cell_size_in_meters);
                if (x < 0 || x >= m_dimensions_in_cells.x) return;

                var z = (int)(pos.z / (float)m_cell_size_in_meters);
                if (z < 0 || z >= m_dimensions_in_cells.z) return;

                var cell_idx = z * m_dimensions_in_cells.x + x;

                var previous_density = layer[cell_idx];
                var new_density = Mathf.Clamp01(previous_density + density_change.m_amount);
                if (new_density != previous_density)
                {
                    layer[cell_idx] = new_density;

                    for (int j = -1; j <= 1; ++j)
                    {
                        for (int i = -1; i <= 1; ++i)
                        {
                            var offset_x = Math.Min(Math.Max(x + i, 0), m_dimensions_in_cells.x - 1);
                            var offset_z = Math.Min(Math.Max(z + j, 0), m_dimensions_in_cells.z - 1);

                            var chunk_grid_x = (int)(offset_x * m_one_over_chunk_dimensions_in_cells);
                            var chunk_grid_z = (int)(offset_z * m_one_over_chunk_dimensions_in_cells);

                            dirty_chunk_ids.Add(new Vector3Int(chunk_grid_x, y, chunk_grid_z));
                        }
                    }
                }
            }
        }

        m_simulation_timer += Time.deltaTime;
        while (m_simulation_timer >= SIMULATION_TICK_RATE)
        {
            Profiler.BeginSample("UpdateSimulation");
            //UpdateSimulation();
            Profiler.EndSample();
            m_simulation_timer -= SIMULATION_TICK_RATE;
        }

        m_density_changes.Clear();
    }

    public void ApplyHeightMap(float[] densities)
    {
        for (int y = 0; y < m_layers.Length; ++y)
        {
            var layer = m_layers[y];

            for (int z = 0; z < m_dimensions_in_cells.z; ++z)
            {
                for (int x = 0; x < m_dimensions_in_cells.x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.x + x;

                    layer[cell_idx] = densities[cell_idx];
                }
            }
        }
    }

    public float[][] GetLayers() { return m_layers; }

    float[][] m_layers;
    float[][] m_delta_layers;
    float[][] m_solid_layers;
    float m_cell_size_in_meters;

    Vector3Int m_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
    float m_one_over_chunk_dimensions_in_cells;
    List<DensityChange> m_density_changes = new List<DensityChange>();
    float m_simulation_timer;
}