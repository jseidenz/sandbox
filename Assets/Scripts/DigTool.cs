using UnityEngine;
using System.Threading.Tasks;

public class DigTool : MonoBehaviour 
{
    float m_locked_fill_height;
    void Update()
    {
        if(Input.GetKey(KeyCode.Mouse0))
        {
            var camera = Camera.main;
            RaycastHit hit;
            var ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                VoxelWorld.Instance.AddDensity(hit.point, -1.0f * Time.deltaTime);
            }
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            var camera = Camera.main;
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {                
                RaycastHit hit;                

                if (Physics.Raycast(ray, out hit))
                {
                    m_locked_fill_height = hit.point.y + VoxelLayer.VOXEL_HEIGHT;
                }
            }

            var plane = new Plane(Vector3.up, new Vector3(0, m_locked_fill_height, 0));
            if(plane.Raycast(ray, out var distance))
            {
                var hit_point = ray.GetPoint(distance);
                hit_point.y = m_locked_fill_height;
                VoxelWorld.Instance.AddDensity(hit_point, 1.0f * Time.deltaTime);
            }
        }
    }
}