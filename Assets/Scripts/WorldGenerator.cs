using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator
{
    public WorldGenerator(SolidSimulation solid_simulation, Mesher solid_mesher)
    {
        m_dimensions_in_cells = solid_simulation.GetDimensionsInCells();
        m_height_map = new HeightMapGenerator().GenerateHeightMap(m_dimensions_in_cells.x, m_dimensions_in_cells.z, 4f);
    }

    public bool Update()
    {
        return true;
    }

    Vector3Int m_dimensions_in_cells;
    float[] m_height_map;
}