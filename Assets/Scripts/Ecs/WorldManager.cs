using System;
using UnityEngine;
using System.Collections.Generic;


public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    public static WorldManager Initialize()
    {
        if(Instance != null) { throw new Exception($"World manager already exists!"); }

        var world_manager_go = new GameObject("WorldManager");
        Instance = world_manager_go.AddComponent<WorldManager>();
        GameObject.DontDestroyOnLoad(world_manager_go);

        return Instance;
    }

    public void AddWorld(World world)
    {
        if(!m_worlds.Add(world))
        {
            throw new Exception($"World {world.GetType().Name} already exists in world manager.");
        }
    }

    public void RemoveWorld(World world)
    {
        if(!m_worlds.Remove(world))
        {
            throw new Exception($"World {world.GetType().Name} does not exist in world manager.");
        }
    }

    void FixedUpdate()
    {
        float dt = Time.deltaTime;
        foreach (var world in m_worlds)
        {
            world.FixedUpdate(dt);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach(var world in m_worlds)
        {
            world.Update(dt);
        }
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        foreach (var world in m_worlds)
        {
            world.LateUpdate(dt);
        }
    }


    HashSet<World> m_worlds = new HashSet<World>();
}