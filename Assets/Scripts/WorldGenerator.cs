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

        m_solid_mesher.SetCollisionGenerationEnabled(false);
        m_solid_mesher.UpdateOcclusion();

        m_layer_idx = 0;
    }

    bool FadingIn()
    {
        m_solid_mesher.TriangulateLayer(m_layer_idx, true);

        m_layer_idx++;

        return m_layer_idx == m_dimensions_in_cells.y;
    }

    bool Finalizing()
    {
        m_solid_mesher.TriangulateLayer(m_layer_idx, false);

        m_layer_idx--;

        return m_layer_idx < 0;
    }

    public bool Update()
    {
        switch(m_state)
        {
            case State.FadingIn:
                if(FadingIn())
                {
                    m_state = State.Finalizing;
                    m_solid_mesher.SetCollisionGenerationEnabled(true);
                    m_layer_idx = m_dimensions_in_cells.y - 1;
                }

                break;

            case State.Finalizing:
                if(Finalizing())
                {
                    return true;
                }
                break;
        }

        return false;
    }

    enum State
    {
        FadingIn,
        Finalizing
    }

    State m_state;
    int m_layer_idx;
    SolidSimulation m_solid_simulation;
    Mesher m_solid_mesher;
    Vector3Int m_dimensions_in_cells;
    float[] m_height_map;
}