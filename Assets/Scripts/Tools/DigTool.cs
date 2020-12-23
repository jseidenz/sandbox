using UnityEngine;

public class DigTool : Tool
{
    public DigTool(KeyCode key_code, float dig_rate, float dig_distance)
    :   base(key_code)
    {
        m_dig_rate = dig_rate;
        m_dig_distance = dig_distance;
    }

    float m_dig_rate;
    float m_dig_distance;
}