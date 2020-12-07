using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;

public class Game : MonoBehaviour 
{
    [SerializeField] VoxelWorld m_voxel_world;
    [SerializeField] GameObject m_player_avatar;
    [SerializeField] Image m_initial_black;

    async void Awake()
    {
        m_voxel_world = await CreateVoxelWorld();
        m_player_avatar = await CreateAvatar();
        m_voxel_world.BindCamera(Camera.main);

    }

    void Start()
    {
        ScreenFader.StartScreenFade(m_initial_black.gameObject, false, 5f, 0.25f, () => m_initial_black.gameObject.SetActive(false));
    }

    async Task<VoxelWorld> CreateVoxelWorld()
    {
        var voxel_world = GameObject.Instantiate(m_voxel_world);
        voxel_world.m_tuneables = m_voxel_world.m_tuneables;
        return voxel_world;
    }

    async Task<GameObject> CreateAvatar()
    {
        return GameObject.Instantiate(m_player_avatar);
    }
}