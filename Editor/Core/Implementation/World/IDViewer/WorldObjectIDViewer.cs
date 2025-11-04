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

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace XDay.WorldAPI.Editor {

    internal class WorldObjectIDViewer : EditorWindow
    {
        [MenuItem("XDay/地图/World Object ID Viewer")]
        static void Open()
        {
            GetWindow<WorldObjectIDViewer>().Show();
        }

        private void OnGUI()
        {
            var world = WorldEditor.ActiveWorld;
            if (world == null)
            {
                EditorGUILayout.LabelField("地编打开地图后显示");
                return;
            }

            var objects = world.AllObjects;

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            m_GroupedObjects.Clear();

            foreach (var kv in objects)
            {
                var obj = kv.Value;
                AddObjectToGroup(obj);
            }

            DrawGroups();

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroups()
        {
            foreach (var kv in m_GroupedObjects)
            {
                var visible = m_VisibleGroups.Contains(kv.Key);
                var newVisible = EditorGUILayout.Foldout(visible, $"{kv.Key.Name}");
                if (visible != newVisible)
                {
                    if (newVisible)
                    {
                        m_VisibleGroups.Add(kv.Key);
                    }
                    else
                    {
                        m_VisibleGroups.Remove(kv.Key);
                    }
                }

                if (newVisible)
                {
                    foreach (var obj in kv.Value)
                    {
                        EditorGUILayout.IntField("ID", obj.ID);
                    }
                }
            }
        }

        private void AddObjectToGroup(IWorldObject obj)
        {
            m_GroupedObjects.TryGetValue(obj.GetType(), out var group);
            if (group == null)
            {
                group = new();
                m_GroupedObjects[obj.GetType()] = group;
            }
            group.Add(obj);
        }

        private Vector2 m_ScrollPos;
        private Dictionary<Type, List<IWorldObject>> m_GroupedObjects = new();
        private HashSet<Type> m_VisibleGroups = new();
    }
}

