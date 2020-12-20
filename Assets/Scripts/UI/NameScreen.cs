using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class NameScreen : MonoBehaviour
{
    [SerializeField] NameWidget m_name_widget_prefab;

    public static NameScreen Instance;

    Dictionary<PlayerName, NameWidget> m_player_names = new Dictionary<PlayerName, NameWidget>();

    void OnEnable()
    {
        Instance = this;

        m_name_widget_prefab.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        Instance = null;
    }

    public void Register(PlayerName player_name)
    {
        m_player_names[player_name] = null;
    }

    public void Unregister(PlayerName player_name)
    {
        m_player_names.Remove(player_name);
    }
}