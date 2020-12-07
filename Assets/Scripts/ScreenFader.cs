using System;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    float m_fade_speed;
    float m_fade_delay;
    bool m_fade_in;
    System.Action m_fade_callback;

    CanvasGroup m_canvas_group;

    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1 / 30f);

        if(m_fade_delay > 0)
        {
            m_fade_delay -= dt;
        }

        if(m_fade_delay <= 0)
        {
            if(m_fade_in)
            {
                m_canvas_group.alpha = System.Math.Min(m_canvas_group.alpha + dt * m_fade_speed, 1.0f);
                if (m_canvas_group.alpha >= 1.0f)
                {
                    enabled = false;
                    if(m_fade_callback != null)
                    {
                        m_fade_callback();
                    }
                }
            }
            else
            {
                m_canvas_group.alpha = System.Math.Max(m_canvas_group.alpha - dt * m_fade_speed, 0.0f);
                if (m_canvas_group.alpha <= 0.0f)
                {
                    enabled = false;
                    if (m_fade_callback != null)
                    {
                        m_fade_callback();
                    }
                }
            }
        }
    }

    public void StartFade(bool fade_in, float fade_speed, float fade_delay, System.Action fade_callback)
    {
        if(!TryGetComponent<CanvasGroup>(out m_canvas_group))
        {
            m_canvas_group = gameObject.AddComponent<CanvasGroup>();
        }

        m_fade_in = fade_in;
        m_fade_speed = fade_speed;
        m_fade_delay = fade_delay;
        m_fade_callback = fade_callback;

        if (fade_in)
        {
            m_canvas_group.alpha = 0f;
        }
        else
        {
            m_canvas_group.alpha = 1f;
        }

        enabled = true;
    }

    public static void StartScreenFade(GameObject go, bool fade_in, float fade_speed, float fade_delay = 0f, System.Action callback = null)
    {
        if(!go.TryGetComponent<ScreenFader>(out var screen_fader))
        {
            screen_fader = go.AddComponent<ScreenFader>();
        }
        screen_fader.StartFade(fade_in, fade_speed, fade_delay, callback);
    }
}