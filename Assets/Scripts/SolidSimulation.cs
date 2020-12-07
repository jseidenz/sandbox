using UnityEngine;
using System.Collections.Generic;

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
    }

    public void AddDensity(Vector3 world_pos, float amount)
    {
        var layer_idx = (int)(world_pos.y / m_cell_size_in_meters);
        if (layer_idx < 0 || layer_idx >= m_layers.Length) return;

        m_density_changes.Add(new DensityChange { m_position = world_pos, m_layer_idx = layer_idx, m_amount = amount });
    }

    public void Update(HashSet<Vector3Int> dirty_chunk_indices)
    {
        for(int y = 0; y < m_dimensions_in_cells.y; ++y)
        {
            var layer = m_layers[y];
            foreach(var density_change in m_density_changes)
            {

            }
        }
    }

    float[][] m_layers;
    float m_cell_size_in_meters;

    Vector3Int m_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
    List<DensityChange> m_density_changes = new List<DensityChange>();
}