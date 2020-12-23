using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class StartGameScreen : MonoBehaviour
{
    [SerializeField] Button m_button;
    [SerializeField] TMPro.TextMeshProUGUI m_text;
    Vector3 m_initial_camera_pos;
    Quaternion m_initial_camera_rotation;
    Camera m_camera;

    void Awake()
    {
        m_button.onClick.AddListener(() =>
        {
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_select_game_mode_screen.gameObject);
        });
    }

    void Start()
    {
        m_camera = Camera.main;
        m_initial_camera_pos = m_camera.transform.position;
        m_initial_camera_rotation = m_camera.transform.rotation;
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

        const float EPSILON = 0.0001f;
        if ((m_camera.transform.position - m_initial_camera_pos).sqrMagnitude > EPSILON)
        {
            m_camera.transform.position = Vector3.Lerp(m_camera.transform.position, m_initial_camera_pos, Time.deltaTime * 5);
        }

        if(m_camera.transform.rotation != m_initial_camera_rotation)
        { 
            m_camera.transform.rotation = Quaternion.Slerp(m_camera.transform.rotation, m_initial_camera_rotation, Time.deltaTime * 2.5f);
        }
    }
}