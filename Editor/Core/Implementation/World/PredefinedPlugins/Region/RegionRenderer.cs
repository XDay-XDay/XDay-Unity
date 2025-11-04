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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Region.Editor
{
    internal class RegionRenderer
    {
        public GameObject Root => m_Root;
        public Vector3 BuildingPosition => m_Building.transform.position;
        public GameObject BuildingGameObject => m_Building;

        public RegionRenderer(RegionObject region, Transform parent)
        {
            m_RegionObject = region;
            m_Root = new GameObject(region.Name);
            m_Root.AddOrGetComponent<NoKeyDeletion>();
            m_Root.transform.SetParent(parent, false);

            m_Building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            m_Building.AddComponent<NoKeyDeletion>();
            m_Building.transform.SetParent(m_Root.transform);
            m_Building.transform.position = region.BuildingPosition;
            m_Building.transform.localScale = region.Layer.GridWidth * Vector3.one;
            m_Building.name = region.Name;
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Root);
        }

        public void SetActive(bool active)
        {
            m_Root.SetActive(active);
        }

        internal void SetDirty()
        {
        }

        internal void Draw(bool v)
        {
        }

        private GameObject m_Root;
        private GameObject m_Building;
        private readonly RegionObject m_RegionObject;
    }
}
