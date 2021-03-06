﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{    
    [SerializeField] Image m_full_background;
    [SerializeField] Image m_right_background;
    [SerializeField] public TMPro.TextMeshProUGUI m_connecting_text;
    [SerializeField] public StartGameScreen m_start_game_screen;
    [SerializeField] public JoinIslandScreen m_join_island_screen;
    [SerializeField] public ListIslandsScreen m_list_islands_screen;
    [SerializeField] public CreateIslandScreen m_create_island_screen;
    [SerializeField] public SelectGameModeScreen m_select_game_mode_screen;
    [SerializeField] public ListLoadableIslandsScreen m_list_loadable_islands_screen;
    [SerializeField] public LoadIslandScreen m_load_island_screen;
    [SerializeField] public PauseScreen m_pause_screen;
    [SerializeField] public ControlsScreen m_controls_screen;
    [SerializeField] public AudioClip m_button_click_sound;

    public static MainMenu Instance;

    void Awake()
    {
        Instance = this;
        m_start_game_screen.gameObject.SetActive(false);
        m_connecting_text.gameObject.SetActive(false);
    }

    void Start()
    {
        ScreenFader.StartScreenFade(m_full_background.gameObject, false, 0.4f, 1.5f, () =>
        {
            m_full_background.gameObject.SetActive(false);
            
        });

        m_start_game_screen.gameObject.SetActive(true);
        ScreenFader.StartScreenFade(m_start_game_screen.gameObject, true, 0.4f, 3f);
    }

    public void TransitionScreens(GameObject previous_screen, GameObject next_screen)
    {
        float fade_speed = 12f;
        next_screen.gameObject.SetActive(true);
        ScreenFader.StartScreenFade(next_screen.gameObject, true, fade_speed, 1f / fade_speed);
        if (previous_screen != null)
        {
            ScreenFader.StartScreenFade(previous_screen, false, fade_speed, 0, () =>
            {
                previous_screen.gameObject.SetActive(false);
            });
        }
    }
}