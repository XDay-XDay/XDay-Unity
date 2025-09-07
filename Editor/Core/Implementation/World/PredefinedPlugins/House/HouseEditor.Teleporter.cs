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

namespace XDay.WorldAPI.House.Editor
{
    /// <summary>
    /// 传送点
    /// </summary>
    internal partial class HouseEditor
    {
        private void DrawTeleporterSettings(House house)
        {
            EditorGUILayout.BeginHorizontal();

            EditorHelper.DrawFullRect(new Color32(117, 249, 77, 90), new Vector2(27, 0));

            m_ShowTeleporterSettings = EditorGUILayout.Foldout(m_ShowTeleporterSettings, "传送点");

            if (GUILayout.Button("增加传送点", GUILayout.MaxWidth(90)))
            {
                CommandAddTeleporter(house);
            }
            EditorGUILayout.EndHorizontal();

            var teleporters = house.Teleporters;
            if (m_ShowTeleporterSettings)
            {
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < teleporters.Count; ++i)
                    {
                        var teleporter = teleporters[i];
                        var deleted = false;
                        EditorHelper.IndentLayout(() =>
                        {
                            deleted = DrawTeleporterSetting(teleporter, i);
                        });
                        if (deleted)
                        {
                            house.RemoveTeleporter(i);
                            break;
                        }
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        private void DrawTeleporterInstanceSettings(HouseInstance house)
        {
            GetTeleporterInstanceNames();

            m_ShowTeleporterInstanceSettings = EditorGUILayout.Foldout(m_ShowTeleporterInstanceSettings, "传送点");

            var teleporters = house.TeleporterInstances;
            if (m_ShowTeleporterInstanceSettings)
            {
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < teleporters.Count; ++i)
                    {
                        var teleporter = teleporters[i];
                        EditorHelper.IndentLayout(() =>
                        {
                            DrawTeleporterInstanceSetting(teleporter, i);
                        });
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        private bool DrawTeleporterSetting(HouseTeleporter teleporter, int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 20;
            EditorGUILayout.LabelField($"{index}", GUILayout.MaxWidth(30));
            //teleporter.ShowInInspector = EditorHelper.Foldout(teleporter.ShowInInspector);
            teleporter.Name = EditorGUILayout.TextField(GUIContent.none, teleporter.Name, GUILayout.MinWidth(100));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                SetEditMode(EditMode.House);
                CommandLocate(teleporter.GameObject);
            }

            var deleted = false;
            if (GUILayout.Button("删除传送点", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除传送点 \"{teleporter.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            return deleted;
        }

        private void DrawTeleporterInstanceSetting(HouseTeleporterInstance teleporter, int index)
        {
            EditorGUILayout.BeginVertical("GroupBox");
            EditorHelper.DrawFullRect(new Color32(117, 249, 77, 90), new Vector2(27, 0));
            EditorGUILayout.BeginHorizontal();
            teleporter.ShowInInspector = EditorGUILayout.Foldout(teleporter.ShowInInspector, $"{index}.{teleporter.Name}");

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                SetEditMode(EditMode.HouseInstance);
                CommandLocate(teleporter.GameObject);
            }
            EditorGUILayout.EndHorizontal();

            if (teleporter.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    teleporter.ConfigID = EditorGUILayout.IntField("ID", teleporter.ConfigID);
                    GUI.enabled = false;
                    teleporter.Name = EditorGUILayout.TextField("名字", teleporter.Name);
                    GUI.enabled = true;
                    var oldIndex = m_TeleporterInstanceIDs.IndexOf(teleporter.ConnectedID);
                    var newIndex = EditorGUILayout.Popup("连接的传送点ID", oldIndex, m_TeleporterInstanceNames);
                    if (newIndex != oldIndex)
                    {
                        teleporter.ConnectedID = m_TeleporterInstanceIDs[newIndex];
                        var otherTeleporter = GetTeleporterInstance(m_TeleporterInstanceIDs[newIndex]);
                        if (otherTeleporter != null)
                        {
                            otherTeleporter.ConnectedID = teleporter.ConfigID;
                        }
                    }
                    teleporter.Enabled = EditorGUILayout.Toggle("开启", teleporter.Enabled);
                });
            }

            EditorGUILayout.EndVertical();
        }

        private void CommandAddTeleporter(House house)
        {
            var teleporter = new HouseTeleporter(World.AllocateObjectID())
            {
                Name = $"传送点{house.Teleporters.Count + 1}"
            };

            house.AddTeleporter(teleporter);

            var camera = SceneView.lastActiveSceneView.camera;
            teleporter.GameObject.transform.position = Helper.RayCastWithXZPlane(camera.pixelRect.center, camera);

            CommandLocate(teleporter.GameObject);
        }

        private void CommandLocate(GameObject gameObject)
        {
            SceneView.lastActiveSceneView.pivot = gameObject.transform.position;
            Selection.activeGameObject = gameObject;

            SetOperation(OperationType.Select);
        }

        private void GetTeleporterInstanceNames()
        {
            m_TempList.Clear();
            foreach (var house in m_HouseInstances)
            {
                foreach (var teleporter in house.TeleporterInstances)
                {
                    m_TempList.Add(teleporter);
                }
            }

            if (m_TeleporterInstanceNames == null || m_TempList.Count + 1 != m_TeleporterInstanceNames.Length)
            {
                m_TeleporterInstanceNames = new string[m_TempList.Count + 1];
            }
            m_TeleporterInstanceNames[0] = "无连接";
            m_TeleporterInstanceIDs.Clear();
            m_TeleporterInstanceIDs.Add(0);
            for (var i = 1; i <= m_TempList.Count; ++i)
            {
                m_TeleporterInstanceNames[i] = $"{i - 1}-{m_TempList[i - 1].House.Name}-{m_TempList[i - 1].Name}";
                m_TeleporterInstanceIDs.Add(m_TempList[i - 1].ConfigID);
            }
            m_TempList.Clear();
        }
    }
}