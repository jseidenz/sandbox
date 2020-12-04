using UnityEngine;
using System.Threading.Tasks;

public class DigTool : MonoBehaviour 
{
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

        if(Input.GetKey(KeyCode.Mouse1))
        {
            var camera = Camera.main;
            RaycastHit hit;
            var ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                VoxelWorld.Instance.AddDensity(hit.point, 1.0f * Time.deltaTime);
            }
        }
    }
}