using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class DensityField
{
    public DensityField(float[][] layers, int layer_width, int layer_height)
    {
        m_layer_width = layer_width;
        m_layer_height = layer_height;
        m_layers = layers;
    }

    public void SetDensity(int x, int y, int layer_idx, float density)
    {
        m_layers[layer_idx][y * m_layer_width + x] = density;
    }

    public void Line(int x0, int y0, int x1, int y1, int layer_idx, float density)
    {
        for (int y = y0; y <= y1; ++y)
        {
            for (int x = x0; x <= x1; ++x)
            {
                SetDensity(x, y, layer_idx, density);
            }
        }
        
    }

    int m_layer_width;
    int m_layer_height;
    float[][] m_layers;
}