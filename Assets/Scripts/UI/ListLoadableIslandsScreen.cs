using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class ListLoadableIslandsScreen : MonoBehaviour
{
    [SerializeField] LoadIslandWidget m_load_island_widget;
    [SerializeField] Button m_back_button;

    List<LoadIslandWidget> m_load_widgets = new List<LoadIslandWidget>();

    void Awake()
    {
        m_back_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });

        m_load_island_widget.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        foreach(var widget in m_load_widgets)
        {
            GameObject.Destroy(widget.gameObject);
        }
        m_load_widgets.Clear();

        foreach(var file_path in Game.Instance.GetSaveFiles())
        {
            var file_name = System.IO.Path.GetFileNameWithoutExtension(file_path);
            var widget = GameObject.Instantiate(m_load_island_widget);
            widget.GetComponent<RectTransform>().SetParent(m_load_island_widget.transform.parent);
            widget.m_island_name.text = file_name.Split('_')[0];
            widget.transform.localScale = Vector3.one;
            widget.m_button.onClick.AddListener(() =>
            {
                MainMenu.Instance.m_load_island_screen.SetRoom(file_name);
                MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_load_island_screen.gameObject);
            });
            widget.gameObject.SetActive(true);
            m_load_widgets.Add(widget);
        }
    }
}