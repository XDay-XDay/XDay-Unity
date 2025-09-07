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
using UnityEditor.SceneManagement;
using UnityEngine;

namespace XDay.GUIAPI.Editor
{
    public static partial class UITo3D
    {
        [MenuItem("GameObject/UI转换到3D")]
        public static void ConvertSelectedUI3D()
        {
            var selection = Selection.activeGameObject;
            if (selection != null)
            {
                Convert(selection, null);
            }
        }

        [MenuItem("GameObject/UI转换到2D")]
        public static void ConvertSelectedUI2D()
        {
            var selection = Selection.activeGameObject;
            if (selection != null)
            {
                Convert(selection, null);
            }
        }

        [MenuItem("Assets/UI转换到3D")]
        public static void ConvertSelectedPrefab3D()
        {
            var selection = Selection.activeGameObject;
            if (selection != null)
            {
                Convert(selection, null);
            }
        }

        [MenuItem("Assets/UI转换到2D")]
        public static void ConvertSelectedPrefab2D()
        {
            var selection = Selection.activeGameObject;
            if (selection != null)
            {
                var setting = AssetDatabase.LoadAssetAtPath<UITo3DConvertSetting>("Assets/DamageTextConvert.asset");
                Convert(selection, setting);
            }
        }

        [MenuItem("GameObject/复制路径")]
        public static void CopyPath()
        {
            var selection = Selection.activeGameObject;
            if (selection != null)
            {
                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    var path = GetHierarchyPath(selection.transform, null);
                    GUIUtility.systemCopyBuffer = path;
                }
                else
                {
                    Debug.LogError("只支持Prefab模式下使用");
                }
            }
        }
    }
}
