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
using UnityEditor;
using System.Collections.Generic;
using XDay.WorldAPI.Editor;
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    class BuildingInstance : GridObject
    {
        public BuildingInstance()
        {
        }

        public BuildingInstance(int id, Vector2Int size, Vector2Int min, Vector2Int max, Quaternion rotation, int templateID)
            : base(id, size, min, max, rotation)
        {
            m_TemplateID = templateID;
        }

        public void Initialize(CityEditor cityEditor, Transform parent, Grid ownerGrid, IWorld map)
        {
            m_Map = map;

            m_CityEditor = cityEditor;

            m_Template = cityEditor.GetBuildingTemplate(m_TemplateID);

            var prefab = m_Template?.Prefab;
            Initialize(parent, ownerGrid, prefab, m_Template.Name);

            CreateFacilities(m_Template?.RoomPrefab.Facilities);

            var behaviour = m_GameObject.AddComponent<BuildingBehaviour>();
            behaviour.Initialize(ID, OwnerGrid.ID, 
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });

            var name = m_GameObject.AddOrGetComponent<DisplayName>();
            name.Create(m_Template.Name, map.CameraManipulator.Camera, cityEditor.ShowName);
        }

        public void ChangeModel()
        {
            Transform parent = null;
            if (m_GameObject != null) 
            {
                parent = m_GameObject.transform.parent;
            }

            var behaviour = m_GameObject.GetComponent<BuildingBehaviour>();
            behaviour.ProcessDestroyEvent = false;

            Object.DestroyImmediate(m_GameObject);

            CreateGameObject(m_Template?.Prefab, parent);
            CreateFacilities(m_Template?.RoomPrefab.Facilities);

            behaviour = m_GameObject.AddComponent<BuildingBehaviour>();
            behaviour.Initialize(ID, OwnerGrid.ID, (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); },
                (e) => { WorldEditor.EventSystem.Broadcast(e); });

            var name = m_GameObject.AddOrGetComponent<DisplayName>();
            name.Create(m_Template.Name, m_Map.CameraManipulator.Camera, m_CityEditor.ShowName);
            m_GameObject.name = m_Template.Name;

            Selection.activeGameObject = m_GameObject;

            Position = SnapToGround(Position);
        }

        public void DrawHandle()
        {
            if (m_GameObject != null && m_GameObject.activeSelf)
            {
                var size = Size;

                var gridSize = OwnerGrid.GridSize;
                Handles.matrix = Matrix4x4.TRS(Position, OwnerGrid.RootGameObject.transform.rotation, new Vector3(size.x * gridSize, 0.2f, size.y * gridSize));

                var oldColor = Handles.color;
                Handles.color = new Color(1, 0, 0, 0.5f);

                Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1, EventType.Repaint);
                Handles.color = oldColor;
                Handles.matrix = Matrix4x4.identity;
            }
        }

        public override void Save(ISerializer writer, IObjectIDConverter converter)
        {
            writer.WriteInt32(m_VERSION, "BuildingInstance.Version");

            base.Save(writer, converter);

            writer.WriteObjectID(m_TemplateID, "Template ID", converter);
        }

        public override void Load(IDeserializer reader)
        {
            reader.ReadInt32("BuildingInstance.Version");

            base.Load(reader);

            m_TemplateID = reader.ReadInt32("Template ID");
        }

        private void CreateFacilities(List<FacilityPrefab> facilities)
        {
            m_FacilityRoot = new GameObject("设施");
            m_FacilityRoot.transform.SetParent(m_GameObject.transform, worldPositionStays:false);

            for (var idx = 0; idx < facilities.Count; ++idx)
            {
                var facility = facilities[idx];
                GameObject obj = null;
                if (facility.Prefab != null)
                {
                    obj = Object.Instantiate(facility.Prefab);
                }

                if (obj != null)
                {
                    obj.AddOrGetComponent<SelectionBehaviour>();

                    var globalMin = facility.LocalCoordinate + m_Min;
                    var globalMax = globalMin + facility.Size;
                    var pos = CalculateCenterPosition(globalMin, globalMax - Vector2Int.one);
                    obj.transform.SetPositionAndRotation(pos, m_ModelRotation);
                    obj.transform.SetParent(m_FacilityRoot.transform, worldPositionStays: true);
                    var localPos = obj.transform.localPosition;
                    localPos.y = m_Template.RoomPrefab.FacilityLocalY;
                    obj.transform.localPosition = localPos;
                    obj.AddComponent<NoKeyDeletion>();
                    var name = obj.AddOrGetComponent<DisplayName>();
                    name.Create(facilities[idx].Name, m_Map.CameraManipulator.Camera, m_CityEditor.ShowName);

                    //obj.transform.position = SnapToGround(obj.transform.position);
                    obj.name = facilities[idx].Name;
                }
            }
        }

        protected override void OnUpdateMovement()
        {
            //for (var idx = 0; idx < m_FacilityRoot.transform.childCount; ++idx)
            //{
            //    var transform = m_FacilityRoot.transform.GetChild(idx);
            //    transform.position = SnapToGround(transform.position);
            //}
        }

        public BuildingTemplate Template
        {
            get => m_Template;
            set
            {
                m_Template = value;
                m_TemplateID = m_Template.ID;
            }
        }

        [SerializeField]
        int m_TemplateID;

        BuildingTemplate m_Template;

        GameObject m_FacilityRoot;

        IWorld m_Map;

        CityEditor m_CityEditor;

        const int m_VERSION = 1;
    }
}