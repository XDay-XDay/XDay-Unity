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
using XDay.CameraAPI.Editor;

namespace XDay.WorldAPI.Editor
{
    public static partial class WorldEditor
    {
        public static void SceneGUI()
        {
			if (!IsInEditorScene())
			{
				return;
			}

            UpdateControls();

            var oldPadding = GUI.skin.button.padding;
            int pad = 1;
            GUI.skin.button.padding = new RectOffset(pad, pad, pad, pad);

            if (m_ActiveWorld != null)
            {
                UndoRefresh();

                ShowContextMenu();

                PluginSceneGUI();

                DrawBounds();
            }

			DrawControls();

            m_CameraEditor?.SceneGUI();

            GUI.skin.button.padding = oldPadding;
		}

        public static void InspectorGUI()
        {
            if (!IsInEditorScene())
            {
                return;
            }

            BeginScrollView();

            DrawToolbar();

            EndScrollView();
        }

        private static void DrawControls()
        {
            if (m_ActiveWorld != null)
            {
                DrawEditControls();
            }
            else
            {
                DrawEntranceControls();
            }
        }

        private static void DrawRangeText(float x, float y, float width, float height)
        {
            GUI.Label(new Rect(x, y, width, height), $"地图范围: {m_ActiveWorld.Bounds.min:F1}到{m_ActiveWorld.Bounds.max:F1}米");
        }

        private static void DrawEntranceControls()
        {
            if (EditorHelper.IsPrefabMode())
            {
                return;
            }

            var sceneView = SceneView.currentDrawingSceneView;

            BeginGUI(sceneView.camera);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                DrawResetButton();
                DrawCreateButton();
                DrawLoadButton();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            DrawPluginControls(sceneView);

            EndGUI();
        }

        private static void DrawEditControls()
        {
            if (EditorHelper.IsPrefabMode())
            {
                return;
            }

            var sceneView = SceneView.currentDrawingSceneView;

            BeginGUI(sceneView.camera);

            EditorGUILayout.BeginHorizontal();
            {
                DrawPluginToggle();

                DrawPluginList();

                DrawAddPluginButton();

                GUI.enabled = m_ActiveWorld.SelectedPluginIndex >= 0;

                DrawDeletePluginButton();

                DrawRenamePluginButton();

                GUILayout.Space(50);

                DrawLoadButton();

                DrawGameDataButton();

                DrawSaveButton();

                DrawResetButton();

                GUILayout.Space(20);

                GUI.enabled = true;

                var rect = sceneView.camera.pixelRect;
                var width = 400.0f;
                DrawRangeText(rect.center.x - width * 0.5f, rect.yMax - 30, width, 20);

                DrawLoadingProgress(0, rect.yMax - 50, 300, 20);

                GUILayout.Space(100);

                DrawHooks();
            }
            EditorGUILayout.EndHorizontal();

            DrawPluginControls(sceneView);

            EndGUI();
        }

        private static void DrawWorldInspectorGUI()
        {
            EditorGUILayout.BeginVertical();

            m_DrawWorldSetting = EditorGUILayout.Foldout(m_DrawWorldSetting, new GUIContent("地图设置", "通用的地图设置"));
            if (m_DrawWorldSetting)
            {
                EditorHelper.IndentLayout(() => 
                {
                    EditorGUILayout.BeginHorizontal();

                    GUI.enabled = m_ActiveWorld != null;

                    DrawOpenEditorFolderButton();
                    DrawOpenGameFolderButton();
                    DrawSelectGameFolderButton();

                    GUI.enabled = true;

					EditorGUILayout.EndHorizontal();
				});

                DrawWorldSetting();

                DrawGridSetting();

                DrawCameraSetting();

                DrawLODList();
            }

            EditorGUILayout.EndVertical();
		}

        private static void DrawCameraSetting()
        {
            EditorHelper.HorizontalLine();
            m_CameraEditor?.InspectorGUI();
        }

        private static void UpdatePluginNames()
        {
            if (m_ActiveWorld != null)
            {
                if (m_PluginNames == null || m_PluginNames.Length != m_ActiveWorld.PluginCount)
                {
                    m_PluginNames = new string[m_ActiveWorld.PluginCount];
				}

                for (var i = 0; i < m_PluginNames.Length; i++)
                {
                    m_PluginNames[i] = m_ActiveWorld.GetPlugin(i).Name;
                }

				if (m_PluginNames.Length == 0)
				{
					SelectedPluginIndex = -1;
				}
			}
        }

        private static void DrawHooks()
        {
            foreach (var hook in m_Hooks)
            {
                if (hook.Show)
                {
                    if (GUILayout.Button(new GUIContent(hook.DisplayName, hook.Tooltip), GUILayout.MaxWidth(hook.ButtonWidth)))
                    {
                        hook.Execute();
                    }
                }
            }
        }

        private static void DrawPluginControls(SceneView sceneview)
        {
            SelectedPlugin?.SceneViewControl(sceneview.camera.pixelRect);
        }

		private static void UndoRefresh()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                UndoSystem.NextGroupAndJoin();
            }
        }

		private static void PluginSceneGUI()
        {
            if (RayCastControls())
            {
                for (var i = 0; i < m_ActiveWorld.PluginCount; ++i)
                {
                    var plugin = m_ActiveWorld.GetPlugin(i) as EditorWorldPlugin;
                    plugin.SceneGUI();
				}

                var selectedPlugin = m_ActiveWorld.GetPlugin(SelectedPluginIndex) as EditorWorldPlugin;
                selectedPlugin?.SceneGUISelected();
			}
        }

        private static void DrawToolbar()
        {
			m_SelectedToolIndex = GUILayout.Toolbar(m_SelectedToolIndex, m_ToolbarNames);
            if (m_SelectedToolIndex == 1)
            {
                DrawPluginInspectorGUI();
            }
            else
            {
                DrawWorldInspectorGUI();
            }
		}

        private static void DrawPluginInspectorGUI()
        {
            if (m_ActiveWorld != null &&
				m_ActiveWorld.GetPlugin(SelectedPluginIndex) is EditorWorldPlugin plugin)
            {
                plugin.InspectorGUI();   
            }
        }

        private static void BeginScrollView()
        {
            m_ViewScrollPos = EditorGUILayout.BeginScrollView(m_ViewScrollPos);
        }

        private static void EndScrollView()
        {
            EditorGUILayout.EndScrollView();
        }

        private static void UpdateControls()
        {
            if (m_Controls == null)
            {
                m_Controls = new List<UIControl>();

                m_PluginList = new Popup("", "", 200);
                m_Controls.Add(m_PluginList);

                m_VisibilityToggle = EditorWorldHelper.CreateToggleImageButton(true, "show.png", "显示/隐藏层");
                m_Controls.Add(m_VisibilityToggle);

                m_ResetWorld = EditorWorldHelper.CreateImageButton("reset.png", "重置地图");
                m_Controls.Add(m_ResetWorld);

                m_CreateWorld = EditorWorldHelper.CreateImageButton("create.png", "创建地图");
                m_Controls.Add(m_CreateWorld);

                m_GenerateGameData = EditorWorldHelper.CreateImageButton("export.png", "导出数据");
                m_Controls.Add(m_GenerateGameData);

                m_AddPlugin = EditorWorldHelper.CreateImageButton("add.png", "新建层");
                m_Controls.Add(m_AddPlugin);

                m_LoadWorld = EditorWorldHelper.CreateImageButton("file-open.png", "加载地图Shift+O");
                m_Controls.Add(m_LoadWorld);

                m_DeletePlugin = EditorWorldHelper.CreateImageButton("delete.png", "删除层");
                m_Controls.Add(m_DeletePlugin);

                m_SaveWorld = EditorWorldHelper.CreateImageButton("save.png", "保存地图Shift+S");
                m_Controls.Add(m_SaveWorld);

                m_RenamePlugin = EditorWorldHelper.CreateImageButton("rename.png", "修改层名称");
                m_Controls.Add(m_RenamePlugin);
            }
        }

        private static void DrawBounds()
        {
            var oldColor = Handles.color;
            Handles.color = Color.yellow;
            var bounds = m_ActiveWorld.Bounds;
            Handles.DrawWireCube(bounds.center, bounds.size);
            Handles.color = oldColor;
        }

        private static void OnClickCreatePlugin(object param)
        {
            var world = m_ActiveWorld;
            var pluginInfo = param as WorldPluginInfo;
            if (pluginInfo.IsSingleton &&
                world.HasPlugin(pluginInfo.PluginType))
            {
                EditorUtility.DisplayDialog("Error", $"Can only create one plugin of type {pluginInfo.PluginType}", "OK");
                return;
            }

            var createWindow = EditorWindow.GetWindow(pluginInfo.PluginCreateWindowType, false, $"创建 {pluginInfo.DisplayName}") as WorldPluginCreateWindow;
            createWindow.MakeWindowCenterAndSetSize(500, 700);
            createWindow.Show(() => { SelectedPluginIndex = world.PluginCount - 1; }, world);
        }

        private static void ShowContextMenu()
        {
            if (m_ShowContextMenu)
            {
                if (Event.current.button == 1 &&
                    Event.current.type == EventType.MouseDown) 
                {
                    var showMenu = true;
                    GenericMenu menu = null;
                    if (SelectedPlugin != null)
                    {
                        menu = SelectedPlugin.ContextMenu(out showMenu);
                    }

                    if (showMenu)
                    {
                        menu ??= new GenericMenu();
                        menu.ShowAsContext();

                        Event.current.Use();
                    }

                    SceneView.RepaintAll();
                }
            }
        }

        private static bool RayCastControls(List<UIControl> controls)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                currentEvent.alt == false)
            {
                if (controls != null)
                {
                    for (int i = 0; i < controls.Count; ++i)
                    {
                        if (controls[i].Bounds.Contains(currentEvent.mousePosition))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            return false;
        }

        private static void BeginGUI(Camera camera)
        {
            Handles.BeginGUI();

            GUILayout.BeginArea(camera.pixelRect);
        }

        private static void EndGUI()
        {
            GUILayout.EndArea();

            Handles.EndGUI();
        }

        private static void DrawPluginList()
        {
            SelectedPluginIndex = m_PluginList.Render(SelectedPluginIndex, CreatePluginNames(), 95);
        }

        private static void DrawAddPluginButton()
        {
            if (m_AddPlugin.Render(m_ActiveWorld.Inited))
            {
                var menu = new GenericMenu();

                for (var i = 0; i < m_PluginsInfo.Count; ++i)
                {
                    menu.AddItem(new GUIContent(m_PluginsInfo[i].DisplayName, ""), false, OnClickCreatePlugin, m_PluginsInfo[i]);
                }

                menu.ShowAsContext();
            }
        }

        private static void DrawPluginToggle()
        {
            var plugin = SelectedPlugin;
            if (plugin != null)
            {
                m_VisibilityToggle.Active = plugin.IsActive;
                if (m_VisibilityToggle.Render(true, m_ActiveWorld.Inited))
                {
                    plugin.SetActiveUndo(m_VisibilityToggle.Active);
                }
            }
        }

        private static void DrawRenamePluginButton()
        {
            if (m_RenamePlugin.Render(m_ActiveWorld.Inited))
            {
                var plugin = SelectedPlugin;
                if (plugin == null)
                {
                    return;
                }

                var parameters = new List<ParameterWindow.Parameter>()
                {
                    new ParameterWindow.StringParameter("名称", "", plugin.Name),
                };

                ParameterWindow.Open("修改层名", parameters, (p) => {
                    ParameterWindow.GetString(p[0], out var name);
                    if (!string.IsNullOrEmpty(name) &&
                    !m_ActiveWorld.HasPlugin(name))
                    {
                        UndoSystem.SetAspect(plugin, WorldDefine.ASPECT_NAME, IAspect.FromString(name), "Rename Plugin", 0, UndoActionJoinMode.Both);
                        return true;
                    }
                    return false;
                });
            }
        }

        private static void DrawLoadButton()
        {
            if (m_LoadWorld.Render(true))
            {
                LoadWorldAsync();

                GUIUtility.ExitGUI();
            }
        }

        private static void DrawCreateButton()
        {
            if (m_CreateWorld.Render(true))
            {
                CreateWorld();
            }
        }

        private static void DrawSaveButton()
        {
            if (m_SaveWorld.Render(m_ActiveWorld.Inited))
            {
                EditorSerialize();
            }
        }

        private static void DrawDeletePluginButton()
        {
            if (m_DeletePlugin.Render(m_ActiveWorld.Inited))
            {
                var plugin = SelectedPlugin;
                if (plugin != null)
                {
                    if (EditorUtility.DisplayDialog("警告", $"确定删除?", "确定", "取消"))
                    {
                        UndoSystem.NextGroup();

                        UndoSystem.DestroyObject(plugin, $"Delete {plugin.Name}");
                        SelectedPluginIndex--;
                    }
                }
            }
        }

        private static void DrawGameDataButton()
        {
            if (m_GenerateGameData.Render(m_ActiveWorld.Inited))
            {
                GameSerialize();
            }
        }

        private static void DrawOpenGameFolderButton()
        {
            if (GUILayout.Button(new GUIContent("打开游戏目录", "打开地图游戏数据目录")))
            {
                if (m_ActiveWorld != null)
                {
                    EditorHelper.ShowInExplorer(m_ActiveWorld.Setup.GameFolder);
                }
            }
        }

        private static void DrawSelectGameFolderButton()
        {
            if (GUILayout.Button(new GUIContent("选中游戏目录", "选中地图游戏数据目录")))
            {
                if (m_ActiveWorld != null)
                {
                    EditorHelper.SelectFolder(m_ActiveWorld.Setup.GameFolder);
                }
            }
        }

        private static void DrawResetButton()
        {
            if (m_ResetWorld.Render(true))
            {
                if (EditorUtility.DisplayDialog("重置地图编辑器", "确定重置?未保存的数据会丢失!", "确定", "取消"))
                {
                    ResetScene();
                }
            }
        }

        private static void DrawLODList()
        {
            if (m_ActiveWorld != null)
            {
                EditorHelper.HorizontalLine();

                m_LODEditor.InspectorGUI(m_ActiveWorld.WorldLODSystem, (oldName, newName) => {
                    var pluginCount = m_ActiveWorld.PluginCount;
                    for (var k = 0; k < pluginCount; ++k)
                    {
                        var lodSystem = (m_ActiveWorld.GetPlugin(k) as WorldPlugin).LODSystem;
                        lodSystem?.ChangeLODName(oldName, newName);
                    }
                });
            }
        }

        private static void DrawWorldSetting()
        {
            if (m_ActiveWorld != null)
            {
                m_ShowWorldSetting = EditorGUILayout.Foldout(m_ShowWorldSetting, "地图设置");
                if (m_ShowWorldSetting)
                {
                    m_ActiveWorld.VisibleAreaUpdateDistance = EditorGUILayout.FloatField("Visible Area Update Distance", m_ActiveWorld.VisibleAreaUpdateDistance);
                }
            }
        }

        private static void DrawGridSetting()
        {
            if (m_ActiveWorld != null)
            {
                m_DrawGridSetting.Draw(m_ActiveWorld);
            }
        }

        private static void DrawOpenEditorFolderButton()
        {
            if (GUILayout.Button(new GUIContent("打开地编目录", "打开地编数据目录")))
            {
                if (m_ActiveWorld != null)
                {
                    EditorHelper.ShowInExplorer(m_ActiveWorld.Setup.EditorFolder);
                }
            }
        }

        private static bool RayCastControls()
        {
            m_RayCastControls.Clear();
            m_RayCastControls.AddRange(m_Controls);

            for (var i = 0; i < m_ActiveWorld.PluginCount; ++i)
            {
                var plugin = m_ActiveWorld.GetPlugin(i) as EditorWorldPlugin;
                var controls = plugin.GetSceneViewControls();
                if (controls != null)
                {
                    m_RayCastControls.AddRange(controls);
                }
            }

            return !RayCastControls(m_RayCastControls);
        }

        private static void DrawLoadingProgress(float x, float y, float width, float height)
        {
            if (m_ActiveWorld != null)
            {
                var finishedCount = 0;
                string processingPlugin = null;
                for (var i = 0; i < m_ActiveWorld.PluginCount; ++i)
                {
                    var plugin = m_ActiveWorld.GetPlugin(i) as WorldPlugin;
                    if (!plugin.Inited)
                    {
                        processingPlugin = plugin.Name;
                        break;
                    }
                    else
                    {
                        ++finishedCount;
                    }
                }

                if (processingPlugin != null)
                {
                    GUI.Label(new Rect(x, y, width, height), $"({finishedCount} / {m_ActiveWorld.PluginCount}) Now loading plugin {processingPlugin}");
                }
            }
        }

        [MenuItem("XDay/地图/快捷键/重做 #r")]
        static void RedoCommand()
        {
            UndoSystem.Redo();
        }

        [MenuItem("XDay/地图/快捷键/撤销 #z")]
        static void UndoCommand()
        {
            UndoSystem.Undo();
        }

        [MenuItem("XDay/地图/快捷键/加载地图 #o")]
        static void LoadWorldCommand()
        {
            LoadWorld();
        }

        [MenuItem("XDay/地图/快捷键/保存地图 #s")]
        static void SaveWorldCommand()
        {
            EditorSerialize();
        }

        private static Vector2 m_ViewScrollPos = Vector2.zero;
        private static bool m_DrawWorldSetting = true;
        private static bool m_ShowContextMenu = true;
		private static GUIContent[] m_ToolbarNames = new GUIContent[]
        {
            new GUIContent("地图设置", "通用的地图设置"),
            new GUIContent("层设置", "当前层的设置"),
        };
        private static List<WorldPluginInfo> m_PluginsInfo = new();
        private static int m_SelectedToolIndex = 1;
        private static List<UIControl> m_RayCastControls = new();
        private static ImageButton m_AddPlugin;
        private static ImageButton m_GenerateGameData;
        private static ImageButton m_LoadWorld;
        private static ImageButton m_SaveWorld;
        private static ImageButton m_RenamePlugin;
        private static ImageButton m_ResetWorld;
        private static ImageButton m_DeletePlugin;
        private static ImageButton m_CreateWorld;
        private static Popup m_PluginList;
        private static List<UIControl> m_Controls;
        private static ToggleImageButton m_VisibilityToggle;
        private static WorldLODSystemEditor m_LODEditor = new();
        private static DrawGridSetting m_DrawGridSetting = new();
        private static CameraSetupEditor m_CameraEditor;
        private static bool m_ShowWorldSetting = true;
    }
}

//XDay