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

namespace XDay.GUIAPI.Editor
{
    internal static class UIEditorHelper
    {
        public static long GetPrefabContentsChildLocalID(string path)
        {
            var prefab = EditorHelper.GetEditingPrefab();
            var child = prefab.FindChildInHierarchy(path, true);
            return EditorHelper.GetObjectLocalID(child);
        }

        public static long GetPrefabContentsChildComponentLocalID(string path, int componentIndex)
        {
            var prefab = EditorHelper.GetEditingPrefab();
            var child = prefab.FindChildInHierarchy(path, true);
            var component = child.GetComponentAtIndex(componentIndex);
            return EditorHelper.GetObjectLocalID(component);
        }

        public static long GetPrefabContentsChildLocalID(GameObject go)
        {
            var path = Helper.GetPathInHierarchy(go, true);
            return GetPrefabContentsChildLocalID(path);
        }

        public static long GetPrefabContentsChildComponentLocalID(GameObject go, int componentIndex)
        {
            var path = Helper.GetPathInHierarchy(go, true);
            return GetPrefabContentsChildComponentLocalID(path, componentIndex);
        }

        public static GameObject GetGameObjectFromGUIDAndLocalID(string guid, long localID)
        {
            GameObject ret = null;
            List<Component> components = new();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            Helper.Traverse(prefab.transform, false, (transform) =>
            {
                if (EditorHelper.GetObjectLocalID(transform.gameObject) == localID)
                {
                    ret = transform.gameObject;
                }
                else
                {
                    components.Clear();
                    transform.gameObject.GetComponents(components);
                    foreach (var component in components)
                    {
                        if (EditorHelper.GetObjectLocalID(component) == localID)
                        {
                            ret = transform.gameObject;
                        }
                    }
                }
            });
            return ret;
        }

        public static bool IsValidLocalID(string guid, long localID)
        {
            return GetGameObjectFromGUIDAndLocalID(guid, localID) != null;
        }
    }
}
