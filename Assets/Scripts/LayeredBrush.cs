using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LayeredBrush
{
    static int LAYER_IDX_ID = Shader.PropertyToID("_LayerIdx");

    public LayeredBrush(Material[] materials)
    {
        m_material_entries = new MaterialEntry[materials.Length];
        for(int i = 0; i < materials.Length; ++i)
        {
            m_material_entries[i] = new MaterialEntry
            {
                m_material = materials[i],
                m_layer_idx = materials[i].GetInt(LAYER_IDX_ID)
            };
        };

        RefreshLookupTable();
    }

    public void RefreshLookupTable()
    {
        bool is_dirty = false;
        int max_layer_idx = 0;
        for(int i = 0; i < m_material_entries.Length; ++i)
        {
            var entry = m_material_entries[i];
            var new_layer_idx = entry.m_material.GetInt(LAYER_IDX_ID);
            max_layer_idx = Math.Max(max_layer_idx, new_layer_idx);

            if (entry.m_layer_idx == new_layer_idx) continue;

            is_dirty = true;
            entry.m_layer_idx = new_layer_idx;
            m_material_entries[i] = entry;
        }

        if (is_dirty)
        {
            Array.Sort(m_material_entries, (x, y) => x.m_layer_idx - y.m_layer_idx);
            m_layer_lookup = new Material[max_layer_idx + 1];

            int layer_idx = 0;
            foreach(var material_entry in m_material_entries)
            {
                for(; layer_idx <= material_entry.m_layer_idx; ++layer_idx)
                {
                    m_layer_lookup[layer_idx] = material_entry.m_material;
                }
            }
        }
    }

    public static LayeredBrush LoadBrush(string resources_path)
    {
        var materials = Resources.LoadAll<Material>(resources_path);
        return new LayeredBrush(materials);
    }

    struct MaterialEntry
    {
        public Material m_material;
        public int m_layer_idx;
    }

    Material[] m_layer_lookup;
    MaterialEntry[] m_material_entries;
}