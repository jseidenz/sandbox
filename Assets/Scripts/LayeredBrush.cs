using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LayeredBrush
{
    static int LAYER_IDX_ID = Shader.PropertyToID("_LayerIdx");
    static int HUE_VARIATION_AMOUNT_ID = Shader.PropertyToID("_HueVariationAmount");
    static int SATURATION_VARIATION_AMOUNT_ID = Shader.PropertyToID("_SaturationVariationAmount");
    static int COLOR_BLEND_ID = Shader.PropertyToID("_ColorBlend");
    static int COMPUTED_COLOR_ID = Shader.PropertyToID("_ComputedColor");

    static float[] HUE_VARIATION_PATTERN = new float[] { 0, -0.3f, 0.1f, 0.5f, -0.15f };
    static float[] SATURATION_VARIATION_PATTERN = new float[] { 0, -0.5f, 0.1f, -0.3f, -0.4f };

    public LayeredBrush(Material[] materials)
    {
        m_material_entries = new MaterialEntry[materials.Length];
        for(int i = 0; i < materials.Length; ++i)
        {
            m_material_entries[i] = new MaterialEntry
            {
                m_material = materials[i],
            };
        };

        RefreshLookupTable();
    }

    public void RefreshLookupTable()
    {
        bool is_dirty = m_layer_idx_to_material == null;
        int max_layer_idx = 0;
        for(int i = 0; i < m_material_entries.Length; ++i)
        {
            var entry = m_material_entries[i];
            var material = entry.m_material;

            var new_layer_idx = material.GetInt(LAYER_IDX_ID);
            var new_hue_variation_amount = material.GetFloat(HUE_VARIATION_AMOUNT_ID);
            var new_saturation_variation_amount = material.GetFloat(SATURATION_VARIATION_AMOUNT_ID);

            max_layer_idx = Math.Max(max_layer_idx, new_layer_idx);

            bool is_layer_idx_dirty = entry.m_layer_idx != new_layer_idx;
            bool is_hue_variation_amount_dirty = new_hue_variation_amount != entry.m_hue_variation_amount;
            bool is_saturation_variation_amount_dirty = new_saturation_variation_amount != entry.m_saturation_variation_amount;
            bool are_parameters_dirty = is_layer_idx_dirty || is_hue_variation_amount_dirty || is_saturation_variation_amount_dirty;

            if (!are_parameters_dirty) continue;

            is_dirty = true;
            entry.m_layer_idx = new_layer_idx;
            entry.m_hue_variation_amount = new_hue_variation_amount;
            entry.m_saturation_variation_amount = new_saturation_variation_amount;
            m_material_entries[i] = entry;
        }

        if (is_dirty)
        {
            Array.Sort(m_material_entries, (x, y) => x.m_layer_idx - y.m_layer_idx);
            m_layer_idx_to_material = new MaterialLayer[max_layer_idx + 1];

            int last_layer_idx = 0;
            int layer_idx = 0;
            foreach(var material_entry in m_material_entries)
            {
                float blend_range = Mathf.Max((float)material_entry.m_layer_idx - (float)last_layer_idx, 1f);
                for(; layer_idx <= material_entry.m_layer_idx; ++layer_idx)
                {
                    float color_blend = (layer_idx - last_layer_idx) / blend_range;
                    var property_block = new MaterialPropertyBlock();
                    property_block.SetFloat(COLOR_BLEND_ID, color_blend);
                    m_layer_idx_to_material[layer_idx] = new MaterialLayer
                    {
                        m_material = material_entry.m_material,
                        m_property_block = property_block
                    };
                }
                last_layer_idx = material_entry.m_layer_idx;
            }
        }
    }

    public static LayeredBrush LoadBrush(string resources_path)
    {
        var materials = Resources.LoadAll<Material>(resources_path);
        return new LayeredBrush(materials);
    }

    public Material GetMaterialForLayer(int layer_idx)
    {
        layer_idx = Math.Max(layer_idx, 0);
        layer_idx = Math.Min(layer_idx, m_layer_idx_to_material.Length - 1);
        return m_layer_idx_to_material[layer_idx].m_material;
    }

    struct MaterialEntry
    {
        public Material m_material;
        public int m_layer_idx;
        public float m_hue_variation_amount;
        public float m_saturation_variation_amount;
    }

    struct MaterialLayer
    {
        public Material m_material;
        public MaterialPropertyBlock m_property_block;
    }

    MaterialLayer[] m_layer_idx_to_material;
    MaterialEntry[] m_material_entries;
}