﻿using UnityEngine;
using System.Threading.Tasks;

public struct AddSolidDensityCommand : ICommand
{
    public void Run()
    {
        Game.Instance.GetSolidSimulation().AddDensity(m_position, m_amount);
    }

    public float m_amount;
    public Vector3 m_position;
}

public struct AddLiquidDensityCommand : ICommand
{
    public void Run()
    {
        Game.Instance.GetLiquidSimulation().AddDensity(m_position, m_amount);
    }

    public float m_amount;
    public Vector3 m_position;
}

public class DigTool : MonoBehaviour 
{
    [SerializeField] float m_fill_rate;
    [SerializeField] float m_dig_rate;
    [SerializeField] float m_dig_distance;

    [SerializeField] float m_liquid_fill_rate;
    [SerializeField] float m_liquid_remove_rate;
    [SerializeField] Material m_dig_cursor_material;

    float m_locked_fill_height;


    TorusMesh m_cursor_mesh;

    void Awake()
    {
        if(!GetComponent<Photon.Pun.PhotonView>().IsMine)
        {
            GameObject.Destroy(this);
        }
    }

    void OnEnable()
    {
        m_cursor_mesh = new TorusMesh(1, 0.3f, 30);
    }

    void OnDisable()
    {
        m_cursor_mesh.Destroy();
        m_cursor_mesh = null;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.E))
        {
            var command = new AddLiquidDensityCommand
            {
                m_position = transform.position,
                m_amount = m_liquid_fill_rate * Time.deltaTime
            };

            Game.Instance.SendCommand(command);
            command.Run();
        }

        bool did_raycast_hit = CameraRayCast(out var raycast_hit);

        if(!did_raycast_hit)
        {
            return;
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            float teleport_vertical_offset = 1f;
            GetComponent<IL3DN.IL3DN_SimpleFPSController>().Teleport(raycast_hit.point + new Vector3(0, teleport_vertical_offset, 0));
        }
#endif

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Application.isEditor)
            {

            }
            else
            {
                var camera = Game.Instance.GetCamera();
                MainMenu.Instance.m_pause_screen.SetTransforms(transform.localPosition, transform.localRotation, camera.transform.localPosition, camera.transform.localRotation);

                Game.Instance.DestroyAvatar();
                MainMenu.Instance.gameObject.SetActive(true);
                MainMenu.Instance.GetComponent<CanvasGroup>().alpha = 1f;
                MainMenu.Instance.TransitionScreens(null, MainMenu.Instance.m_pause_screen.gameObject);
            }
        }

        UpdateLiquidControl(KeyCode.Q, m_liquid_fill_rate, raycast_hit);


        UpdateDigControl(KeyCode.Mouse0, -m_dig_rate, raycast_hit);
        UpdateDigControl(KeyCode.Mouse1, m_fill_rate, raycast_hit);
    }

    void UpdateLiquidControl(KeyCode key_code, float amount, RaycastHit hit)
    {
        if (Input.GetKey(key_code))
        {
            if (Input.GetKeyDown(key_code))
            {
                var bias = 1f * hit.normal.y;
                m_locked_fill_height = hit.point.y + bias;
            }

            var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));

            var ray = GetCameraRay();
            if (plane.Raycast(ray, out var distance))
            {
                var hit_point = ray.GetPoint(distance);
                hit_point.y = m_locked_fill_height;

                var command = new AddLiquidDensityCommand
                {
                    m_position = hit_point,
                    m_amount = amount * Time.deltaTime
                };

                Game.Instance.SendCommand(command);
                command.Run();
            }
        }
    }

    void UpdateDigControl(KeyCode key_code, float amount, RaycastHit hit)
    {
        if (Input.GetKey(key_code))
        {
            if (Input.GetKeyDown(key_code))
            {
                var bias = hit.normal.y * 0.05f;
                m_locked_fill_height = hit.point.y + bias;
            }

            var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));

            var ray = GetCameraRay();
            if (plane.Raycast(ray, out var distance))
            {
                var hit_point = ray.GetPoint(distance);
                hit_point.y = m_locked_fill_height;

                var command = new AddSolidDensityCommand
                {
                    m_position = hit_point,
                    m_amount = amount * Time.deltaTime
                };

                Game.Instance.SendCommand(command);
                command.Run();
            }
        }
    }

    Ray GetCameraRay()
    {
        return Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    bool CameraRayCast(out RaycastHit hit)
    {
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return Physics.Raycast(ray, out hit, m_dig_distance);
    }
}