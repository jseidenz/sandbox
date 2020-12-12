using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class WorldGenerator
{
    public WorldGenerator(SolidSimulation solid_simulation, Mesher solid_mesher)
    {
        m_solid_simulation = solid_simulation;
        m_solid_mesher = solid_mesher;
        m_dimensions_in_cells = solid_simulation.GetDimensionsInCells();


        Profiler.BeginSample("GenerateHeightMap");
        m_height_map = new HeightMapGenerator().GenerateHeightMap(m_dimensions_in_cells.x, m_dimensions_in_cells.z, 4f);
        Profiler.EndSample();

        Profiler.BeginSample("ApplyHeightMap");
        m_solid_simulation.ApplyHeightMap(m_height_map);
        Profiler.EndSample();

        Profiler.BeginSample("TriangulateAll");
        m_solid_mesher.TriangulateAll();
        Profiler.EndSample();
    }

    public bool Update()
    {
        return true;
    }

    SolidSimulation m_solid_simulation;
    Mesher m_solid_mesher;
    Vector3Int m_dimensions_in_cells;
    float[] m_height_map;
}