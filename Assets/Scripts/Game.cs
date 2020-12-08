using UnityEngine;
using System.Threading.Tasks;
using UnityEngine.UI;
using System.Collections.Generic;

public class Game : MonoBehaviour 
{
    [SerializeField] VoxelWorld m_solid_mesher;
    [SerializeField] VoxelWorld m_liquid_mesher;
    [SerializeField] GameObject m_player_avatar;
    [SerializeField] Image m_initial_black;
    [SerializeField] int m_grid_width_in_voxels;
    [SerializeField] int m_grid_depth_in_voxels;
    [SerializeField] int m_grid_height_in_voxels;
    [SerializeField] Vector3 m_voxel_size_in_meters;
    [SerializeField] int m_voxel_chunk_dimensions;
    [SerializeField] float m_ground_plane_size;
    [SerializeField] float m_water_height;
    [SerializeField] GameObject m_water;
    [SerializeField] float m_solid_iso_level;
    [SerializeField] float m_liquid_iso_level;
    [SerializeField] bool m_use_height_map;
    [SerializeField] bool m_liquid_sim_enabled_on_startup;
    [SerializeField] public float m_min_density_to_allow_flow;

    LiquidSimulation m_liquid_simulation;
    SolidSimulation m_solid_simulation;

    HashSet<Vector3Int> m_dirty_chunk_ids = new HashSet<Vector3Int>();
    GameObject m_ground_plane;

    public static Game Instance;

    void Awake()
    {
        Application.targetFrameRate = -1;
        m_solid_simulation = new SolidSimulation(new Vector3Int(m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels), m_voxel_size_in_meters, m_voxel_chunk_dimensions);
        var solid_layers = m_solid_simulation.GetLayers();

        m_liquid_simulation = new LiquidSimulation(new Vector3Int(m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels), m_voxel_size_in_meters, m_voxel_chunk_dimensions, solid_layers, m_solid_iso_level, m_min_density_to_allow_flow);
        var liquid_layers = m_liquid_simulation.GetLayers();
        m_liquid_simulation.SetSimulationEnabled(m_liquid_sim_enabled_on_startup);

        GenerateWorld(solid_layers, liquid_layers);

        m_solid_mesher = CreateSolidMesher(solid_layers);
        m_liquid_mesher = CreateLiquidMesher(m_liquid_simulation.GetLayers());

        m_solid_mesher.TriangulateAll();
        m_liquid_mesher.TriangulateAll();


        m_player_avatar = CreateAvatar();

        var camera = Camera.main;
        m_solid_mesher.BindCamera(camera);
        m_liquid_mesher.BindCamera(camera);

        CreateGroundPlane();

        m_water = GameObject.Instantiate(m_water);

        m_water.transform.position = new Vector3(0, m_water_height, 0);

        Instance = this;
    }

    void Start()
    {
        ScreenFader.StartScreenFade(m_initial_black.gameObject, false, 5f, 0.25f, () => m_initial_black.gameObject.SetActive(false));
    }

    VoxelWorld CreateSolidMesher(float[][] layers)
    {
        var solid_mesher = GameObject.Instantiate(m_solid_mesher);
        solid_mesher.m_tuneables = m_solid_mesher.m_tuneables;
        solid_mesher.Init("Solid", layers, m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels, m_voxel_size_in_meters, m_voxel_chunk_dimensions, m_water_height, true, m_solid_iso_level, 0f);

        //solid_mesher.enabled = false;

        return solid_mesher;
    }

    VoxelWorld CreateLiquidMesher(float[][] layers)
    {
        var liquid_mesher = GameObject.Instantiate(m_liquid_mesher);
        liquid_mesher.m_tuneables = m_liquid_mesher.m_tuneables;
        liquid_mesher.Init("Liquid", layers, m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels, m_voxel_size_in_meters, m_voxel_chunk_dimensions, m_water_height, false, m_liquid_iso_level, 1f);

        return liquid_mesher;
    }

    void GenerateWorld(float[][] solid_layers, float[][] liquid_layers)
    {
        if (m_use_height_map)
        {
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
        }
        else
        {
            // Make just a solid floor.
            for (int layer_idx = 20; layer_idx < 22; ++layer_idx)
            {
                var layer = solid_layers[layer_idx];
                for (int i = 0; i < layer.Length; ++i)
                {
                    layer[i] = 1;
                }
            }

            var sdf = new DensityField(solid_layers, m_grid_width_in_voxels, m_grid_depth_in_voxels);
            var ldf = new DensityField(liquid_layers, m_grid_width_in_voxels, m_grid_depth_in_voxels);
            var l = 22;

            {
                var x = 188;
                var y = 206;

                sdf.Line(x + 0, y + 0, x + 2, y + 0, l, 1f);
                sdf.Line(x + 0, y + 2, x + 2, y + 2, l, 1f);
                sdf.Line(x + 0, y + 0, x + 0, y + 2, l, 1f);
                sdf.Line(x + 2, y + 0, x + 2, y + 2, l, 1f);

                ldf.Line(x + 1, y + 1, x + 1, y + 1, l, 1f);

            }
        }
    }

    GameObject CreateAvatar()
    {
        return GameObject.Instantiate(m_player_avatar);
    }

    public VoxelWorld GetVoxelWorld()
    {
        return m_solid_mesher;
    }

    public LiquidSimulation GetLiquidSimulation()
    {
        return m_liquid_simulation;
    }

    public SolidSimulation GetSolidSimulation()
    {
        return m_solid_simulation;
    }

    public VoxelWorld GetLiquidMesher()
    {
        return m_liquid_mesher;
    }

    void Update()
    {
        m_dirty_chunk_ids.Clear();
        m_solid_simulation.Update(m_dirty_chunk_ids);
        if (m_dirty_chunk_ids.Count > 0)
        {
            m_solid_mesher.Triangulate(m_dirty_chunk_ids);
        }

        m_dirty_chunk_ids.Clear();
        m_liquid_simulation.Update(m_dirty_chunk_ids);
        if (m_dirty_chunk_ids.Count > 0)
        {
            m_liquid_mesher.Triangulate(m_dirty_chunk_ids);
        }
    }

    void CreateGroundPlane()
    {
        m_ground_plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        m_ground_plane.name = "GroundPlane";

        var ground_plane_material = GameObject.Instantiate(m_solid_mesher.GetMaterial());
        ground_plane_material.color = m_solid_mesher.GetColorForLayer(0);

        var ground_plane_mesh_renderer = m_ground_plane.GetComponent<MeshRenderer>();
        ground_plane_mesh_renderer.sharedMaterial = ground_plane_material;
        ground_plane_mesh_renderer.receiveShadows = false;
        m_ground_plane.transform.localScale = new Vector3(m_ground_plane_size, 1, m_ground_plane_size);
        m_ground_plane.transform.localPosition = new Vector3(0, -0.5f, 0);
    }

    public Vector3 GetVoxelSizeInMeters() { return m_voxel_size_in_meters; }
}