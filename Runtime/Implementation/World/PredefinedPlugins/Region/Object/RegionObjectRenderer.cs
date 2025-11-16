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
using XDay.AssetAPI;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Region
{
    internal class RegionObjectRenderer
    {
        public RegionObjectRenderer(RegionObject region, IAssetLoader loader, Transform parent)
        {
            m_GameObject = loader.LoadGameObject(region.LOD0PrefabPath);
            if (m_GameObject != null)
            {
                m_GameObject.transform.localPosition = region.Position;
                m_GameObject.transform.SetParent(parent, false);
                m_GameObject.SetActive(region.Active);
                SetColor(region.Color);
            }
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_GameObject);
            m_GameObject = null;
        }

        internal void SetActive(bool active)
        {
            if (m_GameObject != null)
            {
                m_GameObject.SetActive(active);
            }
        }

        internal void SetColor(Color color)
        {
            if (m_GameObject != null)
            {
                m_GameObject.GetComponent<MeshRenderer>().sharedMaterial.color = color;
            }
        }

        private GameObject m_GameObject;
    }
}