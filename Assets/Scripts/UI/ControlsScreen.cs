using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class ControlsScreen : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI m_text;

    bool m_is_screen_faded;

    void Awake()
    {
        m_text.text = 
            "<b>Controls:</b>\n" +
            "WASD - Move\n" +
            "Space - Jump\n" +
            "Shift - Run\n" +
            "Q - Flood\n" +
            "Left Click - Dig\n" +
            "Right Click - Fill\n";
    }
}