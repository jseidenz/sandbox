﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class JoinIslandScreen : MonoBehaviour
{
    [SerializeField] Button m_back_button;
    [SerializeField] Button m_join_button;
    [SerializeField] TMPro.TMP_InputField m_your_name;
    [SerializeField] TMPro.TextMeshProUGUI m_island_name;

    string m_room_id;
    bool m_is_screen_faded;

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {

            NetCode.Instance.LeaveRoom();
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_list_islands_screen.gameObject);
        });

        m_join_button.onClick.AddListener(() =>
        {
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
        m_your_name.text = Player.GetPlayerName();
    }

    public void SetRoom(string room_id)
    {
        Game.Instance.SetRoomId(room_id);
        NetCode.Instance.JoinRoom(room_id);
        m_room_id = room_id;
        m_island_name.text = Game.Instance.GetIslandName();
    }

    void Update()
    {
        if (m_is_screen_faded && NetCode.Instance.HasJoinedRoom() && Game.Instance.IsWorldGenerationComplete())
        {
            Game.Instance.SpawnAvatar();
            MainMenu.Instance.gameObject.SetActive(false);
        }
    }
}