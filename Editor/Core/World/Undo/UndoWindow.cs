/*
 * Copyright (c) 2024 XDay
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

namespace XDay.WorldAPI.Editor
{
    internal class UndoWindow : EditorWindow
    {
        [MenuItem("XDay/World/Undo History")]
        private static void Open()
        {
            var window = GetWindow<UndoWindow>("Undo History");
            window.Show();
        }

        private void OnEnable()
        {
            SetEvents(false);
            SetEvents(true);
        }

        private void OnDisable()
        {
            SetEvents(false);
        }

        private void OnGUI()
        {
            var size = UndoSystem.Size();
            if (size < MB)
            {
                EditorGUILayout.LabelField($"Size: {size / (float)KB} KB");
            }
            else
            {
                EditorGUILayout.LabelField($"Size: {size / (float)MB} MB");
            }

            DrawButtons();

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

            for (var i = 0; i < UndoSystem.Actions.Count; i++)
            {
                DrawAction(i, UndoSystem.Actions[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAction(int index, UndoAction action)
        {
            var pointer = UndoSystem.Pointer;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{index}", GUILayout.MaxWidth(50));

            if (pointer == index)
            {
                EditorGUILayout.Toggle(true, GUILayout.MaxWidth(50));
            }
            else
            {
                if (index >= pointer)
                {
                    if (GUILayout.Button("Redo", GUILayout.MaxWidth(50)))
                    {
                        var n = index - pointer;
                        for (var k = 0; k < n; k++)
                        {
                            UndoSystem.Redo();
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button("Undo", GUILayout.MaxWidth(50)))
                    {
                        var n = pointer - index;
                        for (var k = 0; k < n; k++)
                        {
                            UndoSystem.Undo();
                        }
                    }
                }
            }

            EditorGUILayout.LabelField($"Group {action.Group.ID}", GUILayout.MaxWidth(70));
            EditorGUILayout.LabelField($"Join {action.Group.JoinID}", GUILayout.MaxWidth(70));

            if (action is CustomUndoAction)
            {
                EditorGUILayout.TextField($"{action.Type}@{action.DisplayName}@{action.GetType().Name}");
            }
            else if (action is UndoActionObjectFactory factory)
            {
                EditorGUILayout.TextField($"{action.Type}@{action.DisplayName}@{factory.TypeName}");
            }
            else if (action is UndoActionAspect aspect)
            {
                EditorGUILayout.TextField($"{action.Type}@{action.DisplayName}@{aspect.AspectName}");
            }
            else
            {
                Debug.Assert(false, "unknonw action");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawButtons()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Clear"))
                {
                    UndoSystem.Clear();
                    Repaint();
                }

                if (GUILayout.Button("Redo"))
                {
                    UndoSystem.Redo();
                    Repaint();
                }

                if (GUILayout.Button("Undo"))
                {
                    UndoSystem.Undo();
                    Repaint();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnActionPointerChanged(int newIndex)
        {
            Repaint();
        }

        private void OnActionRemoved(int startIndex, int count)
        {
            Repaint();
        }

        private void OnActionAdded(int index)
        {
            Repaint();
        }

        private void SetEvents(bool add)
        {
            if (add)
            {
                UndoSystem.EventActionPointerChanged += OnActionPointerChanged;
                UndoSystem.EventActionDestroyed += OnActionRemoved;
                UndoSystem.EventActionAdded += OnActionAdded;
            }
            else
            {
                UndoSystem.EventActionPointerChanged -= OnActionPointerChanged;
                UndoSystem.EventActionDestroyed -= OnActionRemoved;
                UndoSystem.EventActionAdded -= OnActionAdded;
            }
        }

        private Vector2 m_ScrollPos;
        private const int KB = 1000;
        private const int MB = 1000 * KB;
    }
}