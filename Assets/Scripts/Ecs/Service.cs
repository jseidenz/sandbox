using System;
using UnityEngine;
using UnityEngine.UI;


public class Service
{
    internal virtual void OnAddedToWorld() { }
    internal virtual void FixedUpdate(float dt) { }
    internal virtual void Update(float dt) { }
    internal virtual void LateUpdate(float dt) { }

    public T GetService<T>() where T : Service { return m_world.GetService<T>(); }

    internal void SetWorld(World world) { m_world = world; }

    protected World m_world;
}