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
        if (GUILayout.Button("Create Client"))
        {

        }      
    }

    void Update()
    {
        bool is_simulation_ready = Game.Instance != null && Game.Instance.GetLiquidSimulation() != null;
        if (!is_simulation_ready) return;

        Repaint();
    }
}