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

    void OnGUI()
    {
        var solid_mesher = Game.Instance.GetSolidMesher();
        var liquid_mesher = Game.Instance.GetLiquidMesher();
        var liquid_simulation = Game.Instance.GetLiquidSimulation();

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
    }

    void Update()
    {
        bool is_simulation_ready = Game.Instance != null && Game.Instance.GetLiquidSimulation() != null;
        if (!is_simulation_ready) return;

        Repaint();
    }
}