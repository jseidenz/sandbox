using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "LiquidTuning", menuName = "ScriptableObjects/LiquidTuning", order = 1)]
public class LiquidTuning : ScriptableObject
{
    public float m_min_mass_for_transfer_between_cells;
    public float m_min_mass_for_flow;
}