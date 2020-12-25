using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class ChunkVisibilityMatrix
{
    public ChunkVisibilityMatrix(Vector3 cell_dimensions_in_meters, Vector3Int grid_dimensions_in_cells, int chunk_dimensions_in_cells)
    {
        m_cell_dimensions_in_meters = cell_dimensions_in_meters;
        m_grid_dimensions_in_cells = grid_dimensions_in_cells;
        m_chunk_dimensions_in_cells = chunk_dimensions_in_cells;
    }

    public void Update(Camera camera, Vector3 camera_pos, Vector3[] frustum_corners, Plane[] frustum_planes)
    {

    }

    Vector3 m_cell_dimensions_in_meters;
    Vector3Int m_grid_dimensions_in_cells;
    int m_chunk_dimensions_in_cells;
}