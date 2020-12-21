using UnityEngine;
using System.Collections.Generic;
using System;

public class DirtyChunkTable
{
    Dictionary<Vector3Int, Chunk> m_chunks = new Dictionary<Vector3Int, Chunk>();

    public struct Chunk
    {
        public int m_min_x;
        public int m_min_z;
        public int m_max_x;
        public int m_max_z;
        public int m_layer_idx;

        public Chunk(int min_x, int min_z, int max_x, int max_z, int layer_idx)
        {
            m_min_x = min_x;
            m_min_z = min_z;
            m_max_x = max_x;
            m_max_z = max_z;
            m_layer_idx = layer_idx;
        }

        public void AddCell(int x, int z)
        {
            m_min_x = Math.Min(x, m_min_x);
            m_max_x = Math.Max(x, m_max_x);
            m_min_z = Math.Min(z, m_min_z);
            m_max_z = Math.Max(z, m_max_z);
        }
    }

    public DirtyChunkTable(Vector3Int grid_dimensions_in_cells, int chunk_dimensions_in_cells)
    {
        m_grid_dimensions_in_cells = grid_dimensions_in_cells;
        m_chunk_dimensions_in_cells = chunk_dimensions_in_cells;
    }


    public void Clear()
    {
        m_chunks.Clear();
    }

    public Dictionary<Vector3Int, Chunk> GetChunks() { return m_chunks; }
    public int Count { get => m_chunks.Count; }

    public void AddCell(int x, int layer_idx, int z)
    {
        if (x < 0 || x >= m_grid_dimensions_in_cells.x) return;
        if (layer_idx < 0 || layer_idx >= m_grid_dimensions_in_cells.y) return;
        if (z < 0 || z >= m_grid_dimensions_in_cells.z) return;

        var chunk_x = x / m_chunk_dimensions_in_cells;
        var chunk_z = z / m_chunk_dimensions_in_cells;
        var chunk_id = new Vector3Int(chunk_x, layer_idx, chunk_z);
        if(!m_chunks.TryGetValue(chunk_id, out var region))
        {
            region = new Chunk(x, z, x, z, layer_idx);            
        }
        else
        {
            region.AddCell(x, z);
        }

        m_chunks[chunk_id] = region;
    }

    Vector3Int m_grid_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
}