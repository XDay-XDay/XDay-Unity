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

using UnityEngine;
using System.Collections.Generic;

namespace XDay.UtilityAPI.Editor
{
    public class LayerChanger
    {
        public void Start(GameObject gameObject, int layer)
        {
            foreach (Transform trans in gameObject.GetComponentsInChildren<Transform>(true))
            {
                m_LayerChangedGameObjects[trans.gameObject.GetInstanceID()] = trans.gameObject.layer;
                trans.gameObject.layer = layer;
            }
        }

        public void Stop(GameObject gameObject)
        {
            foreach (Transform trans in gameObject.GetComponentsInChildren<Transform>(true))
            {
                bool found = m_LayerChangedGameObjects.TryGetValue(trans.gameObject.GetInstanceID(), out var originalLayer);
                Debug.Assert(found);
                trans.gameObject.layer = originalLayer;
            }

            m_LayerChangedGameObjects.Clear();
        }

        private readonly Dictionary<int, int> m_LayerChangedGameObjects = new();
    }
}
