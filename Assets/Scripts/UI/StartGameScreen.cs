using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class StartGameScreen : MonoBehaviour
{
    [SerializeField] Button m_button;
    [SerializeField] TMPro.TextMeshProUGUI m_text;

    void Awake()
    {
        m_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });
    }

    void Update()
    {
        bool is_connected_to_master = NetCode.Instance.IsConnectedToMaster();
        if (m_button.interactable != is_connected_to_master)
        {
            if(is_connected_to_master)
            {
                m_text.text = "Start";
                m_button.interactable = true;
            }
            else
            {
                m_text.text = "Connecting...";
                m_button.interactable = false;
            }
        }
    }
}