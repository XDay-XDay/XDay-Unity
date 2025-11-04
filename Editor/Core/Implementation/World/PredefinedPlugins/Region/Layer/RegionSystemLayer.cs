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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI.Editor;
using XDay.UtilityAPI.Math;
using static XDay.WorldAPI.Region.Editor.RegionSystem;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystemLayer : WorldObject
    {
        public RegionSystemLayerRenderer Renderer => m_Renderer;
        public RegionSystem System => World.QueryObject<RegionSystem>(m_RegionSystemID);
        public override string TypeName => "RegionSystemLayer";
        public string Name { get => m_Name; set => m_Name = value; }
        public int HorizontalGridCount => m_HorizontalGridCount;
        public int VerticalGridCount => m_VerticalGridCount;
        public float GridWidth => m_GridWidth;
        public float GridHeight => m_GridHeight;
        public float Width => m_Width;
        public float Height => m_Height;
        public float Alpha
        {
            get => m_Alpha;
            set
            {
                m_Alpha = Mathf.Clamp01(value);
            }
        }
        public Vector2 Origin => m_Origin;
        public Bounds Bounds => new(new Vector3(m_Width * 0.5f, 0, m_Height * 0.5f), new Vector3(m_Width, 0, m_Height));
        public bool GridVisible { get => m_GridVisible; set => m_GridVisible = value; }
        public List<RegionObject> Regions => m_Regions;
        public int RegionCount => Regions.Count;
        public int SelectedRegionIndex { get => m_SelectedRegionIndex; set => m_SelectedRegionIndex = value; }
        public RegionObject SelectedRegion
        {
            get
            {
                if (m_SelectedRegionIndex >= 0 && m_SelectedRegionIndex < m_Regions.Count)
                {
                    return m_Regions[m_SelectedRegionIndex];
                }
                return null;
            }
        }
        public bool ShowRegions { get => m_ShowRegions; set => m_ShowRegions = value; }
        public string[] RegionNames => m_RegionNames;
        protected override bool EnabledInternal { get => m_Visible; set => m_Visible = value; }
        protected override WorldObjectVisibility VisibilityInternal
        {
            set { }
            get => WorldObjectVisibility.Visible;
        }

        public RegionSystemLayer()
        {
        }

        public RegionSystemLayer(int id, int index, string name, int regionSystemID,
            int horizontalGridCount, int verticalGridCount, float gridWidth, float gridHeight, Vector2 origin)
            : base(id, index)
        {
            m_Name = name;
            m_RegionSystemID = regionSystemID;
            m_HorizontalGridCount = horizontalGridCount;
            m_VerticalGridCount = verticalGridCount;
            m_GridWidth = gridWidth;
            m_GridHeight = gridHeight;
            m_Origin = origin;

            m_GridData = new int[verticalGridCount * horizontalGridCount];

            CreateDefaultParams();
        }

        protected override void OnInit()
        {
            m_SelectedRegionIndex = EditorPrefs.GetInt(RegionDefine.SELECTED_REGION_INDEX);
            m_ShowRegions = EditorPrefs.GetBool(RegionDefine.SHOW_REGION);

            m_Width = m_GridWidth * m_HorizontalGridCount;
            m_Height = m_GridHeight * m_VerticalGridCount;

            foreach (var region in m_Regions)
            {
                region.Init(World);
            }

            foreach (var param in m_MeshGenerationParamForLODs)
            {
                param.Init();
            }

            m_CurveRegionMeshGenerationParam.Init();

            m_Renderer = new RegionSystemLayerRenderer(System.Renderer.Root.transform, this);

            InitEdges();

            ShowObjects();
        }

        protected override void OnUninit()
        {
            m_Renderer.OnDestroy();
            foreach (var region in m_Regions)
            {
                region.Uninit();
            }

            DestroyPreviewObjects();

            for (int i = 0; i < m_CreatorsForLODs.Count; ++i)
            {
                m_CreatorsForLODs[i].OnDestroy();
            }
        }

        public void SetRegion(int x, int y, int width, int height, int regionID)
        {
            var maxX = x + width - 1;
            var maxY = y + height - 1;
            for (var i = y; i <= maxY; ++i)
            {
                for (var j = x; j <= maxX; ++j)
                {
                    if (IsValidCoordinate(j, i))
                    {
                        m_GridData[i * HorizontalGridCount + j] = regionID;
                    }
                }
            }
            m_Renderer.UpdateColors(x, y, x + width - 1, y + height - 1);
        }

        public int GetRegionID(int x, int y)
        {
            if (IsValidCoordinate(x, y))
            {
                return m_GridData[y * HorizontalGridCount + x];
            }

            return 0;
        }

        public Color GetColor(int x, int y)
        {
            var regionID = GetRegionID(x, y);
            if (regionID == 0)
            {
                return m_Empty;
            }

            var region = GetRegion(regionID);
            var color = region.Color;
            color.a = m_Alpha;
            return color;
        }

        private RegionObject GetRegion(int regionID)
        {
            foreach (var r in m_Regions)
            {
                if (r.ID == regionID)
                {
                    return r;
                }
            }
            return null;
        }

        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < m_HorizontalGridCount &&
                y >= 0 && y < m_VerticalGridCount;
        }

        internal Vector2Int PositionToCoordinate(float x, float z)
        {
            var coordX = Mathf.FloorToInt((x - m_Origin.x) / m_GridWidth);
            var coordY = Mathf.FloorToInt((z - m_Origin.y) / m_GridHeight);
            return new Vector2Int(coordX, coordY);
        }

        internal Vector2Int PositionToCoordinate(Vector3 pos)
        {
            return PositionToCoordinate(pos.x, pos.z);
        }

        internal Vector3 CoordinateToPosition(int x, int y)
        {
            return new Vector3(
                x * m_GridWidth + m_Origin.x,
                0,
                y * m_GridHeight + m_Origin.y);
        }

        internal Vector3 CoordinateToCenterPosition(int x, int y)
        {
            return new Vector3(
                (x + 0.5f) * m_GridWidth + m_Origin.x,
                0,
                (y + 0.5f) * m_GridHeight + m_Origin.y);
        }

        private void ShowObjects()
        {
            foreach (var region in m_Regions)
            {
                m_Renderer.ToggleVisibility(region);
            }
        }

        internal bool SizeEqual(RegionSystemLayer otherLayer)
        {
            return
                Mathf.Approximately(m_GridWidth, otherLayer.GridWidth) &&
                Mathf.Approximately(m_GridHeight, otherLayer.GridHeight) &&
                Mathf.Approximately(m_HorizontalGridCount, otherLayer.HorizontalGridCount) &&
                Mathf.Approximately(m_VerticalGridCount, otherLayer.VerticalGridCount);
        }

        internal void Update()
        {
            m_Renderer?.Update();
        }

        internal bool Contains(int objectID)
        {
            foreach (var region in m_Regions)
            {
                if (region.ID == objectID)
                {
                    return true;
                }
            }
            return false;
        }

        internal void AddRegion(RegionObject region)
        {
            m_Regions.Add(region);
        }

        internal bool DestroyObject(int regionID)
        {
            foreach (var region in m_Regions)
            {
                if (region.ID == regionID)
                {
                    for (var i = 0; i < m_GridData.Length; ++i)
                    {
                        if (m_GridData[i] == regionID)
                        {
                            m_GridData[i] = 0;
                        }
                    }
                    region.Uninit();
                    m_Regions.Remove(region);
                    m_Renderer.UpdateColors(0, 0, m_HorizontalGridCount - 1, m_VerticalGridCount - 1);
                    return true;
                }
            }
            Debug.Assert(false, $"Destroy object {regionID} failed!");
            return false;
        }

        public override void EditorSerialize(ISerializer serializer, string label, IObjectIDConverter converter)
        {
            base.EditorSerialize(serializer, label, converter);

            serializer.WriteInt32(m_EditorVersion, "RegionSystemLayer.Version");

            serializer.WriteString(m_Name, "Name");
            serializer.WriteBoolean(m_Visible, "Visible");
            serializer.WriteBoolean(m_GridVisible, "Grid Visible");
            serializer.WriteObjectID(m_RegionSystemID, "Region System ID", converter);
            serializer.WriteInt32(m_HorizontalGridCount, "Horizontal Grid Count");
            serializer.WriteInt32(m_VerticalGridCount, "Vertical Grid Count");
            serializer.WriteSingle(m_GridWidth, "Grid Width");
            serializer.WriteSingle(m_GridHeight, "Grid Height");
            serializer.WriteSingle(m_Alpha, "Alpha");

            var ids = ConvertToIDs(converter);
            serializer.WriteInt32Array(ids, "Grid Data");

            serializer.WriteList(m_Regions, "Objects", (obj, index) =>
            {
                serializer.WriteSerializable(obj, $"Object {index}", converter, false);
            });

            serializer.WriteList(m_MeshGenerationParamForLODs, "MeshGenerationParamForLODs", (param, index) =>
            {
                serializer.WriteSerializable(param, $"MeshGenerationParamForLOD {index}", converter, false);
            });

            serializer.WriteSerializable(m_CurveRegionMeshGenerationParam, "CurveRegionMeshGenerationParam", converter, false);

            EditorPrefs.SetInt(RegionDefine.SELECTED_REGION_INDEX, m_SelectedRegionIndex);
            EditorPrefs.SetBool(RegionDefine.SHOW_REGION, m_ShowRegions);
        }

        public override void EditorDeserialize(IDeserializer deserializer, string label)
        {
            base.EditorDeserialize(deserializer, label);

            deserializer.ReadInt32("RegionSystemLayer.Version");

            m_Name = deserializer.ReadString("Name");
            m_Visible = deserializer.ReadBoolean("Visible");
            m_GridVisible = deserializer.ReadBoolean("Grid Visible");
            m_RegionSystemID = deserializer.ReadInt32("Region System ID");
            m_HorizontalGridCount = deserializer.ReadInt32("Horizontal Grid Count");
            m_VerticalGridCount = deserializer.ReadInt32("Vertical Grid Count");
            m_GridWidth = deserializer.ReadSingle("Grid Width");
            m_GridHeight = deserializer.ReadSingle("Grid Height");
            m_Alpha = deserializer.ReadSingle("Alpha");
            m_GridData = deserializer.ReadInt32Array("Grid Data");

            m_Regions = deserializer.ReadList("Objects", (index) =>
            {
                return deserializer.ReadSerializable<RegionObject>($"Object {index}", false);
            });

            m_MeshGenerationParamForLODs = deserializer.ReadList("MeshGenerationParamForLODs", (index) =>
            {
                return deserializer.ReadSerializable<EditorRegionMeshGenerationParam>($"MeshGenerationParamForLOD {index}", false);
            });

            m_CurveRegionMeshGenerationParam = deserializer.ReadSerializable<CurveRegionMeshGenerationParam>("CurveRegionMeshGenerationParam", false);
        }

        private int[] ConvertToIDs(IObjectIDConverter converter)
        {
            var ids = new int[m_GridData.Length];
            for (var i = 0; i < ids.Length; ++i)
            {
                ids[i] = converter.Convert(m_GridData[i]);
            }
            return ids;
        }

        public void UpdateRegionNames()
        {
            if (m_RegionNames == null || m_RegionNames.Length != m_Regions.Count)
            {
                m_RegionNames = new string[m_Regions.Count];
            }

            for (var i = 0; i < m_RegionNames.Length; ++i)
            {
                m_RegionNames[i] = m_Regions[i].Name;
            }
        }

        internal void SyncWithRenderer()
        {
            foreach (var region in m_Regions)
            {
                region.BuildingPosition = m_Renderer.GetRegionBuildingPosition(region.ID);
            }
        }

        public List<Vector2Int> GetRegionCoordinates(int id)
        {
            List<Vector2Int> coordinates = new();
            for (int i = 0; i < m_VerticalGridCount; ++i)
            {
                for (int j = 0; j < m_HorizontalGridCount; ++j)
                {
                    if (m_GridData[i * m_HorizontalGridCount + j] == id)
                    {
                        coordinates.Add(new Vector2Int(j, i));
                    }
                }
            }
            return coordinates;
        }

        public Rect GetRegionWorldBounds(List<Vector2Int> regionCoordinates)
        {
            FloatBounds2D bounds = new();
            for (int i = 0; i < regionCoordinates.Count; ++i)
            {
                var minPos = CoordinateToPosition(regionCoordinates[i].x, regionCoordinates[i].y);
                var maxPos = CoordinateToPosition(regionCoordinates[i].x + 1, regionCoordinates[i].y + 1);
                bounds.AddPoint(minPos.x, minPos.z);
                bounds.AddPoint(maxPos.x, maxPos.z);
            }

            return new Rect(bounds.Min.x, bounds.Min.y, bounds.Size.x, bounds.Size.y);
        }

        public Vector3 GetRegionCenter(int regionID)
        {
            Vector3 total = Vector3.zero;
            var coords = GetRegionCoordinates(regionID);
            if (coords.Count == 0)
            {
                return total;
            }
            for (int i = 0; i < coords.Count; ++i)
            {
                total += CoordinateToCenterPosition(coords[i].x, coords[i].y);
            }
            return total / coords.Count;
        }

        public int GetGridData(int x, int y)
        {
            if (x >= 0 && y >= 0 &&  x < m_HorizontalGridCount && y < m_VerticalGridCount)
            {
                return m_GridData[y * m_HorizontalGridCount + x];
            }
            return 0;
        }

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
        private int m_RegionSystemID;
        [SerializeField]
        private bool m_Visible = true;
        [SerializeField]
        private bool m_GridVisible = true;
        [SerializeField]
        private int m_SelectedRegionIndex = -1;
        [SerializeField]
        private bool m_ShowRegions = true;
        [SerializeField]
        private int[] m_GridData;
        [SerializeField]
        private float m_Alpha = 0.7f;
        private string[] m_RegionNames;
        private float m_Width;
        private float m_Height;
        private List<RegionObject> m_Regions = new();
        private RegionSystemLayerRenderer m_Renderer = null;
        private const int m_EditorVersion = 1;
        private static Color m_Empty = new(0, 0, 0, 0);
    }
}

