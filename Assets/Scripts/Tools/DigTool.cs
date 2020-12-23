using UnityEngine;

public class DigTool : Tool
{
    public DigTool(KeyCode key_code, float dig_rate)
    :   base(key_code)
    {
        m_dig_rate = dig_rate;
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

    float m_dig_rate;
    float m_locked_fill_height;
}