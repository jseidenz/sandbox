using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections.Generic;

public class Game : MonoBehaviour 
{
    [SerializeField] VoxelWorld m_voxel_world;
    [SerializeField] GameObject m_player_avatar;
    [SerializeField] Image m_initial_black;
    [SerializeField] int m_grid_width_in_voxels;
    [SerializeField] int m_grid_depth_in_voxels;
    [SerializeField] int m_grid_height_in_voxels;
    [SerializeField] float m_voxel_size_in_meters;
    [SerializeField] int m_voxel_chunk_dimensions;
    [SerializeField] float m_ground_plane_size;

    LiquidSimulation m_liquid_simulation;
    SolidSimulation m_solid_simulation;

    HashSet<Vector3Int> m_dirty_chunk_ids = new HashSet<Vector3Int>();
    GameObject m_ground_plane;

    public static Game Instance;

    async void Awake()
    {
        m_solid_simulation = new SolidSimulation(new Vector3Int(m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels), m_voxel_size_in_meters, m_voxel_chunk_dimensions);

        var layers = m_solid_simulation.GetLayers();

        m_voxel_world = await CreateVoxelWorld(layers);
        m_liquid_simulation = new LiquidSimulation();
        

        m_player_avatar = await CreateAvatar();
        m_voxel_world.BindCamera(Camera.main);

        CreateGroundPlane();

        Instance = this;
    }

    void Start()
    {
        ScreenFader.StartScreenFade(m_initial_black.gameObject, false, 5f, 0.25f, () => m_initial_black.gameObject.SetActive(false));
    }

    async Task<VoxelWorld> CreateVoxelWorld(float[][] layers)
    {
        var voxel_world = GameObject.Instantiate(m_voxel_world);
        voxel_world.m_tuneables = m_voxel_world.m_tuneables;
        voxel_world.Init(layers, m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels, m_voxel_size_in_meters, m_voxel_chunk_dimensions);


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
        
        m_solid_simulation.ApplyHeightMap(densities);
        voxel_world.TriangulateAll();

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

    public LiquidSimulation GetLiquidSimulation()
    {
        return m_liquid_simulation;
    }

    public SolidSimulation GetSolidSimulation()
    {
        return m_solid_simulation;
    }

    void Update()
    {
        m_dirty_chunk_ids.Clear();
        m_solid_simulation.Update(m_dirty_chunk_ids);
        m_voxel_world.Triangulate(m_dirty_chunk_ids);
    }

    void CreateGroundPlane()
    {
        m_ground_plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        m_ground_plane.name = "GroundPlane";

        var ground_plane_material = GameObject.Instantiate(m_voxel_world.GetMaterial());
        ground_plane_material.color = m_voxel_world.GetColorForLayer(0);

        var ground_plane_mesh_renderer = m_ground_plane.GetComponent<MeshRenderer>();
        ground_plane_mesh_renderer.sharedMaterial = ground_plane_material;
        ground_plane_mesh_renderer.receiveShadows = false;
        m_ground_plane.transform.localScale = new Vector3(m_ground_plane_size, 1, m_ground_plane_size);
        m_ground_plane.transform.localPosition = new Vector3(0, -0.5f, 0);
    }
}