#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;

class SimulationDebugger : EditorWindow
{
    [MenuItem("Tools/Simulation Debugger")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(SimulationDebugger));
    }

    public static DensityCell m_raycast_cell;
    bool m_is_rasycast_cell_expanded;
    bool m_is_debug_cell_expanded;

    void OnGUI()
    {
        if (!IsSimulationReady()) return;

        var solid_mesher = Game.Instance.GetSolidMesher();
        var liquid_mesher = Game.Instance.GetLiquidMesher();
        var liquid_simulation = Game.Instance.GetLiquidSimulation();
        var solid_simulation = Game.Instance.GetSolidSimulation();

        if (GUILayout.Button("Triangulate All Liquids"))
        {
            liquid_mesher.TriangulateAll();
        }

        if (GUILayout.Button("Step Liquids"))
        {
            liquid_simulation.StepOnce(false);
        }

        if (GUILayout.Button("Step Liquids(Force)"))
        {
            liquid_simulation.StepOnce(true);
        }

        bool is_liquid_simulation_enabled = GUILayout.Toggle(liquid_simulation.IsSimulationEnabled(), "Liquid Simulation");
        liquid_simulation.SetSimulationEnabled(is_liquid_simulation_enabled);

        if (GUILayout.Button("Triangulate Solids"))
        {
            solid_mesher.TriangulateAll();
        }

        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if(Physics.Raycast(ray, out var hit_info))
        {
            solid_simulation.TryGetDensityCellFromWorldPosition(hit_info.point, out m_raycast_cell);
        }

        ShowCellFoldout("Raycast Cell", m_raycast_cell, ref m_is_rasycast_cell_expanded, solid_simulation, liquid_simulation);
        ShowCellFoldout("Debug Cell(F2)", CellDebugger.s_debug_cell, ref m_is_debug_cell_expanded, solid_simulation, liquid_simulation);
    }

    void ShowCellFoldout(string cell_name, DensityCell cell, ref bool is_expanded, SolidSimulation solid_simulation, LiquidSimulation liquid_simulation)
    {
        if(is_expanded = EditorGUILayout.Foldout(is_expanded, cell_name))
        {
            EditorGUI.indentLevel += 2;

            EditorGUILayout.LabelField($"x={cell.m_x}, z={cell.m_z}, l={cell.m_layer_idx}");

            ShowCellLine("C", cell, solid_simulation, liquid_simulation);
            ShowCellLine("L", cell.Dx(-1), solid_simulation, liquid_simulation);
            ShowCellLine("R", cell.Dx(+1), solid_simulation, liquid_simulation);
            ShowCellLine("N", cell.Dz(-1), solid_simulation, liquid_simulation);
            ShowCellLine("F", cell.Dz(+1), solid_simulation, liquid_simulation);
            ShowCellLine("B", cell.Dl(-1), solid_simulation, liquid_simulation);
            ShowCellLine("A", cell.Dl(+1), solid_simulation, liquid_simulation);

            EditorGUI.indentLevel -= 2;
        }
    }

    void ShowCellLine(string cell_name, DensityCell cell, SolidSimulation solid_simulation, LiquidSimulation liquid_simulation)
    {
        bool is_valid = solid_simulation.TryGetDensity(cell, out var solid_density);
        liquid_simulation.TryGetDensity(cell, out var liquid_density);
        string text = $"{cell_name}: sd={solid_density}, ld={liquid_density}";

        EditorGUILayout.BeginHorizontal();
        if (is_valid)
        {
            EditorGUILayout.LabelField(text);
            if (GUILayout.Button("->"))
            {
                CellDebugger.s_debug_cell = cell;
            }
        }
        else
        {
            EditorGUILayout.LabelField($"{cell_name}: INVALID");
        }
        EditorGUILayout.EndHorizontal();
    }

    bool IsSimulationReady()
    {
        return Game.Instance != null && Game.Instance.GetLiquidSimulation() != null;
    }

    void Update()
    {
        if (!IsSimulationReady()) return;

        Repaint();
    }
}
#endif