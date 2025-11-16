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

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using XDay.UtilityAPI;
using System.Buffers;

namespace XDay.WorldAPI.Attribute.Editor
{
    public partial class AttributeSystemRenderer
    {
        public AttributeSystem AttributeSystem => m_AttributeSystem;
        public GameObject Root => m_Root;
        public ArrayPool<Color32> Pool => m_Pool;

        public AttributeSystemRenderer(AttributeSystem system)
        {
            m_AttributeSystem = system;
            m_Root = new GameObject(system.Name);
            m_Root.AddComponent<NoKeyDeletion>();
            m_Root.transform.SetParent(system.World.Root.transform, true);
            m_Root.transform.SetSiblingIndex(system.ObjectIndex);
            m_Root.tag = "EditorOnly";
            m_Root.transform.position = new Vector3(0, 0.1f, 0);
            Selection.activeGameObject = m_Root;

            foreach (var layer in system.Layers)
            {
                var renderer = new LayerRenderer(layer, m_Root.transform, m_Pool);
                m_LayerRenderers.Add(renderer);
                ToggleVisibility(layer);
            }
        }

        public void Uninitialize()
        {
            foreach (var layer in m_LayerRenderers)
            {
                layer.Uninitialize();
            }

            Helper.DestroyUnityObject(m_Root);
            m_Root = null;
        }

        public void UpdateGrid(int layerIndex, int minX, int minY, int maxX, int maxY)
        {
            m_LayerRenderers[layerIndex].UpdateGrid(minX, minY, maxX, maxY);
        }

        public void Update()
        {
            foreach (var layer in m_LayerRenderers)
            {
                layer.Update(false, false);
            }
        }

        public void OnActiveLayerChange()
        {
        }

        public void OnAddLayer(AttributeSystem.LayerBase layer)
        {
            var layerRenderer = new LayerRenderer(layer, m_Root.transform, m_Pool);
            m_LayerRenderers.Add(layerRenderer);

            foreach (var renderer in m_LayerRenderers)
            {
                renderer.Root.transform.SetSiblingIndex(m_AttributeSystem.GetLayerIndex(layer.ID));
            }
        }

        public void OnRemoveLayer(int layerID)
        {
            foreach(var layer in m_LayerRenderers)
            {
                if (layer.LayerID == layerID)
                {
                    layer.Uninitialize();
                    m_LayerRenderers.Remove(layer);
                    break;
                }
            }
        }

        public void ToggleVisibility(AttributeSystem.LayerBase layer)
        {
            var renderer = GetLayerRenderer(layer.ID);
            renderer.Root.SetActive(layer.IsActive);
        }

        public void SetAspect(int objectID, string name)
        {
            if (name == "Layer Color")
            {
                var layerRenderer = GetLayerRenderer(objectID);
                if (layerRenderer != null)
                {
                    layerRenderer.Update(true, true);
                }
            }
            else if (name == "Layer Name")
            {
                var layerRenderer = GetLayerRenderer(objectID);
                if (layerRenderer != null)
                {
                    var layer = m_AttributeSystem.QueryObjectUndo(objectID) as AttributeSystem.LayerBase;
                    layerRenderer.Root.name = layer.Name;
                }
            }
            else if (name == "Layer Visibility")
            {
                var layerRenderer = GetLayerRenderer(objectID);
                if (layerRenderer != null)
                {
                    var layer = m_AttributeSystem.QueryObjectUndo(objectID) as AttributeSystem.LayerBase;
                    ToggleVisibility(layer);
                }
            }
            else if (name == "Grid Visible")
            {
                var layer = m_AttributeSystem.QueryObjectUndo(objectID) as AttributeSystem.LayerBase;
                ShowGrid(objectID, layer.GridVisible);
            }
        }

        private void ShowGrid(int layerID, bool show)
        {
            var renderer = GetLayerRenderer(layerID);
            if (renderer != null)
            {
                renderer.ShowGrid(show);
            }
        }

        private LayerRenderer GetLayerRenderer(int layerID)
        {
            foreach (var renderer in m_LayerRenderers)
            {
                if (renderer.LayerID == layerID)
                {
                    return renderer;
                }
            }
            return null;
        }

        private GameObject m_Root;
        private readonly List<LayerRenderer> m_LayerRenderers = new();
        private readonly AttributeSystem m_AttributeSystem;
        private readonly ArrayPool<Color32> m_Pool = ArrayPool<Color32>.Create();
    }
}
