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
using XDay.UtilityAPI;

namespace XDay.WorldAPI.City.Editor
{
    partial class CityEditor
    {
        protected override void InspectorGUIInternal()
        {
            CreateStyles();

            DrawGridSettings();

            DrawRegionSettings();

            DrawBuildingSettings();

            DrawBuildingEditor();

            DrawInteractivePointSettings();

            DrawWaypointSettings();

            DrawGridLabelSettings();

            DrawAgentSettings();
        }

        void CommandAddGrid()
        {
            var input = new List<ParameterWindow.Parameter>()
            {
                new ParameterWindow.StringParameter("名字", "", "CityGrid"),
                new ParameterWindow.IntParameter("横向格子数", "", 50),
                new ParameterWindow.IntParameter("纵向格子数", "", 50),
                new ParameterWindow.FloatParameter("格子大小", "", 1.0f),
                new ParameterWindow.FloatParameter("旋转角度", "", 0),
            };
            ParameterWindow.Open("创建格子", input, (items) =>
            {
                var ok = ParameterWindow.GetString(items[0], out var name);
                ok &= ParameterWindow.GetInt(items[1], out var horizontalGridCount);
                ok &= ParameterWindow.GetInt(items[2], out var verticalGridCount);
                ok &= ParameterWindow.GetFloat(items[3], out var gridSize);
                ok &= ParameterWindow.GetFloat(items[4], out var rotation);
                if (ok)
                {
                    var tiles = new GridTileInstance[horizontalGridCount * verticalGridCount];
                    for (var i = 0; i < tiles.Length; ++i)
                    {
                        tiles[i] = new GridTileInstance();
                    }
                    AddGrid(name, horizontalGridCount, verticalGridCount, gridSize, Quaternion.Euler(0, rotation, 0), tiles);
                    return true;
                }
                return false;
            });
        }

        void DrawGridSettings()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowGridSettings = EditorGUILayout.Foldout(m_ShowGridSettings, "格子");
            if (GUILayout.Button("增加格子", GUILayout.MaxWidth(80)))
            {
                CommandAddGrid();
            }
            EditorGUILayout.EndHorizontal();
            if (m_ShowGridSettings)
            {
                UpdateGridNames();
                EditorHelper.IndentLayout(() =>
                {
                    m_SelectedGridIndex = EditorGUILayout.Popup("当前格子", m_SelectedGridIndex, m_GridNames);
                });

                for (var i = 0; i < m_Grids.Count; ++i)
                {
                    var grid = m_Grids[i];
                    EditorHelper.IndentLayout(() =>
                    {
                        DrawGridSetting(grid);
                    });
                }
            }

            EditorHelper.HorizontalLine();
        }

        void DrawGridSetting(Grid grid)
        {
            EditorGUILayout.BeginHorizontal();
            grid.ShowInInspector = EditorGUILayout.Foldout(grid.ShowInInspector, grid.Name);
            if (GUILayout.Button(new GUIContent("移动物体", ""), GUILayout.MaxWidth(80)))
            {
                MoveObjects();
            }

            if (GUILayout.Button(new GUIContent("选中", "在Hierarchy窗口中选中"), GUILayout.MaxWidth(40)))
            {
                Selection.activeGameObject = grid.RootGameObject;
            }
            EditorGUILayout.EndHorizontal();
            if (grid.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    grid.Name = EditorGUILayout.TextField("名字", grid.Name);
                    GUI.enabled = false;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.IntField("横向格子数", grid.HorizontalGridCount);
                    EditorGUILayout.IntField("纵向格子数", grid.VerticalGridCount);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.FloatField("格子大小", grid.GridSize);
                    GUI.enabled = true;

                    var oldRotation = grid.Rotation.eulerAngles.y;
                    var rotation = EditorGUILayout.FloatField("旋转", oldRotation);
                    if (!Mathf.Approximately(rotation, oldRotation))
                    {
                        grid.Rotation = Quaternion.Euler(0, rotation, 0);
                    }
                    grid.Position = EditorGUILayout.Vector3Field("位置", grid.Position);
                });
            }
        }

        void DrawRegionSettings()
        {
            if (m_SelectedGridIndex < 0)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            m_ShowRegions = EditorGUILayout.Foldout(m_ShowRegions, "区域");
            if (GUILayout.Button("增加区域", GUILayout.MaxWidth(80)))
            {
                CommandAddRegion();
            }
            EditorGUILayout.EndHorizontal();
            if (m_ShowRegions)
            {
                var grid = GetSelectedGrid();
                grid.UpdateRegionNames();

                EditorHelper.IndentLayout(() =>
                {
                    var index = EditorGUILayout.Popup("当前区域", grid.SelectedRegionIndex, grid.RegionNames);
                    if (index != grid.SelectedRegionIndex)
                    {
                        grid.SelectedRegionIndex = index;
                        Grayout(grid);
                    }
                });

                if (grid.SelectedRegionIndex >= 0)
                {
                    var regions = m_Grids[m_SelectedGridIndex].RegionTemplates;
                    var region = regions[grid.SelectedRegionIndex];
                    var deleted = false;
                    EditorHelper.IndentLayout(() =>
                    {
                        deleted = DrawRegionSetting(region, grid.SelectedRegionIndex);
                    });
                    if (deleted)
                    {
                        m_Grids[m_SelectedGridIndex].RemoveRegionTemplate(grid.SelectedRegionIndex);
                    }
                }
            }

            EditorHelper.HorizontalLine();
        }

        bool DrawRegionSetting(RegionTemplate region, int index)
        {
            var deleted = false;
            var grid = GetSelectedGrid();
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", grid.SelectedRegionIndex == index, GUILayout.MaxWidth(20)))
            {
                if (grid.SelectedRegionIndex != index)
                {
                    grid.SelectedRegionIndex = index;
                    Grayout(grid);
                }
            }
            EditorGUIUtility.labelWidth = 0;
            var style = region.Lock ? m_FoldoutStyle : EditorStyles.foldout;
            region.ShowInInspector = EditorGUILayout.Foldout(region.ShowInInspector, region.Name, style);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("删除区域", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除区域 \"{region.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            if (region.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    region.ConfigID = EditorGUILayout.IntField("ID", region.ConfigID);
                    region.Name = EditorGUILayout.TextField("名字", region.Name);
                    EditorGUILayout.BeginHorizontal();
                    region.Color = EditorGUILayout.ColorField("颜色", region.Color);
                    if (GUILayout.Button("应用", GUILayout.MaxWidth(40)))
                    {
                        grid.GetLayer<RegionLayer>().UpdateColors();
                    }
                    EditorGUILayout.EndHorizontal();

                    DrawScenePrefab(region);

                    DrawAreaSettings(region);
                    
                    DrawEventSettings(region);
                });
            }

            return deleted;
        }

        void DrawScenePrefab(IScenePrefabSetter setter, bool keepPosition = false, System.Action onSetPrefab = null, System.Action drawExtra = null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            setter.Visible = EditorGUILayout.ToggleLeft("", setter.Visible, GUILayout.MaxWidth(20));
            EditorGUIUtility.labelWidth = 0;
            var newPrefab = (GameObject)EditorGUILayout.ObjectField("模型", setter.Prefab, typeof(GameObject), false);
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

        void DrawAreaSettings(RegionTemplate region)
        {
            EditorGUILayout.BeginHorizontal();
            region.ShowAreaTemplates = EditorGUILayout.Foldout(region.ShowAreaTemplates, "地块");
            if (GUILayout.Button("增加地块", GUILayout.MaxWidth(90)))
            {
                CommandAddArea(region);
            }
            EditorGUILayout.EndHorizontal();
            if (region.ShowAreaTemplates)
            {
                var areas = region.AreaTemplates;
                for (var i = 0; i < areas.Count; ++i)
                {
                    var area = areas[i];
                    var deleted = false;
                    EditorHelper.IndentLayout(() =>
                    {
                        deleted = DrawAreaSetting(area, region, i);
                    });
                    if (deleted)
                    {
                        region.RemoveAreaTemplate(i);
                        break;
                    }
                }

                EditorHelper.HorizontalLine();
            }
        }

        bool DrawAreaSetting(AreaTemplate area, RegionTemplate region, int index)
        {
            var deleted = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", region.SelectedAreaTemplateIndex == index, GUILayout.MaxWidth(20)))
            {
                region.SetSelectedAreaTemplate(index);
            }
            EditorGUIUtility.labelWidth = 0;
            var style = area.Lock ? m_FoldoutStyle : EditorStyles.foldout;
            area.ShowInInspector = EditorGUILayout.Foldout(area.ShowInInspector, area.Name, style);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("删除地块", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除地块 \"{area.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            if (area.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    area.ConfigID = EditorGUILayout.IntField("ID", area.ConfigID);
                    area.Name = EditorGUILayout.TextField("名字", area.Name);
                    EditorGUILayout.BeginHorizontal();
                    area.Color = EditorGUILayout.ColorField("颜色", area.Color);
                    if (GUILayout.Button("应用", GUILayout.MaxWidth(40)))
                    {
                        GetSelectedGrid().GetLayer<AreaLayer>().UpdateColors();
                    }
                    EditorGUILayout.EndHorizontal();
                });
            }

            return deleted;
        }

        void DrawGridLabelSettings()
        {
            var grid = GetSelectedGrid();
            if (grid != null)
            {
                EditorGUILayout.BeginHorizontal();
                m_ShowGridLabels = EditorGUILayout.Foldout(m_ShowGridLabels, "寻路地面类型");
                if (GUILayout.Button("增加类型", GUILayout.MaxWidth(80)))
                {
                    CommandAddGridLabel();
                }
                EditorGUILayout.EndHorizontal();
                if (m_ShowGridLabels)
                {
                    var gridLabels = grid.GridLabels;
                    for (var i = 0; i < gridLabels.Count; ++i)
                    {
                        var gridLabel = gridLabels[i];
                        var deleted = false;
                        EditorHelper.IndentLayout(() =>
                        {
                            deleted = DrawGridLabel(gridLabel, i);
                        });
                        if (deleted)
                        {
                            grid.RemoveGridLabel(i);
                            break;
                        }
                    }

                    EditorHelper.HorizontalLine();
                }
            }
        }

        bool DrawGridLabel(GridLabelTemplate gridLabel, int index)
        {
            var deleted = false;
            var grid = GetSelectedGrid();
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", grid.SelectedGridLabelIndex == index, GUILayout.MaxWidth(20)))
            {
                if (grid.SelectedGridLabelIndex != index)
                {
                    grid.SelectedGridLabelIndex = index;
                    Grayout(grid);
                }
            }
            EditorGUIUtility.labelWidth = 0;
            
            gridLabel.ShowInInspector = EditorGUILayout.Foldout(gridLabel.ShowInInspector, gridLabel.Name);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("删除", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除区域 \"{gridLabel.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            if (gridLabel.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    gridLabel.Name = EditorGUILayout.TextField("名字", gridLabel.Name);
                    gridLabel.Value = (byte)Mathf.Clamp(EditorGUILayout.IntField("值", gridLabel.Value), 1, byte.MaxValue);
                    EditorGUILayout.BeginHorizontal();
                    gridLabel.Color = EditorGUILayout.ColorField("颜色", gridLabel.Color);
                    if (GUILayout.Button("应用", GUILayout.MaxWidth(40)))
                    {
                        grid.GetLayer<GridLabelLayer>().UpdateColors();
                    }
                    EditorGUILayout.EndHorizontal();
                    gridLabel.Walkable = EditorGUILayout.Toggle("可行走", gridLabel.Walkable);
                });
            }

            return deleted;
        }

        void DrawEventSettings(RegionTemplate region)
        {
            EditorGUILayout.BeginHorizontal();
            region.ShowEventTemplates = EditorGUILayout.Foldout(region.ShowEventTemplates, "事件");
            if (GUILayout.Button("增加事件", GUILayout.MaxWidth(90)))
            {
                CommandAddEvent(region);
            }
            EditorGUILayout.EndHorizontal();
            if (region.ShowEventTemplates)
            {
                var events = region.EventTemplates;
                for (var i = 0; i < events.Count; ++i)
                {
                    var e = events[i];
                    var deleted = false;
                    EditorHelper.IndentLayout(() =>
                    {
                        deleted = DrawEventSetting(e, region, i);
                    });
                    if (deleted)
                    {
                        region.RemoveEventTemplate(i);
                        break;
                    }
                }

                EditorHelper.HorizontalLine();
            }
        }

        bool DrawEventSetting(EventTemplate e, RegionTemplate region, int index)
        {
            var deleted = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", region.SelectedEventTemplateIndex == index, GUILayout.MaxWidth(20)))
            {
                region.SetSelectedEventTemplate(index);   
            }
            EditorGUIUtility.labelWidth = 0;
            e.ShowInInspector = EditorGUILayout.Foldout(e.ShowInInspector, e.Name);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(50)))
            {
                var center = e.Center;
                SceneView.lastActiveSceneView.pivot = GetSelectedGrid().CoordinateToGridCenterPosition(center.x, center.y);
            }

            if (GUILayout.Button(new GUIContent("格子数", "事件是否只占一个格子"), GUILayout.MaxWidth(50)))
            {
                EditorUtility.DisplayDialog("", $"事件占有{e.Coordinates.Count}个格子", "确定");  
            }

            if (GUILayout.Button("删除事件", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除事件 \"{e.Name}\"?", "确定", "取消"))
                {
                    deleted = true;
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            if (e.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    e.ConfigID = EditorGUILayout.IntField("ID", e.ConfigID);
                    e.Name = EditorGUILayout.TextField("名字", e.Name);
                    EditorGUILayout.BeginHorizontal();
                    e.Color = EditorGUILayout.ColorField("颜色", e.Color);
                    if (GUILayout.Button("应用", GUILayout.MaxWidth(40)))
                    {
                        Debug.Assert(false);
                        //GetSelectedGrid().GetLayer<LandLayer>().UpdateColors();
                    }
                    EditorGUILayout.EndHorizontal();

                    e.UseGroundHeight = EditorGUILayout.ToggleLeft(new GUIContent("使用地形高度", "使用地形高度或者使用设施高度"), e.UseGroundHeight);

                    DrawScenePrefab(e, false,
                        () =>
                        {
                            e.SyncToGridPosition();
                        },
                    () =>
                    {
                        if (GUILayout.Button(new GUIContent("同步位置", "将模型位置设置到事件的格子位置"), GUILayout.MaxWidth(80)))
                        {
                            e.SyncToGridPosition();
                        }
                    });
                });
            }

            return deleted;
        }

        void CommandAddTile()
        {
            var tile = new GridTileTemplate(NextObjectID, "未知类型");
            m_Tiles.Add(tile);
        }

        void CommandAddRegion()
        {
            var grid = m_Grids[m_SelectedGridIndex];
            var region = new RegionTemplate(NextObjectID, $"区域{grid.RegionTemplates.Count + 1}", Random.ColorHSV(0, 1, 0, 1, 1, 1), GetNextRegionConfigID());
            grid.AddRegionTemplate(region);
        }

        void CommandAddArea(RegionTemplate region)
        {
            var area = new AreaTemplate(NextObjectID, $"地块 {region.AreaTemplateCount + 1}", Random.ColorHSV(0, 1, 0, 1, 1, 1), GetNextAreaConfigID());
            region.AddAreaTemplate(area);
        }

        void CommandAddLand(RegionTemplate region)
        {
            var land = new LandTemplate(NextObjectID, $"绿地 {region.LandTemplateCount + 1}", Random.ColorHSV(0, 1, 0, 1, 1, 1), GetNextLandConfigID());
            region.AddLandTemplate(land);
        }

        void CommandAddGridLabel()
        {
            var grid = m_Grids[m_SelectedGridIndex];
            var n = grid.GridLabelCount;
            var gridLabel = new GridLabelTemplate(NextObjectID, $"地面类型 {n + 1}", Random.ColorHSV(0, 1, 0, 1, 1, 1), (byte)n, 1.0f, true);
            grid.AddGridLabel(gridLabel);
        }

        void CommandAddEvent(RegionTemplate region)
        {
            var e = new EventTemplate(NextObjectID, $"事件 {region.EventTemplateCount + 1}", Random.ColorHSV(0, 1, 0, 1, 1, 1), GetNextEventConfigID());
            region.AddEventTemplate(e);
        }

        void DrawBuildingEditor()
        {
            var grid = GetSelectedGrid();
            if (grid != null)
            {
                grid.RoomEditor.DrawInspector();
            }
        }

        void DrawBuildingSettings()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowBuildingSettings = EditorGUILayout.Foldout(m_ShowBuildingSettings, "建筑");

            var show = GUILayout.Toggle(m_ShowBuildingNames, "名字", GUILayout.MaxWidth(100));
            if (show != m_ShowBuildingNames)
            {
                m_ShowBuildingNames = show;
                var displayNames = Object.FindObjectsByType<DisplayName>(FindObjectsSortMode.None);
                foreach(var name in displayNames)
                {
                    name.Show(show);
                }
            }

            if (GUILayout.Button(new GUIContent("刷新", "房间改变后需手动刷新模型"), GUILayout.MaxWidth(40)))
            {
                var grid = GetSelectedGrid();
                foreach (var building in grid.Buildings)
                {
                    building.ChangeModel();
                }
            }
            if (GUILayout.Button("增加建筑", GUILayout.MaxWidth(80)))
            {
                CommandAddBuildingTemplate();
            }

            EditorGUILayout.EndHorizontal();
            if (m_ShowBuildingSettings)
            {
                for (var i = 0; i < m_BuildingTemplates.Count; ++i)
                {
                    var buildingTemplate = m_BuildingTemplates[i];
                    var deleted = false;
                    EditorHelper.IndentLayout(() =>
                    {
                        deleted = DrawBuildingSetting(buildingTemplate, i);
                    });
                    if (deleted)
                    {
                        RemoveBuildingTemplate(i);
                        break;
                    }
                }
            }

            EditorHelper.HorizontalLine();
        }

        bool DrawBuildingSetting(BuildingTemplate building, int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", m_SelectedBuildingTemplateIndex == index, GUILayout.MaxWidth(20)))
            {
                m_SelectedBuildingTemplateIndex = index;
            }
            EditorGUIUtility.labelWidth = 0;
            building.ShowInInspector = EditorGUILayout.Foldout(building.ShowInInspector, building.Name);

            GUILayout.FlexibleSpace();

            var grid = GetSelectedGrid();

            if (GUILayout.Button("定位", GUILayout.MaxWidth(40)))
            {
                if (grid != null)
                {
                    var buildingInstance = grid.GetBuildingInstanceOfType(building.ConfigID);
                    if (buildingInstance != null)
                    {
                        SceneView.lastActiveSceneView.pivot = buildingInstance.Position;
                        Selection.activeGameObject = buildingInstance.GameObject;
                        SetOperation(OperationType.Building);
                        SetOperation(OperationType.Select);
                        m_SelectedBuildingTemplateIndex = index;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("出错了", "地图上没有摆放该ID的建筑", "确定");
                    }
                }
            }

            if (GUILayout.Button(new GUIContent("刷新", "房间改变后需手动刷新模型"), GUILayout.MaxWidth(40)))
            {
                var buildingInstance = grid.GetBuildingInstanceOfType(building.ConfigID);
                if (buildingInstance != null)
                {
                    buildingInstance.ChangeModel();
                }
            }

            var deleted = false;
            if (GUILayout.Button("删除建筑", GUILayout.MaxWidth(90)))
            {
                if (EditorUtility.DisplayDialog("注意", $"确定删除建筑 \"{building.Name}\"?", "确定", "取消"))
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
                    var buildingInstance = grid.GetBuildingInstanceOfType(building.ConfigID);
                    if (buildingInstance != null)
                    {
                        buildingInstance.Name = building.Name;
                    }
                    building.ConfigID = EditorGUILayout.IntField("ID", building.ConfigID);
                    GUI.enabled = false;
                    EditorGUILayout.Vector2IntField("占地范围", building.Size);
                    GUI.enabled = true;

                    if (grid != null)
                    {
                        var curIndex = grid.RoomEditor.GetRoomIndex(building.RoomID);
                        var newIndex = EditorGUILayout.Popup("建筑", curIndex, grid.RoomEditor.RoomNames);
                        if (curIndex != newIndex)
                        {
                            building.RoomID = grid.RoomEditor.GetRoomPrefab(newIndex).ID;
                        }
                    }
                });
            }

            return deleted;
        }

        void CommandAddBuildingTemplate()
        {
            var building = new BuildingTemplate(NextObjectID)
            {
                Name = "建筑"
            };
            building.Initialize(this);
            m_BuildingTemplates.Add(building);

            m_SelectedBuildingTemplateIndex = m_BuildingTemplates.Count - 1;

            SetOperation(OperationType.Building);
        }

        void RemoveBuildingTemplate(int index)
        {
            foreach (var grid in m_Grids)
            {
                grid.RemoveBuildingInstanceOfType(m_BuildingTemplates[index].ID);
            }

            m_BuildingTemplates.RemoveAt(index);

            --m_SelectedBuildingTemplateIndex;
            if (m_SelectedBuildingTemplateIndex < 0 && m_BuildingTemplates.Count > 0)
            {
                m_SelectedBuildingTemplateIndex = 0;
            }
        }

        void DrawAgentSettings()
        {
            EditorGUILayout.BeginHorizontal();
            m_ShowAgentSettings = EditorGUILayout.Foldout(m_ShowAgentSettings, "漫游");
            if (GUILayout.Button("增加NPC", GUILayout.MaxWidth(80)))
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

        void DrawAgentSetting(AgentTemplate agent, int index)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", m_SelectedAgentTemplateIndex == index, GUILayout.MaxWidth(20)))
            {
                m_SelectedAgentTemplateIndex = index;
            }
            EditorGUIUtility.labelWidth = 0;
            agent.ShowInInspector = EditorGUILayout.Foldout(agent.ShowInInspector, agent.Name);
            EditorGUILayout.Space();
            EditorGUILayout.EndHorizontal();
            if (agent.ShowInInspector)
            {
                EditorHelper.IndentLayout(() =>
                {
                    agent.Name = EditorGUILayout.TextField("名字", agent.Name);
                    agent.Type = EditorGUILayout.IntField("类型", agent.Type);
                    agent.Size = EditorGUILayout.Vector2IntField("大小", agent.Size);
                    var newPrefab = EditorGUILayout.ObjectField("模型", agent.Prefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
                    if (EditorHelper.IsPrefab(newPrefab) || newPrefab == null)
                    {
                        agent.Prefab = newPrefab;
                    }
                });
            }
        }

        void CommandAddAgentTemplate()
        {
            var agent = new AgentTemplate(NextObjectID)
            {
                Name = "人物"
            };
            m_AgentTemplates.Add(agent);

            if (m_SelectedAgentTemplateIndex < 0)
            {
                m_SelectedAgentTemplateIndex = 0;
            }
        }

        void CommandShowGridOccupyState(bool show)
        {
            foreach (var grid in m_Grids)
            {
                grid.ShowOccupyState(show);
            }
        }

        void CommandShowGridCost(bool show)
        {
            foreach (var grid in m_Grids)
            {
                grid.ShowGridCost(show);
            }
        }

        void CommandShowGridLine(bool show)
        {
            foreach (var grid in m_Grids)
            {
                grid.ShowGridLine(show);
            }
        }

        void UpdateGridNames()
        {
            if (m_GridNames == null || m_GridNames.Length != m_Grids.Count)
            {
                m_GridNames = new string[m_Grids.Count];
            }

            for (var i = 0; i < m_GridNames.Length; ++i)
            {
                m_GridNames[i] = m_Grids[i].Name;
            }
        }

        Grid GetSelectedGrid()
        {
            if (m_SelectedGridIndex >= 0)
            {
                return m_Grids[m_SelectedGridIndex];
            }
            return null;
        }

        RegionTemplate GetSelectedRegionTemplate()
        {
            var grid = GetSelectedGrid();
            if (grid != null)
            {
                if (grid.SelectedRegionIndex >= 0)
                {
                    return grid.RegionTemplates[grid.SelectedRegionIndex];
                }
            }
            return null;
        }

        AreaTemplate GetSelectedAreaTemplate()
        {
            var region = GetSelectedRegionTemplate();
            if (region != null && region.SelectedAreaTemplateIndex >= 0)
            {
                return region.AreaTemplates[region.SelectedAreaTemplateIndex];
            }
            return null;
        }

        LandTemplate GetSelectedLandTemplate()
        {
            var region = GetSelectedRegionTemplate();
            if (region != null && region.SelectedLandTemplateIndex >= 0)
            {
                return region.LandTemplates[region.SelectedLandTemplateIndex];
            }
            return null;
        }

        EventTemplate GetSelectedEventTemplate()
        {
            var region = GetSelectedRegionTemplate();
            if (region != null && region.SelectedEventTemplateIndex >= 0)
            {
                return region.EventTemplates[region.SelectedEventTemplateIndex];
            }
            return null;
        }

        int GetNextRegionConfigID()
        {
            var maxID = int.MinValue;
            var grid = GetSelectedGrid();
            foreach (var region in grid.RegionTemplates)
            {
                if (region.ConfigID > maxID)
                {
                    maxID = region.ConfigID;
                }
            }

            if (maxID == int.MinValue)
            {
                maxID = 0;
            }

            return maxID + 1;
        }

        int GetNextAreaConfigID()
        {
            var maxID = int.MinValue;
            var grid = GetSelectedGrid();
            foreach (var region in grid.RegionTemplates)
            {
                foreach (var area in region.AreaTemplates)
                {
                    if (area.ConfigID > maxID)
                    {
                        maxID = area.ConfigID;
                    }
                }
            }

            if (maxID == int.MinValue)
            {
                maxID = 0;
            }

            return maxID + 1;
        }

        int GetNextLandConfigID()
        {
            var maxID = int.MinValue;
            var grid = GetSelectedGrid();
            foreach (var region in grid.RegionTemplates)
            {
                foreach (var land in region.LandTemplates)
                {
                    if (land.ConfigID > maxID)
                    {
                        maxID = land.ConfigID;
                    }
                }
            }

            if (maxID == int.MinValue)
            {
                maxID = 0;
            }

            return maxID + 1;
        }

        int GetNextEventConfigID()
        {
            var maxID = int.MinValue;
            var grid = GetSelectedGrid();
            foreach (var region in grid.RegionTemplates)
            {
                foreach (var e in region.EventTemplates)
                {
                    if (e.ConfigID > maxID)
                    {
                        maxID = e.ConfigID;
                    }
                }
            }

            if (maxID == int.MinValue)
            {
                maxID = 0;
            }

            return maxID + 1;
        }

        void CreateStyles()
        {
            m_FoldoutStyle ??= new GUIStyle(EditorStyles.foldout)
                {
                    // 修改文字颜色
                    normal = { textColor = Color.red },
                    onNormal = { textColor = Color.red },
                    hover = { textColor = Color.red },
                    onHover = { textColor = Color.red },
                    focused = { textColor = Color.red },
                    onFocused = { textColor = Color.red },
                    active = { textColor = Color.red },
                    onActive = { textColor = Color.red }
                };
        }

        public bool ShowName => m_ShowBuildingNames;

        [SerializeField]
        bool m_ShowGridSettings = true;
        [SerializeField]
        bool m_ShowGridLabels = true;
        [SerializeField]
        bool m_ShowRegions = true;
        [SerializeField]
        bool m_ShowBuildingSettings = true;
        [SerializeField]
        bool m_ShowBuildingNames = true;
        [SerializeField]
        bool m_ShowInteractivePointSettings = true;
        [SerializeField]
        bool m_ShowWaypointSettings = true;
        [SerializeField]
        bool m_ShowAllWaypoints = true;
        [SerializeField]
        bool m_ShowAllInteractivePoints = true;
        [SerializeField]
        bool m_ShowAgentSettings = true;
        [SerializeField]
        bool m_ShowTileSettings = true;

        [SerializeField]
        int m_SelectedTileIndex = 0;

        [SerializeField]
        int m_SelectedBuildingTemplateIndex = -1;

        [SerializeField]
        int m_SelectedAgentTemplateIndex = -1;

        [SerializeField]
        int m_SelectedGridIndex = -1;
        [SerializeField]
        string[] m_GridNames;

        string[] m_GroundTypeNames = new string[3]
        {
            "荒地",
            "绿地",
            "地貌",
        };

        GUIStyle m_FoldoutStyle;
    }
}
