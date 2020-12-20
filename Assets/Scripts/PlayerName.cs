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
        NameScreen.Instance.Unregister(this);
    }
}