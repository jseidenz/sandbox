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
    public int m_subdivision_count;
    public void ApplyParameters(Material material)
    {

    }
}