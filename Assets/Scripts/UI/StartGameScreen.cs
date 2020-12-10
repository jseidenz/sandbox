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
            ScreenFader.StartScreenFade(gameObject, false, 0.4f, 0.25f, () =>
            {
                ScreenFader.StartScreenFade(MainMenu.Instance.gameObject, false, 0.4f, 2f, () =>
                {
                    Game.Instance.SpawnAvatar();
                    MainMenu.Instance.gameObject.SetActive(false);
                });
            });
        });
    }

    void Update()
    {
        bool is_connected_to_master = NetCode.Instance.IsConnectedToMaster();
        if (m_button.interactable != is_connected_to_master)
        {
            if(is_connected_to_master)
            {
                m_text.text = "Start Game";
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