using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.IO;
using UnityEngine.Profiling;
using System.IO.Compression;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Game : MonoBehaviour 
{
    [SerializeField] GameObject m_player_avatar;
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
    [SerializeField] bool m_use_world_gen;
    [SerializeField] bool m_liquid_sim_enabled_on_startup;
    [SerializeField] public float m_min_density_to_allow_flow;
    [SerializeField] Camera m_camera;
    [SerializeField] Vector3 m_camera_offset;
    [SerializeField] bool m_draw_solid_meshes;
    [SerializeField] BevelTuning m_bevel_tuning;
    

    LiquidSimulation m_liquid_simulation;
    SolidSimulation m_solid_simulation;
    Mesher m_solid_mesher;
    Mesher m_liquid_mesher;
    WorldGenerator m_world_generator;
    SolidLayeredBrush m_solid_brush;
    LiquidLayeredBrush m_liquid_brush;


    HashSet<Vector3Int> m_dirty_chunk_ids = new HashSet<Vector3Int>();
    GameObject m_ground_plane;
    CommandBuffer m_outgoing_command_buffer = new CommandBuffer();
    string m_room_id;
    bool m_is_command_line_new_game;
    bool m_is_waiting_to_spawn;
    bool m_has_spawned_avatar;

    public static Game Instance;

    void Awake()
    {
        Application.targetFrameRate = -1;
        m_solid_simulation = new SolidSimulation(new Vector3Int(m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels), m_voxel_size_in_meters, m_voxel_chunk_dimensions);
        var solid_layers = m_solid_simulation.GetLayers();

        m_liquid_simulation = new LiquidSimulation(new Vector3Int(m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels), m_voxel_size_in_meters, m_voxel_chunk_dimensions, solid_layers, m_solid_iso_level, m_min_density_to_allow_flow);
        var liquid_layers = m_liquid_simulation.GetLayers();
        m_liquid_simulation.SetSimulationEnabled(m_liquid_sim_enabled_on_startup);

        m_solid_brush = SolidLayeredBrush.LoadBrush("SolidMaterials");
        m_liquid_brush = new LiquidLayeredBrush(Resources.Load<Material>("LiquidMaterials/Liquid"));

        m_solid_mesher = CreateSolidMesher(solid_layers, m_solid_brush);
        m_liquid_mesher = CreateLiquidMesher(m_liquid_simulation.GetLayers(), m_liquid_brush);

        m_solid_mesher.BindCamera(m_camera);
        m_liquid_mesher.BindCamera(m_camera);

        CreateGroundPlane(m_solid_brush);

        m_water = GameObject.Instantiate(m_water);

        m_water.transform.position = new Vector3(0, m_water_height, 0);

        SetRoomId("quicksave_12345");

        ProcessCommandLineFile();

        GenerateWorld(solid_layers, liquid_layers);

        Instance = this;
    }


    Mesher CreateSolidMesher(float[][] layers, LayeredBrush brush)
    {
        var solid_mesher = new Mesher();
        solid_mesher.Init("Solid", layers, m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels, m_voxel_size_in_meters, m_voxel_chunk_dimensions, true, m_solid_iso_level, 0f, brush, true, m_bevel_tuning);

        //solid_mesher.enabled = false;

        return solid_mesher;
    }

    Mesher CreateLiquidMesher(float[][] layers, LayeredBrush brush)
    {
        var liquid_mesher = new Mesher();
        liquid_mesher.Init("Liquid", layers, m_grid_width_in_voxels, m_grid_height_in_voxels, m_grid_depth_in_voxels, m_voxel_size_in_meters, m_voxel_chunk_dimensions, false, m_liquid_iso_level, 1f, brush, false, m_bevel_tuning);

        return liquid_mesher;
    }

    public void GenerateWorld()
    {
        var world_generator = new HeightMapGenerator();
        Profiler.BeginSample("GenerateHeightMap");
        var height_map = world_generator.GenerateHeightMap(m_grid_width_in_voxels, m_grid_depth_in_voxels, 4);
        Profiler.EndSample();

        Profiler.BeginSample("ApplyHeightMap");
        m_solid_simulation.ApplyHeightMap(height_map);
        Profiler.EndSample();

        Profiler.BeginSample("TriangulateAll");
        m_solid_mesher.TriangulateAll();
        Profiler.EndSample();
    }

    void GenerateWorld(float[][] solid_layers, float[][] liquid_layers)
    {
        if(m_use_world_gen)
        {
            GenerateWorld();
        }
        else if (m_use_height_map)
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

            m_solid_mesher.TriangulateAll();
            m_liquid_mesher.TriangulateAll();
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

            m_solid_mesher.TriangulateAll();
            m_liquid_mesher.TriangulateAll();
        }
    }

    public void SpawnAvatar()
    {
        var pos = m_camera.transform.position - m_camera_offset;
        m_player_avatar = PhotonNetwork.Instantiate("PlayerPrefab", pos, m_player_avatar.transform.rotation);

        m_camera.transform.parent = m_player_avatar.transform;
        m_camera.transform.localPosition = m_camera_offset;
        m_camera.transform.forward = -Vector3.forward;

        m_has_spawned_avatar = true;
    }

    public Mesher GetSolidMesher()
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

    public Mesher GetLiquidMesher()
    {
        return m_liquid_mesher;
    }

    void Update()
    {
        if(m_world_generator != null)
        {
            if(m_world_generator.Update())
            {
                m_world_generator = null;
            }
        }

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

        if(Input.GetKeyDown(KeyCode.F9))
        {
            Save();
        }

        if (Input.GetKeyDown(KeyCode.F10))
        {
            Load();
        }

        if(m_outgoing_command_buffer.TryGetCommandBuffer(out var buffer))
        {
            NetCode.Instance.SendCommandsToServer(buffer);
        }

        if(m_is_command_line_new_game && NetCode.Instance.HasJoinedLobby())
        {
            if (!m_is_waiting_to_spawn)
            {
                var room_id = $"quick_save_{SystemInfo.deviceUniqueIdentifier}";
                SetRoomId(room_id);
                NetCode.Instance.CreateRoom(room_id);

                m_is_waiting_to_spawn = true;
            }
            else if(NetCode.Instance.HasJoinedRoom() && !m_has_spawned_avatar)
            {
                SpawnAvatar();
            }
            
        }
    }

    void LateUpdate()
    {
#if UNITY_EDITOR
        Profiler.BeginSample("RefreshLookupTable");
        m_solid_brush.RefreshLookupTable();
        Profiler.EndSample();
#endif

        float dt = Time.deltaTime;

        if (m_draw_solid_meshes)
        {
            m_solid_mesher.Render(dt);
        }
        m_liquid_mesher.Render(dt);
    }

    void CreateGroundPlane(LayeredBrush brush)
    {
        m_ground_plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        m_ground_plane.name = "GroundPlane";

        brush.GetMaterialForLayer(0, out var material);

        var ground_plane_mesh_renderer = m_ground_plane.GetComponent<MeshRenderer>();
        ground_plane_mesh_renderer.sharedMaterial = material;
        ground_plane_mesh_renderer.receiveShadows = false;
        m_ground_plane.transform.localScale = new Vector3(m_ground_plane_size, 1, m_ground_plane_size);
        m_ground_plane.transform.localPosition = new Vector3(0, -0.5f, 0);
    }

    void OnDestroy()
    {
        m_solid_mesher.OnDestroy();
        m_liquid_mesher.OnDestroy();
    }

    public string GetSaveFilePath()
    {
        return System.IO.Path.Combine(GetSaveFileFolder(), $"{m_room_id}.sav");
    }

    public string GetSaveFileFolder()
    {
        return Application.persistentDataPath;
    }

    public string[] GetSaveFiles()
    {
        return System.IO.Directory.GetFiles(Game.Instance.GetSaveFileFolder(), "*.sav");
    }

    public byte[] CreateSaveData()
    {
        var chunk_serializer = new ChunkSerializer();
        m_solid_simulation.Save(chunk_serializer);
        m_liquid_simulation.Save(chunk_serializer);

        chunk_serializer.Finalize(out var data, out int data_length);

        var compressed_stream = new MemoryStream();        
        using (var dstream = new DeflateStream(compressed_stream, System.IO.Compression.CompressionLevel.Fastest))
        {
            dstream.Write(data, 0, data_length);
            dstream.Close();
            return compressed_stream.ToArray();
        }
    }

    public void Save()
    {
        var path = GetSaveFilePath();
        Debug.Log($"Saving to {path}");
        var data = CreateSaveData();
        File.WriteAllBytes(path, data);
    }

    public void Load()
    {
        var path = GetSaveFilePath();
        Debug.Log($"Loading {path}");
        byte[] save_file_bytes = null;
        using (var dstream = new DeflateStream(File.OpenRead(path), CompressionMode.Decompress))
        {
            var data = new MemoryStream();
            dstream.CopyTo(data);

            save_file_bytes = data.ToArray();
        }

        LoadUncompressedSaveFileBytes(save_file_bytes);
    }

    public void LoadCompressedSaveFileBytes(byte[] compressed_save_file_bytes)
    {
        byte[] save_file_bytes = null;
        using (var dstream = new DeflateStream(new MemoryStream(compressed_save_file_bytes), CompressionMode.Decompress))
        {
            var data = new MemoryStream();
            dstream.CopyTo(data);

            save_file_bytes = data.ToArray();
        }

        LoadUncompressedSaveFileBytes(save_file_bytes);
    }

    void LoadUncompressedSaveFileBytes(byte[] save_file_bytes)
    {
        var chunk_deserializer = new ChunkDeserializer(save_file_bytes, 0);
        m_solid_simulation.Load(chunk_deserializer);
        m_liquid_simulation.Load(chunk_deserializer);

        StartWorldGeneration();
    }

    public string GetRoomName()
    {
        return m_room_id;
    }

    public string GetIslandName()
    {
        return m_room_id.Split('_')[0];
    }

    public void SetRoomId(string room_id)
    {
        m_room_id = room_id;
    }

    void OnApplicationQuit()
    {
        Save();
    }

    public void StartWorldGeneration()
    {
        m_world_generator = new WorldGenerator(m_solid_simulation, m_solid_mesher);
    }

    public bool IsWorldGenerationComplete()
    {
        return m_world_generator == null;
    }

    public void SendCommand<T>(T command) where T : struct, ICommand
    {
        m_outgoing_command_buffer.WriteCommand(command);
    }

    static string COMMAND_LINE_FILE = "command_line.txt";

    public static string NEW_GAME_COMMAND = "-new_game";

#if UNITY_EDITOR
    public static void LaunchGameWithCommandLine(string command_line)
    {
        EditorApplication.isPlaying = true;
        File.WriteAllText(COMMAND_LINE_FILE, command_line);
    }
#endif

    public void ProcessCommandLineFile()
    {
        if (!File.Exists(COMMAND_LINE_FILE)) return;
        var command_line = File.ReadAllText(COMMAND_LINE_FILE).Split(' ')[0];

        if(command_line == NEW_GAME_COMMAND)
        {
            StartCoroutine(NewGame());
        }

        DeleteCommandLineFile();
    }

    IEnumerator NewGame()
    {
        m_is_command_line_new_game = true;
        yield return null;        
        MainMenu.Instance.gameObject.SetActive(false);
    }

        public static void DeleteCommandLineFile()
    {
        if (!File.Exists(COMMAND_LINE_FILE)) return;

        File.Delete(COMMAND_LINE_FILE);
    }

    public Vector3 GetVoxelSizeInMeters() { return m_voxel_size_in_meters; }
}