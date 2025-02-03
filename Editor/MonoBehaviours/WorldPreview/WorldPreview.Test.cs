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

#if UNITY_EDITOR


using System;
using System.Collections.Generic;
using UnityEngine;
using XDay;
using XDay.CameraAPI;
using XDay.UtilityAPI;
using XDay.WorldAPI;

internal partial class WorldPreview
{
    public Camera Camera;
    public float MoveOffset = 10;
    public float Radius = 5;
    public string AssetPath;

    private void UpdateTests()
    {
        MoveCamera();
        DecorationSystemTest();
        AddressableTest();
        LogTest();
    }

    private void MoveCamera()
    {
        if (m_XDay.WorldManager == null)
        {
            return;
        }

        var world = m_XDay.WorldManager.FirstWorld;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (world == null)
            {
                m_XDay.WorldManager.LoadWorld("");
            }
            else
            {
                m_XDay.WorldManager.UnloadWorld(world.Name);
            }
        }

        if (world == null)
        {
            return;
        }

        var manipulator = world.CameraManipulator;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(-MoveOffset, 0, 0));
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(MoveOffset, 0, 0));
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(0, 0, MoveOffset));
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                manipulator.SetPosition(manipulator.RenderPosition + new Vector3(0, 0, -MoveOffset));
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            var param = new FocusParam(new Vector3(50, 0, 100), 30);
            manipulator.Focus(param);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            manipulator.Shake(2, 10, 0.2f, true);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            manipulator.SetPosition(new Vector3(110, 30, 120));
        }
    }

    private void DecorationSystemTest()
    {
        if (Input.GetMouseButtonDown(1))
        {
            var world = m_XDay.WorldManager.FirstWorld;
            if (world != null)
            {
                var pos = Helper.RayCastWithXZPlane(Input.mousePosition, world.CameraManipulator.Camera);
                var decorationSystem = world.QueryPlugin<IDecorationSystem>();
                if (decorationSystem != null)
                {
                    List<int> decorationIDs = new();
                    decorationSystem.QueryDecorationIDsInCircle(pos, Radius, decorationIDs);
                    var circle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    circle.transform.localScale = Vector3.one * Radius * 2;
                    circle.transform.position = pos;
                    foreach (var id in decorationIDs)
                    {
                        //decorationSystem.ShowDecoration(id, false);
                        decorationSystem.PlayAnimation(id, "Drunk Walk");
                    }
                }
            }
        }
    }

    private async void AddressableTest()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            var inst = await m_XDay.WorldAssetLoader.LoadGameObjectAsync(AssetPath);
            inst.name += "_addressable";
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            m_XDay.WorldAssetLoader.UnloadAsset(AssetPath);
        }
    }

    private void LogTest()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            int h = 3;
            Log.Instance?.Info($"Log {h}");
            Log.Instance?.Warning($"LogWarning");
            Log.Instance?.Error($"LogError {1}");
            try
            {
                throw new NotImplementedException("not impl");
            }
            catch (Exception e)
            {
                Log.Instance?.Exception(e);
            }
        }
    }

}


#endif