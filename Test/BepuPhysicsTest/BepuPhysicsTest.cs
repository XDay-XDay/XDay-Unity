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

using FixMath.NET;
using UnityEngine;

public class BepuPhysicsTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_EntityManager = new();
        m_EntityRendererManager = new(m_EntityManager);

        m_Player = m_EntityManager.CreateSphereEntity("Player", new Vector3(0, 0, 0.5f), 0.5f, 1, true);
        var box1 = m_EntityManager.CreateBoxEntity("Box1", new Vector3(4, 0, 0), new Vector3(1, 1, 1), 1, true);
        var box2 = m_EntityManager.CreateBoxEntity("Box2", new Vector3(4, 5, 0), new Vector3(2, 2, 2), 1, false);
        var sphere1 = m_EntityManager.CreateSphereEntity("Sphere1", new Vector3(4, -3, 0), 2, 1, false);
    }

    private void Update()
    {
        m_Dir = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            m_Dir += Vector3.up;
        }
        if (Input.GetKey(KeyCode.S))
        {
            m_Dir += Vector3.down;
        }
        if (Input.GetKey(KeyCode.A))
        {
            m_Dir += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            m_Dir += Vector3.right;
        }

        if (m_Dir != Vector3.zero)
        {
            m_Dir.Normalize();
        }
        m_EntityRendererManager.Update();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_Player.SetVelocity(BepuHelper.ToBEPUVector3(m_Dir) * m_Speed);
        m_EntityManager.Update();
    }

    private Vector3 m_Dir;
    private Fix64 m_Speed = 3;
    private PhyEntity m_Player;
    private PhyEntityManager m_EntityManager;
    private PhyEntityRendererManager m_EntityRendererManager;
}
