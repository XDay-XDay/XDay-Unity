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

namespace XDay.WorldAPI.Editor
{
    internal class DrawGridSetting
    {
        public void Draw(EditorWorld world)
        {
            m_Show = EditorGUILayout.Foldout(m_Show, "格子设置");
            if (m_Show)
            {
                EditorGUI.indentLevel++;
                world.EnableGrid = EditorGUILayout.ToggleLeft("显示格子", world.EnableGrid);

                var grid = world.Grid;
                EditorGUI.BeginChangeCheck();
                var newHorizontalGridCount = EditorGUILayout.IntField("横向格子数", grid.HorizontalGridCount);
                var newVerticalGridCount = EditorGUILayout.IntField("纵向格子数", grid.VerticalGridCount);
                var newGridWidth = EditorGUILayout.FloatField("格子宽(米)", grid.GridWidth);
                var newGridHeight = EditorGUILayout.FloatField("格子高(米)", grid.GridHeight);
                if (EditorGUI.EndChangeCheck())
                {
                    var material = new Material(Shader.Find("XDay/Grid"));
                    var newGrid = new GridMesh("Custom Grid", newHorizontalGridCount, newVerticalGridCount, newGridWidth, newGridHeight, material, Color.white, world.Root.transform, true);
                    newGrid.SetActive(grid.GetActive());
                    world.Grid = newGrid;
                }
                EditorGUI.indentLevel--;
            }
        }

        private bool m_Show = true;
    }
}
