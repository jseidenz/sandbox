using UnityEngine;
using System.Collections.Generic;
using System;

public class CellDebugger : MonoBehaviour
{
    [SerializeField] GameObject m_debug_cell_visualizer_prefab;
    
    GameObject m_debug_cell_visualizer;

    public static DensityCell s_debug_cell;

#if UNITY_EDITOR
    void Awake()
    {
        m_debug_cell_visualizer = GameObject.Instantiate(m_debug_cell_visualizer_prefab);
    }

    void Update()
    {
        if (Game.Instance == null) return;

        m_debug_cell_visualizer.transform.position = s_debug_cell.ToWorldPositionCenter(Game.Instance.GetCellSizeInMeters());
    }
#endif

}