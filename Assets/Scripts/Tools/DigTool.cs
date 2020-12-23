using UnityEngine;
public struct AddSolidDensityCommand : ICommand
{
    public void Run()
    {
        Game.Instance.GetSolidSimulation().AddDensity(m_position, m_amount);
    }

    public float m_amount;
    public Vector3 m_position;
}

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
            m_locked_visual_height = m_cursor.transform.position.y;

            return true;
        }
        else
        {
            return false;
        }
    }

    public override void LateUpdate(float dt)
    {
        var plane = new Plane(Vector3.up, new Vector3(0, m_locked_visual_height, 0));

        var ray = GetCameraRay();
        if (plane.Raycast(ray, out var distance))
        {
            var hit_point = ray.GetPoint(distance);

            m_cursor.m_target_cursor_position = new Vector3(hit_point.x, m_locked_visual_height, hit_point.z);

            var cell_size_in_meters = Game.Instance.GetCellSizeInMeters();
            hit_point.x += 0.5f * cell_size_in_meters.x;
            hit_point.z += 0.5f * cell_size_in_meters.z;
            hit_point.y += 0.99f * cell_size_in_meters.y;

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
    float m_locked_visual_height;
}