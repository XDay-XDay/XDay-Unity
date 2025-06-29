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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Navigation.Editor
{
    internal partial class NavigationPlugin
    {
        protected override void InspectorGUIInternal()
        {
            DrawBuildParam();
        }

        protected override void SceneGUISelectedInternal()
        {
            var e = Event.current;
            if (m_Action == Operation.FindPath)
            {
                if (e.type == EventType.MouseDown)
                {
                    if (e.button == 0 && !e.alt)
                    {
                        var worldPosition = Helper.GUIRayCastWithXZPlane(e.mousePosition, World.CameraManipulator.Camera);
                        bool ready = m_PathFinder.Set(worldPosition);
                        SceneView.RepaintAll();
                        if (ready && m_LastBuiltResult != null)
                        {
                            m_NavigationSystem = new();
                            m_NavigationSystem.Init(m_LastBuiltResult.MeshVertices, m_LastBuiltResult.MeshIndices);
                            List<Vector3> path = new();
                            m_NavigationSystem.FindPath(m_PathFinder.Start, m_PathFinder.End, path, null);
                            m_DrawPath.SetVertices(path);
                            m_Renderer.Debugger.Init(m_NavigationSystem);
                        }
                    }
                }

                m_PathFinder.Draw(1);

                HandleUtility.AddDefaultControl(0);
            }
        }

        protected override void SceneViewControlInternal(Rect sceneViewRect)
        {
            var evt = Event.current;
            if ((evt.type == EventType.KeyDown) && evt.shift == false)
            {
                if (evt.keyCode == KeyCode.Alpha1 && evt.control)
                {
                    ChangeOperation(Operation.Select);
                    evt.Use();
                }
                else if (evt.keyCode == KeyCode.Alpha2 && evt.control)
                {
                    ChangeOperation(Operation.FindPath);
                    evt.Use();
                }
            }

            CreateUIControls();

            EditorGUILayout.BeginHorizontal();
            {
                DrawOperation();

                DrawBuildNavMeshButton();
                DrawClearNavMeshButton();
                DrawShowDebugInfoButton();
                DrawEnableDepthTestButton();

                GUILayout.Space(30);
            }
            EditorGUILayout.EndHorizontal();

            DrawDescription();
        }

        private void CreateUIControls()
        {
            if (m_Controls == null)
            {
                m_Controls = new();

                m_PopupOperation = new Popup("操作", "", 130);
                m_Controls.Add(m_PopupOperation);

                m_BuildNavMeshButton = EditorWorldHelper.CreateImageButton("build.png", "生成NavMesh");
                m_Controls.Add(m_BuildNavMeshButton);

                m_ClearNavMeshButton = EditorWorldHelper.CreateImageButton("clear.png", "清除NavMesh");
                m_Controls.Add(m_ClearNavMeshButton);

                m_ShowDebugInfoButton = EditorWorldHelper.CreateToggleImageButton(false, "show.png", "显隐调试信息");
                m_Controls.Add(m_ShowDebugInfoButton);

                m_EnableDepthTestButton = EditorWorldHelper.CreateToggleImageButton(false, "depth.png", "NavMesh显示是否开启深度测试");
                m_Controls.Add(m_EnableDepthTestButton);
            }
        }

        public override List<UIControl> GetSceneViewControls()
        {
            return m_Controls;
        }

        protected override void SelectionChangeInternal(bool selected)
        {
            if (selected)
            {
                ChangeOperation(m_Action);
            }
            else
            {
                Tools.hidden = false;
            }
        }

        private void DrawDescription()
        {
            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label);
            }

            EditorGUILayout.LabelField("Ctrl+1/Ctrl+2切换操作");
        }
        
        private void DrawOperation()
        {
            ChangeOperation((Operation)m_PopupOperation.Render((int)m_Action, m_ActionNames, 35));
        }

        private void DrawBuildNavMeshButton()
        {
            if (m_BuildNavMeshButton.Render(World != null))
            {
                if (EditorUtility.DisplayDialog("生成导航网格", "确定生成导航网格?", "确定", "取消"))
                {
                    BuildNavMesh();
                }
            }
        }

        private void DrawClearNavMeshButton()
        {
            if (m_ClearNavMeshButton.Render(World != null))
            {
                if (EditorUtility.DisplayDialog("清理导航网格", "确定清理导航网格?", "确定", "取消"))
                {
                    ClearNavMesh();
                }
            }
        }

        private void DrawShowDebugInfoButton()
        {
            m_ShowDebugInfoButton.Active = m_ShowDebugInfo;
            if (m_ShowDebugInfoButton.Render(true, GUI.enabled))
            {
                m_Renderer.Debugger.Show = m_ShowDebugInfoButton.Active;
                m_ShowDebugInfo = m_ShowDebugInfoButton.Active;
            }
        }

        private void DrawEnableDepthTestButton()
        {
            m_EnableDepthTestButton.Active = m_EnableDepthTest;
            if (m_EnableDepthTestButton.Render(true, GUI.enabled))
            {
                m_EnableDepthTest = m_EnableDepthTestButton.Active;
                m_Renderer.SetEnableDepthTest(m_EnableDepthTest);
            }
        }

        private void ChangeOperation(Operation operation)
        {
            m_Action = operation;
            if (m_Action != Operation.Select)
            {
                Tools.hidden = true;
            }
            else
            {
                Tools.hidden = false;
            }
        }

        private void DrawBuildParam()
        {
            m_BuildParam.agentClimb = EditorGUILayout.FloatField(new GUIContent("Agent Climp", "最大攀爬高度,This parameter is used to detect sharp discontinuities in the level (i.e. stairs, steps), and allow the agent to pass them."), m_BuildParam.agentClimb);
            m_BuildParam.agentSlope = EditorGUILayout.FloatField(new GUIContent("Agent Slope", "The maximum slope angle which is walkable (angle in degrees).The valid range is 0–60 degrees. Steep slopes will be excluded from the resulting NavMesh. Please note that setting the slope higher than 45 can give artifacts due to the voxelization process - i.e. a steep slope cannot be distinguished from a wall."), m_BuildParam.agentSlope);
            m_BuildParam.agentRadius = EditorGUILayout.FloatField(new GUIContent("Agent Radius", "The resulting NavMesh will be shrunk by this radius to make sure that agents do not clip to walls when close to obstacles, in some scenarios it can be useful to reduce this radius."), m_BuildParam.agentRadius);
            m_BuildParam.agentHeight = EditorGUILayout.FloatField(new GUIContent("Agent Height", "Agent高度,NavMesh will be removed from areas with a ceiling lower than this height"), m_BuildParam.agentHeight);
            m_BuildParam.tileSize = EditorGUILayout.IntField(new GUIContent("Tile Size", "Sets the tile size in voxel units.The tile size is specified in units of voxels per tile side length.The NavMesh is built in square tiles in order to build the mesh in parallel and to control maximum memory usage. It also helps to make the carving changes more local. If you plan to update NavMesh at runtime, a good tile size is around 32–128 voxels (roughly 5 to 20 meters for human size characters). 64 is good value to start, and you can use the profiler window to find a good trade off. Default value is 256, which is good for static baking. If you use a lot of carving obstacles you can try a smaller size if you see in the profiler that a lot of time is being spent on carving."), m_BuildParam.tileSize);
            m_BuildParam.voxelSize = EditorGUILayout.FloatField(new GUIContent("Voxel Size", "Sets the voxel size in world length units.The NavMesh is built by first voxelizing the Scene, and then figuring out walkable spaces from the voxelized representation of the Scene. The voxel size controls how closely the NavMesh fits the geometry of your Scene, and is defined in world units.\r\n\r\nIf you require a more detail so that the NavMesh more closely fits your Scene’s geometry, you can reduce the voxel size. An increase in detail will also cause your game to consume more memory and take more time to calculate the NavMesh data. The scaling is roughly quadratic, so doubling the resolution will result in an approximate quadrupling of the build time.\r\n\r\nIn general you should aim to have 4-6 voxels per character diameter. For example, if you have a Scene with characters that have a radius of 0.3, a good voxel size is 0.1. The default value is set to a third of the agentRadius.\r\n\r\nNote: If you want to use this setting, you must also set overrideVoxelSize to true."), m_BuildParam.voxelSize);
            m_BuildParam.ledgeDropHeight = EditorGUILayout.FloatField(new GUIContent("Ledge Drop Height", "Maximum agent drop height.\r\n\r\nDrop-Down link generation is controlled by the Drop Height parameter. The parameter controls what is the highest drop that will be connected, setting the value to 0 will disable the generation.\r\n\r\nThe trajectory of the drop-down link is defined so that the horizontal travel is: 2*agentRadius + 4*voxelSize. That is, the drop will land just beyond the edge of the platform. In addition the vertical travel needs to be more than bake settings’ Step Height (otherwise we could just step down) and less than Drop Height. The adjustment by voxel size is done so that any round off errors during voxelization does not prevent the links being generated. You should set the Drop Height to a bit larger value than what you measure in your level, so that the links will connect properly."), m_BuildParam.ledgeDropHeight);
            m_BuildParam.maxJumpAcrossDistance = EditorGUILayout.FloatField(new GUIContent("Max Jump Across Distance", "Maximum agent jump distance.\r\n\r\nJump-Across link generation is controlled by the Jump Distance parameter. The parameter controls what is the furthest distance that will be connected. Setting the value to 0 will disable the generation.\r\n\r\nThe trajectory of the jump-across link is defined so that the horizontal travel is more than 2*agentRadius and less than Jump Distance. In addition the landing location must not be further than voxelSize from the level of the start location."), m_BuildParam.maxJumpAcrossDistance);
            m_BuildParam.minRegionArea = EditorGUILayout.FloatField(new GUIContent("Min Region Area", "The approximate minimum area of individual NavMesh regions.\r\n\r\nThis property allows you to cull away small non-connected NavMesh regions. NavMesh regions whose surface area is smaller than the specified value, will be removed.\r\n\r\nNote: some regions may not get removed. The NavMesh is built in parallel as a grid of tiles. If a region straddles a tile boundary, the region is not removed. The reason for this is that the region pruning happens at a stage in the build process where surrounding tiles are not available."), m_BuildParam.minRegionArea);
            m_BuildParam.overrideTileSize = EditorGUILayout.Toggle(new GUIContent("Override Tile Size", "Enables overriding the default tile size"), m_BuildParam.overrideTileSize);
            m_BuildParam.overrideVoxelSize = EditorGUILayout.Toggle(new GUIContent("Override Voxel Size", "Enables overriding the default voxel size"), m_BuildParam.overrideVoxelSize);
            m_BuildParam.preserveTilesOutsideBounds = EditorGUILayout.Toggle(new GUIContent("Preserve Tiles Outside Bounds", ""), m_BuildParam.preserveTilesOutsideBounds);
            m_BuildParam.buildHeightMesh = EditorGUILayout.Toggle(new GUIContent("Build Height Mesh", "Enables the creation of additional data needed to determine the height at any position on the NavMesh more accurately.\r\n\r\nThe NavMesh Agent is constrained to the surface of the NavMesh as it navigates. Since the NavMesh is an approximation of the walkable space, some features are evened out when the NavMesh is built. For example, stairs may appear as a slope in the NavMesh. If you need accurate placement of the agent for your game, enable height mesh building when you build the NavMesh. Note that building the height mesh will take up memory and processing at runtime, and it increases the time needed to bake the NavMesh.\r\n\r\nThe current implementation of the height mesh has the following limitations:\r\n\r\nIt can construct height data for a Terrain only when its horizontal plane is parallel to the XZ plane of the NavMesh.\r\nDuring a NavMesh update, if the build setting \"preserveTilesOutsideBounds\" is true the height mesh will not be created and if it already exists, will be removed."), m_BuildParam.buildHeightMesh);
        }

        private enum Operation
        {
            Select,
            FindPath,
        }

        private ImageButton m_BuildNavMeshButton;
        private ImageButton m_ClearNavMeshButton;
        private ToggleImageButton m_ShowDebugInfoButton;
        private ToggleImageButton m_EnableDepthTestButton;
        private GUIStyle m_LabelStyle;
        private List<UIControl> m_Controls;
        private Popup m_PopupOperation;
        private Operation m_Action = Operation.Select;
        private DrawPolyLineInEditor m_DrawPath;
        private string[] m_ActionNames = new string[]
        {
            "选择",
            "寻路",
        };
    }
}
