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

public class ControllerTest : MonoBehaviour
{
    public Material SphereMaterial;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_World = new(null);

        {
            var rect = XDay.Rigidbody.CreateBoxBody("",10, 1, true, m_LedgeMaterial, m_World);
            var entity = new Entity(rect, m_World);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(10, 0, 0));
            entity.SetRotation(90);
        }

        {
            var rect = XDay.Rigidbody.CreateBoxBody("", 10, 1, true, m_LedgeMaterial, m_World);
            var entity = new Entity(rect, m_World);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(-10, 0, 0));
            entity.SetRotation(90);
        }

        {
            m_Sphere = XDay.Rigidbody.CreateCircleBody("", 1, false, m_BallMaterial, m_World);
            m_Sphere.EnableGravity = false;
            m_Sphere.IsKinematic = true;
            var entity = new Entity(m_Sphere, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(Vector3.zero);
        }

        {
            var s = XDay.Rigidbody.CreateCircleBody("", 2, false, m_BallMaterial, m_World);
            s.EnableGravity = false;
            s.IsKinematic = false;
            var entity = new Entity(s, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(4, 0, 0));
        }

        {
            var s1 = XDay.Rigidbody.CreateCircleBody("", 1, false, m_BallMaterial, m_World);
            s1.EnableGravity = false;
            s1.IsKinematic = true;
            var entity = new Entity(s1, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(0, 0, 5));
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
            var circle = XDay.Rigidbody.CreateCircleBody("", 1, false, m_BallMaterial, m_World);
            var entity = new Entity(circle, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(pos.x, 0, pos.z));
        }

        if (Input.GetMouseButtonDown(2))
        {
            var body = XDay.Rigidbody.CreateBoxBody("", 2, 2, false, m_CubeMaterial, m_World);
            var entity = new Entity(body, m_World);
            entity.SetMaterial(SphereMaterial);
            m_Entities.Add(entity);
            entity.SetPosition(new Vector3(pos.x, 0, pos.z));
        }

#if false
        if (Input.GetKey(KeyCode.A))
        {
            m_Sphere.AddImpulse(FixedVector2.Left * m_Force);
        }

        if (Input.GetKey(KeyCode.D))
        {
            m_Sphere.AddImpulse(FixedVector2.Right * m_Force);
        }

        if (Input.GetKey(KeyCode.W))
        {
            m_Sphere.AddImpulse(FixedVector2.Up * m_Force);
        }

        if (Input.GetKey(KeyCode.S))
        {
            m_Sphere.AddImpulse(FixedVector2.Down * m_Force);
        }
#else
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        var dir = new FixedVector2(x, y);
        dir.Normalize();
        m_Sphere.LinearVelocity = dir * m_Speed;
#endif

        m_World.Step(new FixedPoint(Time.deltaTime), 1);

        foreach (var entity in m_Entities)
        {
            entity.Sync();
        }
    }

    private PhysicsWorld m_World;
    private List<Entity> m_Entities = new();
    private FixedPoint m_Force = new(10);
    private FixedPoint m_Speed = new(5);
    private XDay.Rigidbody m_Sphere;
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
