using UnityEngine;
using System.Collections.Generic;
using System;

public class SolidSimulation
{
    struct DensityChange
    {
        public Vector3 m_position;
        public int m_layer_idx;
        public float m_amount;
    }

    public SolidSimulation(Vector3Int dimensions_in_cells, float cell_size_in_meters, int chunk_dimensions_in_cells)
    {
        m_cell_size_in_meters = cell_size_in_meters;
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
        var layer_idx = (int)(world_pos.y / m_cell_size_in_meters);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_density_changes.Add(new DensityChange { m_position = world_pos, m_layer_idx = layer_idx, m_amount = amount });
    }

    public void Update(HashSet<Vector3Int> dirty_chunk_ids)
    {
        for(int y = 0; y < m_dimensions_in_cells.y; ++y)
        {
            var layer = m_layers[y];
            foreach(var density_change in m_density_changes)
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
                if(new_density != previous_density)
                {
                    layer[cell_idx] = new_density;

                    for (int i = -1; i <= 1; ++i)
                    {
                        for (int j = -1; j <= 1; ++j)
                        {
                            var offset_x = Math.Min(Math.Max(x + i, 0), m_dimensions_in_cells.x - 1);
                            var offset_z = Math.Min(Math.Max(z + j, 0), m_dimensions_in_cells.z - 1);

                            var chunk_grid_x = (int)(offset_x * m_one_over_chunk_dimensions_in_cells);
                            var chunk_grid_y = (int)(offset_z * m_one_over_chunk_dimensions_in_cells);

                            dirty_chunk_ids.Add(new Vector3Int(offset_x, y, offset_z));
                        }
                    }
                }
            }
        }

        m_density_changes.Clear();
    }

    public void ApplyHeightMap(float[] densities)
    {
        for (int y = 0; y < m_layers.Length; ++y)
        {
            var layer = m_layers[y];

            for(int z = 0; z < m_dimensions_in_cells.z; ++z)
            {
                for(int x = 0; x < m_dimensions_in_cells.x; ++x)
                {
                    var cell_idx = z * m_dimensions_in_cells.x + x;

                    layer[cell_idx] = densities[cell_idx];
                }
            }
        }
    }

    float[][] m_layers;
    float m_cell_size_in_meters;

    Vector3Int m_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
    float m_one_over_chunk_dimensions_in_cells;
    List<DensityChange> m_density_changes = new List<DensityChange>();
}