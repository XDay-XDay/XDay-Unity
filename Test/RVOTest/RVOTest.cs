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
using System.IO;
using Assets.XDay.Test.RVOTest;
using RVOFixed;
using UnityEngine;
using XDay;
using XDay.CameraAPI;
using XDay.InputAPI;
using XDay.UtilityAPI;

public class RVOTest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupScenario();

        m_Target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m_Target.name = "Target";

        m_DeviceInput = IDeviceInput.Create();
        var setup = new CameraSetup("RVO Test");
        setup.Load(File.ReadAllText("Assets/XDay/Test/RVOTest/CameraSetup.bytes"));
        m_CameraManipulator = ICameraManipulator.Create(Camera.main, setup, m_DeviceInput);
        m_CameraManipulator.SetActive(true);
        m_CameraManipulator.EnableFocusPointClamp = false;
    }

    // Update is called once per frame
    void Update()
    {
        m_DeviceInput.Update();

        Simulator.Instance.doStep();
        for (var i = m_Agents.Count - 1; i >= 0; --i)
        {
            bool removed = m_Agents[i].Update();
            if (removed)
            {
                m_Agents.RemoveAt(i);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            var pos = Helper.RayCastWithXZPlane(Input.mousePosition, m_CameraManipulator.Camera);
            m_Target.transform.position = pos;
        }
        foreach (var agent in m_Agents)
        {
            var dir = m_Target.transform.position - agent.GetPosition();
            dir.Normalize();
            agent.SetVelocity(dir);
        }
    }

    private void LateUpdate()
    {
        m_CameraManipulator.Update();
    }

    private void SetupScenario()
    {
        /* Specify the global time step of the simulation. */
        Simulator.Instance.setTimeStep((FixedPoint)(1/60.0f));

        /*
         * Specify the default parameters for agents that are subsequently
         * added.
         */
        Simulator.Instance.setAgentDefaults((FixedPoint)3.0f, 5, (FixedPoint)10.0f, (FixedPoint)10.0f, (FixedPoint)0.5f, m_MaxSpeed, FixedVector2.Zero);

        float radius = 30;
        int n = 10;
        for (int i = 0; i < n; ++i)
        {
            float x = radius * (float)Math.Cos(i * 2.0f * Math.PI / n);
            float z = radius * (float)Math.Sin(i * 2.0f * Math.PI / n);
            var agent = new RVOAgent(x, z);
            m_Agents.Add(agent);
        }

        Simulator.Instance.processObstacles();
    }

    private List<RVOAgent> m_Agents = new();
    private IDeviceInput m_DeviceInput;
    private ICameraManipulator m_CameraManipulator;
    private GameObject m_Target;
    private FixedPoint m_MaxSpeed = 6;
}
