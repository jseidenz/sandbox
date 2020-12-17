using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "BevelTuning", menuName = "ScriptableObjects/BevelTuning", order = 1)]
public class BevelTuning : ScriptableObject
{
    public float m_extrusion_distance;
    public float m_extrusion_vertical_offset;
    public float m_extrusion_lower_vertical_offset;
    public float m_max_edge_seperation;
    public float m_bottom_extrusion_distance;
    public bool m_disable_dot_product_check;

    public float m_debug_horizontal_left_offset;
    public float m_debug_horizontal_right_offset;
    public void ApplyParameters(Material material)
    {

    }
}