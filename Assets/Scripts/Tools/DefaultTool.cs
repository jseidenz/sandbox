using UnityEngine;

class DefaultTool : Tool
{
    public DefaultTool()
    :   base(KeyCode.None)
    {

    }

    public override void LateUpdate(float dt)
    {
        bool should_cursor_be_active = false;
        if (CameraRayCast(out var hit))
        {
            should_cursor_be_active = true;
            m_cursor.m_target_cursor_position = hit.point;
        }
        else
        {
            m_cursor.m_target_cursor_position = camera.transform.position + camera.transform.forward * m_raycast_distance;
        }

        if(m_cursor.gameObject.activeSelf != should_cursor_be_active)
        {
            m_cursor.gameObject.SetActive(true);
        }
    }

    public override void OnDisable()
    {
        m_cursor.gameObject.SetActive(true);
    }
}