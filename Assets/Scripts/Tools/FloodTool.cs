﻿using UnityEngine;

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
            hit_point.y += 1.05f * cell_size_in_meters.y;

            var command = new AddLiquidDensityCommand
            {
                m_position = hit_point,
                m_amount = m_flood_rate * Time.deltaTime
            };

            Game.Instance.SendCommand(command);
            command.Run();
        }
    }

    public override void OnEnable()
    {
        m_cursor.PlaySound(m_cursor_tuning.m_flood_sounds[Random.Range(0, m_cursor_tuning.m_flood_sounds.Length)]);
    }

    float m_flood_rate;
    float m_locked_visual_height;
}