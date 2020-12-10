using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class CreateIslandScreen : MonoBehaviour
{
    [SerializeField] Button m_create_button;
    [SerializeField] Button m_back_button;
    [SerializeField] TMPro.TMP_InputField m_island_name;
    [SerializeField] TMPro.TMP_InputField m_your_name;

    bool m_is_screen_faded;

    string[] m_pronouns = new string[]
    {
        "The",
        "Our",
    };

    string[] m_adjectives = new string[]
    {
        "Magical",
        "Lovely",
        "Enchanting",
        "Delightful",
        "Good",
        "Exotic"
    };

    string[] m_nouns = new string[]
    {
        "Hills",
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
        "Place"
    };

    void Awake()
    {
        m_your_name.text = Player.GetPlayerName();
        m_your_name.onSubmit.AddListener((name) =>
        {
            Player.SetPlayerName(name);
        });

        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });

        m_create_button.onClick.AddListener(() =>
        {
            NetCode.Instance.CreateRoom();
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
        var noun = m_nouns[UnityEngine.Random.Range(0, m_nouns.Length)];
        var adjective = m_adjectives[UnityEngine.Random.Range(0, m_adjectives.Length)];

        var island_name = $"{adjective} {noun}";
        if (UnityEngine.Random.value > 0.25f)
        {
            var pronoun = m_pronouns[UnityEngine.Random.Range(0, m_pronouns.Length)];
            island_name = $"{pronoun} {island_name}";
        }

        m_island_name.text = island_name;

        m_is_screen_faded = false;
    }

    void Update()
    {
        if(m_is_screen_faded && NetCode.Instance.HasJoinedRoom())
        {
            Game.Instance.SpawnAvatar();
            MainMenu.Instance.gameObject.SetActive(false);
        }
    }
}