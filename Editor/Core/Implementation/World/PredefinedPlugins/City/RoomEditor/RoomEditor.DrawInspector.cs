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
using UnityEditor;
using UnityEngine;
using XDay.UtilityAPI;
using XDay.UtilityAPI.Editor;

namespace XDay.WorldAPI.City.Editor
{
    internal partial class RoomEditor
    {
        public void DrawInspector()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowRooms = EditorGUILayout.Foldout(m_ShowRooms, "房间设置");
            if (GUILayout.Button("增加房间设置", GUILayout.MaxWidth(100)))
            {
                CommandAddBuildingPrefab();
            }

            EditorGUILayout.EndHorizontal();
            if (m_ShowRooms)
            {
                EditorHelper.IndentLayout(() =>
                {
                    var mode = (Mode)EditorGUILayout.Popup("编辑模式", (int)m_Mode, m_ModeNames);
                    SetMode(mode);

                    for (var i = 0; i < m_Rooms.Count; ++i)
                    {
                        var buildingTemplate = m_Rooms[i];
                        var deleted = false;
                        EditorHelper.IndentLayout(() =>
                        {
                            deleted = DrawBuildingSetting(buildingTemplate, i);
                        });
                        if (deleted)
                        {
                            RemoveMainBuildingPrefab(i);
                            break;
                        }
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        private bool DrawBuildingSetting(RoomPrefab building, int index)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", m_SelectedRoomIndex == index, GUILayout.MaxWidth(20)))
            {
                SetMainBuildingSelection(index);
            }
            EditorGUIUtility.labelWidth = 0;
            building.ShowInInspector = EditorGUILayout.Foldout(building.ShowInInspector, building.Name);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                CommandLocate(building.Instance);
                SetMainBuildingSelection(index);
                SetMode(Mode.Room);
            }

            if (GUILayout.Button("增加设施", GUILayout.MaxWidth(80)))
            {
                CommandAddBuildingFacility(building);
            }

            var deleted = false;
            if (GUILayout.Button("删除房间设置", GUILayout.MaxWidth(100)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除房间设置 \"{building.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (building.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    building.Name = EditorGUILayout.TextField("名字", building.Name);

                    building.FacilityLocalY = EditorGUILayout.FloatField("设施高度偏移", building.FacilityLocalY);

                    GUI.enabled = false;
                    EditorGUILayout.BeginHorizontal();
                    building.Size = EditorGUILayout.Vector2IntField("占地大小", building.Size);
                    GUI.enabled = !HasBuildingInstance(building);

                    if (GUILayout.Button(new GUIContent("修改大小", "房间未被使用时才能设置大小")))
                    {
                        ChangeSize(building);
                    }
                    EditorGUILayout.EndHorizontal();

                    GUI.enabled = true;
                    var newPrefab = EditorGUILayout.ObjectField("模型", building.Prefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
                    if (EditorHelper.IsPrefab(newPrefab) || newPrefab == null)
                    {
                        building.Prefab = newPrefab;
                    }

                    DrawBuildingFacilities(building);
                });
            }
            EditorGUILayout.EndVertical();

            return deleted;
        }

        private void ChangeSize(FacilityPrefab facilityPrefab)
        {
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.IntParameter("X", "", facilityPrefab.Size.x),
                new ParameterWindow.IntParameter("Y", "", facilityPrefab.Size.y),
            };
            ParameterWindow.Open("修改设施占地大小", input, (List<ParameterWindow.Parameter> items) =>
            {
                var ok = ParameterWindow.GetInt(items[0], out var width);
                ok &= ParameterWindow.GetInt(items[1], out var height);

                if (ok && width > 0 && height > 0)
                {
                    facilityPrefab.ChangeSize(width, height);
                    return true;
                }
                return false;
            });
        }

        private void ChangeSize(RoomPrefab roomPrefab)
        {
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.IntParameter("X", "", roomPrefab.Size.x),
                new ParameterWindow.IntParameter("Y", "", roomPrefab.Size.y),
            };
            ParameterWindow.Open("修改房间占地大小", input, (items) =>
            {
                var ok = ParameterWindow.GetInt(items[0], out var width);
                ok &= ParameterWindow.GetInt(items[1], out var height);

                if (ok && width > 0 && height > 0)
                {
                    roomPrefab.ChangeSize(width, height);
                    return true;
                }
                return false;
            });
        }

        private void DrawBuildingFacilities(RoomPrefab building)
        {
            var n = building.Facilities.Count;
            if (n > 0)
            {
                EditorGUILayout.BeginVertical("GroupBox");
            }
            else
            {
                EditorGUILayout.BeginVertical();
            }
            for (var i = 0; i < n; ++i)
            {
                var facility = building.Facilities[i];

                EditorGUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 10;
                if (EditorGUILayout.ToggleLeft("", building.SelectedFacilityIndex == i, GUILayout.MaxWidth(20)))
                {
                    building.SelectedFacilityIndex = i;
                }
                EditorGUIUtility.labelWidth = 0;

                facility.ShowInInspector = EditorGUILayout.Foldout(facility.ShowInInspector, facility.Name);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
                {
                    CommandLocate(facility.Instance);
                    building.SelectedFacilityIndex = i;
                    SetMode(Mode.Facility);
                }

                var deleted = false;
                if (GUILayout.Button("删除设施", GUILayout.MaxWidth(90)))
                {
                    if (EditorUtility.DisplayDialog("注意", $"确定删除设施 \"{building.Name}\"?", "确定", "取消"))
                    {
                        RemoveFacilityPrefab(building, i);
                        deleted = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (!deleted && facility.ShowInInspector)
                {
                    EditorHelper.IndentLayout(() =>
                    {
                        facility.Name = EditorGUILayout.TextField("名字", facility.Name);
                        facility.ConfigID = EditorGUILayout.IntField("ID", facility.ConfigID);

                        EditorGUILayout.BeginHorizontal();
                        GUI.enabled = false;
                        facility.Size = EditorGUILayout.Vector2IntField("占地大小", facility.Size);
                        GUI.enabled = !HasBuildingInstance(building);
                        if (GUILayout.Button(new GUIContent("修改大小", "设施未被使用时才能设置大小")))
                        {
                            ChangeSize(facility);
                        }
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;
                        var newPrefab = EditorGUILayout.ObjectField("模型", facility.Prefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
                        if (EditorHelper.IsPrefab(newPrefab) || newPrefab == null)
                        {
                            facility.Prefab = newPrefab;
                        }
                    });
                }

                if (deleted)
                {
                    break;
                }
            }
            EditorGUILayout.EndVertical();
        }

        private bool HasBuildingInstance(RoomPrefab prefab)
        {
            var cityEditor = m_Grid.CityEditor;
            var grid = cityEditor.FirstGrid;
            foreach (var buildingInstance in grid.Buildings)
            {
                if (buildingInstance.Template.RoomPrefab == prefab)
                {
                    return true;
                }
            }

            return false;
        }

        private void CommandAddBuildingPrefab()
        {
            var cityEditor = m_Grid.CityEditor;

            var building = new RoomPrefab
            {
                ID = cityEditor.NextObjectID,
                Name = "房间"
            };

            m_Rooms.Add(building);

            if (m_SelectedRoomIndex < 0)
            {
                m_SelectedRoomIndex = 0;
            }
            else
            {
                SetMainBuildingSelection(m_Rooms.Count - 1);
            }

            SetOperation(CityEditor.OperationType.RoomEdit);
            SetMode(Mode.Room);
        }

        private void RemoveMainBuildingPrefab(int index)
        {
            m_Rooms[index].OnDestroy();
            m_Rooms.RemoveAt(index);

            var selectedIndex = m_SelectedRoomIndex;
            --selectedIndex;
            if (m_SelectedRoomIndex < 0 && m_Rooms.Count > 0)
            {
                selectedIndex = 0;
            }
            SetMainBuildingSelection(selectedIndex);
        }

        private void RemoveFacilityPrefab(RoomPrefab mainBuilding, int index)
        {
            mainBuilding.RemoveFacility(index);
        }

        private void CommandLocate(RoomInstanceBase instance)
        {
            if (instance != null)
            {
                SceneView.lastActiveSceneView.pivot = instance.GameObject.transform.position;
                Selection.activeGameObject = instance.GameObject;

                SetOperation(CityEditor.OperationType.RoomEdit);
                SetOperation(CityEditor.OperationType.Select);
            }
        }

        private void CommandAddBuildingFacility(RoomPrefab building)
        {
            var facility = new FacilityPrefab
            {
                Name = "设施"
            };

            building.AddFacility(facility);

            SetOperation(CityEditor.OperationType.RoomEdit);
            SetMode(Mode.Facility);
        }

        private void SetMainBuildingSelection(int index)
        {
            if (m_SelectedRoomIndex != index)
            {
                for (var i = 0; i < m_Rooms.Count; ++i)
                {
                    m_Rooms[i].IsVisible = false;
                }

                m_SelectedRoomIndex = index;

                if (m_SelectedRoomIndex >= 0 && m_SelectedRoomIndex < m_Rooms.Count)
                {
                    m_Rooms[m_SelectedRoomIndex].IsVisible = true;
                }
            }
        }

        private void SetOperation(CityEditor.OperationType operation)
        {
            m_Grid.CityEditor.SetOperation(operation);
        }

        private void SetMode(Mode mode)
        {
            if (m_Mode != mode)
            {
                m_Mode = mode;
            }
        }
    }
}