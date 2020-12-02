using UnityEngine;

public class VoxelWorld : MonoBehaviour 
{
    [SerializeField] VoxelLayer m_voxel_layer;

    void Awake()
    {
        Instantiate(m_voxel_layer);    
    }
}