using UnityEngine;

public class FloodTool : Tool
{
    public FloodTool(KeyCode key_code, float flood_rate)
    :   base(key_code)
    {
        m_flood_rate = flood_rate;
    }

    public override bool TryStartUsing()
    {
        if (CameraRayCast(out var hit))
        {
            var bias = 1f * hit.normal.y;
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

            var command = new AddLiquidDensityCommand
            {
                m_position = hit_point,
                m_amount = m_flood_rate * Time.deltaTime
            };

            Game.Instance.SendCommand(command);
            command.Run();
        }
    }

    float m_flood_rate;
    float m_locked_fill_height;
}