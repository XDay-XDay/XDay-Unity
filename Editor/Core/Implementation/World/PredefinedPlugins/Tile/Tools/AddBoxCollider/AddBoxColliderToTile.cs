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

namespace XDay.WorldAPI.Tile.Editor
{
    internal class AddBoxColliderToTile
    {
        [MenuItem("XDay/地图/地块/给地块增加BoxCollider")]
        private static void AddBoxCollider()
        {
            var instance = new AddBoxColliderToTile();

            List<GameObject> prefabs = new();
            foreach (var obj in Selection.objects)
            {
                if (EditorHelper.IsPrefab(obj as GameObject))
                {
                    prefabs.Add(obj as GameObject);
                }
            }

            instance.Do(prefabs);
        }

        private void Do(List<GameObject> prefabs)
        {
            foreach (var obj in prefabs)
            {
                var renderers = obj.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (renderer.gameObject.GetComponent<UnityEngine.Collider>() == null)
                    {
                        renderer.gameObject.AddComponent<UnityEngine.BoxCollider>();
                    }
                }

                PrefabUtility.SavePrefabAsset(obj);
            }

            AssetDatabase.Refresh();
        }
    }
}
