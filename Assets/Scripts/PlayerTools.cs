using UnityEngine;
public abstract class Tool
{
    public Tool(KeyCode key_code)
    {
        m_key_code = key_code;
    }

    public virtual bool TryStartUsing() { return true; }

    public virtual void OnEnable() { }

    public virtual void OnDisable() { }

    public virtual void Update(float dt) { }

    public virtual void LateUpdate(float dt) { }

    public KeyCode GetKeyCode() { return m_key_code; }

    public Camera camera { get; set; }
    public Transform transform{ get; set; }
    public GameObject gameObject { get => transform.gameObject; }

    public float m_raycast_distance { get; set; }

    protected bool CameraRayCast(out RaycastHit hit)
    {
        var ray = GetCameraRay();
        return Physics.Raycast(ray, out hit, m_raycast_distance);
    }

    protected Ray GetCameraRay()
    {
        return Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    KeyCode m_key_code;
}

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

public class PlayerTools : MonoBehaviour 
{
    [SerializeField] float m_fill_rate;
    [SerializeField] float m_dig_rate;
    [SerializeField] float m_raycast_distance;

    [SerializeField] float m_liquid_fill_rate;
    [SerializeField] Material m_dig_cursor_material;

    float m_locked_fill_height;


    TorusMesh m_cursor_mesh;


    Tool m_default_tool;
    Tool m_active_tool;
    Tool[] m_tools;


    void Awake()
    {
        if(!GetComponent<Photon.Pun.PhotonView>().IsMine)
        {
            GameObject.Destroy(this);
        }
    }

    void SetActiveTool(Tool tool)
    {
        if(m_active_tool != null)
        {
            m_active_tool.OnDisable();
        }

        m_active_tool = tool;
        if (m_active_tool != null)
        {
            m_active_tool.OnEnable();
        }
    }

    void OnEnable()
    {
        m_cursor_mesh = new TorusMesh(1, 0.3f, 30);

        m_default_tool = new DefaultTool();
        SetActiveTool(m_default_tool);
        
        m_tools = new Tool[]
        {
            m_default_tool,
            new DigTool(KeyCode.Mouse0, -m_dig_rate),
            new DigTool(KeyCode.Mouse1, m_fill_rate),
            new SprayTool(KeyCode.E, m_liquid_fill_rate),
        };

        var camera = Camera.main;
        foreach (var tool in m_tools)
        {
            tool.transform = transform;
            tool.camera = camera;
            tool.m_raycast_distance = m_raycast_distance;
        }

        

    }

    void OnDisable()
    {
        SetActiveTool(null);

        m_cursor_mesh.Destroy();
        m_cursor_mesh = null;
    }

    void Update()
    {
        bool is_default_tool_set = m_active_tool == m_default_tool;
        if(is_default_tool_set)
        {
            foreach(var tool in m_tools)
            {
                if(Input.GetKey(tool.GetKeyCode()) && tool.TryStartUsing())
                {
                    SetActiveTool(tool);
                    break;
                }
            }
        }
        else if(m_active_tool != null)
        {
            if(!Input.GetKey(m_active_tool.GetKeyCode()))
            {
                SetActiveTool(m_default_tool);
            }
        }

        if(m_active_tool != null)
        {
            m_active_tool.Update(Time.deltaTime);
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.T))
        {
            if(CameraRayCast(out var hit))
            {
                float teleport_vertical_offset = 1f;
                GetComponent<IL3DN.IL3DN_SimpleFPSController>().Teleport(hit.point + new Vector3(0, teleport_vertical_offset, 0));
            }
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

        UpdateLiquidControl(KeyCode.Q, m_liquid_fill_rate);
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

    void LateUpdate()
    {
        if(m_active_tool != null)
        {
                m_active_tool.LateUpdate(Time.deltaTime);
        }
    }

    Ray GetCameraRay()
    {
        return Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }

    bool CameraRayCast(out RaycastHit hit)
    {
        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        return Physics.Raycast(ray, out hit, m_raycast_distance);
    }
}