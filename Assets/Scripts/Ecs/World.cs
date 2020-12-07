using System;
using System.Collections.Generic;


public class World
{
    public void AddService<T>(T service) where T : Service
    {
        if (m_type_to_service_map.ContainsKey(typeof(T))) { throw new Exception($"Service {typeof(T).Name} already exists in world {GetType().Name}"); }

        m_type_to_service_map[typeof(T)] = service;
        m_services.Add(service);

        service.SetWorld(this);
        service.OnAddedToWorld();
    }

    internal void FixedUpdate(float dt)
    {
        foreach (var service in m_services)
        {
            service.FixedUpdate(dt);
        }
    }

    internal void Update(float dt)
    {
        foreach (var service in m_services)
        {
            service.Update(dt);
        }
    }

    internal void LateUpdate(float dt)
    {
        foreach (var service in m_services)
        {
            service.LateUpdate(dt);
        }
    }

    public T GetService<T>() where T : Service
    {
        if(!m_type_to_service_map.TryGetValue(typeof(T), out var service))
        {
            throw new Exception($"Could not find service {typeof(T).Name} in world {GetType().Name}");
        }

        return (T)service;
    }

    Dictionary<Type, Service> m_type_to_service_map = new Dictionary<Type, Service>();
    List<Service> m_services = new List<Service>();
}