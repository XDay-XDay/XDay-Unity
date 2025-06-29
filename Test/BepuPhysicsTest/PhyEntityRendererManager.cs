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
using UnityEngine;

internal class PhyEntityRendererManager
{
    public PhyEntityRendererManager(PhyEntityManager entityManager)
    {
        m_EntityManager = entityManager;

        m_EntityManager.OnEntityCreated += OnEntityCreated;
        m_EntityManager.OnEntityDestroyed += OnEntityDestroyed;
    }

    public void OnDestroy()
    {
        m_EntityManager.OnEntityCreated -= OnEntityCreated;
        m_EntityManager.OnEntityDestroyed -= OnEntityDestroyed;
    }

    private void OnEntityCreated(PhyEntity entity)
    {
        GameObject gameObject = null;
        if (entity.Handle is Box box)
        {
            gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.transform.localScale = new Vector3((float)box.Width, (float)box.Height, (float)box.Length);
        }
        else if (entity.Handle is Sphere sphere)
        {
            gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.transform.localScale = (float)sphere.Radius * 2 * Vector3.one;
        }
        else
        {
            Debug.Assert(false, $"unknown entity type {entity.Handle}");
        }
        var renderer = new PhyEntityRenderer(gameObject, entity);
        m_EntityRenderers.Add(entity.ID, renderer);
    }

    private void OnEntityDestroyed(PhyEntity entity)
    {
        if (m_EntityRenderers.TryGetValue(entity.ID, out var renderer))
        {
            renderer.OnDestroy();
            m_EntityRenderers.Remove(entity.ID);
        }
    }

    public void Update()
    {
        foreach (var kv in m_EntityRenderers)
        {
            kv.Value.Sync();
        }
    }

    private readonly PhyEntityManager m_EntityManager;
    private Dictionary<int, PhyEntityRenderer> m_EntityRenderers = new();
}

