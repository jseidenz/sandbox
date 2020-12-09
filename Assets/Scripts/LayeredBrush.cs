using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.Profiling;

public class LayeredBrush
{
    static int LAYER_IDX_ID = Shader.PropertyToID("_LayerIdx");
    static int HUE_VARIATION_AMOUNT_ID = Shader.PropertyToID("_HueVariationAmount");
    static int SATURATION_VARIATION_AMOUNT_ID = Shader.PropertyToID("_SaturationVariationAmount");
    static int COMPUTED_COLOR_ID = Shader.PropertyToID("_ComputedColor");
    static int BOTTOM_COLOR_ID = Shader.PropertyToID("_BottomColor");
    static int TOP_COLOR_ID = Shader.PropertyToID("_TopColor");

    static float[] HUE_VARIATION_PATTERN = new float[] { 0, -0.3f, 0.1f, 0.5f, -0.15f, 0.25f };
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
            var new_bottom_color = material.GetColor(BOTTOM_COLOR_ID);
            var new_top_color = material.GetColor(TOP_COLOR_ID);

            max_layer_idx = Math.Max(max_layer_idx, new_layer_idx);

            bool is_layer_idx_dirty = entry.m_layer_idx != new_layer_idx;
            bool is_hue_variation_amount_dirty = new_hue_variation_amount != entry.m_hue_variation_amount;
            bool is_saturation_variation_amount_dirty = new_saturation_variation_amount != entry.m_saturation_variation_amount;
            bool is_bottom_color_dirty = new_bottom_color != entry.m_bottom_color;
            bool is_top_color_dirty = new_top_color != entry.m_top_color;
            bool are_parameters_dirty = is_layer_idx_dirty || is_hue_variation_amount_dirty || is_saturation_variation_amount_dirty || is_bottom_color_dirty || is_top_color_dirty;

            if (!are_parameters_dirty) continue;

            is_dirty = true;
            entry.m_layer_idx = new_layer_idx;
            entry.m_hue_variation_amount = new_hue_variation_amount;
            entry.m_saturation_variation_amount = new_saturation_variation_amount;
            entry.m_bottom_color = new_bottom_color;
            entry.m_top_color = new_top_color;
            m_material_entries[i] = entry;
        }

        if (is_dirty)
        {
            Array.Sort(m_material_entries, (x, y) => x.m_layer_idx - y.m_layer_idx);
            m_layer_idx_to_material = new MaterialLayer[max_layer_idx + 1];

            int last_layer_idx = 0;
            int layer_idx = 0;
            foreach(var entry in m_material_entries)
            {
                float blend_range = Mathf.Max((float)entry.m_layer_idx - (float)last_layer_idx, 1f);
                for(; layer_idx <= entry.m_layer_idx; ++layer_idx)
                {
                    float color_blend_factor = (layer_idx - last_layer_idx) / blend_range;
                    var property_block = new MaterialPropertyBlock();

                    var computed_color = Color.Lerp(entry.m_bottom_color, entry.m_top_color, color_blend_factor);
                    Color.RGBToHSV(computed_color, out var hue, out var saturation, out var value);

                    if (entry.m_hue_variation_amount > 0)
                    {
                        float hue_variation = HUE_VARIATION_PATTERN[layer_idx % HUE_VARIATION_PATTERN.Length];
                        hue += hue_variation * entry.m_hue_variation_amount;
                    }
                    if(entry.m_saturation_variation_amount > 0)
                    {
                        float saturation_variation = SATURATION_VARIATION_PATTERN[layer_idx % SATURATION_VARIATION_PATTERN.Length];
                        saturation += saturation_variation * entry.m_saturation_variation_amount;
                    }

                    computed_color = Color.HSVToRGB(hue, saturation, value);
                    property_block.SetColor(COMPUTED_COLOR_ID, computed_color);

                    m_layer_idx_to_material[layer_idx] = new MaterialLayer
                    {
                        m_material = entry.m_material,
                        m_property_block = property_block
                    };
                }
                last_layer_idx = entry.m_layer_idx;
            }
        }
    }

    public static LayeredBrush LoadBrush(string resources_path)
    {
        var materials = Resources.LoadAll<Material>(resources_path);
        return new LayeredBrush(materials);
    }

    public void GetMaterialForLayer(int layer_idx, out Material material, out MaterialPropertyBlock property_block)
    {
        layer_idx = Math.Max(layer_idx, 0);
        layer_idx = Math.Min(layer_idx, m_layer_idx_to_material.Length - 1);
        var material_layer = m_layer_idx_to_material[layer_idx];
        material = material_layer.m_material;
        property_block = material_layer.m_property_block;
    }

    struct MaterialEntry
    {
        public Material m_material;
        public int m_layer_idx;
        public float m_hue_variation_amount;
        public float m_saturation_variation_amount;
        public Color m_bottom_color;
        public Color m_top_color;
    }

    struct MaterialLayer
    {
        public Material m_material;
        public MaterialPropertyBlock m_property_block;
    }

    MaterialLayer[] m_layer_idx_to_material;
    MaterialEntry[] m_material_entries;
}