using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    [SerializeField] Button m_disconnect_button;
    [SerializeField] Button m_resume_button;
    [SerializeField] Button m_save_button;
    [SerializeField] TMPro.TMP_InputField m_your_name;
    [SerializeField] TMPro.TextMeshProUGUI m_island_name;

    bool m_is_screen_faded;
    bool m_is_fading;

    Vector3 m_avatar_previous_position;
    Quaternion m_avatar_previous_orientation;
    Vector3 m_camera_previous_position;
    Quaternion m_camera_previous_orientation;
    

    void Awake()
    {
        m_disconnect_button.onClick.AddListener(() =>
        {
            NetCode.Instance.LeaveRoom();
            MainMenu.Instance.TransitionScreens(gameObject, MainMenu.Instance.m_start_game_screen.gameObject);
            ScreenFader.StartScreenFade(MainMenu.Instance.m_controls_screen.gameObject, false, 12f, 0f, () =>
            {
                MainMenu.Instance.m_controls_screen.gameObject.SetActive(false);
            });
        });

        m_resume_button.onClick.AddListener(() =>
        {
            StartFadeOut();
        });

        m_save_button.onClick.AddListener(() =>
        {
            Game.Instance.Save();
            StartFadeOut();
        });
    }

    void StartFadeOut()
    {
        m_is_fading = true;
        ScreenFader.StartScreenFade(gameObject, false, 12f, 0.0f, () =>
        {
            m_is_screen_faded = true;
        });

        ScreenFader.StartScreenFade(MainMenu.Instance.m_controls_screen.gameObject, false, 12f, 0f, () =>
        {
            MainMenu.Instance.m_controls_screen.gameObject.SetActive(false);
        });
    }

    void OnEnable()
    {
        m_is_fading = false;
        m_is_screen_faded = false;
        m_your_name.text = Player.GetPlayerName();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(EnableCursor());


        MainMenu.Instance.m_controls_screen.gameObject.SetActive(true);
        ScreenFader.StartScreenFade(MainMenu.Instance.m_controls_screen.gameObject, true, 12f, 0.25f, () =>
        {

        });
    }

    public void SetTransforms(Vector3 avatar_local_position, Quaternion avatar_local_rotation, Vector3 camera_local_position, Quaternion camera_local_rotation)
    {
        m_avatar_previous_position = avatar_local_position;
        m_avatar_previous_orientation = avatar_local_rotation;
        m_camera_previous_position = camera_local_position;
        m_camera_previous_orientation = camera_local_rotation;
    }

    System.Collections.IEnumerator EnableCursor()
    {
        yield return null;
        yield return null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if(!m_is_fading && Input.GetKeyDown(KeyCode.Escape))
        {
            StartFadeOut();
        }

        if (m_is_screen_faded)
        {
            Game.Instance.SpawnAvatar(m_avatar_previous_position, m_avatar_previous_orientation, m_camera_previous_position, m_camera_previous_orientation);
            MainMenu.Instance.gameObject.SetActive(false);
        }        
    }
}