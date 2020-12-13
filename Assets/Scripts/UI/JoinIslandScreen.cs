using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class JoinIslandScreen : MonoBehaviour
{
    [SerializeField] Button m_back_button;
    [SerializeField] Button m_join_button;
    [SerializeField] TMPro.TMP_InputField m_your_name;
    [SerializeField] TMPro.TextMeshProUGUI m_island_name;

    bool m_is_screen_faded;
    bool m_has_started_loading_island;
    float m_connecting_timer;
    int m_connecting_timer_iteration;

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
        m_has_started_loading_island = false;
        m_your_name.text = Player.GetPlayerName();


        MainMenu.Instance.m_connecting_text.gameObject.SetActive(true);
        MainMenu.Instance.m_connecting_text.text = "Connecting...";
        m_connecting_timer = 0;
        m_connecting_timer_iteration = 0;
        ScreenFader.StartScreenFade(MainMenu.Instance.m_connecting_text.gameObject, true, 12f, 0.25f, () =>
        {

        });
    }

    public void OnIslandStartLoading()
    {
        m_has_started_loading_island = true;

        ScreenFader.StartScreenFade(MainMenu.Instance.m_connecting_text.gameObject, false, 12f, 0f, () =>
        {
            MainMenu.Instance.m_connecting_text.gameObject.SetActive(false);
        });
    }

    void OnDisable()
    {
        if (MainMenu.Instance.m_connecting_text.gameObject.activeSelf)
        {
            ScreenFader.StartScreenFade(MainMenu.Instance.m_connecting_text.gameObject, false, 12f, 0f, () =>
            {
                MainMenu.Instance.m_connecting_text.gameObject.SetActive(false);
            });
        }
    }

    public void SetRoom(string room_id)
    {
        Game.Instance.SetRoomId(room_id);
        NetCode.Instance.JoinRoom(room_id);
        m_island_name.text = Game.Instance.GetIslandName();
    }

    void Update()
    {
        if (m_is_screen_faded && NetCode.Instance.HasJoinedRoom() && m_has_started_loading_island && Game.Instance.IsWorldGenerationComplete())
        {
            Game.Instance.SpawnAvatar();
            MainMenu.Instance.gameObject.SetActive(false);
        }


        m_connecting_timer += Time.deltaTime;
        const float TEXT_UPDATE_INTERVAL = 1f;
        if(m_connecting_timer > TEXT_UPDATE_INTERVAL)
        {
            m_connecting_timer = 0f;
            var text = "Connecting.";
            if(m_connecting_timer_iteration == 1)
            {
                text = "Connecting..";
            }
            else if(m_connecting_timer_iteration == 2)
            {
                text = "Connecting...";
            }
            MainMenu.Instance.m_connecting_text.text = text;

            m_connecting_timer_iteration = (m_connecting_timer_iteration + 1) % 3;
        }
        
    }
}