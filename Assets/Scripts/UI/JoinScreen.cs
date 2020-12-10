using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class JoinScreen : MonoBehaviour
{
    [SerializeField] TMPro.TextMeshProUGUI m_status_text;
    [SerializeField] Button m_refresh_button;
    [SerializeField] Button m_create_island_button;
    [SerializeField] Button m_back_button;

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });

        m_create_island_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_create_island_screen.gameObject);
        });
    }
}