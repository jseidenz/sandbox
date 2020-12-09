using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class StartGameScreen : MonoBehaviour
{
    [SerializeField] Button m_button;

    void Awake()
    {
        m_button.onClick.AddListener(() =>
        {
            ScreenFader.StartScreenFade(gameObject, false, 0.4f, 0.25f, () =>
            {
                ScreenFader.StartScreenFade(MainMenu.Instance.gameObject, false, 0.4f, 2f, () =>
                {
                    Game.Instance.SpawnAvatar();
                    MainMenu.Instance.gameObject.SetActive(false);
                });
            });
        });
    }
}