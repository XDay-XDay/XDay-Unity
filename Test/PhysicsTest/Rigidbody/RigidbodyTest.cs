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

using XDay;
using System.Collections.Generic;
using UnityEngine;
using XDay.UtilityAPI;

public class RigidbodyTest : MonoBehaviour
{
    public Material SphereMaterial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_World = new();

        {
            var rect = XDay.Rigidbody.CreateBoxBody("", 10, 1, true, m_LedgeMaterial);
            var entity = new Entity(rect, m_World);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(0, 0, -10));
            entity.SetRotation(0);
        }

        {
            var rect =XDay.Rigidbody.CreateBoxBody("", 10, 1, true, m_LedgeMaterial);
            var entity = new Entity(rect, m_World);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(-8, 0, -15));
            entity.SetRotation(-45.0f);
        }
    }

    private void OnDestroy()
    {
        foreach (var entity in m_Entities)
        {
            entity.OnDestroy();
        }
    }

    // Update is called once per frame
    void Update()
    {
        var pos = Helper.RayCastWithXZPlane(Input.mousePosition, Camera.main);
        if (Input.GetMouseButtonDown(1))
        {
            var circle = XDay.Rigidbody.CreateCircleBody("", 1, false, m_BallMaterial);
            var entity = new Entity(circle, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(pos.x, 0, pos.z));
        }

        if (Input.GetMouseButtonDown(2))
        {
            var body = XDay.Rigidbody.CreateBoxBody("", 2, 2, false, m_CubeMaterial);
            var entity = new Entity(body, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(pos.x, 0, pos.z));
        }

        if (Input.GetKey(KeyCode.A))
        {
            m_Entities[0].AddForce(Vector3.left * m_Force);
        }

        if (Input.GetKey(KeyCode.D))
        {
            m_Entities[0].AddForce(Vector3.right * m_Force);
        }

        if (Input.GetKey(KeyCode.W))
        {
            m_Entities[0].AddForce(Vector3.forward * m_Force);
        }

        if (Input.GetKey(KeyCode.S))
        {
            m_Entities[0].AddForce(Vector3.back * m_Force);
        }

        m_World.Step(new FixedPoint(Time.deltaTime), 5);

        foreach (var entity in m_Entities)
        {
            entity.Sync();
        }
    }

    private PhysicsWorld m_World;
    private List<Entity> m_Entities = new();
    private const float m_Force = 10;
    private PhysicalMaterial m_LedgeMaterial = new PhysicalMaterial()
    {
        Density = 1,
        Restitution = new(0.2f),
        StaticFriction = new(0.5f),
        DynamicFriction = new(0.2f),
    };

    private PhysicalMaterial m_BallMaterial = new PhysicalMaterial()
    {
        Density = 1,
        Restitution = new(0.5f),
        StaticFriction = new(0.5f),
        DynamicFriction = new(0.2f),
    };

    private PhysicalMaterial m_CubeMaterial = new PhysicalMaterial()
    {
        Density = 1,
        Restitution = new(0.1f),
        StaticFriction = new(0.6f),
        DynamicFriction = new(0.5f),
    };
}
