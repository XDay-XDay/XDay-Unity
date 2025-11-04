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

using System.Buffers;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemRenderer
    {
        public GameObject Root => m_Root;
        public ArrayPool<Color32> Pool => m_Pool;

        public RegionSystemRenderer(Transform parent, RegionSystem system)
        {
            m_Root = new GameObject(system.Name);
            m_Root.transform.SetParent(parent, true);
            Selection.activeGameObject = m_Root;
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Root);
        }

        private readonly GameObject m_Root;
        private readonly ArrayPool<Color32> m_Pool = ArrayPool<Color32>.Create();
    }
}

//XDay