using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] Image m_full_background;
    [SerializeField] Image m_right_background;
    [SerializeField] StartGameScreen m_start_game_screen;

    void Awake()
    {
        m_start_game_screen.gameObject.SetActive(false);    
    }

    private void Start()
    {
        ScreenFader.StartScreenFade(m_full_background.gameObject, false, 0.4f, 3.5f, () =>
        {
            m_full_background.gameObject.SetActive(false);
            
            m_start_game_screen.gameObject.SetActive(true);
            ScreenFader.StartScreenFade(m_start_game_screen.gameObject, true, 0.4f, 0.5f);
        });

        

    }
}