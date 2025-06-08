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
using UnityEditor;
using UnityEditor.AI;
using UnityEngine;
using UnityEngine.AI;
using XDay.NavigationAPI;
using XDay.NavigationAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Navigation.Editor
{
    internal partial class NavigationPluginRenderer
    {
        public GameObject Root => m_Root;
        public MyNavigationSystemDebugger Debugger => m_Debugger;

        public NavigationPluginRenderer(Transform parent, NavigationPlugin plugin)
        {
            m_Root = new GameObject(plugin.Name);
            m_Debugger = m_Root.AddComponent<MyNavigationSystemDebugger>();
            m_Debugger.Show = plugin.ShowDebugInfo;
            m_Root.transform.SetParent(parent, true);
            Selection.activeGameObject = m_Root;
        }

        public void OnDestroy()
        {
            m_LastBuiltNavMesh?.OnDestroy();
            Helper.DestroyUnityObject(m_Root);
        }

        public void CreateNavMeshRenderer(NavMeshBuildResult result)
        {
            if (result == null)
            {
                return;
            }
            m_LastBuiltNavMesh?.OnDestroy();
            var colors = CreateColors(result.MeshIndices, result.TriangleTypes, result.MeshVertices.Length);
            m_LastBuiltNavMesh = new NavMeshRenderer("NavMesh", result.MeshVertices, result.MeshIndices, colors, m_Root.transform);
        }

        public void ClearNavMesh()
        {
            m_LastBuiltNavMesh?.OnDestroy();
            m_LastBuiltNavMesh = null;
        }

        private Color[] CreateColors(int[] indices, ushort[] triangleTypes, int length)
        {
            Color[] colors = new Color[length];
            for (var t = 0; t < triangleTypes.Length; t++)
            {
                var v0 = indices[t * 3];
                var v1 = indices[t * 3 + 1];
                var v2 = indices[t * 3 + 2];

                var color = GetAreaColor(triangleTypes[t]);
                colors[v0] = color;
                colors[v1] = color;
                colors[v2] = color;
            }
            return colors;
        }

        /// <summary>
        /// unity内部color实现
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Color GetAreaColor(int i)
        {
            if (i == 0)
                return new Color(0, 0.75f, 1.0f, 0.5f);
            int r = (Bit(i, 4) + Bit(i, 1) * 2 + 1) * 63;
            int g = (Bit(i, 3) + Bit(i, 2) * 2 + 1) * 63;
            int b = (Bit(i, 5) + Bit(i, 0) * 2 + 1) * 63;
            return new Color(r / 255.0f, g / 255.0f, b / 255.0f, 0.5f);
        }

        private int Bit(int a, int b)
        {
            return (a & (1 << b)) >> b;
        }

        private readonly GameObject m_Root;
        private NavMeshRenderer m_LastBuiltNavMesh;
        private readonly MyNavigationSystemDebugger m_Debugger;
    }
}

//XDay