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
using UnityEngine.Scripting;

namespace XDay.WorldAPI.Region
{
    [Preserve]
    internal partial class RegionSystem : WorldPlugin, IRegionSystem
    {
        public override List<string> GameFileNames => new() { "region" };
        public override string TypeName => "RegionSystem";
        public override string Name { get => m_Name; set => throw new System.NotImplementedException(); }
        internal RegionSystemRenderer Renderer => m_Renderer;
        public int CurrentLOD => m_LODSystem.CurrentLOD;

        public RegionSystem()
        {
        }

        protected override void InitInternal()
        {
            m_LODSystem.Init(World.WorldLODSystem);

            m_Renderer = new RegionSystemRenderer(World.Root.transform);

            m_VisibleAreaUpdater = ICameraVisibleAreaUpdater.Create(World.CameraVisibleAreaCalculator);
            m_VisibleAreaUpdater.SetDistanceThreshold((World as GameWorld).VisibleAreaUpdateDistance);

            foreach (var layer in m_Layers)
            {
                layer.Init(this);
            }
        }

        protected override void UninitInternal()
        {
            foreach (var layer in m_Layers)
            {
                layer.OnDestroy();
            }
            m_Layers.Clear();
        }

        protected override void UpdateInternal(float dt)
        {
            var cameraPos = World.CameraManipulator.RenderPosition;

            var viewportChanged = m_VisibleAreaUpdater.BeginUpdate();
            var lodChanged = m_LODSystem.Update(cameraPos.y);
            if (lodChanged)
            {
                OnLODChanged(m_VisibleAreaUpdater.PreviousArea, m_VisibleAreaUpdater.CurrentArea);
            }
            else if (viewportChanged)
            {
                OnUpdateViewport(m_VisibleAreaUpdater.PreviousArea, m_VisibleAreaUpdater.CurrentArea);
            }

            m_VisibleAreaUpdater.EndUpdate();
        }

        public int GetLayerIndex(string name)
        {
            for (var i = 0; i < m_Layers.Count; ++i)
            {
                if (m_Layers[i].Name == name)
                {
                    return i;
                }
            }
            Debug.LogError($"Invalid region layer {name}");
            return -1;
        }

        public string GetLayerName(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < m_Layers.Count)
            {
                return m_Layers[layerIndex].Name;
            }
            Debug.LogError($"Invalid region layer index {layerIndex}");
            return null;
        }

        public Vector2Int WorldPositionToCoordinate(int layerIndex, float x, float z)
        {
            var layer = GetLayer(layerIndex);
            if (layer != null)
            {
                return layer.PositionToCoordinate(x, z);
            }
            Debug.LogError($"Invalid region layer index {layerIndex}");
            return Vector2Int.zero;
        }

        public IRegionSystemLayer GetLayer(int layerIndex)
        {
            if (layerIndex >= 0 && layerIndex < m_Layers.Count)
            {
                return m_Layers[layerIndex];
            }
            return null;
        }

        public IRegionSystemLayer GetLayer(string name)
        {
            foreach (var layer in m_Layers)
            {
                if (layer.Name == name)
                {
                    return layer;
                }
            }
            return null;
        }

        public void SetBorderLOD1Material(int layerIndex, int regionConfigID, Material material)
        {
            var layer = GetLayer(layerIndex);
            if (layer != null)
            {
                layer.SetBorderLOD1Material(regionConfigID, material);
            }
            else
            {
                Debug.LogError($"SetMaterial failed: invalid layer: {layerIndex}");
            }
        }

        public void SetMeshLOD1Material(int layerIndex, int regionConfigID, Material material)
        {
            var layer = GetLayer(layerIndex);
            if (layer != null)
            {
                layer.SetMeshLOD1Material(regionConfigID, material);
            }
            else
            {
                Debug.LogError($"SetMaterial failed: invalid layer: {layerIndex}");
            }
        }

        public void SetBorderLOD0Color(int layerIndex, int regionConfigID, Color color)
        {
            var layer = GetLayer(layerIndex);
            if (layer != null)
            {
                layer.SetBorderLOD0Color(regionConfigID, color);
            }
            else
            {
                Debug.LogError($"SetColor failed, invalid layer: {layerIndex}");
            }
        }

        public int GetRegionConfigID(int layerIndex, Vector3 position)
        {
            var layer = GetLayer(layerIndex);
            if (layer != null)
            {
                var coord = layer.PositionToCoordinate(position.x, position.z);
                return layer.GetRegionConfigID(coord.x, coord.y);
            }
            else
            {
                Debug.LogError($"GetRegionConfigID failed, invalid layer: {layerIndex}");
            }
            return 0;
        }

        private void OnLODChanged(Rect previousArea, Rect currentArea)
        {
            foreach (var layer in m_Layers)
            {
                layer.OnLODChanged(previousArea, currentArea);
            }
        }

        private void OnUpdateViewport(Rect previousArea, Rect currentArea)
        {
            foreach (var layer in m_Layers)
            {
                layer.OnUpdateViewport(previousArea, currentArea);
            }
        }

        private string m_Name;
        private List<LayerBase> m_Layers;
        private IPluginLODSystem m_LODSystem;
        private ICameraVisibleAreaUpdater m_VisibleAreaUpdater;
        private RegionSystemRenderer m_Renderer;
    }
}

