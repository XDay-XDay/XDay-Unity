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
using UnityEditor;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    /// <summary>
    /// 区域
    /// </summary>
    class RegionTemplate : IScenePrefabSetter
    {
        public int ObjectID => m_ObjectID;
        public int ConfigID { get => m_ConfigID; set => m_ConfigID = value; }
        public string Name { get => m_Name; set => m_Name = value; }
        public Color Color { get => m_Color; set => m_Color = value; }
        public bool ShowInInspector { get => m_ShowInInspector; set => m_ShowInInspector = value; }
        public bool ShowAreaTemplates { get => m_ShowAreaTemplates; set => m_ShowAreaTemplates = value; }
        public bool ShowLandTemplates { get => m_ShowLandTemplates; set => m_ShowLandTemplates = value; }
        public bool ShowEventTemplates { get => m_ShowEventTemplates; set => m_ShowEventTemplates = value; }
        public int AreaTemplateCount => m_AreaTemplates.Count;
        public int LandTemplateCount => m_LandTemplates.Count;
        public int EventTemplateCount => m_EventTemplates.Count;
        public int SelectedAreaTemplateIndex => m_SelectedAreaTemplateIndex;
        public int SelectedLandTemplateIndex { get => m_SelectedLandTemplateIndex; set => m_SelectedLandTemplateIndex = value; }
        public int SelectedEventTemplateIndex => m_SelectedEventTemplateIndex;
        public bool Lock { get => m_Lock; set => m_Lock = value; }
        public List<AreaTemplate> AreaTemplates => m_AreaTemplates;
        public List<LandTemplate> LandTemplates => m_LandTemplates;
        public List<EventTemplate> EventTemplates => m_EventTemplates;
        public bool Visible { get => m_Prefab.Visible; set => m_Prefab.Visible = value; }
        public GameObject Prefab { get => m_Prefab.Prefab; set => m_Prefab.Prefab = value; }
        public GameObject PrefabInstance => m_Prefab.Instance;
        public Vector3 Position
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.position;
                }
                return Vector3.zero;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.position = value;
                }
            }
        }

        public Quaternion Rotation
        {
            get
            {
                if (PrefabInstance != null)
                {
                    return PrefabInstance.transform.rotation;
                }
                return Quaternion.identity;
            }
            set
            {
                if (PrefabInstance != null)
                {
                    PrefabInstance.transform.rotation = value;
                }
            }
        }

        public RegionTemplate()
        {
        }

        public RegionTemplate(int objectID, string name, Color color, int configID)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));

            m_ObjectID = objectID;
            m_Name = name;
            m_Color = color;
            m_ConfigID = configID;
        }

        public void Initialize(Grid grid)
        {
            m_Grid = grid;

            foreach (var e in m_EventTemplates)
            {
                e.Initialize(grid);
            }

            SetSelectedEventTemplate(m_SelectedEventTemplateIndex);
            SetSelectedAreaTemplate(m_SelectedAreaTemplateIndex);

            m_Prefab.Initialize(m_Grid.ScenePrefabRoot.transform, true);
        }

        public void OnDestroy()
        {
            foreach (var e in m_EventTemplates)
            {
                e.OnDestroy();
            }

            m_Prefab.OnDestroy();
        }

        #region 地块
        public void SetSelectedAreaTemplate(int index)
        {
            m_SelectedAreaTemplateIndex = index;

            var areaLayer = m_Grid.GetLayer<AreaLayer>();
            if (index >= 0 && index < m_AreaTemplates.Count)
            {
                areaLayer.SetArea(m_AreaTemplates[index]);
            }
            else
            {
                areaLayer.SetArea(null);
            }
        }

        public void AddAreaTemplate(AreaTemplate area)
        {
            m_AreaTemplates.Add(area);

            SetSelectedAreaTemplate(m_AreaTemplates.Count - 1);
        }

        public void RemoveAreaTemplate(int index)
        {
            if (index >= 0 && index < m_AreaTemplates.Count)
            {
                var areaLayer = m_Grid.GetLayer<AreaLayer>();
                areaLayer.Clear(m_AreaTemplates[index].ObjectID);
                areaLayer.UpdateColors();

                m_AreaTemplates.RemoveAt(index);

                --m_SelectedAreaTemplateIndex;
                if (m_AreaTemplates.Count > 0 && m_SelectedAreaTemplateIndex < 0)
                {
                    m_SelectedAreaTemplateIndex = 0;
                }
                SetSelectedAreaTemplate(m_SelectedAreaTemplateIndex);
            }
        }

        public AreaTemplate GetAreaTemplate(int objectID)
        {
            foreach (var area in m_AreaTemplates)
            {
                if (area.ObjectID == objectID)
                {
                    return area;
                }
            }

            return null;
        }

        public bool IsAreaLocked(int objectID)
        {
            var smallRegion = GetAreaTemplate(objectID);
            if (smallRegion == null)
            {
                return false;
            }
            return smallRegion.Lock;
        }
        #endregion

        #region 绿地
        public void AddLandTemplate(LandTemplate block)
        {
            m_LandTemplates.Add(block);

            m_SelectedLandTemplateIndex = m_LandTemplates.Count - 1;
        }

        public void RemoveLandTemplate(int index)
        {
            //if (index >= 0 && index < m_LandTemplates.Count)
            //{
            //    var landLayer = m_Grid.GetLayer<LandLayer>();
            //    landLayer.Clear(m_LandTemplates[index].ObjectID);
            //    landLayer.UpdateColors();

            //    m_LandTemplates.RemoveAt(index);

            //    --m_SelectedLandTemplateIndex;
            //    if (m_LandTemplates.Count > 0 && m_SelectedLandTemplateIndex < 0)
            //    {
            //        m_SelectedLandTemplateIndex = 0;
            //    }
            //}
        }

        public LandTemplate GetLandTemplate(int objectID)
        {
            foreach (var land in m_LandTemplates)
            {
                if (land.ObjectID == objectID)
                {
                    return land;
                }
            }

            return null;
        }

        public bool IsLandLocked(int objectID)
        {
            var land = GetLandTemplate(objectID);
            if (land == null)
            {
                return false;
            }
            return land.Lock;
        }
        #endregion

        #region 事件
        public void SetSelectedEventTemplate(int index)
        {
            m_SelectedEventTemplateIndex = index;

            var eventLayer = m_Grid.GetLayer<EventLayer>();
            if (index >= 0 && index < m_EventTemplates.Count)
            {
                eventLayer.SetEvent(m_EventTemplates[index]);
            }
            else
            {
                eventLayer.SetEvent(null);
            }
        }

        public void AddEventTemplate(EventTemplate e)
        {
            m_EventTemplates.Add(e);

            SetSelectedEventTemplate(m_EventTemplates.Count - 1);
        }

        public void RemoveEventTemplate(int index)
        {
            if (index >= 0 && index < m_EventTemplates.Count)
            {
                m_EventTemplates.RemoveAt(index);

                --m_SelectedEventTemplateIndex;
                if (m_EventTemplates.Count > 0 && m_SelectedEventTemplateIndex < 0)
                {
                    m_SelectedEventTemplateIndex = 0;
                }
                SetSelectedEventTemplate(m_SelectedEventTemplateIndex);
            }
        }

        public EventTemplate GetEventTemplate(int objectID)
        {
            foreach (var e in m_EventTemplates)
            {
                if (e.ObjectID == objectID)
                {
                    return e;
                }
            }

            return null;
        }
        #endregion

        public void UpdateLocatorSize(float size)
        {
            foreach (var area in m_AreaTemplates)
            {
                area.UpdateLocatorSize(size);
            }

            foreach (var land in m_LandTemplates)
            {
                land.UpdateLocatorSize(size);
            }
        }

        public void RenderLandNumber()
        {
            if (m_Style == null)
            {
                m_Style = new GUIStyle(GUI.skin.label);
                m_Style.normal.textColor = Color.white;
                m_Style.fontStyle = FontStyle.Bold;
            }

            for (var i = 0; i < m_LandTemplates.Count; ++i)
            {
                var center = m_LandTemplates[i].Center;
                var pos = m_Grid.CoordinateToGridCenterPosition(center.x, center.y);
                Handles.Label(pos, i.ToString(), m_Style);
            }
        }

        public void CalculateLandBounds()
        {
            //var layer = m_Grid.GetLayer<LandLayer>();
            //for (var i = 0; i < m_LandTemplates.Count; ++i)
            //{
            //    var bounds = layer.CalculateBounds(m_LandTemplates[i].ObjectID);
            //    m_LandTemplates[i].SetBounds(bounds);
            //}
        }

        public void Save(ISerializer writer, IObjectIDConverter translator)
        {
            writer.WriteInt32(m_Version, "BigRegionTemplate.Version");

            writer.WriteObjectID(m_ObjectID, "ID", translator);
            writer.WriteInt32(m_ConfigID, "Config ID");
            writer.WriteString(m_Name, "Name");
            writer.WriteColor(m_Color, "Color");
            writer.WriteBoolean(m_ShowInInspector, "Show In Inspector");
            writer.WriteBoolean(m_ShowAreaTemplates, "Show Small Regions");
            writer.WriteInt32(m_SelectedAreaTemplateIndex, "Selected Small Region Template Index");
            writer.WriteBoolean(m_Lock, "Lock");

            writer.WriteList(m_AreaTemplates, "Small Regions", (AreaTemplate region, int index) => {
                writer.WriteStructure($"Small Region {index}", () => {
                    region.Save(writer, translator);
                });
            });

            writer.WriteInt32(m_SelectedLandTemplateIndex, "Selected Block Template Index");
            writer.WriteList(m_LandTemplates, "Blocks", (LandTemplate block, int index) => {
                writer.WriteStructure($"Block {index}", () => {
                    block.Save(writer, translator);
                });
            });

            writer.WriteBoolean(m_ShowEventTemplates, "Show Event Templates");
            writer.WriteInt32(m_SelectedEventTemplateIndex, "Selected Event Template Index");
            writer.WriteList(m_EventTemplates, "Events", (EventTemplate e, int index) => {
                writer.WriteStructure($"Event {index}", () => {
                    e.Save(writer, translator);
                });
            });

            writer.WriteStructure("Scene Prefab", () =>
            {
                m_Prefab.Save(writer);
            });
        }

        public void Load(IDeserializer reader)
        {
            var version = reader.ReadInt32("BigRegionTemplate.Version");

            m_ObjectID = reader.ReadInt32("ID");
            m_ConfigID = reader.ReadInt32("Config ID");
            m_Name = reader.ReadString("Name");
            m_Color = reader.ReadColor("Color");
            m_ShowInInspector = reader.ReadBoolean("Show In Inspector");
            m_ShowAreaTemplates = reader.ReadBoolean("Show Small Regions");
            m_SelectedAreaTemplateIndex = reader.ReadInt32("Selected Small Region Template Index");
            if (version >= 2)
            {
                m_Lock = reader.ReadBoolean("Lock");
            }

            m_AreaTemplates = reader.ReadList("Small Regions", (int index) => {
                var region = new AreaTemplate();
                reader.ReadStructure($"Small Region {index}", () => {
                    region.Load(reader);
                });
                return region;
            });

            if (version >= 3)
            {
                m_SelectedLandTemplateIndex = reader.ReadInt32("Selected Block Template Index");
                m_LandTemplates = reader.ReadList("Blocks", (int index) =>
                {
                    var block = new LandTemplate();
                    reader.ReadStructure($"Block {index}", () =>
                    {
                        block.Load(reader);
                    });
                    return block;
                });
                m_LandTemplates.Clear();
            }

            if (version >= 4)
            {
                m_ShowEventTemplates = reader.ReadBoolean("Show Event Templates");
                m_SelectedEventTemplateIndex = reader.ReadInt32("Selected Event Template Index");
                m_EventTemplates = reader.ReadList("Events", (int index) =>
                {
                    var eventTemplate = new EventTemplate();
                    reader.ReadStructure($"Event {index}", () =>
                    {
                        eventTemplate.Load(reader);
                    });
                    return eventTemplate;
                });
            }

            if (version >= 5)
            {
                reader.ReadStructure("Scene Prefab", () =>
                {
                    m_Prefab.Load(reader);
                });
            }
        }

        [SerializeField]
        private int m_ObjectID;

        [SerializeField]
        private int m_ConfigID;

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private Color m_Color = Color.white;

        [SerializeField]
        private bool m_ShowInInspector = true;

        [SerializeField]
        private bool m_ShowAreaTemplates = true;

        [SerializeField]
        private int m_SelectedAreaTemplateIndex = -1;

        [SerializeField]
        private List<AreaTemplate> m_AreaTemplates = new();

        [SerializeField]
        private bool m_ShowLandTemplates = true;

        [SerializeField]
        private bool m_ShowEventTemplates = true;

        [SerializeField]
        private int m_SelectedLandTemplateIndex = -1;

        [SerializeField]
        private List<LandTemplate> m_LandTemplates = new();

        [SerializeField]
        private List<EventTemplate> m_EventTemplates = new();

        [SerializeField]
        private int m_SelectedEventTemplateIndex = -1;

        [SerializeField]
        private bool m_Lock = false;

        private GUIStyle m_Style;

        private Grid m_Grid;

        [SerializeField]
        private ScenePrefab m_Prefab = new();

        private const int m_Version = 5;
    }
}