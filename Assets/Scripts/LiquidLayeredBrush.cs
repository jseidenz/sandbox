using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LiquidLayeredBrush : LayeredBrush
{
    public LiquidLayeredBrush(Material material)
    {
        SetMaterial(material);
        m_material = material;
    }

    public override void GetMaterialForLayer(int layer_idx, out Material material)
    {
        material = m_material;
    }

    public void SetMaterial(Material material)
    {
        m_material = material;
    }

    Material m_material;
}