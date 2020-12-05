using UnityEngine;
using System.Threading.Tasks;

public class DigTool : MonoBehaviour 
{
    [SerializeField] float m_fill_rate;
    [SerializeField] float m_dig_rate;

    float m_locked_fill_height;
    
    void Update()
    {
        if(Input.GetKey(KeyCode.Mouse0))
        {
            var ray = GetCameraRay();
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                VoxelWorld.Instance.AddDensity(hit.point, -m_dig_rate * Time.deltaTime);
            }
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            var ray = GetCameraRay();
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {                
                RaycastHit hit;                

                if (Physics.Raycast(ray, out hit))
                {
                    m_locked_fill_height = hit.point.y + VoxelWorld.Instance.GetVoxelSizeInMeters();
                }
            }

            var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));
            if(plane.Raycast(ray, out var distance))
            {
                var hit_point = ray.GetPoint(distance);
                hit_point.y = m_locked_fill_height;
                VoxelWorld.Instance.AddDensity(hit_point, m_fill_rate * Time.deltaTime);
            }
        }
    }

    Ray GetCameraRay()
    {
        return Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }
}