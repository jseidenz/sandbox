using UnityEngine;
using System.Threading.Tasks;

public class DigTool : MonoBehaviour 
{
    [SerializeField] float m_fill_rate;
    [SerializeField] float m_dig_rate;
    [SerializeField] float m_dig_distance;

    [SerializeField] float m_liquid_fill_rate;
    [SerializeField] float m_liquid_remove_rate;

    float m_locked_fill_height;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if(CameraRayCast(out var hit))
            {
                float teleport_vertical_offset = 1f;
                GetComponent<IL3DN.IL3DN_SimpleFPSController>().Teleport(hit.point + new Vector3(0, teleport_vertical_offset, 0));
            }
        }

        if(Input.GetKeyDown(KeyCode.F1))
        {
            Game.Instance.GetLiquidMesher().TriangulateAll();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Game.Instance.GetLiquidSimulation().StepOnce(false);
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Game.Instance.GetLiquidSimulation().StepOnce(true);
        }


        UpdateLiquidControl(KeyCode.Q, m_liquid_fill_rate);
        UpdateLiquidControl(KeyCode.E, -m_liquid_remove_rate);


        UpdateDigControl(KeyCode.Mouse0, -m_dig_rate);
        UpdateDigControl(KeyCode.Mouse1, m_fill_rate);
    }

    void UpdateLiquidControl(KeyCode key_code, float amount)
    {
        if (Input.GetKey(key_code))
        {
            if (Input.GetKeyDown(key_code))
            {
                if (CameraRayCast(out var hit))
                {
                    var bias = 1f * hit.normal.y;
                    m_locked_fill_height = hit.point.y + bias;
                }
            }

            var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));

            var ray = GetCameraRay();
            if (plane.Raycast(ray, out var distance))
            {
                var hit_point = ray.GetPoint(distance);
                hit_point.y = m_locked_fill_height;

                Game.Instance.GetLiquidSimulation().AddDensity(hit_point, amount * Time.deltaTime);
            }
        }
    }

    void UpdateDigControl(KeyCode key_code, float amount)
    {
        if (Input.GetKey(key_code))
        {
            if (Input.GetKeyDown(key_code))
            {
                if (CameraRayCast(out var hit))
                {
                    var bias = hit.normal.y * 0.05f;
                    m_locked_fill_height = hit.point.y + bias;
                }
            }

            var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));

            var ray = GetCameraRay();
            if (plane.Raycast(ray, out var distance))
            {
                var hit_point = ray.GetPoint(distance);
                hit_point.y = m_locked_fill_height;

                Game.Instance.GetSolidSimulation().AddDensity(hit_point, amount * Time.deltaTime);
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