using UnityEngine;
using System.Collections.Generic;

public class PlayerName : MonoBehaviour
{
    void Start()
    {
        NameScreen.Instance.Register(this);
    }

    void OnDestroy()
    {
        if (NameScreen.Instance != null)
        {
            NameScreen.Instance.Unregister(this);
        }
    }
}