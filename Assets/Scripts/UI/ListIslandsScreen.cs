using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class ListIslandsScreen : MonoBehaviour
{
    [SerializeField] JoinIslandWidget m_join_island_widget;
    [SerializeField] Button m_back_button;

    List<JoinIslandWidget> m_join_widgets = new List<JoinIslandWidget>();

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });

        m_join_island_widget.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        foreach(var widget in m_join_widgets)
        {
            GameObject.Destroy(widget.gameObject);
        }
        m_join_widgets.Clear();

        var rooms = NetCode.Instance.GetRooms();
        foreach (var room in rooms)
        {
            var widget = GameObject.Instantiate(m_join_island_widget);
            widget.GetComponent<RectTransform>().SetParent(m_join_island_widget.transform.parent);
            widget.m_island_name.text = room.Name.Split('|')[0];
            widget.transform.localScale = Vector3.one;
            widget.m_button.onClick.AddListener(() =>
            {
                MainMenu.Instance.m_join_island_screen.SetRoom(room.Name);
                MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_join_island_screen.gameObject);
            });
            widget.gameObject.SetActive(true);
            m_join_widgets.Add(widget);
        }
    }
}