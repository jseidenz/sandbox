using UnityEngine;

public class DigTool : Tool
{
    public DigTool(KeyCode key_code, float dig_rate, float dig_distance)
    :   base(key_code)
    {
        m_dig_rate = dig_rate;
        m_dig_distance = dig_distance;
    }

    public override bool TryStartUsing()
    {
        if (CameraRayCast(out var hit))
        {
            var bias = hit.normal.y * 0.05f;
            m_locked_fill_height = hit.point.y + bias;

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void LateUpdate(float dt)
    {
        var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));

        var ray = GetCameraRay();
        if (plane.Raycast(ray, out var distance))
        {
            var hit_point = ray.GetPoint(distance);
            hit_point.y = m_locked_fill_height;

            var command = new AddSolidDensityCommand
            {
                m_position = hit_point,
                m_amount = m_dig_rate * Time.deltaTime
            };

            Game.Instance.SendCommand(command);
            command.Run();
        }
    }


    bool CameraRayCast(out RaycastHit hit)
    {
        var ray = GetCameraRay();
        return Physics.Raycast(ray, out hit, m_dig_distance);
    }

    Ray GetCameraRay()
    {
        return Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    float m_dig_rate;
    float m_dig_distance;
    float m_locked_fill_height;
}