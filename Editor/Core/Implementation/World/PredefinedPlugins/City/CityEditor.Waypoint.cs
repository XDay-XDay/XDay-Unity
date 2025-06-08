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
    /// <summary>
    /// 捷径点
    /// </summary>
    partial class CityEditor
    {
        private void DrawWaypointSettings()
        {
            var grid = GetSelectedGrid();
            if (grid == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            m_ShowWaypointSettings = EditorGUILayout.Foldout(m_ShowWaypointSettings, "传送点");

            var showAll = EditorGUILayout.ToggleLeft("显隐", m_ShowAllWaypoints);
            if (showAll != m_ShowAllWaypoints)
            {
                m_ShowAllWaypoints = showAll;
                foreach (var g in m_Grids)
                {
                    g.SetWayPointVisibility(m_ShowAllWaypoints);
                }
            }

            if (GUILayout.Button("增加传送点", GUILayout.MaxWidth(90)))
            {
                CommandAddWaypoint();
            }
            EditorGUILayout.EndHorizontal();

            var points = grid.Waypoints;
            if (m_ShowWaypointSettings)
            {
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < points.Count; ++i)
                    {
                        var point = points[i];
                        var deleted = false;
                        EditorHelper.IndentLayout(() =>
                        {
                            deleted = DrawWaypointSetting(point, i);
                        });
                        if (deleted)
                        {
                            grid.RemoveWaypoint(i);
                            break;
                        }
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        bool DrawWaypointSetting(Waypoint point, int index)
        {
            var grid = GetSelectedGrid();
            EditorGUILayout.BeginHorizontal();
            point.ShowInInspector = EditorGUILayout.Foldout(point.ShowInInspector, point.Name);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                CommandLocate(point);
            }

            var deleted = false;
            if (GUILayout.Button("删除捷径点", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除捷径点 \"{point.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (point.ShowInInspector)
            {
                EditorHelper.IndentLayout(()=>
                {
                    point.ID = EditorGUILayout.IntField("ID", point.ID);
                    point.Name = EditorGUILayout.TextField("名字", point.Name);
                    point.ConnectedID = EditorGUILayout.IntField("对应捷径点ID", point.ConnectedID);
                    point.EventID = EditorGUILayout.IntField("解锁事件ID", point.EventID);
                    point.Enabled = EditorGUILayout.Toggle("开启", point.Enabled);
                });
            }

            return deleted;
        }

        private void CommandAddWaypoint()
        {
            var grid = GetSelectedGrid();

            var point = new Waypoint()
            {
                Name = $"捷径点{grid.Waypoints.Count + 1}"
            };
            
            grid?.AddWaypoint(point);

            SetOperation(OperationType.SetWayPoint);

            var camera = SceneView.lastActiveSceneView.camera;
            point.GameObject.transform.position = Helper.RayCastWithXZPlane(camera.pixelRect.center, camera);

            CommandLocate(point);
        }

        private void CommandLocate(Waypoint point)
        {
            SceneView.lastActiveSceneView.pivot = point.GameObject.transform.position;
            Selection.activeGameObject = point.GameObject;

            SetOperation(OperationType.Select);
        }
    }
}
