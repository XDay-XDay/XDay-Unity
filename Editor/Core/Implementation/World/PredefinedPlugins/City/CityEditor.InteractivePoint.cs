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
    /// 交互点
    /// </summary>
    partial class CityEditor
    {
        private void DrawInteractivePointSettings()
        {
            var grid = GetSelectedGrid();
            if (grid == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            m_ShowInteractivePointSettings = EditorGUILayout.Foldout(m_ShowInteractivePointSettings, "交互点");

            var showAll = EditorGUILayout.ToggleLeft("显隐", m_ShowAllInteractivePoints);
            if (showAll != m_ShowAllInteractivePoints)
            {
                m_ShowAllInteractivePoints = showAll;
                foreach (var g in m_Grids)
                {
                    g.SetInteractivePointVisibility(m_ShowAllInteractivePoints);
                }
            }

            if (GUILayout.Button("增加交互点", GUILayout.MaxWidth(90)))
            {
                CommandAddInteractivePoint();
            }
            EditorGUILayout.EndHorizontal();

            var points = grid.InteractivePoints;
            if (m_ShowInteractivePointSettings)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawDefaultInteractivePrefabs(grid);

                    for (var i = 0; i < points.Count; ++i)
                    {
                        var point = points[i];
                        var deleted = false;
                        EditorHelper.IndentLayout(() =>
                        {
                            deleted = DrawInteractivePointSetting(point, i);
                        });
                        if (deleted)
                        {
                            grid.RemoveInteractivePoint(i);
                            break;
                        }
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        void DrawDefaultInteractivePrefabs(Grid grid)
        {
            EditorGUILayout.BeginHorizontal();
            var newStartPrefab = (GameObject)EditorGUILayout.ObjectField("默认起点模型", grid.DefaultInteractivePointStartPrefab, typeof(GameObject), false);
            if (newStartPrefab != grid.DefaultInteractivePointStartPrefab)
            {
                grid.SetDefaultInteractivePointStartPrefab(newStartPrefab, false);
            }
            if (GUILayout.Button("修改所有起点模型", GUILayout.MaxWidth(100)))
            {
                grid.SetDefaultInteractivePointStartPrefab(newStartPrefab, true);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            var newEndPrefab = (GameObject)EditorGUILayout.ObjectField("默认终点模型", grid.DefaultInteractivePointEndPrefab, typeof(GameObject), false);
            if (newEndPrefab != grid.DefaultInteractivePointEndPrefab)
            {
                grid.SetDefaultInteractivePointEndPrefab(newEndPrefab, false);
            }
            if (GUILayout.Button("修改所有终点模型", GUILayout.MaxWidth(100)))
            {
                grid.SetDefaultInteractivePointEndPrefab(newEndPrefab, true);
            }
            EditorGUILayout.EndHorizontal();
        }

        bool DrawInteractivePointSetting(InteractivePoint point, int index)
        {
            var grid = GetSelectedGrid();
            EditorGUILayout.BeginHorizontal();
            point.ShowInInspector = EditorGUILayout.Foldout(point.ShowInInspector, point.Name);

            GUILayout.FlexibleSpace();

            var deleted = false;
            if (GUILayout.Button("删除交互点", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除交互点 \"{point.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (point.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    point.Name = EditorGUILayout.TextField("名字", point.Name);
                    point.ID = EditorGUILayout.IntField("ID", point.ID);
                    DrawStart(point.Start);
                    DrawEnd(point);
                });
            }

            return deleted;
        }

        private void CommandAddInteractivePoint()
        {
            var point = new InteractivePoint()
            {
                Name = "交互点"
            };
            var grid = GetSelectedGrid();
            grid?.AddInteractivePoint(point);

            if (grid.DefaultInteractivePointStartPrefab != null)
            {
                point.Start.Prefab = grid.DefaultInteractivePointStartPrefab;
            }

            if (grid.DefaultInteractivePointEndPrefab != null)
            {
                point.End.Prefab = grid.DefaultInteractivePointEndPrefab;
            }

            SetOperation(OperationType.SetInteractivePoint);

            var camera = SceneView.lastActiveSceneView.camera;
            point.Position = Helper.RayCastWithXZPlane(camera.pixelRect.center, camera);
        }

        private void DrawStart(InteractivePointStartCoordinate start)
        {
            start.ShowInInspector = EditorGUILayout.Foldout(start.ShowInInspector, "起点");
            if (start.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawScenePrefab(start, true);
                });
            }
        }

        private void DrawEnd(InteractivePoint point)
        {
            var end = point.End;
            EditorGUILayout.BeginHorizontal();
            end.ShowInInspector = EditorGUILayout.Foldout(end.ShowInInspector, "终点");
            if (GUILayout.Button(new GUIContent("重合", "和起点重合"), GUILayout.MaxWidth(40)))
            {
                point.CopyStartCoordinate();
            }
            EditorGUILayout.EndHorizontal();
            if (end.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawScenePrefab(end, true);
                });
            }
        }
    }
}
