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
using XDay.WorldAPI.Editor;

namespace XDay.WorldAPI.Region.Editor
{
    internal partial class RegionSystem
    {
        private void DrawRegionSettings()
        {
            CreateStyles();

            var layer = GetCurrentLayer();
            if (layer == null)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            layer.ShowRegions = EditorGUILayout.Foldout(layer.ShowRegions, "区域");
            if (GUILayout.Button("增加区域", GUILayout.MaxWidth(80)))
            {
                CommandAddRegion(layer);
            }
            EditorGUILayout.EndHorizontal();
            if (layer.ShowRegions)
            {
                layer.UpdateRegionNames();

                var deleted = false;
                EditorHelper.IndentLayout(() =>
                {
                    for (var i = 0; i < layer.RegionCount; ++i)
                    {
                        var region = layer.Regions[i];
                        deleted = DrawRegionSetting(region, i);
                        if (deleted)
                        {
                            UndoSystem.DestroyObject(region, "Remove Region", layer.System.ID);
                            --layer.SelectedRegionIndex;
                            break;
                        }
                    }
                });
            }

            EditorHelper.HorizontalLine();
        }

        private void CommandAddRegion(RegionSystemLayer layer)
        {
            var region = new RegionObject(World.AllocateObjectID(), layer.RegionCount, layer.ID, Random.ColorHSV(0, 1, 0, 1, 1, 1), 0, "Region", Vector3.zero);
            UndoSystem.CreateObject(region, World.ID, "Add Region System Layer", layer.System.ID, lod: 0);
            layer.SelectedRegionIndex = layer.RegionCount - 1;
        }

        private bool DrawRegionSetting(RegionObject region, int index)
        {
            var deleted = false;
            var layer = GetCurrentLayer();
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 10;
            if (EditorGUILayout.ToggleLeft("", layer.SelectedRegionIndex == index, GUILayout.MaxWidth(20)))
            {
                if (layer.SelectedRegionIndex != index)
                {
                    layer.SelectedRegionIndex = index;
                }
            }
            EditorGUIUtility.labelWidth = 0;
            var style = region.Lock ? m_FoldoutStyle : EditorStyles.foldout;
            region.ShowInInspector = EditorGUILayout.Foldout(region.ShowInInspector, region.Name, style);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("选中建筑", GUILayout.MaxWidth(80)))
            {
                EditorHelper.PingObject(layer.Renderer.GetRegionGameObject(region.ID));
                ChangeOperation(Operation.Select);
            }

            if (GUILayout.Button(new GUIContent("建筑居中", "将建筑放到区域中心点"), GUILayout.MaxWidth(80)))
            {
                var position = layer.GetRegionCenter(region.ID);
                var gameObject = layer.Renderer.GetRegionGameObject(region.ID);
                gameObject.transform.position = position;
            }

            if (GUILayout.Button("删除区域", GUILayout.MaxWidth(80)))
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
                        layer.Renderer.UpdateColors(0, 0, layer.HorizontalGridCount - 1, layer.VerticalGridCount - 1);
                    }
                    EditorGUILayout.EndHorizontal();
                });
            }

            return deleted;
        }

        private void CreateStyles()
        {
            m_FoldoutStyle ??= new GUIStyle(EditorStyles.foldout)
            {
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

        private GUIStyle m_FoldoutStyle;
    }
}
