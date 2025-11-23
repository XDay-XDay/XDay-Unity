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

namespace XDay.WorldAPI.Region
{
    internal partial class RegionSystem
    {
        internal abstract class LayerBase : IRegionSystemLayer
        {
            public string Name { get => m_Name; set => m_Name = value; }
            public int HorizontalGridCount => m_HorizontalGridCount;
            public int VerticalGridCount => m_VerticalGridCount;
            public float GridWidth => m_GridWidth;
            public float GridHeight => m_GridHeight;
            public Vector2 Origin => m_Origin;
            public RegionSystem System => m_RegionSystem;
            public float Height => m_Height;

            public LayerBase(string name, int horizontalGridCount, int verticalGridCount, 
                float gridWidth, float gridHeight, Vector2 origin, float height)
            {
                m_Name = name;
                m_HorizontalGridCount = horizontalGridCount;
                m_VerticalGridCount = verticalGridCount;
                m_GridWidth = gridWidth;
                m_GridHeight = gridHeight;
                m_Origin = origin;
                m_Height = height;
            }

            public void Init(RegionSystem regionSystem)
            {
                m_RegionSystem = regionSystem;

                OnInit(regionSystem.Renderer.Root.transform);
            }

            public abstract void OnDestroy();

            public Vector2Int PositionToCoordinate(float x, float z)
            {
                var coordX = Mathf.FloorToInt((x - m_Origin.x) / m_GridWidth);
                var coordY = Mathf.FloorToInt((z - m_Origin.y) / m_GridHeight);
                return new Vector2Int(coordX, coordY);
            }

            public Vector3 CoordinateToPosition(int x, int y)
            {
                return new Vector3(
                    x * m_GridWidth + m_Origin.x,
                    0,
                    y * m_GridHeight + m_Origin.y);
            }

            public Vector3 CoordinateToCenterPosition(int x, int y)
            {
                return new Vector3(
                    (x + 0.5f) * m_GridWidth + m_Origin.x,
                    0,
                    (y + 0.5f) * m_GridHeight + m_Origin.y);
            }

            internal bool SizeEqual(LayerBase otherLayer)
            {
                return
                    Mathf.Approximately(m_GridWidth, otherLayer.GridWidth) &&
                    Mathf.Approximately(m_GridHeight, otherLayer.GridHeight) &&
                    Mathf.Approximately(m_HorizontalGridCount, otherLayer.HorizontalGridCount) &&
                    Mathf.Approximately(m_VerticalGridCount, otherLayer.VerticalGridCount);
            }

            protected abstract void OnInit(Transform parent);
            internal abstract void OnLODChanged(Rect oldVisibleArea, Rect newVisibleArea);
            internal abstract void OnUpdateViewport(Rect oldVisibleArea, Rect newVisibleArea);
            public abstract int GetValue(int x, int y);
            public abstract void SetValue(int x, int y, int type);
            public abstract void SetBorderLOD0Color(int regionConfigID, Color color);
            public abstract void SetBorderLOD0Material(int regionConfigID, Material material);
            public abstract void SetBorderLOD1Material(int regionConfigID, Material material);
            public abstract void SetMeshLOD1Material(int regionConfigID, Material material);

            [SerializeField]
            private string m_Name;
            [SerializeField]
            private float m_GridWidth;
            [SerializeField]
            private float m_GridHeight;
            [SerializeField]
            private int m_HorizontalGridCount;
            [SerializeField]
            private int m_VerticalGridCount;
            [SerializeField]
            private Vector2 m_Origin;
            [SerializeField]
            private float m_Height;
            private RegionSystem m_RegionSystem;
        }

        internal class Layer : LayerBase
        {
            public List<RegionObject> RegionList => m_RegionsList;

            public Layer(string name, int horizontalGridCount, int verticalGridCount, float gridWidth, float gridHeight, 
                Vector2 origin, float height) 
                : base(name, horizontalGridCount, verticalGridCount, gridWidth, gridHeight, origin, height)
            {
            }

            protected override void OnInit(Transform parent) 
            {
                m_Renderer = new RegionSystemLayerRenderer(this, parent, Height);
            }

            public override void OnDestroy()
            {
                m_Renderer?.OnDestroy();
            }

            public override void SetValue(int x, int y, int value)
            {
            }

            public override int GetValue(int x, int y)
            {
                return 0;
            }

            public override void SetBorderLOD0Color(int regionConfigID, Color color)
            {
                if (m_RegionsDic.TryGetValue(regionConfigID, out var region))
                {
                    region.Color = color;
                    m_Renderer.OnRegionBorderLOD0ColorChange(region);
                }
            }

            public override void SetBorderLOD0Material(int regionConfigID, Material material)
            {
                if (m_RegionsDic.TryGetValue(regionConfigID, out var region))
                {
                    m_Renderer.OnRegionBorderLOD0MaterialChange(region, material);
                }
            }

            public override void SetBorderLOD1Material(int regionConfigID, Material material)
            {
                if (m_RegionsDic.TryGetValue(regionConfigID, out var region))
                {
                    m_Renderer.OnRegionBorderLOD1MaterialChange(region, material);
                }
            }

            public override void SetMeshLOD1Material(int regionConfigID, Material material)
            {
                if (m_RegionsDic.TryGetValue(regionConfigID, out var region))
                {
                    m_Renderer.OnRegionMeshLOD1MaterialChange(region, material);
                }
            }

            internal void AddRegion(RegionObject region)
            {
                m_RegionsDic.Add(region.ConfigID, region);
                m_RegionsList.Add(region);
            }

            internal override void OnLODChanged(Rect oldVisibleArea, Rect newVisibleArea)
            {
                for (var idx = m_VisibleRegions.Count - 1; idx >= 0; --idx)
                {
                    SetRegionActive(m_VisibleRegions[idx], false);
                }

                foreach (var region in m_RegionsList)
                {
                    if (region.Intersect(ref newVisibleArea))
                    {
                        SetRegionActive(region, true);
                    }
                }
            }

            //todo optimize
            internal override void OnUpdateViewport(Rect oldVisibleArea, Rect newVisibleArea)
            {
                for (var idx = m_VisibleRegions.Count - 1; idx >= 0; --idx)
                {
                    if (!m_VisibleRegions[idx].Intersect(ref newVisibleArea))
                    {
                        SetRegionActive(m_VisibleRegions[idx], false);
                    }
                }

                foreach (var region in m_RegionsList)
                {
                    if (region.Intersect(ref newVisibleArea))
                    {
                        SetRegionActive(region, true);
                    }
                }
            }

            private void SetRegionActive(RegionObject region, bool active)
            {
                if (region.Active != active)
                {
                    region.Active = active;
                    m_Renderer.OnActiveStateChange(region);
                    if (active)
                    {
                        m_VisibleRegions.Add(region);
                    }
                    else
                    {
                        m_VisibleRegions.Remove(region);
                    }
                }
            }

            private Dictionary<int, RegionObject> m_RegionsDic = new();
            private List<RegionObject> m_VisibleRegions = new();
            private List<RegionObject> m_RegionsList = new();
            private RegionSystemLayerRenderer m_Renderer;
        }
    }
}
