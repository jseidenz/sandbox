using UnityEngine;

[CreateAssetMenu(fileName = "CursorTuning", menuName = "ScriptableObjects/CursorTuning", order = 1)]
public class CursorTuning : ScriptableObject
{
    public float m_default_radius;
    public float m_default_thickness;
    public float m_position_lerp_rate;
    public float m_radius_lerp_rate;

    public float m_flood_sound_rate;
    public AudioClip[] m_flood_sounds;
}