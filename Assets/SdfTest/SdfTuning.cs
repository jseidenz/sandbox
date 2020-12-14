using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "SdfTuning", menuName = "ScriptableObjects/SdfTuning", order = 1)]
public class SdfTuning : ScriptableObject
{
    public float m_min_distance;
    public float m_step_size;
    public float m_cell_radius;

    public void ApplyParameters(Material material)
    {
        material.SetFloat("m_min_distance", m_min_distance);
        material.SetFloat("m_step_size", m_step_size);
        material.SetFloat("m_cell_radius", m_cell_radius);
    }
}