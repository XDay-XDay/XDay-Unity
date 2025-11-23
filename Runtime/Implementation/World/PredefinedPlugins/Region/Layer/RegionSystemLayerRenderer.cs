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

using System.Collections.Generic;
using UnityEngine;
using XDay.AssetAPI;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.Region
{
    internal class RegionSystemLayerRenderer
    {
        public RegionSystemLayerRenderer(RegionSystem.Layer layer, Transform parent, float height)
        {
            m_Layer = layer;

            m_Root = new GameObject(layer.Name);
            m_Root.transform.parent = parent;
            m_Root.transform.position = new Vector3(layer.Origin.x, height, layer.Origin.y);

            InitRegionRenderers();
        }

        public void OnDestroy()
        {
            foreach (var kv in m_Renderers)
            {
                kv.Value.OnDestroy();
            }

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;
        }

        private void InitRegionRenderers()
        {
            var loader = m_Layer.System.World.AssetLoader;
            foreach (var region in m_Layer.RegionList)
            {
                AddRenderer(region, loader);
            }
        }

        private void AddRenderer(RegionObject region, IAssetLoader loader)
        {
            var renderer = new RegionObjectRenderer(region, loader, m_Root.transform);
            m_Renderers.Add(region.ConfigID, renderer);
        }

        internal void OnActiveStateChange(RegionObject region)
        {
            if (m_Renderers.TryGetValue(region.ConfigID, out var renderer))
            {
                renderer.SetActive(region.Active);
                if (region.Active)
                {
                    renderer.SetLOD(m_Layer.System.CurrentLOD);
                }
            }
        }

        internal void OnRegionBorderLOD0ColorChange(RegionObject region)
        {
            if (m_Renderers.TryGetValue(region.ConfigID, out var renderer))
            {
                renderer.SetColor(region.Color);
            }
        }

        internal void OnRegionBorderLOD0MaterialChange(RegionObject region, Material material)
        {
            if (m_Renderers.TryGetValue(region.ConfigID, out var renderer))
            {
                renderer.SetBorderLOD0Material(material);
            }
        }

        internal void OnRegionBorderLOD1MaterialChange(RegionObject region, Material material)
        {
            if (m_Renderers.TryGetValue(region.ConfigID, out var renderer))
            {
                renderer.SetBorderLOD1Material(material);
            }
        }

        internal void OnRegionMeshLOD1MaterialChange(RegionObject region, Material material)
        {
            if (m_Renderers.TryGetValue(region.ConfigID, out var renderer))
            {
                renderer.SetMeshLOD1Material(material);
            }
        }

        private GameObject m_Root;
        private readonly RegionSystem.Layer m_Layer;
        private readonly Dictionary<int, RegionObjectRenderer> m_Renderers = new();
    }
}
