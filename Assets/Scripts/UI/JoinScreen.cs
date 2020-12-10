using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class JoinScreen : MonoBehaviour
{
    [SerializeField] JoinIslandWidget m_join_island_widget;
    [SerializeField] Button m_back_button;

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });

        m_join_island_widget.gameObject.SetActive(false);
    }
}