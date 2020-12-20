using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;

public class NameScreen : MonoBehaviour
{
    [SerializeField] NameWidget m_name_widget_prefab;

    public static NameScreen Instance;
    public float m_name_height;

    Dictionary<PlayerName, NameWidget> m_player_names = new Dictionary<PlayerName, NameWidget>();

    void OnEnable()
    {
        Instance = this;

        m_name_widget_prefab.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        Instance = null;
    }

    public void Register(PlayerName player_name)
    {
        if (player_name.gameObject == Game.Instance.GetPlayerAvatar()) return;

        var widget = GameObject.Instantiate(m_name_widget_prefab);
        widget.GetComponent<RectTransform>().SetParent(m_name_widget_prefab.transform.parent);
        widget.transform.localPosition = new Vector3(0, m_name_height, 0);
        widget.transform.localRotation = Quaternion.identity;
        widget.transform.localScale = Vector3.one;
        widget.m_text.text = player_name.GetComponent<Photon.Pun.PhotonView>().Owner.NickName;
        widget.gameObject.SetActive(true);
        m_player_names[player_name] = widget;
    }

    void Update()
    {
        var camera_pos = Game.Instance.GetCamera().transform.position;
        foreach(var kvp in m_player_names)
        {
            if (kvp.Key == null) continue;
            kvp.Value.transform.position = kvp.Key.transform.position + new Vector3(0, m_name_height, 0);

            var dir = (camera_pos = kvp.Value.transform.position).normalized;
            kvp.Value.transform.forward = dir;
        }
    }

    public void Unregister(PlayerName player_name)
    {
        if(m_player_names.TryGetValue(player_name, out var widget))
        {
            m_player_names.Remove(player_name);
            GameObject.Destroy(widget.gameObject);
        }
        
    }
}