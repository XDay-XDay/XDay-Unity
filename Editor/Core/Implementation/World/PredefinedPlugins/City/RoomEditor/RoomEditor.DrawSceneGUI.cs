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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    internal partial class RoomEditor
    {
        public void DrawSceneGUI(Event e, IWorld map)
        {
            if (m_Mode == Mode.Room)
            {
                DrawSceneGUIMainBuilding(e, map);
            }
            else if (m_Mode == Mode.Facility)
            {
                DrawSceneGUIFacility(e, map);
            }
            else
            {
                Debug.Assert(false, "todo");
            }
        }

        private void DrawSceneGUIMainBuilding(Event e, IWorld map)
        {
            if (m_SelectedRoomIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, map.CameraManipulator.Camera);
                var building = m_Rooms[m_SelectedRoomIndex];
                if (building.Instance == null)
                {
                    var pos = m_Grid.CityEditor.UpdateModelIndicator(worldPosition, building.Size, building.Prefab);
                    DrawArea(building.Size, pos, new Color(1, 0, 0, 0.5f));

                    if (e.type == EventType.MouseDown &&
                        e.button == 0 &&
                        e.alt == false)
                    {
                        if (building.Prefab == null)
                        {
                            EditorUtility.DisplayDialog("错误", "建筑模型未设置", "确定");
                            e.Use();
                        }
                        else
                        {
                            if (building.Instance == null)
                            {
                                building.CreateInstance(m_Grid.CityEditor.NextObjectID, worldPosition.x, worldPosition.z, m_Grid, m_Root.transform, building);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("错误", "已经创建了房间", "确定");
                                e.Use();
                            }
                        }
                    }
                    HandleUtility.AddDefaultControl(0);
                }
                else
                {
                    Tools.hidden = false;
                    m_Grid.CityEditor.HideModelIndicator();
                }
            }
            else
            {
                m_Grid.CityEditor.HideModelIndicator();
			}
        }

        private void DrawSceneGUIFacility(Event e, IWorld map)
        {
            if (m_SelectedRoomIndex >= 0)
            {
                var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, map.CameraManipulator.Camera);
                var mainBuilding = m_Rooms[m_SelectedRoomIndex];

                var facility = mainBuilding.GetFacilityPrefab(mainBuilding.SelectedFacilityIndex);

                if (facility != null && facility.Instance == null)
                {
                    var pos = m_Grid.CityEditor.UpdateModelIndicator(worldPosition, facility.Size, facility.Prefab);
                    DrawArea(facility.Size, pos, new Color(0,1,0,0.5f));

                    if (e.type == EventType.MouseDown && e.button == 0 && e.alt == false)
                    {
                        if (facility.Prefab == null)
                        {
                            EditorUtility.DisplayDialog("错误", "设施模型未设置", "确定");
                            e.Use();
                        }
                        else
                        {
                            if (facility.Instance == null)
                            {
                                facility.CreateInstance(m_Grid.CityEditor.NextObjectID, worldPosition.x, worldPosition.z, m_Grid, mainBuilding.Instance.GameObject.transform, mainBuilding.FacilityLocalY);
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("错误", "该类型设施已经存在", "确定");
                                e.Use();
                            }
                        }
                    }
                    HandleUtility.AddDefaultControl(0);
                }
                else
                {
                    Tools.hidden = false;
                    m_Grid.CityEditor.HideModelIndicator();
				}
            }
        }

        /// <summary>
        /// 绘制占地范围
        /// </summary>
        private void DrawArea(Vector2Int size, Vector3 position, Color color)
        {
            Handles.matrix = Matrix4x4.TRS(position, m_Grid.RootGameObject.transform.rotation, new Vector3(size.x * m_Grid.GridSize, 0.2f, size.y * m_Grid.GridSize));

            var oldColor = Handles.color;
            Handles.color = color;

            Handles.CubeHandleCap(0, Vector3.zero, Quaternion.identity, 1, EventType.Repaint);
            Handles.color = oldColor;
            Handles.matrix = Matrix4x4.identity;
        }
    }
}
