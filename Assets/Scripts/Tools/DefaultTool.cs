using UnityEngine;

class DefaultTool : Tool
{
    public DefaultTool()
    :   base(KeyCode.None)
    {

    }

    public override void LateUpdate(float dt)
    {
        if (CameraRayCast(out var hit))
        {
            m_cursor.m_target_cursor_position = hit.point;
        }
        else
        {
            m_cursor.m_target_cursor_position = camera.transform.position + camera.transform.forward * m_raycast_distance;
        }

        m_cursor.m_target_cursor_radius = m_cursor_tuning.m_default_radius;
    }
}