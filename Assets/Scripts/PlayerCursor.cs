using UnityEngine;
using Photon.Compression;

public class PlayerCursor : MonoBehaviour
{
    [SerializeField] public CursorTuning m_cursor_tuning;

    public Vector3 m_target_cursor_position;
    Transform m_camera_transform;


    void OnEnable()
    {
        m_camera_transform = Camera.main.transform;
    }

    void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, m_target_cursor_position, m_cursor_tuning.m_position_lerp_rate * Time.deltaTime);
        transform.position = m_target_cursor_position;
    }

    public void PlaySound(AudioClip audio_clip)
    {
        var audio_source = GetComponent<AudioSource>();
        audio_source.clip = audio_clip;
        audio_source.Play();
    }
}