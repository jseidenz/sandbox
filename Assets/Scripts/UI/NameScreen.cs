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
        widget.m_owner = player_name.GetComponent<Photon.Pun.PhotonView>().Owner;
        widget.m_text.text = widget.m_owner.NickName;
        widget.gameObject.SetActive(true);
        m_player_names[player_name] = widget;
    }

    void Update()
    {
        var camera_transform = Game.Instance.GetCamera().transform;
        foreach(var kvp in m_player_names)
        {
            if (kvp.Key == null) continue;
            if (kvp.Value.m_owner == null) continue;

            var nick_name = kvp.Value.m_owner.NickName;
            if (nick_name != kvp.Value.m_text.text)
            {
                kvp.Value.m_text.text = nick_name;
            }

            var widget_transform = kvp.Value.transform;
            var avatar_pos = kvp.Key.transform.position;

            widget_transform.position = avatar_pos + new Vector3(0, m_name_height, 0);
            widget_transform.LookAt(widget_transform.position + camera_transform.rotation * Vector3.forward, camera_transform.rotation * Vector3.up);
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