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

        ShowCellFoldout("Raycast Cell", m_raycast_cell, ref m_is_rasycast_cell_expanded, false, solid_simulation, liquid_simulation);
        ShowCellFoldout("Debug Cell(F2)", CellDebugger.s_debug_cell, ref m_is_debug_cell_expanded, true, solid_simulation, liquid_simulation);
    }

    void ShowCellFoldout(string cell_name, DensityCell cell, ref bool is_expanded, bool allow_editing, SolidSimulation solid_simulation, LiquidSimulation liquid_simulation)
    {
        if(is_expanded = EditorGUILayout.Foldout(is_expanded, cell_name))
        {
            EditorGUI.indentLevel += 2;

            EditorGUILayout.LabelField($"x={cell.m_x}, z={cell.m_z}, l={cell.m_layer_idx}");

            ShowCellLine("C", cell, false, solid_simulation, liquid_simulation);

            EditorGUI.indentLevel -= 2;
        }
    }

    void ShowCellLine(string cell_name, DensityCell cell, bool allow_editing, SolidSimulation solid_simulation, LiquidSimulation liquid_simulation)
    {
        solid_simulation.TryGetDensity(cell, out var density);
        string text = $"{cell_name}: sd={density}";
        EditorGUILayout.LabelField(text);
    }

    void Update()
    {
        bool is_simulation_ready = Game.Instance != null && Game.Instance.GetLiquidSimulation() != null;
        if (!is_simulation_ready) return;

        Repaint();
    }
}
#endif