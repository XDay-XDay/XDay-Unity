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

namespace XDay.GUIAPI.Editor
{
    [InitializeOnLoad]
    public static class UIBinderHierarchyDrawer
    {
        static UIBinderHierarchyDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!EditorHelper.IsPrefabMode())
            {
                return;
            }

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null)
            {
                return;
            }

            if (go.transform.parent == null &&
                go.GetComponent<Canvas>() != null)
            {
                if (DrawButton(selectionRect, "UI Builder", 50, 70))
                {
                    EditorWindow.GetWindow<UIBinder>().Show();
                }
            }

            var uiBuilder = EditorHelper.GetEditorWindow<UIBinder>();
            if (uiBuilder != null && uiBuilder.IsValid)
            {
                var localID = UIEditorHelper.GetPrefabContentsChildLocalID(go);
                var isBound = uiBuilder.IsBound(localID);
                var bound = DrawToggle(selectionRect, 0, 30, isBound);
                if (bound != isBound)
                {
                    if (bound || EditorUtility.DisplayDialog("Warning", "Continue?", "Yes", "No"))
                    {
                        uiBuilder.Bind(localID, bound);
                    }
                }
            }
        }

        private static bool DrawButton(Rect selectionRect, string text, float offset, float width)
        {
            if (GUI.Button(new Rect(selectionRect.x + selectionRect.width - width - offset, selectionRect.y, width, selectionRect.height), text))
            {
                return true;
            }
            return false;
        }

        private static bool DrawToggle(Rect selectionRect, float offset, float width, bool on)
        {
            return GUI.Toggle(new Rect(selectionRect.x + selectionRect.width - width - offset, selectionRect.y, width, selectionRect.height), on, "");
        }
    }
}