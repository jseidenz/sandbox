using UnityEngine;
using Photon.Compression;

public class PlayerCursor : MonoBehaviour
{
    [SerializeField] public CursorTuning m_cursor_tuning;

    TorusMesh m_cursor_mesh;
    MeshFilter m_mesh_filter;

    [SyncVar]
    public Vector3 m_target_cursor_position;
    [SyncVar]
    public float m_target_cursor_radius;
    Transform m_camera_transform;


    void OnEnable()
    {
        m_cursor_mesh = new TorusMesh(m_cursor_tuning.m_default_radius, m_cursor_tuning.m_default_thickness, 30);
        m_target_cursor_radius = m_cursor_tuning.m_default_radius;
        m_mesh_filter = GetComponent<MeshFilter>();
        m_mesh_filter.sharedMesh = m_cursor_mesh.GetMesh();
        m_camera_transform = Camera.main.transform;
    }

    void OnDisable()
    {
        m_cursor_mesh.Destroy();
        m_cursor_mesh = null;
    }

    void LateUpdate()
    {
        var new_radius = Mathf.Lerp(m_cursor_mesh.GetRadius(), m_target_cursor_radius, m_cursor_tuning.m_radius_lerp_rate);
        m_cursor_mesh.SetRadius(new_radius);
        transform.position = Vector3.Lerp(transform.position, m_target_cursor_position, m_cursor_tuning.m_position_lerp_rate * Time.deltaTime);
        transform.position = m_target_cursor_position;
        transform.up = (m_camera_transform.position - transform.position).normalized;
    }
}