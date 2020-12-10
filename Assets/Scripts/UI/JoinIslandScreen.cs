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

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_list_islands_screen.gameObject);
        });
    }

    void OnEnable()
    {
        m_your_name.text = Player.GetPlayerName();
    }

    public void SetIsland(string island_name)
    {
        m_island_name.text = island_name;
    }
}