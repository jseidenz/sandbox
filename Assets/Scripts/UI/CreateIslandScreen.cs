using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class CreateIslandScreen : MonoBehaviour
{
    [SerializeField] Button m_create_button;
    [SerializeField] Button m_back_button;

    bool m_is_screen_faded;

    void Awake()
    {
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