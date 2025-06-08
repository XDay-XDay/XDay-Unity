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
using XDay.WorldAPI.City.Editor;
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.House.Editor
{
    internal partial class HouseEditor
    {
        protected override void InspectorGUIInternal()
        {
            DrawPages();

            DrawAgentSettings();
        }

        private void DrawPages()
        {
            var newEditMode = GUILayout.Toolbar((int)m_EditMode, m_PageNames, GUILayout.MaxWidth(200));
            if (newEditMode != (int)m_EditMode)
            {
                SetEditMode((EditMode)newEditMode);
            }

            if (m_EditMode == EditMode.House)
            {
                DrawHouses();
            }
            else
            {
                DrawHouseInstances();
            }
        }

        private void DrawHouses()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowHouseSettings = EditorGUILayout.Foldout(m_ShowHouseSettings, "房间模板");
            if (GUILayout.Button("创建房间模板", GUILayout.MaxWidth(100)))
            {
                OpenCreateHouseWindow();
            }
            EditorGUILayout.EndHorizontal();
            if (m_ShowHouseSettings)
            {
                for (var i = 0; i < m_Houses.Count; ++i)
                {
                    var house = m_Houses[i];

                    EditorGUI.indentLevel++;
                    bool deleted = DrawHouseSetting(house, i);
                    if (deleted)
                    {
                        UndoSystem.DestroyObject(house, "Destroy House", ID);
                        break;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorHelper.HorizontalLine();
        }

        private bool DrawHouseSetting(House house, int index)
        {
            bool deleted = false;
            EditorGUILayout.BeginHorizontal();

            EditorHelper.DrawFullRect(new Color32(0, 162, 132, 128), new Vector2(40, 0));

            EditorGUIUtility.labelWidth = 10;
            var oldSelected = m_ActiveHouseID == house.ID;
            bool selected = EditorGUILayout.ToggleLeft(GUIContent.none, oldSelected, GUILayout.MaxWidth(30));
            if (selected != oldSelected)
            {
                SetActiveHouse(house.ID);
            }

            EditorGUIUtility.labelWidth = 0;
            house.ShowInInspector = EditorGUILayout.Foldout(house.ShowInInspector, $"{index}.{house.Name}");

            EditorGUILayout.Space();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                CommandLocate(house.Root);
                SetEditMode(EditMode.House);
            }

            if (GUILayout.Button("旋转", GUILayout.MaxWidth(40)))
            {
                RotateHouse(house);
            }

            if (GUILayout.Button(new GUIContent("同步修改", "将房间模板的修改同步到房间中"), GUILayout.MaxWidth(70)))
            {
                CopyHouseModifications(house);
            }

            if (GUILayout.Button("创建房间", GUILayout.MaxWidth(70)))
            {
                CreateHouseInstance(house);
            }

            if (GUILayout.Button(new GUIContent("删除", "删除房间模板"), GUILayout.MaxWidth(35)))
            {
                if (EditorUtility.DisplayDialog("注意", "确定删除房间模板?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (house.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    house.Name = EditorGUILayout.TextField("名字", house.Name);
                    GUI.enabled = false;
                    EditorGUILayout.FloatField("格子大小(米)", house.GridSize);
                    EditorGUILayout.ObjectField("模型", house.Prefab, typeof(GameObject), allowSceneObjects: false);
                    GUI.enabled = true;
                    DrawInteractivePointSettings(house);
                    DrawTeleporterSettings(house);
                });
            }

            return deleted;
        }

        private void DrawHouseInstances()
        {
            DrawScenePrefab(this);

            m_ShowHouseInstanceSettings = EditorGUILayout.Foldout(m_ShowHouseInstanceSettings, "房间");
            if (m_ShowHouseInstanceSettings)
            {
                for (var i = 0; i < m_HouseInstances.Count; ++i)
                {
                    var houseInstance = m_HouseInstances[i];
                    EditorGUI.indentLevel++;
                    bool deleted = DrawHouseInstanceSetting(houseInstance, i);
                    if (deleted)
                    {
                        UndoSystem.DestroyObject(houseInstance, "Destroy House Instance", ID);
                        break;
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorHelper.HorizontalLine();
        }

        private bool DrawHouseInstanceSetting(HouseInstance houseInstance, int index)
        {
            bool deleted = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;

            EditorHelper.DrawFullRect(new Color32(0, 162, 132, 128), new Vector2(40, 0));

            var oldSelected = m_ActiveHouseInstanceID == houseInstance.ID;
            bool selected = EditorGUILayout.ToggleLeft(GUIContent.none, oldSelected, GUILayout.MaxWidth(30));
            if (selected != oldSelected)
            {
                SetActiveHouseInstance(houseInstance.ID);
            }
            EditorGUIUtility.labelWidth = 0;
            houseInstance.ShowInInspector = EditorGUILayout.Foldout(houseInstance.ShowInInspector, $"{index}.{houseInstance.Name}");
            EditorGUILayout.Space();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                CommandLocate(houseInstance.Root);
                SetEditMode(EditMode.HouseInstance);
            }

            if (GUILayout.Button(new GUIContent("删除", "删除房间"), GUILayout.MaxWidth(35)))
            {
                if (EditorUtility.DisplayDialog("注意", "确定删除房间?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (houseInstance.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    houseInstance.Name = EditorGUILayout.TextField("名字", houseInstance.Name);
                    houseInstance.ConfigID = EditorGUILayout.IntField("ID", houseInstance.ConfigID);
                    var house = GetHouse(houseInstance.HouseID);
                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = false;
                    EditorGUILayout.TextField("房间模板", house.Name);
                    GUI.enabled = true;
                    if (GUILayout.Button(new GUIContent("=>", "选中模板"), GUILayout.MaxWidth(25)))
                    {
                        SetActiveHouse(house.ID);
                    }
                    EditorGUILayout.EndHorizontal();
                    DrawTeleporterInstanceSettings(houseInstance);
                    DrawInteractivePointInstanceSettings(houseInstance);
                });
            }

            return deleted;
        }

        private void OpenCreateHouseWindow()
        {
            var parameters = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.ObjectParameter("房间模型", "", null, typeof(GameObject), false),
                new ParameterWindow.StringParameter("房间名字", "", "新房间"),
                new ParameterWindow.FloatParameter("房间格子大小", "", 1),
            };
            ParameterWindow.Open("创建房间", parameters, (items) => {
                bool ok = ParameterWindow.GetObject<GameObject>(items[0], out var prefab);
                ok &= ParameterWindow.GetString(items[1], out var name);
                ok &= ParameterWindow.GetFloat(items[2], out var gridSize);
                if (ok && prefab != null)
                {
                    var collider = prefab.transform.GetComponentInChildren<UnityEngine.BoxCollider>();
                    if (collider == null)
                    {
                        EditorUtility.DisplayDialog("出错了", "无法创建房间, 房间Prefab没有BoxCollider, BoxCollider用来确定房间的格子覆盖范围!", "确定");
                        return false;
                    }

                    if (!Helper.IsGameObjectTransformIdentity(prefab))
                    {
                        EditorUtility.DisplayDialog("出错了", "无法创建房间, 房间Prefab根节点的Transform不是初始值!请检查prefab的根节点Transform组件设置", "确定");
                        return false;
                    }

                    var house = CreateHouse(name, AssetDatabase.GetAssetPath(prefab), gridSize);
                    UndoSystem.CreateObject(house, World.ID, "Create House", ID);
                    UndoSystem.NextGroupAndJoin();
                    return true;
                }
                return false;
            });
        }

        private void CreateHouseInstance(House house)
        {
            var instance = CreateHouseInstance(house, house.Name + "实例");
            UndoSystem.CreateObject(instance, World.ID, "Create House Instance", ID, 0, (newInstance) => {
                var newHouseInstance = newInstance as HouseInstance;
                house.CopyTo(newHouseInstance);
                SetActiveHouseInstance(newHouseInstance.ID);
            });
            UndoSystem.NextGroupAndJoin();
        }

        private void DrawAgentSettings()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowAgentSettings = EditorGUILayout.Foldout(m_ShowAgentSettings, "机器人");
            if (GUILayout.Button("创建机器人", GUILayout.MaxWidth(80)))
            {
                CommandAddAgentTemplate();
            }
            EditorGUILayout.EndHorizontal();
            if (m_ShowAgentSettings)
            {
                for (var i = 0; i < m_AgentTemplates.Count; ++i)
                {
                    var agent = m_AgentTemplates[i];
                    EditorHelper.IndentLayout(() =>
                    {
                        DrawAgentSetting(agent, i);
                    });
                }
            }

            EditorHelper.HorizontalLine();
        }

        private void DrawAgentSetting(HouseAgentTemplate agent, int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", m_SelectedAgentTemplateIndex == index, GUILayout.MaxWidth(20)))
            {
                m_SelectedAgentTemplateIndex = index;
            }
            EditorGUIUtility.labelWidth = 0;
            agent.ShowInInspector = EditorGUILayout.Foldout(agent.ShowInInspector, $"{index}.{agent.Name}");
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            if (agent.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    agent.Name = EditorGUILayout.TextField("名字", agent.Name);
                    agent.Type = EditorGUILayout.IntField("类型", agent.Type);
                    agent.Size = EditorGUILayout.Vector2IntField("大小", agent.Size);
                    agent.MoveSpeed = EditorGUILayout.FloatField("移动速度", agent.MoveSpeed);
                    agent.RotateSpeed = EditorGUILayout.FloatField("转向速度", agent.RotateSpeed);
                    agent.RunAnimName = EditorGUILayout.TextField("Run动画名", agent.RunAnimName);
                    agent.IdleAnimName = EditorGUILayout.TextField("Idle动画名", agent.IdleAnimName);
                    var newPrefab = EditorGUILayout.ObjectField("模型", agent.Prefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
                    if (EditorHelper.IsPrefab(newPrefab) || newPrefab == null)
                    {
                        agent.Prefab = newPrefab;
                    }
                });
            }
        }

        private void CommandAddAgentTemplate()
        {
            var agent = new HouseAgentTemplate(NextObjectID)
            {
                Name = "机器人"
            };
            m_AgentTemplates.Add(agent);

            if (m_SelectedAgentTemplateIndex < 0)
            {
                m_SelectedAgentTemplateIndex = 0;
            }
        }

        private void RotateHouse(House house)
        {
            if (house != null)
            {
                if (EditorUtility.DisplayDialog("注意", "确定旋转房间内物体?", "确定", "取消"))
                {
                    house.RotateObjects();
                }
            }
        }

        private void CopyHouseModifications(House house)
        {
            foreach (var instance in m_HouseInstances)
            {
                if (instance.HouseID == house.ID)
                {
                    house.CopyTo(instance);
                }
            }
        }

        private void DrawScenePrefab(IScenePrefabSetter setter, 
            bool keepPosition = false, 
            System.Action onSetPrefab = null, 
            System.Action drawExtra = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            setter.Visible = EditorGUILayout.ToggleLeft("", setter.Visible, GUILayout.MaxWidth(20));
            EditorGUIUtility.labelWidth = 0;
            var newPrefab = (GameObject)EditorGUILayout.ObjectField("场景模型", setter.Prefab, typeof(GameObject), false);
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
                    SceneView.lastActiveSceneView.pivot = setter.PrefabInstance.transform.position;
                    Selection.activeGameObject = setter.PrefabInstance;
                    SetOperation(OperationType.Select);
                }
            }
            drawExtra?.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        [SerializeField]
        private bool m_ShowAgentSettings = true;
        [SerializeField]
        private bool m_ShowHouseSettings = true;
        [SerializeField]
        private bool m_ShowHouseInstanceSettings = true;
        [SerializeField]
        private bool m_ShowTeleporterSettings = true;
        [SerializeField]
        private bool m_ShowTeleporterInstanceSettings = true;
        private GUIContent[] m_PageNames = new GUIContent[]
        {
            new GUIContent("房间模板", "房间模板设置"),
            new GUIContent("房间", "房间设置"),
        };
    }
}
