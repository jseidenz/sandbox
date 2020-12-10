﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class SelectGameModeScreen : MonoBehaviour
{
    [SerializeField] Button m_join_island_button;
    [SerializeField] Button m_create_island_button;
    [SerializeField] Button m_back_button;

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_start_game_screen.gameObject);
        });

        m_join_island_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_join_screen.gameObject);
        });

        m_create_island_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_create_island_screen.gameObject);
        });
    }
}