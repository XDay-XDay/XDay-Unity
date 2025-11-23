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
            m_Root = loader.LoadGameObject(region.FullPrefabPath);
            if (m_Root != null)
            {
                m_Root.transform.localPosition = region.Position;
                m_Root.transform.SetParent(parent, false);

                m_BorderLOD0 = m_Root.transform.Find("LOD0Border").gameObject;
                m_BorderLOD1 = m_Root.transform.Find("LOD1Border").gameObject;
                m_MeshLOD1 = m_Root.transform.Find("LOD1Mesh").gameObject;
                Debug.Assert(m_BorderLOD0 != null && m_BorderLOD1 != null && m_MeshLOD1 != null);

                m_BorderLOD0Renderer = m_BorderLOD0.GetComponent<MeshRenderer>();
                m_BorderLOD1Renderer = m_BorderLOD1.GetComponent<MeshRenderer>();
                m_MeshLOD1Renderer = m_MeshLOD1.GetComponent<MeshRenderer>();

                m_Root.SetActive(region.Active);
                SetColor(Color.white);
            }
        }

        public void OnDestroy()
        {
            Helper.DestroyUnityObject(m_Root);
            m_Root = null;
        }

        internal void SetActive(bool active)
        {
            if (m_Root != null)
            {
                m_Root.SetActive(active);
            }
        }

        internal void SetColor(Color color)
        {
            if (m_Root != null)
            {
                m_BorderLOD0Renderer.sharedMaterial.color = color;
            }
        }

        internal void SetBorderLOD0Material(Material material)
        {
            m_BorderLOD0Renderer.sharedMaterial = material;
        }

        internal void SetBorderLOD1Material(Material material)
        {
            m_BorderLOD1Renderer.sharedMaterial = material;
        }

        internal void SetMeshLOD1Material(Material material)
        {
            m_MeshLOD1Renderer.sharedMaterial = material;
        }

        internal void SetLOD(int lod)
        {
            if (lod == 0)
            {
                m_BorderLOD0.SetActive(true);
                m_BorderLOD1.SetActive(false);
                m_MeshLOD1.SetActive(false);
            }
            else
            {
                m_BorderLOD0.SetActive(false);
                m_BorderLOD1.SetActive(true);
                m_MeshLOD1.SetActive(true);
            }
        }

        private GameObject m_Root;
        private GameObject m_BorderLOD0;
        private GameObject m_BorderLOD1;
        private GameObject m_MeshLOD1;
        private MeshRenderer m_BorderLOD0Renderer;
        private MeshRenderer m_BorderLOD1Renderer;
        private MeshRenderer m_MeshLOD1Renderer;
    }
}