using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using UnityEngine.Profiling;

public class CreateIslandScreen : MonoBehaviour
{
    [SerializeField] Button m_create_button;
    [SerializeField] Button m_back_button;
    [SerializeField] Button m_randomize_button;
    [SerializeField] TMPro.TMP_InputField m_island_name;
    [SerializeField] TMPro.TMP_InputField m_your_name;

    bool m_is_screen_faded;

    string[] m_pronouns = new string[]
    {
        "The",
        "Our",
        "This"
    };

    string[] m_adjectives = new string[]
    {
        "Magical",
        "Lovely",
        "Enchanting",
        "Delightful",
        "Good",
        "Great",
        "Exotic",
        "Peaceful",
        "Tranquil",
        "Serene",
        "Sunny"
    };

    string[] m_nouns = new string[]
    {
        "Land",
        "Island",
        "Archipelago",
        "Retreat",
        "Isle",
        "Reef",
        "Peninsula",
        "Cove",
        "Delight",
        "Bliss",
        "Home",
        "Paradise",
        "Place",
        "Treasure"
    };

    void Awake()
    {
        m_your_name.onSubmit.AddListener((name) =>
        {
            Player.SetPlayerName(name);
        });

        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });

        m_randomize_button.onClick.AddListener(() =>
        {
            RandomizeIslandName();
            RandomizeWorld();
        });

        m_create_button.onClick.AddListener(() =>
        {
            var room_id = $"{m_island_name.text.Replace("_", " ")}_{UnityEngine.Random.Range(int.MinValue, int.MaxValue)}";
            Game.Instance.SetRoomId(room_id);
            NetCode.Instance.CreateRoom(room_id);
            ScreenFader.StartScreenFade(gameObject, false, 12f, 0.0f, () =>
            {
                ScreenFader.StartScreenFade(MainMenu.Instance.gameObject, false, 0.4f, 1f, () =>
                {
                    m_is_screen_faded = true;
                });
            });
        });
    }

    void OnEnable()
    {
        m_your_name.text = Player.GetPlayerName();

        RandomizeIslandName();

        m_is_screen_faded = false;
    }

    void RandomizeIslandName()
    {
        var noun = m_nouns[UnityEngine.Random.Range(0, m_nouns.Length)];
        var adjective = m_adjectives[UnityEngine.Random.Range(0, m_adjectives.Length)];

        var island_name = $"{adjective} {noun}";
        if (UnityEngine.Random.value > 0.25f)
        {
            var pronoun = m_pronouns[UnityEngine.Random.Range(0, m_pronouns.Length)];
            island_name = $"{pronoun} {island_name}";
        }

        m_island_name.text = island_name;
    }

    void RandomizeWorld()
    {
        Profiler.BeginSample("GenerateHeightMap");
        var solid_simulation = Game.Instance.GetSolidSimulation();
        var dimensions_in_cells = solid_simulation.GetDimensionsInCells();
        var height_map = new HeightMapGenerator().GenerateHeightMap(dimensions_in_cells.x, dimensions_in_cells.z, 4f);
        Profiler.EndSample();

        Profiler.BeginSample("ApplyHeightMap");
        solid_simulation.ApplyHeightMap(height_map);
        Profiler.EndSample();

        Game.Instance.GetLiquidSimulation().Clear();

        Game.Instance.StartWorldGeneration();
    }

    void Update()
    {
        if(m_is_screen_faded && NetCode.Instance.HasJoinedRoom() && Game.Instance.IsWorldGenerationComplete())
        {
            Game.Instance.SpawnAvatar();
            MainMenu.Instance.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}