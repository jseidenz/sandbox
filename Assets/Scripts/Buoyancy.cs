using UnityEngine;
using System.Threading.Tasks;

public class Buoyancy : MonoBehaviour 
{
    [SerializeField] float m_amount;
    [SerializeField] float m_rate;

    float m_original_y;

    void Start()
    {
        m_original_y = transform.position.y;
    }

    void Update()
    {
        var pos = transform.position;
        pos.y = m_original_y + Mathf.Sin(Time.time * m_rate) * m_amount;
        transform.position = pos;
    }
}