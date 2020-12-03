﻿using UnityEngine;
using System.Threading.Tasks;

public class Game : MonoBehaviour 
{
    [SerializeField] VoxelWorld m_voxel_world;
    [SerializeField] GameObject m_player_avatar;

    async void Awake()
    {
        m_voxel_world = await CreateVoxelWorld();
        m_player_avatar = await CreateAvatar();
    }

    async Task<VoxelWorld> CreateVoxelWorld()
    {
        return GameObject.Instantiate(m_voxel_world);
    }

    async Task<GameObject> CreateAvatar()
    {
        return GameObject.Instantiate(m_player_avatar);
    }
}