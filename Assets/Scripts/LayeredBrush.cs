using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public abstract class LayeredBrush
{
    public abstract void GetMaterialForLayer(int layer_idx, out Material material);
}