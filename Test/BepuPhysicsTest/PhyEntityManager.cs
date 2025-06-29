/*
 * Copyright (c) 2024-2025 XDay
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using BEPUphysics.Entities.Prefabs;
using FixMath.NET;
using UnityEngine;

internal class PhyEntityManager
{
    public event Action<PhyEntity> OnEntityCreated;
    public event Action<PhyEntity> OnEntityDestroyed;

    public PhyEntityManager()
    {
        m_Space = new();
        m_Space.TimeStepSettings.TimeStepDuration = 1 / 30f;
    }

    public void OnDestroy()
    {
        foreach (var entity in m_Entities)
        {
            entity.OnDestroy();
            OnEntityDestroyed?.Invoke(entity);
        }
    }

    public PhyEntity CreateBoxEntity(string name, Vector3 position, Vector3 size, Fix64 mass, bool dynamic)
    {
        var realSize = BepuHelper.ToBEPUVector3(size);
        var box = new Box(BepuHelper.ToBEPUVector3(position), realSize.X, realSize.Y, realSize.Z, mass);
        var entity = new PhyEntity(++m_NextID, name, dynamic, m_Space, box);
        m_Entities.Add(entity);
        OnEntityCreated?.Invoke(entity);
        return entity;
    }

    public PhyEntity CreateSphereEntity(string name, Vector3 position, Fix64 radius, Fix64 mass, bool dynamic)
    {
        var sphere = new Sphere(BepuHelper.ToBEPUVector3(position), radius, mass);
        var entity = new PhyEntity(++m_NextID, name, dynamic, m_Space, sphere);
        m_Entities.Add(entity);
        OnEntityCreated?.Invoke(entity);
        return entity;
    }

    public void DestroyEntity(PhyEntity entity)
    {
        var idx = m_Entities.IndexOf(entity);
        if (idx != -1)
        {
            entity.OnDestroy();
            m_Entities[idx] = m_Entities[^1];
            m_Entities.RemoveAt(m_Entities.Count - 1);
            OnEntityDestroyed?.Invoke(entity);
        }
    }

    public void Update()
    {
        m_Space.Update();
    }

    private int m_NextID;
    private readonly List<PhyEntity> m_Entities = new();
    private readonly BEPUphysics.Space m_Space;
}

