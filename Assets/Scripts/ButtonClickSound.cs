using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSound : MonoBehaviour
{
    void Awake()
    {

    }

    void Start()
    {
        var audio_source = gameObject.AddComponent<AudioSource>();
        audio_source.clip = MainMenu.Instance.m_button_click_sound;
        audio_source.playOnAwake = false;
        GetComponent<Button>().onClick.AddListener(() =>
        {
            audio_source.Play();
        });
    }
}

