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

namespace XDay.WorldAPI.House.Editor
{
    public class RotatePrefab
    {
        [MenuItem("Assets/旋转房间模型")]
        private static void RotateHouseModel()
        {
            var prefab = Selection.activeGameObject;
            var type = PrefabUtility.GetPrefabAssetType(prefab);
            if (type == PrefabAssetType.Regular ||
                type == PrefabAssetType.Variant)
            {
                RotatePrefab r = new RotatePrefab();
                r.Rotate(prefab);
            }
        }

        public void Rotate(GameObject prefab)
        {
            var path = AssetDatabase.GetAssetPath(prefab);
            var obj = Object.Instantiate(prefab);
            var originalCollider = prefab.GetComponentInChildren<UnityEngine.BoxCollider>();
            var originalWorldBounds = Helper.CalculateBoxColliderWorldBounds(originalCollider);
            var newCollider = obj.GetComponentInChildren<UnityEngine.BoxCollider>();
            obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var newWorldBounds = Helper.CalculateBoxColliderWorldBounds(newCollider);
            var offset = originalWorldBounds.center - newWorldBounds.center;

            var prefabRoot = new GameObject(prefab.name + "_Rotated");
            obj.transform.SetParent(prefabRoot.transform);
            obj.transform.position = offset;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            Helper.DestroyUnityObject(prefabRoot);
            AssetDatabase.Refresh();
        }
    }
}
