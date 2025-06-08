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
using XDay.WorldAPI.City.Editor;

namespace XDay.WorldAPI.House.Editor
{
    /// <summary>
    /// 交互点
    /// </summary>
    internal partial class HouseEditor
    {
        private void DrawInteractivePointSettings(House house)
        {
            EditorGUILayout.BeginHorizontal();

            EditorHelper.DrawFullRect(new Color32(255, 127, 39, 90), new Vector2(27, 0));
            m_ShowInteractivePointSettings = EditorGUILayout.Foldout(m_ShowInteractivePointSettings, "交互点");

            if (GUILayout.Button("增加交互点", GUILayout.MaxWidth(90)))
            {
                CommandAddInteractivePoint(house);
            }
            EditorGUILayout.EndHorizontal();

            var points = house.InteractivePoints;
            if (m_ShowInteractivePointSettings)
            {
                EditorHelper.IndentLayout(() =>
                {
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
                            house.RemoveInteractivePoint(i);
                            break;
                        }
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        private void DrawInteractivePointInstanceSettings(HouseInstance houseInstance)
        {
            m_ShowInteractivePointInstanceSettings = EditorGUILayout.Foldout(m_ShowInteractivePointInstanceSettings, "交互点");

            var points = houseInstance.InteractivePointInstance;
            if (m_ShowInteractivePointInstanceSettings)
            {
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < points.Count; ++i)
                    {
                        var point = points[i];
                        EditorHelper.IndentLayout(() =>
                        {
                            DrawInteractivePointInstanceSetting(point, i);
                        });
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        private bool DrawInteractivePointSetting(HouseInteractivePoint point, int index)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorGUILayout.BeginHorizontal();
            point.ShowInInspector = EditorGUILayout.Foldout(point.ShowInInspector, $"{index}.{point.Name}");

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
                    DrawStart(point.Start, true);
                    DrawEnd(point, true);
                });
            }
            EditorGUILayout.EndVertical();

            return deleted;
        }

        private void DrawInteractivePointInstanceSetting(HouseInteractivePointInstance point, int index)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorHelper.DrawFullRect(new Color32(255, 127, 39, 90), new Vector2(27, 0));
            point.ShowInInspector = EditorGUILayout.Foldout(point.ShowInInspector, $"{index}.{point.Name}");
            if (point.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    GUI.enabled = false;
                    EditorGUILayout.TextField("名字", point.Name);
                    GUI.enabled = true;
                    point.ConfigID = EditorGUILayout.IntField("ID", point.ConfigID);
                    DrawStart(point.Start, false);
                    DrawEnd(point, false);
                });
            }
            EditorGUILayout.EndVertical();
        }

        private void CommandAddInteractivePoint(House house)
        {
            var point = new HouseInteractivePoint(World.AllocateObjectID())
            {
                Name = "交互点",
            };
            house.AddInteractivePoint(point);

            var camera = SceneView.lastActiveSceneView.camera;
            point.Position = Helper.RayCastWithXZPlane(camera.pixelRect.center, camera);

            Selection.activeGameObject = point.Start.PrefabInstance;
        }

        private void DrawStart(InteractivePointStartCoordinate start, bool enable)
        {
            start.ShowInInspector = EditorGUILayout.Foldout(start.ShowInInspector, "起点");
            if (start.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawScenePrefab(enable, start, true);
                });
            }
        }

        private void DrawEnd(HouseInteractivePoint point, bool enable)
        {
            var end = point.End;
            EditorGUILayout.BeginHorizontal();
            end.ShowInInspector = EditorGUILayout.Foldout(end.ShowInInspector, "终点");
            GUI.enabled = enable;
            if (GUILayout.Button(new GUIContent("重合", "和起点重合"), GUILayout.MaxWidth(40)))
            {
                point.CopyStartCoordinate();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            if (end.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    DrawScenePrefab(enable, end, true, null, null);
                });
            }
        }

        private void DrawScenePrefab(bool enable,
            IScenePrefabSetter setter, 
            bool keepPosition = false, 
            System.Action onSetPrefab = null, 
            System.Action drawExtra = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            setter.Visible = EditorGUILayout.ToggleLeft("", setter.Visible, GUILayout.MaxWidth(20));
            EditorGUIUtility.labelWidth = 0;
            GUI.enabled = enable;
            var newPrefab = (GameObject)EditorGUILayout.ObjectField("模型", setter.Prefab, typeof(GameObject), false);
            GUI.enabled = true;
            if (newPrefab != setter.Prefab)
            {
                var oldPrefab = setter.Prefab;
                var oldPos = Vector3.zero;
                var oldRot = Quaternion.identity;
                if (keepPosition)
                {
                    oldPos = setter.Position;
                    oldRot = setter.Rotation;
                }

                setter.Prefab = newPrefab;

                if (keepPosition)
                {
                    setter.Position = oldPos;
                    setter.Rotation = oldRot;
                }

                if (oldPrefab == null)
                {
                    var camera = SceneView.lastActiveSceneView.camera;
                    setter.Position = Helper.RayCastWithXZPlane(camera.pixelRect.center, camera);
                }

                onSetPrefab?.Invoke();
            }
            if (GUILayout.Button(new GUIContent("定位", "选中模型,可编辑模型坐标，旋转，大小"), GUILayout.MaxWidth(40)))
            {
                if (setter.PrefabInstance != null)
                {
                    if (enable)
                    {
                        SetEditMode(EditMode.House);
                    }
                    else
                    {
                        SetEditMode(EditMode.HouseInstance);
                    }
                    SceneView.lastActiveSceneView.pivot = setter.PrefabInstance.transform.position;
                    Selection.activeGameObject = setter.PrefabInstance;
                    SetOperation(OperationType.Select);
                }
            }
            GUI.enabled = enable;
            drawExtra?.Invoke();
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
    }
}
