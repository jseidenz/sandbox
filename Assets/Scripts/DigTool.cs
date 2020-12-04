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
            RaycastHit hit;
            var ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if(Input.GetKeyDown(KeyCode.Mouse1))
                {
                    m_locked_fill_height = hit.point.y + VoxelLayer.VOXEL_HEIGHT;
                }

                var hit_point = hit.point;
                hit_point.y = m_locked_fill_height;

                VoxelWorld.Instance.AddDensity(hit_point, 1.0f * Time.deltaTime);
            }
        }
    }
}