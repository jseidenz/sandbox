using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;

public class Game : MonoBehaviour 
{
    [SerializeField] VoxelWorld m_voxel_world;
    [SerializeField] GameObject m_player_avatar;
    [SerializeField] Image m_initial_black;
    [SerializeField] int m_grid_width_in_voxels;
    [SerializeField] int m_grid_depth_in_voxels;
    [SerializeField] int m_grid_height_in_voxels;

    public static Game Instance;

    async void Awake()
    {
        m_voxel_world = await CreateVoxelWorld();
        m_player_avatar = await CreateAvatar();
        m_voxel_world.BindCamera(Camera.main);

        Instance = this;
    }

    void Start()
    {
        ScreenFader.StartScreenFade(m_initial_black.gameObject, false, 5f, 0.25f, () => m_initial_black.gameObject.SetActive(false));
    }

    async Task<VoxelWorld> CreateVoxelWorld()
    {
        var voxel_world = GameObject.Instantiate(m_voxel_world);
        voxel_world.m_tuneables = m_voxel_world.m_tuneables;
        voxel_world.Init(m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels);


        var height_map_tex = Resources.Load<Texture2D>("heightmap");
        var pixels = height_map_tex.GetPixels();
        var height_map_width = height_map_tex.width;

        Resources.UnloadAsset(height_map_tex);

        var densities = new float[m_grid_width_in_voxels * m_grid_depth_in_voxels];

        for (int y = 0; y < m_grid_depth_in_voxels; ++y)
        {
            for (int x = 0; x < m_grid_width_in_voxels; ++x)
            {
                var density_idx = y * m_grid_width_in_voxels + x;
                var pixel_idx = y * height_map_width + x;
                var density = pixels[pixel_idx].r;

                densities[density_idx] = density;
            }
        }


        float cell_height_in_color_space = 1f / m_grid_height_in_voxels;
        voxel_world.ApplyHeightMap(densities, cell_height_in_color_space);

        return voxel_world;
    }

    async Task<GameObject> CreateAvatar()
    {
        return GameObject.Instantiate(m_player_avatar);
    }

    public VoxelWorld GetVoxelWorld()
    {
        return m_voxel_world;
    }
}